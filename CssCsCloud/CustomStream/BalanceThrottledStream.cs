using System.Collections.Generic;
namespace CssCsCloud.CustomStream
{
  internal delegate void FinishTransfer(ThrottledStream throttledStream);
  internal class BalanceThrottledStream
  {
    readonly bool IsDown;
    internal bool PrioritizeFirst { get; set; } = false;
    readonly List<ThrottledStream> throttledStreams = new List<ThrottledStream>();

    internal BalanceThrottledStream(bool IsDown)
    {
      this.IsDown = IsDown;
    }

    internal void Add(ThrottledStream throttledStream)
    {
      lock (throttledStreams)
      {
        throttledStreams.Add(throttledStream);
        throttledStream.finishTransfer += Remove;
        Balance();
      }
    }

    void Remove(ThrottledStream throttledStream)
    {
      lock (throttledStreams)
      {
        throttledStreams.Remove(throttledStream);
        Balance();
      }
    }

    void Balance()
    {
      if (throttledStreams.Count == 0) return;

      int balance_speed = int.MaxValue;
      int bytelimit = IsDown ? DllCloudInit.SpeedDownloadLimitByte : DllCloudInit.SpeedUploadLimitByte;

      if (PrioritizeFirst)
      {
        if (0 == bytelimit)
        {
          foreach (var throttledStream in throttledStreams) throttledStream.MaxBytesPerSecond = balance_speed;
        }
        else
        {
          int main_speed = bytelimit - ((throttledStreams.Count - 1) * DllCloudInit.SpeedMinLimit);
          if (main_speed < DllCloudInit.SpeedMinLimit) main_speed = DllCloudInit.SpeedMinLimit;

          throttledStreams[0].MaxBytesPerSecond = main_speed;
          for (int i = 1; i < throttledStreams.Count; i++) throttledStreams[i].MaxBytesPerSecond = DllCloudInit.SpeedMinLimit;
        }
      }
      else
      {
        if (bytelimit != 0) balance_speed = bytelimit / throttledStreams.Count;
        foreach (var throttledStream in throttledStreams) throttledStream.MaxBytesPerSecond = balance_speed;
      }
    }

    public void LimitChange()
    {
      lock (throttledStreams) Balance();
    }
  }
}
