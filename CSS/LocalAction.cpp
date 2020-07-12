#include "pch.h"
#include "LocalAction.h"
namespace CSS
{
	void LocalAction::InsertLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIdsNew, CloudItem^ ci)
	{
		IList<LocalItem^>^ li_parents = LocalItem::FindAll(srvm, ParentIdsNew);//parents of item will create
		if (li_parents->Count > 0 && ci->Size == -1) srvm->CEVM->Cloud->ListAllItemsToDb(srvm, ci->Id);//list child of new item to db
		for (int i = 0; i < li_parents->Count; i++)
		{
			LocalItem^ li = Placeholders::CreateItem(srvm, li_parents[i]->LocalId, li_parents[i]->GetRelativePath()->ToString(), ci);
			if (ci->Size == -1 && li) Placeholders::CreateAll(srvm, li->CloudId, li->LocalId, li->GetRelativePath()->ToString());//create childs of newitem
		}
	}
	void LocalAction::DeleteLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIdsRemove, String^ CloudId)
	{
		IList<LocalItem^>^ li_parents = LocalItem::FindAll(srvm, ParentIdsRemove);
		for (int i = 0; i < li_parents->Count; i++) DeleteLocal(srvm, LocalItem::Find(srvm, CloudId, li_parents[i]->LocalId));
	}
	bool LocalAction::DeleteLocal(SyncRootViewModel^ srvm, LocalItem^ li_delete)
	{
		if (li_delete)
		{
			//note: Revert a	file PINNED (download full on disk) -> move to RecycleBin
			//					file UNPINNED -> will delete when move to RecycleBin
			//folder scan file and revert inside, and delete folder
			if (li_delete->Flag.HasFlag(LocalItemFlag::Folder)) return Delete_RevertFolder(srvm, li_delete);//revert all file inside and this folder
			else return RevertFilePlaceholdersAndMoveRecyleBin(srvm, li_delete,true);
		}
	}
	bool LocalAction::Delete_RevertFolder(SyncRootViewModel^ srvm, LocalItem^ li_folder_delete)
	{
		IList<LocalItem^>^ lis = LocalItem::FindAll(srvm, li_folder_delete->LocalId);
		bool result{ false };
		for (int i = 0; i < lis->Count; i++)
		{
			if (lis[i]->Flag.HasFlag(LocalItemFlag::Folder)) result = Delete_RevertFolder(srvm, lis[i]);//revert child in folder
			else result = RevertFilePlaceholdersAndMoveRecyleBin(srvm, lis[i], true);//file
			if (!result) return result;
		}
		return RevertFilePlaceholdersAndMoveRecyleBin(srvm, li_folder_delete, true);//revert this folder
	}

	bool LocalAction::RevertFilePlaceholdersAndMoveRecyleBin(SyncRootViewModel^ srvm, LocalItem^ li, bool TryAgain)
	{
		if (li)
		{
			Placeholders::Revert(srvm, li);

			String^ fullPathItem = li->GetFullPath();
			PinStr(fullPathItem);

			if (PathFileExists(pin_fullPathItem))
			{
				if (!MoveToRecycleBin(std::wstring(pin_fullPathItem)))
				{
					LogWriter::WriteLog(std::wstring(L"LocalAction::RevertPlaceholdersAndMoveRecyleBin MoveToRecycleBin Failed Path:").append(pin_fullPathItem), 0);
					if (TryAgain) LocalError::Insert(li->LocalId, srvm->SRId, LocalErrorType::Revert, String::Empty);
					return false;
				}
			}

			LogWriter::WriteLog(std::wstring(L"LocalAction::RevertPlaceholdersAndMoveRecyleBin Success Path:").append(pin_fullPathItem), 0);
			li->Delete(true);
			return true;
		}
		else
		{
			LogWriter::WriteLog(L"LocalAction::RevertPlaceholdersAndMoveRecyleBin LocalItem is null", 0);
			return false;
		}
	}


	void LocalAction::UpdateLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIds, CloudItem^ ci)
	{
		IList<LocalItem^>^ li_parents = LocalItem::FindAll(srvm, ParentIds);
		for (int i = 0; i < li_parents->Count; i++)
		{
			LocalItem^ li = LocalItem::Find(srvm, ci->Id, li_parents[i]->LocalId);
			Placeholders::Update(srvm, li, ci);
		}
	}
	bool LocalAction::RenameLocal(SyncRootViewModel^ srvm, IList<LocalItem^>^ lis, CloudItem^ ci)
	{
		bool result{ true };
		ci->Name = CssCs::Extensions::RenameFileNameUnInvalid(ci->Name, ci->Size != -1);
		for (int i = 0; i < lis->Count; i++)
		{
			if (!RenameLocal(srvm, lis[i], ci, true)) result = false;
		}
		return result;
	}
	bool LocalAction::RenameLocal(SyncRootViewModel^ srvm, LocalItem^ li, CloudItem^ ci, bool TryAgain)
	{
		if (li->LocalId == 0 || li->Name->Equals(ci->Name)) return true;
		LocalItem^ parent_li = LocalItem::Find(li->LocalParentId);
		String^ parentFullPath = parent_li->GetFullPath();
		String^ itemFullPath = parentFullPath + L"\\" + li->Name;
		String^ itemnewFullPath = parentFullPath + L"\\" + ci->Name;
		li->Name = ci->Name;
		PinStr(itemFullPath);
		PinStr(itemnewFullPath);

		DWORD file_attri = GetFileAttributes(pin_itemnewFullPath);
		if (file_attri != INVALID_FILE_ATTRIBUTES)
		{
			li->Name = FindNewNameItem(parentFullPath, ci->Name, ci->Size == -1);
			itemnewFullPath = parentFullPath + L"\\" + li->Name;
			PinStr3(pin_itemnewFullPath, itemnewFullPath);
		}
		if (MoveFile(pin_itemFullPath, pin_itemnewFullPath))//not trigger CF_CALLBACK_TYPE_NOTIFY_RENAME
		{
			WriteLog(String::Format("CSS::LocalAction::RenameLocal from {0} to {1}", itemFullPath, itemnewFullPath), 2);
			li->Update();
			return true;
		}
		else
		{
			LogWriter::WriteLogError(std::wstring(L"CSS::LocalAction::RenameLocal MoveFile failed:").append(L",Path:").append(pin_itemFullPath), (int)GetLastError());
			if (TryAgain) LocalError::Insert(li->LocalId, srvm->SRId, LocalErrorType::Rename, ci->Id);
			return false;
		}
	}



	void LocalAction::FindNonPlaceholderAndUploadTask(Object^ obj)
	{
		SyncRootViewModel^ srvm = (SyncRootViewModel^)obj;
		PinStr2(rootpath, srvm->LocalPath);
		FindNonPlaceholderAndUpload(srvm, rootpath);
	}

	void LocalAction::FindNonPlaceholderAndUpload(SyncRootViewModel^ srvm, LPCWSTR FullPath)
	{
		WIN32_FIND_DATA find{ 0 };
		std::wstring sfullpath(FullPath);
		sfullpath.append(L"\\*");
		HANDLE handle = FindFirstFileEx(sfullpath.data(), FindExInfoStandard, &find, FindExSearchNameMatch, NULL, FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY);
		if (handle != INVALID_HANDLE_VALUE)
		{
			do
			{
				if (!wcscmp(find.cFileName, L".") || !wcscmp(find.cFileName, L"..")) continue;

				std::wstring itempath(FullPath);
				itempath.append(L"\\").append(find.cFileName);
				bool isfolder = find.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY;
				CF_PLACEHOLDER_STATE state = CfGetPlaceholderStateFromFindData(&find);
				if (!(state & CF_PLACEHOLDER_STATE_IN_SYNC))
				{
					String^ itempath_ = gcnew String(itempath.c_str());
					LocalItem^ li = LocalItem::FindFromPath(srvm, itempath_, 0);
					LocalItem^ parent_li = LocalItem::FindFromPath(srvm, itempath_, 1);
					if (!li)
					{
						//new upload                        
						li = gcnew LocalItem();
						if (isfolder) li->Flag = LocalItemFlag::Folder;
						li->LocalParentId = parent_li->LocalId;
						li->Name = gcnew String(find.cFileName);
						li->SRId = srvm->SRId;
						li->Insert();
					}
					UploadQueue^ uq = gcnew UploadQueue(srvm, li);
					if (isfolder) uq->IsPrioritize = true;
					TaskQueues::Add(uq);
				}
				if (isfolder) FindNonPlaceholderAndUpload(srvm, itempath.c_str());
			} while (FindNextFile(handle, &find));
			FindClose(handle);
		}
		else
		{
			//error
		}
	}

	void LocalAction::TryAgain(LocalError^ le)
	{
		if (!le) return;
		bool flag{ false };
		SyncRootViewModel^ srvm = SyncRootViewModel::Find(le->SRId);
		LocalItem^ li = LocalItem::Find(le->LI_Id);
		if (!srvm || !li) flag = true;//srvm/li not found -> clear
		else
		{
			String^ fullpath = li->GetFullPath();
			PinStr(fullpath);
			if (!PathFileExists(pin_fullpath))
			{
				flag = true;//file does not exist
				li->Delete(true);
			}
			else switch (le->Type)
				{
				case LocalErrorType::Revert:
				{
					flag = RevertFilePlaceholdersAndMoveRecyleBin(srvm, li, false);
					break;
				}
				case LocalErrorType::Update:
				{
					CloudItem^ ci = CloudItem::Select(le->CIId, srvm->CEVM->Sqlid);
					if (!ci) flag = true;
					else flag = Placeholders::Update(srvm, li, ci, false);
					break;
				}
				case LocalErrorType::Convert:
				{
					flag = Placeholders::Convert(srvm, li, le->CIId, false);
					break;
				}
				case LocalErrorType::Rename:
				{
					CloudItem^ ci = CloudItem::Select(le->CIId, srvm->CEVM->Sqlid);
					if (!ci) flag = true;
					else flag = LocalAction::RenameLocal(srvm, li, ci, false);
					break;
				}
				default: break;
				}
		}
		if (flag) le->Delete();
	}
}