#include "pch.h"
#include "ThumbnailProvider.h"
#include <ShlObj_core.h>//BHID_ThumbnailHandler
#include <pathcch.h>//PathAllocCombine
// IInitializeWithItem
namespace CssWinrt
{
    IFACEMETHODIMP ThumbnailProvider::Initialize(_In_ IShellItem* item, _In_ DWORD mode)
    {        
        try
        {
            winrt::check_hresult(item->QueryInterface(__uuidof(_itemDest), _itemDest.put_void()));
            // We want to identify the original item in the source folder that we're mirroring,
            // based on the placeholder item that we get initialized with.  There's probably a way
            // to do this based on the file identity blob but this just uses path manipulation.
            winrt::com_array<wchar_t> destPathItem;
            winrt::check_hresult(_itemDest->GetDisplayName(SIGDN_FILESYSPATH, winrt::put_abi(destPathItem)));

            //LogWriter::WriteLog(std::wstring(L"ThumbnailProvider::Initialize, destPath:").append(destPathItem.data()), 2);

            // Verify the item is underneath the root as we expect.
            //for(CFP* cfp : cfps) if (PathIsPrefix(cfp->LocalPath, destPathItem.data())) _cfp = cfp;
            //if(!_cfp) return E_UNEXPECTED;

            // Find the relative segment to the sync root.
            //wchar_t relativePath[MAX_PATH];
            //winrt::check_bool(PathRelativePathTo(relativePath, ProviderFolderLocations::GetClientFolder(), FILE_ATTRIBUTE_DIRECTORY, destPathItem.data(), FILE_ATTRIBUTE_NORMAL));

            // Now combine that relative segment with the original source folder, which results
            // in the path to the source item that we're mirroring.
            //winrt::com_array<wchar_t> sourcePathItem;
            //winrt::check_hresult(PathAllocCombine(ProviderFolderLocations::GetServerFolder(), relativePath, PATHCCH_ALLOW_LONG_PATHS, winrt::put_abi(sourcePathItem)));

            //winrt::check_hresult(SHCreateItemFromParsingName(sourcePathItem.data(), nullptr, __uuidof(_itemSrc), _itemSrc.put_void()));
        }
        catch (...)
        {
            return winrt::to_hresult();
        }

        return E_UNEXPECTED;//S_OK;
    }

    // IThumbnailProvider
    IFACEMETHODIMP ThumbnailProvider::GetThumbnail(_In_ UINT width, _Out_ HBITMAP* bitmap, _Out_ WTS_ALPHATYPE* alphaType)
    {
        // Retrieve thumbnails of the placeholders on demand by delegating to the thumbnail of the source items.
        *bitmap = nullptr;
        *alphaType = WTSAT_UNKNOWN;
        LogWriter::WriteLog(L"ThumbnailProvider::GetThumbnail", 2);
        try
        {
            winrt::com_ptr<IThumbnailProvider> thumbnailProviderSource;
            winrt::check_hresult(_itemSrc->BindToHandler(nullptr, BHID_ThumbnailHandler, __uuidof(thumbnailProviderSource), thumbnailProviderSource.put_void()));
            winrt::check_hresult(thumbnailProviderSource->GetThumbnail(width, bitmap, alphaType));
        }
        catch (...)
        {
            return winrt::to_hresult();
        }

        return E_UNEXPECTED;//S_OK;
    }
}
