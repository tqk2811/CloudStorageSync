using CssCsCloud.CustomStream;
using CssCsData;
using CssCsData.Cloud;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections;
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

namespace CssCsCloud.Cloud
{
  internal sealed class CloudGoogleDrive : ICloud
  {
    const string OrderBy = "folder,name,createdTime";

    const string Fields_DriveFile = "id,name,mimeType,trashed,parents,webContentLink,hasThumbnail,thumbnailLink,createdTime,modifiedTime,ownedByMe,capabilities,md5Checksum,size,shortcutDetails";
    const string Fields_DriveFileList = "nextPageToken,incompleteSearch,files(" + Fields_DriveFile + ")";
    const string Fields_WatchChange = "nextPageToken,newStartPageToken,changes(type,changeType,time,removed,fileId,file(" + Fields_DriveFile + "))";

    const string MimeType_Folder = "application/vnd.google-apps.folder";
    const string MimeType_Shortcut = "application/vnd.google-apps.shortcut";
    const string MimeType_googleapp = "application/vnd.google-apps.";

    const string Query_ListFolder = "'{0}' in parents and mimeType = '" + MimeType_Folder + "' and trashed = false";
    const string Query_List = "'{0}' in parents and trashed = false";

    const string DownloadUri = "https://www.googleapis.com/drive/v3/files/{0}?alt=media";
    const string UploadUri = "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable";
    const string UploadRevisionUri = "https://www.googleapis.com/upload/drive/v3/files/{0}?uploadType=resumable";

    readonly Account account;
    readonly UserCredential user;
    readonly DriveService ds;

    internal CloudGoogleDrive(Account account)
    {
      if (account == null) throw new ArgumentNullException(nameof(account));
      if (string.IsNullOrEmpty(account.Token)) throw new NullReferenceException("cevm.Token");
      this.account = account;
      user = Oauth2(account.Token).ConfigureAwait(false).GetAwaiter().GetResult();
      ds = GetDriveService(user);
    }


    internal static async Task<UserCredential> Oauth2(string user = null)
    {
      ClientSecrets clientSecrets = new ClientSecrets()
      {
        ClientId = Properties.Resources.GoogleOauth2_ClientID,
        ClientSecret = Properties.Resources.GoogleOauth2_Clientsecret
      };
      if (string.IsNullOrEmpty(user))
      {
#if DEBUG        
        user = "T3Qe6goFLCQAUZrY0dgfPQThOzxdNf0L";
#else
        user = Extensions.RandomString(32);
#endif
      }
      using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DllCloudInit.OauthWait))
      {
        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            new[] { DriveService.Scope.Drive },
            user,
            cancellationTokenSource.Token,
#if DEBUG
          new FileDataStore("D:\\GDToken"/*CPPCLR_Callback.UWPLocalStatePath + "\\GoogleDriveToken"*/, true)).ConfigureAwait(false);
#else
          new FileDataStore(DllInit.UWPLocalStatePath + "\\GoogleDriveToken",true));
#endif
      }
    }

    internal static DriveService GetDriveService(UserCredential user)
    {
      if (null == user) throw new ArgumentNullException(nameof(user));

      return new DriveService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = user,
        ApplicationName = Properties.Resources.ApplicationName,
        ApiKey = Properties.Resources.GoogleDriveApiKey
      });
    }

    CloudItem InsertToDb(Google.Apis.Drive.v3.Data.File drivefile)
    {
      if (drivefile == null) return null;

      bool isFolder = drivefile.MimeType.Equals(MimeType_Folder, StringComparison.OrdinalIgnoreCase);
      //bool isShortcut = drivefile.MimeType.Equals(MimeType_Shortcut, StringComparison.OrdinalIgnoreCase);
      CloudItem ci = new CloudItem
      {
        Id = drivefile.Id,
        IdAccount = account.Id,
        Name = drivefile.Name,
        DateCreate = drivefile.CreatedTime.Value.GetUnixTimeSeconds(),
        DateMod = drivefile.ModifiedTime.Value.GetUnixTimeSeconds()
      };
      ci.ParentId = drivefile?.Parents[0];//goolge api will change after sep 2020
      ci.Size = isFolder ? -1 : drivefile.Size.Value;
      ci.HashString = drivefile.Md5Checksum;
      if (drivefile.Capabilities.CanDownload == true) ci.Flag |= CloudItemFlag.CanDownload;
      if (drivefile.Capabilities.CanEdit == true) ci.Flag |= CloudItemFlag.CanEdit;
      if (drivefile.Capabilities.CanRename == true) ci.Flag |= CloudItemFlag.CanRename;
      if (drivefile.Capabilities.CanShare == true) ci.Flag |= CloudItemFlag.CanShare;
      if (drivefile.Capabilities.CanTrash == true) ci.Flag |= CloudItemFlag.CanTrash;
      if (drivefile.Capabilities.CanUntrash == true) ci.Flag |= CloudItemFlag.CanUntrash;
      if (drivefile.Capabilities.CanAddChildren == true) ci.Flag |= CloudItemFlag.CanAddChildren;
      if (drivefile.Capabilities.CanRemoveChildren == true) ci.Flag |= CloudItemFlag.CanRemoveChildren;
      //if (isShortcut)
      //{
      //  ci.Flag |= CloudItemFlag.Shortcut;
      //  ci.IdTargetOfShortcut = drivefile.ShortcutDetails.TargetId;
      //}
      if (drivefile.OwnedByMe == true) ci.Flag |= CloudItemFlag.OwnedByMe;
      ci.InsertOrUpdate();
      return ci;
    }

    async Task InitWatch()
    {
      var change = await ds.Changes.GetStartPageToken().ExecuteAsync().ConfigureAwait(false);
      account.WatchToken = change.StartPageTokenValue;
    }

    async Task<ICloudChangeTypeCollection> WatchChange(string WatchToken)
    {
      if (string.IsNullOrEmpty(WatchToken)) throw new ArgumentNullException(nameof(WatchToken));

      CloudChangeTypeCollection result = new CloudChangeTypeCollection();
      var change_request = ds.Changes.List(WatchToken);
      change_request.Fields = Fields_WatchChange;
      change_request.PageSize = 1000;
      change_request.RestrictToMyDrive = true;
      var change_list = await change_request.ExecuteAsync().ConfigureAwait(false);
      foreach (var change in change_list.Changes)
      {
        if (!change.ChangeType.Equals("file", StringComparison.OrdinalIgnoreCase)) continue;
        if (change.Removed == false && //not delete
          !change.File.MimeType.Equals(MimeType_Folder, StringComparison.OrdinalIgnoreCase) &&//not folder
          //!change.File.MimeType.Equals(MimeType_Shortcut, StringComparison.OrdinalIgnoreCase) &&//not shortcut
          change.File.MimeType.IndexOf(MimeType_googleapp, StringComparison.OrdinalIgnoreCase) >= 0) continue;//ignore google app (& shortcut)

        CloudItem ci_old = CloudItem.GetFromId(change.FileId, account.Id);
        CloudItem ci_new = null;
        if (change.Removed == true || change.File.Trashed == true)
        {
          if (null == ci_old) continue;//old & new can't null
        }
        else ci_new = InsertToDb(change.File);

        CloudChangeType cloudChangeType = new CloudChangeType(ci_old, ci_new);
        if(cloudChangeType.Flag.HasFlag(CloudChangeFlag.Deleted) || cloudChangeType.ChangedId) CloudItem.Delete(change.FileId, account.Id);

        result.Add(cloudChangeType);
      }
      if (!string.IsNullOrEmpty(change_list.NextPageToken)) result.AddRange(await WatchChange(change_list.NextPageToken).ConfigureAwait(false));

      result.NewWatchToken = change_list.NewStartPageToken;
      return result;
    }

    async Task<Stream> Download_(string uri, long start, long end)
    {
      using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
      {
        requestMessage.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
        var response = ds.HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false).GetAwaiter().GetResult();
        if (response.IsSuccessStatusCode)
        {
          return new ThrottledStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), true);
        }
        else
        {
          if (DllCloudInit.SkipNoticeMalware)
          {
            if (uri.IndexOf("&acknowledgeAbuse=true", StringComparison.OrdinalIgnoreCase) >= 0) throw new HttpRequestException(response.Content.ReadAsStringAsync().Result);
            else return await Download_(uri += "&acknowledgeAbuse=true", start, end).ConfigureAwait(false);
          }
          else return null;
        }
      }
    }

    IList<CloudItem> CloudOpenDialogLoadChildFolder(string Id, string NextPageToken = null)
    {
      List<CloudItem> cis = new List<CloudItem>();
      var list_request = ds.Files.List();
      if (!string.IsNullOrEmpty(NextPageToken)) list_request.PageToken = NextPageToken;
      list_request.Q = string.Format(DllCloudInit.DefaultCulture, Query_ListFolder, Id);
      list_request.OrderBy = OrderBy;
      list_request.Fields = Fields_DriveFileList;
      list_request.PageSize = 1000;
      var list_result = list_request.Execute();
      foreach (var file in list_result.Files)
      {
        file.Parents = new List<string>() { Id };//google api will change after sep 2020
        cis.Add(InsertToDb(file));
      }
      if (list_result.IncompleteSearch == true) cis.AddRange(CloudOpenDialogLoadChildFolder(Id, list_result.NextPageToken));
      return cis;
    }

    public void _ListAllItemsToDb(SyncRoot syncRoot, string StartFolderId)
    {
      //Hashtable hashtable = new Hashtable();
      Queue<string> queue = new Queue<string>(new List<string>() { StartFolderId });
      syncRoot.SyncRootViewModel.EnumStatus = SyncRootStatus.ScanningCloud;
      GetMetadata(StartFolderId).ConfigureAwait(true).GetAwaiter().GetResult();//read root first 
      int reset = 0;
      int total = 0;
      string NextPageToken = null;
      while (queue.Count > 0)
      {
        string Id = queue.Dequeue();
        //if (hashtable.ContainsKey(Id)) continue;
        //else hashtable.Add(Id, Id);

        var list_request = ds.Files.List();
        if (!string.IsNullOrEmpty(NextPageToken)) list_request.PageToken = NextPageToken;
        list_request.Q = string.Format(DllCloudInit.DefaultCulture, Query_List, Id);
        list_request.OrderBy = OrderBy;
        list_request.Fields = Fields_DriveFileList;
        list_request.PageSize = 1000;
        FileList list_result = list_request.Execute();
        foreach (var file in list_result.Files)
        {
          if (file.MimeType.Equals(MimeType_Folder, StringComparison.OrdinalIgnoreCase)) queue.Enqueue(file.Id);//folder
          else
          {
            //if (hashtable.ContainsKey(file.Id)) continue;
            //else hashtable.Add(file.Id, file.Id);

            //if (file.MimeType.Equals(MimeType_Shortcut, StringComparison.OrdinalIgnoreCase)) continue;//ignore Shortcut
            //else
            if (file.MimeType.IndexOf(MimeType_googleapp, StringComparison.OrdinalIgnoreCase) >= 0) continue;//ignore google app (&shortcut)
          }
          file.Parents = new List<string>() { Id };//google api will change after sep 2020
          InsertToDb(file);
          total++;
          reset++;
          if (syncRoot.SyncRootViewModel.EnumStatus == SyncRootStatus.ScanningCloud)
            syncRoot.SyncRootViewModel.Message = string.Format(CultureInfo.InvariantCulture, "Items Scanned: {0}, Name: {1}", total, file.Name);
        }

        if (list_result.IncompleteSearch == true) NextPageToken = list_result.NextPageToken;
        else NextPageToken = null;

        if (reset >= 500)
        {
          reset = 0;
          GC.Collect();
        }
      }
    }



    #region ICloud
    public Task<bool> LogOut()
    {
      using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(30000))
      {
        return user.RevokeTokenAsync(cancellationTokenSource.Token);
      }
    }

    public async Task<Stream> Download(string Id, long posStart, long posEnd)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException("ci or ci.Id is null");
      string uri = string.Format(DllCloudInit.DefaultCulture, DownloadUri, Id);
      return await Download_(uri, posStart, posEnd).ConfigureAwait(false);
    }

    public async Task<CloudItem> Upload(string FilePath, string ParentId, string ItemCloudId = null)
    {
      if (string.IsNullOrEmpty(FilePath)) throw new ArgumentNullException(nameof(FilePath));
      if (string.IsNullOrEmpty(ParentId)) throw new ArgumentException(nameof(ParentId));

      FileInfo fi = new FileInfo(FilePath);
      if (fi.Attributes.HasFlag(FileAttributes.Directory))
      {
        return await CreateFolder(fi.Name, ParentId).ConfigureAwait(false);
      }
      else//file
      {
        Uri uploadid;
        HttpRequestMessage requestMessage;
        Google.Apis.Drive.v3.Data.File drivefile;
        if (string.IsNullOrEmpty(ItemCloudId))//upload new file
        {
          drivefile = new Google.Apis.Drive.v3.Data.File
          {
            Name = fi.Name,
            CreatedTime = fi.CreationTimeUtc,
            ModifiedTime = fi.LastWriteTimeUtc,
            Parents = new List<string>() { ParentId }
          };

          requestMessage = new HttpRequestMessage(HttpMethod.Post, UploadUri);
          requestMessage.Headers.Add("X-Upload-Content-Type", MimeTypeMap.GetMimeType(fi.Extension.Substring(1)));
          requestMessage.Headers.Add("X-Upload-Content-Length", fi.Length.ToString(DllCloudInit.DefaultCulture));
          requestMessage.Content = new StringContent(ds.Serializer.Serialize(drivefile), Encoding.UTF8, "application/json");
        }
        else//upload new revision
        {
          HttpMethod httpMethod = new HttpMethod("PATCH");
          string uri = string.Format(DllCloudInit.DefaultCulture, UploadRevisionUri, ItemCloudId);

          requestMessage = new HttpRequestMessage(httpMethod, uri);
          requestMessage.Headers.Add("X-Upload-Content-Type", MimeTypeMap.GetMimeType(fi.Extension.Substring(1)));
          requestMessage.Headers.Add("X-Upload-Content-Length", fi.Length.ToString(DllCloudInit.DefaultCulture));
        }
        HttpResponseMessage responseMessage = await ds.HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
        uploadid = responseMessage.Headers.Location;
        requestMessage.Dispose();

        //Upload file
        using (ThrottledStream fs = new ThrottledStream(new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, DllCloudInit.FileShareDefault), false))
        {
          HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uploadid);
          request.Method = "PUT";
          request.AllowWriteStreamBuffering = false;
          request.KeepAlive = true;
          request.ContentType = "application/octet-stream";
          if (fs.Length != 0) request.Headers.Add(HttpRequestHeader.ContentRange, string.Format(DllCloudInit.DefaultCulture, "bytes {0}-{1}/{2}", 0, fs.Length - 1, fs.Length));
          request.ContentLength = fs.Length;
          using (var requestStream = request.GetRequestStream()) fs.CopyTo(requestStream);
          var response = request.GetResponse();
          using (StreamReader sr = new StreamReader(response.GetResponseStream()))
          {
            string result = sr.ReadToEnd();
            drivefile = ds.Serializer.Deserialize<Google.Apis.Drive.v3.Data.File>(result);
            return await GetMetadata(drivefile.Id).ConfigureAwait(false);
          }
        }
      }
    }

    public async Task<ICloudChangeTypeCollection> WatchChange()
    {
      if (string.IsNullOrEmpty(account.WatchToken)) await InitWatch().ConfigureAwait(false);
      else return await WatchChange(account.WatchToken).ConfigureAwait(false);
      return new CloudChangeTypeCollection();
    }

    public Task<IList<CloudItem>> ListChildsFolderOfFolder(string itemId)
    {
      if (string.IsNullOrEmpty(itemId)) throw new ArgumentNullException(nameof(itemId));
      return Task.Factory.StartNew<IList<CloudItem>>(() => CloudOpenDialogLoadChildFolder(itemId, null), 
        CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public Task ListAllItemsToDb(SyncRoot syncRoot, string StartFolderId)
    {
      if (null == syncRoot) throw new ArgumentNullException(nameof(syncRoot));
      if (string.IsNullOrEmpty(StartFolderId)) throw new ArgumentNullException(nameof(StartFolderId));

      return Task.Factory.StartNew(() => _ListAllItemsToDb(syncRoot, StartFolderId),
        syncRoot.SyncRootViewModel.TokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);//
    }

    public async Task UpdateMetadata(UpdateCloudItem updateCloudItem)
    {
      if (null == updateCloudItem || string.IsNullOrEmpty(updateCloudItem.Id)) throw new ArgumentNullException(nameof(updateCloudItem));

      var getrequest = ds.Files.Get(updateCloudItem.Id);
      getrequest.Fields = Fields_DriveFile;
      Google.Apis.Drive.v3.Data.File drivefile_get = getrequest.Execute();

      Google.Apis.Drive.v3.Data.File drivefile = new Google.Apis.Drive.v3.Data.File()
      {
        Name = updateCloudItem.NewName,
        CreatedTime = updateCloudItem.CreationTime,
        ModifiedTime = updateCloudItem.LastWriteTime,
      };
      var request = ds.Files.Update(drivefile, updateCloudItem.Id);
      request.Fields = Fields_DriveFile;
      request.AddParents = updateCloudItem.NewParentId;
      request.RemoveParents = Extensions.ParentsCommaSeparatedList(drivefile_get.Parents);
      await request.ExecuteAsync().ConfigureAwait(false);
    }

    public async Task TrashItem(string Id)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));

      await ds.Files.Update(new Google.Apis.Drive.v3.Data.File() { Trashed = true }, Id).ExecuteAsync().ConfigureAwait(false);
    }

    public async Task<CloudItem> CreateFolder(string Name, string ParentId)
    {
      if (string.IsNullOrEmpty(Name)) throw new ArgumentNullException(nameof(Name));
      if (string.IsNullOrEmpty(ParentId)) throw new ArgumentException(nameof(ParentId));

      Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File
      {
        Name = Name,
        MimeType = MimeType_Folder,
        Parents = new List<string>() { ParentId }
      };
      var request = ds.Files.Create(file);
      request.Fields = Fields_DriveFile;
      var result = await request.ExecuteAsync().ConfigureAwait(false);
      result.Parents = new List<string>() { ParentId };
      return InsertToDb(result);
    }

    public async Task<Quota> GetQuota()
    {
      var aboutrequest = ds.About.Get();
      aboutrequest.Fields = "storageQuota";
      var about = await aboutrequest.ExecuteAsync().ConfigureAwait(false);
      Quota quota = new Quota
      {
        Limit = about.StorageQuota.Limit,
        Usage = about.StorageQuota.Usage.Value
      };
      return quota;
    }

    public bool HashCheck(string filePath, CloudItem ci)
    {
      if (null != ci && System.IO.File.Exists(filePath))
      {
        FileInfo finfo = new FileInfo(filePath);
        if (finfo.Length == ci.Size && !string.IsNullOrEmpty(ci.HashString))
        {
          using (var md5 = MD5.Create())
          {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
              byte[] hash = md5.ComputeHash(fs);
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

      var getrequest = ds.Files.Get(Id);
      getrequest.Fields = Fields_DriveFile;

      Task<Google.Apis.Drive.v3.Data.File> task_file = getrequest.ExecuteAsync();
      CloudItem ci = CloudItem.GetFromId(Id, this.account.Id);
      Google.Apis.Drive.v3.Data.File file = await task_file.ConfigureAwait(false);
      if (null != ci && null != file.Parents) file.Parents = new List<string>() { ci.Id };

      return InsertToDb(file);
    }
    #endregion
  }
}
