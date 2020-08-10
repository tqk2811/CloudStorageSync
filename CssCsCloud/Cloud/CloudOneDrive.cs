using CssCsCloud.CustomStream;
using CssCsData;
using CssCsData.Cloud;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CssCsCloud.Cloud
{
  internal sealed class CloudOneDrive : ICloud
  {    
    static class TokenCacheHelper
    {
      public static void EnableSerialization(ITokenCache tokenCache)
      {
        tokenCache.SetBeforeAccess(BeforeAccessNotification);
        tokenCache.SetAfterAccess(AfterAccessNotification);
      }
      /// <summary>
      /// Path to the token cache. Note that this could be something different for instance for MSIX applications:
      /// private static readonly string CacheFilePath =$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\{AppName}\msalcache.bin";
      /// </summary>
      public static string CacheFilePath;
      private static readonly object FileLock = new object();
      private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
      {
        lock (FileLock)
        {
          args.TokenCache.DeserializeMsalV3(System.IO.File.Exists(CacheFilePath)
                  ? ProtectedData.Unprotect(System.IO.File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser) : null);
        }
      }
      private static void AfterAccessNotification(TokenCacheNotificationArgs args)
      {
        // if the access operation resulted in a cache update
        if (args.HasStateChanged)
        {
          lock (FileLock)
          {
            // reflect changesgs in the persistent store
            System.IO.File.WriteAllBytes(CacheFilePath,
                   ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
          }
        }
      }
    }
    private class UploadSessionResource
    {
      [JsonProperty("uploadUrl")]
      public string UploadUrl { get; set; }

      [JsonProperty("expirationDateTime")]
      public DateTime ExpirationDateTime { get; set; }
    }
    private class TrackChangesResult
    {
      [JsonProperty("value")]
      public IList<DriveItem> DriveItems { get; set; }

      [JsonProperty("@odata.deltaLink")]
      public string DeltaLink { get; set; }

      [JsonProperty("@odata.nextLink")]
      public string NextLink { get; set; }
    }


    const string api_endpoint = "https://graph.microsoft.com/v1.0";
    const string oauth_nativeclient = "https://login.microsoftonline.com/common/oauth2/nativeclient";
    const string create_upload_session = api_endpoint + "/me{0}:/createUploadSession";
    static readonly List<string> Scopes = new List<string>() { "Files.ReadWrite.All", "offline_access" };

    static IPublicClientApplication publicClientApplication;
    static readonly HttpClient httpClient = new HttpClient();
    internal static void Init()
    {
      if (publicClientApplication != null) return;
      TokenCacheHelper.CacheFilePath = DllCloudInit.UWPLocalStatePath + "\\msalcache.bin3";

      publicClientApplication = PublicClientApplicationBuilder.Create(Properties.Resources.OneDriveClientId)
                .WithRedirectUri(oauth_nativeclient)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount)
                .Build();
      TokenCacheHelper.EnableSerialization(publicClientApplication.UserTokenCache);
    }
    internal static Task<AuthenticationResult> Oauth()
    {
      return publicClientApplication.AcquireTokenInteractive(Scopes).ExecuteAsync();
    }
    static async Task<IAccount> GetAccount(string HomeAccountId_Identifier)
    {
      var accounts = await publicClientApplication.GetAccountsAsync().ConfigureAwait(false);
      return accounts.ToList().Find((acc) => acc.HomeAccountId.Identifier.Equals(HomeAccountId_Identifier, StringComparison.OrdinalIgnoreCase));
    }
    static Task<AuthenticationResult> GetAuthenticationResult(IAccount account)
    {
      if (account == null) throw new ArgumentNullException(nameof(account));

      return publicClientApplication.AcquireTokenSilent(Scopes, account).ExecuteAsync();
    }
    static GraphServiceClient GetGraphServiceClient(IAccount account)
    {
      return new GraphServiceClient(api_endpoint,
        new DelegateAuthenticationProvider(async (requestMessage) =>
        {
          var auth_result = await GetAuthenticationResult(account).ConfigureAwait(false);
          requestMessage.Headers.Authorization =
              new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth_result.AccessToken);
        }));
    }


    internal CloudOneDrive(Account account)
    {
      if (account == null) throw new ArgumentNullException(nameof(account));
      if (string.IsNullOrEmpty(account.Token)) throw new ArgumentNullException(nameof(account.Token));
      this.account = account;

      this.msaccount = GetAccount(account.Token).ConfigureAwait(false).GetAwaiter().GetResult();
      graphServiceClient = GetGraphServiceClient(msaccount);
    }

    readonly IAccount msaccount;
    readonly Account account;
    readonly GraphServiceClient graphServiceClient;
    IDriveRequestBuilder MyDrive { get { return graphServiceClient.Me.Drive; } }

    internal CloudItem InsertToDb(DriveItem driveItem)
    {
      bool isfolder = driveItem.Folder != null;
      CloudItem ci = new CloudItem
      {
        Id = driveItem.Id,
        Name = driveItem.Name,
        DateCreate = driveItem.CreatedDateTime.Value.ToUnixTimeSeconds(),
        DateMod = driveItem.LastModifiedDateTime.Value.ToUnixTimeSeconds(),
        ParentIds = new List<string>() { driveItem.ParentReference.Id },
        Size = isfolder ? -1 : driveItem.Size.Value,
        IdAccount = account.Id
      };
      if (!isfolder) ci.HashString = driveItem.File.Hashes.Sha1Hash;
      ci.Flag = CloudItemFlag.All;
      ci.InsertOrUpdate();
      return ci;
    }

    private async Task<ICloudChangeTypeCollection> WatchChange(string UrlWatch)
    {
      CloudChangeTypeCollection result = new CloudChangeTypeCollection();

      var auth_result = await GetAuthenticationResult(msaccount).ConfigureAwait(false);
      TrackChangesResult trackChangesResult;
      using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, UrlWatch))
      {
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth_result.AccessToken);
        HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        if (!responseMessage.IsSuccessStatusCode)
          throw new HttpException((int)responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));

        trackChangesResult = JsonConvert.DeserializeObject<TrackChangesResult>(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
      }
      foreach (var item in trackChangesResult.DriveItems)
      {
        CloudChangeType cloudChangeType;
        CloudItem ci_old = CloudItem.GetFromId(item.Id, account.Id);
        if (item.Deleted != null)
        {
          cloudChangeType = new CloudChangeType(item.Id, null, null);
          cloudChangeType.Flag |= CloudChangeFlag.IsDeleted;
        }
        else
        {
          cloudChangeType = new CloudChangeType(item.Id, ci_old?.ParentIds, new List<string>() { item.ParentReference.Id });
          if (ci_old != null && !item.Name.Equals(ci_old.Name, StringComparison.OrdinalIgnoreCase)) cloudChangeType.Flag |= CloudChangeFlag.IsRename;
          //if (!item.Id.Equals(item.Id)) cloudChangeType.Flag |= CloudChangeFlag.IsChangedId;
          //cloudChangeType.IdNew = cloudChangeType.Flag.HasFlag(CloudChangeFlag.IsChangedId) ? item.Id : null;
          if (ci_old != null &&
            (ci_old.Size != item.Size ||
            ci_old.DateMod != item.LastModifiedDateTime.Value.ToUnixTimeSeconds())) cloudChangeType.Flag |= CloudChangeFlag.IsChangeTimeAndSize;
        }
        cloudChangeType.IdAccount = account.Id;
        if (cloudChangeType.Flag.HasFlag(CloudChangeFlag.IsDeleted)) CloudItem.Delete(item.Id, account.Id);
        else
        {
          //if (cloudChangeType.Flag.HasFlag(CloudChangeFlag.IsChangedId)) CloudItem.Delete(item.Id, cevm.Id);
          cloudChangeType.CiNew = InsertToDb(item);
        }
        result.Add(cloudChangeType);
      }
      if (!string.IsNullOrEmpty(trackChangesResult.DeltaLink)) result.NewWatchToken = trackChangesResult.DeltaLink;
      else if (!string.IsNullOrEmpty(trackChangesResult.NextLink)) result.AddRange(await WatchChange(trackChangesResult.NextLink).ConfigureAwait(false));
      else throw new Exception("Can't find deltaLink/nextLink");
      return result;
    }

    public void _ListAllItemsToDb(SyncRoot syncRoot, string StartFolderId)
    {
      syncRoot.SyncRootViewModel.EnumStatus = SyncRootStatus.ScanningCloud;
      DriveItem di = MyDrive.Items[StartFolderId].Request().GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
      InsertToDb(di);
      List<string> folderIds = new List<string>() { di.Id };
      IDriveItemChildrenCollectionPage childs;
      string expand = "children";
      int reset = 0;
      int total = 0;
      while (folderIds.Count > 0)
      {
        di = MyDrive.Items[folderIds[0]].Request().Expand(expand).GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        folderIds.RemoveAt(0);
        childs = di.Children;
        while (true)
        {
          foreach (var child in childs)
          {
            InsertToDb(child);
            if (child.Folder != null) folderIds.Add(child.Id);
            total++;
            reset++;
            if (syncRoot.SyncRootViewModel.EnumStatus == SyncRootStatus.ScanningCloud)
              syncRoot.SyncRootViewModel.Message = string.Format(CultureInfo.InvariantCulture, "Items Scanned: {0}, Name: {1}", total, child.Name);
          }
          if (childs.NextPageRequest == null) break;
          else childs = di.Children.NextPageRequest.GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        if (reset >= 500)
        {
          reset = 0;
          GC.Collect();
        }
      }
    }


    static string GetLink(IDriveItemDeltaCollectionPage delta, string linkname)
    {
      return delta.AdditionalData.ContainsKey(linkname) ? delta.AdditionalData[linkname] as string : null;
    }


    #region ICloud
    public async Task<bool> LogOut()
    {
      await publicClientApplication.RemoveAsync(msaccount).ConfigureAwait(false);
      return true;
    }

    public async Task<Stream> Download(CloudItem ci, long start, long end)
    {
      if (null == ci || string.IsNullOrEmpty(ci.Id)) throw new ArgumentNullException("ci or ci.Id is null");
      if (start < 0 || end < 0 || end > ci.Size - 1 || start > end) throw new ArgumentException("Start and end is invalid.");

      var driveItemInfo = await MyDrive.Items[ci.Id].Request().GetAsync().ConfigureAwait(false);
      driveItemInfo.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out object downloadUrl);
      using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, (string)downloadUrl))
      {
        requestMessage.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
        var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
          return new ThrottledStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), true);
        }
        else return null;
      }
    }


    /// <summary>
    /// Upload file.
    /// </summary>
    /// <param name="FilePath">Path of file</param>
    /// <param name="ParentIds">Id of parent. For onedrive, only 1 parent.</param>
    /// <param name="ItemCloudId">In onedrive, it not important.</param>
    /// <returns>CloudItem</returns>
    public async Task<CloudItem> Upload(string FilePath, IList<string> ParentIds, string ItemCloudId = null)
    {
      if (string.IsNullOrEmpty(FilePath)) throw new ArgumentNullException(nameof(FilePath));
      if (null == ParentIds || ParentIds.Count == 0 || ParentIds.Count > 1) throw new ArgumentException("ParentIds is invalid");

      FileInfo fi = new FileInfo(FilePath);
      if (fi.Attributes.HasFlag(FileAttributes.Directory)) return await CreateFolder(fi.Name, ParentIds).ConfigureAwait(false);
      else
      {
        DriveItemUploadableProperties driveItemUploadableProperties = new DriveItemUploadableProperties
        {
          Name = fi.Name,
          FileSystemInfo = new Microsoft.Graph.FileSystemInfo()
          {
            ODataType = "microsoft.graph.fileSystemInfo",
            CreatedDateTime = fi.CreationTimeUtc,
            LastModifiedDateTime = fi.LastWriteTimeUtc,
            LastAccessedDateTime = fi.LastAccessTimeUtc
          }
        };

        string uploadrequest_json = JsonConvert.SerializeObject(driveItemUploadableProperties, DllCloudInit.JsonSerializerSettings);
        DriveItem di = await MyDrive.Items[ParentIds[0]].Request().GetAsync().ConfigureAwait(false);
        string itemRelativePath = di.ParentReference.Path + "/" + di.Name + "/" + fi.Name;
        Uri uri = new Uri(string.Format(DllCloudInit.DefaultCulture, create_upload_session, itemRelativePath));

        var auth_result = await GetAuthenticationResult(msaccount).ConfigureAwait(false);
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
        {
          requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth_result.AccessToken);
          requestMessage.Content = new StringContent("{ \"item\":" + uploadrequest_json + "}", Encoding.UTF8, "application/json");

          HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
          if (!responseMessage.IsSuccessStatusCode)
          {
            throw new HttpException((int)responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
          }
          else
          {
            UploadSessionResource uploadSessionResource = JsonConvert.DeserializeObject<UploadSessionResource>(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
            using (ThrottledStream fs = new ThrottledStream(new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite), false))
            {
              long part = fs.Length / DllCloudInit.ChunkUploadDownload;
              if (part * DllCloudInit.ChunkUploadDownload < fs.Length) part++;

              for (int i = 0; i < part; i++)
              {
                long start_offset = DllCloudInit.ChunkUploadDownload * i;
                long end_offset = DllCloudInit.ChunkUploadDownload * (i + 1) - 1;
                if (end_offset > fs.Length) end_offset = fs.Length - 1;
                long contentlength = end_offset - start_offset + 1;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(uploadSessionResource.UploadUrl));
                request.Method = "PUT";
                request.AllowWriteStreamBuffering = false;
                request.ContentType = "application/octet-stream";
                request.KeepAlive = true;
                request.ContentLength = contentlength;
                request.Timeout = System.Threading.Timeout.Infinite;
                request.Headers.Add(HttpRequestHeader.ContentRange, string.Format(DllCloudInit.DefaultCulture, "bytes {0}-{1}/{2}", start_offset, end_offset, fs.Length));
                using (var requestStream = request.GetRequestStream())
                {
                  using (var part_stream = new LimitTransferStream(fs, DllCloudInit.ChunkUploadDownload)) part_stream.CopyTo(requestStream);

                  var response = request.GetResponse();
                  using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                  {
                    string result = sr.ReadToEnd();
                    if (i == part - 1)
                    {
                      DriveItem driveItem = JsonConvert.DeserializeObject<DriveItem>(result);
                      return await GetMetadata(driveItem.Id).ConfigureAwait(false);
                    }
                  }
                }
              }
              throw new Exception("Error transfer");
            }
          }
        }
      }
    }

    public async Task<ICloudChangeTypeCollection> WatchChange()
    {
      if (string.IsNullOrEmpty(account.WatchToken))
      {
        var queryOptions = new List<QueryOption>() { new QueryOption("token", "latest") };
        IDriveItemDeltaCollectionPage delta = await MyDrive.Root.Delta().Request(queryOptions).GetAsync().ConfigureAwait(false);
        account.WatchToken = GetLink(delta, "@odata.deltaLink");
        return new CloudChangeTypeCollection();
      }
      else return await WatchChange(account.WatchToken).ConfigureAwait(false);
    }

    public Task<IList<CloudItem>> CloudFolderGetChildFolder(string itemId)
    {
      if (string.IsNullOrEmpty(itemId)) throw new ArgumentNullException(nameof(itemId));
      return Task.Factory.StartNew<IList<CloudItem>>(() =>
      {
        List<CloudItem> cis = new List<CloudItem>();
        string expand = "children";
        DriveItem di = MyDrive.Items[itemId].Request().Expand(expand).GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        foreach (var child in di.Children) cis.Add(InsertToDb(child));
        return cis;
      }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public Task ListAllItemsToDb(SyncRoot syncRoot, string StartFolderId)
    {
      if (string.IsNullOrEmpty(StartFolderId)) throw new ArgumentNullException(nameof(StartFolderId));
      if (syncRoot == null) throw new ArgumentNullException(nameof(syncRoot));

      return Task.Factory.StartNew(() => _ListAllItemsToDb(syncRoot,StartFolderId),
        syncRoot.SyncRootViewModel.TokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public async Task UpdateMetadata(UpdateCloudItem updateCloudItem)
    {
      if (null == updateCloudItem) throw new ArgumentNullException(nameof(updateCloudItem));

      DriveItem di = new DriveItem()
      {
        Id = updateCloudItem.Id,
        Name = updateCloudItem.NewName,
        CreatedDateTime = updateCloudItem.CreationTime,
        LastModifiedDateTime = updateCloudItem.LastWriteTime
      };
      if (!string.IsNullOrEmpty(updateCloudItem.NewParentId)) di.ParentReference = new ItemReference() { Id = updateCloudItem.NewParentId };
      await MyDrive.Items[updateCloudItem.Id].Request().UpdateAsync(di).ConfigureAwait(false);
    }

    public Task TrashItem(string Id)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));

      return MyDrive.Items[Id].Request().DeleteAsync();
    }

    public async Task<CloudItem> CreateFolder(string name, IList<string> ParentIds)
    {
      if (null == ParentIds || ParentIds.Count == 0 || ParentIds.Count > 1) throw new ArgumentException("ParentIds is invalid");
      var driveItem = new DriveItem
      {
        Name = name,
        Folder = new Microsoft.Graph.Folder { }
      };
      DriveItem di = await MyDrive.Items[ParentIds[0]].Children.Request().AddAsync(driveItem).ConfigureAwait(false);
      return InsertToDb(di);
    }

    public async Task<CssCsData.Cloud.Quota> GetQuota()
    {
      Drive drive = await MyDrive.Request().GetAsync().ConfigureAwait(false);
      CssCsData.Cloud.Quota quota = new CssCsData.Cloud.Quota
      {
        Usage = drive.Quota.Used.Value,
        Limit = drive.Quota.Total
      };
      return quota;
    }

    public bool HashCheck(string filepath, CloudItem ci)
    {
      if (null != ci && System.IO.File.Exists(filepath))
      {
        FileInfo finfo = new FileInfo(filepath);
        if (finfo.Length == ci.Size && !string.IsNullOrEmpty(ci.HashString))
        {
          using (SHA1 sha1 = SHA1.Create())
          {
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
              byte[] hash = sha1.ComputeHash(fs);
              string hashstring = BitConverter.ToString(hash).Replace("-", "");
              return ci.HashString.Equals(hashstring, StringComparison.OrdinalIgnoreCase);
            }
          }
        }
      }
      return false;
    }

    public async Task<CloudItem> GetMetadata(string Id)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));

      Task<DriveItem> task_di;
      if (Id.Equals("root", StringComparison.OrdinalIgnoreCase)) task_di = MyDrive.Root.Request().GetAsync();
      else task_di = MyDrive.Items[Id].Request().GetAsync();
      return InsertToDb(await task_di.ConfigureAwait(false));
    }
    #endregion
  }
}
