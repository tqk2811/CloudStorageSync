using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CssCs.StreamLimit
{
  internal class ThrottledStream : Stream
  {
    readonly Stream parent;
    Throttle throttle;
    public ThrottledStream(Stream parent, Throttle throttle)
    {
      if (null == parent) throw new ArgumentNullException("parent");
      if (null == throttle) throw new ArgumentNullException("throttle");
      this.parent = parent;
      this.throttle = throttle;
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
      int read = parent.Read(buffer, offset, count);
      throttle(read);
      return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throttle(count);
      parent.Write(buffer, offset, count);
    }



    public override void Close()
    {
      parent.Close();
      base.Close();
    }
    protected override void Dispose(bool disposing)
    {
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
