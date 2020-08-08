using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCs
{
  public static class UnitConventer
  {
    public static readonly string[] UnitSize = { "Byte", "Kib", "Mib", "Gib", "Tib" };
    public static readonly string[] UnitSpeed = { "Byte/s", "Kib/s", "Mib/s", "Gib/s", "Tib/s" };

    public static string ConvertSize(double num, int round, string[] unit, int div = 1024)
    {
      if (null == unit) throw new ArgumentNullException(nameof(unit));
#pragma warning disable CA1303 // Do not pass literals as localized parameters
      if (round < 0) throw new ArgumentException("round can't <0");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

      if (num == 0) return "0 " + unit[0];
      for (int i = 0; i < unit.Length; i++)
      {
        double sizeitem = num / Math.Pow(div, i);
        if (sizeitem < 1)
        {
          if (i == 0) return "0 " + unit[0];
          else return Math.Round((num / Math.Pow(div, i - 1)), round).ToString(CultureInfo.InvariantCulture) + " " + unit[(int)i - 1];
        }
      }
      return Math.Round((decimal)num / (decimal)Math.Pow(div, unit.Length - 1), round).ToString(CultureInfo.InvariantCulture) + " " + unit[unit.Length - 1];
    }
  }
}
