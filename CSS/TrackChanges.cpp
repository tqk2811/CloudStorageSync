#include "pch.h"
#include "TrackChanges.h"
namespace CSS
{	
	void TrackChanges::InitTimer()
	{
		if (resetevent != nullptr) return;
		resetevent = gcnew ManualResetEvent(false);
		aTimer = gcnew System::Timers::Timer(1000);
		aTimer->Elapsed += gcnew System::Timers::ElapsedEventHandler(&OnElapsed);
		aTimer->AutoReset = false;
		OnElapsed(nullptr, nullptr);
	}

	void TrackChanges::UnInitTimer()
	{
		if (resetevent == nullptr) return;		
		resetevent->WaitOne();//confirm timer done before shutdown
		aTimer->Stop();

		delete aTimer;
		delete resetevent;
		aTimer = nullptr;
		resetevent = nullptr;
	}

	void TrackChanges::OnElapsed(Object^ source, System::Timers::ElapsedEventArgs^ e)
	{
		//resetevent->Reset();
		//static int count = CssCs::Settings::Setting->TimeWatchChangeCloud;
		//static int firsttime = true;
		//try
		//{
		//	IList<LocalError^>^ les = LocalError::ListAll();
		//	for (int i = 0; i < les->Count; i++) LocalAction::TryAgain(les[i]);
		//	count++;
		//	if (count >= CssCs::Settings::Setting->TimeWatchChangeCloud && CssCs::Extensions::Ping())//check internet
		//	{
		//		count = 0;
		//		List<Task^>^ taskwait = gcnew List<Task^>();
		//		for (int i = 0; i < CloudEmailViewModel::CEVMS->Count; i++)
		//		{
		//			auto action = gcnew Action<Task<CloudChangeTypeCollection^>^, Object^>(trackchanges, &CSS::TrackChanges::WatchChangeResult);
		//			if (firsttime) CloudEmailViewModel::CEVMS[i]->LoadQuota();
		//			taskwait->Add(CloudEmailViewModel::CEVMS[i]->Cloud->WatchChange()->ContinueWith(action, CloudEmailViewModel::CEVMS[i]));
		//		}
		//		firsttime = false;
		//		Task::WaitAll(taskwait->ToArray());
		//		static int count_for_collectGC = 0;
		//		if (count_for_collectGC++ >= 36)//~ >10p
		//		{
		//			count_for_collectGC = 0;
		//			GC::Collect();
		//		}
		//	}
		//}
		//catch (...) {}
		//aTimer->Start();
		//resetevent->Set();
	}

	void TrackChanges::WatchChangeResult(Task<ICollection<ICloudChangeType^>^>^ t, Object^ obj)
	{
		////update change
		//if (t->Status.HasFlag(TaskStatus::Faulted))
		//{
		//	String^ str = String::Format(CultureInfo::InvariantCulture, L"TrackChanges::WatchChangeResult Exception, Message:{0}", t->Exception->InnerException->Message);
		//	PinStr(str);
		//	LogWriter::WriteLog(pin_str, 0);
		//	return;
		//}else if(t->Status.HasFlag(TaskStatus::Canceled)) return;
		//CloudEmailViewModel^ cevm = (CloudEmailViewModel^)obj;
		//CloudChangeTypeCollection^ changes = t->Result;
		//List<SyncRootViewModel^>^ workingCF_inEmail = SyncRootViewModel::FindAllWorking(cevm);
		//for (int i = 0; i < changes->Count; i++)
		//{
		//	for (int j = 0; j < workingCF_inEmail->Count; j++)
		//	{
		//		if(workingCF_inEmail[j]->Status == SyncRootStatus::Working) 
		//			UpdateChange(changes[i], workingCF_inEmail[j]);
		//	}
		//}
		//cevm->WatchToken = changes->NewWatchToken;
	}

	void TrackChanges::UpdateChange(ICloudChangeType^ change, SyncRootViewModel^ srvm)
	{
		//String^ log = String::Format(CultureInfo::InvariantCulture, "CSS::TrackChanges::UpdateChange for CloudItemId:{0} in SRId:{1}", change->Id, srvm->SRId);
		//WriteLog(log, 2);
		//IList<LocalItem^>^ localitems = LocalItem::FindAll(srvm, change->Id);
		//if (change->Flag.HasFlag(CloudChangeFlag::IsDeleted)) for (int i = 0; i < localitems->Count; i++) LocalAction::DeleteLocal(srvm, localitems[i]);
		//else if (change->Flag.HasFlag(CloudChangeFlag::IsNewItem)) LocalAction::InsertLocal(srvm, change->ParentsNew, change->CiNew);
		//else
		//{
		//	if (localitems->Count == 0) LocalAction::InsertLocal(srvm, change->CiNew->ParentsId, change->CiNew);//item not found in local -> try insert inside parent
		//	else
		//	{
		//		//check is change parent
		//		IList<String^>^ localitems_Parents = nullptr;
		//		if (change->IsChangeParent)
		//		{
		//			LocalAction::DeleteLocal(srvm, change->ParentsRemove, change->Id);
		//			LocalAction::InsertLocal(srvm, change->ParentsNew, change->CiNew);
		//			localitems_Parents = change->ParentsCurrent;
		//		}
		//		else localitems_Parents = change->CiNew->ParentsId;
		//		if (change->Flag.HasFlag(CloudChangeFlag::IsRename)) LocalAction::RenameLocal(srvm, localitems, change->CiNew);//if rename -> time Mod change -> UpdateLocal
		//		if (change->Flag.HasFlag(CloudChangeFlag::IsChangeTimeAndSize)) LocalAction::UpdateLocal(srvm, localitems_Parents, change->CiNew);
		//	}
		//}
	}

}