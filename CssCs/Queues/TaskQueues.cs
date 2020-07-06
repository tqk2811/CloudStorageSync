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
  public sealed class TaskQueues
  {
    public static TaskQueues UploadQueues { get; internal set; } = new TaskQueues();

    System.Timers.Timer timer;
    List<IQueue> Queues;
    List<IQueue> Runnings;
    int timeloop = 2000;
		bool runtask = true;
    List<Task> RunningTasks = new List<Task>();

    public int MaxRun { get; set; } = 1;
    internal TaskQueues()
    {
      this.Queues = new List<IQueue>();
      this.Runnings = new List<IQueue>();
      Task.Factory.StartNew(OnStartTask, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }


    
    void OnStartTask()
    {
      while(runtask)
      {
        if (Queues.Count > 0)
        {
          IQueue queue;
          lock (Queues)
          {
            queue = Queues[0];
            Queues.RemoveAt(0);
          }
          lock (Runnings) Runnings.Add(queue);

          Task task = queue.DoWork();
          task.ContinueWith((Task t) => { lock (Runnings) Runnings.Remove(queue); });//auto clear running

          RunningTasks.Add(task);
          if(RunningTasks.Count >= MaxRun)
          {
            Task.WaitAny(RunningTasks.ToArray());
            RunningTasks.RemoveAll((t) => t.IsCompleted || t.IsFaulted || t.IsCanceled);
          }
        }
        else Task.Delay(timeloop).Wait();
      }
    }

    public void Add(IQueue queue)
    {
      if (queue.IsPrioritize) queue.DoWork();
      else lock (Queues)
        {
          bool flag = true;
          if (Queues.Any(o => o.Check(queue))) flag = false;
          else
          {
            lock(Runnings)
            {
              List<IQueue> runs = Runnings.FindAll(o => o.Check(queue));
              for (int i = 0; i < runs.Count; i++)
              {                
                runs[i].Cancel();
                if (flag)
                {
                  flag = false;
                  Queues.Add(queue);
                }
              }
            }
          }
          if (flag) Queues.Add(queue);
        }
    }

    public void Cancel(IQueue queue)
    {
      lock (Queues)
      {
        Queues.RemoveAll(o => o.Check(queue));
      }
      lock(Runnings)
      {
        Runnings.ForEach(o => { if (o.Check(queue)) o.Cancel(); });
      }
    }

    public void Reset(IQueue queue)
    {
      Cancel(queue);
      Add(queue);
    }

    public void ShutDown()
    {
      lock (Queues) Queues.Clear();
      runtask = false;
    }
  }
}
