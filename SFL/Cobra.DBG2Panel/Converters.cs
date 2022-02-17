using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Cobra.Common;

namespace Cobra.DBG2Panel
{
	public class Bool2BrushConverter : IValueConverter
	{
		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			bool boolval = (bool)value;
			Brush brush = null;
			if (boolval)
			{
				brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
			}
			else
			{
				brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
			}

			return brush;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
