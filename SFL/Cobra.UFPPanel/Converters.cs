using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Cobra.Common;

namespace Cobra.UFPPanel
{
    [ValueConversion(typeof(byte), typeof(string))]
    public class byte2StringConvert : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            byte bVal = (byte)value;
            return string.Format("0x{0:x2}", bVal);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte bVal = 0;
            string sVal = (string)value;
            try
            {
                bVal = System.Convert.ToByte(sVal, 16);
            }
            catch (System.Exception ex)
            {
            	
            }
            return bVal;
        }
    }

    [ValueConversion(typeof(double), typeof(string))]
    public class data2StringConvert : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            double yVal = (double)value;
            Visibility vTgt = Visibility.Visible;
            if (yVal == 0)
            {
                vTgt = Visibility.Collapsed;
            }
            return vTgt;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(byte), typeof(Visibility))]
    public class byte2VisibilityConvert : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            byte yVal = (byte)value;
			Visibility vTgt = Visibility.Visible;
			if (yVal == 0)
            {
				vTgt = Visibility.Collapsed;
            }
            return vTgt;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Multi16Bits2Width : IMultiValueConverter
    {
        public Object Convert(Object[] value, Type targetType, Object parameter, CultureInfo culture)
        {
            int rate = (int)value[0];
            double dbWidth = (double)value[1];
            return (double)(dbWidth / rate);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

	public class Multi08Bits2Width : IMultiValueConverter
	{
		public Object Convert(Object[] value, Type targetType, Object parameter, CultureInfo culture)
		{
			byte yVal = (byte)value[0];
			double dbActual = 0;
			double dbWidth = 0;
			if (value[1] == null)
			{
			}
			else
			{
				dbActual = (double)value[1] / 8;
				if ((yVal == 0) || (dbActual == 0))
				{
				}
				else
				{
					dbWidth = dbActual * yVal;
				}
			}
			return dbWidth;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class DataComponentSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FrameworkElement fwelement = container as FrameworkElement;
			if ((fwelement != null) && (item != null) && (item is Model))
			{
                Model model = item as Model;
                switch (model.parent.subsection)
                {
                    case 0:
                    case 1:
                    case 2:
                        return fwelement.FindResource("DataTmp") as DataTemplate;
                    case 3:
                        return fwelement.FindResource("DataTmp2") as DataTemplate;
                    case 4:
                        return fwelement.FindResource("DataTmp4") as DataTemplate;
                    /*
                    case 1:
                        return fwelement.FindResource("DataTmp1") as DataTemplate;*/
                }

			}
			return base.SelectTemplate(item, container);
		}
	}
	
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

	public class Bool2BrushBackground : IValueConverter
	{
		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			bool boolval = (bool)value;
			Brush brush = null;
			if (boolval)
			{
				brush = new SolidColorBrush(Colors.LightSlateGray);
			}
			else
			{
				brush = new SolidColorBrush(Colors.WhiteSmoke);
			}

			return brush;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
