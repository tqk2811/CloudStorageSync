#include "pch.h"
#include "Placeholders.h"
namespace CSS
{
    void Placeholders::CreateAll(SyncRootViewModel^ srvm, LocalItem^ parent, CloudItem^ ci_parent,  String^ RelativeOfParent)
    {
        if (srvm && parent && ci_parent)
        {
            for each(CloudItem^ child in ci_parent->GetChilds())
            {
                LocalItem^ localitem = CreateItem(srvm, parent, RelativeOfParent, child);
                if (localitem)
                {
                    if (srvm->EnumStatus == SyncRootStatus::CreatingPlaceholder) srvm->Message = String::Format(CultureInfo::InvariantCulture, L"ItemCreated: {0}", localitem->Name);
                    if (child->Size == -1)
                    {
                        String^ itemRelative = RelativeOfParent;
                        if (String::IsNullOrEmpty(itemRelative)) itemRelative = localitem->Name;
                        else itemRelative = itemRelative + L"\\" + localitem->Name;
                        CreateAll(srvm, localitem, child, itemRelative);
                    }
                }
            }
        }
    }

    LocalItem^ Placeholders::CreateItem(SyncRootViewModel^ srvm,LocalItem^ parent, String^ ParentRelative, CloudItem^ clouditem)
    {
        if (!srvm || !parent || !clouditem) return nullptr;
        if (!String::IsNullOrEmpty(ParentRelative))
        {
            if (ParentRelative[0] == L'\\') ParentRelative = ParentRelative->Substring(1);
            if (ParentRelative[ParentRelative->Length - 1] == L'\\') ParentRelative = ParentRelative->Substring(0, ParentRelative->Length - 2);
        }
        bool cloud_isfolder = clouditem->Size == -1;
        bool convert_to_placeholder(false);
        bool create_placeholder(false);
        bool rename_cloud(false);
        bool file_exist(false);
        //bool create_hardlink(false);

        clouditem->Name = CssCs::Extensions::RenameFileNameUnInvalid(clouditem->Name, clouditem->Size != -1);
        LocalItem^ localitem = parent->Childs->FindFromId(clouditem->Id);
        if (localitem)
        {
            //unlock?
            return localitem;
        }
        //LocalItem^ localitem_hardlinkbase = srvm->Root->FindFromCloudId(clouditem->Id);
        //if (localitem_hardlinkbase) create_hardlink = true;

        String^ fullPathItemParent = srvm->LocalPath;
        String^ relativeItem;
        if (String::IsNullOrEmpty(ParentRelative)) relativeItem = clouditem->Name;
        else
        {
            fullPathItemParent = fullPathItemParent + L"\\" + ParentRelative;
            relativeItem = ParentRelative + L"\\" + clouditem->Name;
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
            //item is not placeholder
            bool localIsFolder = attribs & FILE_ATTRIBUTE_DIRECTORY;
            if (localIsFolder == cloud_isfolder)//same file/folder
            {
                //check two item is hardlink?
                //if (create_hardlink)
                //{
                //    //this is placeholder
                //    String^ basePathItem = localitem_hardlinkbase->GetFullPath()->ToString();
                //    PinStr(basePathItem);
                //    if (TwoItemIsHardLink(pin_basePathItem, pin_fullPathItem))
                //    {
                //        localitem = gcnew LocalItem(srvm, clouditem->Id);
                //        localitem->Name = clouditem->Name;
                //        parent->Childs->Add(localitem);
                //        return localitem;
                //    }
                //    else
                //    {
                //        rename_cloud = true;
                //    }
                //}
                //else//
                //{
                    if (localIsFolder) convert_to_placeholder = true;//same folder
                    else//same file
                    {
                        if (srvm->EnumStatus.HasFlag(SyncRootStatus::CreatingPlaceholder))
                            srvm->Message = String::Format(CultureInfo::InvariantCulture, L"Checking hash file: {0}", clouditem->Name);
                        if (srvm->SyncRootData->Account->AccountViewModel->Cloud->HashCheck(fullPathItem, clouditem))
                        {
                            //same size and hash
                            convert_to_placeholder = true;
                        }
                        else
                        {
                            //diff size / hash
                            rename_cloud = true;
                            create_placeholder = true;
                        }
                    }
                //}
            }
            else//file != folder
            {
                rename_cloud = true;
                create_placeholder = true;
            }
        }

        if (rename_cloud)
        {
            clouditem->Name = FindNewNameItem(srvm, fullPathItemParent, clouditem /*, create_hardlink*/);//if newname not found -> create, if found -> convert
            fullPathItem = fullPathItemParent + L"\\" + clouditem->Name;            
            relativeItem = fullPathItem->Substring(srvm->LocalPath->Length + 1);
            PinStr3(pin_fullPathItem, fullPathItem);
            PinStr3(pin_relativeItem, relativeItem);
            attribs = GetFileAttributes(pin_fullPathItem);
            file_exist = INVALID_FILE_ATTRIBUTES != attribs;//if create_hardlink -> file_exist alway false
            if (file_exist)
            {
                convert_to_placeholder = true;
                create_placeholder = false;
            }
            else create_placeholder = true;
        }

        
        PlacehoderResult result = PlacehoderResult::Failed;
        if (file_exist)
        {
            if (convert_to_placeholder) result = Convert(pin_fullPathItem, clouditem->Id);
        }
        else
        {
            //if (create_hardlink)
            //{
            //    String^ baselink = localitem_hardlinkbase->GetFullPath()->ToString();
            //    PinStr(baselink);
            //    if (CreateHardLink(pin_fullPathItem, pin_baselink, NULL)) result = PlacehoderResult::Success;
            //    else
            //    {
            //        LogWriter::WriteLogError(std::wstring(L"CreateHardLink failed, FileName:").append(pin_fullPathItem)
            //            .append(L", ExistingFileName:").append(pin_baselink), (int)GetLastError());
            //    }
            //}
            //else 
            if (create_placeholder)
            {
                if (Create(pin_LocalPath, pin_relativeItem, clouditem)) result = PlacehoderResult::Success;
            }
        }

        if (PlacehoderResult::Success == result)
        {
            localitem = gcnew LocalItem(srvm, clouditem->Id);
            localitem->Name = clouditem->Name;
            parent->Childs->Add(localitem);
            return localitem;
        }
        else return nullptr;
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

    PlacehoderResult Placeholders::Revert(LPCWSTR fullPath)
    {
        PlacehoderResult result{ PlacehoderResult::Failed };
        if (fullPath)
        {
            HANDLE hfile = CreateFile(fullPath, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
            if (INVALID_HANDLE_VALUE != hfile)
            {
                HRESULT hr = CfRevertPlaceholder(hfile, CF_REVERT_FLAG_NONE, nullptr);
                if (CheckHr(hr, L"Placeholders::Revert CfRevertPlaceholder", fullPath, true)) result = PlacehoderResult::Success;
                else if (HR_FileOpenningByOtherProcess == hr || HR_InUse == hr)  result = PlacehoderResult::Failed | PlacehoderResult::OpenByOtherProcess;
                CloseHandle(hfile);
            }
            else
            {
                LogWriter::WriteLogError(std::wstring(L"Placeholders::Revert CreateFile error:").append(fullPath).c_str(), (int)GetLastError());
                if (PathExists(fullPath)) result = PlacehoderResult::Failed | PlacehoderResult::CanNotOpen;
                else result = PlacehoderResult::Failed | PlacehoderResult::FileNotFound;
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
    PlacehoderResult Placeholders::Update(LPCWSTR fullPath, CloudItem^ clouditem)
    {
        PlacehoderResult result{ PlacehoderResult::Failed };
        if (fullPath && clouditem)
        {
            HANDLE hfile = CreateFile(fullPath, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
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

                if (CheckHr(hr, L"Placeholders::Update CfUpdatePlaceholder", fullPath, true)) result = PlacehoderResult::Success;
                else if (HR_FileOpenningByOtherProcess == hr || HR_InUse == hr) result = PlacehoderResult::Failed | PlacehoderResult::OpenByOtherProcess;
                CloseHandle(hfile);
            }
            else//file can't open
            {
                LogWriter::WriteLogError(std::wstring(L"Placeholders::Update CreateFile error:").append(fullPath).c_str(), (int)GetLastError());
                if (PathExists(fullPath)) result = PlacehoderResult::Failed | PlacehoderResult::CanNotOpen;
                else result = PlacehoderResult::Failed | PlacehoderResult::FileNotFound;
            }
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
    PlacehoderResult Placeholders::Convert(LPCWSTR fullPath, String^ fileIdentity)
    {
        PlacehoderResult result{ PlacehoderResult::Failed };
        if (fullPath && !String::IsNullOrEmpty(fileIdentity))
        {
            HANDLE hfile = CreateFile(fullPath, WRITE_DAC, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
            if (INVALID_HANDLE_VALUE != hfile)
            {
                USN usn{ 0 };
                char FileIdentity[LengthFileIdentity]{ 0 };
                FillFileIdentity(FileIdentity, fileIdentity);
                //doc recommend oplock
                HRESULT hr = CfConvertToPlaceholder(hfile, FileIdentity, LengthFileIdentity, CF_CONVERT_FLAG_MARK_IN_SYNC, &usn, nullptr);
                if (CheckHr(hr, L"Placeholders::Convert CfConvertToPlaceholder", fullPath ,true)) result = PlacehoderResult::Success;
                else if (HR_FileOpenningByOtherProcess == hr || HR_InUse == hr) result = PlacehoderResult::Failed | PlacehoderResult::OpenByOtherProcess;
                CloseHandle(hfile);
            }
            else//file can't open
            {
                LogWriter::WriteLogError(std::wstring(L"Placeholders::Convert CreateFile error:").append(fullPath).c_str(), (int)GetLastError());
                if (PathExists(fullPath)) result = PlacehoderResult::Failed | PlacehoderResult::CanNotOpen;
                else result = PlacehoderResult::Failed | PlacehoderResult::FileNotFound;
            }
        }
        return result;
    }

    PlacehoderResult Placeholders::Hydrate(LPCWSTR fullPath)
    {
        PlacehoderResult result{ PlacehoderResult::Failed };
        if (fullPath)
        {
            DWORD attrib = GetFileAttributes(fullPath);
            if ((attrib != INVALID_FILE_ATTRIBUTES) && 
                !(attrib & FILE_ATTRIBUTE_DIRECTORY) && //skip if folder
                (attrib & FILE_ATTRIBUTE_PINNED))
            {
                HANDLE hfile = CreateFile(fullPath, 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr);
                if (INVALID_HANDLE_VALUE != hfile)
                {
                    LARGE_INTEGER offset = { 0 };
                    LARGE_INTEGER length;
                    length.QuadPart = MAXLONGLONG;
                    HRESULT hr = CfHydratePlaceholder(hfile, offset, length, CF_HYDRATE_FLAG_NONE, NULL);
                    if (CheckHr(hr, L"Placeholders::Hydrate CfHydratePlaceholder", fullPath), true) result = PlacehoderResult::Success;
                    CloseHandle(hfile);
                }
                else
                {
                    LogWriter::WriteLogError(std::wstring(L"Placeholders::Hydrate CreateFile error:").append(fullPath).c_str(), (int)GetLastError());
                }
            }
        }
        return result;
    }

    PlacehoderResult Placeholders::Dehydrate(LPCWSTR fullPath)
    {
        PlacehoderResult result{ PlacehoderResult::Failed };
        if (fullPath)
        {
            DWORD attrib = GetFileAttributes(fullPath);
            if ((attrib != INVALID_FILE_ATTRIBUTES) && 
                !(attrib & FILE_ATTRIBUTE_DIRECTORY) && 
                (attrib & FILE_ATTRIBUTE_UNPINNED))
            {
                HANDLE hfile = CreateFile(fullPath, 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr);
                if (INVALID_HANDLE_VALUE != hfile)
                {
                    LARGE_INTEGER offset = { 0 };
                    LARGE_INTEGER length;
                    length.QuadPart = MAXLONGLONG;
                    HRESULT hr;
                    hr = CfDehydratePlaceholder(hfile, offset, length, CF_DEHYDRATE_FLAG_BACKGROUND, NULL);
                    if (CheckHr(hr, L"Placeholders::Dehydrate CfDehydratePlaceholder", fullPath, true)) result = PlacehoderResult::Success;
                    else if (HR_FileOpenningByOtherProcess == hr || HR_InUse == hr) result = PlacehoderResult::Failed | PlacehoderResult::OpenByOtherProcess;
                    CloseHandle(hfile);                    
                }
                else
                {
                    LogWriter::WriteLogError(std::wstring(L"Placeholders::Dehydrate CreateFile error:").append(fullPath).c_str(), (int)GetLastError());
                }
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
}