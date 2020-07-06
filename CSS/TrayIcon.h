#pragma once
namespace CSS
{
#ifndef TRAY_WINDOW_MESSAGE
#define TRAY_WINDOW_MESSAGE (WM_USER+100)
#endif
	enum ETooltipIcon
	{
		eTI_None,			// NIIF_NONE(0)
		eTI_Info,			// NIIF_INFO(1)
		eTI_Warning,		// NIIF_WARNING(2)
		eTI_Error			// NIIF_ERROR(3)
	};
	class TrayIcon
	{
	public:		
		TrayIcon(WNDPROC WndProc = NULL,const wchar_t* name = L"tray_icon", bool visible = false, HICON hIcon = NULL, LPCWSTR szWindowClass = L"TrayIconClass",LPCWSTR menuname = NULL);
		~TrayIcon();

		void SetIcon(HICON hIcon, bool destroy_current_icon = true);
		HICON GetIcon() { return p_hIcon; }

		void SetName(const wchar_t* name);
		const wchar_t* GetName() const { return p_Name.c_str(); }

		bool SetVisible(bool visible);
		bool ShowBalloonTooltip(const wchar_t* title, const WCHAR* msg, ETooltipIcon icon);
		HWND GetHWND() { return p_hwnd; }
	private:
		bool AddIcon();
		bool RemoveIcon();
		HWND CreateHWND();
		void FillNotifyIconData(NOTIFYICONDATA& data);

		LPCWSTR p_szWindowClass;
		std::wstring p_Name;
		bool p_Visible;
		HICON p_hIcon;
		WNDPROC p_WndProc;
		HWND p_hwnd;
		LPCWSTR p_menuname;
		UINT p_uid;
		//bool p_classregister;
	};
}
