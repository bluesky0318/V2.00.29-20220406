using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace Cobra.UFPSBSPanel
{
    public class DataTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) 
        {
            UInt16 controlType = (UInt16)editortype.TextBox_EditType;
            if (item != null)
            {
                Model param = item as Model;
                controlType = param.editortype;

                switch (controlType)
                {
                    case (UInt16)editortype.TextBox_EditType:
                        return TextBoxTemplate;
                    case (UInt16)editortype.ComboBox_EditType:
                        return ComboBoxTemplate;
                    default:
                        return TextBoxTemplate;
                }
            }
            return TextBoxTemplate;
        }
    }
}
