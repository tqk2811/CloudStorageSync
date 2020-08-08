using CssCsData;
using CssCsData.Cloud;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CssCsCloud.Cloud
{
  public class CloudChangeTypeCollection : ICloudChangeTypeCollection
  {
    ICollection<ICloudChangeType> collection = new Collection<ICloudChangeType>();
    public string NewWatchToken { get; internal set; }

    public int Count => collection.Count;

    public bool IsReadOnly => collection.IsReadOnly;

    internal void AddRange(IEnumerable<ICloudChangeType> collection)
    {
      if (null == collection) throw new ArgumentNullException(nameof(collection));
      foreach (var item in collection) this.collection.Add(item);
    }

    public void Add(ICloudChangeType item) => throw new NotImplementedException();
    public void Clear() => throw new NotImplementedException();
    public bool Contains(ICloudChangeType item) => collection.Contains(item);
    public void CopyTo(ICloudChangeType[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);
    public bool Remove(ICloudChangeType item) => throw new NotImplementedException();
    public IEnumerator<ICloudChangeType> GetEnumerator() => collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
  }
  internal class CloudChangeType: ICloudChangeType
  {
    internal CloudChangeType(string Id, IList<string> parent_old, IList<string> parent_new)
    {
      if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id));
      this.Id = Id;
      if (parent_old == null) Flag |= CloudChangeFlag.IsNewItem;
      else ParentsRemove = new List<string>(parent_old);

      if (parent_new != null) ParentsNew = new List<string>(parent_new);

      if (parent_old != null && parent_new != null) CheckParent();
    }
    public CloudChangeFlag Flag { get; set; } = CloudChangeFlag.None;
    public bool IsChangeParent
    {
      get
      {
        if (ParentsCurrent == null || (ParentsRemove.Count == 0 && ParentsNew.Count == 0)) return false;
        return true;
      }
    }

    //public long SQLId { get; set; } = -1;
    public string IdAccount { get; internal set; }
    public string Id { get; internal set; }
    public string IdNew { get; internal set; }
    public IList<string> ParentsRemove { get; } = new List<string>();
    public IList<string> ParentsNew { get; } = new List<string>();
    public IList<string> ParentsCurrent { get; } = new List<string>();
    public CloudItem CiNew { get; internal set; }
    void CheckParent()
    {
      for (int i = 0; i < ParentsRemove.Count; i++)
      {
        for (int j = 0; j < ParentsNew.Count; j++)
        {
          if (ParentsNew[j].Equals(ParentsRemove[i], StringComparison.OrdinalIgnoreCase))
          {
            ParentsCurrent.Add(ParentsRemove[i]);
            ParentsRemove.RemoveAt(i);
            ParentsNew.RemoveAt(j);
            i--;
            j--;
          }
        }
      }
    }
  }
}
