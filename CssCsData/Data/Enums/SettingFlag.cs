using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCsData
{
  public enum SettingFlag : long
  {
    None = 0,
    SkipNoticeMalware = 1 << 0,
    UploadPrioritizeFirst = 1 << 1,
    DownloadPrioritizeFirst = 1 << 2,
  }
}
