using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Cobra.Common;

namespace Cobra.UFPSBSPanel
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class HiddenConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            bool boolval = (bool)value;
            Visibility bvisib = Visibility.Visible;
            switch (boolval)
            {
                case true:
                    bvisib = Visibility.Visible;
                    break;
                case false:
                    bvisib = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
            return bvisib;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
