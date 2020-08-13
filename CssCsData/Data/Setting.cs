namespace CssCsData
{
  public enum SettingFlag : long
  {
    None = 0,
    SkipNoticeMalware = 1 << 0,
    UploadPrioritizeFirst = 1 << 1,
    DownloadPrioritizeFirst = 1 << 2,
  }
  public class Setting
  {
    public string FileIgnore { get; set; }
    public long TryAgainAfter { get; set; }
    public long TryAgainTimes { get; set; }
    public long FilesUploadSameTime { get; set; }
    public long SpeedUploadLimit { get; set; }
    public long SpeedDownloadLimit { get; set; }
    public long TimeWatchChangeCloud { get; set; }
    public SettingFlag Flag { get; set; }



    public void Update()
    {

    }

    public static Setting SettingData { get; internal set; }
  }
}
