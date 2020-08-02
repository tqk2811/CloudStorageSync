using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CssCs
{
  public enum ChangeInfo
  {
    None,
    Attribute,
    LastWrite,
    LastAccess,
    Security
  }
  public class CustomFileSystemEventArgs
  {
    internal CustomFileSystemEventArgs(FileSystemEventArgs e, ChangeInfo ChangeInfo)
    {
      this.FullPath = e.FullPath;
      this.FullPath = e.FullPath;
      this.ChangeType = e.ChangeType;
      this.ChangeInfo = ChangeInfo;
    }
    public string Name { get; }
    public string FullPath { get; }
    public WatcherChangeTypes ChangeType { get; }
    public ChangeInfo ChangeInfo { get; set; }
  }
  public delegate void CustomFileSystemEventHandler(SyncRootViewModel srvm, CustomFileSystemEventArgs e);



  public class Watcher : IDisposable
  {
    CustomFileSystemEventHandler customFileSystemEventHandler;
    FileSystemWatcher watcher_lastwrite;
    FileSystemWatcher watcher_attribute;

    SyncRootViewModel srvm;

    ManualResetEvent resetEvent = new ManualResetEvent(false);

    System.Timers.Timer aTimer;
    Queue<CustomFileSystemEventArgs> FileSystemEventArgsQueue { get; } = new Queue<CustomFileSystemEventArgs>();

    internal Watcher(SyncRootViewModel srvm)
    {
      this.srvm = srvm;
      watcher_attribute = new FileSystemWatcher();
      watcher_attribute.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime |
        NotifyFilters.FileName | NotifyFilters.DirectoryName;//attribute & main
      watcher_attribute.Filter = "*";
      watcher_attribute.IncludeSubdirectories = true;
      watcher_attribute.InternalBufferSize = 16 * 1024;
      watcher_attribute.Changed += Watcher_attribute_event;
      watcher_attribute.Created += Watcher_attribute_event;
      watcher_attribute.Deleted += Watcher_attribute_event;
      watcher_attribute.Error += Watcher_Error;

      watcher_lastwrite = new FileSystemWatcher();
      watcher_lastwrite.NotifyFilter = NotifyFilters.LastWrite /*| NotifyFilters.FileName | NotifyFilters.DirectoryName*/;//LastWrite
      watcher_lastwrite.Filter = "*";
      watcher_lastwrite.IncludeSubdirectories = true;
      watcher_lastwrite.InternalBufferSize = 16 * 1024;
      watcher_lastwrite.Changed += Watcher_lastwrite_Changed;
      watcher_lastwrite.Error += Watcher_Error;
    }

   

    private void Watcher_Error(object sender, ErrorEventArgs e)
    {
      CppInterop.OutPutDebugString("Watcher_Error:" + e.GetException().Message);
    }

    private void Watcher_attribute_event(object sender, FileSystemEventArgs e)
    {
      if(customFileSystemEventHandler != null)
      {
        customFileSystemEventHandler(srvm, new CustomFileSystemEventArgs(e, e.ChangeType == WatcherChangeTypes.Changed ? ChangeInfo.Attribute : ChangeInfo.None));
      }
    }
    private void Watcher_lastwrite_Changed(object sender, FileSystemEventArgs e)
    {
      if (customFileSystemEventHandler != null)
      {
        customFileSystemEventHandler(srvm, new CustomFileSystemEventArgs(e, ChangeInfo.LastWrite));
      }
    }

    public void Dispose()
    {
      aTimer.Dispose();
      resetEvent.Dispose();
      watcher_attribute.Dispose();
      watcher_lastwrite.Dispose();
    }

    void StartThread()
    {
      watcher_attribute.EnableRaisingEvents = true;
      watcher_lastwrite.EnableRaisingEvents = true;
      if (aTimer == null)
      {
        aTimer = new System.Timers.Timer(10000);
        aTimer.Elapsed += OnElapsed;
        aTimer.AutoReset = false;
      }
      aTimer.Start();
      resetEvent.Reset();
      resetEvent.WaitOne();
    }

    void OnElapsed(object source, System.Timers.ElapsedEventArgs e)
    {
      if (customFileSystemEventHandler == null) return;
      List<CustomFileSystemEventArgs> list = new List<CustomFileSystemEventArgs>();
      lock(FileSystemEventArgsQueue)
        while (FileSystemEventArgsQueue.Count > 0) 
          list.Add(FileSystemEventArgsQueue.Dequeue());

      for (int i = 0; i < list.Count; i++)
      {
        try
        {
          customFileSystemEventHandler(srvm, list[i]);
        }
        catch(Exception)
        {

        }
      }
      aTimer.Start();
    }


    public void Start()
    {
      if (!watcher_attribute.EnableRaisingEvents)
      {
        new Thread(StartThread).Start();
      }
    }

    public void Stop()
    {
      if (watcher_attribute.EnableRaisingEvents)
      {
        aTimer.Stop();
        resetEvent.Set();
        watcher_attribute.EnableRaisingEvents = false;
        watcher_lastwrite.EnableRaisingEvents = false;
      }
    }
    
    public void Change(string newpath, CustomFileSystemEventHandler customFileSystemEventHandler)
    {
      Stop();
      watcher_lastwrite.Path = newpath;
      watcher_attribute.Path = newpath;
      this.customFileSystemEventHandler = customFileSystemEventHandler;
    }


    public void AddQueue(CustomFileSystemEventArgs e)
    {
      lock(FileSystemEventArgsQueue)
      {
        FileSystemEventArgsQueue.Enqueue(e);
      }
    }
  }
}