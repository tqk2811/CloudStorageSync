using CssCs.Cloud;
using CssCs.DataClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CssCs.UI.ViewModel
{
  public class CustomCollectionInherited<T> : ObservableCollection<T>
  {
    public CustomCollectionInherited(IList<T> items):base(items)
    {
      
    }
    protected override void MoveItem(int oldIndex, int newIndex)
    {
      throw new NotImplementedException();
    }
    protected override void InsertItem(int index, T item)
    {
      throw new NotImplementedException();
    }
    protected override void RemoveItem(int index)
    {
      throw new NotImplementedException();
    }

    public void MyInsert(T item)
    {
      base.InsertItem(base.Count, item);
    }

    public void MyRemove(T item)
    {
      int index = base.IndexOf(item);
      if(index >= 0) base.RemoveItem(index);
    }
  }


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
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
  public class CloudEmailViewModel : INotifyPropertyChanged
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
  {
    #region Static function and Property
    public static CustomCollectionInherited<CloudEmailViewModel> CEVMS { get; private set; }
    internal static void Load(IList<CloudEmailViewModel> cevms)
    {
      if (CEVMS == null) CEVMS = new CustomCollectionInherited<CloudEmailViewModel>(cevms);
    }
    public static CloudEmailViewModel Find(string SqlId)
    {
      return CEVMS.ToList().Find((cevm) => cevm.EmailSqlId.Equals(SqlId));
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
    internal CloudEmailViewModel(string Email, CloudName CloudName, string Token) : this(Email, CloudName, Token, null, null)
    {
      
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
    internal CloudEmailViewModel(string Email, CloudName CloudName, string Token, string EmailSqlId, string WatchToken)
    {
      if (string.IsNullOrEmpty(Email)) throw new ArgumentNullException(nameof(Email));
      if (string.IsNullOrEmpty(Token)) throw new ArgumentNullException(nameof(Token));
      bool flagnew = false;
      if (string.IsNullOrEmpty(EmailSqlId))
      {
        this.EmailSqlId = Extensions.RandomString(32);
        flagnew = true;
      }
      else this.EmailSqlId = EmailSqlId;


      this.Email = Email;
      this.CloudName = CloudName;
      this._Token = Token;
      this._WatchToken = WatchToken;

      System.Drawing.Bitmap Img;
      switch (CloudName)
      {
        case CloudName.GoogleDrive:
          Img = Properties.Resources.Google_Drive_Icon256x256;
          Cloud = new CloudGoogleDrive(this);
          break;

        case CloudName.OneDrive:
          Img = Properties.Resources.onedrive_logo;
          Cloud = new CloudOneDrive(this);
          break;

        default: throw new Exception("This cloud not support: " + CloudName.ToString());
      }
      this.Img = Img.ToImageSource();
      
      if (flagnew)
      {
        SqliteManager.CEVMInsert(this);
        CEVMS.MyInsert(this);
        _ = LoadQuota();
        Cloud.WatchChange();
      }
    }
    #endregion

    #region MVVM
    public string Email { get; }
    public System.Windows.Media.ImageSource Img { get; private set; }
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
    public readonly ICloud Cloud;
    public readonly string EmailSqlId;
    public readonly CloudName CloudName;

    string _Token;
    public string Token
    {
      get { return _Token; }
      set { _Token = value; Save(); }
    }

    string _WatchToken;
    public string WatchToken
    {
      get { return _WatchToken; }
      set { _WatchToken = value; Save(); }
    }

    public bool IsDeleted { get; private set; } = false;
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

    internal bool Delete()
    {
      if (IsDeleted || SyncRootViewModel.FindAll(this).Count > 0) return false;

      SqliteManager.CEVMDelete(this);
      IsDeleted = true;
      CEVMS.MyRemove(this);
      return IsDeleted;
    }

    public override bool Equals(object obj)
    {
      bool result = false;
      if(obj is CloudEmailViewModel)
      {
        CloudEmailViewModel cevm = obj as CloudEmailViewModel;
        result = cevm.EmailSqlId.Equals(EmailSqlId);
      }
      return result;
    }

    void Save()
    {
      if (!IsDeleted) SqliteManager.CEVMUpdate(this);
    }
    #endregion
  }
}
