using System;
using System.Collections.Generic;
using System.Linq;

namespace CssCsData
{
  public class SyncRoot
  {
    public SyncRoot(string Id, string IdAccount)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
      if (string.IsNullOrEmpty(IdAccount)) throw new ArgumentNullException(nameof(IdAccount));
      this.Id = Id;
      this.IdAccount = IdAccount;
    }
    public string Id { get; }
    public string IdAccount { get; }
    public string CloudFolderName { get; set; }
    public string CloudFolderId { get; set; }
    public string LocalPath { get; set; }
    public string DisplayName { get; set; }
    public SyncRootFlag Flag { get; set; }
    public SyncRootViewModelBase SyncRootViewModel { get; set; }


    public bool IsAvailableInDb { get; internal set; } = false;



    public void Insert()
    {
      if (IsAvailableInDb) return;
      this.SyncRootInsert();
      lock (SyncRoots)
      {
        SyncRoots.Add(this);
        IsAvailableInDb = true;
      }
    }
    public void Update()
    {
      if(IsAvailableInDb) this.SyncRootUpdate();
    }
    public void Delete()
    {
      if (!IsAvailableInDb) return;
      this.SyncRootDelete();
      lock (SyncRoots)
      {
        SyncRoots.Remove(this);
        IsAvailableInDb = false;
      }
    }

    public override bool Equals(object obj)
    {
      if (obj is SyncRoot syncRoot) return this.Id.Equals(syncRoot.Id, StringComparison.OrdinalIgnoreCase);
      else return base.Equals(obj);
    }
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    Account _account;
    Account GetAccount()
    {
      lock (Account.Accounts) return Account.Accounts.Find(acc => acc.Id.Equals(this.IdAccount, StringComparison.OrdinalIgnoreCase));
    }

    public Account Account 
    { 
      get { return _account ?? GetAccount(); }
    }
    

    #region static
    internal static List<SyncRoot> SyncRoots = new List<SyncRoot>();
    public static SyncRoot FindWithConnectionKey(long key)
    {
      return SyncRoots.Find((sr) => sr.SyncRootViewModel.CheckConnectionKey(key));
    }
    public static List<SyncRoot> GetAll()
    {
      lock (SyncRoots) return SyncRoots.ToList();
    }
    public static List<SyncRoot> GetFromAccount(Account account)
    {
      if (null == account) throw new ArgumentNullException(nameof(account));
      lock (SyncRoots) return SyncRoots.FindAll(sr => sr.IdAccount.Equals(account.Id, StringComparison.OrdinalIgnoreCase));
    }
    public static List<SyncRoot> GetWorkingFromAccount(Account account)
    {
      if (null == account) throw new ArgumentNullException(nameof(account));
      lock (SyncRoots) return SyncRoots.FindAll(  sr => sr.Flag.HasFlag(SyncRootFlag.IsWork) &&
                                                  sr.IdAccount.Equals(account.Id, StringComparison.OrdinalIgnoreCase));
    }
    public static List<SyncRoot> GetWorking()
    {
      lock (SyncRoots) return SyncRoots.FindAll(sr => sr.Flag.HasFlag(SyncRootFlag.IsWork));
    }

    #endregion
  }
}
