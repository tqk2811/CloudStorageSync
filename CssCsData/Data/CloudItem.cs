using System;
using System.Collections.Generic;

namespace CssCsData
{
  public class CloudItem
  {
    public string Id { get; set; }
    public string IdAccount { get; set; }
    public string Name { get; set; }
    public IList<string> ParentIds
    {
      get { return ParentsString.StringSplit(); }
      set { ParentsString = value.MakeSplitString(); }
    }    
    public long Size { get; set; }
    public long DateCreate { get; set; }
    public long DateMod { get; set; }
    public CloudItemFlag Flag { get; set; }
    public string HashString { get; set; }

    internal string ParentsString { get; set; }


    public override bool Equals(object obj)
    {
      if (obj is CloudItem cloudItem) return this.Id.Equals(cloudItem.Id) && this.IdAccount.Equals(cloudItem.IdAccount);
      return base.Equals(obj);
    }
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }


    public void InsertOrUpdate() => this.CloudItemInsertUpadte();
    public void Delete() => SqliteManaged.CloudItemDelete(Id, IdAccount);


    public IList<CloudItem> GetChilds() => SqliteManaged.CloudItemFindChildIds(this.Id, this.IdAccount);
    public Account GetAccount()
    {
      lock (Account.Accounts) return Account.Accounts.Find(acc => acc.Id.Equals(this.IdAccount, StringComparison.OrdinalIgnoreCase));
    }
    #region static
    public static CloudItem GetFromId(string Id, string IdAccount) => SqliteManaged.CloudItemSelect(Id, IdAccount);
    public static IList<CloudItem> GetChilds(string IdParent, string IdAccount) => SqliteManaged.CloudItemFindChildIds(IdParent, IdAccount);
    public static void Delete(string Id,string IdAccount) => SqliteManaged.CloudItemDelete(Id, IdAccount);
    #endregion
  }
}
