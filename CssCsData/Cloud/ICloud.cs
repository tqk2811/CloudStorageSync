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
    /// Change the name, otherwise null (default).
    /// </summary>
    public string NewName { get; set; }
    /// <summary>
    /// Change the CreationTime, otherwise null (default).
    /// </summary>
    public DateTime? CreationTime { get; set; }
    /// <summary>
    /// Change the LastWriteTime, otherwise null (default).
    /// </summary>
    public DateTime? LastWriteTime { get; set; }
    /// <summary>
    /// Change parent, otherwise null (default).
    /// </summary>
    public string NewParentId { get; set; }
  }
  public class Quota
  {
    public long? Limit { get; set; }
    public long Usage { get; set; } = 0;
  }

  public interface ICloud
  {
    Task<bool> LogOut();

    /// <summary>
    /// Download file.
    /// </summary>
    /// <param name="Id">Id of item download.</param>
    /// <param name="posStart"></param>
    /// <param name="posEnd"></param>
    /// <returns></returns>
    Task<Stream> Download(string Id, long posStart, long posEnd);
    /// <summary>
    /// Upload new file or upload new revision of file or create folder.
    /// </summary>
    /// <param name="FilePath">Path of file for upload.</param>
    /// <param name="ParentId">Parent id, that folder will container.</param>
    /// <param name="ItemCloudId">For upload new revision, this is file id.</param>
    /// <returns></returns>
    Task<CloudItem> Upload(string FilePath, string ParentId, string ItemCloudId = null);
    /// <summary>
    /// Create Folder.
    /// </summary>
    /// <param name="Name">Name of folder will create.</param>
    /// <param name="ParentId">Parent id, that folder will container.</param>
    /// <returns></returns>
    Task<CloudItem> CreateFolder(string Name, string ParentId);
    /// <summary>
    /// Read change of cloud.
    /// </summary>
    /// <returns></returns>
    Task<ICloudItemActionCollection> WatchChange();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="syncRoot"></param>
    /// <param name="StartFolderId"></param>
    /// <returns></returns>
    Task ListAllItemsToDb(SyncRoot syncRoot, string StartFolderId);

    Task<IList<CloudItem>> ListChildsFolderOfFolder(string Id);
    /// <summary>
    /// Update change (name, time create, time modify, change parent) to cloud.
    /// </summary>
    /// <param name="updateCloudItem"></param>
    /// <returns></returns>
    Task UpdateMetadata(UpdateCloudItem updateCloudItem);
    /// <summary>
    /// Move item to trash
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    Task TrashItem(string Id);
    /// <summary>
    /// Read Quota
    /// </summary>
    /// <returns></returns>
    Task<Quota> GetQuota();
    /// <summary>
    /// Check hash of file
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="ci"></param>
    /// <returns></returns>
    bool HashCheck(string filePath, CloudItem ci);
    /// <summary>
    /// Get new CloudItem and update to Db
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    Task<CloudItem> GetMetadata(string Id);
  }
}
