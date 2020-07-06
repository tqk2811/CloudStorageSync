#pragma once
namespace CSS
{
	class Placeholders
	{
	public:
		static void CreateAll(SyncRootViewModel^ srvm);
		static void CreateAll(SyncRootViewModel^ srvm, String^ CI_ParentId, LONGLONG LI_ParentId, String^ RelativeOfParent);
		//return fullpath item
		static LocalItem^ CreateItem(SyncRootViewModel^ srvm, LONGLONG LI_ParentId, String^ Relative, CloudItem^ clouditem);
		static bool Create(LPCWSTR syncRootPath, LPCWSTR relativePathItem, CloudItem^ clouditem);
		static bool Revert(SyncRootViewModel^ srvm, LocalItem^ li);
		static bool Update(SyncRootViewModel^ srvm, LocalItem^ li, CloudItem^ clouditem, bool InsertErrorDb = true);
		static bool Convert(SyncRootViewModel^ srvm, LocalItem^ li, String^ fileIdentity, bool InsertErrorDb = true);

		static bool Hydrate(SyncRootViewModel^ srvm, LocalItem^ li, bool InsertErrorDb = true);
		static bool Dehydrate(SyncRootViewModel^ srvm, LocalItem^ li, bool InsertErrorDb = true);

		//static bool SetInSyncState(SyncRootViewModel^ srvm, LocalItem^ li, CF_IN_SYNC_STATE state);
		//static bool GetPlaceholderStandarInfo(LPCWSTR fullPathItem, MY_CF_PLACEHOLDER_STANDARD_INFO* info);

		//only work after connectsyncroot
		static CF_PLACEHOLDER_STATE GetPlaceholderState(LPCWSTR fullPathItem);
		static CF_PLACEHOLDER_STATE GetPlaceholderState(HANDLE hfile);
	};
}
