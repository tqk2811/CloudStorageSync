#pragma once
namespace CssWinrt
{
    class Utilities
    {
    public:
        static void ApplyCustomStateToPlaceholderFile(
            _In_ PCWSTR path, 
            _In_ PCWSTR filename, 
            _In_ int prop_id, 
            _In_ PCWSTR prop_value, 
            _In_ PCWSTR prop_IconResource);

        static winrt::com_array<wchar_t> MyConvertSidToStringSid(_In_ PSID sid);

        //inline static CF_OPERATION_INFO ToOperationInfo(_In_ CF_CALLBACK_INFO const* info, _In_ CF_OPERATION_TYPE operationType)
        //{
        //    return CF_OPERATION_INFO
        //    {
        //        sizeof(CF_OPERATION_INFO),
        //        operationType,
        //        info->ConnectionKey,
        //        info->TransferKey
        //    };
        //}
    };
}
