using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CssCsData
{
  public class Account
  {
    public Account(string Id,string Email,CloudName cloudName)
    {
      if(string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
      if (string.IsNullOrEmpty(Email)) throw new ArgumentNullException(nameof(Email));
      this.Id = Id;
      this.Email = Email;
      this.CloudName = cloudName;
    }
    public string Id { get; }
    public string Email { get; set; }
    public CloudName CloudName { get; set; }
    public string Token { get; set; }
    public string WatchToken { get; set; }


    public bool IsAvailableInDb { get; internal set; } = false;
    public AccountViewModelBase AccountViewModel { get; internal set; }


    public void Insert()
    {
      if (IsAvailableInDb) return;
      this.AccountInsert();//may exception
      lock (Accounts)
      {
        Accounts.Add(this);
        IsAvailableInDb = true;
      }
    }
    public void Update()
    {
      if(IsAvailableInDb) this.AccountUpdate();
    }
    public void Delete()
    {
      if (!IsAvailableInDb) return;
      this.AccountDelete();
      lock (Accounts)
      {
        Accounts.Remove(this);
        IsAvailableInDb = false;
      }
    }
    public void ClearSyncRoot()
    {
      this.SyncRootClear();
      lock(SyncRoot.SyncRoots) SyncRoot.SyncRoots.RemoveAll(sr => sr.IdAccount.Equals(this.Id, StringComparison.OrdinalIgnoreCase));
    }
    public List<SyncRoot> GetSyncRoot() => SyncRoot.GetFromAccount(this);
    public List<SyncRoot> GetSyncRootWorking() => SyncRoot.GetWorkingFromAccount(this);
    public CloudItem GetCloudItem(string Id) => CloudItem.GetFromId(Id, this.Id);


    public override bool Equals(object obj)
    {
      if (obj is Account account) return this.Id.Equals(account.Id, StringComparison.OrdinalIgnoreCase);
      else return base.Equals(obj);
    }
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    #region static
    internal static List<Account> Accounts = new List<Account>();
    public static List<Account> GetAll()
    {
      lock (Accounts) return Accounts.ToList();
    }
    #endregion
  }
}
