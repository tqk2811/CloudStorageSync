using System;
using System.Collections.Generic;

namespace CssCsData
{
  public enum CloudItemFlag : long
  {
    None = 0,

    CanDownload = 1 << 0,
    CanEdit = 1 << 1,
    CanRename = 1 << 2,
    CanShare = 1 << 3,
    CanTrash = 1 << 4,
    CanUntrash = 1 << 5,

    CanAddChildren = 1 << 6,
    CanRemoveChildren = 1 << 7,

    /// <summary>
    /// only google drive can not OwnedByMe
    /// </summary>
    OwnedByMe = 1 << 63,
    All = CanDownload | CanEdit | CanRename | CanShare | CanTrash | CanUntrash | CanAddChildren | CanRemoveChildren |
          OwnedByMe,
  }
  public class CloudItem
  {
    public string Id { get; set; }
    public string IdAccount { get; set; }
    public string Name { get; set; }
    public string ParentId { get; set; }
    public long Size { get; set; }
    public long DateCreate { get; set; }
    public long DateMod { get; set; }
    public CloudItemFlag Flag { get; set; } = CloudItemFlag.None;
    /// <summary>
    /// Hash of file
    /// </summary>
    public string HashString { get; set; }
    /// <summary>
    /// For google drive, this is Id target of application/vnd.google-apps.shortcut
    /// </summary>
    public string Shortcut { get; set; }

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
    public CloudItem GetParent() => SqliteManaged.CloudItemSelect(this.ParentId, this.IdAccount);
    public Account GetAccount()
    {
      lock (Account.Accounts) return Account.Accounts.Find(acc => acc.Id.Equals(this.IdAccount, StringComparison.OrdinalIgnoreCase));
    }

    #region static
    public static CloudItem GetFromId(string Id, string IdAccount) => SqliteManaged.CloudItemSelect(Id, IdAccount);
    public static IList<CloudItem> GetChilds(string IdParent, string IdAccount) => SqliteManaged.CloudItemFindChildIds(IdParent, IdAccount);
    public static void Delete(string Id,string IdAccount) => SqliteManaged.CloudItemDelete(Id, IdAccount);
    #endregion

    public override string ToString()
    {
      return string.Format("Name: {0} , Id: {1}", this.Name, this.Id);
    }
  }
}
