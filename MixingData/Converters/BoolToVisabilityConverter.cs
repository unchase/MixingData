using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MixingData.Converters
{
    public class BoolToVisabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isChecked = (bool)value;
                switch (isChecked)
                {
                    case true:
                        return System.Windows.Visibility.Hidden;
                    case false:
                        return System.Windows.Visibility.Visible;
                    default:
                        return System.Windows.Visibility.Visible;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
