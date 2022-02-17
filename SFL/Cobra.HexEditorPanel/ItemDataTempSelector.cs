using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Cobra.HexEditorPanel
{
    public class ItemDataTempSelector : DataTemplateSelector
    {
        public DataTemplate projFileExistDataTemp { get; set; }
        public DataTemplate projFileNullDataTemp { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement elemnt = container as FrameworkElement;
            Model pf = item as Model;
            if (pf == null) return elemnt.FindResource("ProjFileNullDataTemp") as DataTemplate;

            if (pf.bExist)
                return elemnt.FindResource("ProjFileExistDataTemp") as DataTemplate;
            else
                return elemnt.FindResource("ProjFileNullDataTemp") as DataTemplate;
        }
    }
}