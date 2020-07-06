#include "pch.h"
#include "TrayIcon.h"
#include <cassert>
namespace CSS
{
	static UINT GetNextTrayIconId()
	{
		static UINT next_id = 1;
		return next_id++;
	}

	TrayIcon::TrayIcon(WNDPROC WndProc, const wchar_t* name, bool visible, HICON hIcon, LPCWSTR szWindowClass, LPCWSTR menuname):
		p_WndProc(WndProc),p_szWindowClass(szWindowClass) ,p_Name(name),p_Visible(false),p_hIcon(hIcon),p_menuname(menuname),
		p_hwnd(NULL), p_uid(GetNextTrayIconId())//, p_classregister(false)
	{
		SetVisible(visible);
	}

	TrayIcon::~TrayIcon()
	{
		SetVisible(false);
		if (p_hIcon) DestroyIcon(p_hIcon);//clean memory hicon
	}


	//NIM_MODIFY
	void TrayIcon::SetIcon(HICON hIcon, bool destroy_current_icon)
	{
		if (!hIcon || p_hIcon == hIcon) return;
		if (destroy_current_icon && p_hIcon) DestroyIcon(p_hIcon);//clean memory hicon
		p_hIcon = hIcon;
		if (p_Visible)
		{
			NOTIFYICONDATA data;
			FillNotifyIconData(data);
			data.uFlags |= NIF_ICON;
			data.hIcon = p_hIcon;
			Shell_NotifyIcon(NIM_MODIFY, &data);
		}
	}

	//NIM_MODIFY
	void TrayIcon::SetName(const wchar_t* name)
	{
		p_Name = name;
		if (p_Visible)
		{
			NOTIFYICONDATA data;
			FillNotifyIconData(data);
			data.uFlags |= NIF_TIP;
			size_t tip_len = max(sizeof(data.szTip) - 1, wcslen(name));
			memcpy(data.szTip, name, tip_len);
			Shell_NotifyIcon(NIM_MODIFY, &data);
		}
	}

	//NIM_MODIFY
	bool TrayIcon::ShowBalloonTooltip(const wchar_t* title, const WCHAR* msg, ETooltipIcon icon)
	{
#ifndef NOTIFYICONDATA_V2_SIZE
		return false;
#else
		if (!p_Visible)	return false;

		NOTIFYICONDATA data;
		FillNotifyIconData(data);
		data.cbSize = NOTIFYICONDATA_V2_SIZE;	// win2k and later
		data.uFlags |= NIF_INFO;
		data.dwInfoFlags = icon;
		data.uTimeout = 10000;	// deprecated as of Windows Vista, it has a min(10000) and max(30000) value on previous Windows versions.

		size_t title_len = max(sizeof(data.szInfoTitle) - 1, wcslen(title));
		memcpy(data.szInfoTitle, title, title_len);
		data.szInfoTitle[title_len] = 0;

		size_t msg_len = max(sizeof(data.szInfo) - 1, wcslen(msg));
		memcpy(data.szInfo, msg, msg_len);
		data.szInfo[msg_len] = 0;

		return FALSE != Shell_NotifyIcon(NIM_MODIFY, &data);
#endif
	}

	bool TrayIcon::SetVisible(bool visible)
	{
		if (p_Visible == visible) return true;
		if (visible)
		{
			p_Visible = AddIcon();
			return p_Visible;
		}
		else
		{
			p_Visible = !RemoveIcon();
			return !p_Visible;
		}
	}

	//-----------private function-------------------//

	//NIM_ADD
	bool TrayIcon::AddIcon()
	{
		if (!p_WndProc) return false;
		NOTIFYICONDATA data;
		FillNotifyIconData(data);
		data.uFlags |= NIF_MESSAGE | NIF_ICON | NIF_TIP;
		data.uCallbackMessage = TRAY_WINDOW_MESSAGE;
		data.hIcon = p_hIcon;

		size_t tip_len = max(sizeof(data.szTip) - 1, wcslen(p_Name.c_str()));
		memcpy(data.szTip, p_Name.c_str(), tip_len);
		data.szTip[tip_len] = 0;
		return FALSE != Shell_NotifyIcon(NIM_ADD, &data);
	}
	//NIM_DELETE
	bool TrayIcon::RemoveIcon()
	{
		NOTIFYICONDATA data;
		FillNotifyIconData(data);
		return FALSE != Shell_NotifyIcon(NIM_DELETE, &data);
	}

	HWND TrayIcon::CreateHWND()
	{
		if (!p_hwnd)
		{
			HINSTANCE hInstance = (HINSTANCE)GetModuleHandle(NULL);

			WNDCLASSEX wc;
			wc.cbSize = sizeof(wc);
			wc.cbClsExtra = 0;
			wc.cbWndExtra = 0;
			wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
			wc.hCursor = LoadCursor(hInstance, IDC_ARROW);
			wc.hIcon = LoadIcon(hInstance, IDI_WINLOGO);
			wc.hIconSm = NULL;
			wc.hInstance = hInstance;
			wc.lpfnWndProc = p_WndProc;
			wc.lpszClassName = p_szWindowClass;
			wc.lpszMenuName = p_menuname;
			wc.style = 0;
			SetLastError(0);//clear last error
			RegisterClassEx(&wc);
			auto err = GetLastError();//1410 ERROR_CLASS_ALREADY_EXISTS
			if (err != 0 & err != 1410) return NULL;
			p_hwnd = CreateWindowEx(0, p_szWindowClass, L"TRAY_ICON_WND", WS_POPUP, 0, 0, 0, 0, NULL, NULL, hInstance, NULL);
		}
		return p_hwnd;
	}

	void TrayIcon::FillNotifyIconData(NOTIFYICONDATA& data)
	{
		memset(&data, 0, sizeof(data));
		// the basic functions need only V1
#ifdef NOTIFYICONDATA_V1_SIZE
		data.cbSize = NOTIFYICONDATA_V1_SIZE;
#else
		data.cbSize = sizeof(data);
#endif
		data.hWnd = CreateHWND();
		assert(data.hWnd);
		data.uID = p_uid;
	}
}