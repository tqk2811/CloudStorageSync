#include "pch.h"
#include "UriSource.h"

namespace winrt::CssWinrt::implementation
{
    void UriSource::GetPathForContentUri(hstring const& contentUri, winrt::StorageProviderGetPathForContentUriResult const& result)
    {
        result.Status(StorageProviderUriSourceStatus::FileNotFound);        
        //std::wstring prefix(L"http://cloudmirror.example.com/contentUri/");
        //std::wstring uri(contentUri);
        //if (0 == uri.compare(0, prefix.length(), prefix))
        //{
        //    std::wstring localPath(L"ProviderFolderLocations::GetClientFolder()");
        //    localPath.append(L"\\");
        //    localPath.append(uri.substr(prefix.length(), uri.find(L'?') - prefix.length()));

        //    if (!true)//PathFileExists(localPath.c_str())
        //    {
        //        result.Path(localPath);
        //        result.Status(StorageProviderUriSourceStatus::Success);
        //    }
        //}
    }

    void UriSource::GetContentInfoForPath(hstring const& path, Windows::Storage::Provider::StorageProviderGetContentInfoForPathResult const& result)
    {
        result.Status(StorageProviderUriSourceStatus::FileNotFound);
        //PCWSTR fileName = L"PathFindFileName(path.c_str())";
        //std::wstring id(L"http://cloudmirror.example.com/contentId/");
        //id.append(fileName);
        //result.ContentId(id);

        //id.assign(L"http://cloudmirror.example.com/contentUri/");
        //id.append(fileName);
        //id.append(L"?StorageProviderId=TestStorageProvider");
        //result.ContentUri(id);

        //result.Status(StorageProviderUriSourceStatus::Success);
    }
}
