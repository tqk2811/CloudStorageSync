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

		void Work();

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
			if (uq && uq->li->Equals(this->li)) return true;
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