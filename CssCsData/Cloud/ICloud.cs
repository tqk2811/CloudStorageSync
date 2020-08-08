using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CssCsData.Cloud
{
  public class UpdateCloudItem
  {
    /// <summary>
    /// Id of cloud item.
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// Change the name, otherwise set to null.
    /// </summary>
    public string NewName { get; set; }
    /// <summary>
    /// Change the CreationTime, otherwise set to null.
    /// </summary>
    public DateTime? CreationTime { get; set; }
    /// <summary>
    /// Change the LastWriteTime, otherwise set to null.
    /// </summary>
    public DateTime? LastWriteTime { get; set; }
    /// <summary>
    /// Only Google Drive have multiple parents.
    /// </summary>
    public List<string> ParentIdsAdd { get; } = new List<string>();
    /// <summary>
    /// Only Google Drive have multiple parents.
    /// </summary>
    public List<string> ParentIdsRemove { get; } = new List<string>();

    public string NewParentId
    {
      get
      {
        if (ParentIdsAdd.Count == 0) return null;
        else return ParentIdsAdd[0];
      }
    }
  }
  public class Quota
  {
    public long? Limit { get; set; }
    public long Usage { get; set; } = 0;
  }

  public interface ICloud
  {
    Task<bool> LogOut();

    Task<Stream> Download(CloudItem ci, long posStart, long posEnd);
    Task<CloudItem> Upload(string FilePath, IList<string> ParentIds, string ItemCloudId = null);

    Task<CloudItem> CreateFolder(string name, IList<string> ParentIds);

    Task<ICloudChangeTypeCollection> WatchChange();

    void ListAllItemsToDb(SyncRoot syncRoot, string StartFolderId);

    IList<CloudItem> CloudFolderGetChildFolder(string itemId);

    Task UpdateMetadata(UpdateCloudItem updateCloudItem);

    Task TrashItem(string Id);

    Task<Quota> GetQuota();

    bool HashCheck(string filepath, CloudItem ci);

    Task<CloudItem> GetMetadata(string Id);
  }
}
