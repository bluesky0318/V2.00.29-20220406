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
    /// Interaction logic for curCalibControl.xaml
    /// </summary>
    public partial class curCalibControl : UserControl
    {
        private curPanel m_parent;
        public curPanel parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private Model m_cal_model;
        public Model cal_model
        {
            get { return m_cal_model; }
            set { m_cal_model = value; }
        }

        private double m_Point1;
        public double point1
        { get => m_Point1; set => m_Point1 = value; }
        private double m_Point2;
        public double point2
        { get => m_Point2; set => m_Point2 = value; }

        private Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();
        public curCalibControl()
        {
            InitializeComponent();
        }

        public curCalibControl(object pParent, Model model)
        {
            InitializeComponent();
            parent = (curPanel)pParent;
            cal_model = model;
        }

        private void calBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mod = null;
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            mod = btn.DataContext as Model;
            if (mod == null) return;

            if((!Double.TryParse(point1Tb.Text.Trim(),out m_Point1))|(!Double.TryParse(point2Tb.Text.Trim(), out m_Point2)))
            {
                parent.parent.gm.level = 1;
                parent.parent.gm.controls = btn.Name;
                parent.parent.gm.message = "The Point input illegal,Please check!";
                parent.parent.gm.bupdate = true;
                parent.parent.CallWarningControl(parent.parent.gm);
                return;
            }
            if((m_Point1 > mod.dbPhyMax) | (m_Point1 < mod.dbPhyMin)|(m_Point2 > mod.dbPhyMax) | (m_Point2 < mod.dbPhyMin) | (m_Point2 == m_Point1))
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
            parent.parent.msg.sub_task_json = BuildJsonTask("TM_CALIBRATION", "Current");
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
            subTask_Dic.Add("Point1",point1.ToString());
            subTask_Dic.Add("Point2", point2.ToString());
            return SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
        }
    }
}
