using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Cobra.Common;

namespace Cobra.LotusEvPanel
{
	public class DCLDOSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FrameworkElement fwelement = container as FrameworkElement;
			if ((fwelement != null) && (item != null) && (item is DCLDOModel))
			{
				DCLDOModel myModel = item as DCLDOModel;
				if (myModel.yDCCatagory == 0x01)
				{
					return fwelement.FindResource("TemplateDCMargin") as DataTemplate;
				}
				else
				{
					return fwelement.FindResource("TemplateDCVolt") as DataTemplate;
				}
			}
			return base.SelectTemplate(item, container);
		}
	}
}
