using CssCs.DataClass;
using CssCs.Queues;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CssCs
{
  public class Settings : INotifyPropertyChanged
  {
    public static Settings Setting { get; } = new Settings();
    System.Timers.Timer timer;
    internal Settings()
    {
      timer = new System.Timers.Timer();
      timer.Elapsed += Timer_Elapsed;
      timer.Interval = 2000;
      timer.AutoReset = false;
    }

    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      Save();
    }

    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      if (LoadSetting) return;
      timer.Stop();
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      timer.Start();
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    public bool HasInternet { get; set; } = false;


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
      set { _SpeedUploadLimit = value; NotifyPropertyChange(); }
    }

    int _SpeedDownloadLimit = 0;
    public int SpeedDownloadLimit
    {
      get { return _SpeedDownloadLimit; }
      set { _SpeedDownloadLimit = value; NotifyPropertyChange(); }
    }

    public int _TimeWatchChangeCloud = 15;
    public int TimeWatchChangeCloud
    {
      get { return _TimeWatchChangeCloud; }
      set { _TimeWatchChangeCloud = value; NotifyPropertyChange(); }
    }

    public int OauthWait { get; set; } = 5*60000;

    public async Task Save() => SqliteManager.UpdateSetting();

    public void Load() => SqliteManager.SettingSelect();

    internal bool LoadSetting { get; set; } = false;
  }
}
