#include "pch.h"
#include "LocalAction.h"
namespace CSS
{
	void LocalAction::InsertLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIdsNew, CloudItem^ ci)
	{
		//IList<LocalItem^>^ li_parents = LocalItem::FindAll(srvm, ParentIdsNew);//parents of item will create
		//if (li_parents->Count > 0 && ci->Size == -1) srvm->CEVM->Cloud->ListAllItemsToDb(srvm, ci->Id);//list child of new item to db
		//for (int i = 0; i < li_parents->Count; i++)
		//{
		//	LocalItem^ li = Placeholders::CreateItem(srvm, li_parents[i]->LocalId, li_parents[i]->GetRelativePath()->ToString(), ci);
		//	if (ci->Size == -1 && li) Placeholders::CreateAll(srvm, li->CloudId, li->LocalId, li->GetRelativePath()->ToString());//create childs of newitem
		//}
	}
	void LocalAction::DeleteLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIdsRemove, String^ CloudId)
	{
		//IList<LocalItem^>^ li_parents = LocalItem::FindAll(srvm, ParentIdsRemove);
		//for (int i = 0; i < li_parents->Count; i++) DeleteLocal(srvm, LocalItem::Find(srvm, CloudId, li_parents[i]->LocalId));
	}
	bool LocalAction::DeleteLocal(SyncRootViewModel^ srvm, LocalItem^ li_delete)
	{
		//if (li_delete)
		//{
		//	//note: Revert a	file PINNED (download full on disk) -> move to RecycleBin
		//	//					file UNPINNED -> will delete when move to RecycleBin
		//	//folder scan file and revert inside, and delete folder
		//	if (li_delete->Flag.HasFlag(LocalItemFlag::Folder)) return Delete_RevertFolder(srvm, li_delete);//revert all file inside and this folder
		//	else return RevertFilePlaceholdersAndMoveRecyleBin(srvm, li_delete, true);
		//}
		//else return false;
		return false;
	}
	bool LocalAction::Delete_RevertFolder(SyncRootViewModel^ srvm, LocalItem^ li_folder_delete)
	{
		//IList<LocalItem^>^ lis = LocalItem::FindAll(srvm, li_folder_delete->LocalId);
		//bool result{ false };
		//for (int i = 0; i < lis->Count; i++)
		//{
		//	if (lis[i]->Flag.HasFlag(LocalItemFlag::Folder)) result = Delete_RevertFolder(srvm, lis[i]);//revert child in folder
		//	else result = RevertFilePlaceholdersAndMoveRecyleBin(srvm, lis[i], true);//file
		//	if (!result) return result;
		//}
		//return RevertFilePlaceholdersAndMoveRecyleBin(srvm, li_folder_delete, true);//revert this folder
		return false;
	}

	bool LocalAction::RevertFilePlaceholdersAndMoveRecyleBin(SyncRootViewModel^ srvm, LocalItem^ li, bool TryAgain)
	{
		//if (li)
		//{
		//	Placeholders::Revert(srvm, li);
		//	String^ fullPathItem = li->GetFullPath();
		//	PinStr(fullPathItem);
		//	if (PathExists(pin_fullPathItem))
		//	{
		//		if (!MoveToRecycleBin(std::wstring(pin_fullPathItem)))
		//		{
		//			LogWriter::WriteLog(std::wstring(L"LocalAction::RevertPlaceholdersAndMoveRecyleBin MoveToRecycleBin Failed Path:").append(pin_fullPathItem), 0);
		//			if (TryAgain) LocalError::Insert(li->LocalId, srvm->SRId, LocalErrorType::Revert, String::Empty);
		//			return false;
		//		}
		//	}
		//	LogWriter::WriteLog(std::wstring(L"LocalAction::RevertPlaceholdersAndMoveRecyleBin Success Path:").append(pin_fullPathItem), 0);
		//	li->Delete(true);
		//	return true;
		//}
		//else
		//{
		//	LogWriter::WriteLog(L"LocalAction::RevertPlaceholdersAndMoveRecyleBin LocalItem is null", 0);
		//	return false;
		//}
		return false;
	}


	void LocalAction::UpdateLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIds, CloudItem^ ci)
	{
		//IList<LocalItem^>^ li_parents = LocalItem::FindAll(srvm, ParentIds);
		//for (int i = 0; i < li_parents->Count; i++)
		//{
		//	LocalItem^ li = LocalItem::Find(srvm, ci->Id, li_parents[i]->LocalId);
		//	Placeholders::Update(srvm, li, ci);
		//}
	}
	bool LocalAction::RenameLocal(SyncRootViewModel^ srvm, IList<LocalItem^>^ lis, CloudItem^ ci)
	{
		bool result{ true };
		//ci->Name = CssCs::Extensions::RenameFileNameUnInvalid(ci->Name, ci->Size != -1);
		//for (int i = 0; i < lis->Count; i++)
		//{
		//	if (!RenameLocal(srvm, lis[i], ci, true)) result = false;
		//}
		return result;
	}
	bool LocalAction::RenameLocal(SyncRootViewModel^ srvm, LocalItem^ li, CloudItem^ ci, bool TryAgain)
	{
		//if (li->LocalId == 0 || li->Name->Equals(ci->Name,StringComparison::OrdinalIgnoreCase)) return true;
		//LocalItem^ parent_li = LocalItem::Find(li->LocalParentId);
		//String^ parentFullPath = parent_li->GetFullPath();
		//String^ itemFullPath = parentFullPath + L"\\" + li->Name;
		//String^ itemnewFullPath = parentFullPath + L"\\" + ci->Name;
		//li->Name = ci->Name;
		//PinStr(itemFullPath);
		//PinStr(itemnewFullPath);
		//DWORD file_attri = GetFileAttributes(pin_itemnewFullPath);
		//if (file_attri != INVALID_FILE_ATTRIBUTES)
		//{
		//	li->Name = FindNewNameItem(srvm, parentFullPath, ci);
		//	itemnewFullPath = parentFullPath + L"\\" + li->Name;
		//	PinStr3(pin_itemnewFullPath, itemnewFullPath);
		//}
		//if (MoveFile(pin_itemFullPath, pin_itemnewFullPath))//not trigger CF_CALLBACK_TYPE_NOTIFY_RENAME
		//{
		//	WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::LocalAction::RenameLocal from {0} to {1}", itemFullPath, itemnewFullPath), 2);
		//	li->Update();
		//	return true;
		//}
		//else
		//{
		//	LogWriter::WriteLogError(std::wstring(L"CSS::LocalAction::RenameLocal MoveFile failed:").append(L",Path:").append(pin_itemFullPath), (int)GetLastError());
		//	if (TryAgain) LocalError::Insert(li->LocalId, srvm->SRId, LocalErrorType::Rename, ci->Id);
		//	return false;
		//}
		return false;
	}

	//void LocalAction::TryAgain(LocalError^ le)
	//{
	//	if (!le) return;
	//	bool flag{ false };
	//	SyncRootViewModel^ srvm = SyncRootViewModel::Find(le->SrId);
	//	LocalItem^ li = LocalItem::Find(le->LiId);
	//	if (!srvm || !li) flag = true;//srvm/li not found -> clear
	//	else
	//	{
	//		String^ fullpath = li->GetFullPath();
	//		PinStr(fullpath);
	//		if (!PathExists(pin_fullpath))
	//		{
	//			flag = true;//file does not exist
	//			li->Delete(true);
	//		}
	//		else switch (le->Type)
	//			{
	//			case LocalErrorType::Revert:
	//			{
	//				flag = RevertFilePlaceholdersAndMoveRecyleBin(srvm, li, false);
	//				break;
	//			}
	//			case LocalErrorType::Update:
	//			{
	//				CloudItem^ ci = CloudItem::Select(le->CIId, srvm->CEVM->EmailSqlId);
	//				if (!ci) flag = true;
	//				else flag = Placeholders::Update(srvm, li, ci, false);
	//				break;
	//			}
	//			case LocalErrorType::Convert:
	//			{
	//				flag = Placeholders::Convert(srvm, li, le->CIId, false);
	//				break;
	//			}
	//			case LocalErrorType::Rename:
	//			{
	//				CloudItem^ ci = CloudItem::Select(le->CIId, srvm->CEVM->EmailSqlId);
	//				if (!ci) flag = true;
	//				else flag = LocalAction::RenameLocal(srvm, li, ci, false);
	//				break;
	//			}
	//			default: break;
	//			}
	//	}
	//	if (flag) le->Delete();
	//}
}