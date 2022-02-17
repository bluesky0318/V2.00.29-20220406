using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Cobra.ProjectPanel
{
    public class ItemDataTempSelector : DataTemplateSelector
    {
        public DataTemplate projFileExistDataTemp { get; set; }
        public DataTemplate projFileNullDataTemp { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement elemnt = container as FrameworkElement;
            ProjFile pf = item as ProjFile;
            if (pf == null) return elemnt.FindResource("ProjFileNullDataTemp") as DataTemplate;

            if (pf.bExist)
                return elemnt.FindResource("ProjFileExistDataTemp") as DataTemplate;
            else
                return elemnt.FindResource("ProjFileNullDataTemp") as DataTemplate;
        }
    }
}