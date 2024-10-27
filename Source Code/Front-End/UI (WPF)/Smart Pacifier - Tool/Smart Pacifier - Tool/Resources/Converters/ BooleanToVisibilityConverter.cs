using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Smart_Pacifier___Tool.Converters
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool boolValue)
			{
				bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool paramValue) && paramValue;
				if (invert)
				{
					return boolValue ? Visibility.Collapsed : Visibility.Visible;
				}
				return boolValue ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Visibility visibility)
			{
				bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool paramValue) && paramValue;
				if (invert)
				{
					return visibility == Visibility.Collapsed;
				}
				return visibility == Visibility.Visible;
			}
			return false;
		}
	}
}