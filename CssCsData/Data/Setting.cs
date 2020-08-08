using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCsData
{
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
