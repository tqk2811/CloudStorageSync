using CssCs.UI.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CssCs.Queues
{
  public interface IQueue
  {
    bool IsPrioritize { get; } 
    Task DoWork();
    bool Check(IQueue queue);
    void Cancel();
  }  
  public static class TaskQueues
  {
    static List<IQueue> Queues = new List<IQueue>();
    static List<IQueue> Runnings = new List<IQueue>();

    static int _MaxRun = 1;
    public static int MaxRun
    {
      get { return _MaxRun; }
      set
      {
        bool flag = value > _MaxRun;
        _MaxRun = value; 
        if(flag) RunNewQueue();
      }
    }    

    static void ContinueTaskResult(Task Result,object queue_obj)
    {
      IQueue queue = queue_obj as IQueue;
      lock (Runnings) Runnings.Remove(queue);
      RunNewQueue();
    }

    static void RunNewQueue()
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


    public static void Add(IQueue queue)
    {
      if (queue.IsPrioritize) queue.DoWork();
      else
      {
        lock (Queues) Queues.Add(queue);
        RunNewQueue();
      }
    }

    public static void Cancel(IQueue queue)
    {
      lock (Queues) Queues.RemoveAll(o => o.Check(queue));
      lock (Runnings) Runnings.ForEach(o => { if (o.Check(queue)) o.Cancel(); });
    }

    public static void Reset(IQueue queue)
    {
      Cancel(queue);
      Add(queue);
    }

    public static void ShutDown()
    {
      MaxRun = 0;
      lock (Runnings) Runnings.ForEach(o => o.Cancel());
    }
  }
}
