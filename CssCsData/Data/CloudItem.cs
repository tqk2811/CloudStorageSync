using System;
using System.Collections.Generic;

namespace CssCsData
{
  public enum CloudItemPermissionFlag : long
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
    OwnedByMe = 1 << 62,
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
    public CloudItemPermissionFlag PermissionFlag { get; set; } = CloudItemPermissionFlag.None;
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



  public enum CloudItemActionFlag : long
  {
    None = 0,
    Delete = 1 << 0,
    Move = 1 << 1,
    Change = 1 << 2,
    Create = 1 << 3
  }
  public interface ICloudItemAction
  {
    string Id { get; }
    string IdAccount { get; }
    CloudItemActionFlag Flag { get; }
  }

  public class CloudItemAction : ICloudItemAction
  {
    internal CloudItemAction()
    {

    }
    public CloudItemAction(CloudItem CloudItemOld, CloudItem CloudItemNew)
    {
      if (null == CloudItemNew && null == CloudItemOld) throw new ArgumentException("old & new can't same null.");
      else if (null != CloudItemOld && null == CloudItemNew)
      {
        Flag |= CloudItemActionFlag.Delete;
        this.Id = CloudItemOld.Id;
        this.IdAccount = CloudItemOld.IdAccount;
      }
      else if (null == CloudItemOld && null != CloudItemNew)
      {
        Flag |= CloudItemActionFlag.Create;
        this.Id = CloudItemNew.Id;
        this.IdAccount = CloudItemNew.IdAccount;
      }
      else
      {
        if (string.IsNullOrEmpty(CloudItemOld.Id) || string.IsNullOrEmpty(CloudItemOld.IdAccount) || !CloudItemOld.Equals(CloudItemNew))
          throw new ArgumentException("Id/IdAccount null or not equal");

        //only root or shared item has ParentId = null
        if (!string.IsNullOrEmpty(CloudItemOld.ParentId) &&
              !CloudItemOld.ParentId.Equals(CloudItemNew.ParentId, StringComparison.OrdinalIgnoreCase))
          Flag |= CloudItemActionFlag.Move;//change parent

        if (CloudItemOld.DateCreate != CloudItemNew.DateCreate ||
            CloudItemOld.DateMod != CloudItemNew.DateMod ||
            CloudItemOld.Size != CloudItemNew.Size) Flag |= CloudItemActionFlag.Change;//change size,time

        //root name can null
        if (!string.IsNullOrEmpty(CloudItemOld.Name) &&
            !CloudItemOld.Name.Equals(CloudItemNew.Name)) Flag |= CloudItemActionFlag.Move;//change name
      }
    }

    /// <summary>
    /// CloudItem Id
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// Account Id
    /// </summary>
    public string IdAccount { get; set; }
    public CloudItemActionFlag Flag { get; set; } = CloudItemActionFlag.None;

    public static IList<CloudItemAction> GetAllInAccount(string IdAccount) => SqliteManaged.CloudItemActionSelect(IdAccount);
    public bool InsertOrUpdate() => this.InsertOrUpdate();
    public bool Delete() => this.Delete();
  }

  public interface ICloudItemActionCollection : ICollection<CloudItemAction>
  {
    //string NewWatchToken { get; }
  }
}
