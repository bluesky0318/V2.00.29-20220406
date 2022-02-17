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
using System.Collections.ObjectModel;
using System.Reflection;
using System.ComponentModel;
using Cobra.EM;
using Cobra.Common;

namespace Cobra.CalibratePanel
{
    /// <summary>
    /// MainControl.xaml 的交互逻辑
    /// </summary>
    public partial class MainControl
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

        public TASKMessage m_Msg = new TASKMessage();
        public TASKMessage msg
        {
            get { return m_Msg; }
            set { m_Msg = value; }
        }

        private ViewMode m_viewmode;
        public ViewMode viewmode
        {
            get { return m_viewmode; }
            set { m_viewmode = value; }
        }

        public ObservableCollection<Infor> m_Debug_Infors = new ObservableCollection<Infor>();
        public GeneralMessage gm = new GeneralMessage("Calibrate SFL", "", 0);
        public MainControl(object pParent, string name)
        {
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;

            this.InitializeComponent();
            infoLb.ItemsSource = m_Debug_Infors;

            m_Debug_Infors.Clear();
            m_Debug_Infors.Add(new Infor("Welcome to calibrate SFL...."));

            gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            msg.gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);

            viewmode = new ViewMode(pParent, this);
            initUI();
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.SFL);
            #endregion
        }

        void gm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            parent.gm = (GeneralMessage)sender;
        }

        void msg_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
                        case COMMON_CONTROL.COMMON_CONTROL_SELECT:
                            {
                                CallSelectControl(msg.gm);
                                break;
                            }
                    }
                    break;
            }
        }

        #region 通用控件消息响应
        public void CallWarningControl(GeneralMessage message)
        {
            WarningPopControl.Dispatcher.Invoke(new Action(() =>
            {
                WarningPopControl.ShowDialog(message);
            }));
        }

        public void CallSelectControl(GeneralMessage message)
        {
            SelectPopControl.Dispatcher.Invoke(new Action(() =>
            {
                msg.controlmsg.bcancel = SelectPopControl.ShowDialog(message);
            }));
        }
        #endregion

        private void initUI()
        {
            Group.curPanel curPan = new Group.curPanel(this);
            Group.volPanel volPan = new Group.volPanel(this);
            Group.tmpPanel tmpPan = new Group.tmpPanel(this);
            Group.miscPanel miscPan = new Group.miscPanel(this);
            if(viewmode.sfl_cur_parameterlist.Count !=0)
                calPanel.Children.Add(curPan);
            if (viewmode.sfl_vol_parameterlist.Count != 0)
                calPanel.Children.Add(volPan);
            if (viewmode.sfl_temp_parameterlist.Count != 0)
                calPanel.Children.Add(tmpPan);
            if (viewmode.sfl_misc_parameterlist.Count != 0)
                calPanel.Children.Add(miscPan);
        }
    }
}
