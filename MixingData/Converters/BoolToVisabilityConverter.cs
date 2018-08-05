using System;
using System.Globalization;
using System.Windows.Data;

namespace MixingData.Converters
{
    public class BoolToVisabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                switch ((bool)value)
                {
                    case true:
                        return System.Windows.Visibility.Hidden;
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
