using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CssCs.StreamLimit
{
  internal delegate void Throttle(int BytesTransfer);
  internal static class ThrottledManaged
  {
    public static Throttle upload { get; private set; }
    static int uploaded = 0;
    static AutoResetEvent ARE_up = new AutoResetEvent(true);

    public static Throttle download { get; private set; }
    static int downloaded = 0;
    static AutoResetEvent ARE_down = new AutoResetEvent(true);

    static System.Timers.Timer timer;

    public static void Init()
    {
      upload = new Throttle(Up);
      download = new Throttle(Down);
      timer = new System.Timers.Timer();
      timer.Interval = 1000;
      timer.AutoReset = true;
      timer.Elapsed += Timer_Elapsed;
      timer.Start();
    }
    public static void UnInit()
    {
      timer.Close();
    }

    private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      lock (ARE_up) uploaded = 0;
      ARE_up.Set();
      lock (ARE_down) downloaded = 0;
      ARE_down.Set();
    }

    static void Up(int progress)
    {
      if (Settings.Setting.SpeedUploadLimitByte == 0) return;
      lock (ARE_up) uploaded += progress;

      if (uploaded > Settings.Setting.SpeedUploadLimitByte) ARE_up.WaitOne();
    }

    static void Down(int progress)
    {
      if (Settings.Setting.SpeedDownloadLimitByte == 0) return;
      lock (ARE_down) downloaded += progress;

      if (downloaded > Settings.Setting.SpeedDownloadLimitByte) ARE_down.WaitOne();
    }
  }
}
