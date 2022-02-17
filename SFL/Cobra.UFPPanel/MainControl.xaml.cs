using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
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
using System.Windows.Controls;
using Cobra.EM;
using Cobra.Common;

namespace Cobra.UFPPanel
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
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

        private TASKMessage m_Msg = new TASKMessage();
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

        private ControlMessage m_CtrlMg = new ControlMessage();
        public GeneralMessage gm = new GeneralMessage("UFP SFL", "", 0);

        public MainControl(object pParent, string name)
        {
            try
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

                viewmode = new ViewMode(pParent, this);
                #endregion

                ListCollectionView GroupedCustomers = new ListCollectionView(viewmode.sfl_parameterlist);
                GroupedCustomers.GroupDescriptions.Add(new PropertyGroupDescription("description"));
                paramListBox.ItemsSource = GroupedCustomers;
            }
            catch (System.Exception ex)
            {
                FolderMap.WriteFile(string.Format("{0}{1}", sflname, ex.Message.ToString()));
            }
        }

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

        #region 通用控件消息响应
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

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.CommandParameter == null)
            {
                viewmode.dm_part_parameterlist.parameterlist.Clear();
                viewmode.BuildPartParameterList(btn.DataContext);
                msg.task_parameterlist = viewmode.dm_part_parameterlist;
                Read();
            }
            else
            {
                TestRead(btn);
                return;
            }

        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.CommandParameter == null)
            {
                viewmode.dm_part_parameterlist.parameterlist.Clear();
                viewmode.BuildPartParameterList(btn.DataContext);
                msg.task_parameterlist = viewmode.dm_part_parameterlist;
                Write();
            }
            else
            {
                TestWrite(btn);
                return;
            }
        }

        private void TestRead(Button btn)
        {
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Read From Device button";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.task = TM.TM_COMMAND;
            msg.sub_task = 0;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            (btn.DataContext as Model).bReg0 = (byte)msg.sm.misc[0];
            (btn.DataContext as Model).bReg1 = (byte)msg.sm.misc[1];
            (btn.DataContext as Model).bReg2 = (byte)msg.sm.misc[2];
            (btn.DataContext as Model).bReg3 = (byte)msg.sm.misc[3];
            (btn.DataContext as Model).bReg4 = (byte)msg.sm.misc[4];
            (btn.DataContext as Model).bReg5 = (byte)msg.sm.misc[5];
        }

        private void TestWrite(Button btn)
        {
            msg.sm.misc[0] = (btn.DataContext as Model).bReg0;
            msg.sm.misc[1] = (btn.DataContext as Model).bReg1;
            msg.sm.misc[2] = (btn.DataContext as Model).bReg2;
            msg.sm.misc[3] = (btn.DataContext as Model).bReg3;
            msg.sm.misc[4] = (btn.DataContext as Model).bReg4;
            msg.sm.misc[5] = (btn.DataContext as Model).bReg5;

            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Read From Device button";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.task = TM.TM_COMMAND;
            msg.sub_task = 1;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
        }

        private void Read()
        {
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Read From Device button";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.task = TM.TM_READ;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            msg.task = TM.TM_CONVERT_HEXTOPHYSICAL;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            parent.bBusy = false;
            return;
        }

        private void Write()
        {
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Write To Device button!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();

            msg.task = TM.TM_WRITE;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            msg.task = TM.TM_READ;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            msg.task = TM.TM_CONVERT_HEXTOPHYSICAL;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();

            parent.bBusy = false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox ck = sender as CheckBox;
            Model mo = ck.DataContext as Model;
        }
    }
}
