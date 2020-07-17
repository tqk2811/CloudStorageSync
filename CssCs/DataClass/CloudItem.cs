using CssCs.UI.ViewModel;
using System.Collections.Generic;

namespace CssCs.DataClass
{
  public enum CloudCapabilitiesAndFlag : long
  {
    None = 0,

    CanDownload = 1,
    CanEdit = 1 << 1,
    CanRename = 1 << 2,
    CanShare = 1 << 3,
    CanTrash = 1 << 4,
    CanUntrash = 1 << 5,

    CanAddChildren = 1 << 6,
    CanRemoveChildren = 1 << 7,

    
    OwnedByMe = 1 << 62,
    All = CanDownload | CanEdit | CanRename | CanShare | CanTrash | CanUntrash | CanAddChildren | CanRemoveChildren | 
          OwnedByMe,
  }

  public class CloudItem
  {
    public static CloudItem Select(string Id, SyncRootViewModel srvm) => SqliteManager.CISelect(Id, srvm.CEVM.EmailSqlId);
    public static CloudItem Select(string Id,string IdEmail) => SqliteManager.CISelect(Id, IdEmail);
    public static IList<CloudItem> FindChildIds(string CI_ParentId, string IdEmail) => SqliteManager.CIFindChildIds(CI_ParentId, IdEmail);
    public static void Delete(string Id, string IdEmail) => SqliteManager.CIDelete(Id, IdEmail);

    public CloudItem()
    {
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string IdEmail { get; set; }
    public IList<string> ParentsId { get; set; }
    public long Size { get; set; } = -1;
    public long DateCreate { get; set; } = 0;
    public long DateMod { get; set; } = 0;
    public CloudCapabilitiesAndFlag CapabilitiesAndFlag { get; set; } = CloudCapabilitiesAndFlag.None;
    public string HashString { get; set; }


    public void InsertUpdate() => SqliteManager.CIInsertUpadte(this);
    public void Delete() => SqliteManager.CIDelete(Id, IdEmail);

    public override string ToString()
    {
      return string.Format("Name:\"{0}\", Id:{1}", Name, Id);
    }

    
  }
}
