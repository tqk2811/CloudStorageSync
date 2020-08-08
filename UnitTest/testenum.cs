using CssCsData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
  [TestClass]
  public class testenum
  {
    [TestMethod]
    public void test()
    {
      SettingFlag flag = SettingFlag.None;

      flag |= SettingFlag.DownloadPrioritizeFirst;
      flag |= SettingFlag.SkipNoticeMalware;
      Assert.IsTrue(flag.HasFlag(SettingFlag.DownloadPrioritizeFirst));
      flag ^= SettingFlag.DownloadPrioritizeFirst;
      Assert.IsFalse(flag.HasFlag(SettingFlag.DownloadPrioritizeFirst));
      Assert.IsTrue(flag.HasFlag(SettingFlag.SkipNoticeMalware));
    }
  }
}
