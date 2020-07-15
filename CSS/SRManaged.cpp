#include "pch.h"
#include "SRManaged.h"
namespace CSS
{
	void SRManaged::Init()
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

	void SRManaged::UnInit()
	{
		auto listSR = SyncRootViewModel::FindAllWorking(nullptr);
		for (int i = 0; i < listSR->Count; i++) listSR[i]->Watcher->Stop();
		TaskQueues::ShutDown();
	}

	void SRManaged::Register(SyncRootViewModel^ srvm)
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
				CreatePlaceholders(srvm);
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

	void SRManaged::UnRegister(SyncRootViewModel^ srvm)
	{
		PinStr2(SRid, srvm->SRId);
		srvm->Watcher->Stop();
		ConnectSyncRoot::DisconnectSyncRootTransferCallbacks(srvm->ConnectionKey);
		srvm->ConnectionKey = 0;
		SyncRoot_UnRegister(SRid);
		srvm->IsListedAll = false;
		LocalItem::Clear(srvm);
		srvm->Update();		
		srvm->Status = SyncRootStatus::NotWorking;
		LogWriter::WriteLog(std::wstring(L"Syncroot UnRegister: Success SrId:").append(SRid), 2);
	}

	void SRManaged::CreatePlaceholders(SyncRootViewModel^ srvm)
	{
		LocalItem^ root = gcnew LocalItem();
		root->CloudId = srvm->CloudFolderId;
		root->Name = srvm->CloudFolderName;
		root->LocalParentId = 0;
		root->SRId = srvm->SRId;
		root->Flag = LocalItemFlag::Folder;
		root->Insert();
		Placeholders::CreateAll(srvm, srvm->CloudFolderId, root->LocalId, String::Empty);
	}
}