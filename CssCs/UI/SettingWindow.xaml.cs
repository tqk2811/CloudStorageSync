using CssCs.Cloud;
using CssCs.UI.ViewModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Microsoft.Identity.Client;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CssCs.UI
{
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

      cloudEmailMenuViewModels.Add(cloud_add);
      cloudEmailMenuViewModels.Add(new MenuViewModel(MenuAction.Delete));

      SRMenuViewModels.Add(new MenuViewModel(MenuAction.Refresh));
      SRMenuViewModels.Add(new MenuViewModel(MenuAction.Add));
      SRMenuViewModels.Add(new MenuViewModel(MenuAction.Delete));
    }


    #region setting
    public Settings Setting { get { return CssCs.Settings.Setting; } }
    #endregion
    

    #region list email
    public ObservableCollection<CloudEmailViewModel> cloudEmailViewModels
    {
      get { return CloudEmailViewModel.CEVMS; }
    }

    public ObservableCollection<MenuViewModel> cloudEmailMenuViewModels { get; } = new ObservableCollection<MenuViewModel>();

    CloudEmailViewModel GetCloudEmailSelected()
    {
      return LV_listemail.SelectedItem as CloudEmailViewModel;
    }

    private void LV_listemail_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var cloudEmailSelected = GetCloudEmailSelected();
      if (cloudEmailSelected != null)
      {
        this.SRViewModels.Clear();
        SyncRootViewModel.FindAll(cloudEmailSelected).ForEach((cevm) => this.SRViewModels.Add(cevm));
      }
    }

    private async void LV_listemail_MenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = sender as MenuItem;
      MenuViewModel menuDataModel = menuItem.DataContext as MenuViewModel;
      switch (menuDataModel.Action)
      {
        case MenuAction.Add:
          AddCloud(menuDataModel);
            break;
        case MenuAction.Delete:
          var ceSelected = GetCloudEmailSelected();
          if (ceSelected == null) return;
          var srvms = SRViewModels.ToList();
          bool isworking = srvms.Any((srvm) => srvm.IsWork);          
          if (isworking)
          {
            MessageBox.Show(this, "Please unregister all syncroot.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
          }
          else
          {
            srvms.ForEach((srvm) => srvm.Delete());
            SRViewModels.Clear();

            if (await ceSelected.Cloud.LogOut()) ceSelected.Delete();
          }
          
          break;

        default: break;
      }

    }

    private async void AddCloud(MenuViewModel menuDataModel)
    {
      try
      {
        string str_token = string.Empty;
        string str_email = string.Empty;
        switch (menuDataModel.CloudName)
        {
          case CloudName.None: return;
          case CloudName.GoogleDrive:
            UserCredential credential = await CloudGoogleDrive.Oauth2();
            DriveService service = CloudGoogleDrive.GetDriveService(credential);
            var getRequest = service.About.Get();
            getRequest.Fields = "user";
            About about = await getRequest.ExecuteAsync();
            str_email = about.User.EmailAddress;
            str_token = credential.UserId;
            break;

          case CloudName.OneDrive:
            AuthenticationResult result = await CloudOneDrive.Oauth();
            str_token = result.Account.HomeAccountId.Identifier;
            str_email = result.Account.Username;
            break;

          case CloudName.Dropbox:

          case CloudName.MegaNz:


          default: MessageBox.Show("This Cloud Not Support Now"); return;
        }
        for (int i = 0; i < cloudEmailViewModels.Count; i++)
          if (cloudEmailViewModels[i].Email.Equals(str_email) && cloudEmailViewModels[i].CloudName == menuDataModel.CloudName)//update if equal
          {
            cloudEmailViewModels[i].Token = str_token;
            await cloudEmailViewModels[i].LoadQuota();
            return;
          }
        CloudEmailViewModel cevm = new CloudEmailViewModel(str_email, menuDataModel.CloudName, str_token);//add new if not equal
      }
      catch (AggregateException ae)
      {
        MessageBox.Show("Message: " + ae.InnerException.Message + "\r\n\r\n" + ae.InnerException.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        if (ex is Microsoft.Identity.Client.MsalClientException msalex &&
          !string.IsNullOrEmpty(msalex.ErrorCode) &&
          msalex.ErrorCode.Equals("authentication_canceled")) return;

        MessageBox.Show("Message: " + ex.Message + "\r\n\r\n" + ex.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
    #endregion

    #region SyncRoot
    public ObservableCollection<SyncRootViewModel> SRViewModels { get; } = new ObservableCollection<SyncRootViewModel>();
    public ObservableCollection<MenuViewModel> SRMenuViewModels { get; } = new ObservableCollection<MenuViewModel>();
    SyncRootViewModel GetSRVMSelected()
    {
      return LV_SR.SelectedItem as SyncRootViewModel;
    }
    private void CloudPath_changeClick(object sender, RoutedEventArgs e)
    {
      try
      {
        Button button = sender as Button;
        SyncRootViewModel data = button.DataContext as SyncRootViewModel;
        if (data.IsWork) return;
        FolderBrowserCloudDialog folderBrowserCloudDialog = new FolderBrowserCloudDialog(GetCloudEmailSelected());
        folderBrowserCloudDialog.ShowDialog();
        if (folderBrowserCloudDialog.Result != null)
        {
          data.CloudFolderName = folderBrowserCloudDialog.Result.Name;
          data.CloudFolderId = folderBrowserCloudDialog.Result.Id;
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
      SyncRootViewModel data = button.DataContext as SyncRootViewModel;
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
        else MessageBox.Show("App do not have permission to access this folder.\r\nPlease try select difference folder.\r\n\r\nFolder selected:" + dialog.SelectedPath, "Error", MessageBoxButton.OK);
      }
      dialog.Dispose();
    }
    private void DisplayName_changeClick(object sender, RoutedEventArgs e)
    {
      Button button = sender as Button;
      SyncRootViewModel srvm = button.DataContext as SyncRootViewModel;
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
      SyncRootViewModel srvm = textBox.DataContext as SyncRootViewModel;
      srvm.IsEditingDisplayName = false;
    }

    private void tb_DisplayName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        TextBox textBox = sender as TextBox;
        SyncRootViewModel srvm = textBox.DataContext as SyncRootViewModel;
        srvm.IsEditingDisplayName = false;
      }
    }

    private void CF_MenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = sender as MenuItem;
      MenuViewModel menuDataModel = menuItem.DataContext as MenuViewModel;
      var ceSelected = GetCloudEmailSelected();
      switch (menuDataModel.Action)
      {
        case MenuAction.Add:
          if (ceSelected != null)
          {
            SyncRootViewModel SRViewModel = new SyncRootViewModel(ceSelected);
            SRViewModels.Add(SRViewModel);
          }
          break;
        case MenuAction.Delete:
          var SRVMSelected = GetSRVMSelected();
          if (SRVMSelected != null && !SRVMSelected.IsWork)
          {
            SRVMSelected.Delete();
            SRViewModels.Remove(SRVMSelected);
          }
          break;

        case MenuAction.Refresh:
          if (ceSelected != null) LV_listemail_SelectionChanged(null, null);
          break;

        default: break;
      }
    }
    #endregion

    [DllImport("User32.dll")]
    private static extern void PostQuitMessage(int nExitCode);
    private void bt_exit_Click(object sender, RoutedEventArgs e)
    {
      if(MessageBoxResult.Yes == MessageBox.Show("Are you sure want to quit?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question))
        PostQuitMessage(0);
    }
  }
}
