using System.Collections.Generic;

namespace CssCsData.Cloud
{
  public enum CloudChangeFlag : int
  {
    None = 0,
    ChangeTimeAndSize = 1 << 0,
    Deleted = 1 << 1,
    ChangedId = 1 << 2,
    Rename = (1 << 3) | ChangeTimeAndSize,
    NewItem = 1 << 4,
    ChangedParent = 1 << 5
  }
  public interface ICloudChangeType
  {
    CloudChangeFlag Flag { get; }
    CloudItem CloudItemNew { get; }
    CloudItem CloudItemOld { get; }
  }

  public interface ICloudChangeTypeCollection : ICollection<ICloudChangeType>
  {
    string NewWatchToken { get; }
  }
}
