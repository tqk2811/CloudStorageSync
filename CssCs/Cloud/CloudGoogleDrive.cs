﻿using CssCs.DataClass;
using CssCs.UI.ViewModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CssCs.Cloud
{
  internal sealed class CloudGoogleDrive : ICloud
  {
    const string OrderBy = "folder,name,createdTime";

    const string Fields_DriveFile = "id,name,mimeType,trashed,parents,webContentLink,hasThumbnail,thumbnailLink,createdTime,modifiedTime,ownedByMe,capabilities,md5Checksum,size";
    const string Fields_DriveFileList = "nextPageToken,incompleteSearch,files(" + Fields_DriveFile + ")";
    const string Fields_WatchChange = "nextPageToken,newStartPageToken,changes(type,changeType,time,removed,fileId,file(" + Fields_DriveFile + "))";

    const string MimeType_Folder = "application/vnd.google-apps.folder";
    const string MimeType_googleapp = "application/vnd.google-apps.";

    const string Query_ListFolder = "'{0}' in parents and mimeType = '" + MimeType_Folder + "' and trashed = false";
    const string Query_List = "'{0}' in parents and trashed = false";

    const string DownloadUri = "https://www.googleapis.com/drive/v3/files/{0}?alt=media";
    const string UploadUri = "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable";
    const string UploadRevisionUri = "https://www.googleapis.com/upload/drive/v3/files/{0}?uploadType=resumable";

    CloudEmailViewModel cevm;
    DriveService ds;
    internal CloudGoogleDrive(CloudEmailViewModel cevm)
    {
      if (cevm == null) throw new ArgumentNullException("cevm");
      if (string.IsNullOrEmpty(cevm.Token)) throw new NullReferenceException("cevm.Token");
      this.cevm = cevm;
      ds = GetDriveService(cevm.Token).Result;
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
      using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(Settings.Setting.OauthWait))
      {
        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            new[] { DriveService.Scope.Drive },
            user,
            cancellationTokenSource.Token,
#if DEBUG
          new FileDataStore("D:\\GDToken"/*CPPCLR_Callback.UWPLocalStatePath + "\\GoogleDriveToken"*/, true));
#else
          new FileDataStore(CPPCLR_Callback.UWPLocalStatePath + "\\GoogleDriveToken",true));
#endif
      }
    }
    internal static async Task<DriveService> GetDriveService(string user = null)
    {
      return new DriveService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = await Oauth2(user),
        ApplicationName = Properties.Resources.ApplicationName,
        ApiKey = Properties.Resources.GoogleDriveApiKey
      });
    }
    internal static DriveService GetDriveService(UserCredential user)
    {
      return new DriveService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = user,
        ApplicationName = Properties.Resources.ApplicationName,
        ApiKey = Properties.Resources.GoogleDriveApiKey
      });
    }

    internal CloudItem InsertToDb(Google.Apis.Drive.v3.Data.File drivefile)
    {
      if (drivefile == null) return null;
      bool isfolder = drivefile.MimeType.Equals(MimeType_Folder);
      CloudItem ci = new CloudItem();
      ci.Id = drivefile.Id;
      ci.IdEmail = cevm.Sqlid;
      ci.Name = drivefile.Name;
      ci.DateCreate = drivefile.CreatedTime.Value.GetUnixTimeSeconds();
      ci.DateMod = drivefile.ModifiedTime.Value.GetUnixTimeSeconds();
      if (drivefile.Parents != null) ci.ParentsId = new List<string>(drivefile.Parents);
      ci.Size = isfolder ? -1 : drivefile.Size.Value;
      ci.HashString = drivefile.Md5Checksum;
      if (drivefile.Capabilities.CanDownload == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanDownload;
      if (drivefile.Capabilities.CanEdit == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanEdit;
      if (drivefile.Capabilities.CanRename == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanRename;
      if (drivefile.Capabilities.CanShare == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanShare;
      if (drivefile.Capabilities.CanTrash == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanTrash;
      if (drivefile.Capabilities.CanUntrash == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanUntrash;
      if (drivefile.Capabilities.CanAddChildren == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanAddChildren;
      if (drivefile.Capabilities.CanRemoveChildren == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.CanRemoveChildren;

      if (drivefile.OwnedByMe == true) ci.CapabilitiesAndFlag |= CloudCapabilitiesAndFlag.OwnedByMe;
      ci.InsertUpdate();
      return ci;
    }
    internal Google.Apis.Drive.v3.Data.File GetMetadata(string fileid)
    {
      var getrequest = ds.Files.Get(fileid);
      getrequest.Fields = Fields_DriveFile;
      return getrequest.Execute();
    }

    private async Task InitWatch()
    {
      var change = await ds.Changes.GetStartPageToken().ExecuteAsync();
      cevm.WatchToken = change.StartPageTokenValue;
      cevm.Update();
    }
    private async Task<IList<CloudChangeType>> WatchChange(string WatchToken)
    {
      List<CloudChangeType> result = new List<CloudChangeType>();
      var change_request = ds.Changes.List(WatchToken);
      change_request.Fields = Fields_WatchChange;
      change_request.PageSize = 1000;
      change_request.RestrictToMyDrive = true;
      var change_list = await change_request.ExecuteAsync();
      foreach (var change in change_list.Changes)
      {
        if (!change.ChangeType.Equals("file")) continue;

        if (change.Removed == false && //not delete
          !change.File.MimeType.Equals(MimeType_Folder) &&//not folder
          change.File.MimeType.IndexOf(MimeType_googleapp) >= 0) continue;//ignore google app

        CloudItem ci_old = CloudItem.Select(change.FileId, cevm.Sqlid);
        CloudChangeType changetype;
        if (change.Removed == true || change.File.Trashed == true)
        {
          changetype = new CloudChangeType(change.FileId, ci_old == null ? null : ci_old.ParentsId, null);//delete
          changetype.Flag |= CloudChangeFlag.IsDeleted;
        }
        else
        {
          changetype = new CloudChangeType(change.FileId, ci_old == null ? null : ci_old.ParentsId, change.File.Parents);

          if (ci_old == null ? false : !change.File.Name.Equals(ci_old.Name)) changetype.Flag |= CloudChangeFlag.IsRename;
          if (!change.FileId.Equals(change.File.Id)) changetype.Flag |= CloudChangeFlag.IsChangedId;
          changetype.IdNew = changetype.Flag.HasFlag(CloudChangeFlag.IsChangedId) ? change.File.Id : null;

          if (ci_old != null &&
            (ci_old.Size != change.File.Size ||
            ci_old.DateMod != change.File.ModifiedTime.Value.GetUnixTimeSeconds())) changetype.Flag |= CloudChangeFlag.IsChangeTimeAndSize;
        }
        changetype.CEId = cevm.Sqlid;
        if (changetype.Flag.HasFlag(CloudChangeFlag.IsDeleted)) CloudItem.Delete(change.FileId, cevm.Sqlid);
        else
        {
          if (changetype.Flag.HasFlag(CloudChangeFlag.IsChangedId)) CloudItem.Delete(change.FileId, cevm.Sqlid);
          changetype.CiNew = InsertToDb(change.File);
        }
        result.Add(changetype);
      }
      if (!string.IsNullOrEmpty(change_list.NextPageToken)) result.AddRange(await WatchChange(change_list.NextPageToken));
      cevm.WatchToken = change_list.NewStartPageToken;
      cevm.Update();
      return result;
    }

    private Task<Stream> Download_(string uri, long? start, long? end)
    {
      var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
      requestMessage.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
      var response = ds.HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).Result;
      if (response.IsSuccessStatusCode) return response.Content.ReadAsStreamAsync();
      else
      {
        if (Settings.Setting.SkipNoticeMalware)
        {
          if (uri.LastIndexOf("&acknowledgeAbuse=true") >= 0) throw new HttpRequestException(response.Content.ReadAsStringAsync().Result);
          else return Download_(uri += "&acknowledgeAbuse=true", start, end);
        }
        else return null;
      }
    }

    private IList<CloudItem> CloudOpenDialogLoadChildFolder(string item_id, string NextPageToken = null)
    {
      List<CloudItem> cis = new List<CloudItem>();
      var list_request = ds.Files.List();
      if (!string.IsNullOrEmpty(NextPageToken)) list_request.PageToken = NextPageToken;
      list_request.Q = string.Format(Query_ListFolder, item_id);
      list_request.OrderBy = OrderBy;
      list_request.Fields = Fields_DriveFileList;
      list_request.PageSize = 1000;
      var list_result = list_request.Execute();
      foreach (var file in list_result.Files) cis.Add(InsertToDb(file));
      if (list_result.IncompleteSearch == true) cis.AddRange(CloudOpenDialogLoadChildFolder(item_id, list_result.NextPageToken));
      return cis;
    }









    #region ICloud
    public Task<Stream> Download(string fileid, long? start, long? end)
    {
      string uri = string.Format(DownloadUri, fileid);
      return Download_(uri, start, end);
    }
    public async Task<CloudItem> Upload(string FilePath, IList<string> ParentIds, string ItemCloudId = null)
    {
      FileInfo fi = new FileInfo(FilePath);      
      if (fi.Attributes.HasFlag(FileAttributes.Directory))
      {
        CPPCLR_Callback.OutPutDebugString("CloudGDrive.Upload: Creating Folder in cloud,path:" + FilePath, 1);
        return await CreateFolder(fi.Name, ParentIds);
      }
      else//file
      {
        Uri uploadid;
        HttpRequestMessage requestMessage;
        Google.Apis.Drive.v3.Data.File drivefile;
        if (string.IsNullOrEmpty(ItemCloudId))//upload new file
        {
          drivefile = new Google.Apis.Drive.v3.Data.File();
          drivefile.Name = fi.Name;
          drivefile.CreatedTime = fi.CreationTimeUtc;
          drivefile.ModifiedTime = fi.LastWriteTimeUtc;
          drivefile.Parents = new List<string>(ParentIds);

          requestMessage = new HttpRequestMessage(HttpMethod.Post, UploadUri);
          requestMessage.Headers.Add("X-Upload-Content-Type", MimeTypeMap.GetMimeType(fi.Extension.Substring(1)));
          requestMessage.Headers.Add("X-Upload-Content-Length", fi.Length.ToString());
          requestMessage.Content = new StringContent(ds.Serializer.Serialize(drivefile), Encoding.UTF8, "application/json");          
        }
        else//upload new revision
        {
          HttpMethod httpMethod = new HttpMethod("PATCH");
          string uri = string.Format(UploadRevisionUri, ItemCloudId);

          requestMessage = new HttpRequestMessage(httpMethod, uri);
          requestMessage.Headers.Add("X-Upload-Content-Type", MimeTypeMap.GetMimeType(fi.Extension.Substring(1)));
          requestMessage.Headers.Add("X-Upload-Content-Length", fi.Length.ToString());
        }
        HttpResponseMessage responseMessage = await ds.HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);
        uploadid = responseMessage.Headers.Location;

        //Upload file
        using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
        {
          HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uploadid);
          request.Method = "PUT";
          request.AllowWriteStreamBuffering = false;
          request.ContentType = "application/octet-stream";
          if (fs.Length != 0) request.Headers.Add(HttpRequestHeader.ContentRange, string.Format("bytes {0}-{1}/{2}", 0, fs.Length - 1, fs.Length));
          request.ContentLength = fs.Length;
          using (var requestStream = request.GetRequestStream()) fs.CopyTo(requestStream);
          var response = request.GetResponse();
          StreamReader sr = new StreamReader(response.GetResponseStream());
          string result = sr.ReadToEnd();
          drivefile = ds.Serializer.Deserialize<Google.Apis.Drive.v3.Data.File>(result);
          drivefile = GetMetadata(drivefile.Id);
          return InsertToDb(drivefile);
        }
      }
    }


    public async Task<IList<CloudChangeType>> WatchChange()
    {
      List<CloudChangeType> result = new List<CloudChangeType>();
      if (string.IsNullOrEmpty(cevm.WatchToken)) await InitWatch();
      else result.AddRange(await WatchChange(cevm.WatchToken));
      return result;
    }

    public IList<CloudItem> CloudFolderGetChildFolder(string itemid)
    {
      return CloudOpenDialogLoadChildFolder(itemid, null);
    }
    public void ListAllItemsToDb(SyncRootViewModel srvm, string StartFolderId)
    {
      List<string> FolderIds = new List<string>() { StartFolderId };
      InsertToDb(GetMetadata(StartFolderId));//read root first 
      int reset = 0;
      int total = 0;
      string NextPageToken = null;
      while (FolderIds.Count > 0)
      {
        var list_request = ds.Files.List();
        if (!string.IsNullOrEmpty(NextPageToken)) list_request.PageToken = NextPageToken;
        list_request.Q = string.Format(Query_List, FolderIds[0]);
        list_request.OrderBy = OrderBy;
        list_request.Fields = Fields_DriveFileList;
        list_request.PageSize = 1000;
        FileList list_result = list_request.Execute();
        foreach (var file in list_result.Files)
        {
          if (!srvm.IsWork) return;
          if (file.MimeType.Equals(MimeType_Folder)) FolderIds.Add(file.Id);//folder
          else if (file.MimeType.IndexOf(MimeType_googleapp) >= 0) continue;//ignore google app
          InsertToDb(file);
          total++;
          reset++;
          if (srvm.Status == SyncRootStatus.ScanningCloud) srvm.Message = string.Format("Items Scanned: {0}, Name: {1}", total, file.Name);
        }
        if (!srvm.IsWork) return;//stop
        if (list_result.IncompleteSearch == true) NextPageToken = list_result.NextPageToken;
        else
        {
          NextPageToken = null;
          FolderIds.RemoveAt(0);
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
      Google.Apis.Drive.v3.Data.File drivefile = new Google.Apis.Drive.v3.Data.File()
      {
        Name = updateCloudItem.NewName,
        CreatedTime = updateCloudItem.CreationTime,
        ModifiedTime = updateCloudItem.LastWriteTime,
      };
      var request = ds.Files.Update(drivefile, updateCloudItem.Id);
      request.Fields = Fields_DriveFile;
      request.AddParents = Extensions.ParentsCommaSeparatedList(updateCloudItem.ParentIdsAdd);
      request.RemoveParents = Extensions.ParentsCommaSeparatedList(updateCloudItem.ParentIdsRemove);
      await request.ExecuteAsync();
    }
    public async Task TrashItem(string Id)
    {
      await ds.Files.Update(new Google.Apis.Drive.v3.Data.File() { Trashed = true }, Id).ExecuteAsync();
    }

    public async Task<CloudItem> CreateFolder(string name, IList<string> ParentIds)
    {
      Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File();
      file.Name = name;
      file.MimeType = MimeType_Folder;
      file.Parents = new List<string>(ParentIds);
      var request = ds.Files.Create(file);
      request.Fields = Fields_DriveFile;
      var result = await request.ExecuteAsync();
      return InsertToDb(result);
    }

    public async Task<Quota> GetQuota()
    {
      var aboutrequest = ds.About.Get();
      aboutrequest.Fields = "storageQuota";
      var about = await aboutrequest.ExecuteAsync();
      Quota quota = new Quota();
      quota.Limit = about.StorageQuota.Limit;
      quota.Usage = about.StorageQuota.Usage.Value;
      return quota;
    }

    public bool HashCheck(string filepath, CloudItem ci)
    {
      if(ci != null && System.IO.File.Exists(filepath))
      {
        FileInfo finfo = new FileInfo(filepath);
        if(finfo.Length == ci.Size && !string.IsNullOrEmpty(ci.HashString))
        {
          using (var md5 = MD5.Create())
          {
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
              byte[] hash = md5.ComputeHash(fs);
              string hashstring = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
              return ci.HashString.ToLowerInvariant().Equals(hashstring);
            }
          }
        }
      }
      return false;
    }
    #endregion
  }
}
