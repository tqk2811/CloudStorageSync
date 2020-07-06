#pragma once
#include <thumbcache.h>
namespace CssWinrt
{
    //[Guid("C7459EB2-BF10-49B3-96A9-F3B528F0C07E")]
    class __declspec(uuid("C7459EB2-BF10-49B3-96A9-F3B528F0C07E")) ThumbnailProvider : public winrt::implements<ThumbnailProvider, IInitializeWithItem, IThumbnailProvider>
    {
    public:
        // IInitializeWithItem
        IFACEMETHODIMP Initialize(_In_ IShellItem* item, _In_ DWORD mode);

        // IThumbnailProvider
        IFACEMETHODIMP GetThumbnail(_In_ UINT width, _Out_ HBITMAP* bitmap, _Out_ WTS_ALPHATYPE* alphaType);

    private:
        winrt::com_ptr<IShellItem2> _itemDest;
        winrt::com_ptr<IShellItem2> _itemSrc;
    };
}
