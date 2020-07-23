using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
