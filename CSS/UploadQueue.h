#pragma once
namespace CSS
{
	ref class UploadQueue : CssCs::Queues::IQueue
	{
	private:
		SyncRootViewModel^ srvm;
		String^ itemPath;
		String^ parentCloudId;
		CancellationTokenSource^ source;
		CancellationToken token;
		bool _IsPrioritize = false;
		int tryAgainCount = 0;

		void Work()
		{
			if ((int)srvm->CEVM->CloudName > 200) return;
			PinStr(itemPath);
			if (PathExists(itemPath)) return;
			try
			{
				String^ parentPath = Path::GetDirectoryName(itemPath);
				PinStr(parentPath);
				CF_PLACEHOLDER_STATE parent_state = Placeholders::GetPlaceholderState(pin_parentPath);
				if (parent_state == CF_PLACEHOLDER_STATE::CF_PLACEHOLDER_STATE_INVALID ||
					!(parent_state & CF_PLACEHOLDER_STATE::CF_PLACEHOLDER_STATE_PLACEHOLDER))
				{
					//not placeholder -> try again
					TryAgain();
					return;
				}
				
				MY_CF_PLACEHOLDER_STANDARD_INFO placeholder_parent_info{ 0 };
				if (!Placeholders::GetPlaceholderStandarInfo(pin_parentPath, &placeholder_parent_info))
				{
					//can't read
					return;
				}
				String^ parent_CloudId = gcnew String(placeholder_parent_info.FileIdentity);

				CloudItem^ ci_parent = CloudItem::Select(parent_CloudId,this->srvm->CEVM->EmailSqlId);
				if (!ci_parent || !ci_parent->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanAddChildren))
				{
					//folder can't add child (because user not have permission)
					return;
				}

				CF_PLACEHOLDER_STATE parent_state = Placeholders::GetPlaceholderState(pin_itemPath);
				if (parent_state != CF_PLACEHOLDER_STATE_INVALID && (parent_state & CF_PLACEHOLDER_STATE_IN_SYNC))
				{
					//in sync state -> cancel upload
					return;
				}

				bool isNewUpload = parent_state == CF_PLACEHOLDER_STATE_INVALID ||
					!(parent_state & CF_PLACEHOLDER_STATE_PLACEHOLDER);
				String^ itemCloudId = nullptr;
				if (!isNewUpload)
				{
					MY_CF_PLACEHOLDER_STANDARD_INFO placeholder_item_info{ 0 };
					if (!Placeholders::GetPlaceholderStandarInfo(pin_itemPath, &placeholder_item_info))
					{
						//can't read info for upload revision
						return;
					}
					itemCloudId = gcnew String(placeholder_item_info.FileIdentity);
				}
				
				List<String^>^ parents = gcnew List<String^>();
				parents->Add(parent_CloudId);
				CloudItem^ ci_back = this->srvm->CEVM->Cloud->Upload(itemPath, parents, itemCloudId)->Result;
				if (ci_back)
				{
					if (isNewUpload) Placeholders::Convert(srvm, pin_itemPath, ci_back->Id);
					else Placeholders::Update(srvm, pin_itemPath, ci_back);
				}
				else
				{
					//error
				}
			}
			catch (AggregateException^ ae)
			{
				IOException^ ioe = safe_cast<IOException^>(ae->InnerException);
				if (ioe)
				{
					HRESULT hresult = (HRESULT)ioe->HResult;
					if (hresult == 0x80070020)//can't open file because other process opening (not share read) -> re-queue
					{
						TryAgain();
						return;
					}
				}
				WriteLog(String::Format("UploadQueue.DoWork: Exception, path:{0}, Message:{0}", itemPath, ae->InnerException->Message));
			}
			catch (Exception^ ex)
			{
				WriteLog(String::Format("UploadQueue.DoWork: Exception, path:{0}, Message:{1}, StackTrace:{2}", itemPath, ex->Message, ex->StackTrace), 1);
			}
		}

		void TryAgain()
		{
			Task::Delay(CssCs::Settings::Setting->TryAgainAfter * 1000)
				->ContinueWith(gcnew Action<Task^>(this, &UploadQueue::TryAgain));
		}

		void TryAgain(Task^ task)
		{
			CssCs::Queues::TaskQueues::UploadQueues->Add(this);
		}

	public:
		UploadQueue(SyncRootViewModel^ srvm, String^ itemPath)
		{
			if (!srvm) throw gcnew ArgumentNullException(gcnew String("srvm"));

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
			if (uq && uq->itemPath->Equals(this->itemPath)) return true;
			else return false;
		}

		virtual void Cancel()
		{
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