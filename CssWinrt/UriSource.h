#pragma once
#include "UriSource.g.h"

// {B3BBD883-0E09-423A-A986-5A9FC4A4A397}
constexpr CLSID CLSID_UriSource = { 0xb3bbd883, 0xe09, 0x423a, { 0xa9, 0x86, 0x5a, 0x9f, 0xc4, 0xa4, 0xa3, 0x97 } };

namespace winrt::CssWinrt::implementation
{
    struct UriSource : UriSourceT<UriSource>
    {
        UriSource() = default;

        void GetPathForContentUri(_In_ hstring const& contentUri, _Out_ Windows::Storage::Provider::StorageProviderGetPathForContentUriResult const& result);
        void GetContentInfoForPath(_In_ hstring const& path, _Out_ Windows::Storage::Provider::StorageProviderGetContentInfoForPathResult const& result);
    };
}

namespace winrt::CssWinrt::factory_implementation
{
    struct UriSource : UriSourceT<UriSource, implementation::UriSource>
    {
    };
}



