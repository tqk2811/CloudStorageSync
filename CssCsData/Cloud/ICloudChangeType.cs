using System.Collections.Generic;

namespace CssCsData.Cloud
{
  public enum CloudChangeFlag : byte
  {
    None = 0,
    IsChangeTimeAndSize = 1,
    IsDeleted = 1 << 1,
    IsChangedId = 1 << 2,
    IsRename = 1 << 3,
    IsNewItem = 1 << 4
  }
  public interface ICloudChangeType
  {
    CloudChangeFlag Flag { get; }
    bool IsChangeParent { get; }
    string IdAccount { get; }
    string Id { get; }
    string IdNew { get; }
    IList<string> ParentsRemove { get; }
    IList<string> ParentsNew { get; } 
    IList<string> ParentsCurrent { get; }
    CloudItem CiNew { get; }
  }
}
