using CssCs.DataClass;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CssCs.UI.ViewModel
{
  public class TreeviewCloudItemViewModel : INotifyPropertyChanged
  {
    internal TreeviewCloudItemViewModel(CloudItem ci)
    {
      this.Id = ci.Id;
      this.Name = ci.Name;
      this.CloudName = CloudName.Folder;
      this.Img = this.CloudName.ToBitmapImage();
    }
    internal TreeviewCloudItemViewModel(string id, string name, CloudName cloudName)
    {
      this.Id = id;
      this.Name = name;
      this.CloudName = cloudName;
      this.Img = cloudName.ToBitmapImage();
    }
    public ObservableCollection<TreeviewCloudItemViewModel> Childs { get; } = new ObservableCollection<TreeviewCloudItemViewModel>();
    public TreeviewCloudItemViewModel Parent { get; set; } = null;
    public string Id { get; set; }
    public CloudName CloudName { get; private set; }
    /// <summary>
    /// loaded Childs
    /// </summary>
    public bool LoadedChilds { get; set; } = false;


    bool _IsEditing = false;
    /// <summary>
    /// editing mode
    /// </summary>
    public bool IsEditing
    {
      get { return _IsEditing; }
      set { _IsEditing = value; if (value) IsSelected = true; NotifyPropertyChange(); }
    }


    bool _LoadingVisibility = false;
    /// <summary>
    /// show/hide loading ui
    /// </summary>
    public bool LoadingVisibility
    {
      get { return _LoadingVisibility; }
      set { _LoadingVisibility = value; NotifyPropertyChange(); }
    }


    public string OldName { get; set; } = string.Empty;
    string _name = string.Empty;
    public string Name
    {
      get { return _name; }
      set { OldName = _name; _name = value; NotifyPropertyChange(); }
    }


    bool _IsSelected = false;
    /// <summary>
    /// treeview item selected
    /// </summary>
    public bool IsSelected
    {
      get { return _IsSelected; }
      set { _IsSelected = value; NotifyPropertyChange(); }
    }


    bool _IsExpanded = false;
    /// <summary>
    /// treeview item expandedd
    /// </summary>
    public bool IsExpanded
    {
      get { return _IsExpanded; }
      set
      {
        if (value && Childs.Count == 0) return;//can't Expand when childs empty
        _IsExpanded = value;
        NotifyPropertyChange();
      }
    }


    public System.Windows.Media.ImageSource _img = null;
    public System.Windows.Media.ImageSource Img
    {
      get { return _img; }
      private set { _img = value; NotifyPropertyChange(); }
    }


    #region INotifyPropertyChanged
    private void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }
}
