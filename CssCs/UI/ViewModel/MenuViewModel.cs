using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
namespace CssCs.UI.ViewModel
{
  public enum MenuAction
  {
    Refresh,
    Add,
    Delete,
    NewFolder,
    Rename
  }

  public sealed class MenuViewModel : INotifyPropertyChanged
  {
    public MenuViewModel(MenuAction action)
    {
      this.Action = action;
      this.Text = action.ToString(); ;
    }

    public MenuViewModel(CloudName cloudName, System.Drawing.Bitmap img)
    {
      this.Action = MenuAction.Add;
      this.CloudName = cloudName;
      this.Text = cloudName.ToString();
      this.Img = img.ToWindowsControlsImage_PNG();
    }

    private bool _IsEnabled = true;
    public bool IsEnabled
    {
      get { return _IsEnabled; }
      set { _IsEnabled = value; NotifyPropertyChange(); }
    }

    public MenuAction Action { get; private set; }
    public string Text { get; private set; }
    public Image Img { get; private set; }
    public CloudName CloudName { get; private set; } = CloudName.None;

    public ObservableCollection<MenuViewModel> Childs { get; } = new ObservableCollection<MenuViewModel>();

    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }
}
