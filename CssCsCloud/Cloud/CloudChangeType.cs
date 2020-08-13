using CssCsData;
using CssCsData.Cloud;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CssCsCloud.Cloud
{
  internal class CloudChangeTypeCollection : ICloudChangeTypeCollection
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
  internal class CloudChangeType : ICloudChangeType
  {
    internal CloudChangeType(CloudItem CloudItemOld, CloudItem CloudItemNew)
    {
      this.CloudItemNew = CloudItemNew;
      this.CloudItemOld = CloudItemOld;

      if (null == CloudItemNew) Flag |= CloudChangeFlag.Deleted;
      else if (null == CloudItemOld) Flag |= CloudChangeFlag.NewItem;
      else
      {
        //only google drive can change id
        if (!CloudItemOld.Id.Equals(CloudItemNew.Id, StringComparison.OrdinalIgnoreCase)) Flag |= CloudChangeFlag.ChangedId;

        //only root or shared item has ParentId = null
        if (  !string.IsNullOrEmpty(CloudItemOld.ParentId) && 
              !CloudItemOld.ParentId.Equals(CloudItemNew.ParentId, StringComparison.OrdinalIgnoreCase)) Flag |= CloudChangeFlag.ChangedParent;

        if( CloudItemOld.DateCreate != CloudItemNew.DateCreate || 
            CloudItemOld.DateMod != CloudItemNew.DateMod || 
            CloudItemOld.Size != CloudItemNew.Size) Flag |= CloudChangeFlag.ChangeTimeAndSize;

        //need test, root name can null
        if( !string.IsNullOrEmpty(CloudItemOld.Name) && 
            !CloudItemOld.Name.Equals(CloudItemNew.Name)) Flag |= CloudChangeFlag.Rename;
      }
    }

    public CloudItem CloudItemNew { get; }

    public CloudItem CloudItemOld { get; }

    public CloudChangeFlag Flag { get; } = CloudChangeFlag.None;
  }
}
