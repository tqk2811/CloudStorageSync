using CssCsData;
using CssCsData.Cloud;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CssCsCloud.Cloud
{
  internal class CloudItemActionCollection : ICloudItemActionCollection
  {
    ICollection<CloudItemAction> collection = new Collection<CloudItemAction>();
    //public string NewWatchToken { get; internal set; }

    public int Count => collection.Count;

    public bool IsReadOnly => collection.IsReadOnly;

    internal void AddRange(IEnumerable<CloudItemAction> collection)
    {
      if (null == collection) throw new ArgumentNullException(nameof(collection));
      foreach (var item in collection) this.collection.Add(item);
    }

    public void Add(CloudItemAction item) => throw new NotImplementedException();
    public void Clear() => throw new NotImplementedException();
    public bool Contains(CloudItemAction item) => collection.Contains(item);
    public void CopyTo(CloudItemAction[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);
    public bool Remove(CloudItemAction item) => throw new NotImplementedException();
    public IEnumerator<CloudItemAction> GetEnumerator() => collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
  }
}
