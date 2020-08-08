using CssCsCloud.Cloud;
using CssCsData;
using CssCsData.Cloud;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public override event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }
}
