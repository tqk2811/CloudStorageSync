#include "pch.h"
#include "Utilities.h"
namespace CssWinrt
{
    void Utilities::ApplyCustomStateToPlaceholderFile(
        _In_ PCWSTR path, 
        _In_ PCWSTR filename, 
        _In_ int prop_id, 
        _In_ PCWSTR prop_value, 
        _In_ PCWSTR prop_IconResource)
    {
        try
        {
            winrt::StorageProviderItemProperty prop;
            prop.Id(prop_id);
            prop.Value(prop_value);
            prop.IconResource(prop_IconResource);

            std::wstring fullPath(path);
            fullPath.append(L"\\");
            fullPath.append(filename);

            auto customProperties{ winrt::single_threaded_vector<winrt::StorageProviderItemProperty>() };
            customProperties.Append(prop);

            winrt::IStorageItem item = winrt::StorageFile::GetFileFromPathAsync(fullPath).get();
            winrt::StorageProviderItemProperties::SetAsync(item, customProperties).get();
        }
        catch (...)
        {
            LogWriter::WriteLogError(
                std::wstring(L"Utilities::ApplyCustomStateToPlaceholderFile Failed to set custom state with \"").append(path).append(L"\""), 
                winrt::to_hresult());
        }
    }
}