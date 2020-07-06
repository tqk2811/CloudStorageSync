#include "pch.h"
#include "ShellCall.h"

#include <comdef.h>
#include <ShlObj_core.h>
#include <SearchAPI.h>//ISearchManager,ISearchCatalogManager,ISearchCrawlScopeManager
#include <propkey.h>//PKEY_SyncTransferStatus
#include <propvarutil.h>//InitPropVariantFromUInt64Vector,InitPropVariantFromUInt32
#define MSSEARCH_INDEX L"SystemIndex"
DEFINE_PROPERTYKEY(PKEY_StorageProviderTransferProgress, 0xE77E90DF, 0x6271, 0x4F5B, 0x83, 0x4F, 0x2D, 0xD1, 0xF2, 0x45, 0xDD, 0xA4, 4);
_COM_SMARTPTR_TYPEDEF(IShellItem2, IID_IShellItem2);
_COM_SMARTPTR_TYPEDEF(IPropertyStore, IID_IPropertyStore);
_COM_SMARTPTR_TYPEDEF(ISearchManager, IID_ISearchManager);
_COM_SMARTPTR_TYPEDEF(ISearchCatalogManager, IID_ISearchCatalogManager);
_COM_SMARTPTR_TYPEDEF(ISearchCrawlScopeManager, IID_ISearchCrawlScopeManager);
namespace CSS
{
	void ShellCall::TransferProgressBar(_In_ PCWSTR fullPath,
		_In_ const CF_CONNECTION_KEY ConnectionKey,
		_In_ const CF_TRANSFER_KEY TransferKey,
		UINT64 total,
		UINT64 completed)
	{
        IShellItem2Ptr shellItem;
        IPropertyStorePtr propStoreVolatile;
        PROPVARIANT transferProgress;
        UINT64 values[]{ completed , total };
        PROPVARIANT transferStatus;

        HRESULT hr = SHCreateItemFromParsingName(fullPath, nullptr, IID_PPV_ARGS(&shellItem));
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar SHCreateItemFromParsingName")) return;

        hr = shellItem->GetPropertyStore(GPS_READWRITE | GPS_VOLATILEPROPERTIESONLY, IID_PPV_ARGS(&propStoreVolatile));
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar shellItem->GetPropertyStore")) return;
        
        hr = InitPropVariantFromUInt64Vector(values, ARRAYSIZE(values), &transferProgress);
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar InitPropVariantFromUInt64Vector")) return;        
        hr = propStoreVolatile->SetValue(PKEY_StorageProviderTransferProgress, transferStatus);
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar propStoreVolatile->SetValue PKEY_StorageProviderTransferProgress")) return;
       
        hr = InitPropVariantFromUInt32((completed < total) ? STS_TRANSFERRING : STS_NONE, &transferStatus);
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar InitPropVariantFromUInt32")) return;        
        hr = propStoreVolatile->SetValue(PKEY_SyncTransferStatus, transferStatus);
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar propStoreVolatile->SetValue PKEY_SyncTransferStatus")) return;

        hr = propStoreVolatile->Commit();
        if (!CheckHr(hr, L"ShellCall::TransferProgressBar propStoreVolatile->Commit")) return;

        SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATH, static_cast<LPCVOID>(fullPath), nullptr);
	}

    void ShellCall::AddFolderToSearchIndexer(_In_ PCWSTR folder)
    {
        std::wstring url(L"file:///");
        url.append(folder);
        ISearchManagerPtr searchManager;
        ISearchCatalogManagerPtr searchCatalogManager;
        ISearchCrawlScopeManagerPtr searchCrawlScopeManager;

        HRESULT hr = CoCreateInstance(__uuidof(CSearchManager), NULL, CLSCTX_SERVER, IID_PPV_ARGS(&searchManager));
        if (!CheckHr(hr, L"ShellCall::AddFolderToSearchIndexer CoCreateInstance")) return;

        hr = searchManager->GetCatalog(MSSEARCH_INDEX, &searchCatalogManager);
        if (!CheckHr(hr, L"ShellCall::AddFolderToSearchIndexer searchManager->GetCatalog")) return;

        hr = searchCatalogManager->GetCrawlScopeManager(&searchCrawlScopeManager);
        if (!CheckHr(hr, L"ShellCall::AddFolderToSearchIndexer searchCatalogManager->GetCrawlScopeManager")) return;

        hr = searchCrawlScopeManager->AddDefaultScopeRule(url.data(), TRUE, FOLLOW_FLAGS::FF_INDEXCOMPLEXURLS);
        if (!CheckHr(hr, L"ShellCall::AddFolderToSearchIndexer searchCrawlScopeManager->AddDefaultScopeRule")) return;

        hr = searchCrawlScopeManager->SaveAll();
        CheckHr(hr, L"ShellCall::AddFolderToSearchIndexer searchCrawlScopeManager->SaveAll");
    }
}