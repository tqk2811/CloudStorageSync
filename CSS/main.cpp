#include "pch.h"
using namespace System;
using namespace System::Threading;
using namespace System::Threading::Tasks;
using namespace CssCs::UI::ViewModel;
MSG g_msg{ 0 };
gcroot<Mutex^> mutex;
gcroot<ManualResetEvent^> resetEvent;

void MainThread()
{
    CssCs::CppInterop::OutPutDebugString = gcnew CssCs::_OutPutDebugString(CSS::WriteLog);
    if(!CssCs::CppInterop::Init(gcnew String(CssWinrt::GetLocalStateUWPFolder().c_str()),
        gcnew SrRegister(CSS::SRManaged::Register),
        gcnew SrUnRegister(CSS::SRManaged::UnRegister))) return;
    CSS::SRManaged::Init();
    CSS::UiManaged::Init();
    resetEvent->Set();
    //------------------------main loop-----------------------------
    while (GetMessage(&g_msg, NULL, 0, 0))
    {
        TranslateMessage(&g_msg);
        DispatchMessage(&g_msg);
    }
    //------------- close app---------------------------------------
    CSS::SRManaged::UnInit();
    CSS::UiManaged::UnInit();
    //--------------------------------------------------------------
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
{
    mutex = gcnew Mutex(true, gcnew String("{6084AB82-C0EB-4D48-845B-995CC4052A5C}"));
    if (mutex->WaitOne(TimeSpan::Zero, true))
    {
        CssWinrt::LogWriter::Init();
        CSS::trackchanges = gcnew CSS::TrackChanges();
        resetEvent = gcnew ManualResetEvent(false);
        ThreadStart^ thrstart = gcnew ThreadStart(MainThread);
        Thread^ thr = gcnew Thread(thrstart);
        thr->SetApartmentState(ApartmentState::STA);

        CssWinrt::InitAndStartServiceTask();
        thr->Start();
        resetEvent->WaitOne();

        CSS::TrackChanges::InitTimer();
        thr->Join();
        CSS::TrackChanges::UnInitTimer();
        CssCs::CppInterop::ShutDown();
        CssWinrt::LogWriter::ShutDown();
        mutex->ReleaseMutex();
        return (int)g_msg.wParam;
    }
    else return 0;
}
