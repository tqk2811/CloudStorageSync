#include "pch.h"
#include "SyncRootRegistrar.h"
namespace CssWinrt
{
#define STORAGE_PROVIDER_ID L"CSS"
    SyncRootRegisterStatus SyncRootRegistrar::RegisterWithShell(const SyncRootRegistrarInfo& registerarInfo)//LocalFolder,
    {
        if(!registerarInfo.LocalPath || !registerarInfo.DisplayName || !registerarInfo.SrId)
            return SyncRootRegisterStatus::Failed;

        try
        {
            auto syncRootID = GetSyncRootId(registerarInfo.SrId);// {STORAGE_PROVIDER_ID}!{sidString}!cfid            
            if (!CheckSyncRootExist(syncRootID))
            {
                winrt::StorageProviderSyncRootInfo info;
                info.Id(syncRootID);

                auto folder = winrt::StorageFolder::GetFolderFromPathAsync(registerarInfo.LocalPath).get();
                info.Path(folder);

                info.DisplayNameResource(registerarInfo.DisplayName);
                std::wstring icon_p = GetExecutablePath();
                icon_p.append(L",-").append(std::to_wstring(registerarInfo.IconIndex + 500));
                info.IconResource(icon_p.c_str());//L"%SystemRoot%\\system32\\charmap.exe,0"
                info.HydrationPolicy((winrt::StorageProviderHydrationPolicy)registerarInfo.HydrationPolicy);
                info.HydrationPolicyModifier((winrt::StorageProviderHydrationPolicyModifier)registerarInfo.HydrationPolicyModifier);
                info.PopulationPolicy((winrt::StorageProviderPopulationPolicy)registerarInfo.PopulationPolicy);
                info.InSyncPolicy((winrt::StorageProviderInSyncPolicy)registerarInfo.InSyncPolicy);
                if(registerarInfo.Version) info.Version(registerarInfo.Version);
                info.ShowSiblingsAsGroup(registerarInfo.ShowSiblingsAsGroup);
                info.HardlinkPolicy((winrt::StorageProviderHardlinkPolicy)registerarInfo.HardlinkPolicy);
                //info.ProtectionMode()
                if (registerarInfo.RecycleBinUri)
                {
                    winrt::Uri uri(registerarInfo.RecycleBinUri);
                    info.RecycleBinUri(uri);
                }
                // Context
                std::wstring syncRootIdentity(registerarInfo.SrId);
                winrt::IBuffer contextBuffer = winrt::CryptographicBuffer::ConvertStringToBinary(syncRootIdentity.data(), winrt::BinaryStringEncoding::Utf8);
                info.Context(contextBuffer);

                /*winrt::IVector<winrt::StorageProviderItemPropertyDefinition> customStates = info.StorageProviderItemPropertyDefinitions();
                AddCustomState(customStates, L"CustomStateName1", 1);
                AddCustomState(customStates, L"CustomStateName2", 2);
                AddCustomState(customStates, L"CustomStateName3", 3);*/

                winrt::StorageProviderSyncRootManager::Register(info);
                // Give the cache some time to invalidate
                Sleep(1000);
                return SyncRootRegisterStatus::Register;
            } 
            else return SyncRootRegisterStatus::Exist;
        }
        catch (...)
        {
            LogWriter::WriteLogError(L"SyncRootRegistrar::RegisterWithShell error (StorageProviderSyncRootManager::Register)", static_cast<HRESULT>(winrt::to_hresult()));
            return SyncRootRegisterStatus::Failed;
        }
    }

    //  A real sync engine should NOT unregister the sync root upon exit.
    //  This is just to demonstrate the use of StorageProviderSyncRootManager::Unregister.
    bool SyncRootRegistrar::Unregister(LPCWSTR CFid)
    {
        try
        {
            winrt::StorageProviderSyncRootManager::Unregister(GetSyncRootId(CFid));
            return true;
        }
        catch (...)
        {
            LogWriter::WriteLogError(L"SyncRootRegistrar::Unregister error (StorageProviderSyncRootManager::Unregister)", static_cast<HRESULT>(winrt::to_hresult()));
        }
        return false;
    }

    bool SyncRootRegistrar::CheckSyncRootExist(std::wstring& syncRootID)
    {
        auto SyncRootsRegisted = winrt::StorageProviderSyncRootManager::GetCurrentSyncRoots();
        auto syncroot = SyncRootsRegisted.First();
        int size = SyncRootsRegisted.Size();
        for (int i = 0; i < size; i++)
        {
            if (syncroot.HasCurrent())
            {
                winrt::StorageProviderSyncRootInfo info = syncroot.Current();
                if (info.Id() == syncRootID)
                    return true;
            }
            syncroot.MoveNext();
        }
        return false;
    }

    std::unique_ptr<TOKEN_USER> SyncRootRegistrar::GetTokenInformation()
    {
        std::unique_ptr<TOKEN_USER> tokenInfo;

        // get the tokenHandle from current thread/process if it's null
        auto tokenHandle{ GetCurrentThreadEffectiveToken() }; // Pseudo token, don't free.

        DWORD tokenInfoSize{ 0 };
        if (!::GetTokenInformation(tokenHandle, TokenUser, nullptr, 0, &tokenInfoSize))
        {
            if (::GetLastError() == ERROR_INSUFFICIENT_BUFFER)
            {
                tokenInfo.reset(reinterpret_cast<TOKEN_USER*>(new char[tokenInfoSize]));
                if (!::GetTokenInformation(tokenHandle, TokenUser, tokenInfo.get(), tokenInfoSize, &tokenInfoSize))
                {
                    throw std::exception("GetTokenInformation failed");
                }
            }
            else
            {
                throw std::exception("GetTokenInformation failed");
            }
        }
        return tokenInfo;
    }

    std::wstring SyncRootRegistrar::GetSyncRootId(LPCWSTR CFid)
    {
        std::unique_ptr<TOKEN_USER> tokenInfo(GetTokenInformation());
        auto sidString = Utilities::ConvertSidToStringSid(tokenInfo->User.Sid);
        std::wstring syncRootID(STORAGE_PROVIDER_ID);
        syncRootID.append(L"!");
        syncRootID.append(sidString.data());
        syncRootID.append(L"!");
        syncRootID.append(CFid);
        return syncRootID;
    }

    void SyncRootRegistrar::AddCustomState(
        _In_ winrt::IVector<winrt::StorageProviderItemPropertyDefinition>& customStates,
        _In_ LPCWSTR displayNameResource,
        _In_ int id)
    {
        winrt::StorageProviderItemPropertyDefinition customState;
        customState.DisplayNameResource(displayNameResource);
        customState.Id(id);
        customStates.Append(customState);
    }
}
