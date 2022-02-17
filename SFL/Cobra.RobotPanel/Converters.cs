using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Cobra.Common;

namespace Cobra.RobotPanel
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class Bool2VisibilityConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            bool bval = (bool)value;
            if (bval)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(UInt32), typeof(String))]
    public class UInt2StrConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            UInt32 uval = (UInt32)value;
            return string.Format("0x{0:x8}", uval);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UInt32 wval = 0;
            string str = (string)value;
            if (str.StartsWith("0x"))
                str = str.Substring("0x".Length);
            if (!UInt32.TryParse(str,NumberStyles.HexNumber,null, out wval))
                wval = 0;
            return wval;
        }
    }

    [ValueConversion(typeof(int), typeof(Brush))]
    public class Execute2BrushConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            int ival = (int)value;
            Brush brush = null;
            switch (ival)
            {
                case 0:
                    brush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    break;
                case 1:
                    brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                default:
                    break;
            }
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(int), typeof(bool))]
    public class Int2BoolConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            int ival = (int)value;
            switch(ival)
            {
                case 0: //read
                    return false;
                case 1: //write
                    return true;
                case 2: //RW
                    return false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
