using CssCs.Cloud;
using CssCs.DataClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CssCs.UI.ViewModel
{
  public enum CloudName : byte
  {
    GoogleDrive = 0,
    OneDrive = 1,
    MegaNz = 2,
    Dropbox = 3,

    File = 250,
    Folder = 251,
    //Empty = 252,
    None = 255
  }
  public sealed class CloudEmailViewModel : INotifyPropertyChanged
  {
    #region Static function and Property
    public static ObservableCollection<CloudEmailViewModel> CEVMS { get; private set; }
    internal static void Load(IList<CloudEmailViewModel> cevms)
    {
      if(CEVMS == null) CEVMS = new ObservableCollection<CloudEmailViewModel>(cevms);
    }
    public static CloudEmailViewModel Find(string SqlId)
    {
      return CEVMS.ToList().Find((cevm) => cevm.Sqlid.Equals(SqlId));
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Add new
    /// </summary>
    /// <param name="Email"></param>
    /// <param name="cloudName"></param>
    /// <param name="Token"></param>
    /// <exception cref="ArgumentNullException"/>
    internal CloudEmailViewModel(string Email, CloudName cloudName, string Token)
    {
      if (string.IsNullOrEmpty(Email)) throw new ArgumentNullException("Email");
      if (string.IsNullOrEmpty(Token)) throw new ArgumentNullException("Token");
      this.Email = Email;
      this.CloudName = cloudName;
      this.Token = Token;
      this.Sqlid = Extensions.RandomString(32);
      System.Drawing.Bitmap Img;
      switch (cloudName)
      {
        case CloudName.GoogleDrive: 
          Img = Properties.Resources.Google_Drive_Icon256x256;
          Cloud = new CloudGoogleDrive(this);
          break;

        case CloudName.OneDrive: 
          Img = Properties.Resources.onedrive_logo;
          Cloud = new CloudOneDrive(this);
          break;

        case CloudName.MegaNz: 
          Img = Properties.Resources.MegaSync;
          //
          break;

        case CloudName.Dropbox: 
          Img = Properties.Resources.Dropbox256x256;
          //
          break;

        default: throw new Exception("This cloud not support: " + cloudName.ToString());
      }
      this.Img = Img.ToImageSource();
    }
    /// <summary>
    /// Load From DB
    /// </summary>
    /// <param name="Email"></param>
    /// <param name="cloudName"></param>
    /// <param name="Sqlid"></param>
    /// <param name="Token"></param>
    /// <param name="WatchTime"></param>
    /// <param name="WatchToken"></param>
    internal CloudEmailViewModel(string Email, CloudName cloudName, string Token, string Sqlid, string WatchToken):this(Email, cloudName, Token)
    {
      this.Sqlid = Sqlid;
      this.WatchToken = WatchToken;
    }
    #endregion

    #region MVVM
    public string Email { get; }
    public System.Windows.Media.ImageSource Img { get; }
    string _QuotaString;
    public string QuotaString
    {
      get { return _QuotaString; }
      private set { _QuotaString = value; NotifyPropertyChange(); }
    }
    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
    #endregion

    #region Non MVVM Property
    public ICloud Cloud { get; }
    public string Sqlid { get; }
    public CloudName CloudName { get; }
    public string Token { get; internal set; }
    public string WatchToken { get; internal set; }
    public bool IsDeleted { get; internal set; } = false;
    #endregion

    #region Function
    public async Task LoadQuota()
    {
      Quota quota = await Cloud.GetQuota();
      string usage = UnitConventer.ConvertSize(quota.Usage, 2, UnitConventer.unit_size);
      if(quota.Limit == null) QuotaString = usage + "/Unlimited";
      else
      {
        string limit = UnitConventer.ConvertSize(quota.Limit.Value, 2, UnitConventer.unit_size);
        QuotaString = usage + "/" + limit;
      }
    }

    internal void Insert()
    {
      SqliteManager.CEVMInsert(this);
      CEVMS.Add(this);
    }
    public void Update() => SqliteManager.CEVMUpdate(this);
    internal void Delete()
    {
      SqliteManager.CEVMDelete(this);
      IsDeleted = true;
      CEVMS.Remove(this);
    }

    public override bool Equals(object obj)
    {
      bool result = false;
      if(obj is CloudEmailViewModel)
      {
        CloudEmailViewModel cevm = obj as CloudEmailViewModel;
        result = cevm.Sqlid.Equals(Sqlid);
      }
      return result;
    }
    #endregion


  }
}
