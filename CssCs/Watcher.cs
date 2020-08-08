using CssCsData;
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
  public delegate void CustomFileSystemEventHandler(CustomFileSystemEventArgs e);



  public class Watcher : IDisposable
  {
    CustomFileSystemEventHandler customFileSystemEventHandler;
    readonly FileSystemWatcher watcher_lastwrite;
    readonly FileSystemWatcher watcher_attribute;
    readonly SyncRootViewModelBase srvm;
    readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

    System.Timers.Timer aTimer;
    Queue<CustomFileSystemEventArgs> FileSystemEventArgsQueue { get; } = new Queue<CustomFileSystemEventArgs>();

    internal Watcher(SyncRootViewModelBase srvm)
    {
      this.srvm = srvm;
      watcher_attribute = new FileSystemWatcher
      {
        NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime |
        NotifyFilters.FileName | NotifyFilters.DirectoryName,//attribute & main
        Filter = "*",
        IncludeSubdirectories = true,
        InternalBufferSize = 16 * 1024
      };
      watcher_attribute.Changed += Watcher_attribute_event;
      watcher_attribute.Created += Watcher_attribute_event;
      watcher_attribute.Deleted += Watcher_attribute_event;
      watcher_attribute.Error += Watcher_Error;

      watcher_lastwrite = new FileSystemWatcher
      {
        NotifyFilter = NotifyFilters.LastWrite /*| NotifyFilters.FileName | NotifyFilters.DirectoryName*/,//LastWrite
        Filter = "*",
        IncludeSubdirectories = true,
        InternalBufferSize = 16 * 1024
      };
      watcher_lastwrite.Changed += Watcher_lastwrite_Changed;
      watcher_lastwrite.Error += Watcher_Error;
    }

   

    private void Watcher_Error(object sender, ErrorEventArgs e)
    {
      CppInterop.OutPutDebugString("Watcher_Error:" + e.GetException().Message);
    }

    private void Watcher_attribute_event(object sender, FileSystemEventArgs e)
    {
      customFileSystemEventHandler?.Invoke(new CustomFileSystemEventArgs(e, e.ChangeType == WatcherChangeTypes.Changed ? ChangeInfo.Attribute : ChangeInfo.None));
    }
    private void Watcher_lastwrite_Changed(object sender, FileSystemEventArgs e)
    {
      customFileSystemEventHandler?.Invoke(new CustomFileSystemEventArgs(e, ChangeInfo.LastWrite));
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      aTimer.Close();
      resetEvent.Close();
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
          customFileSystemEventHandler(list[i]);
        }
        catch (Exception)
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