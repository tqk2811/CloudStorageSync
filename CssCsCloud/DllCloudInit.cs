using CssCsCloud.Cloud;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace CssCsCloud
{
  public static class DllCloudInit
  {
    internal static readonly IFormatProvider DefaultCulture = CultureInfo.InvariantCulture;
    internal const int OauthWait = 5 * 60000;
    internal const int SpeedMinLimit = 64 * 1024;
    internal const int ChunkUploadDownload = 50 * 1024 * 1024;//50Mib
    internal static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
    {
      NullValueHandling = NullValueHandling.Ignore
    };

    public static int SpeedDownloadLimitByte { get; set; } = 0;
    public static int SpeedUploadLimitByte { get; set; } = 0;
    public static bool SkipNoticeMalware { get; set; } = false;


    internal static string UWPLocalStatePath { get; private set; }
    public static void Init(string UWPLocalStatePath)
    {
      DllCloudInit.UWPLocalStatePath = UWPLocalStatePath;
      CloudOneDrive.Init();

    }

    public static void UnInit()
    {

    }
  }
}
