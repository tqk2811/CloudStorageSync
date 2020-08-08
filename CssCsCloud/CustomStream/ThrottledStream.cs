using System;
using System.IO;
using System.Threading;

namespace CssCsCloud.CustomStream
{
  internal class ThrottledStream : Stream
  {
    internal static BalanceThrottledStream Up = new BalanceThrottledStream(false);
    internal static BalanceThrottledStream Down = new BalanceThrottledStream(true);

    readonly object _lock = new object();
    readonly Stream parent;
    readonly System.Timers.Timer timer;
    readonly AutoResetEvent wh = new AutoResetEvent(true);
    long processed = 0;
    internal FinishTransfer finishTransfer;

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

    public ThrottledStream(Stream parent, bool download = true)
    {
      this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

      timer = new System.Timers.Timer
      {
        Interval = 1000,
        AutoReset = true
      };
      timer.Elapsed += Timer_Elapsed;
      timer.Start();

      if (download) Down.Add(this);
      else Up.Add(this);
    }

    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      lock (_lock) processed = 0;
      wh.Set();
    }
    protected void Throttle(int bytes)
    {
      lock (_lock) processed += bytes;
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
