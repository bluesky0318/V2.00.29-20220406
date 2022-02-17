using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Cobra.Common;

namespace Cobra.FSBS2Panel
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
    class VolBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            double width;
            int iCount = (int)value[0];
            double xWidth = (double)value[1];
            width = xWidth / iCount - 4.0;
            if (width < 20)
                width = 20;
            return width;
        }

        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class WidthConverter2 : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            double p;
            if (param is string)
                p = double.Parse(param as string);
            else
                p = (double)(param);
            double width = (double)value - p;
            return width;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class PositionConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            int ipos = (int)value;
            return (ipos % 2) + 1;
        }

        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class PositionConverter2 : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            double ipos = (double)value;
            return (ipos / 2.0)+5.0;
        }

        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class BarConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            if (Double.IsNaN((Double)value[3]))
            {
                return null;
            }
            double res;
            if (!Double.TryParse(value[0].ToString(), out res))
                value[0] = (double)5000;
            if (!Double.TryParse(value[1].ToString(), out res))
                value[1] = (double)0;
            if (!Double.TryParse(value[2].ToString(), out res))
                value[2] = (double)0;
            double a, b, y;
            a = ((double)value[3] * 0.80) / ((double)value[0] - (double)value[1]);
            b = ((double)value[3] * 0.1) - a * (double)value[1];
            y = a * (double)value[2] + b;
            if (y < 0)
                y = 0;
            else if (y > (double)value[3])
                y = (double)value[3];
            return y;
        }

        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    public class DataComponentSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement fwelement = container as FrameworkElement;
            if (item is Model)
            {
                Model model = item as Model;
                switch (model.subType)
                {
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_STATIC:
                        return fwelement.FindResource("StaticDG") as DataTemplate;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT:
                        return fwelement.FindResource("EventDG") as DataTemplate;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_DYNAMIC:
                        return fwelement.FindResource("DynamicDG") as DataTemplate;
                }

            }
            return base.SelectTemplate(item, container);
        }
    }
    class CurrentBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            if (value[0] == null || value[1] == null || value[2] == null)   //如果值不存在，则下限设为0，上限设为5000
            {
                value[0] = (double)5000;
                value[1] = (double)0;
                value[2] = (double)0;
            }
            double a, b, y, r;
            a = ((double)value[3] * 0.80) / ((double)value[0] - (double)value[1]);
            b = ((double)value[3] * 0.1) - a * (double)value[1];

            y = Math.Abs(a * (double)value[2]);

            if (y < 0)
                y = 1;
            else
            {
                r = (double)value[3] - b;
                if ((double)value[2] >= 0)
                {
                    if (y > r)
                        y = r;
                }
                else
                {
                    if (y > b)
                        y = b;
                }
            }
            return y;
        }

        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class CurrentBarLeftConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            if (value[0] == null || value[1] == null || value[2] == null)   //如果值不存在，则下限设为0，上限设为5000
            {
                value[0] = (double)5000;
                value[1] = (double)0;
                value[2] = (double)0;
            }
            double a, b, y;
            a = ((double)value[3] * 0.80) / ((double)value[0] - (double)value[1]);
            b = ((double)value[3] * 0.1) - a * (double)value[1];

            if ((double)value[2] >= 0)
            {
                y = b;
            }
            else
            {
                y = a * (double)value[2] + b;
            }

            if (y < 0)
                y = 0;
            else if (y > (double)value[3])
                y = (double)value[3];
            return y;
        }

        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class ColorConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            if (value[0] == null || value[1] == null || value[2] == null)   //如果值不存在，则直接返回，返回值已不重要，因为后续不会处理
                return Brushes.Gray;
            Brush b;
            if ((double)value[0] < (double)value[2] || (double)value[1] > (double)value[2])
            {
                b = Brushes.Red;
            }
            else
                b = Brushes.Gray;
            if (value[3] != null)
                if ((bool)value[3])
                    b = Brushes.Gray;
            return b;
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class WidthConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (value == null)  //如果值不存在，则直接返回，返回值已不重要，因为后续不会处理
                return "";
            string str;
            decimal num = new decimal((double)value);
            if (num == -999999)
                str = "No Value";
            else
                str = Decimal.Round(num, 1).ToString();
            return str;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class MidConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            double width = (double)value / 2 - 3;
            return width;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class MainWidthConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            double width = (double)value * Int32.Parse(param as string) / 100;
            return width;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class HeightConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            double height = ((int)value + 1) * ((Double)param + 4) - 4;
            return height;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class CanvasHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            double height;
            height = ((double)value[1] + 4) * ((int)value[0] + 0);
            return height;
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class TempBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            double width;
            width = ((double)value[2] - (double)param * 2) / ((int)value[0] + (int)value[1]) - 4; //20是TH的宽度，4是margin的宽度
            if (width < 30)
                width = 30;
            return width;
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class GlobalMargintConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            double height;
            height = 0.2 * (double)value[0] / ((int)value[1] + 3);
            return height;
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class TimerConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            Visibility v = (value == null) ? Visibility.Hidden : Visibility.Visible;
            return v;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class BleedingConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            Brush br;
            if ((bool?)value == null)
                br = Brushes.Black;
            else if ((bool?)value == false)
                br = Brushes.Black;
            else
                br = Brushes.LightGreen;

            return br;
        }
        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class EnableConverter1 : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            int Count = (int)value[0];
            bool isChecked = (bool)value[1];
            if (isChecked)
                return false;
            return (Count == 1);
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class EnableConverter2 : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            int Count = (int)value[0];
            bool isChecked = (bool)value[1];
            if (isChecked)
                return false;
            return (Count >= 1);
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class FDColorConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object param, CultureInfo culture)
        {
            if (value == null) return Brushes.Black;
            Brush br = (((double)value) > 0) ? Brushes.Red  : Brushes.Black;
            return br;
        }

        public object ConvertBack(object value, Type typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class ShiftConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            double shift;
            shift = ((double)value[1] - (double)value[0]) / 2;
            return shift;
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    class WidthRatioConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typetarget, object param, CultureInfo culture)
        {
            double ratio;
            ratio = (int)value[0] * ((double)value[1] + 4) + (double)param;
            return ratio;
        }
        public object[] ConvertBack(object value, Type[] typetarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}
