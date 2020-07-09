#pragma once
namespace CSS
{
	LRESULT CALLBACK WndProc_trayicon(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
	ref class UiManaged
	{
	public:
		static void Init();
		static void UnInit();		
	internal:
		static CssCs::UI::SettingWindow^ settingwindow{ nullptr };
		static void OnClosed(System::Object^ sender, System::EventArgs^ e);
	};
}


