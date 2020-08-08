using CssCsCloud.Cloud;
using CssCsData;
using CssCsData.Cloud;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CssCs.UI.ViewModel
{
  public class AccountViewModel : AccountViewModelBase
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="account"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AccountViewModel(Account account) : base(account)
    {
      this.Cloud = Oauth.GetCloud(account);
      System.Drawing.Bitmap Img;
      switch (account.CloudName)
      {
        case CloudName.GoogleDrive:
          Img = Properties.Resources.Google_Drive_Icon256x256;
          break;

        case CloudName.OneDrive:
          Img = Properties.Resources.onedrive_logo;
          break;

        default: throw new NotSupportedException();
      }
      this.Img = Img.ToImageSource();
    }
    public System.Windows.Media.ImageSource Img { get; }
    public string Quota
    {
      get { return _Quota; }
      set { _Quota = value; NotifyPropertyChange(); }
    }
    public override ICloud Cloud { get; }


    string _Quota;


    public override void WatchChange()
    {
      Cloud.WatchChange().ContinueWith(WatchChangeResult, TaskScheduler.Default);
    }

    void WatchChangeResult(Task<ICloudChangeTypeCollection> t)
    {
      if (t.IsCanceled || t.IsFaulted) return;
      foreach (var sr in AccountData.GetSyncRootWorking()) 
        foreach (var change in t.Result) 
          sr.SyncRootViewModel.UpdateChange(change);
      AccountData.WatchToken = t.Result.NewWatchToken;
      AccountData.Update();
    }

    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public override event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }
}
