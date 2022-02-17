using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using Cobra.EM;
using Cobra.Common;
using System.Threading;

namespace Cobra.FSBS2Panel
{
    enum editortype
    {
        TextBox_RO_EditType = 0,
        ComboBox_EditType = 1,
        CheckBox_EditType = 2,
        TextBox_WR_EditType = 3,
    }
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

        public ObservableCollection<string> m_ScanIntervalCol = new ObservableCollection<string>();
        public ObservableCollection<string> scanIntervalCol
        {
            set { m_ScanIntervalCol = value; }
            get { return m_ScanIntervalCol; }
        }

        public Dictionary<string, ushort> m_ScanModeDic = new Dictionary<string, ushort>();
        public Dictionary<string, ushort> scanModeDic
        {
            set { m_ScanModeDic = value; }
            get { return m_ScanModeDic; }
        }
        public int session_id = -1;
        public ulong session_row_number = 0;
        public string subTaskJson = string.Empty;
        public bool border = false; //是否采用Order排序模式
        public GeneralMessage gm = new GeneralMessage("FSBS SFL", "", 0);
        public ControlMessage cm = new ControlMessage();
        private Stopwatch m_stopWatch = new Stopwatch();
        private Stopwatch m_detectWatch = new Stopwatch();
        private long intervalTime = 1000, detectTime = 5000;
        private UInt16 subTask = 0;
        private UInt16 configSubTask = 0;
        private BackgroundWorker m_ui_bgWorker;// 申明后台对象

        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            msg.gm.sflname = sflname;
            if (String.IsNullOrEmpty(sflname)) return;
            #endregion

            parent.PropertyChanged += new PropertyChangedEventHandler(parent_PropertyChanged);
            viewmode = new ViewMode(pParent, this);
            InitUI();
            InitBWork();
        }

        void parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "bDestroyed":
                    if (parent.bDestroyed)
                        ClosePopWindows();
                    break;
            }
            return;
        }

        public void InitUI()
        {
            #region 初始化interval和SubTask
            Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();
            subTask_Dic.Add("SFL", sflname);
            XmlNodeList nodelist = parent.GetUINodeList(sflname);
            foreach (XmlNode node in nodelist)
            {
                switch (node.Name)
                {
                    case "IntervalTime":
                        {
                            foreach (XmlNode sub in node)
                                scanIntervalCol.Add(sub.InnerText);
                            ScanIntervalCB.ItemsSource = scanIntervalCol;
                            ScanIntervalCB.SelectedIndex = 0;
                            break;
                        }
                    case "ScanMode":
                        {
                            foreach (XmlNode sub in node)
                                scanModeDic.Add(sub.Name, Convert.ToUInt16(sub.InnerText));
                            ScanModeCB.ItemsSource = scanModeDic.Keys;
                            ScanModeCB.SelectedIndex = 0;
                            break;
                        }
                    case "Section":
                        {
                            if (!node.HasChildNodes) break;
                            if ((node.Attributes["Name"] != null) && (node.Attributes["Name"].Value == "Configuration"))
                            {
                                if (node.Attributes["SubTask"] != null)
                                    configSubTask = Convert.ToUInt16(node.Attributes["SubTask"].Value, 16);
                            }
                            foreach (XmlNode subnode in node.ChildNodes)
                            {
                                setModel smodel = new setModel(subnode);
                                viewmode.setmodel_list.Add(smodel);
                                switch (smodel.editortype)
                                {
                                    case 0: //Text
                                        break;
                                    case 1: //Comboboxs
                                        smodel.sphydata = smodel.itemlist[smodel.listindex];
                                        break;
                                    case 2: //Checkbox
                                        smodel.sphydata = smodel.bcheck.ToString();
                                        break;
                                }
                                subTask_Dic.Add(smodel.nickname, smodel.sphydata);
                            }
                            break;
                        }
                }
            }
            if (scanModeDic.Count == 0)
                ScanModeCB.Visibility = Visibility.Collapsed;
            if (scanIntervalCol.Count == 0)
                ScanIntervalCB.Visibility = Visibility.Collapsed;
            if (viewmode.setmodel_list.Count == 0)
                configBtn.Visibility = Visibility.Collapsed;
            else
                subTaskJson = SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
            #endregion

            pageBar.OnPageSwitch += new PageBarControl.PageSwitchEventHandler(Page_Switch);
            sbsctrl.Init(this);
            ldctrl.Init(this);
        }

        public void ClosePopWindows()
        {
            foreach (Model mod in viewmode.sfl_parameterlist)
            {
                switch (mod.subType)
                {
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_VOL:
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_CUR:
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_TEMP:
                        if (!mod.waveControl.IsActive)
                            mod.waveControl.Close();
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT:
                        {
                            break;
                        }
                    default:
                        break;
                }

            }
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
        public void CallWaitControl(ControlMessage msg)
        {
            WaitPopControl.Dispatcher.Invoke(new Action(() =>
            {
                WaitPopControl.IsBusy = msg.bshow;
                WaitPopControl.Text = msg.message;
                WaitPopControl.Percent = String.Format("{0}", msg.percent);
            }));
        }
        #endregion

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            setModel setMod = null;
            string interval = null;

            if (scanModeDic.Count != 0)
                subTask = Convert.ToUInt16(scanModeDic[(ScanModeCB.SelectedItem).ToString()].ToString());
            else
                subTask = (UInt16)0xFFFF;

            if (ScanIntervalCB.Visibility == Visibility.Visible)
                interval = ScanIntervalCB.SelectedItem.ToString();
            else
            {
                setMod = viewmode.GetSetModelByName("IntervalTime");
                if (setMod == null) return;
                interval = setMod.itemlist[(UInt16)setMod.phydata];
            }
            if (interval.ToLower().EndsWith("ms"))
            {
                interval = interval.Remove(interval.Length - 2);
                intervalTime = Convert.ToUInt16(interval);
            }
            else if (interval.ToLower().EndsWith("s"))
            {
                interval = interval.Remove(interval.Length - 1);
                intervalTime = Convert.ToUInt16(interval) * 1000;
            }
            if (runBtn.IsChecked == true)
            {
                if (PreRun() != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    runBtn.Content = "Start";
                    runBtn.IsChecked = false;
                    return;
                }
                SetTextBoxEdit(false);
                ldctrl.BuildColumn();
                sbsctrl.Reset();
                m_stopWatch.Reset();
                runBtn.Content = "Stop";
                session_row_number = 0;
                parent.db_Manager.NewSession(sflname, ref session_id, DateTime.Now.ToString());
                m_ui_bgWorker.RunWorkerAsync();
                m_stopWatch.Start();
            }
            else
            {
                if (m_stopWatch.IsRunning) m_stopWatch.Stop();
                runBtn.Content = "Start";
                runBtn.IsChecked = false;
                SetTextBoxEdit(true);
                m_ui_bgWorker.CancelAsync();
                while (m_ui_bgWorker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
            }
        }

        public void InitBWork()
        {
            m_ui_bgWorker = new BackgroundWorker(); // 实例化后台对象
            m_ui_bgWorker.WorkerReportsProgress = true; // 设置可以通告进度
            m_ui_bgWorker.WorkerSupportsCancellation = true; // 设置可以取消
            m_ui_bgWorker.DoWork += new DoWorkEventHandler(ui_DoWork);
            m_ui_bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CompletedWork);
        }

        void ui_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            while (true)
            {
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if (m_stopWatch.ElapsedMilliseconds >= intervalTime)
                {
                    m_stopWatch.Restart();
                    if (LoopDetection() != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        e.Cancel = true;
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(delegate ()
                    {
                        ldctrl.update();
                    }));
                    sbsctrl.update();
                }
                else
                {
                    int spaceTime = (int)(intervalTime - m_stopWatch.ElapsedMilliseconds);
                    if (spaceTime < 2) break;
                    Thread.Sleep(spaceTime);
                }
            }
        }

        void CompletedWork(object sender, RunWorkerCompletedEventArgs e)
        {
            runBtn.Content = "Start";
            runBtn.IsChecked = false;
            if (m_stopWatch.IsRunning) m_stopWatch.Stop();

            m_ui_bgWorker.CancelAsync();
            while (m_ui_bgWorker.IsBusy)
                System.Windows.Forms.Application.DoEvents();

            ldctrl.UpdateDBRecordList();
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

            msg.task = TM.TM_SPEICAL_GETDEVICEINFOR;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                ret = msg.errorcode;
                return ret;
            }

            msg.task_parameterlist = viewmode.dm_parameterlist;
            msg.task = TM.TM_SPEICAL_GETSYSTEMINFOR;
            msg.sub_task = (UInt16)subTask;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                ret = msg.errorcode;
                return ret;
            }
            parent.bBusy = false;

            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private void UpdateRunParamList()
        {
            #region 通知DEM操作
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Run SBS";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;
            msg.task_parameterlist = viewmode.dm_parameterlist;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = configSubTask;
            msg.sub_task_json = subTaskJson;
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
            #endregion

            #region UI操作
            viewmode.sfl_log_parameterlist.Clear();
            viewmode.scan_parameterlist.parameterlist.Clear();
            foreach (Model mod in viewmode.sfl_parameterlist)
            {
                if (!mod.parent.bShow) continue;
                switch (mod.subType)
                {
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_DYNAMIC:
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        viewmode.sfl_log_parameterlist.Add(mod);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT:
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        viewmode.sfl_log_parameterlist.Add(mod);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_WR:
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        viewmode.sfl_log_parameterlist.Add(mod);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT_WR:
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        viewmode.sfl_log_parameterlist.Add(mod);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT_BIT:
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_VOL:
                        viewmode.sfl_log_parameterlist.Add(mod);
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_CUR:
                        viewmode.sfl_log_parameterlist.Add(mod);
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        break;
                    case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_TEMP:
                        viewmode.sfl_log_parameterlist.Add(mod);
                        viewmode.scan_parameterlist.parameterlist.Add(mod.parent);
                        break;
                }
            }
            #endregion
        }

        private UInt32 Read()
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

            msg.task = TM.TM_COMMAND;
            msg.sub_task = (UInt16)subTask;
            msg.sub_task_json = subTaskJson;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {/*
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;*/
                ret = msg.errorcode;
                return ret;
            }

            msg.task_parameterlist = viewmode.scan_parameterlist;
            msg.task = TM.TM_READ;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {/*
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;*/
                ret = msg.errorcode;
                return ret;
            }

            msg.task = TM.TM_CONVERT_HEXTOPHYSICAL;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {/*
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;*/
                ret = msg.errorcode;
                return ret;
            }

            if (viewmode.wr_dm_parameterlist.parameterlist.Count != 0)
            {
                msg.task_parameterlist = viewmode.wr_dm_parameterlist;
                msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {/*
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    CallWarningControl(gm);
                    parent.bBusy = false;*/
                    ret = msg.errorcode;
                    return ret;
                }

                msg.task = TM.TM_WRITE;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {/*
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    CallWarningControl(gm);
                    parent.bBusy = false;*/
                    ret = msg.errorcode;
                    return ret;
                }
                viewmode.wr_dm_parameterlist.parameterlist.Clear(); //只写一次
            }

            parent.bBusy = false;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 write()
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Write one parameter!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                ret = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
                return ret;
            }
            else
                parent.bBusy = true;

            msg.brw = false;
            msg.percent = 20;
            msg.task = TM.TM_SPEICAL_GETDEVICEINFOR;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                ret = msg.errorcode;
                return ret;
            }

            msg.percent = 60;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                CallWarningControl(gm);
                parent.bBusy = false;
                ret = msg.errorcode;
                return ret;
            }

            msg.percent = 80;
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
                ret = msg.errorcode;
                return ret;
            }
            parent.bBusy = false;
            return ret;
        }

        #region Work Mode Button
        private void workModeBtn_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configwindow = new ConfigWindow(this);
            configwindow.ShowDialog();

            if (!configwindow.IsActive&&(configwindow.subTask_Dic.Count != 0))
                UpdateRunParamList();
        }

        private UInt32 LoopDetection()
        {
            bool bOne = false;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            
            do
            {
                ret = Read();
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    bOne = false;
                    if (!cm.bshow) break;
                    cm.bshow = false;
                    cm.message = string.Empty;
                    cm.percent = 0;
                    CallWaitControl(cm);
                    break;
                }
                parent.bBusy = false;
                cm.bshow = true;
                cm.message = LibErrorCode.GetErrorDescription(ret) + "\nReconnect continuelly until clicked the stop button!";
                cm.percent++;
                CallWaitControl(cm);
                m_detectWatch.Restart();
                do
                {
                    if (this.Dispatcher.Invoke(new Func<bool>(() =>
                       {
                           if(!bOne) FolderMap.WriteFile("Monitor error:" + LibErrorCode.GetErrorDescription(ret));
                           if (runBtn.IsChecked == false) return true;
                           else return false;
                       })))
                    {
                        cm.bshow = false;
                        cm.message = string.Empty;
                        cm.percent = 0;
                        CallWaitControl(cm);
                        m_detectWatch.Stop();
                        return ret;
                    }
                    bOne = true;
                } while (m_detectWatch.ElapsedMilliseconds < detectTime);
                m_detectWatch.Stop();
            } while (true);
            return ret;
        }

        private void SetTextBoxEdit(bool bEnable)
        {
            foreach(Model md in viewmode.sfl_wr_parameterlist)
            {
                md.bEnable = bEnable;
                md.bWrite = !bEnable;
            }
        }
        #endregion
    }
}
