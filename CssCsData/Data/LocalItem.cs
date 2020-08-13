using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CssCsData
{
  public class LocalItemRoot : LocalItem
  {
    public LocalItemRoot(SyncRootViewModelBase srvm, string CloudId) : base(srvm, CloudId)
    {
      if (null == srvm) throw new ArgumentNullException(nameof(srvm));
      if (string.IsNullOrEmpty(CloudId)) throw new ArgumentNullException(nameof(CloudId));
      base.hashtable = new Hashtable();
      base.IsRoot = true;
      hashtable.Add(CloudId, this);
    }

    public void Remove()
    {
      this.IsRoot = false;
      this.srvm = null;
    }

    public LocalItem FindFromCloudId(string CloudId)
    {
      if (!string.IsNullOrEmpty(CloudId) && hashtable.ContainsKey(CloudId)) return (LocalItem)hashtable[CloudId];
      return null;
    }

    public LocalItem FindFromFullPath(string fullPath)
    {
      if (string.IsNullOrEmpty(fullPath)) return null;
      if (fullPath.Equals(srvm.LocalPath, StringComparison.OrdinalIgnoreCase)) return FindFromCloudId(srvm.SyncRootData.CloudFolderId);
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
      foreach (string name in names)
      {
        result = result.Childs.ToList().Find(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (null == result) return null;
      }
      return result;
    }
  }

  public class LocalItem
  {
    public LocalItem(SyncRootViewModelBase srvm, string CloudId)
    {
      if (string.IsNullOrEmpty(CloudId)) throw new ArgumentNullException(nameof(CloudId));
      this._CloudId = CloudId;
      this.srvm = srvm ?? throw new ArgumentNullException(nameof(srvm));
      _Childs = new LocalItemChildCollection(this);
      ReferenceFrom = new LocalItemReferenceCollection(this);
    }
    protected Hashtable hashtable;//only for root
    protected bool IsRoot = false;
    protected SyncRootViewModelBase srvm;
    protected LocalItemChildCollection _Childs;
    protected string _CloudId;
    public string CloudId
    {
      get
      {
        return _CloudId;
      }
      set
      {
        if (null == ReferenceTo)
        {
          //if this is main
          if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
          if (value.Equals(CloudId, StringComparison.OrdinalIgnoreCase)) return;//if not change id
          if (srvm.Root.hashtable.ContainsKey(CloudId)) srvm.Root.hashtable.Remove(CloudId);//remove id in hash
          else throw new Exception("bug code");

          srvm.Root.hashtable.Add(value, this);
          _CloudId = value;
          foreach (var sub in ReferenceFrom) sub._CloudId = value;
        }
        else ReferenceTo.CloudId = value;//if this is sub -> move to main
      }
    }
    public string Name { get; set; }

    public LocalItem ReferenceTo { get; private set; }//only set from LocalItemReferenceCollection

    public LocalItemReferenceCollection ReferenceFrom { get; }

    public LocalItem Parent { get; private set; }//only set from LocalItemChildCollection

    public LocalItemChildCollection Childs
    {
      get
      {
        if (ReferenceTo == null) return _Childs;
        else return ReferenceTo.Childs;
      }
    }

    public bool IsRemoved
    {
      get
      {
        if (null == Parent) return !IsRoot;
        else return Parent.IsRemoved;
      }
    }

    public StringBuilder GetFullPath()
    {
      return GetRelativePath().Insert(0, srvm.LocalPath + "\\");
    }

    public StringBuilder GetRelativePath()
    {
      if (this.IsRoot) return new StringBuilder();//for root only
      else
      {
        if (null != this.Parent)
        {
          if (this.Parent.IsRoot) return new StringBuilder(this.Name);
          StringBuilder stringBuilder = this.Parent.GetRelativePath();
          if (null != stringBuilder) return stringBuilder.Append("\\").Append(this.Name);
        }
        return null;
      }
    }

    public override string ToString()
    {
      return string.Format("Name: {0} , Id: {1}", this.Name, this.CloudId);
    }

    public sealed class LocalItemChildCollection : Collection<LocalItem>
    {
      LocalItem parent;
      internal LocalItemChildCollection(LocalItem parent)
      {
        this.parent = parent;
      }

      protected override void SetItem(int index, LocalItem item) => throw new NotSupportedException();

      protected override void InsertItem(int index, LocalItem item)
      {
        if (null != item.Parent) throw new Exception("Item has parent.");
        if (!parent.srvm.Equals(item.srvm)) throw new Exception("srvm not equal");
        if (parent.IsRemoved) throw new Exception("Parent is not in root");

        if (parent.srvm.Root.hashtable.ContainsKey(item.CloudId))
        {
          LocalItem main = (LocalItem)parent.srvm.Root.hashtable[item.CloudId];
          main.ReferenceFrom.MyInsertItem(item);//item is sub -> link ref to main
        }
        else parent.srvm.Root.hashtable.Add(item.CloudId, item);//add to hash as main
        item.Parent = this.parent;
        base.InsertItem(index, item);
      }

      protected override void RemoveItem(int index)
      {
        RemoveParentAndRefItem(this[index]);
        base.RemoveItem(index);
      }

      protected override void ClearItems()
      {
        foreach (var item in this) RemoveParentAndRefItem(item);//remove item.parent
        base.ClearItems();
      }

      void MyAddItems(LocalItem item)
      {
        item.Parent = this.parent;
        base.InsertItem(this.Count, item);
      }

      void MoveChildsFromThisToTarget(LocalItemChildCollection target)
      {
        foreach (var item in this) target.MyAddItems(item);
        base.ClearItems();
      }

      void RemoveParentAndRefItem(LocalItem childRemove)
      {
        childRemove.Parent = null;//clear parent
        if (null == childRemove.ReferenceTo)//if main, it link in hash
        {
          childRemove.srvm.Root.hashtable.Remove(childRemove.CloudId);//remove main from hash
          if (childRemove.ReferenceFrom.Count > 0)//if main have sub
          {
            LocalItem newRefTo = childRemove.ReferenceFrom[0];//first sub -> new main
            childRemove._Childs.MoveChildsFromThisToTarget(newRefTo._Childs);//move childs from old main to new main
            parent.srvm.Root.hashtable.Add(newRefTo.CloudId, newRefTo);//add new main to hash
            newRefTo.ReferenceTo = null;//clear ref to old main
            while (1 < childRemove.ReferenceFrom.Count)//move ref from old main to new main
            {
              LocalItem refFrom = childRemove.ReferenceFrom[1];
              childRemove.ReferenceFrom.MyRemoveItemAt(1);
              newRefTo.ReferenceFrom.MyInsertItem(refFrom);
            }
          }
        }
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

      /// <summary>
      /// If find result by Id null, then find with Name.
      /// </summary>
      /// <param name="ci"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentNullException"/>
      public LocalItem Find(CloudItem ci)
      {
        if (null == ci) throw new ArgumentNullException(nameof(ci));
        return Find(ci.Id, ci.Name);
      }
    }

    public sealed class LocalItemReferenceCollection : Collection<LocalItem>
    {
      LocalItem refTo;
      internal LocalItemReferenceCollection(LocalItem refTo)
      {
        this.refTo = refTo;
      }
      protected override void InsertItem(int index, LocalItem item) => throw new NotSupportedException();
      protected override void RemoveItem(int index) => throw new NotSupportedException();
      protected override void ClearItems() => throw new NotSupportedException();
      protected override void SetItem(int index, LocalItem item) => throw new NotSupportedException();

      internal void MyInsertItem(LocalItem item)
      {
        if (null != item.ReferenceTo) throw new Exception("item had ReferenceTo other");
        if (!refTo.srvm.Equals(item.srvm)) throw new Exception("srvm not equal");
        if (!refTo.CloudId.Equals(item.CloudId)) throw new Exception("CloudId not equal");

        item.ReferenceTo = this.refTo;
        base.InsertItem(this.Count, item);
      }

      internal void MyRemoveItemAt(int index)
      {
        this[index].ReferenceTo = null;
        base.RemoveItem(index);
      }

      internal void MyClearItems()
      {
        foreach (var item in this) item.ReferenceTo = null;
        base.ClearItems();
      }

    }
  }
}
