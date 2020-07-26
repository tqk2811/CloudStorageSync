#include "pch.h"
#include "Placeholders.h"
#include <sstream>

#include <propvarutil.h>
#include <propkey.h>
namespace CSS
{
    gcroot<Regex^> rg_extension = gcnew Regex(gcnew String(L"\\.[^\\.]+$"));
    gcroot<Regex^> rg_subname = gcnew Regex(gcnew String(L"\\(\\d+\\)$"));
    String^ GetFileExtension(String^& filename, bool isfolder)
    {
        Match^ m = rg_extension->Match(filename);
        String^ extension{ nullptr };
        if (!isfolder && m->Success)
        {
            extension = m->Value;
            filename = filename->Substring(0, filename->Length - extension->Length)->Trim();
        }
        return extension;
    }
    String^ FixFileName(String^ filename, String^ extension)
    {
        Match^ m = rg_subname->Match(filename);
        if (m->Success)
        {
            filename = filename->Substring(0, filename->Length - m->Value->Length)->Trim();
        }

        if ((filename->Length + String::IsNullOrEmpty(extension) ? 0 : extension->Length) >= 250)
            filename = filename->Substring(0, 250 - extension->Length);

        return filename;
    }
    String^ FindNewFileName(SyncRootViewModel^ srvm, String^ ParentDirectory, String^ filename, String^ extension, CloudItem^ ci)
    {
        int i = 0;
        bool isfolder = ci->Size == -1;
        do
        {
            i++;
            String^ newpath = String::Format(L"{0}\\{1} ({2}){3}", ParentDirectory, filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
            PinStr(newpath);
            DWORD dw = GetFileAttributes(pin_newpath);
            if (INVALID_FILE_ATTRIBUTES == dw) break;//file not found
            else
            {
                if (((dw & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY) != isfolder) continue;//file != folder -> continue
                else
                {
                    CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(pin_newpath);
                    if (state == CF_PLACEHOLDER_STATE_INVALID ||
                        !(state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)
                    {
                        //not placeholder or invalid
                        if (isfolder) break;//-> convert
                        else
                        {
                            if (srvm->Status.HasFlag(SyncRootStatus::CreatingPlaceholder)) srvm->Message = String::Format(L"Checking hash file: {0}", filename);
                            if (srvm->CEVM->Cloud->HashCheck(newpath, ci)) break;//same hash -> convert
                            else continue;//diff hash
                        }                       
                    }
                }
                
            }
        } while (true);
        return String::Format(L"{0} ({1}){2}", filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
    }
    String^ FindNewNameItem(SyncRootViewModel^ srvm, String^ parentFullPath, CloudItem^ ci)
    {
        String^ Name = ci->Name->Trim();
        String^ extension = GetFileExtension(Name, ci->Size == -1);
        Name = FixFileName(Name, extension);
        return FindNewFileName(srvm, parentFullPath, Name, extension, ci);
    }


    void Placeholders::CreateAll(SyncRootViewModel^ srvm, String^ CI_ParentId, LONGLONG LI_ParentId,String^ RelativeOfParent)
    {
        if (srvm) 
        {
            IList<CloudItem^>^ childsci = CloudItem::FindChildIds(CI_ParentId, srvm->CEVM->EmailSqlId);
            for (int i = 0; i < childsci->Count; i++)
            {
                if (!srvm->IsWork)
                {
                    srvm->Status = SyncRootStatus::Error | SyncRootStatus::CreatingPlaceholder;
                    srvm->Message = gcnew String("Cancelling");
                    return;
                }
                LocalItem^ localitem = CreateItem(srvm, LI_ParentId, RelativeOfParent, childsci[i]);
                if (localitem)
                {
                    if (srvm->Status.HasFlag(SyncRootStatus::CreatingPlaceholder)) srvm->Message = String::Format(L"ItemCreated: {0}", localitem->Name);
                    if (localitem->LocalId > 0 && childsci[i]->Size == -1)
                    {
                        String^ itemRelative = RelativeOfParent;
                        if (String::IsNullOrEmpty(itemRelative)) itemRelative = localitem->Name;
                        else itemRelative = itemRelative + L"\\" + localitem->Name;
                        CreateAll(srvm, childsci[i]->Id, localitem->LocalId, itemRelative);
                    }
                }
            }
        }
    }

    LocalItem^ Placeholders::CreateItem(SyncRootViewModel^ srvm,LONGLONG LI_ParentId, String^ Relative, CloudItem^ clouditem)
    {
        if (!srvm || !clouditem) return nullptr;

        bool cloud_isfolder = clouditem->Size == -1;
        bool convert_to_placeholder(false);
        bool create_placeholder(false);
        bool rename_cloud(false);
        bool file_exist(false);
        bool create_hardlink(false);

        clouditem->Name = CssCs::Extensions::RenameFileNameUnInvalid(clouditem->Name, clouditem->Size != -1);
        LocalItem^ localitem = LocalItem::Find(srvm, LI_ParentId, clouditem->Name);

        String^ fullPathItemParent = srvm->LocalPath;
        String^ relativeItem;
        if (String::IsNullOrEmpty(Relative)) relativeItem = clouditem->Name;
        else
        {
            fullPathItemParent = fullPathItemParent + L"\\" + Relative;
            relativeItem = Relative + L"\\" + clouditem->Name;
        }
        String^ fullPathItem = srvm->LocalPath + L"\\" + relativeItem;

        PinStr(fullPathItem);
        PinStr(relativeItem);
        PinStr2(pin_LocalPath, srvm->LocalPath);

        DWORD attribs = GetFileAttributes(pin_fullPathItem);
        file_exist = INVALID_FILE_ATTRIBUTES != attribs;

        if (!file_exist) create_placeholder = true;
        else
        {
            /*CF_PLACEHOLDER_STATE state = GetPlaceholderState(pin_fullPathItem);
            if (state != CF_PLACEHOLDER_STATE_INVALID && (state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)
            {
                if (localitem && !String::IsNullOrEmpty(localitem->CloudId) && localitem->CloudId->Equals(clouditem->Id))
                {*/
            if (localitem && !String::IsNullOrEmpty(localitem->CloudId))
            {
                if (!localitem->CloudId->Equals(clouditem->Id))
                {
                    //diff id
                    rename_cloud = true;
                    create_placeholder = true;
                }
            }
            else
            {
                //item is not placeholder
                bool localIsFolder = attribs & FILE_ATTRIBUTE_DIRECTORY;
                if (localIsFolder && cloud_isfolder) convert_to_placeholder = true;//same folder
                else if (!localIsFolder && !cloud_isfolder)//same file
                {
                    if (srvm->Status.HasFlag(SyncRootStatus::CreatingPlaceholder)) srvm->Message = String::Format(L"Checking hash file: {0}", clouditem->Name);
                    if (srvm->CEVM->Cloud->HashCheck(fullPathItem, clouditem))
                    {
                        //same size and hash
                        convert_to_placeholder = true;
                    }
                    else//diff size / hash
                    {
                        rename_cloud = true;
                        create_placeholder = true;
                    }
                }
                else//file != folder
                {
                    rename_cloud = true;
                    create_placeholder = true;
                }
            }
        }

        if (rename_cloud)
        {
            clouditem->Name = FindNewNameItem(srvm, fullPathItemParent, clouditem);
            fullPathItem = fullPathItemParent + L"\\" + clouditem->Name;            
            relativeItem = fullPathItem->Substring(srvm->LocalPath->Length + 1);

            PinStr3(pin_fullPathItem, fullPathItem);
            PinStr3(pin_relativeItem, relativeItem);

            attribs = GetFileAttributes(pin_fullPathItem);
            file_exist = INVALID_FILE_ATTRIBUTES != attribs;
            if (file_exist)
            {
                localitem = LocalItem::Find(srvm, LI_ParentId, clouditem->Name);
                convert_to_placeholder = true;
            }
        }

        if (localitem)
        {
            if (localitem->Flag.HasFlag(LocalItemFlag::LockWaitUpdateFromCloudWatch)) localitem->RemoveFlagWithLock(LocalItemFlag::LockWaitUpdateFromCloudWatch);
            localitem->Update();
        }
        else
        {
            localitem = gcnew LocalItem();
            localitem->CloudId = clouditem->Id;
            localitem->Name = clouditem->Name;
            if (clouditem->Size == -1) localitem->Flag = LocalItemFlag::Folder;
            localitem->SRId = srvm->SRId;
            localitem->LocalParentId = LI_ParentId;
            localitem->Insert();
        }


        if (file_exist)
        {
            if(convert_to_placeholder) Convert(srvm, localitem, clouditem->Id);
        }
        else
        {
            if (create_placeholder) Create(pin_LocalPath, pin_relativeItem, clouditem);
            else if (create_hardlink) CreateHardLink(pin_fullPathItem, L"", NULL);
        }
        return localitem;
    }

    bool Placeholders::Create(LPCWSTR syncRootPath, LPCWSTR relativePathItem, CloudItem^ clouditem)
    {
        std::wstring fullpath(syncRootPath);
        fullpath.append(L"\\").append(relativePathItem);

        CF_PLACEHOLDER_CREATE_INFO cloudEntry{ 0 };
        char FileIdentity[LengthFileIdentity]{ 0 };
        FillFileIdentity(FileIdentity, clouditem->Id);

        cloudEntry.FileIdentity = FileIdentity;
        cloudEntry.FileIdentityLength = LengthFileIdentity;

        cloudEntry.FsMetadata.BasicInfo.CreationTime.QuadPart = CssCs::Extensions::GetFileTime(clouditem->DateCreate);
        cloudEntry.FsMetadata.BasicInfo.ChangeTime.QuadPart = CssCs::Extensions::GetFileTime(clouditem->DateMod);
        cloudEntry.FsMetadata.BasicInfo.LastAccessTime.QuadPart = cloudEntry.FsMetadata.BasicInfo.ChangeTime.QuadPart;
        cloudEntry.FsMetadata.BasicInfo.LastWriteTime.QuadPart = cloudEntry.FsMetadata.BasicInfo.ChangeTime.QuadPart;
        cloudEntry.FsMetadata.BasicInfo.FileAttributes = clouditem->Size == -1 ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
        cloudEntry.FsMetadata.FileSize.QuadPart = clouditem->Size == -1 ? 0 : clouditem->Size;

        cloudEntry.RelativeFileName = relativePathItem;
        cloudEntry.Flags = clouditem->Size == -1 ?
            CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC | CF_PLACEHOLDER_CREATE_FLAG_DISABLE_ON_DEMAND_POPULATION : CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC;

        HRESULT hr = CfCreatePlaceholders(syncRootPath, &cloudEntry, 1, CF_CREATE_FLAG_NONE, NULL);
        
        if (CheckHr(hr,L"Placeholders::Create CfCreatePlaceholders", fullpath.c_str(),true)) return true;
        else return false;
    }

    bool Placeholders::Revert(SyncRootViewModel^ srvm, LocalItem^ li)
    {
        bool result{ false };        
        if (li)
        {
            String^ fullPathItem = li->GetFullPath();
            PinStr(fullPathItem);
            HANDLE hfile = CreateFile(pin_fullPathItem, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
            if (INVALID_HANDLE_VALUE != hfile)
            {
                HRESULT hr = CfRevertPlaceholder(hfile, CF_REVERT_FLAG_NONE, nullptr);
                if (CheckHr(hr, L"Placeholders::Revert CfRevertPlaceholder", pin_fullPathItem), true) result = true;
                CloseHandle(hfile);
            }
            else
            {
                if (PathExists(pin_fullPathItem)) WriteLog(String::Format(L"Placeholders::Revert can't OpenFile path:{0}", fullPathItem), 0);
                else
                {
                    WriteLog(String::Format(L"Placeholders::Revert Path File doesn't Exists path:{0}", fullPathItem), 0);
                    li->Delete(true);
                }
            }
        }
        else
        {
            LogWriter::WriteLog(L"Placeholders::Revert LocalItem is null", 0);
        }
        return result;
    }

    //To update a placeholder :
    //    The placeholder to be updated must be contained in a registered sync root tree; it can be the sync root directory itself, or any descendant directory; 
    //        otherwise, the call with be failed with HRESULT(ERROR_CLOUD_FILE_NOT_UNDER_SYNC_ROOT).
    //    If dehydration is requested, the sync root must be registered with a valid hydration policy that is not CF_HYDRATION_POLICY_ALWAYS_FULL; 
    //        otherwise the call will be failed with HRESULT(ERROR_CLOUD_FILE_NOT_SUPPORTED).
    //    If dehydration is requested, the placeholder must not be pinned locally or the call with be failed with HRESULT(ERROR_CLOUD_FILE_PINNED).
    //    If dehydration is requested, the placeholder must be in sync or the call with be failed with HRESULT(ERROR_CLOUD_FILE_NOT_IN_SYNC).
    //    The caller must have WRITE_DATA or WRITE_DAC access to the placeholder to be updated.
    //        Otherwise the operation will be failed with HRESULT(ERROR_CLOUD_FILE_ACCESS_DENIED).
    bool Placeholders::Update(SyncRootViewModel^ srvm, LocalItem^ li, CloudItem^ clouditem, bool InsertErrorDb)
    {
        bool result{ false };
        bool tryagain{ false };
        if (li)
        {
            String^ fullPathItem = li->GetFullPath();
            PinStr(fullPathItem);
            HANDLE hfile = CreateFile(pin_fullPathItem, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
            if (INVALID_HANDLE_VALUE != hfile)
            {
                CF_FS_METADATA metadata{ 0 };
                metadata.BasicInfo.CreationTime.QuadPart = CssCs::Extensions::GetFileTime(clouditem->DateCreate);
                metadata.BasicInfo.ChangeTime.QuadPart = CssCs::Extensions::GetFileTime(clouditem->DateMod);
                metadata.BasicInfo.LastAccessTime.QuadPart = metadata.BasicInfo.ChangeTime.QuadPart;
                metadata.BasicInfo.LastWriteTime.QuadPart = metadata.BasicInfo.ChangeTime.QuadPart;
                metadata.BasicInfo.FileAttributes = clouditem->Size == -1 ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                metadata.FileSize.QuadPart = clouditem->Size == -1 ? 0 : clouditem->Size;
                CF_FILE_RANGE filerange{ 0 };
                filerange.Length.QuadPart = MAXLONGLONG;
                filerange.StartingOffset.QuadPart = 0;
                DWORD dehydrateRangeCount(0);
                USN usn{ 0 };
                char FileIdentity[LengthFileIdentity]{ 0 };
                FillFileIdentity(FileIdentity, clouditem->Id);

                HRESULT hr = CfUpdatePlaceholder(
                    hfile,
                    &metadata,
                    FileIdentity,
                    LengthFileIdentity,
                    &filerange,
                    dehydrateRangeCount,
                    metadata.BasicInfo.FileAttributes & FILE_ATTRIBUTE_DIRECTORY ? CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAG_DISABLE_ON_DEMAND_POPULATION : CF_UPDATE_FLAG_MARK_IN_SYNC,
                    &usn,
                    nullptr);

                if (CheckHr(hr, L"Placeholders::Update CfUpdatePlaceholder", pin_fullPathItem,true)) result = true;
                else if (HR_FileOpenningByOtherProcess == hr || HR_InUse == hr) tryagain = true;

                CloseHandle(hfile);
            }
            else//file can't open
            {
                if (PathExists(pin_fullPathItem)) tryagain = true;//file found -> try again
                else li->Delete(true);//file not found, delete local item.
            }
        }
        else
        {
            WriteLog(String::Format(L"Placeholders::Update LocalItem is null,CloudItem:{0}", clouditem), 0);
        }

        if(InsertErrorDb && tryagain) LocalError::Insert(li->LocalId, srvm->SRId, LocalErrorType::Update, clouditem->Id);
        return result;
    }

    //To convert a placeholder :
    //  The file or directory to be converted must be contained in a registered sync root tree; it can be the sync root directory itself, or any descendant directory;
    //      otherwise, the call with be failed with HRESULT(ERROR_CLOUD_FILE_NOT_UNDER_SYNC_ROOT).
    //  If dehydration is requested, the sync root must be registered with a valid hydration policy that is not CF_HYDRATION_POLICY_ALWAYS_FULL; 
    //      otherwise the call will be failed with HRESULT(ERROR_CLOUD_FILE_NOT_SUPPORTED).
    //  If dehydration is requested, the placeholder must not be pinned locally or the call with be failed with HRESULT(ERROR_CLOUD_FILE_PINNED).
    //  If dehydration is requested, the placeholder must be in sync or the call with be failed with HRESULT(ERROR_CLOUD_FILE_NOT_IN_SYNC).
    //  The caller must have WRITE_DATA or WRITE_DAC access to the file or directory to be converted.
    //     Otherwise the operation will be failed with HRESULT(ERROR_CLOUD_FILE_ACCESS_DENIED).
    bool Placeholders::Convert(SyncRootViewModel^ srvm, LocalItem^ li, String^ fileIdentity, bool InsertErrorDb)
    {
        bool result{ false };
        bool tryagain{ false };
        if (li)
        {
            String^ fullPathItem = li->GetFullPath();
            PinStr(fullPathItem);
            HANDLE hfile = CreateFile(pin_fullPathItem, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
            if (INVALID_HANDLE_VALUE != hfile)
            {
                USN usn{ 0 };
                char FileIdentity[LengthFileIdentity]{ 0 };
                FillFileIdentity(FileIdentity, fileIdentity);
                //doc recommend oplock
                HRESULT hr = CfConvertToPlaceholder(hfile, FileIdentity, LengthFileIdentity, CF_CONVERT_FLAG_MARK_IN_SYNC, &usn, nullptr);

                if (CheckHr(hr, L"Placeholders::Convert CfConvertToPlaceholder", pin_fullPathItem,true)) result = true;
                else if (HR_FileOpenningByOtherProcess == hr || HR_InUse == hr) tryagain = true;

                CloseHandle(hfile);
            }
            else//file can't open
            {
                if (PathExists(pin_fullPathItem)) tryagain = true;//file found -> try again
                else li->Delete(true);//file not found, delete local item.
            }
        }
        else
        {
            WriteLog(String::Format(L"Placeholders::Convert LocalItem is null,fileIdentity:{0}", fileIdentity), 0);
        }
        if (InsertErrorDb && tryagain) LocalError::Insert(li->LocalId, srvm->SRId, LocalErrorType::Convert, fileIdentity);
        return result;
    }

    bool Placeholders::Hydrate(SyncRootViewModel^ srvm, LocalItem^ li, bool InsertErrorDb)
    {
        bool result{ false };
        HRESULT hr{ 0 };
        if (li)
        {
            String^ fullPathItem = li->GetFullPath();
            PinStr(fullPathItem);
            DWORD attrib = GetFileAttributes(pin_fullPathItem);
            if ((attrib != INVALID_FILE_ATTRIBUTES) && 
                !(attrib & FILE_ATTRIBUTE_DIRECTORY) && //skip if folder
                (attrib & FILE_ATTRIBUTE_PINNED))
            {
                HANDLE hfile = CreateFile(pin_fullPathItem, 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr);
                if (hfile == INVALID_HANDLE_VALUE)
                {
                    LogWriter::WriteLogError(std::wstring(L"Placeholders::Hydrate CreateFile error:").append(pin_fullPathItem).c_str(), (int)GetLastError());
                    return result;
                }
                else
                {
                    LARGE_INTEGER offset = { 0 };
                    LARGE_INTEGER length;
                    length.QuadPart = MAXLONGLONG;
                    HRESULT hr;
                    hr = CfHydratePlaceholder(hfile, offset, length, CF_HYDRATE_FLAG_NONE, NULL);
                    if (CheckHr(hr, L"Placeholders::Hydrate CfHydratePlaceholder", pin_fullPathItem), true) result = true;
                    CloseHandle(hfile);
                }
            }
        }
        else
        {
            LogWriter::WriteLog(L"Placeholders::Hydrate LocalItem is null", 0);
        }
        return result;
    }

    bool Placeholders::Dehydrate(SyncRootViewModel^ srvm, LocalItem^ li, bool InsertErrorDb)
    {
        bool result{ false };
        HRESULT hr{ 0 };
        if (li)
        {
            String^ fullPathItem = li->GetFullPath();
            PinStr(fullPathItem);
            DWORD attrib = GetFileAttributes(pin_fullPathItem);
            if ((attrib != INVALID_FILE_ATTRIBUTES) && 
                !(attrib & FILE_ATTRIBUTE_DIRECTORY) && 
                (attrib & FILE_ATTRIBUTE_UNPINNED))
            {
                HANDLE hfile = CreateFile(pin_fullPathItem, 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr);
                if (hfile == INVALID_HANDLE_VALUE)
                {
                    LogWriter::WriteLogError(std::wstring(L"Placeholders::Dehydrate CreateFile error:").append(pin_fullPathItem).c_str(), (int)GetLastError());
                }
                else
                {
                    LARGE_INTEGER offset = { 0 };
                    LARGE_INTEGER length;
                    length.QuadPart = MAXLONGLONG;
                    HRESULT hr;
                    hr = CfDehydratePlaceholder(hfile, offset, length, CF_DEHYDRATE_FLAG_BACKGROUND, NULL);
                    if (CheckHr(hr, L"Placeholders::Dehydrate CfDehydratePlaceholder", pin_fullPathItem,true)) result = true;
                    CloseHandle(hfile);
                }
            }
        }
        else
        {
            LogWriter::WriteLog(L"Placeholders::Dehydrate LocalItem is null", 0);
        }
        return result;
    }

    //bool Placeholders::SetInSyncState(SyncRootViewModel^ srvm, LocalItem^ li, CF_IN_SYNC_STATE state)
    //{
    //    bool result{ false };
    //    if (li)
    //    {
    //        String^ fullPathItem = li->GetFullPath(srvm->LocalPath);
    //        PinStr(fullPathItem);
    //        HANDLE hfile{ 0 };
    //        HRESULT hr = CfOpenFileWithOplock(pin_fullPathItem, CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAG_EXCLUSIVE, &hfile);
    //        if (CheckHr(hr, L"Placeholders::SetInSyncState CfOpenFileWithOplock", pin_fullPathItem))
    //        {
    //            USN usn;
    //            hr = CfSetInSyncState(hfile, state, CF_SET_IN_SYNC_FLAG_NONE, &usn);
    //            if (CheckHr(hr, L"Placeholders::SetInSyncState CfSetInSyncState", pin_fullPathItem))
    //            {
    //                LogWriter::WriteLog(std::wstring(L"CSS::Placeholders::SetInSyncState SRSetInSyncState Success path:").append(pin_fullPathItem), 1);
    //                result = true;
    //            }
    //            CfCloseHandle(hfile);
    //        }
    //    }        
    //    return result;
    //}

    //bool Placeholders::GetPlaceholderStandarInfo(LPCWSTR fullPathItem, MY_CF_PLACEHOLDER_STANDARD_INFO* info)
    //{
    //    HANDLE hfile = CreateFile(fullPathItem, 0, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
    //    if (INVALID_HANDLE_VALUE != hfile)
    //    {
    //        DWORD returnlength{ 0 };
    //        HRESULT hr = CfGetPlaceholderInfo(hfile, CF_PLACEHOLDER_INFO_STANDARD, info, sizeof(MY_CF_PLACEHOLDER_STANDARD_INFO), &returnlength);
    //        CloseHandle(hfile);
    //        if(CheckHr(hr, L"Placeholders::GetPlaceholderStandarInfo SRGetPlaceholderInfo", fullPathItem)) return true;
    //    }
    //    return false;
    //}

    //only work after connectsyncroot
    CF_PLACEHOLDER_STATE Placeholders::GetPlaceholderState(LPCWSTR fullPathItem)
    {
        CF_PLACEHOLDER_STATE state = CF_PLACEHOLDER_STATE_INVALID;
        FILE_ATTRIBUTE_TAG_INFO info{};
        HANDLE hfile = CreateFile(fullPathItem, 0, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
        if (INVALID_HANDLE_VALUE != hfile && GetFileInformationByHandleEx(hfile, FILE_INFO_BY_HANDLE_CLASS::FileAttributeTagInfo, &info, sizeof(FILE_ATTRIBUTE_TAG_INFO)))
        {
            state = CfGetPlaceholderStateFromAttributeTag(info.FileAttributes, info.ReparseTag);
        }
        else
        {
            int err = GetLastError();
        }
        CloseHandle(hfile);
        return state;
    }

    //CF_PLACEHOLDER_STATE Placeholders::GetPlaceholderState(HANDLE hfile)
    //{
    //    FILE_ATTRIBUTE_TAG_INFO info{ 0 };
    //    if (GetFileInformationByHandleEx(hfile, FILE_INFO_BY_HANDLE_CLASS::FileAttributeTagInfo, &info, sizeof(FILE_ATTRIBUTE_TAG_INFO)))
    //        return CfGetPlaceholderStateFromAttributeTag(info.FileAttributes, info.ReparseTag);
    //    else return CF_PLACEHOLDER_STATE::CF_PLACEHOLDER_STATE_INVALID;
    //}
}