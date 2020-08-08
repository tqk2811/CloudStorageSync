#pragma once
namespace CSS
{
	ref class UploadQueue : CssCs::Queues::IQueue
	{
	private:
		SyncRootViewModel^ srvm;
		LocalItem^ li;
		bool _IsPrioritize = false;
		CancellationTokenSource^ source;
		CancellationToken token;
		int tryAgainCount = 0;

		void Work()
		{
			Account^ account = this->srvm->SyncRootData->Account;
			if ((int)account->CloudName > 200) return;
			String^ fullpath = String::Empty;

			try
			{
				fullpath = li->GetFullPath()->ToString();
				if (!PathExists(fullpath)) return;
				LocalItem^ li_parent = li->Parent;
				if (!li_parent)//deleted
				{
					LogWriter::WriteLog(L"CSS::UploadQueue Can't find parent_li", 0);
					return;
				}
				else if (String::IsNullOrEmpty(li_parent->CloudId))//parent not have id -> TryAgain (wait parent created in cloud)
				{
					TryAgain(fullpath);
				}
				else
				{
					CloudItem^ ci_parent = account->GetCloudItem(li_parent->CloudId);
					if (ci_parent  && !ci_parent->Flag.HasFlag(CloudItemFlag::CanAddChildren))
					{
						WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: Upload cancel because cloud folder hasn't permision for add child, path:{0}", fullpath), 0);
						return;//folder can't add child (because user not have permission)
					}
				}

				bool isNewUpload = String::IsNullOrEmpty(li->CloudId);
				if (!isNewUpload)
				{
					CloudItem^ ci = account->GetCloudItem(li->CloudId);
					if (srvm->CEVM->Cloud->HashCheck(fullpath, ci))
					{
						WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: Upload cancel because same hash, path:{0}", fullpath), 0);
						return;//if upload revision -> check hash before upload -> if hash equal > skip
					}
				}
				WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: Starting Upload, path:{0}", fullpath), 0);
				List<String^>^ parents = gcnew List<String^>();
				parents->Add(parent_li->CloudId);
				CloudItem^ ci_back = srvm->CEVM->Cloud->Upload(fullpath, parents, li->CloudId)->Result;
				if (ci_back)
				{
					WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: success, path:{0}, Id:{1}", fullpath, ci_back->Id), 0);
					li->CloudId = ci_back->Id;
					if (isNewUpload) li->AddFlagWithLock(LocalItemFlag::LockWaitUpdateFromCloudWatch);//CSS::Placeholders::CreateItem will release it (only for new item)
					li->Update();
					if (isNewUpload) Placeholders::Convert(srvm, li, ci_back->Id);
					else Placeholders::Update(srvm, li, ci_back);
				}
				else
				{
					WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: Failed, CloudItemTransfer.Upload return null, path:{0}", fullpath), 0);
				}
			}
			catch (AggregateException^ ae)
			{
				IOException^ io_ex = safe_cast<IOException^>(ae->InnerException);
				if (io_ex)
				{
					HRESULT hr = (HRESULT)io_ex->HResult;
					if (hr == 0x80070020) TryAgain(fullpath);
				}
				WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue::DoWork: Exception, Message:{0}", ae->InnerException->Message),0);
			}
			catch (Exception^ ex)
			{
				WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue::DoWork: Exception, path:{0}, Message:{1}, StackTrace:{2}", fullpath, ex->Message, ex->StackTrace), 0);
			}
		}

		void TryAgain(String^ fullpath)
		{
			tryAgainCount++;
			if (tryAgainCount > CssCsData::Setting::SettingData->TryAgainTimes)
			{
				WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: Upload cancel because cloud folder hasn't id, path:{0}", fullpath), 0);
				return;
			}
			Task::Delay(CssCsData::Setting::SettingData->TryAgainAfter * 1000)
				->ContinueWith(gcnew Action<Task^>(this, &UploadQueue::TryAgain));
			WriteLog(String::Format(CultureInfo::InvariantCulture, "CSS::UploadQueue: Upload TryAgain because cloud folder hasn't id, path:{0}", fullpath), 0);
		}

		void TryAgain(Task^ task)
		{
			CssCs::Queues::TaskQueues::UploadQueues->Add(this);
		}

	public:
		UploadQueue(SyncRootViewModel^ srvm, LocalItem^ li)
		{
			if (!srvm) throw gcnew ArgumentNullException(gcnew String("srvm"));
			if (!li) throw gcnew ArgumentNullException(gcnew String("li"));

			this->srvm = srvm;
			this->li = li;
			source = gcnew CancellationTokenSource();
			token = source->Token;
		}

		~UploadQueue()
		{
			delete source;
		}


		virtual Task^ DoWork()
		{
			return Task::Factory->StartNew(
				gcnew Action(this, &UploadQueue::Work),
				token,
				TaskCreationOptions::LongRunning,
				TaskScheduler::Default);
		}

		virtual bool Check(IQueue^ queue)
		{
			UploadQueue^ uq = safe_cast<UploadQueue^>(queue);
			if (uq && uq->li->LocalId == this->li->LocalId) return true;
			return false;
		}

		virtual void Cancel()
		{
			WriteLog(String::Format(CultureInfo::InvariantCulture, "UploadQueue: canceling, filepath:{0}", this->li->GetFullPath()), 1);
			source->Cancel();
		}

		virtual property bool IsPrioritize
		{
			bool get()
			{
				return _IsPrioritize;
			}

			void set(bool val)
			{
				_IsPrioritize = val;
			}
		}
	};
}