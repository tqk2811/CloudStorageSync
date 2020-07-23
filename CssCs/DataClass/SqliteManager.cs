using System.Collections.Generic;
using System.Data.SQLite;
using CssCs.UI.ViewModel;
using System.IO;
using System;
using System.Text;
using System.Windows;

namespace CssCs.DataClass
{
  internal static class SqliteManager
  {
    static string MakeSplitString(this IList<string> ids)
    {
      if (ids == null) return null;
      StringBuilder stringBuilder = new StringBuilder();
      if (ids.Count > 0) stringBuilder.Append(ids[0]);
      for (int i = 1; i < ids.Count; i++)
      {
        stringBuilder.Append('|');
        stringBuilder.Append(ids[i]);
      }
      return stringBuilder.ToString();
    }
    static IList<string> StringSplit(this string parents)
    {
      List<string> list = new List<string>();
      if (!string.IsNullOrEmpty(parents)) list.AddRange(parents.Split(new char[] { '|' }));
      return list;
    }


    private static List<string> create_tables = new List<string>()
    {
      _cevm_create,
      _srvm_create,
      _ci_create,
      _li_create,
      _le_create,
      _setting_create
    };
    static SQLiteConnection _con;
    internal static void Init()
    {
      if (_con != null) return;
      string filepath = CppInterop.UWPLocalStatePath + "\\data.sqlite3";
      string _strConnect = string.Format("Data Source={0};Version=3;", filepath);
      bool flag = false;
      if (!File.Exists(filepath))
      {
        flag = true;
        File.Create(filepath).Close();
      }
      _con = new SQLiteConnection(_strConnect);
      _con.Open();
      if (flag)
      {
        foreach (var com_table in create_tables)
        {
          SQLiteCommand command = new SQLiteCommand(com_table, _con);
          command.ExecuteNonQuery();
        }
        SettingInsert();
      }
      CEVMListAll();
      SRVMListAll();
      LIListAll();
    }
    internal static void Close()
    {
      if(_con != null)
      {
        _con.Close();
        _con = null;
      }
    }


    #region cevm
    const string _cevm_create = @"create table if not exists Email(
                                    Id CHAR(32) PRIMARY KEY NOT NULL,
                                    Email CHAR(321) NOT NULL,
                                    CloudName INTEGER NOT NULL,
                                    Token TEXT NOT NULL,
                                    WatchToken TEXT);";
    const string _cevm_listall = "select * from Email;";
    const string _cevm_insert = "insert into Email(Id,Email,CloudName,Token,WatchToken) values($Id,$Email,$CloudName,$Token,$WatchToken);";
    const string _cevm_update = "update Email set Email = $Email , CloudName = $CloudName , Token = $Token , WatchToken = $WatchToken WHERE Id = $Id;";
    const string _cevm_delete = "delete from Email where Id = $Id;";
   
    private static void CEVMListAll()
    {
      List<CloudEmailViewModel> cevms = new List<CloudEmailViewModel>();
      var command = _con.CreateCommand();
      command.CommandText = _cevm_listall;
      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          string id = reader.GetString(0);
          string email = reader.GetString(1);
          CloudName cn = (CloudName)reader.GetInt32(2);
          string token = reader.GetString(3);
          string watchtoken = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);//nullable
          cevms.Add(new CloudEmailViewModel(email, cn, token, id, watchtoken));
        }
      }      
      CloudEmailViewModel.Load(cevms);
    }
    internal static void CEVMInsert(CloudEmailViewModel cevm)
    {
      var command = _con.CreateCommand();
      command.CommandText = _cevm_insert;
      command.Parameters.AddWithValue("$Id", cevm.EmailSqlId);
      command.Parameters.AddWithValue("$Email", cevm.Email);
      command.Parameters.AddWithValue("$CloudName", (int)cevm.CloudName);
      command.Parameters.AddWithValue("$Token", cevm.Token);
      command.Parameters.AddWithValue("$WatchToken", cevm.WatchToken);
      lock (_con) command.ExecuteNonQuery();
    }
    internal static void CEVMUpdate(CloudEmailViewModel cevm)
    {
      var command = _con.CreateCommand();
      command.CommandText = _cevm_update;
      command.Parameters.AddWithValue("$Id", cevm.EmailSqlId);
      command.Parameters.AddWithValue("$Email", cevm.Email);
      command.Parameters.AddWithValue("$CloudName", (int)cevm.CloudName);
      command.Parameters.AddWithValue("$Token", cevm.Token);
      command.Parameters.AddWithValue("$WatchToken", cevm.WatchToken);
      command.ExecuteNonQuery();
    }
    internal static void CEVMDelete(CloudEmailViewModel cevm)
    {
      var command = _con.CreateCommand();
      command.CommandText = _cevm_delete;
      command.Parameters.AddWithValue("$Id", cevm.EmailSqlId);
      command.ExecuteNonQuery();
    }
    #endregion

    #region srvm
    enum srvmBoolFlag : long
    {
      None = 0,
      IsWork = 1 << 0,
      IsListedAll = 2 << 1
    }
    const string _srvm_create = @"create table if not exists SyncRoot(
                                    Id CHAR(32) PRIMARY KEY NOT NULL,
                                    IdEmail CHAR(32) NOT NULL,
                                    CloudFolderName TEXT,
                                    CloudFolderId TEXT,
                                    LocalPath TEXT,
                                    DisplayName TEXT,
                                    Flag BIG INT NOT NULL DEFAULT 0,
                                    FOREIGN KEY(IdEmail) REFERENCES Email(Id));";
    const string _srvm_listall = "select * from SyncRoot;";
    const string _srvm_insert = @"insert into SyncRoot(Id,IdEmail) values($Id,$IdEmail);";
    const string _srvm_update = @"update SyncRoot set CloudFolderName = $CloudFolderName , CloudFolderId = $CloudFolderId , 
LocalPath = $LocalPath , DisplayName = $DisplayName , Flag = $Flag where Id = $Id;";
    const string _srvm_delete = "delete from SyncRoot where Id = $Id;";
    private static void SRVMListAll()
    {
      List<SyncRootViewModel> srvms = new List<SyncRootViewModel>();
      var command = _con.CreateCommand();
      command.CommandText = _srvm_listall;
      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          string id = reader.GetString(0);
          string IdEmail = reader.GetString(1);
          string CloudFolderName = reader.IsDBNull(2) ? null : reader.GetString(2);//nullable
          string CloudFolderId = reader.IsDBNull(3) ? null : reader.GetString(3);//nullable
          string LocalPath = reader.IsDBNull(4) ? null : reader.GetString(4);//nullable
          string DisplayName = reader.IsDBNull(5) ? null : reader.GetString(5);
          srvmBoolFlag flag = (srvmBoolFlag)reader.GetInt64(6);
          bool IsWork = flag.HasFlag(srvmBoolFlag.IsWork);
          bool IsListedAll = flag.HasFlag(srvmBoolFlag.IsListedAll);
          srvms.Add(new SyncRootViewModel(CloudEmailViewModel.Find(IdEmail), id, CloudFolderName, CloudFolderId, LocalPath, DisplayName, IsWork, IsListedAll));
        }
      }
      SyncRootViewModel.Load(srvms);
    }
    internal static void SRVMInsert(SyncRootViewModel srvm)
    {
      var command = _con.CreateCommand();
      command.CommandText = _srvm_insert;
      command.Parameters.AddWithValue("$Id", srvm.SRId);
      command.Parameters.AddWithValue("$IdEmail", srvm.CEVM.EmailSqlId);
      lock (_con) command.ExecuteNonQuery();
    }
    internal static void SRVMUpdate(SyncRootViewModel srvm)
    {
      var command = _con.CreateCommand();
      command.CommandText = _srvm_update;
      command.Parameters.AddWithValue("$Id", srvm.SRId);
      command.Parameters.AddWithValue("$IdEmail", srvm.CEVM.EmailSqlId);
      command.Parameters.AddWithValue("$CloudFolderName", srvm.CloudFolderName);
      command.Parameters.AddWithValue("$CloudFolderId", srvm.CloudFolderId);
      command.Parameters.AddWithValue("$LocalPath", srvm.LocalPath);
      command.Parameters.AddWithValue("$DisplayName", srvm.DisplayName);
      srvmBoolFlag flag = srvmBoolFlag.None;
      if (srvm.IsWork) flag |= srvmBoolFlag.IsWork;
      if (srvm.IsListedAll) flag |= srvmBoolFlag.IsListedAll;
      command.Parameters.AddWithValue("$Flag", (long)flag);
      command.ExecuteNonQuery();
    }
    internal static void SRVMDelete(SyncRootViewModel srvm)
    {
      var command = _con.CreateCommand();
      command.CommandText = _srvm_delete;
      command.Parameters.AddWithValue("$Id", srvm.SRId);
      command.ExecuteNonQuery();
    }
    #endregion

    #region clouditem
    const string _ci_create = @"create table if not exists CloudItem(
                                    Id CHAR(128)        NOT NULL,
                                    IdEmail CHAR(32)    NOT NULL,
                                    Name TEXT           NOT NULL,
                                    Parents TEXT,
                                    Size BIG INT        DEFAULT 0,
                                    DateCreate BIG INT  DEFAULT 0,
                                    DateMod BIG INT     DEFAULT 0,
                                    CapabilitiesAndFlag BIG INT DEFAULT 0,
                                    HashString Text,
                                    PRIMARY KEY (Id, IdEmail),
                                    FOREIGN KEY(IdEmail) REFERENCES Email(Id));";
    const string _ci_select = "select * from CloudItem where Id = $Id and IdEmail = $IdEmail;";
    const string _ci_insert_update = @"insert into CloudItem(Id,IdEmail,Name,Parents,Size,DateCreate,DateMod,CapabilitiesAndFlag,HashString)
values($Id,$IdEmail,$Name,$Parents,$Size,$DateCreate,$DateMod,$CapabilitiesAndFlag,$HashString) on conflict(Id, IdEmail)
do update set Name=$Name, Parents=$Parents, Size = $Size, DateCreate = $DateCreate, DateMod = $DateMod, CapabilitiesAndFlag = $CapabilitiesAndFlag, HashString = $HashString
where Id = $Id AND IdEmail = $IdEmail;";
    const string _ci_delete = "delete from CloudItem where Id = $Id and IdEmail = $IdEmail;";
    const string _ci_findchild = "select * from CloudItem where IdEmail = $IdEmail and Parents like $Parent;";
    internal static CloudItem CISelect(string Id,string IdEmail)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
      if (string.IsNullOrEmpty(IdEmail)) throw new ArgumentNullException(nameof(IdEmail));
      var command = _con.CreateCommand();
      command.CommandText = _ci_select;
      command.Parameters.AddWithValue("$Id", Id);
      command.Parameters.AddWithValue("$IdEmail", IdEmail);
      using (var reader = command.ExecuteReader())
      {
        if (reader.Read())
        {
          CloudItem ci = new CloudItem();
          ci.Id = reader.GetString(0);
          ci.IdEmail = reader.GetString(1);
          ci.Name = reader.GetString(2);
          ci.ParentsId = reader.GetString(3)?.StringSplit();
          ci.Size = reader.GetInt64(4);
          ci.DateCreate = reader.GetInt64(5);
          ci.DateMod = reader.GetInt64(6);
          ci.CapabilitiesAndFlag = (CloudCapabilitiesAndFlag)reader.GetInt32(7);
          ci.HashString = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);//nullable
          return ci;
        } 
        else return null;
      }      
    }
    internal static void CIInsertUpadte(CloudItem ci)
    {
      var command = _con.CreateCommand();
      command.CommandText = _ci_insert_update;
      command.Parameters.AddWithValue("$Id", ci.Id);
      command.Parameters.AddWithValue("$IdEmail", ci.IdEmail);
      command.Parameters.AddWithValue("$Name", ci.Name);
      command.Parameters.AddWithValue("$Parents", ci.ParentsId.MakeSplitString());
      command.Parameters.AddWithValue("$Size", ci.Size);
      command.Parameters.AddWithValue("$DateCreate", ci.DateCreate);
      command.Parameters.AddWithValue("$DateMod", ci.DateMod);
      command.Parameters.AddWithValue("$CapabilitiesAndFlag", (int)ci.CapabilitiesAndFlag);
      command.Parameters.AddWithValue("$HashString", ci.HashString);
      lock (_con) command.ExecuteNonQuery();
    }
    internal static void CIDelete(string Id, string IdEmail)
    {
      if (string.IsNullOrEmpty(Id))throw new ArgumentNullException(nameof(Id));
      if (string.IsNullOrEmpty(IdEmail)) throw new ArgumentNullException(nameof(IdEmail));

      var command = _con.CreateCommand();
      command.CommandText = _ci_delete;
      command.Parameters.AddWithValue("$Id", Id);
      command.Parameters.AddWithValue("$IdEmail", IdEmail);
      command.ExecuteNonQuery();
    }
    internal static IList<CloudItem> CIFindChildIds(string CI_ParentId, string IdEmail)
    {
      if (string.IsNullOrEmpty(CI_ParentId)) throw new ArgumentNullException(nameof(CI_ParentId));
      if (string.IsNullOrEmpty(IdEmail)) throw new ArgumentNullException(nameof(IdEmail));

      var command = _con.CreateCommand();
      command.CommandText = _ci_findchild;
      command.Parameters.AddWithValue("$IdEmail", IdEmail);
      command.Parameters.AddWithValue("$Parent", string.Format("%{0}%", CI_ParentId));
      List<CloudItem> cis = new List<CloudItem>();
      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          CloudItem ci = new CloudItem();
          ci.Id = reader.GetString(0);
          ci.IdEmail = reader.GetString(1);
          ci.Name = reader.GetString(2);
          ci.ParentsId = reader.GetString(3).StringSplit();
          ci.Size = reader.GetInt64(4);
          ci.DateCreate = reader.GetInt64(5);
          ci.DateMod = reader.GetInt64(6);
          ci.CapabilitiesAndFlag = (CloudCapabilitiesAndFlag)reader.GetInt32(7);
          ci.HashString = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);//nullable
          cis.Add(ci);
        }
      }
      return cis;
    }
    #endregion

    #region localitem
    const string _li_create = @"create table if not exists LocalItem(
                                    Name NVARCHAR(255) NOT NULL,
                                    SRId CHAR(32) NOT NULL,
                                    CloudId CHAR(128),
                                    LocalParentId BIG INT NOT NULL DEFAULT 0,
                                    Flag BIG INT NOT NULL DEFAULT 0,
                                    UNIQUE(Name,LocalParentId),
                                    FOREIGN KEY(SRId) REFERENCES SyncRoot(Id));";
    const string _li_listall = "select rowid,* from LocalItem;";
    const string _li_insert = "insert into LocalItem(Name,SRId,CloudId,LocalParentId,Flag) VALUES ($Name,$SRId,$CloudId,$LocalParentId,$Flag);";
    const string _li_update = "update LocalItem set Name = $Name, CloudId = $CloudId, SRId = $SRId,LocalParentId = $LocalParentId,Flag = $Flag WHERE rowid = $rowid;";
    const string _li_delete = "delete from LocalItem where rowid = $rowid;";
    const string _li_clear = "delete from LocalItem where SRId = $SRId;";
    private static void LIListAll()
    {
      List<LocalItem> lis = new List<LocalItem>();
      var command = _con.CreateCommand();
      command.CommandText = _li_listall;
      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          LocalItem li = new LocalItem(true);
          li.LocalId = reader.GetInt64(0);
          li.Name = reader.GetString(1);
          li.SRId = reader.GetString(2);
          li.CloudId = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);//nullable
          li.LocalParentId = reader.GetInt64(4);
          li.Flag = (LocalItemFlag)reader.GetInt64(5);
          lis.Add(li);
        }
      }
      LocalItem.Load(lis);
    }
    internal static void LIInsert(LocalItem li)
    {
      var command = _con.CreateCommand();
      command.CommandText = _li_insert;
      command.Parameters.AddWithValue("$Name", li.Name);
      command.Parameters.AddWithValue("$SRId", li.SRId);
      command.Parameters.AddWithValue("$CloudId", li.CloudId);
      command.Parameters.AddWithValue("$LocalParentId", li.LocalParentId);
      command.Parameters.AddWithValue("$Flag", (long)li.Flag);
      lock (_con)
      {
        command.ExecuteNonQuery();
        li.LocalId = _con.LastInsertRowId;
      }
    }
    internal static void LIUpdate(LocalItem li)
    {
      var command = _con.CreateCommand();
      command.CommandText = _li_update;
      command.Parameters.AddWithValue("$Name", li.Name);
      command.Parameters.AddWithValue("$SRId", li.SRId);
      command.Parameters.AddWithValue("$CloudId", li.CloudId);
      command.Parameters.AddWithValue("$LocalParentId", li.LocalParentId);
      command.Parameters.AddWithValue("$Flag", (long)li.Flag);
      command.Parameters.AddWithValue("$rowid", li.LocalId);
      command.ExecuteNonQuery();
    }
    internal static void LIDelete(LocalItem li)
    {
      var command = _con.CreateCommand();
      command.CommandText = _li_delete;
      command.Parameters.AddWithValue("$rowid", li.LocalId);
      command.ExecuteNonQuery();
    }
    internal static void LIClear(string SRId)
    {
      if (string.IsNullOrEmpty(SRId)) throw new ArgumentNullException(nameof(SRId));

      var command = _con.CreateCommand();
      command.CommandText = _li_clear;
      command.Parameters.AddWithValue("$SRId", SRId);
      command.ExecuteNonQuery();
    }
    #endregion

    #region localerror
    const string _le_create = @"create table if not exists LocalError(
                                    LIId BIG INT NOT NULL DEFAULT 0,
                                    SRId CHAR(32) NOT NULL,
                                    Type INT NOT NULL DEFAULT 0,
                                    CIId CHAR(128));";
    const string _le_listall = "SELECT rowid,* FROM LocalError;";
    const string _le_insert = "insert into LocalError(LIId,SRId,Type,CIId) values($LIId,$SRId,$Type,$CIId);";
    const string _le_delete = "delete from LocalError where rowid = $rowid;";
    const string _le_clear = "delete from LocalError where SRId = $SRId;";
    internal static IList<LocalError> LEListAll()
    {
      List<LocalError> les = new List<LocalError>();
      var command = _con.CreateCommand();
      command.CommandText = _le_listall;
      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          LocalError le = new LocalError();
          le.SQLId = reader.GetInt64(0);
          le.LI_Id = reader.GetInt64(1);
          le.SRId = reader.GetString(2);
          le.Type = (LocalErrorType)reader.GetInt32(3);
          le.CIId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);//nullable
          les.Add(le);
        }
      }
      return les;
    }
    internal static void LEInsert(long LiId,string SrId, LocalErrorType type,string CiId)
    {
      if (LiId < 0 || string.IsNullOrEmpty(SrId)) return;

      var command = _con.CreateCommand();
      command.CommandText = _le_insert;
      command.Parameters.AddWithValue("$LIId", LiId);
      command.Parameters.AddWithValue("$SRId", SrId);
      command.Parameters.AddWithValue("$Type", (int)type);
      command.Parameters.AddWithValue("$CIId", CiId);
      lock (_con) command.ExecuteNonQuery();
    }
    internal static void LEDelete(long LeId)
    {
      var command = _con.CreateCommand();
      command.CommandText = _le_delete;
      command.Parameters.AddWithValue("$rowid", LeId);
      command.ExecuteNonQuery();
    }
    internal static void LEClear(string SrId)
    {
      if (string.IsNullOrEmpty(SrId)) return;
      var command = _con.CreateCommand();
      command.CommandText = _le_clear;
      command.Parameters.AddWithValue("$SRId", SrId);
      command.ExecuteNonQuery();
    }
    #endregion

    #region setting
    const int DBVer = 1;
    enum SettingBoolFlag : long
    {
      None = 0,
      SkipNoticeMalware = 1 << 0,
      UploadPrioritizeFirst = 1 << 1,
      DownloadPrioritizeFirst = 1 << 2,
    }
    const string _setting_create = @"create table if not exists Setting(
                                      Lock char(1) not null default 'X',

                                      FileIgnore Text,
                                      TryAgainAfter integer not null default 5,
                                      TryAgainTimes integer not null default 3,
                                      FilesUploadSameTime integer not null default 1,
                                      SpeedUploadLimit integer not null default 0,
                                      SpeedDownloadLimit integer not null default 0,
                                      TimeWatchChangeCloud integer not null default 15,
                                      Flag BIG INT NOT NULL default 7,
                                      DBVersion integer not null default 1,

                                      constraint PK_T1 PRIMARY KEY (Lock),
                                      constraint CK_T1_Locked CHECK (Lock='X'));";
    const string _setting_insert = @"insert into Setting(FileIgnore) values ($FileIgnore);";
    const string _setting_select = @"select * from Setting;";
    const string _setting_update = @"update Setting set 
FileIgnore = $FileIgnore, 
TryAgainAfter = $TryAgainAfter, 
FilesUploadSameTime = $FilesUploadSameTime,
SpeedUploadLimit = $SpeedUploadLimit, 
SpeedDownloadLimit = $SpeedDownloadLimit, 
TimeWatchChangeCloud = $TimeWatchChangeCloud, 
Flag = $Flag
where Lock = 'X';";
    private static void SettingInsert()
    {
      var command = _con.CreateCommand();
      command.CommandText = _setting_insert;
      command.Parameters.AddWithValue("$FileIgnore", Settings.Setting.FileIgnore);
      lock (_con) command.ExecuteNonQuery();
    }
    internal static bool SettingSelect()
    {
      var command = _con.CreateCommand();
      command.CommandText = _setting_select;
      using (var reader = command.ExecuteReader())
      {
        if(reader.Read())
        {
          int DBVersion = reader.GetInt32(9);
          if (DBVersion != DBVer)
          {
            CppInterop.OutPutDebugString("Different DB version, please uninstall and install again.");
            Extensions.PostQuitMessage(0);
            return false;
          }
          Settings.Setting.LoadSetting = true;
          Settings.Setting.FileIgnore = reader.GetString(1);
          Settings.Setting.TryAgainAfter = reader.GetInt32(2);
          Settings.Setting.TryAgainTimes = reader.GetInt32(3);
          Settings.Setting.FilesUploadSameTime = reader.GetInt32(4); 
          Settings.Setting.SpeedUploadLimit = reader.GetInt32(5);
          Settings.Setting.SpeedDownloadLimit = reader.GetInt32(6);
          Settings.Setting.TimeWatchChangeCloud = reader.GetInt32(7);
          SettingBoolFlag flag = (SettingBoolFlag)reader.GetInt64(8);

          Settings.Setting.SkipNoticeMalware = flag.HasFlag(SettingBoolFlag.SkipNoticeMalware);
          Settings.Setting.UploadPrioritizeFirst = flag.HasFlag(SettingBoolFlag.UploadPrioritizeFirst);
          Settings.Setting.DownloadPrioritizeFirst = flag.HasFlag(SettingBoolFlag.DownloadPrioritizeFirst);
          Settings.Setting.LoadSetting = false;
          return true;
        }
        else
        {
          CppInterop.OutPutDebugString("Can't read setting.");
          return false;
        }
      }
    }
    internal static void UpdateSetting()
    {
      var command = _con.CreateCommand();
      command.CommandText = _setting_update;
      command.Parameters.AddWithValue("$FileIgnore", Settings.Setting.FileIgnore);
      command.Parameters.AddWithValue("$TryAgainAfter", Settings.Setting.TryAgainAfter); 
      command.Parameters.AddWithValue("$TryAgainTimes", Settings.Setting.TryAgainTimes);
      command.Parameters.AddWithValue("$FilesUploadSameTime", Settings.Setting.FilesUploadSameTime);
      command.Parameters.AddWithValue("$SpeedUploadLimit", Settings.Setting.SpeedUploadLimit);
      command.Parameters.AddWithValue("$SpeedDownloadLimit", Settings.Setting.SpeedDownloadLimit);
      command.Parameters.AddWithValue("$TimeWatchChangeCloud", Settings.Setting.TimeWatchChangeCloud);
      SettingBoolFlag flag = SettingBoolFlag.None;
      if (Settings.Setting.SkipNoticeMalware) flag |= SettingBoolFlag.SkipNoticeMalware;
      if (Settings.Setting.UploadPrioritizeFirst) flag |= SettingBoolFlag.UploadPrioritizeFirst;
      if (Settings.Setting.DownloadPrioritizeFirst) flag |= SettingBoolFlag.DownloadPrioritizeFirst;
      command.Parameters.AddWithValue("$Flag", (long)flag);
      command.ExecuteNonQuery();
    }
    #endregion
  }
}
