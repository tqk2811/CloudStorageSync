using CssCs.UI.ViewModel;
using CssCsCloud.Cloud;
using CssCsData;
using CssCsData.Cloud;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CssCs.UI
{

  /// <summary>
  /// Interaction logic for FolderBrowserCloudDialog.xaml
  /// </summary>
  public sealed partial class FolderBrowserCloudDialog : Window
  {
    readonly AccountViewModel accvm = null;
    internal FolderBrowserCloudDialog(AccountViewModel accvm)
    {
      this.DataContext = this;
      InitializeComponent();
      this.accvm = accvm;
    }    

    #region Window Event
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
      TreeViewMenuViewModels.Add(new MenuViewModel(MenuAction.NewFolder));
      TreeViewMenuViewModels.Add(new MenuViewModel(MenuAction.Rename));
      TreeViewMenuViewModels.Add(new MenuViewModel(MenuAction.Delete));

      TreeviewCloudItemViewModel rootcloud = new TreeviewCloudItemViewModel(accvm.Email, accvm.AccountData.CloudName);
      CloudItem ci = await accvm.Cloud.GetMetadata(rootcloud.Id).ConfigureAwait(true);
      rootcloud.Id = ci.Id;
      TreeviewCloudItemViewModels.Add(rootcloud);
      await LoadChildTV(rootcloud).ConfigureAwait(true);
      rootcloud.IsExpanded = true;
    }
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
      EdittingEnd(this.Result);
    }
    #endregion

    #region Treeview
    public ObservableCollection<TreeviewCloudItemViewModel> TreeviewCloudItemViewModels { get; } = new ObservableCollection<TreeviewCloudItemViewModel>();
    TreeviewCloudItemViewModel _result = null;
    internal TreeviewCloudItemViewModel Result
    {
      get { return _result; }
      private set
      {
        if (value == null) bt_sellect.IsEnabled = false;
        else bt_sellect.IsEnabled = true;
        _result = value;
      }
    }
    private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      Result = treeView.SelectedItem as TreeviewCloudItemViewModel;
    }

    private async Task LoadChildTV(TreeviewCloudItemViewModel item)
    {
      item.LoadingVisibility = true;
      IList<CloudItem> cis = await accvm.Cloud.ListChildsFolderOfFolder(item.Id).ConfigureAwait(true);
      foreach (var ci in cis) item.Childs.Add(new TreeviewCloudItemViewModel(ci));
      item.LoadingVisibility = false;
      item.LoadedChilds = true;
    }

    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
      if (e.Source is TreeViewItem treeViewItem && sender == treeViewItem)
      {
        TreeviewCloudItemViewModel tvcloudItemViewModel = treeViewItem.DataContext as TreeviewCloudItemViewModel;
        foreach (var child in tvcloudItemViewModel.Childs)
          if (!child.LoadedChilds && !child.LoadingVisibility) _ = LoadChildTV(child);
      }
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      TextBox textBox = sender as TextBox;
      Dispatcher.BeginInvoke(DispatcherPriority.Input,
          new Action(delegate ()
          {
            Keyboard.Focus(textBox);
            textBox.SelectAll();
          }));
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      TextBox textBox = sender as TextBox;
      TreeviewCloudItemViewModel tvcloudItemViewModel = textBox.DataContext as TreeviewCloudItemViewModel;
      EdittingEnd(tvcloudItemViewModel);
    }

    private void tb_name_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        TextBox textBox = sender as TextBox;
        TreeviewCloudItemViewModel tvcloudItemViewModel = textBox.DataContext as TreeviewCloudItemViewModel;
        EdittingEnd(tvcloudItemViewModel);
      }
    }

    private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      (sender as TreeViewItem).IsSelected = true;
    }
    #endregion  

    #region Treeview Menu
    public ObservableCollection<MenuViewModel> TreeViewMenuViewModels { get; } = new ObservableCollection<MenuViewModel>();
    private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
      bool flag = false;
      if (Result != null && !Result.LoadingVisibility && Result.CloudName == CloudName.Folder) flag = true;
      foreach (var item in TreeViewMenuViewModels) item.IsEnabled = flag;
      if (Result != null && Result.CloudName != CloudName.Folder) TreeViewMenuViewModels[0].IsEnabled = true;
    }

    private async void MenuItem_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (Result != null && !Result.LoadingVisibility && Result.CloudName == CloudName.Folder)
        {
          MenuItem item = sender as MenuItem;
          MenuViewModel data = item.DataContext as MenuViewModel;
          switch (data.Action)
          {
            case MenuAction.NewFolder:
              Result.IsExpanded = true;
              TreeviewCloudItemViewModel newfolder = new TreeviewCloudItemViewModel("New Folder", CloudName.Folder);
              Result.Childs.Add(newfolder);
              newfolder.Parent = Result;
              newfolder.IsEditing = true;
              break;

            case MenuAction.Rename:
              if (Result.CloudName == CloudName.Folder) Result.IsEditing = true;
              break;

            case MenuAction.Delete:
              CloudItem ci = accvm.AccountData.GetCloudItem(Result.Id);
              if (ci.PermissionFlag.HasFlag(CloudItemPermissionFlag.OwnedByMe))//my file
              {
                MessageBoxResult result = MessageBox.Show("Are you sure trash that folder?\r\n" + Result.Name, "Confirm Trash", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (MessageBoxResult.Yes == result)
                {
                  Result.LoadingVisibility = true;
                  await accvm.Cloud.TrashItem(Result.Id).ConfigureAwait(true);
                  Result.Parent.Childs.Remove(Result);
                }
              }
              else MessageBox.Show("You not have permision do delete item.\r\n" + Result.Name, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
              break;

            default: break;
          }
        }
      }
      catch(AggregateException ae)
      {
        MessageBox.Show(ae.InnerException.Message + "\r\n" + ae.InnerException.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Result.LoadingVisibility = false;
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Result.LoadingVisibility = false;
      }
    }
    #endregion

    internal async void EdittingEnd(TreeviewCloudItemViewModel tvcloudItemViewModel)
    {
      if (tvcloudItemViewModel == null) return;
      if (tvcloudItemViewModel.IsEditing)
      {
        try
        {
          tvcloudItemViewModel.IsEditing = false;
          tvcloudItemViewModel.LoadingVisibility = true;
          if (string.IsNullOrEmpty(tvcloudItemViewModel.Id))//new folder
          {
            if (!string.IsNullOrEmpty(tvcloudItemViewModel.Name) && tvcloudItemViewModel.Parent != null)
            {
              CloudItem ci = await accvm.Cloud.CreateFolder(tvcloudItemViewModel.Name, tvcloudItemViewModel.Parent.Id).ConfigureAwait(true);
              tvcloudItemViewModel.Id = ci.Id;
            }
            else tvcloudItemViewModel.IsEditing = true;
          }
          else if (!tvcloudItemViewModel.Name.Equals(tvcloudItemViewModel.OldName, StringComparison.OrdinalIgnoreCase))//rename
          {
            await accvm.Cloud.UpdateMetadata(new UpdateCloudItem() { Id = tvcloudItemViewModel.Id, NewName = tvcloudItemViewModel.Name }).ConfigureAwait(true);
          }
        }
        catch (Exception ex)
        {
          if (!string.IsNullOrEmpty(tvcloudItemViewModel.Id)) tvcloudItemViewModel.Name = tvcloudItemViewModel.OldName;
          MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
        }
        finally
        {
          tvcloudItemViewModel.LoadingVisibility = false;
        }
      }
    }

    #region button
    private void bt_sellect_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void bt_cancel_Click(object sender, RoutedEventArgs e)
    {
      Result = null;
      this.Close();
    }
    #endregion
  }
}
