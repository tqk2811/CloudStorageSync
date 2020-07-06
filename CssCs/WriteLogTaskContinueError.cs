using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCs
{
  public class WriteLogTaskContinueError
  {
    string info;
    public WriteLogTaskContinueError(string info)
    {
      if (string.IsNullOrEmpty(info)) throw new ArgumentNullException("info");
      this.info = info;
    }

    public void Check(Task t)
    {
      if (t.Status.HasFlag(TaskStatus.Faulted)) CPPCLR_Callback.OutPutDebugString(info +
                                                      ", TaskStatus:" + TaskStatus.Faulted.ToString() +
                                                      ", Exception Message:" + t.Exception.InnerException.Message +
                                                      ", Exception StackTrace" + t.Exception.InnerException.StackTrace, 0);
      else if (t.Status.HasFlag(TaskStatus.Canceled)) CPPCLR_Callback.OutPutDebugString(info + ", TaskStatus:" + TaskStatus.Faulted.ToString(), 1);

    }
  }

  public class WriteLogTaskContinueError<T>
  {
    string info;
    public WriteLogTaskContinueError(string info)
    {
      if (string.IsNullOrEmpty(info)) throw new ArgumentNullException("info");
      this.info = info;
    }

    public void Check(Task<T> t)
    {
      if (t.Status.HasFlag(TaskStatus.Faulted)) CPPCLR_Callback.OutPutDebugString(info +
                                                      ", TaskStatus:" + TaskStatus.Faulted.ToString() +
                                                      ", Exception Message:" + t.Exception.InnerException.Message +
                                                      ", Exception StackTrace" + t.Exception.InnerException.StackTrace, 0);
      else if (t.Status.HasFlag(TaskStatus.Canceled)) CPPCLR_Callback.OutPutDebugString(info + ", TaskStatus:" + TaskStatus.Faulted.ToString(), 1);
    }
  }
}
