#pragma once
namespace CssWinrt
{
    enum class SyncRootRegisterStatus
    {
        Register,
        Exist,
        Failed
    };

    //doc https://docs.microsoft.com/en-us/uwp/api/windows.storage.provider.storageprovidersyncrootinfo.hardlinkpolicy?view=winrt-19041
    enum class HardlinkPolicy : uint32_t
    {
        None = 0,
        Allowed = 0x1,
    };

    //doc https://docs.microsoft.com/en-us/uwp/api/windows.storage.provider.storageprovidersyncrootinfo.hydrationpolicy?view=winrt-19041
    enum class HydrationPolicy : int32_t
    {
        Partial = 0,
        Progressive = 1,
        Full = 2,
        AlwaysFull = 3,
    };

    //doc https://docs.microsoft.com/en-us/uwp/api/windows.storage.provider.storageprovidersyncrootinfo.hydrationpolicymodifier?view=winrt-19041
    enum class HydrationPolicyModifier : uint32_t
    {
        None = 0,
        ValidationRequired = 0x1,
        StreamingAllowed = 0x2,
        AutoDehydrationAllowed = 0x4,
    };
    DEFINE_ENUM_FLAG_OPERATORS (HydrationPolicyModifier)
    //inline HydrationPolicyModifier operator|(HydrationPolicyModifier l,HydrationPolicyModifier r)
    //{
    //    return (HydrationPolicyModifier)((uint32_t)l | (uint32_t)r);
    //}

    //doc https://docs.microsoft.com/en-us/uwp/api/windows.storage.provider.storageprovidersyncrootinfo.insyncpolicy?view=winrt-19041
    enum class InSyncPolicy : uint32_t
    {
        Default = 0,
        FileCreationTime = 0x1,
        FileReadOnlyAttribute = 0x2,
        FileHiddenAttribute = 0x4,
        FileSystemAttribute = 0x8,
        DirectoryCreationTime = 0x10,
        DirectoryReadOnlyAttribute = 0x20,
        DirectoryHiddenAttribute = 0x40,
        DirectorySystemAttribute = 0x80,
        FileLastWriteTime = 0x100,
        DirectoryLastWriteTime = 0x200,
        PreserveInsyncForSyncEngine = 0x80000000,
    };
    DEFINE_ENUM_FLAG_OPERATORS(InSyncPolicy)
    //inline InSyncPolicy operator|(InSyncPolicy l, InSyncPolicy r)
    //{
    //    return (InSyncPolicy)((uint32_t)l | (uint32_t)r);
    //}

    //doc https://docs.microsoft.com/en-us/uwp/api/windows.storage.provider.storageprovidersyncrootinfo.populationpolicy?view=winrt-19041
    enum class PopulationPolicy : int32_t
    {
        Full = 1,
        AlwaysFull = 2,
    };

    DLL_EXPORTS struct SyncRootRegistrarInfo
	{
		LPCWSTR SrId				{ nullptr };
		LPCWSTR LocalPath			{ nullptr };
		LPCWSTR DisplayName			{ nullptr };
		LPCWSTR Version				{ L"1.0.0" };
		LPCWSTR RecycleBinUri		{ nullptr };
		int32_t IconIndex			{ 0 };
        bool ShowSiblingsAsGroup    { false };

        HydrationPolicy HydrationPolicy	                { HydrationPolicy::Full };
        HydrationPolicyModifier HydrationPolicyModifier	{ HydrationPolicyModifier::None };
        PopulationPolicy PopulationPolicy			    { PopulationPolicy::AlwaysFull };
        InSyncPolicy InSyncPolicy				        { InSyncPolicy::FileCreationTime | InSyncPolicy::DirectoryCreationTime };		
        HardlinkPolicy HardlinkPolicy					{ HardlinkPolicy::None };
	};
}