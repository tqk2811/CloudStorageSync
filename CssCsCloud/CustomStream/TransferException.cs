using System;

namespace CssCsCloud.CustomStream
{
  public class TransferException : Exception
  {
    public TransferException() : base() { }
    public TransferException(string message) : base(message) { }
    public TransferException(string message, Exception innerException) : base(message, innerException) { }
  }
}
