using CssCs.DataClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CssCs.UI.ViewModel
{
  public enum SyncRootStatus : byte
  {
    NotWorking = 0,           //00000000
    ScanningCloud = 1,        //00000001
    RegisteringSyncRoot = 2,  //00000010
    ScanningLocal = 4,        //00000100
    CreatingPlaceholder = 8,  //00001000
    Working = 16,             //00010000
    Error = 128               //10000000
  }
  public sealed class SyncRootViewModel : INotifyPropertyChanged
  {
    #region Static Function
    static List<SyncRootViewModel> SRVMS;
    internal static void Load(IList<SyncRootViewModel> srvms)
    {
      if (SRVMS == null && srvms != null) SRVMS = new List<SyncRootViewModel>(srvms);
      else throw new Exception("SyncRootViewModel Load Failed");
    }

    public static SyncRootViewModel FindWithConnectionKey(long ConnectionKey)
    {
      return SRVMS.Find((srvm) => srvm.IsWork && srvm.ConnectionKey == ConnectionKey);
    }

    public static List<SyncRootViewModel> FindAllWorking(CloudEmailViewModel cevm = null)
    {
      if(cevm != null) return SRVMS.FindAll((srvm) => srvm.IsWork && srvm.CEVM.Equals(cevm));
      else return SRVMS.FindAll((srvm) => srvm.IsWork);
    }
    public static List<SyncRootViewModel> Find(CloudEmailViewModel cevm)
    {
      return SRVMS.FindAll((srvm) => srvm.CEVM.Equals(cevm));
    }
    public static SyncRootViewModel Find(string SrId)
    {
      return SRVMS.Find((srvm) => srvm.SRId.Equals(SrId));
    }
    public static SyncRootViewModel FindFromWatcher(Watcher obj)
    {
      return SRVMS.Find((srvm) => srvm.Watcher == obj);
    }
    #endregion

    #region Constructor
    /// <summary>
    /// for create new
    /// </summary>
    internal SyncRootViewModel(CloudEmailViewModel cevm, string SrId = null)
    {
      this.CEVM = cevm;
      Watcher = new Watcher(this);
      if (string.IsNullOrEmpty(SrId)) this.SRId = Extensions.RandomString(32);
      else this.SRId = SrId;
    }

    /// <summary>
    /// load from db
    /// </summary>
    internal SyncRootViewModel(
      CloudEmailViewModel cevm, 
      string SrId, 
      string CloudFolderName,
      string CloudFolderId,
      string LocalPath,
      bool iswork,
      bool isListAll) : this(cevm, SrId)
    {
      _CloudFolderName = CloudFolderName;
      this.CloudFolderId = CloudFolderId;
      _LocalPath = LocalPath;
      _IsWork = iswork;
      this.IsListedAll = isListAll;
    }
    #endregion

    #region Non MVVM Property
    public CloudEmailViewModel CEVM { get; }
    public string SRId { get; }
    public string CloudFolderId { get; internal set; }
    public bool IsListedAll { get; set; } = false;
#endregion

#region MVVM
    string _CloudFolderName = string.Empty;
    public string CloudFolderName
    {
      get { return _CloudFolderName; }
      set { IsListedAll = false; _CloudFolderName = value; NotifyPropertyChange(); }
    }

    bool _IsWork = false;
    public bool IsWork
    {
      get { return _IsWork; }
      set
      {
        if (value && (string.IsNullOrEmpty(CloudFolderName) || string.IsNullOrEmpty(LocalPath))) return;
        if (value && TaskRun != null && !TaskRun.IsCompleted) return;
        if(!value)
        {
          MessageBoxResult result = MessageBox.Show("Are you sure to un-register this syncroot?", "Confim", MessageBoxButton.YesNo);
          if (result == MessageBoxResult.No) return;
        }
        _IsWork = value;
        NotifyPropertyChange();
        Run();
      }
    }

    string _LocalPath = string.Empty;
    public string LocalPath
    {
      get { return _LocalPath; }
      set { _LocalPath = value; NotifyPropertyChange(); }
    }

    string _StatusString = "Not Working";
    public string StatusString
    {
      get { return _StatusString; }
      set { _StatusString = value; NotifyPropertyChange(); }
    }

    string _Message = string.Empty;
    public string Message
    {
      get { return _Message; }
      set { _Message = value; NotifyPropertyChange(); }
    }
#region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler PropertyChanged;
#endregion
#endregion

    internal void Insert()
    {
      SqliteManager.SRVMInsert(this);
      SRVMS.Add(this);
    }
    public void Update()
    {
      SqliteManager.SRVMUpdate(this);
    }
    internal void Delete()
    {
      SqliteManager.SRVMDelete(this);
      SRVMS.Remove(this);
    }

#region SyncRoot
    void UpdateStatus()
    {
      if ((_Status & SyncRootStatus.Error) == SyncRootStatus.Error)
      {
        StatusString = "Error";
      }
      else switch (_Status)
        {
          case SyncRootStatus.NotWorking: StatusString = "Not Working"; break;
          case SyncRootStatus.ScanningCloud: StatusString = "Scanning Cloud"; break;
          case SyncRootStatus.RegisteringSyncRoot: StatusString = "Registering Syncroot"; break;
          case SyncRootStatus.ScanningLocal: StatusString = "Scanning Local"; break;
          case SyncRootStatus.CreatingPlaceholder: StatusString = "Creating Placeholder"; break;
          case SyncRootStatus.Working: StatusString = "Working"; break;
        }
    }
    SyncRootStatus _Status = SyncRootStatus.NotWorking;
    public SyncRootStatus Status
    {
      get { return _Status; }
      set
      {
        _Status = value;
        UpdateStatus();
      }
    }
    

    public Task TaskRun { get; private set; }

    public void Run()
    {
      if (IsWork)
      {
        if (!IsListedAll)
        {
          Status = SyncRootStatus.ScanningCloud;
          TaskRun = Task.Factory.StartNew(
            () => { CEVM.Cloud.ListAllItemsToDb(this, CloudFolderId); },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

          TaskRun.ContinueWith(
            (Task t) =>
            {
              if (t.IsFaulted || t.IsCanceled) return;
              else
              {
                GC.Collect();
                Message = string.Empty;
                IsListedAll = true;
                Update();
                this.Register();
              }              
            });
        }
        else Register();
      }
      else Unregister();
    }

    void Register()
    {
      TaskRun = Task.Factory.StartNew(() => CPPCLR_Callback.SRRegister(this));
    }
    void Unregister()
    {
      if (TaskRun != null && !TaskRun.IsCompleted) TaskRun.Wait();
      CPPCLR_Callback.SRUnRegister(this);
    }

    public long ConnectionKey { get; set; } = 0;
    public Watcher Watcher { get; }

#endregion


  }
}
