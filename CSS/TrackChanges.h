#pragma once
namespace CSS
{
	ref class TrackChanges
	{
	public:
		static void InitTimer();
		static void UnInitTimer();
		void LocalOnChanged(SyncRootViewModel^ srvm, CustomFileSystemEventArgs^ e);
		static void OnElapsed(Object^ source, System::Timers::ElapsedEventArgs^ e);
	private:
		void WatchChangeResult(Task<IList<CloudChangeType^>^>^ t, Object^ obj);
		void UpdateChange(CloudChangeType^ change, SyncRootViewModel^ srvm);
		static System::Timers::Timer^ aTimer;
		static ManualResetEvent^ resetevent;
	};
	extern gcroot<TrackChanges^> trackchanges;
}
