#pragma once
namespace CssWinrt
{
    class SyncRootRegistrar
    {
    public:
        static SyncRootRegisterStatus RegisterWithShell(const SyncRootRegistrarInfo& registerarInfo);
        static bool Unregister(LPCWSTR CFid);
        static void UnregisterAll();
    private:
        static bool CheckSyncRootExist(std::wstring& syncRootID);
        static std::wstring GetSyncRootId(LPCWSTR CFid);
        static std::unique_ptr<TOKEN_USER> GetTokenInformation();
        static void AddCustomState(
            _In_ winrt::IVector<winrt::StorageProviderItemPropertyDefinition>& customStates,
            _In_ LPCWSTR displayNameResource,
            _In_ int id);
    };
}
