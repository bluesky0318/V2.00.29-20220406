using System;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.ComponentModel;
using System.Data;
using Cobra.EM;
using Cobra.Common;

namespace Cobra.BlackBoxPanel
{
    public enum LOG_TYPE : ushort
    {
        COUNT_LOG,
        STATISTIC_LOG,
        EVENT_LOG,
        MAXMIN_LOG,
        EVENT2_LOG,
    }
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        //父对象保存
        private Device m_parent;
        public Device parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private string m_SFLname;
        public string sflname
        {
            get { return m_SFLname; }
            set { m_SFLname = value; }
        }

        public GeneralMessage gm = new GeneralMessage("BlackBox SFL", "", 0);
        public TASKMessage m_Msg = new TASKMessage();
        public TASKMessage msg
        {
            get { return m_Msg; }
            set { m_Msg = value; }
        }
        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;

            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.SFL);
            gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            msg.gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);

            WarningPopControl.SetParent(LayoutRoot);
            WaitPopControl.SetParent(LayoutRoot);
            #endregion
            UC_Init();
        }

        private void UC_Init()
        {
            StatisticLogUC.init(parent, sflname);
            EventLogUC.init(parent, sflname);
            EventLog2UC.init(parent, sflname); 
        }

        #region 通用控件消息响应
        private void gm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            parent.gm = (GeneralMessage)sender;
        }

        private void msg_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TASKMessage msg = sender as TASKMessage;
            switch (e.PropertyName)
            {
                case "controlreq":
                    switch (msg.controlreq)
                    {
                        case COMMON_CONTROL.COMMON_CONTROL_WARNING:
                            {
                                CallWarningControl(msg.gm);
                                break;
                            }

                        case COMMON_CONTROL.COMMON_CONTROL_WAITTING:
                            {
                                CallWaitControl(msg.controlmsg);
                                break;
                            }
                    }
                    break;
            }
        }
        public void CallWarningControl(GeneralMessage message)
        {
            WarningPopControl.Dispatcher.Invoke(new Action(() =>
            {
                WarningPopControl.ShowDialog(message);
            }));
        }

        public void CallWaitControl(ControlMessage msg)
        {
            WaitPopControl.Dispatcher.Invoke(new Action(() =>
            {
                WaitPopControl.IsBusy = msg.bshow;
                WaitPopControl.Text = msg.message;
                WaitPopControl.Percent = String.Format("{0}%", msg.percent);
            }));
        }
        #endregion
    }
}
