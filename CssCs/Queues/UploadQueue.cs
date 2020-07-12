using CssCs.DataClass;
using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CssCs.Queues
{
  public sealed class UploadQueue : IQueue
  {
    public UploadQueue(SyncRootViewModel srvm, LocalItem li)
    {
      this.li = li;
      this.srvm = srvm;
      source = new CancellationTokenSource();
      token = source.Token;
    }

    ~UploadQueue()
    {
      source.Dispose();
    }
    SyncRootViewModel srvm;
    LocalItem li;
    CancellationTokenSource source;
    CancellationToken token;
    Task task;
    int tryagain = 0;
    public bool IsPrioritize { get; set; } = false;

    public Task DoWork()
    {
      if(task == null) task = Task.Factory.StartNew(Work, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
      return task; 
    }

    public bool Check(IQueue queue)
    {
      UploadQueue uploadQueue = queue as UploadQueue;
      if (uploadQueue != null && uploadQueue.li.LocalId == li.LocalId) return true;
      return false;
    }

    public void Cancel()
    {
      CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: canceling, filepath:{0}",li.GetFullPath()), 1);
      source.Cancel();
    }

    async void Work()
    {
      if ((int)srvm.CEVM.CloudName > 200) return;
      string fullpath = string.Empty;
      
      try
      {
        fullpath = li.GetFullPath();
        if (!Directory.Exists(fullpath) && !File.Exists(fullpath)) return;

        LocalItem parent_li = LocalItem.Find(li.LocalParentId);
        if(string.IsNullOrEmpty(parent_li.CloudId))//parent not have id -> TryAgain (wait parent created in cloud)
        {
          tryagain++;
          if (tryagain > Settings.Setting.TryAgainTimes)
          {
            CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: Upload cancel because cloud folder hasn't id, path:{0}", fullpath), 1);
            return;
          }
          Task.Delay(Settings.Setting.TryAgainAfter * 1000).ContinueWith((Task task) => TaskQueues.UploadQueues.Add(this));
          CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: Upload try again because cloud folder hasn't id, path:{0}", fullpath), 1);
          return;
        }
        else
        {
          CloudItem ci_parent = CloudItem.Select(parent_li.CloudId, srvm.CEVM.Sqlid);
          if (!ci_parent.CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag.CanAddChildren))
          {
            CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: Upload cancel because cloud folder hasn't permision for add child, path:{0}", fullpath), 1);
            return;//folder can't add child (because user not have permission)
          }
        }

        bool isNewUpload = string.IsNullOrEmpty(li.CloudId);        
        if(!isNewUpload)
        {
          CloudItem ci = CloudItem.Select(li.CloudId, srvm.CEVM.Sqlid);
          if (srvm.CEVM.Cloud.HashCheck(fullpath, ci))
          {
            CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: Upload cancel because same hash, path:{0}", fullpath), 1);
            return;//if upload revision -> check hash before upload -> if hash equal > skip
          }
        }
        CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: Starting Upload, path:{0}", fullpath), 1);
        CloudItem ci_back = await srvm.CEVM.Cloud.Upload(fullpath, new List<string>() { parent_li.CloudId }, li.CloudId);
        if(ci_back != null)
        {
          CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: success, path:{0}, Id:{1}", fullpath, ci_back.Id), 1);
          li.CloudId = ci_back.Id;
          if (isNewUpload) li.AddFlagWithLock(LocalItemFlag.LockWaitUpdateFromCloudWatch);//CSS::Placeholders::CreateItem will release it (only for new item)
          li.Update();
          if (isNewUpload) CPPCLR_Callback.ConvertToPlaceholder(srvm, li, ci_back.Id);
          else CPPCLR_Callback.UpdatePlaceholder(srvm, li, ci_back);//revision
        }
        else
        {
          CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue: Failed, CloudItemTransfer.Upload return null, path:{0}", fullpath), 1);
        }
      }
      catch(AggregateException ae)
      {
        if(ae.InnerException is IOException)
        {
          IOException ioex = ae.InnerException as IOException;
          uint hresult = (uint)ioex.HResult;
          if (hresult == 0x80070020)//can't open file because other process opening (not share read) -> re-queue
          {
            Task t = Task.Delay(Settings.Setting.TryAgainAfter);
            t.ContinueWith((Task task) => TaskQueues.UploadQueues.Add(this));
          }
        }
        CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue.DoWork: Exception, Message:{0}",ae.InnerException.Message));
      }
      catch (Exception ex)
      {
        CPPCLR_Callback.OutPutDebugString(string.Format("UploadQueue.DoWork: Exception, path:{0}, Message:{1}, StackTrace:{2}", fullpath, ex.Message, ex.StackTrace), 1);
      }
    }    

    //public string RunTest(CFViewModel srvm,string fullpath, string fileid)
    //{
    //  return CloudItemTransfer.Upload(fullpath, srvm.CEVM, null, fileid).Result;
    //}
  }
}
