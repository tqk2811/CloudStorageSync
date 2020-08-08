#include "pch.h"
#include "UiManaged.h"

#include "TrayIcon.h"

namespace CSS
{
    TrayIcon* tray{ nullptr };
    HMENU tray_menu{ NULL };
	void UiManaged::Init()
	{
        if (!tray)
        {
            tray = new TrayIcon(WndProc_trayicon, L"Cloud Storage Sync", true, LoadIconFromResource(IDI_Cloud));
            tray_menu = CreatePopupMenu();
            AppendMenu(tray_menu, MF_STRING, IDM_SETTING, L"Setting");
#if _DEBUG
            AppendMenu(tray_menu, MF_STRING, IDM_EXIT, L"Exit");
#endif
            WriteLog(L"UiManaged::Init end");
        }
	}

    void UiManaged::UnInit()
    {
        if (!tray) 
            return;
        delete tray;
        delete settingwindow;
        DestroyMenu(tray_menu);

        tray = nullptr;
        settingwindow = nullptr;
        tray_menu = NULL;
    }

    void UiManaged::OnClosed(System::Object^ sender, System::EventArgs^ e)
    {
        delete UiManaged::settingwindow;
        UiManaged::settingwindow = nullptr;
        GC::Collect();
    }

    void UiManaged::OnCreateSyncRoot()
    {
        SyncRoot^ sr = gcnew SyncRoot(CssCs::Extensions::RandomString(32), settingwindow->GetAccountVMSelected()->AccountData->Id);
        sr->Insert();
        CSS::SyncRootViewModel^ srvm = gcnew CSS::SyncRootViewModel(sr);
        settingwindow->SyncRootViewModels->Add(srvm);
    }

    LRESULT CALLBACK WndProc_trayicon(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
    {
        UINT umsg = (UINT)lParam;
        switch (msg)
        {
        case TRAY_WINDOW_MESSAGE:
        {
            switch (umsg)
            {
            case WM_RBUTTONUP:
                POINT curPoint;
                GetCursorPos(&curPoint);
                SetForegroundWindow(hWnd);
                TrackPopupMenu(tray_menu, TPM_BOTTOMALIGN | TPM_LEFTALIGN, curPoint.x, curPoint.y, 0, hWnd, NULL);
                return 0;
            default:
                break;
            }
            break;
        }
        case WM_COMMAND:
        {
            int wmId = LOWORD(wParam);
            switch (wmId)
            {
            case IDM_SETTING:
                if (UiManaged::settingwindow == nullptr) 
                {
                    UiManaged::settingwindow = gcnew CssCs::UI::SettingWindow();
                    UiManaged::settingwindow->Closed += gcnew System::EventHandler(&UiManaged::OnClosed);
                    UiManaged::settingwindow->OnCreateSyncRoot += gcnew CssCs::UI::OnCreateSyncRoot(&UiManaged::OnCreateSyncRoot);
                    UiManaged::settingwindow->Show();
                }
                else UiManaged::settingwindow->Show();
                return 0;
            case IDM_EXIT:
                PostQuitMessage(0);
                return 0;
            default:
                break;
            }
            break;
        }
        case WM_DESTROY:
        {
            DestroyWindow(hWnd);
            return 0;
        }
        case WM_ENDSESSION:
        case WM_QUERYENDSESSION:        
            PostQuitMessage(0);
            return 0;
        
        default:
        {
            break;
        }
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
