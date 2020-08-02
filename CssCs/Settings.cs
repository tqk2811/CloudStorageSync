using CssCs.DataClass;
using CssCs.Queues;
using CssCs.StreamLimit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CssCs
{
  public class Settings : INotifyPropertyChanged, IDisposable
  {
    public static Settings Setting { get; } = new Settings();
    public const int ChunkUploadDownload = 50 * 1024 * 1024;//50Mib
    public const int OauthWait = 5 * 60000;
    public const int AutoSaveTime = 1000;




    public bool HasInternet { get; set; } = false;
    System.Timers.Timer timer;
    internal Settings()
    {
      timer = new System.Timers.Timer();
      timer.Elapsed += Timer_Elapsed;
      timer.Interval = AutoSaveTime;
      timer.AutoReset = false;
    }
    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      SqliteManager.UpdateSetting();
    }

    internal bool LoadSetting { get; set; } = false;
    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      if (LoadSetting) return;
      timer.Stop();
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      timer.Start();
    }

    public void Dispose()
    {
      timer.Dispose();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    



    bool _SkipNoticeMalware = false;
    public bool SkipNoticeMalware
    {
      get { return _SkipNoticeMalware; }
      set { _SkipNoticeMalware = value; NotifyPropertyChange(); }
    }


    string _FileIgnore = "desktop.ini;";
    public string FileIgnore
    {
      get { return _FileIgnore; }
      set { _FileIgnore = value; NotifyPropertyChange(); }
    }


    int _TryAgainAfter = 5;
    public int TryAgainAfter
    {
      get { return _TryAgainAfter; }
      set { _TryAgainAfter = value; NotifyPropertyChange(); }
    }


    int _TryAgainTimes = 3;
    public int TryAgainTimes
    {
      get { return _TryAgainTimes; }
      set { _TryAgainTimes = value; NotifyPropertyChange(); }
    }


    int _filesUploadSameTime = 1;
    public int FilesUploadSameTime
    {
      get { return _filesUploadSameTime; }
      set
      {
        _filesUploadSameTime = value; 
        NotifyPropertyChange();
        TaskQueues.UploadQueues.MaxRun = value;
      }
    }


    int _SpeedUploadLimit = 0;
    public int SpeedUploadLimit
    {
      get { return _SpeedUploadLimit; }
      set
      {
        _SpeedUploadLimit = value;
        SpeedUploadLimitByte = value * 1024;
        NotifyPropertyChange();
        ThrottledStream.Up.LimitChange();
      }
    }
    internal int SpeedUploadLimitByte { get; private set; } = 0;

    bool _uploadPrioritizeFirst = true;
    public bool UploadPrioritizeFirst
    {
      get { return _uploadPrioritizeFirst; }
      set { _uploadPrioritizeFirst = value; NotifyPropertyChange(); ThrottledStream.Up.PrioritizeFirst = value; }
    }


    int _SpeedDownloadLimit = 0;
    public int SpeedDownloadLimit
    {
      get { return _SpeedDownloadLimit; }
      set
      {
        _SpeedDownloadLimit = value;
        SpeedDownloadLimitByte = value * 1024;
        NotifyPropertyChange();
        ThrottledStream.Down.LimitChange();
      }
    }
    internal int SpeedDownloadLimitByte { get; private set; } = 0;

    bool _downloadPrioritizeFirst = true;
    public bool DownloadPrioritizeFirst
    {
      get { return _downloadPrioritizeFirst; }
      set { _downloadPrioritizeFirst = value; NotifyPropertyChange(); ThrottledStream.Down.PrioritizeFirst = value; }
    }


    public int _TimeWatchChangeCloud = 15;
    public int TimeWatchChangeCloud
    {
      get { return _TimeWatchChangeCloud; }
      set { _TimeWatchChangeCloud = value; NotifyPropertyChange(); }
    }
  }
}
