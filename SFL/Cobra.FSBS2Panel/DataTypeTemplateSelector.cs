using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace Cobra.FSBS2Panel
{
    public class ButtonTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DetailBtnTemplate { get; set; }
        public DataTemplate WriteBtnTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null)
            {
                Model param = item as Model;
                switch (param.catalog)
                {
                    case "Writable":
                        return WriteBtnTemplate;
                    default:
                        return DetailBtnTemplate;
                }
            }
            return DetailBtnTemplate;
        }
    }
    public class DataTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ROTextBoxTemplate { get; set; }
        public DataTemplate WRTextBoxTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) 
        {
            UInt16 controlType = (UInt16)editortype.TextBox_RO_EditType;
            if (item != null)
            {
                Model param = item as Model;
                controlType = param.editortype;

                switch (controlType)
                {
                    case (UInt16)editortype.TextBox_RO_EditType:
                        return ROTextBoxTemplate;
                    case (UInt16)editortype.ComboBox_EditType:
                        return ComboBoxTemplate;
                    case (UInt16)editortype.TextBox_WR_EditType:
                        return WRTextBoxTemplate;
                    default:
                        return ROTextBoxTemplate;
                }
            }
            return ROTextBoxTemplate;
        }
    }

    public class SetDataDataTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            UInt16 controlType = (UInt16)editortype.TextBox_RO_EditType;
            if (item != null)
            {
                setModel param = item as setModel;
                controlType = param.editortype;

                switch (controlType)
                {
                    case (UInt16)editortype.TextBox_RO_EditType:
                        return TextBoxTemplate;
                    case (UInt16)editortype.ComboBox_EditType:
                        return ComboBoxTemplate;
                    case (UInt16)editortype.CheckBox_EditType:
                        return CheckBoxTemplate;
                    default:
                        return TextBoxTemplate;
                }
            }
            return TextBoxTemplate;
        }
    }
}
