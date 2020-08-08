#pragma once
namespace CSS
{
	ref class TrackChanges
	{
	public:
		static void InitTimer();
		static void UnInitTimer();
	private:
		static void OnElapsed(Object^ source, System::Timers::ElapsedEventArgs^ e);
		static System::Timers::Timer^ aTimer;
		static ManualResetEvent^ resetevent;


	public:
		void LocalOnChanged(SyncRootViewModel^ srvm, CustomFileSystemEventArgs^ e);		
	private:
		void WatchChangeResult(Task<ICollection<ICloudChangeType^>^>^ t, Object^ obj);
		void UpdateChange(ICloudChangeType^ change, SyncRootViewModel^ srvm);
	};
	extern gcroot<TrackChanges^> trackchanges;
}
