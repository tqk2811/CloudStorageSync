using CssCsCloud.Cloud;
using CssCsData;
using CssCsData.Cloud;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
      Extensions.WriteLogIfError(GetQuota(), string.Format(CultureInfo.InvariantCulture, "AccountViewModel(id:{0}).GetQuota", account.Id));
    }
    public System.Windows.Media.ImageSource Img { get; }
    public string Quota
    {
      get { return _Quota; }
      set { _Quota = value; NotifyPropertyChange(); }
    }
    public override ICloud Cloud { get; }


    string _Quota;


    public override Task WatchChange()
    {
      return Cloud.WatchChange().ContinueWith(WatchChangeResult, TaskScheduler.Default);
    }

    void WatchChangeResult(Task<ICloudItemActionCollection> t)
    {
      if (t.IsCanceled) return;
      if(t.IsFaulted)
      {
        CppInterop.OutPutDebugString("WatchChangeResult Exception:" + t.Exception.Message + ", StackTrace:" + t.Exception.StackTrace, 0);
        return;
      }
      foreach (var change in t.Result)
      {
        bool flag = true;
        foreach (var sr in AccountData.GetSyncRootWorking())
        {
          try
          {
            if (!sr.SyncRootViewModel.UpdateChange(change)) flag = false;
          }
          catch (Exception ex)
          {
            CppInterop.OutPutDebugString(string.Format("SyncRootViewModel.UpdateChange CloudItemId:{0}, Syncroot Id:{1}, Exception:{2}, StackTrace:{3}",
              change.Id, sr.Id, ex.Message, ex.StackTrace), 0);
          }
        }
        if(flag)
        {
          change.Delete();
        }
      }
    }

    public async Task GetQuota()
    {
      Quota quota = await Cloud.GetQuota().ConfigureAwait(false);
      string usage = UnitConventer.ConvertSize(quota.Usage, 2, UnitConventer.UnitSize);
      if (quota.Limit == null) this.Quota = usage + "/Unlimited";
      else
      {
        string limit = UnitConventer.ConvertSize(quota.Limit.Value, 2, UnitConventer.UnitSize);
        this.Quota = usage + "/" + limit;
      }
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
