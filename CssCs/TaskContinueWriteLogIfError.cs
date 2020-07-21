using System;
using System.Threading.Tasks;

namespace CssCs
{
  internal class TaskContinueWriteLogIfError
  {
    string message;
    public TaskContinueWriteLogIfError(string message)
    {
      if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
      this.message = message;
    }

    public void Check(Task t)
    {
      if (t.Status.HasFlag(TaskStatus.Faulted)) CPPCLR_Callback.OutPutDebugString(message +
                                                      ", TaskStatus:" + TaskStatus.Faulted.ToString() +
                                                      ", Exception Message:" + t.Exception.InnerException.Message +
                                                      ", Exception StackTrace" + t.Exception.InnerException.StackTrace, 0);

      else if (t.Status.HasFlag(TaskStatus.Canceled)) CPPCLR_Callback.OutPutDebugString(message + ", TaskStatus:" + TaskStatus.Canceled.ToString(), 1);
    }
  }

  internal class TaskContinueWriteLogIfError<T>
  {
    string info;
    public TaskContinueWriteLogIfError(string info)
    {
      if (string.IsNullOrEmpty(info)) throw new ArgumentNullException(nameof(info));
      this.info = info;
    }

    public void Check(Task<T> t)
    {
      if (t.Status.HasFlag(TaskStatus.Faulted)) CPPCLR_Callback.OutPutDebugString(info +
                                                      ", TaskStatus:" + TaskStatus.Faulted.ToString() +
                                                      ", Exception Message:" + t.Exception.InnerException.Message +
                                                      ", Exception StackTrace" + t.Exception.InnerException.StackTrace, 0);
      else if (t.Status.HasFlag(TaskStatus.Canceled)) CPPCLR_Callback.OutPutDebugString(info + ", TaskStatus:" + TaskStatus.Canceled.ToString(), 1);
    }    
  }
}
