using System;
using System.IO;

namespace CssCsCloud.CustomStream
{
  internal class LimitTransferStream : Stream
  {
    readonly Stream parent;
    readonly long transferLimit;
    long transfered = 0;
    public LimitTransferStream(Stream parent, long transferLimit)
    {
      this.parent = parent;
      this.transferLimit = transferLimit;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      long bytesread = transferLimit - transfered;
      if (bytesread == 0) return 0;
      if (bytesread > count) bytesread = count;
      int readed = parent.Read(buffer, offset, (int)bytesread);
      transfered += readed;
      return readed;
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
      long byteswrite = transferLimit - transfered;
      if (byteswrite == 0) return;
      if (byteswrite > count) byteswrite = count;
      parent.Write(buffer, offset, (int)byteswrite);
      transfered += byteswrite;
    }












    public override bool CanRead => parent.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => parent.CanWrite;

    public override long Length => transferLimit;

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
      throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }
  }
}
