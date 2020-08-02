#include "pch.h"
#include "Placeholders.h"
#include <sstream>

#include <propvarutil.h>
#include <propkey.h>
namespace CSS
{
    bool GetFileInformation(LPCWSTR fullpath, BY_HANDLE_FILE_INFORMATION& info)
    {
        HANDLE hfile = CreateFile(fullpath, 0, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
        bool result{ false };
        if (INVALID_HANDLE_VALUE != hfile) result = GetFileInformationByHandle(hfile, &info);
        CloseHandle(hfile);
        return result;
    }
    static bool TwoItemIsHardLink(LPCWSTR fullpath0, LPCWSTR fullpath1)
    {
        BY_HANDLE_FILE_INFORMATION info0{};
        BY_HANDLE_FILE_INFORMATION info1{};
        if (GetFileInformation(fullpath0, info0) && GetFileInformation(fullpath1, info1) &&
            info0.dwVolumeSerialNumber == info1.dwVolumeSerialNumber &&
            info0.nFileIndexLow == info1.nFileIndexLow && info0.nFileIndexHigh == info1.nFileIndexHigh) return true;
        return false;
    }


    void Placeholders::CreateAll(SyncRootViewModel^ srvm, LocalItem^ li, String^ RelativeOfParent)
    {
        if (srvm && li)
        {
            IList<CloudItem^>^ childsci = CloudItem::FindChildIds(li->CloudId, srvm->CEVM->EmailSqlId);
            for (int i = 0; i < childsci->Count; i++)
            {
                if (!srvm->IsWork)
                {
                    srvm->Status = SyncRootStatus::Error | SyncRootStatus::CreatingPlaceholder;
                    srvm->Message = gcnew String("Cancelling");
                    return;
                }

                LocalItem^ itemCreated = CreateItem(srvm, li, RelativeOfParent, childsci[i]);
                if (itemCreated)
                {
                    if (srvm->Status.HasFlag(SyncRootStatus::CreatingPlaceholder)) srvm->Message = String::Format(L"Item Created: {0}", itemCreated->Name);
                    if (childsci[i]->Size == -1)
                    {
                        String^ itemRelative = RelativeOfParent;
                        if (String::IsNullOrEmpty(itemRelative)) itemRelative = itemCreated->Name;
                        else itemRelative = itemRelative + L"\\" + itemCreated->Name;
                        CreateAll(srvm, itemCreated, itemRelative);
                    }
                }
            }
        }
    }

    LocalItem^ Placeholders::CreateItem(SyncRootViewModel^ srvm, LocalItem^ li_parent, String^ parentRelative, CloudItem^ clouditem)
    {
        if (!clouditem || !li_parent) return nullptr;
        bool cloud_isfolder = clouditem->Size == -1;
        clouditem->Name = CssCs::Extensions::RenameFileNameUnInvalid(clouditem->Name, !cloud_isfolder);
        LocalItem^ localitem = li_parent->Childs->Find(clouditem->Id, clouditem->Name);//for upload/create local -> send to cloud -> watch back
        if (localitem)
        {
            //unlock?
            return localitem;
        }

        if (!srvm || !parentRelative) return nullptr;
        bool convert_to_placeholder(false);
        bool create_placeholder(false);
        bool rename_cloud(false);
        bool file_exist(false);
        bool create_hardlink(false);
        localitem = srvm->Root->FindFromCloudId(clouditem->Id);
        String^ fileRefPath = String::Empty;
        if(localitem) fileRefPath = localitem->GetFullPath()->ToString();

        String^ fullPathItemParent = srvm->LocalPath;
        String^ relativeItem;
        if (String::IsNullOrEmpty(parentRelative)) relativeItem = clouditem->Name;
        else
        {
            fullPathItemParent = fullPathItemParent + L"\\" + parentRelative;
            relativeItem = parentRelative + L"\\" + clouditem->Name;
        }
        String^ fullPathItem = srvm->LocalPath + L"\\" + relativeItem;

        PinStr(fullPathItem);
        PinStr(relativeItem);
        PinStr2(pin_LocalPath, srvm->LocalPath);
        PinStr(fileRefPath);

        DWORD attribs = GetFileAttributes(pin_fullPathItem);
        file_exist = INVALID_FILE_ATTRIBUTES != attribs;

        if (!file_exist)//file not exist
        {
            if (localitem) create_hardlink = true;
            else create_placeholder = true;
        }
        else//if file_exist
        {
            bool localIsFolder = attribs & FILE_ATTRIBUTE_DIRECTORY;
            if (localitem)//create hardlink
            {
                if (TwoItemIsHardLink(pin_fullPathItem, pin_fileRefPath))
                {
                    LocalItem^ li = gcnew LocalItem(srvm, clouditem->Id);
                    li_parent->Childs->Add(li);
                    return li;
                }
                else
                {
                    rename_cloud = true;
                    create_hardlink = true;
                }
            }
            else
            {
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
                else //file != folder
                {
                    rename_cloud = true;
                    create_placeholder = true;
                }
            }
        }

        if (rename_cloud)
        {
            clouditem->Name = FindNewNameItem(srvm, fullPathItemParent, clouditem, !create_hardlink);//if create_hardlink -> file_exist = false
            fullPathItem = fullPathItemParent + L"\\" + clouditem->Name;            
            relativeItem = fullPathItem->Substring(srvm->LocalPath->Length + 1);

            PinStr3(pin_fullPathItem, fullPathItem);
            PinStr3(pin_relativeItem, relativeItem);

            attribs = GetFileAttributes(pin_fullPathItem);
            file_exist = INVALID_FILE_ATTRIBUTES != attribs;
            if (file_exist)
            {
                //localitem = LocalItem::Find(srvm, LI_ParentId, clouditem->Name);
                convert_to_placeholder = true;
            }
        }

        LocalItem^ item = nullptr;
        if (create_hardlink)
        {
            if (CreateHardLink(pin_fullPathItem, pin_fileRefPath, NULL))
            {
                item = gcnew LocalItem(srvm, clouditem->Id);
                li_parent->Childs->Add(item);
                return item;
            }
        }

        if (convert_to_placeholder)
        {
            Convert(srvm, pin_fullPathItem, clouditem->Id);
            item = gcnew LocalItem(srvm, clouditem->Id);
            li_parent->Childs->Add(item);
            return item;
        }

        if (create_placeholder) 
        {
            Create(pin_LocalPath, pin_relativeItem, clouditem);
            item = gcnew LocalItem(srvm, clouditem->Id);
            li_parent->Childs->Add(item);
            return item;
        }
        return item;
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

    bool Placeholders::Revert(SyncRootViewModel^ srvm, LPCWSTR fullPathItem)
    {
        bool result{ false };
        HANDLE hfile = CreateFile(fullPathItem, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
        if (INVALID_HANDLE_VALUE != hfile)
        {
            HRESULT hr = CfRevertPlaceholder(hfile, CF_REVERT_FLAG_NONE, nullptr);
            if (CheckHr(hr, L"Placeholders::Revert CfRevertPlaceholder", fullPathItem), true) result = true;
            CloseHandle(hfile);
        }
        else
        {
            if (PathExists(fullPathItem))
            {
                WriteLog(String::Format(L"Placeholders::Revert can't OpenFile path:{0}", gcnew String(fullPathItem)), 0);
            }
            else
            {
                WriteLog(String::Format(L"Placeholders::Revert Path File doesn't Exists path:{0}", gcnew String(fullPathItem)), 0);
            }
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
    bool Placeholders::Update(SyncRootViewModel^ srvm, LPCWSTR fullPathItem, CloudItem^ clouditem)
    {
        bool result{ false };
        bool tryagain{ false };
        HANDLE hfile = CreateFile(fullPathItem, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
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
                metadata.BasicInfo.FileAttributes & FILE_ATTRIBUTE_DIRECTORY ? (CF_UPDATE_FLAG_MARK_IN_SYNC | CF_UPDATE_FLAG_DISABLE_ON_DEMAND_POPULATION) : CF_UPDATE_FLAG_MARK_IN_SYNC,
                &usn,
                nullptr);
            if (CheckHr(hr, L"Placeholders::Update CfUpdatePlaceholder", fullPathItem, true)) result = true;
            CloseHandle(hfile);
        }
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
    bool Placeholders::Convert(SyncRootViewModel^ srvm, LPCWSTR fullPathItem, String^ fileIdentity)
    {
        bool result{ false };
        bool tryagain{ false };
        HANDLE hfile = CreateFile(fullPathItem, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
        if (INVALID_HANDLE_VALUE != hfile)
        {
            USN usn{ 0 };
            char FileIdentity[LengthFileIdentity]{ 0 };
            FillFileIdentity(FileIdentity, fileIdentity);
            //doc recommend oplock
            HRESULT hr = CfConvertToPlaceholder(hfile, FileIdentity, LengthFileIdentity, CF_CONVERT_FLAG_MARK_IN_SYNC, &usn, nullptr);

            if (CheckHr(hr, L"Placeholders::Convert CfConvertToPlaceholder", fullPathItem, true)) result = true;
            CloseHandle(hfile);
        }
        return result;
    }

    bool Placeholders::Hydrate(SyncRootViewModel^ srvm, LPCWSTR fullPathItem)
    {
        bool result{ false };
        HRESULT hr{ 0 };
        DWORD attrib = GetFileAttributes(fullPathItem);
        if ((attrib != INVALID_FILE_ATTRIBUTES) &&
            !(attrib & FILE_ATTRIBUTE_DIRECTORY) && //skip if folder
            (attrib & FILE_ATTRIBUTE_PINNED))
        {
            HANDLE hfile = CreateFile(fullPathItem, 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr);
            if (hfile == INVALID_HANDLE_VALUE)
            {
                LogWriter::WriteLogError(std::wstring(L"Placeholders::Hydrate CreateFile error:").append(fullPathItem).c_str(), (int)GetLastError());
                return result;
            }
            else
            {
                LARGE_INTEGER offset = { 0 };
                LARGE_INTEGER length;
                length.QuadPart = MAXLONGLONG;
                HRESULT hr;
                hr = CfHydratePlaceholder(hfile, offset, length, CF_HYDRATE_FLAG_NONE, NULL);
                if (CheckHr(hr, L"Placeholders::Hydrate CfHydratePlaceholder", fullPathItem), true) result = true;
                CloseHandle(hfile);
            }
        }
        return result;
    }

    bool Placeholders::Dehydrate(SyncRootViewModel^ srvm, LPCWSTR fullPathItem)
    {
        bool result{ false };
        HRESULT hr{ 0 };
        DWORD attrib = GetFileAttributes(fullPathItem);
        if ((attrib != INVALID_FILE_ATTRIBUTES) &&
            !(attrib & FILE_ATTRIBUTE_DIRECTORY) &&
            (attrib & FILE_ATTRIBUTE_UNPINNED))
        {
            HANDLE hfile = CreateFile(fullPathItem, 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr);
            if (hfile == INVALID_HANDLE_VALUE)
            {
                LogWriter::WriteLogError(std::wstring(L"Placeholders::Dehydrate CreateFile error:").append(fullPathItem).c_str(), (int)GetLastError());
            }
            else
            {
                LARGE_INTEGER offset = { 0 };
                LARGE_INTEGER length;
                length.QuadPart = MAXLONGLONG;
                HRESULT hr;
                hr = CfDehydratePlaceholder(hfile, offset, length, CF_DEHYDRATE_FLAG_NONE, NULL);
                if (CheckHr(hr, L"Placeholders::Dehydrate CfDehydratePlaceholder", fullPathItem, true)) result = true;
                CloseHandle(hfile);
            }
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

    bool Placeholders::GetPlaceholderStandarInfo(LPCWSTR fullPathItem, MY_CF_PLACEHOLDER_STANDARD_INFO* info)
    {
        HANDLE hfile = CreateFile(fullPathItem, 0, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
        if (INVALID_HANDLE_VALUE != hfile)
        {
            DWORD returnlength{ 0 };
            HRESULT hr = CfGetPlaceholderInfo(hfile, CF_PLACEHOLDER_INFO_STANDARD, info, sizeof(MY_CF_PLACEHOLDER_STANDARD_INFO), &returnlength);
            CloseHandle(hfile);
            if(CheckHr(hr, L"Placeholders::GetPlaceholderStandarInfo SRGetPlaceholderInfo", fullPathItem)) return true;
        }
        return false;
    }

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