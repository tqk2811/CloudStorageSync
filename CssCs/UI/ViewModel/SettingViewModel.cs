using CssCsData;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CssCs.UI.ViewModel
{
  public class SettingViewModel : INotifyPropertyChanged, IDisposable
  {
    readonly System.Timers.Timer timer;
    public SettingViewModel()
    {
      timer = new System.Timers.Timer();
      timer.Elapsed += Timer_Elapsed;
      timer.Interval = 1000;
      timer.AutoReset = false;
    }
    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      Setting.SettingData.Update();
    }
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool Disposing)
    {
      timer.Close();
    }
    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      timer.Stop();
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      timer.Start();
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion



    public bool SkipNoticeMalware
    {
      get { return Setting.SettingData.Flag.HasFlag(SettingFlag.SkipNoticeMalware); }
      set
      {
        if (value) Setting.SettingData.Flag |= SettingFlag.SkipNoticeMalware;
        else Setting.SettingData.Flag ^= SettingFlag.SkipNoticeMalware;
        NotifyPropertyChange();
      }
    }
    public bool UploadPrioritizeFirst
    {
      get { return Setting.SettingData.Flag.HasFlag(SettingFlag.UploadPrioritizeFirst); }
      set
      {
        if (value) Setting.SettingData.Flag |= SettingFlag.UploadPrioritizeFirst;
        else Setting.SettingData.Flag ^= SettingFlag.UploadPrioritizeFirst;
        NotifyPropertyChange();
      }
    }
    public bool DownloadPrioritizeFirst
    {
      get { return Setting.SettingData.Flag.HasFlag(SettingFlag.DownloadPrioritizeFirst); }
      set
      {
        if (value) Setting.SettingData.Flag |= SettingFlag.DownloadPrioritizeFirst;
        else Setting.SettingData.Flag ^= SettingFlag.DownloadPrioritizeFirst;
        NotifyPropertyChange();
      }
    }

    public string FileIgnore
    {
      get { return Setting.SettingData.FileIgnore; }
      set { Setting.SettingData.FileIgnore = value; NotifyPropertyChange(); }
    }

    public long TryAgainAfter
    {
      get { return Setting.SettingData.TryAgainAfter; }
      set { Setting.SettingData.TryAgainAfter = value; NotifyPropertyChange(); }
    }

    public long TryAgainTimes
    {
      get { return Setting.SettingData.TryAgainTimes; }
      set { Setting.SettingData.TryAgainTimes = value; NotifyPropertyChange(); }
    }

    public long FilesUploadSameTime
    {
      get { return Setting.SettingData.FilesUploadSameTime; }
      set { Setting.SettingData.FilesUploadSameTime = value; NotifyPropertyChange(); }
    }

    public long SpeedUploadLimit
    {
      get { return Setting.SettingData.SpeedUploadLimit; }
      set { Setting.SettingData.SpeedUploadLimit = value; NotifyPropertyChange(); }
    }

    public long SpeedDownloadLimit
    {
      get { return Setting.SettingData.SpeedDownloadLimit; }
      set { Setting.SettingData.SpeedDownloadLimit = value; NotifyPropertyChange(); }
    }

    public long TimeWatchChangeCloud
    {
      get { return Setting.SettingData.TimeWatchChangeCloud; }
      set { Setting.SettingData.TimeWatchChangeCloud = value; NotifyPropertyChange(); }
    }
  }
}
