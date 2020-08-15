#include "pch.h"
#include "SyncRootViewModel.h"
namespace CSS
{
	void SyncRootViewModel::Register()
    {
        this->Root = gcnew LocalItemRoot(this, this->SyncRootData->CloudFolderId);
        EnumStatus = SyncRootStatus::RegisteringSyncRoot;
        if (String::IsNullOrEmpty(DisplayName)) DisplayName = CloudFolderName + gcnew String(L" - ") + SyncRootData->Account->Email;
        PinStr2(pin_SrId, SyncRootData->Id);
        PinStr(LocalPath);
        PinStr(DisplayName);

        SyncRootRegistrarInfo srinfo;
        srinfo.SrId = pin_SrId;
        srinfo.LocalPath = pin_LocalPath;
        srinfo.DisplayName = pin_DisplayName;
        srinfo.IconIndex = (int)SyncRootData->Account->CloudName;
        srinfo.Version = L"1.0.0";
        srinfo.RecycleBinUri = nullptr;
        srinfo.ShowSiblingsAsGroup = false;
        srinfo.HardlinkPolicy = HardlinkPolicy::Allowed;
        if (SyncRootData->Account->CloudName == CloudName::MegaNz) srinfo.HydrationPolicy = HydrationPolicy::AlwaysFull;
        else srinfo.HydrationPolicy = HydrationPolicy::Full;
        srinfo.HydrationPolicyModifier = HydrationPolicyModifier::AutoDehydrationAllowed | HydrationPolicyModifier::StreamingAllowed;
        srinfo.PopulationPolicy = PopulationPolicy::AlwaysFull;
        srinfo.InSyncPolicy = InSyncPolicy::FileCreationTime | InSyncPolicy::DirectoryCreationTime;

        ConnectionKey = 0;

        switch (CssWinrt::SyncRoot_RegisterWithShell(srinfo))
        {
        case SyncRootRegisterStatus::Register:
        {
            ShellCall::AddFolderToSearchIndexer(pin_LocalPath);
            ConnectionKey = ConnectSyncRoot::ConnectSyncRootTransferCallbacks(pin_LocalPath);

            EnumStatus = SyncRootStatus::CreatingPlaceholder;
            Placeholders::CreateAll(this, this->Root, CloudItem::GetFromId(this->SyncRootData->CloudFolderId, this->SyncRootData->Account->Id), "");
            if (EnumStatus.HasFlag(SyncRootStatus::Error)) return;
            else Message = String::Empty;
        }
        case SyncRootRegisterStatus::Exist:
        {
            if (!ConnectionKey) ConnectionKey = ConnectSyncRoot::ConnectSyncRootTransferCallbacks(pin_LocalPath);

            watcher->Change(LocalPath,
                gcnew CssCs::CustomFileSystemEventHandler(this, &CSS::SyncRootViewModel::LocalOnChanged));
            watcher->Start();
            EnumStatus = SyncRootStatus::Working;

            Task::Factory->StartNew(gcnew Action(this, &SyncRootViewModel::FindNonPlaceholderAndUpload));
            LogWriter::WriteLog(std::wstring(L"Syncroot Register: Success SrId:").append(pin_SrId), 2);
            break;
        }
        case SyncRootRegisterStatus::Failed://Syncroot B can't inside folder in syncroot A, and some...
        {
            EnumStatus = SyncRootStatus::RegisteringSyncRoot | SyncRootStatus::Error;
            Message = gcnew String(L"Registering SyncRoot Failed");
            break;
        }
        }
        GC::Collect();
    }

    void SyncRootViewModel::UnRegister()
    {
        PinStr2(pin_SrId, SyncRootData->Id);
        watcher->Stop();
        ConnectSyncRoot::DisconnectSyncRootTransferCallbacks(ConnectionKey);
        ConnectionKey = 0;
        SyncRoot_UnRegister(pin_SrId);
        IsListed = false;
        if(this->Root) Root->Remove();
        EnumStatus = SyncRootStatus::NotWorking;
        LogWriter::WriteLog(std::wstring(L"Syncroot UnRegister: Success SrId:").append(pin_SrId), 2);
    }

    void SyncRootViewModel::LocalOnChanged(CustomFileSystemEventArgs^ e)
    {
        //PinStr2(pin_fullpath, e->FullPath);
        //String^ log = String::Format(CultureInfo::InvariantCulture, "SyncRootViewModel::LocalOnChanged ChangeType: {0}, ChangeInfo:{1}, FullPath: {2}", e->ChangeType, e->ChangeInfo, e->FullPath);
        //WriteLog(log, 1);
        //DWORD attrib = GetFileAttributes(pin_fullpath);
        //switch (e->ChangeType)
        //{
        //case WatcherChangeTypes::Changed://attribute,file change,...
        //{
        //    LocalItem^ li = Root->FindFromFullPath(e->FullPath);
        //    switch (e->ChangeInfo)
        //    {
        //    case ChangeInfo::Attribute:
        //    {
        //        if (!(attrib & FILE_ATTRIBUTE_DIRECTORY))//file only
        //        {
        //            CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(pin_fullpath);
        //            if (state == CF_PLACEHOLDER_STATE_INVALID)
        //                return;
        //            if (li && (state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)//placeholder
        //            {
        //                if (attrib & FILE_ATTRIBUTE_PINNED) Placeholders::Hydrate(srvm, li, false);
        //                else if (attrib & FILE_ATTRIBUTE_UNPINNED) Placeholders::Dehydrate(srvm, li, false);
        //            }
        //        }
        //        break;
        //    }
        //    case ChangeInfo::LastWrite:
        //    {
        //        if (!(attrib & FILE_ATTRIBUTE_DIRECTORY))//file only
        //        {
        //            LocalItem^ li = LocalItem::FindFromPath(srvm, e->FullPath, 0);
        //            if (li)//file revert li was del
        //            {
        //                UploadQueue^ uq = gcnew UploadQueue(srvm, li);
        //                TaskQueues::UploadQueues->Reset(uq);
        //            }
        //        }
        //        break;
        //    }
        //    default:
        //        break;
        //    }
        //    break;
        //}
        //case WatcherChangeTypes::Created://new file created
        //{
        //    //if item is placeholder -> skip
        //    //if item is not placeholder -> upload
        //    //if can't open for check item -> Queue (for try again later)
        //    CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(pin_fullpath);
        //    if (state == CF_PLACEHOLDER_STATE_INVALID)
        //        return;
        //    if (!(state & CF_PLACEHOLDER_STATE_PLACEHOLDER))//if new item is not placeholder
        //    {
        //        FileInfo^ finfo = gcnew FileInfo(e->FullPath);
        //        String^ parent_fullpath = e->FullPath->Substring(0, e->FullPath->Length - finfo->Name->Length - 1);
        //        LocalItem^ li_parent = LocalItem::FindFromPath(srvm, parent_fullpath, 0);
        //        if (!String::IsNullOrEmpty(li_parent->CloudId))
        //        {
        //            CloudItem^ ci_parent = CloudItem::Select(li_parent->CloudId, srvm->CEVM->EmailSqlId);
        //            if (!ci_parent->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanAddChildren)) return;//can't upload child
        //        }
        //        //upload
        //        LocalItem^ li_new = LocalItem::Find(srvm, li_parent->LocalId, finfo->Name);
        //        if (!li_new)
        //        {
        //            li_new = gcnew LocalItem();
        //            li_new->LocalParentId = li_parent->LocalId;
        //            li_new->Name = finfo->Name;
        //            li_new->SRId = srvm->SRId;
        //            if (finfo->Attributes.HasFlag(FileAttributes::Directory)) li_new->Flag = LocalItemFlag::Folder;
        //            li_new->Insert();
        //        }
        //        UploadQueue^ uq = gcnew UploadQueue(srvm, li_new);
        //        if (finfo->Attributes.HasFlag(FileAttributes::Directory)) uq->IsPrioritize = true;
        //        TaskQueues::UploadQueues->Add(uq);
        //    }
        //    break;
        //}
        //case WatcherChangeTypes::Deleted:
        //{
        //    //if item is not placeholder delete localitem
        //    //if item is placeholder -> skip (ConnectSyncRoot NOTIFY_DELETE/NOTIFY_DELETE_COMPLETION trigger)
        //    LocalItem^ li = LocalItem::FindFromPath(srvm, e->FullPath, 0);
        //    if (li && String::IsNullOrEmpty(li->CloudId))
        //    {
        //        //request cancel upload
        //        TaskQueues::UploadQueues->Cancel(gcnew UploadQueue(srvm, li));
        //        li->Delete(true);
        //    }
        //    break;
        //}
        //default: break;
        //}
    }

    void SyncRootViewModel::FindNonPlaceholderAndUpload(LPCWSTR FullPath)
    {
        //WIN32_FIND_DATA find{ 0 };
    //std::wstring sfullpath(FullPath);
    //sfullpath.append(L"\\*");
    //HANDLE handle = FindFirstFileEx(sfullpath.data(), FindExInfoStandard, &find, FindExSearchNameMatch, NULL, FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY);
    //if (handle != INVALID_HANDLE_VALUE)
    //{
    //	do
    //	{
    //		if (!wcscmp(find.cFileName, L".") || !wcscmp(find.cFileName, L"..")) continue;
    //		std::wstring itempath(FullPath);
    //		itempath.append(L"\\").append(find.cFileName);
    //		bool isfolder = find.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY;
    //		CF_PLACEHOLDER_STATE state = CfGetPlaceholderStateFromFindData(&find);
    //		if (!(state & CF_PLACEHOLDER_STATE_IN_SYNC))
    //		{
    //			String^ itempath_ = gcnew String(itempath.c_str());
    //			LocalItem^ li = LocalItem::FindFromPath(srvm, itempath_, 0);
    //			LocalItem^ parent_li = LocalItem::FindFromPath(srvm, itempath_, 1);
    //			if (!li)
    //			{
    //				//new upload                        
    //				li = gcnew LocalItem();
    //				if (isfolder) li->Flag = LocalItemFlag::Folder;
    //				li->LocalParentId = parent_li->LocalId;
    //				li->Name = gcnew String(find.cFileName);
    //				li->SRId = srvm->SRId;
    //				li->Insert();
    //			}
    //			UploadQueue^ uq = gcnew UploadQueue(srvm, li);
    //			if (isfolder) uq->IsPrioritize = true;
    //			TaskQueues::UploadQueues->Add(uq);
    //		}
    //		if (isfolder) FindNonPlaceholderAndUpload(srvm, itempath.c_str());
    //	} while (FindNextFile(handle, &find));
    //	FindClose(handle);
    //}
    //else
    //{
    //	//error
    //}
    }

    //revert placeholder and move to recycle bin
    bool DeleteItem(SyncRootViewModel^ srvm, CloudItem^ ci_old)
    {
        LocalItem^ li_delete = srvm->Root->FindFromCloudId(ci_old->Id);
        if (li_delete)
        {
            String^ fullPathItem = li_delete->GetFullPath()->ToString();
            PinStr(fullPathItem);
            PlacehoderResult result = Placeholders::Revert(pin_fullPathItem);
            if (result.HasFlag(PlacehoderResult::FileNotFound)) ;
            else if (result.HasFlag(PlacehoderResult::OpenByOtherProcess) || result.HasFlag(PlacehoderResult::CanNotOpen))
            {
                //try again later
                return false;
            }
            else//Success/Failed only -> move to recycle bin
            {
                if (!MoveToRecycleBin(std::wstring(pin_fullPathItem)))
                {
                    //try again later
                    return false;
                }
            }
            li_delete->Parent->Childs->Remove(li_delete);
        }
        return true;
    }

    //create new placeholder and child
    void CreateItem(SyncRootViewModel^ srvm, CloudItem^ ci_new)
    {
        LocalItem^ li_parent = srvm->Root->FindFromCloudId(ci_new->ParentId);
        if (li_parent)
        {
            LocalItem^ item_created = Placeholders::CreateItem(srvm, li_parent, li_parent->GetRelativePath()->ToString(), ci_new);
            if (ci_new->Size == -1)//folder
            {
                srvm->SyncRootData->Account->AccountViewModel->Cloud->ListAllItemsToDb(srvm->SyncRootData, ci_new->Id)->Wait();
                Placeholders::CreateAll(srvm, item_created, ci_new, item_created->GetRelativePath()->ToString());
            }
        }
    }

    //for rename & change parent   (need old & new because maybe change id)
    //need update placeholder after this
    bool MoveItem(SyncRootViewModel^ srvm, CloudItem^ ci_old, CloudItem^ ci_new)
    {
        String^ Name = CssCs::Extensions::RenameFileNameUnInvalid(ci_new->Name, ci_new->Size != -1);//new name
        LocalItem^ li = srvm->Root->FindFromCloudId(ci_old->Id);
        LocalItem^ li_newparent = srvm->Root->FindFromCloudId(ci_new->Id);

        String^ newparentpath = li->Parent->GetFullPath()->ToString();//old parent
        String^ oldpath = li->GetFullPath()->ToString();
        String^ newpath = newparentpath + L"\\" + Name;
        if (PathExists(newpath))
        {
            Name = FindNewNameItem(srvm, newparentpath, ci_new);
            newpath = newparentpath + L"\\" + Name;
        }
        PinStr(oldpath);
        PinStr(newpath);
        if (MoveFile(pin_oldpath, pin_newpath))
        {
            li->Name = Name;//rename
            if (!li->Parent->Equals(li_newparent))//if move -> move 
            {
                li->Parent->Childs->Remove(li);
                li_newparent->Childs->Add(li);
            }            
            return true;
        }
        else
        {
            int err = (int)GetLastError();
            LogWriter::WriteLogError(std::wstring(L"RenameItem failed, oldpath:").append(pin_oldpath)
                .append(L", newpath:").append(pin_newpath), err);
        }
        return false;
    }

    //for change size, time, id,
    bool ChangeItem(SyncRootViewModel^ srvm, CloudItem^ ci_old, CloudItem^ ci_new)
    {
        LocalItem^ li = srvm->Root->FindFromCloudId(ci_old->Id);
        String^ fullpath = li->GetFullPath()->ToString();
        PinStr(fullpath);
        PlacehoderResult result = Placeholders::Update(pin_fullpath, ci_new);
        if (result.HasFlag(PlacehoderResult::Failed))
        {
            if (result.HasFlag(PlacehoderResult::FileNotFound))
            {
                li->Parent->Childs->Remove(li);
                LogWriter::WriteLog(std::wstring(L"Placeholders::Update FileNotFound"), 0);
                return true;
            }
        }
        else
        {
            li->CloudId = ci_new->Id;
            return true;
        }
        return false;
    }

    bool SyncRootViewModel::UpdateChange(ICloudItemAction^ change)
    {
        bool flag = false;
        //find with localitem






        return flag;
    }
}