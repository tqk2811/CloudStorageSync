using CssCs.DataClass;
using CssCs.UI.ViewModel;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CssCs.Cloud
{
  public class Quota
  {
    public long? Limit { get; set; }
    public long Usage { get; set; } = 0;
  }
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
    public IList<string> ParentIdsAdd { get; set; }
    /// <summary>
    /// Only Google Drive have multiple parents.
    /// </summary>
    public IList<string> ParentIdsRemove { get; set; }

    internal string NewParentId
    {
      get
      {
        if (ParentIdsAdd == null || ParentIdsAdd.Count == 0) return null;
        else return ParentIdsAdd[0];
      }
    }
  }
  public interface ICloud
  {

    Task<Stream> Download(string fileid, long? start, long? end);
    Task<CloudItem> Upload(string FilePath, IList<string> ParentIds, string ItemCloudId = null);
    Task<CloudItem> CreateFolder(string name, IList<string> ParentIds);

    Task<IList<CloudChangeType>> WatchChange();

    void ListAllItemsToDb(SyncRootViewModel srvm, string StartFolderId);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="itemid"></param>
    /// <returns>IList CloudItem, (only Id and Name)</returns>
    IList<CloudItem> CloudFolderGetChildFolder(string itemid);

    /// <summary>
    /// Update Metadata
    /// </summary>
    /// <returns></returns>
    Task UpdateMetadata(UpdateCloudItem updateCloudItem);

    Task TrashItem(string Id);

    Task<Quota> GetQuota();

    bool HashCheck(string filepath, CloudItem ci);
  }
}
