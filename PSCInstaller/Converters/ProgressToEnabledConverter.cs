using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PSCInstaller.Converters
{
    [ValueConversion(typeof(double), typeof(bool))]
    public class ProgressToEnabledConverter : IValueConverter
    {
        public int ProgressMaxValue { get; set; }

        public ProgressToEnabledConverter()
        {
            ProgressMaxValue = 100;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int progress = (int)((double)value);
            return progress >= ProgressMaxValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
