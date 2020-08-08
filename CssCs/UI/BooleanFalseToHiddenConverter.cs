using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CssCs.UI
{
  public class BooleanFalseToHiddenConverter : IValueConverter
  {
    public bool IsReversed { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      bool val = (bool)value;//
      if (this.IsReversed) val = !val;
      if (val) return Visibility.Visible;
      else return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
