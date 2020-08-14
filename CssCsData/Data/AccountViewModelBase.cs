using CssCsData.Cloud;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CssCsData
{
  public abstract class AccountViewModelBase : INotifyPropertyChanged
  {
    public AccountViewModelBase(Account account)
    {
      if (null == account) throw new ArgumentNullException(nameof(account));
      this.AccountData = account;
      account.AccountViewModel = this;
    }

    public string Email { get { return AccountData.Email; } }

    public Account AccountData { get; }

    public virtual ICloud Cloud { get; }

    public virtual event PropertyChangedEventHandler PropertyChanged;

    public abstract Task WatchChange();
  }
}
