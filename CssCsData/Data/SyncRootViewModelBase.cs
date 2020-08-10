using CssCsData.Cloud;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CssCsData
{
  public enum SyncRootStatus : int
  {
    NotWorking = 0,
    ScanningCloud = 1,
    RegisteringSyncRoot = 2,
    ScanningLocal = 3,
    CreatingPlaceholder = 4,
    Working = 5,
    Error = 7 << 31           //10000000
  }

  public abstract class SyncRootViewModelBase : INotifyPropertyChanged
  {
    public SyncRootViewModelBase(SyncRoot syncRoot)
    {
      if (null == syncRoot) throw new ArgumentNullException(nameof(syncRoot));
      this.SyncRootData = syncRoot;
      TokenSource = new CancellationTokenSource();
    }
    public SyncRoot SyncRootData { get; }
    public virtual bool IsEditingDisplayName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual string CloudFolderName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual bool IsWork { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual bool IsListed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual string LocalPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual string Status { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual string Message { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual string DisplayName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual SyncRootStatus EnumStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public virtual LocalItemRoot Root => throw new NotImplementedException();
    public virtual event PropertyChangedEventHandler PropertyChanged;
    public CancellationTokenSource TokenSource { get; }
    public abstract void UpdateChange(ICloudChangeType change);
  }
}
