using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
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
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using Cobra.EM;
using Cobra.Common;


namespace Cobra.UFPSBSPanel
{
    enum editortype
    {
        TextBox_EditType = 0,
        ComboBox_EditType = 1
    }

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MainControl
    {
        public volatile static bool brun;
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

        public int session_id = -1;
        public ulong session_row_number = 0;
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

        public GeneralMessage gm = new GeneralMessage("FSBS SFL", "", 0);
        private System.Timers.Timer uiTimer = new System.Timers.Timer();
        private BackgroundWorker m_BackgroundWorker;// 申明后台对象

        private Task IOTask = null;
        private Task UITask = null;

        private object IO_Lock = new object();
        private object UI_Lock = new object();

        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;

            pageBar.OnPageSwitch += new PageBarControl.PageSwitchEventHandler(Page_Switch);
            viewmode = new ViewMode(pParent, this);

            sbsctrl.Init(this);
            ldctrl.Init(this);
            InitBWork();

            parent.db_Manager.NewSession(sflname, ref session_id, DateTime.Now.ToString());
            #endregion
        }

        public void InitBWork()
        {
            IOTask = new Task(IO_Callback);
            UITask = new Task(UI_Callback);

            uiTimer.Elapsed += new ElapsedEventHandler(ui_DoWork);

            m_BackgroundWorker = new BackgroundWorker(); // 实例化后台对象
            m_BackgroundWorker.WorkerSupportsCancellation = true; // 设置可以取消
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(io_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(io_CompletedWork);
        }

        public void Page_Switch(int index)
        {
            switch (index)
            {
                case 0:
                    sbsctrl.Visibility = Visibility.Visible;
                    ldctrl.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    sbsctrl.Visibility = Visibility.Collapsed;
                    ldctrl.Visibility = Visibility.Visible;
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
        #endregion

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            int log_id = -1;
            UInt16 uTime = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (UInt16.TryParse(timerTb.Text, out uTime))
                uiTimer.Interval = uTime;
            else
                uiTimer.Interval = 2000;

            if (runBtn.IsChecked == true)
            {
                brun = true;
                ret = PreRun();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    Stop(ret);
                    return;
                }
                session_row_number = 0;
                parent.db_Manager.NewSession(sflname, ref session_id, DateTime.Now.ToString());
                Start();
            }
            else
                Stop(LibErrorCode.IDS_ERR_SUCCESSFUL);

        }

        void ui_DoWork(object sender, EventArgs e)
        {
            if (!UITask.IsCompleted) return;
            UITask = Task.Factory.StartNew(UI_Callback);
            Task.WaitAll(UITask);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private UInt32 PreRun()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Run SBS";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                ret = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
                return ret;
            }
            else
                parent.bBusy = true;

            ret = Read(viewmode.rd_one_dm_parameterlist);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            return ConvertHexToPhysical(viewmode.rd_one_dm_parameterlist);
        }

        public uint Read(ParamContainer pc)
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            msg.task = TM.TM_READ;
            msg.task_parameterlist = pc;
            uint ret = parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            return m_Msg.errorcode;
        }

        public uint Write(ParamContainer pc)
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            msg.task = TM.TM_WRITE;
            msg.task_parameterlist = pc;
            uint ret = parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            return m_Msg.errorcode;
        }

        public uint ConvertPhysicalToHex(ParamContainer pc)
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            msg.task_parameterlist = pc;
            uint ret = parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            return m_Msg.errorcode;
        }

        public uint ConvertHexToPhysical(ParamContainer pc)
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            msg.task = TM.TM_CONVERT_HEXTOPHYSICAL;
            msg.task_parameterlist = pc;
            uint ret = parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            return m_Msg.errorcode;
        }

        private void IO_Callback()
        {
            lock (IO_Lock)
            {
                uint errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (brun == false) return;
                if (msg.bgworker.IsBusy == true) //bus是否正忙
                {
                    errorcode = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
                    if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        Stop(errorcode);
                        return;
                    }
                }
                else
                {
                    errorcode = Read(viewmode.rd_dm_parameterlist);
                    if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        Stop(errorcode);
                        return;
                    }

                    errorcode = ConvertHexToPhysical(viewmode.rd_dm_parameterlist);
                    if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        Stop(errorcode);
                        return;
                    }

                    if (viewmode.wr_dm_parameterlist.parameterlist.Count != 0)
                    {
                        errorcode = ConvertPhysicalToHex(viewmode.wr_dm_parameterlist);
                        if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            viewmode.wr_dm_parameterlist.parameterlist.Clear();
                            Stop(errorcode);
                            return;
                        }

                        errorcode = Write(viewmode.wr_dm_parameterlist);
                        if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            viewmode.wr_dm_parameterlist.parameterlist.Clear();
                            Stop(errorcode);
                            return;
                        }
                        viewmode.wr_dm_parameterlist.parameterlist.Clear();
                    }
                }

            }
        }

        private void UI_Callback()
        {
            lock (UI_Lock)
            {
                if (brun == false) return;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ldctrl.update();
                    sbsctrl.update();
                }));
            }
        }

        private void Start()
        {
            ldctrl.BuildColumn();
            sbsctrl.Reset();

            gm.controls = "Run button";
            gm.message = "Read Device";
            gm.bupdate = false;
            runBtn.Content = "Stop";
            m_BackgroundWorker.RunWorkerAsync();
            while (!IOTask.IsCompleted) ;
            UITask = Task.Factory.StartNew(UI_Callback);
            uiTimer.Start();
        }

        private void Stop(UInt32 errorcode)
        {
            brun = false;
            Dispatcher.Invoke(new Action(() =>
            {
                runBtn.Content = "Run";
                runBtn.IsChecked = false;

                uiTimer.Stop();
                parent.bBusy = false;

                gm.controls = "Stop button";
                gm.message = LibErrorCode.GetErrorDescription(errorcode);
                gm.bupdate = true;
                CallWarningControl(gm);
            }));
        }

        #region 访问设备
        void io_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime dt1;
            BackgroundWorker bw = sender as BackgroundWorker;
            while (brun)
            {
                try
                {
                    dt1 = DateTime.Now;
                    IOTask = Task.Factory.StartNew(IO_Callback);
                    Task.WaitAll(IOTask);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    while ((DateTime.Now - dt1).TotalMilliseconds < 1000);
                }
                catch (System.Exception ex)
                {
                    FolderMap.WriteFile(ex.Message.ToString());
                    brun = false;
                }
            }
            e.Cancel = true;
        }

        void io_CompletedWork(object sender, RunWorkerCompletedEventArgs e)
        {
            parent.bControl = false;
            if (e.Error != null)
            {
                Stop(LibErrorCode.IDS_ERR_I2C_BUS_ERROR);
            }
            else if (e.Cancelled)
            {
                Stop(LibErrorCode.IDS_ERR_SUCCESSFUL);
            }
            else
            {
                Stop(LibErrorCode.IDS_ERR_SUCCESSFUL);
            }
        }
        #endregion
    }
}
