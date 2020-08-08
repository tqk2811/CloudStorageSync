using CssCs.Queues;
using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CssCs
{
  public delegate void _OutPutDebugString(string text, int loglevel = 10);
  public static class CppInterop
  {
    public static bool Init(string UWPLocalStatePath)
    {
      if (string.IsNullOrEmpty(UWPLocalStatePath)) throw new ArgumentNullException(UWPLocalStatePath);      
      if (CppInterop.UWPLocalStatePath != null) return false;
      CssCsCloud.DllCloudInit.Init(UWPLocalStatePath);
      CssCsData.DllDataInit.Init(UWPLocalStatePath);
      return true;
    }
    public static string UWPLocalStatePath { get; private set; } = null;

    public static void UnInit()
    {
      TaskQueues.UploadQueues.ShutDown();
      CssCsData.DllDataInit.UnInit();
    }

    public static _OutPutDebugString OutPutDebugString { get; set; }

    public static ObservableCollection<AccountViewModel> AccountViewModels { get; } = new ObservableCollection<AccountViewModel>();

    public static bool HasInternet { get; set; } = false;
  }
}
