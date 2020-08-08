using System;
using System.IO;

namespace CssCsData
{
  public static class DllDataInit
  {
    static bool flagInit = false;
    /// <summary>
    /// Init lib
    /// </summary>
    /// <param name="UWPLocalStatePath">UWP LocalState Path</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void Init(string UWPLocalStatePath)
    {
      if (flagInit) return;
      if (string.IsNullOrEmpty(UWPLocalStatePath)) throw new ArgumentNullException(nameof(UWPLocalStatePath));
      if (!Directory.Exists(UWPLocalStatePath)) throw new DirectoryNotFoundException(UWPLocalStatePath);
      DllDataInit.UWPLocalStatePath = UWPLocalStatePath;
      SqliteManaged.Init();
      flagInit = true;
    }
    internal static string UWPLocalStatePath { get; private set; }
    public static void UnInit()
    {
      SqliteManaged.UnInit();
    }
  }
}
