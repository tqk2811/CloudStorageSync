using CssCs.Cloud;
using CssCs.DataClass;
using CssCs.Queues;
using CssCs.StreamLimit;
using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;

namespace CssCs
{
  public delegate void _OutPutDebugString(string text, int loglevel = 10);
  public static class CppInterop
  {
    public static bool Init(string UWPLocalStatePath, SrRegister srRegister, SrUnRegister srUnRegister)
    {
      if (string.IsNullOrEmpty(UWPLocalStatePath)) throw new ArgumentNullException(UWPLocalStatePath);      
      if (CppInterop.UWPLocalStatePath != null) return false;

      SyncRootViewModel.Init(srRegister, srUnRegister);
      CppInterop.UWPLocalStatePath = UWPLocalStatePath;
      CloudOneDrive.Init();
      SqliteManager.Init();
      return SqliteManager.SettingSelect();
    }
    public static string UWPLocalStatePath { get; private set; } = null;

    public static void ShutDown()
    {
      TaskQueues.UploadQueues.ShutDown();
      SqliteManager.Close();
    }

    public static _OutPutDebugString OutPutDebugString { get; set; }
  }
}
