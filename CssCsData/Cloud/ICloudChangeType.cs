using System.Collections.Generic;

namespace CssCsData.Cloud
{
  public enum CloudChangeFlag : int
  {
    None = 0,
    /// <summary>
    /// rename and change parent
    /// </summary>
    Move = 1 << 0,
    /// <summary>
    /// change time, size, id
    /// </summary>
    Change = 1 << 1,
    /// <summary>
    /// delete
    /// </summary>
    Deleted = 1 << 2,
    /// <summary>
    /// create new
    /// </summary>
    NewItem = 1 << 4,
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
