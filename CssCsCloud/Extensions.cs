using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCsCloud
{
  internal static class Extensions
  {
    internal static long GetUnixTimeSeconds(this DateTime dt) => new DateTimeOffset(dt).ToUnixTimeSeconds();
    internal static string ParentsCommaSeparatedList(this IList<string> ids)
    {
      if (ids == null || ids.Count == 0) return string.Empty;
      StringBuilder stringBuilder = new StringBuilder();
      if (ids.Count > 0) stringBuilder.Append(ids[0]);
      for (int i = 1; i < ids.Count; i++)
      {
        stringBuilder.Append(',');
        stringBuilder.Append(ids[i]);
      }
      return stringBuilder.ToString();
    }
  }
}
