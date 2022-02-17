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
using Cobra.CalibratePanel.Group;
using Cobra.Common;

namespace Cobra.CalibratePanel.SubGroup
{
    /// <summary>
    /// Interaction logic for volCalibControl.xaml
    /// </summary>
    public partial class volCalibControl : UserControl
    {
        private volPanel m_parent;
        public volPanel parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private double m_Vol;
        public double vol { get => m_Vol; set => m_Vol = value; }

        private Model m_cal_model;
        public Model cal_model
        {
            get { return m_cal_model; }
            set { m_cal_model = value; }
        }

        private Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();
        public volCalibControl()
        {
            InitializeComponent();
        }

        public volCalibControl(object pParent, Model model)
        {
            InitializeComponent();
            parent = (volPanel)pParent;
            cal_model = model;
        }

        private void volBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mod = null;
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            mod = btn.DataContext as Model;
            if (mod == null) return;

            if (!Double.TryParse(volTb.Text.Trim(), out m_Vol))
            {
                parent.parent.gm.level = 1;
                parent.parent.gm.controls = btn.Name;
                parent.parent.gm.message = "The calibrate point(s) should be invalid,please check!";
                parent.parent.gm.bupdate = true;
                parent.parent.CallWarningControl(parent.parent.gm);
                return;
            }

            if ((m_Vol > mod.dbPhyMax) | (m_Vol < mod.dbPhyMin))
            {
                parent.parent.gm.level = 1;
                parent.parent.gm.controls = btn.Name;
                parent.parent.gm.message = "The calibrate point(s) should be invalid,please check!";
                parent.parent.gm.bupdate = true;
                parent.parent.CallWarningControl(parent.parent.gm);
                return;
            }


            parent.parent.msg.owner = this;
            parent.parent.msg.gm.sflname = parent.parent.sflname;
            if (parent.parent.parent.bBusy)
            {
                parent.parent.gm.level = 1;
                parent.parent.gm.controls = btn.Name;
                parent.parent.gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                parent.parent.gm.bupdate = true;
                parent.parent.CallWarningControl(parent.parent.gm);
                return;
            }
            else
                parent.parent.parent.bBusy = true;

            parent.parent.msg.brw = false;
            parent.parent.msg.task = TM.TM_SPEICAL_GETDEVICEINFOR;
            parent.parent.parent.AccessDevice(ref parent.parent.m_Msg);
            while (parent.parent.msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (parent.parent.msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                parent.parent.gm.level = 2;
                parent.parent.gm.message = LibErrorCode.GetErrorDescription(parent.parent.msg.errorcode);
                parent.parent.CallWarningControl(parent.parent.gm);
                parent.parent.parent.bBusy = false;
                return;
            }

            parent.parent.msg.task = TM.TM_COMMAND;
            parent.parent.msg.sub_task_json = BuildJsonTask("TM_CALIBRATION", "Voltage");
            parent.parent.msg.task_parameterlist.parameterlist.Clear();
            parent.parent.msg.task_parameterlist.parameterlist.Add(mod.parent);
            parent.parent.parent.AccessDevice(ref parent.parent.m_Msg);
            while (parent.parent.msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (parent.parent.msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                parent.parent.gm.level = 2;
                parent.parent.gm.message = LibErrorCode.GetErrorDescription(parent.parent.msg.errorcode);
                parent.parent.CallWarningControl(parent.parent.gm);
                parent.parent.parent.bBusy = false;
                return;
            }
            parent.parent.parent.bBusy = false;
            parent.parent.m_Debug_Infors.Add(new Infor(parent.parent.msg.sub_task_json));
        }

        internal string BuildJsonTask(string key, string value)
        {
            subTask_Dic.Clear();
            subTask_Dic.Add("SFL", parent.parent.sflname);
            subTask_Dic.Add(key, value);
            subTask_Dic.Add("Point1", vol.ToString());
            return SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
        }
    }
}