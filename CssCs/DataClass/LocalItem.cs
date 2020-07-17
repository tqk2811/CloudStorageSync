using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CssCs.DataClass
{
  public enum LocalItemFlag : long
  {
    None = 0,
    Folder = 1,
    LockWaitUpdateFromCloudWatch = 1 << 1,
  }
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
  public class LocalItem
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
  {
    private static List<LocalItem> _lis;
    internal static void Load(IList<LocalItem> lis)
    {
      if (_lis == null && lis != null) _lis = new List<LocalItem>(lis);
      else throw new Exception("Input do not null and call this function only one times.");
    }
    public static LocalItem Find(long LocalId)
    {
      lock (_lis) return _lis.Find((li) => li.LocalId == LocalId);
    }
    public static LocalItem Find(SyncRootViewModel srvm, long LocalParentId, string Name)
    {
      lock (_lis) return _lis.Find((li) =>  li.LocalParentId == LocalParentId && 
                                            li.Name.Equals(Name) &&
                                            li.SRId.Equals(srvm.SRId));
    }
    public static LocalItem Find(SyncRootViewModel srvm, string CloudId, long LocalParentId)
    {
      lock (_lis) return _lis.Find((li) =>  li.LocalParentId == LocalParentId &&
                                            CloudId.Equals(li.CloudId) &&
                                            srvm.SRId.Equals(li.SRId));
    }
    /// <summary>
    /// Find all items in syncroot has CloudId
    /// </summary>
    /// <param name="srvm"></param>
    /// <param name="CloudId"></param>
    /// <returns></returns>
    public static IList<LocalItem> FindAll(SyncRootViewModel srvm, string CloudId)
    {
      if (srvm == null || string.IsNullOrEmpty(CloudId)) return null;
      lock (_lis) return _lis.FindAll((li) => li.SRId.Equals(srvm.SRId) &&
                                              !string.IsNullOrEmpty(li.CloudId) &&
                                              CloudId.Equals(li.CloudId));
    }
    /// <summary>
    /// Find all items in syncroot has CloudIds
    /// </summary>
    /// <param name="srvm"></param>
    /// <param name="CloudIds"></param>
    /// <returns></returns>
    public static IList<LocalItem> FindAll(SyncRootViewModel srvm, IList<string> CloudIds)
    {
      List<LocalItem> lis = new List<LocalItem>();
      CloudIds.ToList().ForEach((cloudid) => lis.AddRange(FindAll(srvm, cloudid)));
      return lis;
    }
    /// <summary>
    /// Find all items has LocalParentId
    /// </summary>
    /// <param name="srvm"></param>
    /// <param name="LocalParentId"></param>
    /// <returns></returns>
    public static IList<LocalItem> FindAll(SyncRootViewModel srvm, long LocalParentId)
    {
      lock (_lis) return _lis.FindAll((li) => li.LocalParentId == LocalParentId && li.SRId.Equals(srvm.SRId));
    }
    public static LocalItem FindFromPath(SyncRootViewModel srvm, string FullPath, int Back = 0)
    {
      if (srvm == null || string.IsNullOrEmpty(FullPath)) return null;
      if (FullPath.Length < srvm.LocalPath.Length || !FullPath.Substring(0, srvm.LocalPath.Length).Equals(srvm.LocalPath)) return null;
      LocalItem result = Find(srvm, srvm.CloudFolderId, 0);//root
      if (FullPath.Length == srvm.LocalPath.Length) return result;

      string relative = FullPath.Substring(srvm.LocalPath.Length + 1);
      string[] names = relative.Split('\\');

      for (int i = 0; i < names.Length - Back; i++)
      {
        result = Find(srvm, result.LocalId, names[i]);
        if (result == null) return null;
      }
      return result;
    }
    public static void Clear(SyncRootViewModel srvm)
    {
      SqliteManager.LIClear(srvm.SRId);
      lock (_lis) _lis.RemoveAll((li) => li.SRId.Equals(srvm.SRId));
    }





    public LocalItem() { }
    internal LocalItem(bool Inserted = true)
    {
      this.Inserted = Inserted;
    }
    public long LocalId { get; set; } = -1;
    public long LocalParentId { get; set; } = -1;
    public string Name { get; set; }
    string _SRId;
    public string SRId
    {
      get { return _SRId; }
      set { _SRId = value; SRVM = SyncRootViewModel.Find(value); }
    }
    public string CloudId { get; set; }
    public LocalItemFlag Flag { get; set; } = LocalItemFlag.None;
    public SyncRootViewModel SRVM { get; private set; } = null;



    object lock_obj = new object();

    internal bool Inserted { get; private set; } = false;
    internal bool Deleted { get; private set; } = false;

    public void AddFlagWithLock(LocalItemFlag FlagAdd)
    {
      lock (lock_obj) Flag &= FlagAdd;
    }
    public void RemoveFlagWithLock(LocalItemFlag FlagRemove)
    {
      lock (lock_obj) Flag ^= FlagRemove;
    }
    public StringBuilder GetRelativePath()
    {
      if (LocalParentId > 0)
      {
        LocalItem parent = Find(LocalParentId);
        if (parent != null)
        {
          StringBuilder sb = parent.GetRelativePath();
          if (sb != null)
          {
            if (sb.Length == 0) return sb.Append(Name);
            else return sb.Append("\\").Append(Name);
          }
        }
        return null;
      }
      else if (LocalParentId == 0) return new StringBuilder();
      else return null;
    }
    public string GetFullPath()
    {
      StringBuilder sb = GetRelativePath();
      if (sb == null) return null;
      if (sb.Length == 0) return SRVM.LocalPath;
      else return sb.Insert(0, "\\").Insert(0, SRVM.LocalPath).ToString();
    }
    public override bool Equals(object obj)
    {
      if (obj == null) return false;
      if (obj.GetType().Equals(typeof(LocalItem)) && LocalId == ((LocalItem)obj).LocalId) return true;
      return base.Equals(obj);
    }

    public void Insert()
    {
      if (Inserted) return;
      SqliteManager.LIInsert(this);
      lock (_lis) _lis.Add(this);
      Inserted = true;
    }

    public void Update()
    {
      if(Inserted && !Deleted) SqliteManager.LIUpdate(this);
    }

    public void Delete(bool Childs = true)
    {
      if (Deleted) return;
      if(Childs)
      {
        IList<LocalItem> lis_child = FindAll(SRVM, LocalId);
        for (int i = 0; i < lis_child.Count; i++) lis_child[i].Delete();
      }
      SqliteManager.LIDelete(this);
      lock (_lis) _lis.Remove(this);
      Deleted = true;
    }


  }
}
