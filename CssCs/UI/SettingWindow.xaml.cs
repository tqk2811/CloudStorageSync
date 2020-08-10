using CssCs.UI.ViewModel;
using CssCsCloud.Cloud;
using CssCsData;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CssCs.UI
{
  public delegate void OnCreateSyncRoot();
  public sealed partial class SettingWindow : Window
  {
    public SettingWindow()
    {
      this.DataContext = this;
      InitializeComponent();
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      MenuViewModel cloud_add = new MenuViewModel(MenuAction.Add);
      cloud_add.Childs.Add(new MenuViewModel(CloudName.GoogleDrive, Properties.Resources.Google_Drive_Icon256x256));
      cloud_add.Childs.Add(new MenuViewModel(CloudName.OneDrive, Properties.Resources.onedrive_logo));
      //cloud_add.Childs.Add(new MenuViewModel(CloudName.MegaNz, Properties.Resources.MegaSync));
      //cloud_add.Childs.Add(new MenuViewModel(CloudName.Dropbox, Properties.Resources.Dropbox256x256));

      AccountMenuViewModels.Add(cloud_add);
      AccountMenuViewModels.Add(new MenuViewModel(MenuAction.Delete));

      SyncRootMenuViewModels.Add(new MenuViewModel(MenuAction.Refresh));
      SyncRootMenuViewModels.Add(new MenuViewModel(MenuAction.Add));
      SyncRootMenuViewModels.Add(new MenuViewModel(MenuAction.Delete));
    }


    #region setting
    public SettingViewModel Setting { get; } = new SettingViewModel();
    #endregion


    #region list email
    public ObservableCollection<AccountViewModel> AccountViewModels => CppInterop.AccountViewModels;

    public ObservableCollection<MenuViewModel> AccountMenuViewModels { get; } = new ObservableCollection<MenuViewModel>();

    public AccountViewModel GetAccountVMSelected()
    {
      return LV_listemail.SelectedItem as AccountViewModel;
    }

    private void LV_listemail_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var cloudEmailSelected = GetAccountVMSelected();
      if (cloudEmailSelected != null)
      {
        this.SyncRootViewModels.Clear();
        cloudEmailSelected.AccountData.GetSyncRoot().ForEach((sr) => this.SyncRootViewModels.Add(sr.SyncRootViewModel));
      }
    }

    private async void LV_listemail_MenuItem_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        AccountViewModel accvm;
        MenuItem menuItem = sender as MenuItem;
        MenuViewModel menuDataModel = menuItem.DataContext as MenuViewModel;
        switch (menuDataModel.Action)
        {
          case MenuAction.Add:
            if ((int)menuDataModel.CloudName >= 250) return;
            OauthResult oauthResult = await Oauth.OauthNewAccount(menuDataModel.CloudName).ConfigureAwait(true);
            if (null != oauthResult)
            {
              accvm = AccountViewModels.ToList().Find(acc => acc.AccountData.CloudName == oauthResult.CloudName &&
                                                       acc.AccountData.Email.Equals(oauthResult.Email, StringComparison.OrdinalIgnoreCase));
              if (null != accvm)//replace token
              {
                accvm.AccountData.Token = oauthResult.User;
                accvm.AccountData.Update();
              }
              else//add new
              {
                Account account = new Account(Extensions.RandomString(32), oauthResult.Email, menuDataModel.CloudName);
                account.Token = oauthResult.User;
                account.Insert();
                AccountViewModels.Add(new AccountViewModel(account));
              }
            }
            break;
          case MenuAction.Delete:
            accvm = GetAccountVMSelected();
            if (accvm == null) return;
            var srvms = SyncRootViewModels.ToList();
            bool isworking = srvms.Any((srvm) => srvm.IsWork);
            if (isworking) MessageBox.Show(this, "Please unregister all syncroot.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
              srvms.ForEach((srvm) => srvm.SyncRootData.Delete());
              SyncRootViewModels.Clear();
              if (await accvm.Cloud.LogOut().ConfigureAwait(true))
              {
                accvm.AccountData.Delete();
                AccountViewModels.Remove(accvm);
              }
            }
            break;

          default: break;
        }
      }
      catch(Exception ex)
      {
        if (ex is AggregateException ae) MessageBox.Show("Message: " + ae.InnerException.Message + "\r\n\r\n" + ae.InnerException.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        else MessageBox.Show("Message: " + ex.Message + "\r\n\r\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
    #endregion

    #region SyncRoot
    public ObservableCollection<SyncRootViewModelBase> SyncRootViewModels { get; } = new ObservableCollection<SyncRootViewModelBase>();
    public ObservableCollection<MenuViewModel> SyncRootMenuViewModels { get; } = new ObservableCollection<MenuViewModel>();
    public event OnCreateSyncRoot OnCreateSyncRoot;

    SyncRootViewModelBase GetSRVMSelected()
    {
      return LV_SR.SelectedItem as SyncRootViewModelBase;
    }

    private void CloudPath_changeClick(object sender, RoutedEventArgs e)
    {
      try
      {
        Button button = sender as Button;
        SyncRootViewModelBase srvm = button.DataContext as SyncRootViewModelBase;
        if (srvm.IsWork) return;
        FolderBrowserCloudDialog folderBrowserCloudDialog = new FolderBrowserCloudDialog(GetAccountVMSelected());
        folderBrowserCloudDialog.ShowDialog();
        if (folderBrowserCloudDialog.Result != null)
        {
          srvm.CloudFolderName = folderBrowserCloudDialog.Result.Name;
          srvm.SyncRootData.CloudFolderId = folderBrowserCloudDialog.Result.Id;
          srvm.DisplayName = srvm.CloudFolderName + " - " + srvm.SyncRootData.Account.Email;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("Message: " + ex.Message + "\r\n\r\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
    private void LocalPath_changeClick(object sender, RoutedEventArgs e)
    {
      Button button = sender as Button;
      SyncRootViewModelBase data = button.DataContext as SyncRootViewModelBase;
      if (data.IsWork) return;
      var dialog = new System.Windows.Forms.FolderBrowserDialog()
      {
        ShowNewFolderButton = true,
        RootFolder = Environment.SpecialFolder.MyComputer
      };
      var result = dialog.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
      {
        if (Extensions.CheckFolderPermission(dialog.SelectedPath))
        {
          data.LocalPath = dialog.SelectedPath;
        }
        else MessageBox.Show("App do not have permission to access this folder.\r\nPlease try select difference folder.\r\n\r\nFolder selected:" + dialog.SelectedPath, 
          "Error", MessageBoxButton.OK);
      }
      dialog.Dispose();
    }
    private void DisplayName_changeClick(object sender, RoutedEventArgs e)
    {
      Button button = sender as Button;
      SyncRootViewModelBase srvm = button.DataContext as SyncRootViewModelBase;
      srvm.IsEditingDisplayName = true;
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
      SyncRootViewModelBase srvm = textBox.DataContext as SyncRootViewModelBase;
      srvm.IsEditingDisplayName = false;
    }
    private void tb_DisplayName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        TextBox textBox = sender as TextBox;
        SyncRootViewModelBase srvm = textBox.DataContext as SyncRootViewModelBase;
        srvm.IsEditingDisplayName = false;
      }
    }

    private void CF_MenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = sender as MenuItem;
      MenuViewModel menuDataModel = menuItem.DataContext as MenuViewModel;
      var ceSelected = GetAccountVMSelected();
      switch (menuDataModel.Action)
      {
        case MenuAction.Add:
          if (ceSelected != null) OnCreateSyncRoot?.Invoke();
          break;
        case MenuAction.Delete:
          var SRVMSelected = GetSRVMSelected();
          if (SRVMSelected == null || SRVMSelected.IsWork) break;
          SRVMSelected.SyncRootData.Delete();
          SyncRootViewModels.Remove(SRVMSelected);
          break;

        case MenuAction.Refresh:
          if (ceSelected != null) LV_listemail_SelectionChanged(null, null);
          break;

        default: break;
      }
    }
    #endregion

    
    private void bt_exit_Click(object sender, RoutedEventArgs e)
    {
      if(MessageBoxResult.Yes == MessageBox.Show("Are you sure want to quit?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question))
        Extensions.PostQuitMessage(0);
    }
  }
}
