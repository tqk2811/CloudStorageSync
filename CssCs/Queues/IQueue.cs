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
