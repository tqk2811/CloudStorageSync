using CssCs.DataClass;
using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Web;
using System.Threading;

namespace CssCs.Cloud
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
    class UploadSessionResource
    {
      public string uploadUrl { get; set; }
      public DateTime expirationDateTime { get; set; }
    }
    class TrackChangesResult
    {
      [JsonProperty("value")]
      public IList<DriveItem> DriveItems { get; set; }

      [JsonProperty("@odata.deltaLink")]
      public string deltaLink { get; set; }

      [JsonProperty("@odata.nextLink")]
      public string nextLink { get; set; }
    }


    const string api_endpoint = "https://graph.microsoft.com/v1.0";
    const string oauth_nativeclient = "https://login.microsoftonline.com/common/oauth2/nativeclient";
    const string create_upload_session = api_endpoint + "/me{0}:/createUploadSession";
    static readonly List<string> Scopes = new List<string>() { "Files.ReadWrite.All", "offline_access" };

    static IPublicClientApplication publicClientApplication;
    static HttpClient httpClient = new HttpClient();
    internal static void Init()
    {
      if (publicClientApplication != null) return;
      TokenCacheHelper.CacheFilePath = CPPCLR_Callback.UWPLocalStatePath + "\\msalcache.bin3";

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
    static Task<AuthenticationResult> GetAuthenticationResult(string HomeAccountId_Identifier)
    {
      var accounts = publicClientApplication.GetAccountsAsync().Result;
      var account = accounts.ToList().Find((acc) => acc.HomeAccountId.Identifier.Equals(HomeAccountId_Identifier));
      if (account == null) throw new Exception("Can't find account");
      return publicClientApplication.AcquireTokenSilent(Scopes, account).ExecuteAsync();
    }


    CloudEmailViewModel cevm;
    GraphServiceClient graphServiceClient;
    IDriveRequestBuilder MyDrive { get { return graphServiceClient.Me.Drive; } }



    internal CloudOneDrive(CloudEmailViewModel cevm)
    {
      if (cevm == null) throw new ArgumentNullException("cevm");
      if (string.IsNullOrEmpty(cevm.Token)) throw new NullReferenceException("cevm.Token");
      this.cevm = cevm;
      graphServiceClient = GetGraphServiceClient(cevm.Token);
    }


    GraphServiceClient GetGraphServiceClient(string HomeAccountId_Identifier)
    {
      return new GraphServiceClient(api_endpoint,
        new DelegateAuthenticationProvider(async (requestMessage) =>
      {
        var auth_result = await GetAuthenticationResult(HomeAccountId_Identifier);
        requestMessage.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth_result.AccessToken);
      }));
    }


    internal CloudItem InsertToDb(DriveItem driveItem)
    {
      bool isfolder = driveItem.Folder != null;
      CloudItem ci = new CloudItem();
      ci.Id = driveItem.Id;
      ci.Name = driveItem.Name;
      ci.DateCreate = driveItem.CreatedDateTime.Value.ToUnixTimeSeconds();
      ci.DateMod = driveItem.LastModifiedDateTime.Value.ToUnixTimeSeconds();
      ci.ParentsId = new List<string>() { driveItem.ParentReference.Id };
      ci.Size = isfolder ? -1 : driveItem.Size.Value;
      ci.IdEmail = cevm.Sqlid;
      if (!isfolder) ci.HashString = driveItem.File.Hashes.Sha1Hash;
      ci.CapabilitiesAndFlag = CloudCapabilitiesAndFlag.All;
      ci.InsertUpdate();
      return ci;
    }

    internal Task<DriveItem> GetMetadata(string fileid)
    {
      return MyDrive.Items[fileid].Request().GetAsync();
    }

    private async Task<IList<CloudChangeType>> WatchChange(string UrlWatch)
    {
      List<CloudChangeType> list = new List<CloudChangeType>();

      var auth_result = await GetAuthenticationResult(cevm.Token);
      HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, UrlWatch);
      requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth_result.AccessToken);
      HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
      if (responseMessage.IsSuccessStatusCode)
      {
        TrackChangesResult result = JsonConvert.DeserializeObject<TrackChangesResult>(await responseMessage.Content.ReadAsStringAsync());
        foreach (var item in result.DriveItems)
        {
          CloudChangeType cloudChangeType;
          CloudItem ci_old = CloudItem.Select(item.Id, cevm.Sqlid);
          if (item.Deleted != null)
          {
            cloudChangeType = new CloudChangeType(item.Id, null, null);
            cloudChangeType.Flag |= CloudChangeFlag.IsDeleted;
          }
          else
          {
            cloudChangeType = new CloudChangeType(item.Id, ci_old == null ? null : ci_old.ParentsId, new List<string>() { item.ParentReference.Id });
            if (ci_old == null ? false : !item.Name.Equals(ci_old.Name)) cloudChangeType.Flag |= CloudChangeFlag.IsRename;
            //if (!item.Id.Equals(item.Id)) cloudChangeType.Flag |= CloudChangeFlag.IsChangedId;
            //cloudChangeType.IdNew = cloudChangeType.Flag.HasFlag(CloudChangeFlag.IsChangedId) ? item.Id : null;
            if (ci_old != null &&
              (ci_old.Size != item.Size ||
              ci_old.DateMod != item.LastModifiedDateTime.Value.ToUnixTimeSeconds())) cloudChangeType.Flag |= CloudChangeFlag.IsChangeTimeAndSize;
          }
          cloudChangeType.CEId = cevm.Sqlid;
          if (cloudChangeType.Flag.HasFlag(CloudChangeFlag.IsDeleted)) CloudItem.Delete(item.Id, cevm.Sqlid);
          else
          {
            //if (cloudChangeType.Flag.HasFlag(CloudChangeFlag.IsChangedId)) CloudItem.Delete(item.Id, cevm.Id);
            cloudChangeType.CiNew = InsertToDb(item);
          }
          list.Add(cloudChangeType);
        }

        if (!string.IsNullOrEmpty(result.deltaLink)) cevm.WatchToken = result.deltaLink;
        else if (!string.IsNullOrEmpty(result.nextLink)) list.AddRange(await WatchChange(result.nextLink));
        else throw new Exception("Can't find deltaLink/nextLink");
        return list;
      }
      else throw new HttpException((int)responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
    }

    string GetLink(IDriveItemDeltaCollectionPage delta, string linkname)
    {
      return delta.AdditionalData.ContainsKey(linkname) ? delta.AdditionalData[linkname] as string : null;
    }




    #region ICloud
    public Task<Stream> Download(string fileid, long? start, long? end)
    {
      if (start != null && end != null)
      {
        HeaderOption ho = new HeaderOption("Range", start.Value.ToString() + "-" + end.Value.ToString());
        var request = MyDrive.Items[fileid].Content.Request();
        request.Headers.Add(ho);
        return request.GetAsync();
      }
      else return MyDrive.Items[fileid].Content.Request().GetAsync();
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
      if (null == ParentIds || ParentIds.Count == 0 || ParentIds.Count > 1) throw new ArgumentException("ParentIds is invalid");

      FileInfo fi = new FileInfo(FilePath);
      if (fi.Attributes.HasFlag(FileAttributes.Directory)) return await CreateFolder(fi.Name, ParentIds);
      else
      {
        DriveItemUploadableProperties driveItemUploadableProperties = new DriveItemUploadableProperties();
        driveItemUploadableProperties.Name = fi.Name;
        driveItemUploadableProperties.FileSystemInfo = new Microsoft.Graph.FileSystemInfo()
        {
          ODataType = "microsoft.graph.fileSystemInfo",
          CreatedDateTime = fi.CreationTimeUtc,
          LastModifiedDateTime = fi.LastWriteTimeUtc,
          LastAccessedDateTime = fi.LastAccessTimeUtc
        };


        //UploadSession us = await MyDrive.Items[ParentIds[0]].CreateUploadSession(driveItemUploadableProperties).Request().PostAsync();
        //using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
        //{
        //  HttpWebRequest request = (HttpWebRequest)WebRequest.Create(us.UploadUrl);
        //  request.Method = "PUT";
        //  request.AllowWriteStreamBuffering = false;
        //  request.ContentType = "application/octet-stream";
        //  request.ContentLength = fs.Length;
        //  using (var requestStream = request.GetRequestStream()) fs.CopyTo(requestStream);
        //  var response = request.GetResponse();
        //  StreamReader sr = new StreamReader(response.GetResponseStream());
        //  string result = sr.ReadToEnd();
        //  DriveItem driveItem = JsonConvert.DeserializeObject<DriveItem>(result);
        //  driveItem = await GetMetadata(driveItem.Id);
        //  return InsertToDb(driveItem);
        //}

        string uploadrequest_json = JsonConvert.SerializeObject(driveItemUploadableProperties, JsonSetting.jsonSerializerSettings);
        DriveItem di = await MyDrive.Items[ParentIds[0]].Request().GetAsync();
        string itemRelativePath = di.ParentReference.Path + "/" + di.Name + "/" + fi.Name;
        string url = string.Format(create_upload_session, itemRelativePath);

        var auth_result = await GetAuthenticationResult(cevm.Token);
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth_result.AccessToken);
        requestMessage.Content = new StringContent("{ \"item\":" + uploadrequest_json + "}", Encoding.UTF8, "application/json");

        HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);
        if (!responseMessage.IsSuccessStatusCode)
        {
          throw new HttpException((int)responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
        }
        else
        {
          UploadSessionResource uploadSessionResource = JsonConvert.DeserializeObject<UploadSessionResource>(await responseMessage.Content.ReadAsStringAsync());
          using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
          {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uploadSessionResource.uploadUrl);
            request.Method = "PUT";
            request.AllowWriteStreamBuffering = false;
            request.ContentType = "application/octet-stream";
            request.ContentLength = fs.Length;
            using (var requestStream = request.GetRequestStream()) fs.CopyTo(requestStream);
            var response = request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            string result = sr.ReadToEnd();
            DriveItem driveItem = JsonConvert.DeserializeObject<DriveItem>(result);
            driveItem = await GetMetadata(driveItem.Id);
            return InsertToDb(driveItem);
          }
        }
      }
    }


    public async Task<IList<CloudChangeType>> WatchChange()
    {
      if (string.IsNullOrEmpty(cevm.WatchToken))
      {
        var queryOptions = new List<QueryOption>() { new QueryOption("token", "latest") };
        IDriveItemDeltaCollectionPage delta = await MyDrive.Root.Delta().Request(queryOptions).GetAsync();
        cevm.WatchToken = GetLink(delta, "@odata.deltaLink");
        return new List<CloudChangeType>();
      }
      else return await WatchChange(cevm.WatchToken);
    }

    public IList<CloudItem> CloudFolderGetChildFolder(string itemid)
    {
      List<CloudItem> cis = new List<CloudItem>();
      string expand = "children";
      DriveItem di;
      if (string.IsNullOrEmpty(itemid) || itemid.Equals("root"))
      {
        di = MyDrive.Root.Request().Expand(expand).GetAsync().Result;
      }
      else
      {
        di = MyDrive.Items[itemid].Request().Expand(expand).GetAsync().Result;
      }
      foreach (var child in di.Children) cis.Add(InsertToDb(child));
      return cis;
    }

    public void ListAllItemsToDb(SyncRootViewModel srvm, string StartFolderId)
    {
      DriveItem di = MyDrive.Items[StartFolderId].Request().GetAsync().Result;
      InsertToDb(di);
      List<string> folderIds = new List<string>() { StartFolderId };
      IDriveItemChildrenCollectionPage childs;
      string expand = "children";
      int reset = 0;
      int total = 0;
      while (folderIds.Count > 0)
      {
        if (!srvm.IsWork) return;

        di = MyDrive.Items[folderIds[0]].Request().Expand(expand).GetAsync().Result;
        folderIds.RemoveAt(0);
        childs = di.Children;
        while (true)
        {
          if (!srvm.IsWork) return;//stop
          foreach (var child in childs)
          {
            InsertToDb(child);
            if (child.Folder != null) folderIds.Add(child.Id);
            total++;
            reset++;
            if (srvm.Status == SyncRootStatus.ScanningCloud) srvm.Message = string.Format("Items Scanned: {0}, Name: {1}", total, child.Name);
          }
          if (childs.NextPageRequest == null) break;
          else childs = di.Children.NextPageRequest.GetAsync().Result;
        }
        if (reset >= 500)
        {
          reset = 0;
          GC.Collect();
        }
      }
    }

    public async Task UpdateMetadata(UpdateCloudItem updateCloudItem)
    {
      DriveItem di = new DriveItem()
      {
        Id = updateCloudItem.Id,
        Name = updateCloudItem.NewName,
        CreatedDateTime = updateCloudItem.CreationTime,
        LastModifiedDateTime = updateCloudItem.LastWriteTime
      };
      if (!string.IsNullOrEmpty(updateCloudItem.NewParentId)) di.ParentReference = new ItemReference() { Id = updateCloudItem.NewParentId };
      await MyDrive.Items[updateCloudItem.Id].Request().UpdateAsync(di);
    }

    public Task TrashItem(string Id)
    {
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
      DriveItem di = await MyDrive.Items[ParentIds[0]].Children.Request().AddAsync(driveItem);
      return InsertToDb(di);
    }

    public async Task<Quota> GetQuota()
    {
      Drive drive = await MyDrive.Request().GetAsync();
      Quota quota = new Quota();
      quota.Usage = drive.Quota.Used.Value;
      quota.Limit = drive.Quota.Total;
      return quota;
    }

    public bool HashCheck(string filepath, CloudItem ci)
    {
      if (ci != null && System.IO.File.Exists(filepath))
      {
        FileInfo finfo = new FileInfo(filepath);
        if (finfo.Length == ci.Size) return true;
        {
          using(SHA1 sha1 = SHA1.Create())
          {
            using(FileStream fs = new FileStream(filepath,FileMode.Open,FileAccess.Read,FileShare.Read))
            {

            }
          }
        }
      }
      return false;
    }
    #endregion
  }
}
