#include "pch.h"
#pragma unmanaged
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        winrt::init_apartment();
#if _DEBUG
        OutputDebugString(L"=============== CssWinrt: DLL_PROCESS_ATTACH\r\n");
#endif // 
        break;
    }
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
    {
        winrt::uninit_apartment();
#if _DEBUG
        OutputDebugString(L"=============== CssWinrt: DLL_PROCESS_DETACH\r\n");
#endif //
        break;
    }
    }
    return TRUE;
}
