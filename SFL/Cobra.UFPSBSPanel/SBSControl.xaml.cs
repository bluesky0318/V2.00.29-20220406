using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Cobra.UFPSBSPanel
{
    /// <summary>
    /// Interaction logic for SBSControl.xaml
    /// </summary>
    public partial class SBSControl : UserControl
    {
        public  MainControl parent { get; set; }
        public SBSControl()
        {
            InitializeComponent();
        }

        public void Init(object pParent)
        {
            parent = (MainControl)pParent;

            dynamicDG.ItemsSource = parent.viewmode.sfl_dynamic_parameterlist;
            staticDG.ItemsSource = parent.viewmode.sfl_static_parameterlist;
            eventDG.ItemsSource = parent.viewmode.sfl_event_parameterlist;

            regctrl.Init(parent);
            InitUI();
        }

        public void InitUI()//后期弹性定制
        {
            //fixResizePanel.Rows = 1;
            foreach(Model model in parent.viewmode.sfl_parameterlist)
            {
                switch ((ElementDefine.SBS_PARAM_SHOWMODE)model.showMode)
                {
                    case ElementDefine.SBS_PARAM_SHOWMODE.PARAM_DEFAULT:
                        continue;
                    case ElementDefine.SBS_PARAM_SHOWMODE.PARAM_DYNAMIC:
                        dyResizePanel.Children.Add(model.waveControl);
                        break;
                    case ElementDefine.SBS_PARAM_SHOWMODE.PARAM_FIXED:
                        //fixResizePanel.Children.Add(model.waveControl);
                        break;
                }
            }
        }

        private void dynamicDG_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Model model = (Model)dynamicDG.SelectedItem;
            if (model.showMode == ElementDefine.SBS_PARAM_SHOWMODE.PARAM_FIXED) return;
            dyResizePanel.Children.Clear();
            dyResizePanel.Children.Add(model.waveControl);
        }

        //UI初始化
        public void Reset()
        {
            foreach (Model mode in parent.viewmode.sfl_dynamic_parameterlist)
            {
                mode.waveControl.Reset();
            }
        }

        //数据更新
        public void update()
        {
            foreach (Model mode in parent.viewmode.sfl_dynamic_parameterlist)
            {
                mode.waveControl.Update(mode.data);
            }
        }
    }
}
