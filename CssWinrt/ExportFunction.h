#pragma once
namespace CssWinrt
{
	DLL_EXPORTS SyncRootRegisterStatus SyncRoot_RegisterWithShell(const SyncRootRegistrarInfo& registerarInfo);
	DLL_EXPORTS bool SyncRoot_UnRegister(LPCWSTR CFid);
	DLL_EXPORTS void SyncRoot_UnRegisterAll();

	DLL_EXPORTS void InitAndStartServiceTask();
	DLL_EXPORTS void ApplyCustomStateToPlaceholderFile(
		_In_ PCWSTR path,
		_In_ PCWSTR filename,
		_In_ int prop_id,
		_In_ PCWSTR prop_value,
		_In_ PCWSTR prop_IconResource);

	DLL_EXPORTS void check_hresult(HRESULT hr);

	DLL_EXPORTS std::wstring GetLocalStateUWPFolder();

	DLL_EXPORTS std::wstring GetExecutablePath();
}
