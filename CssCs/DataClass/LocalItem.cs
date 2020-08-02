using CssCs.UI.ViewModel;
using Microsoft.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CssCs.DataClass
{
  public class LocalItemRoot : LocalItem
  {
    public LocalItemRoot(SyncRootViewModel srvm, string CloudId) : base(srvm, CloudId)
    {
      base.hashtable = new Hashtable();
      base.IsRoot = true;
    }

    public LocalItem FindFromCloudId(string CloudId)
    {
      if (!string.IsNullOrEmpty(CloudId) && hashtable.ContainsKey(CloudId)) return (LocalItem)hashtable[CloudId];
      return null;
    }

    public LocalItem FindFromFullPath(string fullPath)
    {
      if (string.IsNullOrEmpty(fullPath)) return null;
      if (fullPath.Equals(srvm.LocalPath, StringComparison.OrdinalIgnoreCase)) return FindFromCloudId(srvm.CloudFolderId);
      if (fullPath.Length > srvm.LocalPath.Length && fullPath.Substring(0, srvm.LocalPath.Length).Equals(srvm.LocalPath, StringComparison.OrdinalIgnoreCase))
      {
        return FindFromRelativePath(fullPath.Substring(srvm.LocalPath.Length));
      }
      else return null;
    }

    public LocalItem FindFromRelativePath(string relativePath)
    {
      string[] names = relativePath.Split('\\');
      LocalItem result = this;
      foreach(string name in names)
      {
        result = result.Childs.ToList().Find(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (null == result) return null;
      }
      return result;
    }
  }


  public class LocalItem
  {
    public LocalItem(SyncRootViewModel srvm,string CloudId)
    {
      if (null == srvm) throw new ArgumentNullException(nameof(srvm));
      if (string.IsNullOrEmpty(CloudId)) throw new ArgumentNullException(nameof(CloudId));
      this.CloudId = CloudId;
      this.srvm = srvm;
      if (srvm.Root.hashtable.ContainsKey(CloudId))
      {
        LocalItem reftarget = (LocalItem)srvm.Root.hashtable[CloudId];
        reftarget.ReferenceFrom.Add(this);
      }
      else
      {
        srvm.Root.hashtable.Add(CloudId, this);
        _Childs = new LocalItemChildCollection(this);
      }
      ReferenceFrom = new LocalItemReferenceCollection(this);
    }
    string _CloudId;
    public string CloudId
    {
      get { return _CloudId; }
      set
      {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        this._CloudId = value;
      }
    }
    public string Name { get; set; }

    public LocalItem ReferenceTo { get; private set; }//only set from LocalItemReferenceCollection
    public LocalItemReferenceCollection ReferenceFrom { get; }

    public LocalItem Parent { get; private set; }//only set from LocalItemChildCollection
    LocalItemChildCollection _Childs;
    public LocalItemChildCollection Childs => _Childs ?? (ReferenceTo == null ? null : Childs);

    protected Hashtable hashtable;//only for root
    protected bool IsRoot = false;
    protected SyncRootViewModel srvm;

    public StringBuilder GetFullPath()
    {
      return GetRelativePath().Insert(0, srvm.LocalPath);
    }

    public StringBuilder GetRelativePath()
    {
      if (this.IsRoot) return new StringBuilder();//for root only
      else
      {
        if(null != this.Parent)
        {
          if (this.Parent.IsRoot) return new StringBuilder(this.Name);
          StringBuilder stringBuilder = this.Parent.GetRelativePath();
          if(null != stringBuilder) return stringBuilder.Append("\\").Append(this.Name);
        }
        return null;
      }
    }


    public sealed class LocalItemChildCollection : Collection<LocalItem>
    {
      LocalItem parent;
      internal LocalItemChildCollection(LocalItem parent)
      {
        this.parent = parent;
      }

      protected override void InsertItem(int index, LocalItem item)
      {
        if (null != item.Parent) throw new Exception("Item has parent.");
        if (!parent.srvm.Equals(item.srvm)) throw new Exception("srvm not equal");

        item.Parent = this.parent;
        base.InsertItem(index, item);
      }

      protected override void RemoveItem(int index)
      {
        RemoveParentAndRefItem(this[index]);
        base.RemoveItem(index);
      }
      protected override void SetItem(int index, LocalItem item)
      {
        if (null != item.Parent) throw new Exception("Item has parent.");
        if (!parent.srvm.Equals(item.srvm)) throw new Exception("srvm not equal");

        this[index].Parent = null;
        item.Parent = this.parent;
        base.SetItem(index, item);
      }

      void RemoveParentAndRefItem(LocalItem item)
      {
        item.Parent = null;
        if(null == item.ReferenceTo)//if not 2nd
        {
          item.srvm.Root.hashtable.Remove(item.CloudId);//remove hash
          if (item.ReferenceFrom.Count > 0)//change base ref (link to hash)
          {
            LocalItem newRefTo = item.ReferenceFrom[0];
            item.srvm.Root.hashtable.Add(newRefTo.CloudId, newRefTo);//add hash
            while (1 < item.ReferenceFrom.Count)
            {
              LocalItem refFrom = item.ReferenceFrom[1];
              item.ReferenceFrom.RemoveAt(1);
              newRefTo.ReferenceFrom.Add(refFrom);
            }
          }
        }        
      }

      protected override void ClearItems()
      {
        foreach (var item in this) RemoveParentAndRefItem(item);
        base.ClearItems();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="Name"></param>
      /// <returns>LocalItem</returns>
      /// <exception cref="ArgumentNullException"/>
      public LocalItem FindFromName(string Name)
      {
        if (string.IsNullOrEmpty(Name)) throw new ArgumentNullException(nameof(Name));
        return this.ToList().Find(item => Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="Id"></param>
      /// <returns>LocalItem</returns>
      /// <exception cref="ArgumentNullException"/>
      public LocalItem FindFromId(string Id)
      {
        if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
        return this.ToList().Find(item => Id.Equals(item.CloudId, StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>
      /// If find result by Id null, then find with Name.
      /// </summary>
      /// <param name="Id"></param>
      /// <param name="Name"></param>
      /// <returns>LocalItem</returns>
      /// <exception cref="ArgumentNullException"/>
      public LocalItem Find(string Id, string Name)
      {
        LocalItem li = FindFromId(Id);
        if (null == li) li = FindFromName(Name);
        return li;
      }
    }

    public sealed class LocalItemReferenceCollection : Collection<LocalItem>
    {
      LocalItem refTo;
      internal LocalItemReferenceCollection(LocalItem refTo)
      {
        this.refTo = refTo;
      }
      protected override void InsertItem(int index, LocalItem item)
      {
        if (null != item.ReferenceTo) throw new Exception("item had ReferenceTo other");
        if (!refTo.srvm.Equals(item.srvm)) throw new Exception("srvm not equal");
        if (!refTo.CloudId.Equals(item.CloudId)) throw new Exception("CloudId not equal");

        item.ReferenceTo = this.refTo;
        base.InsertItem(index, item);
      }

      protected override void RemoveItem(int index)
      {
        this[index].ReferenceTo = null;
        base.RemoveItem(index);
      }

      protected override void ClearItems()
      {
        foreach (var item in this) item.ReferenceTo = null;
        base.ClearItems();
      }

      protected override void SetItem(int index, LocalItem item)
      {
        if (null != item.ReferenceTo) throw new Exception("item had ReferenceTo other");
        if (!refTo.CloudId.Equals(item.CloudId)) throw new Exception("CloudId not equal");
        if (!refTo.srvm.Equals(item.srvm)) throw new Exception("srvm not equal");

        this[index].ReferenceTo = null;
        base.SetItem(index, item);
      }
    }
  }
}
