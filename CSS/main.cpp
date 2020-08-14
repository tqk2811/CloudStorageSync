#include "pch.h"
using namespace System::Threading::Tasks;
using namespace System::Collections::Generic;
using namespace System;
using namespace System::Threading;
MSG g_msg{ 0 };
gcroot<Mutex^> mutex;
//gcroot<ManualResetEvent^> resetEvent;
gcroot<System::Timers::Timer^> timer;

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
void OnElapsed(Object^ source, System::Timers::ElapsedEventArgs^ e)
{
    //resetevent->Reset();
    static int count = CssCsData::Setting::SettingData->TimeWatchChangeCloud;//run at start app
    try
    {
        //IList<LocalError^>^ les = LocalError::ListAll();
        //for (int i = 0; i < les->Count; i++) LocalAction::TryAgain(les[i]);
        count++;
        if (count >= CssCsData::Setting::SettingData->TimeWatchChangeCloud && CssCs::Extensions::Ping())//check internet
        {
            count = 0;
            List<Task^>^ taskwait = gcnew List<Task^>();
            for (int i = 0; i < CssCs::CppInterop::AccountViewModels->Count; i++)
            {
                taskwait->Add(CssCs::CppInterop::AccountViewModels[i]->WatchChange());
            }
            Task::WaitAll(taskwait->ToArray());

            static int count_for_collectGC = 0;
            if (count_for_collectGC++ >= 36)//~ >10p
            {
                count_for_collectGC = 0;
                GC::Collect();
            }
        }
    }
    catch (...) {}
    timer->Start();
    //resetevent->Set();
}

void MainThread()
{
    CssCs::CppInterop::OutPutDebugString = gcnew CssCs::_OutPutDebugString(CSS::WriteLog);
    if (!CssCs::CppInterop::Init(gcnew String(CssWinrt::GetLocalStateUWPFolder().c_str()))) return;
    LoadAccAndSr();

    timer = gcnew System::Timers::Timer(1000);
    timer->AutoReset = false;
    timer->Elapsed += gcnew System::Timers::ElapsedEventHandler(&OnElapsed);
    OnElapsed(nullptr, nullptr);

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

