#include "pch.h"
//#include "CustomStateProvider.g.cpp"
#include "CustomStateProvider.h"
#include "LogWriter.h"
namespace winrt::CssWinrt::implementation
{
    winrt::IIterable<winrt::StorageProviderItemProperty> CustomStateProvider::GetItemProperties(hstring const& itemPath)
    {
        std::hash<std::wstring> hashFunc;
        auto hash = hashFunc(itemPath.c_str());

        auto propertyVector{ winrt::single_threaded_vector<winrt::StorageProviderItemProperty>() };
        //if ((hash & 0x1) != 0)
        //{
        //    winrt::StorageProviderItemProperty itemProperty;
        //    itemProperty.Id(2);
        //    itemProperty.Value(L"Value2");
        //    // This icon is just for the sample. You should provide your own branded icon here
        //    itemProperty.IconResource(L"shell32.dll,-14");
        //    propertyVector.Append(itemProperty);
        //}

        return propertyVector;
    }
}
