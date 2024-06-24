using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BreadcrumbLib
{
	public class ImageToImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Image originalImage = value as Image;
			if(originalImage != null)
			{
				Image image = new Image();
				image.Width = originalImage.Width;
				image.Height = originalImage.Height;
				image.Source = originalImage.Source;
				return image;
			}
			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
