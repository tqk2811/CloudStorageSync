#pragma once
namespace CSS
{
	enum class PlacehoderResult : int
	{
		Success = 0,
		Failed = 1 << 0,
		OpenByOtherProcess = 1 << 1,
		CanNotOpen = 1 << 2,
		FileNotFound = 1 << 3,
	};
	inline PlacehoderResult operator|(PlacehoderResult l, PlacehoderResult r)
	{
		return (PlacehoderResult)((int)l | (uint32_t)r);
	}

	class Placeholders
	{
	public:
		static void CreateAll(SyncRootViewModel^ srvm, LocalItem^ parent, CloudItem^ ci_parent, String^ RelativeOfParent);
		static LocalItem^ CreateItem(SyncRootViewModel^ srvm, LocalItem^ parent, String^ ParentRelative, CloudItem^ clouditem);
		static bool Create(LPCWSTR syncRootPath, LPCWSTR relativePathItem, CloudItem^ clouditem);
		static PlacehoderResult Revert(LPCWSTR fullPath);
		static PlacehoderResult Update(LPCWSTR fullPath, CloudItem^ clouditem);
		static PlacehoderResult Convert(LPCWSTR fullPath, String^ fileIdentity);

		static PlacehoderResult Hydrate(LPCWSTR fullPath);
		static PlacehoderResult Dehydrate(LPCWSTR fullPath);

		//static bool SetInSyncState(SyncRootViewModel^ srvm, LocalItem^ li, CF_IN_SYNC_STATE state);
		//static bool GetPlaceholderStandarInfo(LPCWSTR fullPathItem, MY_CF_PLACEHOLDER_STANDARD_INFO* info);

		//only work after connectsyncroot
		static CF_PLACEHOLDER_STATE GetPlaceholderState(LPCWSTR fullPathItem);
	};
}
