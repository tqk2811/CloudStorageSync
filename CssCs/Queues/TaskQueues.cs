using CssCs.UI.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CssCs.Queues
{
  public class TaskQueues
  {
    public static TaskQueues UploadQueues { get; } = new TaskQueues();



    List<IQueue> Queues = new List<IQueue>();
    List<IQueue> Runnings = new List<IQueue>();
    int _MaxRun = 1;
    public int MaxRun
    {
      get { return _MaxRun; }
      set
      {
        bool flag = value > _MaxRun;
        _MaxRun = value; 
        if(flag) RunNewQueue();
      }
    }    

    void ContinueTaskResult(Task Result,object queue_obj)
    {
      IQueue queue = queue_obj as IQueue;
      lock (Runnings) Runnings.Remove(queue);
      RunNewQueue();
    }

    void RunNewQueue()
    {
      if (Runnings.Count >= MaxRun || Queues.Count == 0) return;
      lock(Queues)
      {
        IQueue queue = Queues[0];
        Queues.RemoveAt(0);
        lock (Runnings) Runnings.Add(queue);
        Task work = queue.DoWork();
        work.ContinueWith(ContinueTaskResult, queue);
      }
    }


    public void Add(IQueue queue)
    {
      if (queue.IsPrioritize) queue.DoWork();
      else
      {
        lock (Queues) Queues.Add(queue);
        RunNewQueue();
      }
    }

    public void Cancel(IQueue queue)
    {
      lock (Queues) Queues.RemoveAll(o => o.Check(queue));
      lock (Runnings) Runnings.ForEach(o => { if (o.Check(queue)) o.Cancel(); });
    }

    public void Reset(IQueue queue)
    {
      Cancel(queue);
      Add(queue);
    }

    public void ShutDown()
    {
      MaxRun = 0;
      lock (Runnings) Runnings.ForEach(o => o.Cancel());
    }
  }
}
