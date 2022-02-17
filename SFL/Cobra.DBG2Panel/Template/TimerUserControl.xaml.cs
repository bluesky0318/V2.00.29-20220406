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
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using Cobra.Common;

namespace Cobra.DBG2Panel
{
    /// <summary>
    /// Interaction logic for TimerUserControl.xaml
    /// </summary>
    public partial class TimerUserControl : UserControl
    {
        private MainControl m_control_parent;
        public MainControl control_parent
        {
            get { return m_control_parent; }
            set { m_control_parent = value; }
        }

        private UInt32 m_Size;
        public UInt32 size
        {
            get { return m_Size; }
            set { m_Size = value; }
        }

        private UInt32 m_Start_Addr = 0;
        public UInt32 start_addr
        {
            get { return m_Start_Addr; }
            set { m_Start_Addr = value; }
        }

        public Dictionary<string, UInt32> m_IntervalDic = new Dictionary<string, UInt32>();
        public Dictionary<string, UInt32> intervalDic
        {
            set { m_IntervalDic = value; }
            get { return m_IntervalDic; }
        }

        private AsyncObservableCollection<Model> m_SFL_Memory_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_memory_parameterlist
        {
            get { return m_SFL_Memory_ParameterList; }
            set { m_SFL_Memory_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Work_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_work_parameterlist
        {
            get { return m_SFL_Work_ParameterList; }
            set { m_SFL_Work_ParameterList = value; }
        }

        private long intervalTime = 1000;
        private byte[] flashData = null;
        private string csvPath = null;
        private Stopwatch m_stopWatch = new Stopwatch();
        private ControlMessage cmg = new ControlMessage();
        private BackgroundWorker m_bgWorker_Timer;// 申明后台对象
        private BackgroundWorker m_bgWorker_Read;// 申明后台对象
        private BackgroundWorker m_bgWorker_Write;// 申明后台对象

        public TimerUserControl()
        {
            InitializeComponent();
        }

        #region Init
        public void Init(object pParent, XmlNode node)
        {
            Model mod = null;
            string name = String.Empty;
            m_control_parent = (MainControl)pParent;
            foreach (XmlNode sub in node)
            {
                name = sub.Name.ToString();
                switch (name)
                {
                    case "IntervalTime":
                        {
                            foreach (XmlNode snode in sub)
                                intervalDic.Add(snode.InnerText, Convert.ToUInt32(snode.Attributes["Value"].Value));
                            rdItCmb.ItemsSource = intervalDic.Keys;
                            rdItCmb.SelectedIndex = 0;
                            break;
                        }
                    case "Size":
                        {
                            size = UInt32.Parse(sub.InnerText);
                            break;
                        }
                    case "StartAddr":
                        {
                            start_addr = Convert.ToUInt32(sub.InnerText, 16);
                            break;
                        }
                }
            }
            for (UInt32 i = 0; i < (UInt32)(size / 16); i++)
            {
                mod = new Model();
                mod.address = (start_addr + (i << 4));
                mod.nickname = string.Concat("0x", string.Format("{0:x4}", (start_addr + (i << 4))));
                sfl_memory_parameterlist.Add(mod);
            }
            MemoryDG.ItemsSource = sfl_memory_parameterlist;
            flashData = new byte[size];
            InitBWork();
        }

        public void InitBWork()
        {
            m_bgWorker_Timer = new BackgroundWorker(); // 实例化后台对象
            m_bgWorker_Timer.WorkerReportsProgress = true; // 设置可以通告进度
            m_bgWorker_Timer.WorkerSupportsCancellation = true; // 设置可以取消
            m_bgWorker_Timer.DoWork += new DoWorkEventHandler(DoWork_Timer);
            m_bgWorker_Timer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CompletedWork_Timer);

            m_bgWorker_Read = new BackgroundWorker(); // 实例化后台对象
            m_bgWorker_Read.WorkerReportsProgress = true; // 设置可以通告进度
            m_bgWorker_Read.WorkerSupportsCancellation = true; // 设置可以取消
            m_bgWorker_Read.DoWork += new DoWorkEventHandler(DoWork_Read);
            m_bgWorker_Read.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CompletedWork_Read);

            m_bgWorker_Write = new BackgroundWorker(); // 实例化后台对象
            m_bgWorker_Write.WorkerReportsProgress = true; // 设置可以通告进度
            m_bgWorker_Write.WorkerSupportsCancellation = true; // 设置可以取消
            m_bgWorker_Write.DoWork += new DoWorkEventHandler(DoWork_Write);
            m_bgWorker_Write.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CompletedWork_Write);
        }

        private void InitDataGrid()
        {
            Model mo = null;
            for (int i = 0; i < sfl_memory_parameterlist.Count; i++)
            {
                mo = sfl_memory_parameterlist[i];
                if (mo == null) continue;
                mo.byte0.data = 0;
                mo.byte1.data = 0;
                mo.byte2.data = 0;
                mo.byte3.data = 0;
                mo.byte4.data = 0;
                mo.byte5.data = 0;
                mo.byte6.data = 0;
                mo.byte7.data = 0;
                mo.byte8.data = 0;
                mo.byte9.data = 0;
                mo.byte10.data = 0; ;
                mo.byte11.data = 0;
                mo.byte12.data = 0;
                mo.byte13.data = 0;
                mo.byte14.data = 0;
                mo.byte15.data = 0;
            }
        }

        private void UpdateDataGrid()
        {
            foreach (Model mo in sfl_work_parameterlist)
            {
                if (mo == null) continue;
                mo.byte0.data = flashData[mo.address - start_addr];
                mo.byte1.data = flashData[1 + mo.address - start_addr];
                mo.byte2.data = flashData[2 + mo.address - start_addr];
                mo.byte3.data = flashData[3 + mo.address - start_addr];
                mo.byte4.data = flashData[4 + mo.address - start_addr];
                mo.byte5.data = flashData[5 + mo.address - start_addr];
                mo.byte6.data = flashData[6 + mo.address - start_addr];
                mo.byte7.data = flashData[7 + mo.address - start_addr];
                mo.byte8.data = flashData[8 + mo.address - start_addr];
                mo.byte9.data = flashData[9 + mo.address - start_addr];
                mo.byte10.data = flashData[10 + mo.address - start_addr];
                mo.byte11.data = flashData[11 + mo.address - start_addr];
                mo.byte12.data = flashData[12 + mo.address - start_addr];
                mo.byte13.data = flashData[13 + mo.address - start_addr];
                mo.byte14.data = flashData[14 + mo.address - start_addr];
                mo.byte15.data = flashData[15 + mo.address - start_addr];
            }
        } 
        #endregion

        private void rdItem_Click(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var mod = ((DataGridRow)contextMenu.PlacementTarget).Item as Model;
            if (mod == null) return;

            sfl_work_parameterlist.Clear();
            sfl_work_parameterlist.Add(mod);
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            m_control_parent.CallWaitControl(cmg);
            BuildJson(mod.address,16);
            m_bgWorker_Read.RunWorkerAsync();
        }

        private void wrItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var mod = ((DataGridRow)contextMenu.PlacementTarget).Item as Model;
            if (mod == null) return;

            sfl_work_parameterlist.Clear();
            sfl_work_parameterlist.Add(mod);
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            m_control_parent.CallWaitControl(cmg);
            BuildJson(mod.address, 16);
            m_bgWorker_Write.RunWorkerAsync();
        }

        private void gotoBtn_Click(object sender, RoutedEventArgs e)
        {
            UInt16 uRow = 0;
            uRow = UInt16.Parse(txtGoto.Text, System.Globalization.NumberStyles.HexNumber);
            if (uRow > MemoryDG.Items.Count) uRow = (UInt16)(MemoryDG.Items.Count -1);
            MemoryDG.ScrollIntoView(MemoryDG.Items[uRow]);
        }

        private void readAllBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            m_control_parent.CallWaitControl(cmg);
            BuildJson(start_addr,size);
            m_SFL_Work_ParameterList.Clear();
            for (int i = 0; i < sfl_memory_parameterlist.Count; i++)
            {
                mo = sfl_memory_parameterlist[i];
                if (mo == null) continue;
                sfl_work_parameterlist.Add(mo);
            }
            m_bgWorker_Read.RunWorkerAsync();
        }

        private void writeAllBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            m_control_parent.CallWaitControl(cmg);
            BuildJson(start_addr, size);
            m_SFL_Work_ParameterList.Clear();
            for (int i = 0; i < sfl_memory_parameterlist.Count; i++)
            {
                mo = sfl_memory_parameterlist[i];
                if (mo == null) continue;
                sfl_work_parameterlist.Add(mo);
            }
            m_bgWorker_Write.RunWorkerAsync();
        }

        #region TimerBackground
        private void rdCkb_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            if (rdCkb.IsChecked == true)
            {
                intervalTime = intervalDic[rdItCmb.SelectedItem.ToString()];
                BuildJson(start_addr, size); 
                m_SFL_Work_ParameterList.Clear();
                for (int i = 0; i < sfl_memory_parameterlist.Count; i++)
                {
                    mo = sfl_memory_parameterlist[i];
                    if (mo == null) continue;
                    sfl_work_parameterlist.Add(mo);
                }
                if (PreRun() != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    rdCkb.IsChecked = false;
                    return;
                }
                csvPath = FolderMap.m_logs_folder + "DBG" + DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + ".csv";
                m_stopWatch.Reset();
                m_bgWorker_Timer.RunWorkerAsync();
                m_stopWatch.Start();
            }
            else
            {
                if (m_stopWatch.IsRunning) m_stopWatch.Stop();
                m_bgWorker_Timer.CancelAsync();
                while (m_bgWorker_Timer.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
            }
        }

        void DoWork_Timer(object sender, DoWorkEventArgs e)
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
                    if (Read() != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        e.Cancel = true;
                        return;
                    }
                    Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(delegate()
                    {
                        UpdateDataGrid();
                        OpCsv();
                    }));
                }
            }
        }

        void CompletedWork_Timer(object sender, RunWorkerCompletedEventArgs e)
        {
            if (m_stopWatch.IsRunning) m_stopWatch.Stop();

            m_bgWorker_Timer.CancelAsync();
            while (m_bgWorker_Timer.IsBusy)
                System.Windows.Forms.Application.DoEvents();
        }

        private void BuildJson(UInt32 addr, UInt32 len)
        {
            //Memory起始地址 start_addr
            //参数所在地址   address
            //读取长度       len
            control_parent.subTask_Dic.Clear();
            control_parent.subTask_Dic.Add("Size", len.ToString());
            control_parent.subTask_Dic.Add("Address", addr.ToString());
            control_parent.subTask_Dic.Add("StartAddr", start_addr.ToString());
            control_parent.subTaskJson = SharedAPI.SerializeDictionaryToJsonString(control_parent.subTask_Dic);
        }

        private UInt32 Read()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (m_control_parent.parent.bBusy)
            {
                m_control_parent.gm.level = 1;
                m_control_parent.gm.controls = "Read";
                m_control_parent.msg.sub_task_json = m_control_parent.subTaskJson;
                m_control_parent.gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_control_parent.gm.bupdate = true;
                m_control_parent.CallWarningControl(m_control_parent.gm);
                ret = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
                return ret;
            }
            else
                m_control_parent.parent.bBusy = true;

            m_control_parent.msg.task = TM.TM_COMMAND;
            m_control_parent.msg.sub_task = 0x25;
            m_control_parent.msg.flashData = flashData;//new byte[size];
            m_control_parent.msg.sub_task_json = m_control_parent.subTaskJson;
            var vmsg = m_control_parent.msg;
            m_control_parent.parent.AccessDevice(ref vmsg);
            while (m_control_parent.msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (m_control_parent.msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                cmg.bshow = false;
                m_control_parent.CallWaitControl(cmg);
                m_control_parent.gm.level = 2;
                m_control_parent.gm.message = LibErrorCode.GetErrorDescription(m_control_parent.msg.errorcode);
                m_control_parent.CallWarningControl(m_control_parent.gm);
                m_control_parent.parent.bBusy = false;
                ret = m_control_parent.msg.errorcode;
                return ret;
            }
            m_control_parent.parent.bBusy = false;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 PreRun()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (m_control_parent.parent.bBusy)
            {
                m_control_parent.gm.level = 1;
                m_control_parent.gm.controls = "Read";
                m_control_parent.gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_control_parent.gm.bupdate = true;
                m_control_parent.CallWarningControl(m_control_parent.gm);
                ret = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
                return ret;
            }
            else
                m_control_parent.parent.bBusy = true;

            m_control_parent.msg.task = TM.TM_SPEICAL_GETDEVICEINFOR;
            var vmsg = m_control_parent.msg;
            m_control_parent.parent.AccessDevice(ref vmsg);
            while (m_control_parent.msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (m_control_parent.msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                m_control_parent.gm.level = 2;
                m_control_parent.gm.message = LibErrorCode.GetErrorDescription(m_control_parent.msg.errorcode);
                m_control_parent.CallWarningControl(m_control_parent.gm);
                m_control_parent.parent.bBusy = false;
                ret = m_control_parent.msg.errorcode;
                return ret;
            }
            m_control_parent.parent.bBusy = false;

            InitDataGrid();
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        /// <summary>
        /// 写入数据到CSV文件，覆盖形式
        /// </summary>
        /// <param name="csvPath">要写入的字符串表示的CSV文件</param>
        /// <param name="LineDataList">要写入CSV文件的数据，以string[]类型List表示的行集数据</param>
        public void OpCsv()
        {
            try
            {
                using (FileStream fs = new FileStream(csvPath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    StringBuilder sb_csvStr = new StringBuilder();
                    for (int i = 0; i < (flashData.Length / 16); i++)//<--row
                    {
                        sb_csvStr.Clear();
                        for (int j = 0; j < flashData.Length; j++)//<--col
                        {
                            sb_csvStr.Append(string.Format("0x{0:x2},", flashData[i * 16 + j]));
                        }
                        sb_csvStr.Append(string.Format("{0}:{1}", DateTime.Now.ToString(), DateTime.Now.Millisecond.ToString()));
                        sw.WriteLine(sb_csvStr.ToString().Substring(0, sb_csvStr.ToString().Length - 1));
                    }
                }
            }
            catch (System.Exception ex)
            {
            	
            }
        }
        #endregion

        #region ReadBackground
        void DoWork_Read(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            if (Read() != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                e.Cancel = true;
                return;
            }
        }

        void CompletedWork_Read(object sender, RunWorkerCompletedEventArgs e)
        {
            m_bgWorker_Timer.CancelAsync();
            while (m_bgWorker_Timer.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            UpdateDataGrid();
            cmg.bshow = false;
            m_control_parent.CallWaitControl(cmg);
        }
        #endregion

        #region WriteBackground
        void DoWork_Write(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            if (Write() != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                e.Cancel = true;
                return;
            }
        }

        void CompletedWork_Write(object sender, RunWorkerCompletedEventArgs e)
        {
            m_bgWorker_Timer.CancelAsync();
            while (m_bgWorker_Timer.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            UpdateDataGrid();
            cmg.bshow = false;
            m_control_parent.CallWaitControl(cmg);
        }

        private UInt32 Write()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (m_control_parent.parent.bBusy)
            {
                m_control_parent.gm.level = 1;
                m_control_parent.gm.controls = "Write";
                m_control_parent.msg.sub_task_json = m_control_parent.subTaskJson;
                m_control_parent.gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_control_parent.gm.bupdate = true;
                m_control_parent.CallWarningControl(m_control_parent.gm);
                ret = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
                return ret;
            }
            else
                m_control_parent.parent.bBusy = true;

            m_control_parent.msg.task = TM.TM_COMMAND;
            m_control_parent.msg.sub_task = 0x26;
            m_control_parent.msg.flashData = flashData;//new byte[size];
            m_control_parent.msg.sub_task_json = m_control_parent.subTaskJson;
            var vmsg = m_control_parent.msg;
            m_control_parent.parent.AccessDevice(ref vmsg);
            while (m_control_parent.msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (m_control_parent.msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                cmg.bshow = false;
                m_control_parent.CallWaitControl(cmg);
                m_control_parent.gm.level = 2;
                m_control_parent.gm.message = LibErrorCode.GetErrorDescription(m_control_parent.msg.errorcode);
                m_control_parent.CallWarningControl(m_control_parent.gm);
                m_control_parent.parent.bBusy = false;
                ret = m_control_parent.msg.errorcode;
                return ret;
            }
            m_control_parent.parent.bBusy = false;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion
    }
}
