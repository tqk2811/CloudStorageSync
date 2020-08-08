using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CssCsData
{
  internal static class Extensions
  {
    internal static string MakeSplitString(this IList<string> ids)
    {
      if (ids == null) return null;
      StringBuilder stringBuilder = new StringBuilder();
      if (ids.Count > 0) stringBuilder.Append(ids[0]);
      for (int i = 1; i < ids.Count; i++)
      {
        stringBuilder.Append('|');
        stringBuilder.Append(ids[i]);
      }
      return stringBuilder.ToString();
    }
    internal static IList<string> StringSplit(this string parents)
    {
      List<string> list = new List<string>();
      if (!string.IsNullOrEmpty(parents)) list.AddRange(parents.Split(new char[] { '|' }));
      return list;
    }
  }
}
