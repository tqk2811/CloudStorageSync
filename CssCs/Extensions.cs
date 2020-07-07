using CssCs.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;

namespace CssCs
{
  public static class Extensions
  {
    #region image
    public static System.Windows.Media.Imaging.BitmapImage ToBitmapImage(this System.Drawing.Bitmap src, System.Drawing.Imaging.ImageFormat imageFormat)
    {
      using (MemoryStream ms = new MemoryStream())
      {
        src.Save(ms, imageFormat);
        ms.Position = 0;
        System.Windows.Media.Imaging.BitmapImage image = new System.Windows.Media.Imaging.BitmapImage();
        image.BeginInit();
        image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        return image;
      }
    }
    public static System.Windows.Media.Imaging.BitmapImage ToBitmapImage(this System.Drawing.Bitmap bitmap)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        System.Windows.Media.Imaging.BitmapImage result = new System.Windows.Media.Imaging.BitmapImage();
        result.BeginInit();
        result.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        result.StreamSource = stream;
        result.EndInit();
        result.Freeze();
        return result;
      }
    }

    public static System.Windows.Controls.Image ToWindowsControlsImage_PNG(this System.Drawing.Bitmap src, double Width = 16, double Height = 16)
    {
      System.Windows.Controls.Image image = new System.Windows.Controls.Image();
      image.Width = Width;
      image.Height = Height;
      image.Stretch = System.Windows.Media.Stretch.Uniform;
      image.Source = src.ToBitmapImage(System.Drawing.Imaging.ImageFormat.Png);
      return image;
    }

    public static System.Windows.Controls.Image ToImage(this System.Drawing.Icon ico, double Width = 16, double Height = 16)
    {
      return ico.ToBitmap().ToWindowsControlsImage_PNG(Width, Height);
    }

    public static System.Windows.Media.Imaging.BitmapImage ToBitmapImage(this CloudName cloudName)
    {
      System.Drawing.Bitmap bitmap;
      switch (cloudName)
      {
        case CloudName.GoogleDrive: bitmap = Properties.Resources.Google_Drive_Icon256x256; break;
        case CloudName.OneDrive: bitmap = Properties.Resources.onedrive_logo; break;
        case CloudName.MegaNz: bitmap = Properties.Resources.MegaSync; break;
        case CloudName.Dropbox: bitmap = Properties.Resources.Dropbox256x256; break;
        case CloudName.Folder: bitmap = Properties.Resources.folder_closed64x64; break;
        case CloudName.File: bitmap = Properties.Resources.file_files_document_1_19_512; break;
        default: return null;
      }
      return bitmap.ToBitmapImage();
    }

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject([In] IntPtr hObject);
    public static System.Windows.Media.ImageSource ToImageSource(this System.Drawing.Bitmap src)
    {
      var handle = src.GetHbitmap();
      try
      {
        return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
      }
      finally { DeleteObject(handle); }
    }
    #endregion

    private static Random random = new Random();
    private const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public static string RandomString(int length)
    {
      return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static string ParentsCommaSeparatedList(this IList<string> ids)
    {
      if (ids == null) return null;
      StringBuilder stringBuilder = new StringBuilder();
      if (ids.Count > 0) stringBuilder.Append(ids[0]);
      for (int i = 1; i < ids.Count; i++)
      {
        stringBuilder.Append(',');
        stringBuilder.Append(ids[i]);
      }
      return stringBuilder.ToString();
    }

    static readonly char[] NotInvalid = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
    static readonly Regex regex_FileNameExtension = new Regex(@"\.[0-9A-z]+$");
    public static string RenameFileNameUnInvalid(this string input, bool isFile = false)
    {
      if (string.IsNullOrEmpty(input)) return string.Empty;
      foreach (var c in NotInvalid) while (input.IndexOf(c) >= 0) input = input.Replace(c, '_');
      if (input.Length > 245)
      {
        if (isFile)
        {
          string extension = string.Empty;
          Match match = regex_FileNameExtension.Match(input);
          if (match.Success)
          {
            extension = match.Value;
            if (extension.Length > 245) extension = string.Empty;
          }
          input = input.Substring(0, 244 - extension.Length);
          input += extension;
        }
        else input = input.Substring(0, 244);
      }
      return input;
    }

    //public static long GetUnixTimeSecondsNow() => DateTimeOffset.Now.ToUnixTimeSeconds();

    public static long GetUnixTimeSeconds(this DateTime dt) => new DateTimeOffset(dt).ToUnixTimeSeconds();

    //https://docs.microsoft.com/en-us/windows/win32/sysinfo/file-times
    public static long GetFileTime(long UnixTimeSeconds) => DateTimeOffset.FromUnixTimeSeconds(UnixTimeSeconds).ToFileTime();

    public static bool Ping()
    {
      System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
      System.Net.NetworkInformation.PingReply reply = pingSender.Send(IPAddress.Parse("8.8.8.8"));
      if (reply.Status == System.Net.NetworkInformation.IPStatus.Success) Settings.Setting.HasInternet = true;
      else Settings.Setting.HasInternet = false;

      return Settings.Setting.HasInternet;
    }


    public static bool CheckFolderPermission(string directoryPath)
    {
      try
      {
        AuthorizationRuleCollection collection = Directory.GetAccessControl(directoryPath).GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
        foreach (FileSystemAccessRule rule in collection) 
          if (rule.AccessControlType == AccessControlType.Allow) 
            return true;
      }
      catch (Exception ex)
      {

      }
      return false;
    }
  }
}
