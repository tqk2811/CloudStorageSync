using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;

namespace CssCsData
{
  internal static partial class SqliteManaged
  {
    static readonly object _lock = new object();
    static SQLiteConnection _con;
    private static readonly List<string> create_tables = new List<string>()
    {
      _account_create,
      _syncroot_create,
      _clouditem_create,
      _setting_create
    };
    internal static void Init()
    {
      string filepath = DllDataInit.UWPLocalStatePath + "\\data.sqlite3";
      string _strConnect = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;", filepath);
      bool DbExist = File.Exists(filepath);
      if (!DbExist) File.Create(filepath).Close();
      _con = new SQLiteConnection(_strConnect);
      _con.Open();
      if (!DbExist)
      {
        foreach (var com_table in create_tables)
          using (SQLiteCommand command = new SQLiteCommand(com_table, _con))
            command.ExecuteNonQuery();

        SettingInsert();
      }
      SettingSelect();
      AccountListAll();
      SyncRootListAll();
    }

    internal static void UnInit()
    {
      if (_con != null)
      {
        _con.Close();
        _con = null;
      }
    }


    #region Account
    const string _account_create = @"create table if not exists Account(
Id CHAR(32) PRIMARY KEY   NOT NULL,
Email CHAR(321)           NOT NULL,
CloudName INTEGER         NOT NULL,
Token TEXT                NOT NULL,
WatchToken TEXT);";
    const string _account_listall = "select * from Account;";
    const string _account_insert = "insert into Account(Id,Email,CloudName,Token,WatchToken) values($Id,$Email,$CloudName,$Token,$WatchToken);";
    const string _account_update = "update Account set Email = $Email , CloudName = $CloudName , Token = $Token , WatchToken = $WatchToken WHERE Id = $Id;";
    const string _account_delete = "delete from Account where Id = $Id;";
    private static void AccountListAll()
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _account_listall;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            string id = reader.GetString(0);
            string email = reader.GetString(1);
            CloudName cn = (CloudName)reader.GetInt32(2);
            string token = reader.GetString(3);
            string watchtoken = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);//nullable
            Account.Accounts.Add(new Account(id,email,cn)
            {
              Token = token,
              WatchToken = watchtoken,
              IsAvailableInDb = true
            });
          }
        }
      }
    }
    internal static bool AccountInsert(this Account account)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _account_insert;
        command.Parameters.AddWithValue("$Id", account.Id);
        command.Parameters.AddWithValue("$Email", account.Email);
        command.Parameters.AddWithValue("$CloudName", (int)account.CloudName);
        command.Parameters.AddWithValue("$Token", account.Token);
        command.Parameters.AddWithValue("$WatchToken", account.WatchToken);
        lock (_lock) return command.ExecuteNonQuery() == 1;
      }
    }
    internal static bool AccountUpdate(this Account account)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _account_update;
        command.Parameters.AddWithValue("$Id", account.Id);
        command.Parameters.AddWithValue("$Email", account.Email);
        command.Parameters.AddWithValue("$CloudName", (int)account.CloudName);
        command.Parameters.AddWithValue("$Token", account.Token);
        command.Parameters.AddWithValue("$WatchToken", account.WatchToken);
        return command.ExecuteNonQuery() == 1;
      }
    }
    internal static bool AccountDelete(this Account account)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _account_delete;
        command.Parameters.AddWithValue("$Id", account.Id);
        return command.ExecuteNonQuery() == 1;
      }
    }
    #endregion

    #region SyncRoot
    const string _syncroot_create = @"create table if not exists SyncRoot(
Id CHAR(32) PRIMARY KEY NOT NULL,
IdAccount CHAR(32) NOT NULL,
CloudFolderName TEXT,
CloudFolderId TEXT,
LocalPath TEXT,
DisplayName TEXT,
Flag BIG INT NOT NULL DEFAULT 0,
FOREIGN KEY(IdAccount) REFERENCES Email(Id));";
    const string _syncroot_listall = "select * from SyncRoot;";
    const string _syncroot_insert = @"insert into SyncRoot(Id,IdAccount) values($Id,$IdAccount);";
    const string _syncroot_update = @"update SyncRoot set CloudFolderName = $CloudFolderName , CloudFolderId = $CloudFolderId , 
LocalPath = $LocalPath , DisplayName = $DisplayName , Flag = $Flag where Id = $Id;";
    const string _syncroot_delete = "delete from SyncRoot where Id = $Id;";
    const string _syncroot_clear = "delete from SyncRoot where IdAccount = $IdAccount;";
    private static void SyncRootListAll()
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _syncroot_listall;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            string Id = reader.GetString(0);
            string IdAccount = reader.GetString(1);
            string CloudFolderName = reader.IsDBNull(2) ? null : reader.GetString(2);//nullable
            string CloudFolderId = reader.IsDBNull(3) ? null : reader.GetString(3);//nullable
            string LocalPath = reader.IsDBNull(4) ? null : reader.GetString(4);//nullable
            string DisplayName = reader.IsDBNull(5) ? null : reader.GetString(5);
            SyncRootFlag flag = (SyncRootFlag)reader.GetInt64(6);
            SyncRoot.SyncRoots.Add(new SyncRoot(Id,IdAccount)
            {
              CloudFolderName = CloudFolderName,
              CloudFolderId = CloudFolderId,
              LocalPath = LocalPath,
              DisplayName = DisplayName,
              Flag = flag,
              IsAvailableInDb = true
            });
          }
        }
      }
    }
    internal static bool SyncRootInsert(this SyncRoot syncRoot)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _syncroot_insert;
        command.Parameters.AddWithValue("$Id", syncRoot.Id);
        command.Parameters.AddWithValue("$IdAccount", syncRoot.IdAccount);
        lock (_lock) return command.ExecuteNonQuery() == 1;
      }
    }
    internal static bool SyncRootUpdate(this SyncRoot syncRoot)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _syncroot_update;
        command.Parameters.AddWithValue("$Id", syncRoot.Id);
        //command.Parameters.AddWithValue("$IdAccount", syncRoot.IdAccount);
        command.Parameters.AddWithValue("$CloudFolderName", syncRoot.CloudFolderName);
        command.Parameters.AddWithValue("$CloudFolderId", syncRoot.CloudFolderId);
        command.Parameters.AddWithValue("$LocalPath", syncRoot.LocalPath);
        command.Parameters.AddWithValue("$DisplayName", syncRoot.DisplayName);
        command.Parameters.AddWithValue("$Flag", (long)syncRoot.Flag);
        return command.ExecuteNonQuery() == 1;
      }
    }
    internal static bool SyncRootDelete(this SyncRoot syncRoot)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _syncroot_delete;
        command.Parameters.AddWithValue("$Id", syncRoot.Id);
        return command.ExecuteNonQuery() == 1;
      }
    }
    internal static int SyncRootClear(this Account account)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _syncroot_clear;
        command.Parameters.AddWithValue("$IdAccount", account.Id);
        return command.ExecuteNonQuery();
      }
    }
    #endregion

    #region CloudItem
    const string _clouditem_create = @"create table if not exists CloudItem(
Id                  CHAR(128)             NOT NULL,
IdAccount           CHAR(32)              NOT NULL,
Name                TEXT                  NOT NULL,
ParentId            CHAR(128),
Size                BIG INT     DEFAULT 0 NOT NULL,
DateCreate          BIG INT     DEFAULT 0 NOT NULL,
DateMod             BIG INT     DEFAULT 0 NOT NULL,
PermissionFlag      BIG INT     DEFAULT 0 NOT NULL,
HashString          CHAR(256),
Shortcut            CHAR(128),
PRIMARY KEY (Id, IdAccount),
FOREIGN KEY(IdAccount) REFERENCES Email(Id));";
    const string _clouditem_select = "select * from CloudItem where Id = $Id and IdAccount = $IdAccount;";
    const string _clouditem_insert_update = "insert into CloudItem(Id,IdAccount,Name,ParentId,Size,DateCreate,DateMod,PermissionFlag,HashString,Shortcut) " +
"values($Id,$IdAccount,$Name,$ParentId,$Size,$DateCreate,$DateMod,$PermissionFlag,$HashString,$Shortcut) on conflict(Id, IdAccount) " +
"do update set Name=$Name, ParentId=$ParentId, Size = $Size, DateCreate = $DateCreate, DateMod = $DateMod, PermissionFlag = $PermissionFlag, "+
      "HashString = $HashString, Shortcut = $Shortcut " +
"where Id = $Id AND IdAccount = $IdAccount;";
    const string _clouditem_delete = "delete from CloudItem where Id = $Id and IdAccount = $IdAccount;";
    const string _clouditem_findchild = "select * from CloudItem where IdAccount = $IdAccount and ParentId = $ParentId;";
    internal static CloudItem CloudItemSelect(string Id, string IdAccount)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
      if (string.IsNullOrEmpty(IdAccount)) throw new ArgumentNullException(nameof(IdAccount));
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditem_select;
        command.Parameters.AddWithValue("$Id", Id);
        command.Parameters.AddWithValue("$IdAccount", IdAccount);
        using (var reader = command.ExecuteReader())
        {
          if (reader.Read()) return ReadCloudItem(reader);
          else return null;
        }
      }
    }
    internal static bool CloudItemInsertUpadte(this CloudItem cloudItem)
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditem_insert_update;
        command.Parameters.AddWithValue("$Id", cloudItem.Id);
        command.Parameters.AddWithValue("$IdAccount", cloudItem.IdAccount);
        command.Parameters.AddWithValue("$Name", cloudItem.Name);
        command.Parameters.AddWithValue("$ParentId", cloudItem.ParentId);
        command.Parameters.AddWithValue("$Size", cloudItem.Size);
        command.Parameters.AddWithValue("$DateCreate", cloudItem.DateCreate);
        command.Parameters.AddWithValue("$DateMod", cloudItem.DateMod);
        command.Parameters.AddWithValue("$PermissionFlag", (long)cloudItem.PermissionFlag);
        command.Parameters.AddWithValue("$HashString", cloudItem.HashString);
        command.Parameters.AddWithValue("$Shortcut", cloudItem.Shortcut);
        lock (_lock) return command.ExecuteNonQuery() == 1;
      }
    }
    internal static bool CloudItemDelete(string Id, string IdAccount)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
      if (string.IsNullOrEmpty(IdAccount)) throw new ArgumentNullException(nameof(IdAccount));
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditem_delete;
        command.Parameters.AddWithValue("$Id", Id);
        command.Parameters.AddWithValue("$IdAccount", IdAccount);
        return command.ExecuteNonQuery() == 1;
      }
    }
    internal static IList<CloudItem> CloudItemFindChildIds(string ParentId, string IdAccount)
    {
      if (string.IsNullOrEmpty(ParentId)) throw new ArgumentNullException(nameof(ParentId));
      if (string.IsNullOrEmpty(IdAccount)) throw new ArgumentNullException(nameof(IdAccount));

      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditem_findchild;
        command.Parameters.AddWithValue("$IdAccount", IdAccount);
        command.Parameters.AddWithValue("$Parent",  ParentId);
        List<CloudItem> cis = new List<CloudItem>();
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read()) cis.Add(ReadCloudItem(reader));
        }
        return cis;
      }
    }
    static CloudItem ReadCloudItem(SQLiteDataReader reader)
    {
      return new CloudItem
      {
        Id = reader.GetString(0),
        IdAccount = reader.GetString(1),
        Name = reader.GetString(2),
        ParentId = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),//nullable
        Size = reader.GetInt64(4),
        DateCreate = reader.GetInt64(5),
        DateMod = reader.GetInt64(6),
        PermissionFlag = (CloudItemPermissionFlag)reader.GetInt32(7),
        HashString = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),//nullable
        Shortcut = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)//nullable
      };
    }
    #endregion

    #region CloudItemAction
    const string _clouditemaction_create = @"create table if not exists CloudItemAction(
Id                  CHAR(128)             NOT NULL,
IdAccount           CHAR(32)              NOT NULL,
Flag                BIG INT     DEFAULT 0 NOT NULL,
PRIMARY KEY (Id, IdAccount),
FOREIGN KEY(Id) REFERENCES CloudItem(Id),
FOREIGN KEY(IdAccount) REFERENCES Email(Id));";
    const string _clouditemaction_insertupdate = "insert into CloudItemAction(Id,IdAccount,Flag) " +
      "values($Id,$IdAccount,$Flag) on conflict(Id, IdAccount) " +
      "do update set Flag = $Flag where Id = $Id AND IdAccount = $IdAccount;";
    const string _clouditemaction_selectInAccount = "select * from CloudItemAction where IdAccount = $IdAccount;";
    const string _clouditemaction_delete = "delete from CloudItemAction where Id = $Id and IdAccount = $IdAccount;";
    internal static IList<CloudItemAction> CloudItemActionSelect(string IdAccount)
    {
      if (string.IsNullOrEmpty(IdAccount)) throw new ArgumentNullException(nameof(IdAccount));
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditemaction_selectInAccount;
        command.Parameters.AddWithValue("$IdAccount", IdAccount);
        using (var reader = command.ExecuteReader())
        {
          List<CloudItemAction> cias = new List<CloudItemAction>();
          while (reader.Read())
          {
            cias.Add(new CloudItemAction
            {
              Id = reader.GetString(0),
              IdAccount = IdAccount,
              Flag = (CloudItemActionFlag)reader.GetInt64(2)
            });
          }
          return cias;
        }
      }
    }
    internal static bool CloudItemActionInsertUpdate(this CloudItemAction itemAction)
    {
      if (null == itemAction) throw new ArgumentNullException(nameof(itemAction));
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditemaction_insertupdate;
        command.Parameters.AddWithValue("$Id", itemAction.Id);
        command.Parameters.AddWithValue("$IdAccount", itemAction.IdAccount);
        command.Parameters.AddWithValue("$Flag", itemAction.Flag);
        lock (_lock) return command.ExecuteNonQuery() == 1;
      }
    }
    internal static bool CloudItemActionDelete(this CloudItemAction itemAction)
    {
      if (null == itemAction) throw new ArgumentNullException(nameof(itemAction));
      if (itemAction.Flag != CloudItemActionFlag.None) return false;
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _clouditemaction_delete;
        command.Parameters.AddWithValue("$Id", itemAction.Id);
        command.Parameters.AddWithValue("$IdAccount", itemAction.IdAccount);
        lock (_lock) return command.ExecuteNonQuery() == 1;
      }
    }
    #endregion

    #region Setting
    const int DBVer = 1;
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
    private static bool SettingInsert()
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _setting_insert;
        command.Parameters.AddWithValue("$FileIgnore", "desktop.ini;");
        lock (_lock) return command.ExecuteNonQuery() == 1;
      }
    }
    internal static void SettingSelect()
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _setting_select;
        using (var reader = command.ExecuteReader())
        {
          if (reader.Read())
          {
            int DBVersion = reader.GetInt32(9);
            if (DBVersion != DBVer) throw new Exception("Different DB version, please uninstall and install again.");
            Setting setting = new Setting();
            //0 is lock
            setting.FileIgnore = reader.GetString(1);
            setting.TryAgainAfter = reader.GetInt32(2);
            setting.TryAgainTimes = reader.GetInt32(3);
            setting.FilesUploadSameTime = reader.GetInt32(4);
            setting.SpeedUploadLimit = reader.GetInt32(5);
            setting.SpeedDownloadLimit = reader.GetInt32(6);
            setting.TimeWatchChangeCloud = reader.GetInt32(7);
            setting.Flag = (SettingFlag)reader.GetInt64(8);
            Setting.SettingData = setting;
          }
          else throw new Exception("Can't read setting.");
        }
      }
    }
    internal static bool UpdateSetting()
    {
      using (var command = _con.CreateCommand())
      {
        command.CommandText = _setting_update;
        command.Parameters.AddWithValue("$FileIgnore", Setting.SettingData.FileIgnore);
        command.Parameters.AddWithValue("$TryAgainAfter", Setting.SettingData.TryAgainAfter);
        command.Parameters.AddWithValue("$TryAgainTimes", Setting.SettingData.TryAgainTimes);
        command.Parameters.AddWithValue("$FilesUploadSameTime", Setting.SettingData.FilesUploadSameTime);
        command.Parameters.AddWithValue("$SpeedUploadLimit", Setting.SettingData.SpeedUploadLimit);
        command.Parameters.AddWithValue("$SpeedDownloadLimit", Setting.SettingData.SpeedDownloadLimit);
        command.Parameters.AddWithValue("$TimeWatchChangeCloud", Setting.SettingData.TimeWatchChangeCloud);
        command.Parameters.AddWithValue("$Flag", (long)Setting.SettingData.Flag);
        return command.ExecuteNonQuery() == 1;
      }
    }
    #endregion
  }
}
