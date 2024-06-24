using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace BreadcrumbLib
{
	public class DebugConverter:IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
