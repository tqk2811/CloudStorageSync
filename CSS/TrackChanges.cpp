#include "pch.h"
#include "TrackChanges.h"
namespace CSS
{
	gcroot<TrackChanges^> trackchanges;
	
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
		resetevent->Reset();
		static int count = CssCs::Settings::Setting->TimeWatchChangeCloud;
		static int firsttime = true;
		try
		{
			IList<LocalError^>^ les = LocalError::ListAll();
			for (int i = 0; i < les->Count; i++) LocalAction::TryAgain(les[i]);
			count++;
			if (count >= CssCs::Settings::Setting->TimeWatchChangeCloud && CssCs::Extensions::Ping())//check internet
			{
				count = 0;
				List<Task^>^ taskwait = gcnew List<Task^>();
				for (int i = 0; i < CloudEmailViewModel::CEVMS->Count; i++)
				{
					auto action = gcnew Action<Task<IList<CloudChangeType^>^>^, Object^>(trackchanges, &CSS::TrackChanges::WatchChangeResult);
					if (firsttime) CloudEmailViewModel::CEVMS[i]->LoadQuota();
					taskwait->Add(CloudEmailViewModel::CEVMS[i]->Cloud->WatchChange()->ContinueWith(action, CloudEmailViewModel::CEVMS[i]));
				}
				firsttime = false;
				Task::WaitAll(taskwait->ToArray());
				GC::Collect();
			}
		}
		catch (...) {}
		aTimer->Start();
		resetevent->Set();
	}

	void TrackChanges::WatchChangeResult(Task<IList<CloudChangeType^>^>^ t, Object^ obj)
	{
		//update change
		if (t->Status.HasFlag(TaskStatus::Faulted))
		{
			String^ str = String::Format(L"TrackChanges::WatchChangeResult Exception, Message:{0}", t->Exception->InnerException->Message);
			PinStr(str);
			LogWriter::WriteLog(pin_str, 0);
			return;
		}else if(t->Status.HasFlag(TaskStatus::Canceled)) return;

		CloudEmailViewModel^ cevm = (CloudEmailViewModel^)obj;
		IList<CloudChangeType^>^ changes = t->Result;
		List<SyncRootViewModel^>^ workingCF_inEmail = SyncRootViewModel::FindAllWorking(cevm);
		for (int i = 0; i < changes->Count; i++)
		{
			for (int j = 0; j < workingCF_inEmail->Count; j++)
			{
				if(workingCF_inEmail[j]->Status == SyncRootStatus::Working) UpdateChange(changes[i], workingCF_inEmail[j]);
			}
		}
	}

	void TrackChanges::UpdateChange(CloudChangeType^ change, SyncRootViewModel^ srvm)
	{
		String^ log = String::Format("CSS::TrackChanges::UpdateChange for CloudItemId:{0} in SRId:{1}", change->Id, srvm->SRId);
		WriteLog(log, 2);
		IList<LocalItem^>^ localitems = LocalItem::FindAll(srvm, change->Id);
		if (change->Flag.HasFlag(CloudChangeFlag::IsDeleted)) for (int i = 0; i < localitems->Count; i++) LocalAction::DeleteLocal(srvm, localitems[i]);
		else if (change->Flag.HasFlag(CloudChangeFlag::IsNewItem)) LocalAction::InsertLocal(srvm, change->ParentsNew, change->CiNew);
		else
		{
			if (localitems->Count == 0) LocalAction::InsertLocal(srvm, change->CiNew->ParentsId, change->CiNew);//item not found in local -> try insert inside parent
			else
			{
				//check is change parent
				IList<String^>^ localitems_Parents = nullptr;
				if (change->IsChangeParent)
				{
					LocalAction::DeleteLocal(srvm, change->ParentsRemove, change->Id);
					LocalAction::InsertLocal(srvm, change->ParentsNew, change->CiNew);
					localitems_Parents = change->ParentsCurrent;
				}
				else localitems_Parents = change->CiNew->ParentsId;
				if (change->Flag.HasFlag(CloudChangeFlag::IsRename)) LocalAction::RenameLocal(srvm, localitems, change->CiNew);//if rename -> time Mod change -> UpdateLocal
				if (change->Flag.HasFlag(CloudChangeFlag::IsChangeTimeAndSize)) LocalAction::UpdateLocal(srvm, localitems_Parents, change->CiNew);
			}
		}
	}

	void TrackChanges::LocalOnChanged(SyncRootViewModel^ srvm, CustomFileSystemEventArgs^ e)
	{
		PinStr2(pin_fullpath, e->FullPath);
		String^ log = String::Format("TrackChanges::LocalOnChanged ChangeType: {0}, ChangeInfo:{1}, FullPath: {2}", e->ChangeType, e->ChangeInfo, e->FullPath);
		WriteLog(log, 1);
	
		DWORD attrib = GetFileAttributes(pin_fullpath);
		switch (e->ChangeType)
		{
		case WatcherChangeTypes::Changed://attribute,file change,...
		{
			switch (e->ChangeInfo)
			{
			case ChangeInfo::Attribute:
			{
				if (!(attrib & FILE_ATTRIBUTE_DIRECTORY))//file only
				{
					HANDLE hfile = CreateFile(pin_fullpath, FILE_READ_ATTRIBUTES | WRITE_DAC, FILE_SHARE_ALL, nullptr, OPEN_EXISTING, 0, nullptr);
					if (hfile == INVALID_HANDLE_VALUE)
						return;

					CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(hfile);
					if (state == CF_PLACEHOLDER_STATE_INVALID)
						return;

					if ((state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)//placeholder
					{
						LARGE_INTEGER offset = { 0 };
						LARGE_INTEGER length;
						length.QuadPart = MAXLONGLONG;
						if (attrib & FILE_ATTRIBUTE_PINNED)
						{
							CheckHr(CfHydratePlaceholder(hfile, offset, length, CF_HYDRATE_FLAG_NONE, NULL),
								L"TrackChanges::LocalOnChanged CfHydratePlaceholder", pin_fullpath, true);
						}
						else if (attrib & FILE_ATTRIBUTE_UNPINNED)
						{
							CheckHr(CfDehydratePlaceholder(hfile, offset, length, CF_DEHYDRATE_FLAG_BACKGROUND, NULL),
								L"TrackChanges::LocalOnChanged CfDehydratePlaceholder", pin_fullpath, true);
						}
					}
					CloseHandle(hfile);
				}
				break;
			}

			case ChangeInfo::LastWrite:
			{
				if (!(attrib & FILE_ATTRIBUTE_DIRECTORY))//file only
				{
					LocalItem^ li = LocalItem::FindFromPath(srvm, e->FullPath, 0);
					if (li)//file revert li was del
					{
						UploadQueue^ uq = gcnew UploadQueue(srvm, li);
						TaskQueues::UploadQueues->Reset(uq);
					}
				}
				break;
			}
			default:
				break;
			}			
			break;
		}
		case WatcherChangeTypes::Created://new file created
		{
			//if item is placeholder -> skip
			//if item is not placeholder -> upload
			//if can't open for check item -> Queue (for try again later)
			HANDLE hfile = CreateFile(pin_fullpath, FILE_READ_ATTRIBUTES, FILE_SHARE_ALL, nullptr, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, nullptr);
			if (hfile == INVALID_HANDLE_VALUE)
				return;

			CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(hfile);
			if (state == CF_PLACEHOLDER_STATE_INVALID)
				return;

			CloseHandle(hfile);
			if (!(state & CF_PLACEHOLDER_STATE_PLACEHOLDER))//if new item is not placeholder
			{
				FileInfo^ finfo = gcnew FileInfo(e->FullPath);
				String^ parent_fullpath = e->FullPath->Substring(0, e->FullPath->Length - finfo->Name->Length - 1);
				LocalItem^ li_parent = LocalItem::FindFromPath(srvm, parent_fullpath, 0);
				if (!String::IsNullOrEmpty(li_parent->CloudId))
				{
					CloudItem^ ci_parent = CloudItem::Select(li_parent->CloudId, srvm->CEVM->Sqlid);
					if (!ci_parent->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanAddChildren)) return;//can't upload child
				}				

				//upload
				LocalItem^ li_new = LocalItem::Find(srvm, li_parent->LocalId, finfo->Name);
				if (!li_new)
				{
					li_new = gcnew LocalItem();
					li_new->LocalParentId = li_parent->LocalId;
					li_new->Name = finfo->Name;
					li_new->SRId = srvm->SRId;
					if (finfo->Attributes.HasFlag(FileAttributes::Directory)) li_new->Flag = LocalItemFlag::Folder;
					li_new->Insert();
				}

				UploadQueue^ uq = gcnew UploadQueue(srvm, li_new);
				if (finfo->Attributes.HasFlag(FileAttributes::Directory)) uq->IsPrioritize = true;
				TaskQueues::UploadQueues->Add(uq);
			}
			break;
		}
		case WatcherChangeTypes::Deleted:
		{
			//if item is not placeholder delete localitem
			//if item is placeholder -> skip (ConnectSyncRoot NOTIFY_DELETE/NOTIFY_DELETE_COMPLETION trigger)
			LocalItem^ li = LocalItem::FindFromPath(srvm, e->FullPath, 0);
			if (li && String::IsNullOrEmpty(li->CloudId))
			{
				//request cancel upload
				TaskQueues::UploadQueues->Cancel(gcnew UploadQueue(srvm, li));
				li->Delete(true);
			}
			break;
		}
		default: break;
		}
	}
}