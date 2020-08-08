#pragma once
namespace CSS
{
	ref class LocalAction
	{
		static bool Delete_RevertFolder(SyncRootViewModel^ srvm, LocalItem^ li_folder_delete);
		static bool RevertFilePlaceholdersAndMoveRecyleBin(SyncRootViewModel^ srvm, LocalItem^ li, bool TryAgain);
	public:
		static void InsertLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentsNew, CloudItem^ ci);
		static void DeleteLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentsRemove, String^ ci);
		static bool DeleteLocal(SyncRootViewModel^ srvm, LocalItem^ li_delete);
		static void UpdateLocal(SyncRootViewModel^ srvm, IList<String^>^ ParentIds, CloudItem^ ci);
		static bool RenameLocal(SyncRootViewModel^ srvm, IList<LocalItem^>^ lis, CloudItem^ ci);
		static bool RenameLocal(SyncRootViewModel^ srvm, LocalItem^ li, CloudItem^ ci, bool TryAgain);

		//static void TryAgain(LocalError^ le);
	};
}

