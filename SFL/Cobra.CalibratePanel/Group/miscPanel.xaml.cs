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
using Cobra.CalibratePanel.SubGroup;
using Cobra.Common;

namespace Cobra.CalibratePanel.Group
{
    /// <summary>
    /// Interaction logic for curPanel.xaml
    /// </summary>
    public partial class miscPanel : UserControl
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public miscPanel(object pParent)
        {
            InitializeComponent();
            parent = (MainControl)pParent;
            init();
        }

        public void init()
        {
            foreach (Model model in parent.viewmode.sfl_misc_parameterlist)
            {
                Button mBtn = new Button();
                mBtn.Content = model.nickname;
                mBtn.Margin =new Thickness(5);
                mBtn.MinHeight = 30;
                mBtn.MinWidth = 80;
                mBtn.Click += MBtn_Click;
                mBtn.DataContext = model;
                miscPan.Children.Add(mBtn);
            }
        }

        private void MBtn_Click(object sender, RoutedEventArgs e)
        {
            Button mBtn = sender as Button;
            Model mod = mBtn.DataContext as Model;
            if (parent.parent.bBusy)
            {
                parent.gm.level = 1;
                parent.gm.controls = "Misc button";
                parent.gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                parent.gm.bupdate = true;
                parent.CallWarningControl(parent.gm);
                return;
            }
            else
                parent.parent.bBusy = true;

            parent.msg.task = TM.TM_COMMAND;
            parent.msg.sub_task = (UInt16)mod.data;
            parent.parent.AccessDevice(ref parent.m_Msg);
            while (parent.msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (parent.msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                parent.gm.level = 2;
                parent.gm.message = LibErrorCode.GetErrorDescription(parent.msg.errorcode);
                parent.CallWarningControl(parent.gm);
                parent.parent.bBusy = false;
                return;
            }
            parent.parent.bBusy = false;
            return;
        }
    }
}
