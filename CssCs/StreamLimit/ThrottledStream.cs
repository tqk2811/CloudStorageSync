using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Management;

namespace CssCs.StreamLimit
{
  public class TransferException : Exception
  {
    public TransferException(string message)
    {
      Message = message;
    }

    public override string Message { get; }
  }
  delegate void FinishTransfer(ThrottledStream throttledStream);
  internal class BalanceThrottledStream
  {
    bool IsDown;
    internal bool PrioritizeFirst { get; set; } = false;
    const int MinLimit = 64 * 1024;
    internal BalanceThrottledStream(bool IsDown)
    {
      this.IsDown = IsDown;
    }
    List<ThrottledStream> throttledStreams = new List<ThrottledStream>();
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
      int bytelimit = IsDown ? Settings.Setting.SpeedDownloadLimitByte : Settings.Setting.SpeedUploadLimitByte;

      if (PrioritizeFirst)
      {
        if (0 == bytelimit)
        {
          foreach (var throttledStream in throttledStreams) throttledStream.MaxBytesPerSecond = balance_speed;
        }
        else
        {
          int main_speed = bytelimit - ((throttledStreams.Count - 1) * MinLimit);
          if (main_speed < MinLimit) main_speed = MinLimit;

          throttledStreams[0].MaxBytesPerSecond = main_speed;
          for (int i = 1; i < throttledStreams.Count; i++) throttledStreams[i].MaxBytesPerSecond = MinLimit;
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

  internal class ThrottledStream : Stream
  {
    internal static BalanceThrottledStream Up = new BalanceThrottledStream(false);
    internal static BalanceThrottledStream Down = new BalanceThrottledStream(true);


    readonly Stream parent;
    readonly System.Timers.Timer timer;
    readonly AutoResetEvent wh = new AutoResetEvent(true);
    long processed = 0;
    internal FinishTransfer finishTransfer;
#if DEBUG
    long tranfered = 0;
#endif


    int maxBytesPerSecond = int.MaxValue;
    public int MaxBytesPerSecond
    {
      get { return maxBytesPerSecond; }
      set
      {
        if (value < 1) throw new ArgumentException("MaxBytesPerSecond has to be >0");
        maxBytesPerSecond = value;
      }
    }

    public ThrottledStream(Stream parent,bool download = true)
    {
      if (null == parent) throw new ArgumentNullException(nameof(parent));
      this.parent = parent;

      timer = new System.Timers.Timer();
      timer.Interval = 1000;
      timer.AutoReset = true;
      timer.Elapsed += Timer_Elapsed;
      timer.Start();

      if (download) Down.Add(this);
      else Up.Add(this);
    }

    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      lock (wh)
      {
#if DEBUG
        tranfered += processed;
        CPPCLR_Callback.OutPutDebugString(string.Format("Speed processed:{0}, tranfered:{1}", 
          UnitConventer.ConvertSize(processed,2,UnitConventer.unit_speed),
          UnitConventer.ConvertSize(tranfered, 2, UnitConventer.unit_size)));
#endif
        processed = 0;
      }
      wh.Set();
    }
    protected void Throttle(int bytes)
    {
      lock (wh) processed += bytes;
      if (processed >= maxBytesPerSecond) wh.WaitOne();
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
      int read = parent.Read(buffer, offset, count);
      Throttle(read);
      return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      parent.Write(buffer, offset, count);
      Throttle(count);
    }

    protected override void Dispose(bool disposing)
    {
      timer.Close();
      parent.Close();
      finishTransfer(this);
      base.Dispose(disposing);
    }

    public override void SetLength(long value) => parent.SetLength(value);
    public override bool CanRead => parent.CanRead;
    public override bool CanSeek => parent.CanSeek;
    public override bool CanWrite => parent.CanWrite;
    public override long Length => parent.Length;
    public override long Position { get => parent.Position; set => parent.Position = value; }
    public override void Flush() => parent.Flush();
    public override long Seek(long offset, SeekOrigin origin) => parent.Seek(offset, origin);
    public override bool CanTimeout => parent.CanTimeout;
    public override int ReadTimeout { get => parent.ReadTimeout; set => parent.ReadTimeout = value; }
    public override int WriteTimeout { get => parent.WriteTimeout; set => parent.WriteTimeout = value; }
  }
}
