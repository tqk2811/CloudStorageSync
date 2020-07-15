#include "pch.h"
#include "ExportFunction.h"
namespace CssWinrt
{
	SyncRootRegisterStatus SyncRoot_RegisterWithShell(const SyncRootRegistrarInfo& registerarInfo)
	{
		return SyncRootRegistrar::RegisterWithShell(registerarInfo);
	}

	bool SyncRoot_UnRegister(LPCWSTR CFid)
	{
		return SyncRootRegistrar::Unregister(CFid);
	}

	void InitAndStartServiceTask()
	{
		ShellServices::InitAndStartServiceTask();
	}

	void ApplyCustomStateToPlaceholderFile(
		_In_ PCWSTR path,
		_In_ PCWSTR filename,
		_In_ int prop_id,
		_In_ PCWSTR prop_value,
		_In_ PCWSTR prop_IconResource)
	{
		Utilities::ApplyCustomStateToPlaceholderFile(path, filename, prop_id, prop_value, prop_IconResource);
	}

	void check_hresult(HRESULT hr)
	{
		winrt::check_hresult(hr);
	}

	std::wstring GetLocalStateUWPFolder()
	{
		//ApplicationData::Current().RoamingFolder()	->		CloudStorageSync_1rmcy8gdenfkr\RoamingState
		//ApplicationData::Current().LocalFolder()		->		CloudStorageSync_1rmcy8gdenfkr\LocalState
		winrt::hstring path = winrt::Windows::Storage::ApplicationData::Current().LocalFolder().Path();
		return std::wstring(path);
	}

	std::wstring GetExecutablePath()
	{
		std::wstring buffer;
		size_t nextBufferLength = MAX_PATH;

		for (;;)
		{
			buffer.resize(nextBufferLength);
			nextBufferLength *= 2;

			auto pathLength = GetModuleFileName(NULL, &buffer[0], static_cast<DWORD>(buffer.length()));

			if (pathLength == 0)
				throw std::exception("GetModuleFileName failed"); // You can call GetLastError() to get more info here

			if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
			{
				buffer.resize(pathLength);
				return buffer;
			}
		}
	}
}