using CssCs.DataClass;
using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCs.Cloud
{
  internal class EmptyCloud : ICloud
  {
    public IList<CloudItem> CloudFolderGetChildFolder(string itemid)
    {
      return new List<CloudItem>();
    }

    public async Task CloudOpenDialogLoadChildFolder(TreeviewCloudItemViewModel item)
    {

    }

    public async Task<CloudItem> CreateFolder(string name, IList<string> ParentIds)
    {
      return new CloudItem();
    }

    public async Task<Stream> Download(string fileid, long? start, long? end)
    {
      return new MemoryStream(new byte[] { 20, 21, 22 });
    }

    public async Task<Quota> GetQuota()
    {
      return new Quota() { Limit = long.MaxValue, Usage = 0 };
    }

    public void ListAllItemsToDb(SyncRootViewModel srvm, string StartFolderId)
    {
      return;
    }

    public Task TrashItem(string Id)
    {
      return Task.CompletedTask;
    }

    public Task UpdateMetadata(UpdateCloudItem updateCloudItem)
    {
      return Task.CompletedTask;
    }

    public async Task<CloudItem> Upload(string FilePath, IList<string> ParentIds, string ItemCloudId = null)
    {
      return null;
    }

    public async Task<IList<CloudChangeType>> WatchChange()
    {
      return new List<CloudChangeType>();
    }

    public bool HashCheck(string filepath, CloudItem ci)
    {
      return false;
    }
  }
}
