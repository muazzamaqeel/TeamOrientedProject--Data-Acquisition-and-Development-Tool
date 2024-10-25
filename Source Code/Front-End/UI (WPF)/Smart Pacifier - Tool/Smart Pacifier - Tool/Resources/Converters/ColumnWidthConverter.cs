using System.Globalization;
using System.Windows.Data;

namespace Smart_Pacifier___Tool.Resources.Converters
{
    public class ColumnWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double totalWidth = (double)value;
            double proportion = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return totalWidth * proportion;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
