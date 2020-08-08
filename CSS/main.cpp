#include "pch.h"
namespace CSS
{
    MSG g_msg{ 0 };
    gcroot<Mutex^> mutex;
    gcroot<ManualResetEvent^> resetEvent;

    void LoadAccAndSr()
    {
        auto accs = CssCsData::Account::GetAll();
        for (int i = 0; i < accs->Count; i++)
        {
            CssCs::CppInterop::AccountViewModels->Add(gcnew AccountViewModel(accs[i]));
            for each (CssCsData::SyncRoot^ sr in accs[i]->GetSyncRoot()) gcnew SyncRootViewModel(sr);
        }
    }

    void MainThread()
    {
        CssCs::CppInterop::OutPutDebugString = gcnew CssCs::_OutPutDebugString(CSS::WriteLog);
        if (!CssCs::CppInterop::Init(gcnew String(CssWinrt::GetLocalStateUWPFolder().c_str()))) return;
        LoadAccAndSr();
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
            CssCs::CppInterop::UnInit();
            CssWinrt::LogWriter::ShutDown();
            mutex->ReleaseMutex();
            return (int)g_msg.wParam;
        }
        else return 0;
    }
}
