using CssCs.Cloud;
using CssCs.DataClass;
using CssCs.StreamLimit;
using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;

namespace CssCs
{
  public delegate void _OutPutDebugString(string text, int loglevel = 10);
  public delegate void _TestWatchCloud();

  public delegate void _SRRegister(SyncRootViewModel srvm);
  public delegate void _SRUnRegister(SyncRootViewModel srvm);

  public delegate bool _ConvertToPlaceholder(SyncRootViewModel srvm,LocalItem li, string fileIdentity);
  public delegate bool _UpdatePlaceholder(SyncRootViewModel srvm, LocalItem li, CloudItem ci);
  public static class CPPCLR_Callback
  {
    public static void Init(string UWPLocalStatePath)
    {
      if (string.IsNullOrEmpty(UWPLocalStatePath)) throw new ArgumentNullException(UWPLocalStatePath);
      if (CPPCLR_Callback.UWPLocalStatePath != null) return;

      CPPCLR_Callback.UWPLocalStatePath = UWPLocalStatePath;
      CloudOneDrive.Init();
      SqliteManager.Init();
      SqliteManager.SettingSelect();
    }
    public static string UWPLocalStatePath { get; private set; } = null;

    public static void ShutDown()
    {
      SqliteManager.Close();
    }

    public static _TestWatchCloud TestWatchCloud { get; set; }
    public static _OutPutDebugString OutPutDebugString { get; set; }

    public static _SRRegister SRRegister { get; set; }
    public static _SRUnRegister SRUnRegister { get; set; }

    public static _ConvertToPlaceholder ConvertToPlaceholder { get; set; }
    public static _UpdatePlaceholder UpdatePlaceholder { get; set; }
  }
}
