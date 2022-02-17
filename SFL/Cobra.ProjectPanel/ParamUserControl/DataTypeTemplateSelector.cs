using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace Cobra.ProjectPanel.Param
{
    public class DataTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }
        public DataTemplate TextBox1Template { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) 
        {
            UInt16 controlType = (UInt16)editortype.TextBox_EditType;
            if (item != null)
            {
                SFLModel param = item as SFLModel;
                controlType = param.editortype;

                switch (controlType)
                {
                    case (UInt16)editortype.TextBox_EditType:
                        return TextBoxTemplate;
                    case (UInt16)editortype.ComboBox_EditType:
                        return ComboBoxTemplate;
                    case (UInt16)editortype.CheckBox_EditType:
                        return CheckBoxTemplate;
                    case (UInt16)editortype.TextBox1_EditType:
                        return TextBox1Template;
                    default:
                        return TextBoxTemplate;
                }
            }
            return TextBoxTemplate;
        }
    }
}
