#pragma once

#include "CustomStateProvider.g.h"

// {03D0F708-FF49-4E88-811A-0458E1609FCA}
constexpr CLSID CLSID_CustomStateProvider = { 0x3d0f708, 0xff49, 0x4e88, { 0x81, 0x1a, 0x4, 0x58, 0xe1, 0x60, 0x9f, 0xca } };

namespace winrt::CssWinrt::implementation
{
    struct CustomStateProvider : CustomStateProviderT<CustomStateProvider>
    {
        CustomStateProvider() = default;

        Windows::Foundation::Collections::IIterable<Windows::Storage::Provider::StorageProviderItemProperty> GetItemProperties(_In_ hstring const& itemPath);
    };
}

namespace winrt::CssWinrt::factory_implementation
{
    struct CustomStateProvider : CustomStateProviderT<CustomStateProvider, implementation::CustomStateProvider>
    {
    };
}