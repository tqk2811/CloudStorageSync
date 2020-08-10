#include "pch.h"
using namespace System::Threading::Tasks;
using namespace System::Collections::Generic;
using namespace System;
using namespace System::Threading;
MSG g_msg{ 0 };
gcroot<Mutex^> mutex;
//gcroot<ManualResetEvent^> resetEvent;

void LoadAccAndSr()
{
    auto accs = CssCsData::Account::GetAll();
    List<Task^>^ tasks = gcnew List<Task^>();
    for (int i = 0; i < accs->Count; i++)
    {
        CssCs::CppInterop::AccountViewModels->Add(gcnew CSS::AccountViewModel(accs[i]));
        for each (CssCsData::SyncRoot ^ sr in accs[i]->GetSyncRoot())
        {
            CSS::SyncRootViewModel^ srvm = gcnew CSS::SyncRootViewModel(sr);
            tasks->Add(srvm->Run());
        }
    }
    Task::WaitAll(tasks->ToArray());
}

void MainThread()
{
    CssCs::CppInterop::OutPutDebugString = gcnew CssCs::_OutPutDebugString(CSS::WriteLog);
    if (!CssCs::CppInterop::Init(gcnew String(CssWinrt::GetLocalStateUWPFolder().c_str()))) return;
    LoadAccAndSr();
    CSS::UiManaged::Init();
    //resetEvent->Set();
    //------------------------main loop-----------------------------
    while (GetMessage(&g_msg, NULL, 0, 0))
    {
        TranslateMessage(&g_msg);
        DispatchMessage(&g_msg);
    }
    //------------- close app---------------------------------------
    CSS::UiManaged::UnInit();
    //--------------------------------------------------------------
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
{
    mutex = gcnew Mutex(true, gcnew String("{6084AB82-C0EB-4D48-845B-995CC4052A5C}"));
    if (mutex->WaitOne(TimeSpan::Zero, true))
    {
        CssWinrt::LogWriter::Init();
        //resetEvent = gcnew ManualResetEvent(false);
        ThreadStart^ thrstart = gcnew ThreadStart(MainThread);
        Thread^ thr = gcnew Thread(thrstart);
        thr->SetApartmentState(ApartmentState::STA);

        CssWinrt::InitAndStartServiceTask();
        thr->Start();
        //resetEvent->WaitOne();

        thr->Join();
        CssCs::CppInterop::UnInit();
        CssWinrt::LogWriter::ShutDown();
        mutex->ReleaseMutex();
        return (int)g_msg.wParam;
    }
    else return 0;
}

