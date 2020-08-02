#pragma once
namespace CSS
{
	enum class PlaceholderResult
	{

	};

	class Placeholders
	{
	public:
		static void CreateAll(SyncRootViewModel^ srvm, LocalItem^ li, String^ RelativeOfParent);
		static LocalItem^ CreateItem(SyncRootViewModel^ srvm, LocalItem^ li_parent, String^ parentRelative, CloudItem^ clouditem);
		static bool Create(LPCWSTR syncRootPath, LPCWSTR relativePathItem, CloudItem^ clouditem);
		static bool Revert(SyncRootViewModel^ srvm, LPCWSTR fullPathItem);
		static bool Update(SyncRootViewModel^ srvm, LPCWSTR fullPathItem, CloudItem^ clouditem);
		static bool Convert(SyncRootViewModel^ srvm, LPCWSTR fullPathItem, String^ fileIdentity);

		static bool Hydrate(SyncRootViewModel^ srvm, LPCWSTR fullPathItem);
		static bool Dehydrate(SyncRootViewModel^ srvm, LPCWSTR fullPathItem);

		//static bool SetInSyncState(SyncRootViewModel^ srvm, LocalItem^ li, CF_IN_SYNC_STATE state);
		static bool GetPlaceholderStandarInfo(LPCWSTR fullPathItem, MY_CF_PLACEHOLDER_STANDARD_INFO* info);

		//only work after connectsyncroot
		static CF_PLACEHOLDER_STATE GetPlaceholderState(LPCWSTR fullPathItem);
	};
}
