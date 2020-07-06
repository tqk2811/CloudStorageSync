#include "pch.h"
#include "SRManager.h"
namespace CSS
{
	void SRManager::Init()
	{
		auto listSR = SyncRootViewModel::FindAllWorking(nullptr);
		List<Task^>^ waittask = gcnew List<Task^>();
		for (int i = 0; i < listSR->Count; i++)
		{
			listSR[i]->Run();
			if (listSR[i]->IsListedAll) waittask->Add(listSR[i]->TaskRun);
		}
		Task::WaitAll(waittask->ToArray());
	}

	void SRManager::UnInit()
	{
		auto listSR = SyncRootViewModel::FindAllWorking(nullptr);
		for (int i = 0; i < listSR->Count; i++) listSR[i]->Watcher->Stop();
		TaskQueues::UploadQueues->ShutDown();
	}

	void runtest(SyncRootViewModel^ srvm)
	{
		//UploadQueue^ uq = gcnew UploadQueue(nullptr);
		//uq->RunTest(srvm, gcnew String(L"D:\\test.file"), gcnew String(L"10H_3xg4bqY4Aj2PgP-bVX5Bx9yj6CWD1"));
	}

	void SRManager::Register(SyncRootViewModel^ srvm)
	{
		if (srvm->IsWork && !srvm->Status.HasFlag(SyncRootStatus::Error))
		{
			srvm->Status = SyncRootStatus::RegisteringSyncRoot;
			PinStr2(SRid, srvm->SRId);
			PinStr2(LocalPath, srvm->LocalPath);
			PinStr2(displayname, srvm->CloudFolderName + gcnew String(L" - ") + srvm->CEVM->Email);

			switch (CssWinrt::SyncRoot_RegisterWithShell(
				SRid,
				LocalPath,
				displayname,
				(int)srvm->CEVM->CloudName))
			{
			case SyncRootRegisterStatus::Registed:
			{
				ShellCall::AddFolderToSearchIndexer(LocalPath);

				srvm->ConnectionKey = ConnectSyncRoot::ConnectSyncRootTransferCallbacks(LocalPath);

				srvm->Status = SyncRootStatus::CreatingPlaceholder;
				Placeholders::CreateAll(srvm);
				if (srvm->Status.HasFlag(SyncRootStatus::Error)) return;
				else srvm->Message = String::Empty;
			}
			case SyncRootRegisterStatus::Exist:
			{
				if(!srvm->ConnectionKey) srvm->ConnectionKey = ConnectSyncRoot::ConnectSyncRootTransferCallbacks(LocalPath);
				
				srvm->Watcher->Change(
					srvm->LocalPath,
					gcnew CssCs::CustomFileSystemEventHandler(trackchanges, &CSS::TrackChanges::LocalOnChanged));
				srvm->Watcher->Start();				
				srvm->Status = SyncRootStatus::Working;

				LocalAction^ la = gcnew LocalAction();
				Action<Object^>^ action = gcnew Action<Object^>(la, &LocalAction::FindNonPlaceholderAndUploadTask);
				Task::Factory->StartNew(action, srvm);
				LogWriter::WriteLog(std::wstring(L"Syncroot Register: Success SrId:").append(SRid), 2);
				//runtest(srvm);
				break;
			}
			case SyncRootRegisterStatus::Failed://Syncroot B can't inside folder in syncroot A, and some...
			{
				srvm->Status = SyncRootStatus::RegisteringSyncRoot | SyncRootStatus::Error;
				srvm->Message = gcnew String(L"Registering SyncRoot Failed");
				break;
			}
			}
			srvm->Update();
			GC::Collect();
		}
	}
	void SRManager::UnRegister(SyncRootViewModel^ srvm)
	{
		PinStr2(SRid, srvm->SRId);
		if (srvm->Status.HasFlag(SyncRootStatus::Error)) SyncRoot_UnRegister(SRid);
		else
		{
			srvm->Watcher->Stop();
			ConnectSyncRoot::DisconnectSyncRootTransferCallbacks(srvm->ConnectionKey);
			SyncRoot_UnRegister(SRid);
			srvm->IsListedAll = false;
			if (srvm->CEVM->CloudName != CloudName::Empty)
			{
				LocalItem::Clear(srvm);
				srvm->Update();
			}
		}
		srvm->Status = SyncRootStatus::NotWorking;
		LogWriter::WriteLog(std::wstring(L"Syncroot UnRegister: Success SrId:").append(SRid), 2);
	}
}