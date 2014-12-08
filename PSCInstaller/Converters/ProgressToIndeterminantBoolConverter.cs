using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PSCInstaller.Converters
{
    [ValueConversion(typeof(double), typeof(bool))]
    public class ProgressToIndeterminantBoolConverter : IValueConverter
    {
        public int IndeterminateThreshhold { get; set; }

        public ProgressToIndeterminantBoolConverter()
        {
            IndeterminateThreshhold = 100;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int progress = (int)((double)value);
            return progress < IndeterminateThreshhold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
