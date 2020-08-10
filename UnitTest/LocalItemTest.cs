using System;
using System.ComponentModel;
using CssCs;
using CssCsData;
using CssCsData.Cloud;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
  public class SyncRootViewModel : SyncRootViewModelBase
  {
    public SyncRootViewModel(SyncRoot syncRoot):base(syncRoot)
    {
      Root = new LocalItemRoot(this, "0");
    }

    public SyncRoot SyncRootData => throw new NotImplementedException();

    public LocalItemRoot Root { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    public override void UpdateChange(ICloudChangeType change)
    {
      throw new NotImplementedException();
    }
  }



  [TestClass]
  public class LocalItemTest
  {
    //0 |-> 0.0 |-> 0.0.0 |-> 0.0.0.0
    //  |       |-> 0.0.1
    //  |
    //  |-> 0.1 |-> (0.0.0)
    //  |-> 0.2 |-> (0.0.0)
    //note (x) is ref to x
    SyncRootViewModel Init()
    {
      SyncRoot syncRoot = new SyncRoot(Extensions.RandomString(32), Extensions.RandomString(32));
      SyncRootViewModel srvm = new SyncRootViewModel(syncRoot);
      srvm.Root.Childs.Add(new LocalItem(srvm, "0.0"));
      srvm.Root.Childs[0].Childs.Add(new LocalItem(srvm, "0.0.0"));
      srvm.Root.Childs[0].Childs[0].Childs.Add(new LocalItem(srvm, "0.0.0.0"));
      srvm.Root.Childs[0].Childs.Add(new LocalItem(srvm, "0.0.1"));
      srvm.Root.Childs.Add(new LocalItem(srvm, "0.1"));
      srvm.Root.Childs[1].Childs.Add(new LocalItem(srvm, "0.0.0"));
      srvm.Root.Childs.Add(new LocalItem(srvm, "0.2"));
      srvm.Root.Childs[2].Childs.Add(new LocalItem(srvm, "0.0.0"));
      return srvm;
    }

    [TestMethod]
    public void TestChangeId()
    {
      SyncRootViewModel srvm = Init();
      //ref from
      srvm.Root.Childs[1].Childs[0].CloudId = "0.0.0x";
      Assert.AreEqual(srvm.Root.FindFromCloudId("0.0.0x"), srvm.Root.Childs[1].Childs[0].ReferenceTo);
      Assert.IsNull(srvm.Root.FindFromCloudId("0.0.0"));
      Assert.AreEqual(srvm.Root.Childs[0].Childs[0].CloudId, "0.0.0x");
      Assert.IsNull(srvm.Root.FindFromCloudId("0.0.0"));
      Assert.IsNotNull(srvm.Root.FindFromCloudId("0.0.0x"));

      //ref to
      srvm.Root.Childs[0].Childs[0].CloudId = "0.0.0";
      Assert.AreEqual(srvm.Root.FindFromCloudId("0.0.0"), srvm.Root.Childs[0].Childs[0]);
      Assert.AreEqual(srvm.Root.Childs[1].Childs[0].CloudId, srvm.Root.FindFromCloudId("0.0.0").CloudId);

      //root
      srvm.Root.CloudId = "0x";
      Assert.AreEqual(srvm.Root.FindFromCloudId("0x"), srvm.Root);
      Assert.IsNull(srvm.Root.FindFromCloudId("0"));

      //non rot and none ref
      srvm.Root.Childs[0].CloudId = "0.0x";
      Assert.AreEqual(srvm.Root.FindFromCloudId("0.0x"), srvm.Root.Childs[0]);
      Assert.IsNull(srvm.Root.FindFromCloudId("0.0"));
    }

    [TestMethod]
    public void TestRemoveWithRef()
    {
      SyncRootViewModel srvm = Init();
      //remove 0.0.0
      //(0.0.0) will be 0.0.0
      LocalItem itemRemoved = srvm.Root.Childs[0].Childs[0];
      LocalItem childOfRemoved = itemRemoved.Childs[0];
      Assert.IsTrue(itemRemoved.Childs.Count == 1);
      itemRemoved.Parent.Childs.Remove(itemRemoved);
      Assert.IsTrue(itemRemoved.Childs.Count == 0);//check childs in remove is clear?
      Assert.IsNull(itemRemoved.Parent);//old base will remove parent
      
      Assert.IsTrue(srvm.Root.Childs[0].Childs.Count == 1);//only one child 0.0.1
      Assert.IsNull(srvm.Root.Childs[1].Childs[0].ReferenceTo);//this is new base
      Assert.AreEqual(srvm.Root.Childs[1].Childs[0], srvm.Root.FindFromCloudId("0.0.0"));//check new base is in hash?
      Assert.IsNotNull(srvm.Root.Childs[1].Childs[0].ReferenceFrom.Count == 1);//check this is ref to base?
      Assert.AreEqual(srvm.Root.Childs[2].Childs[0].ReferenceTo, srvm.Root.Childs[1].Childs[0]);//check is ref to new base
      Assert.AreEqual(childOfRemoved, srvm.Root.FindFromCloudId("0.0.0").Childs[0]);//check child is in new base?
    }
  }
}
