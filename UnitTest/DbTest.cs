using CssCsData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace UnitTest
{
  [TestClass]
  public class DbTest
  {
    private static readonly Random random = new Random();
    private const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public static string RandomString(int length)
    {
      return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }


    [TestMethod]
    public void DbLoadAndSetting()
    {
      DllDataInit.Init("D:\\Test");
      Assert.IsNotNull(Setting.SettingData);
      DllDataInit.UnInit();
    }
  }
}
