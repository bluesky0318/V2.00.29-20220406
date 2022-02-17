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
using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using Cobra.Common;

namespace Cobra.DBG2Panel
{
    /// <summary>
    /// Interaction logic for MemoryUserControl.xaml
    /// </summary>
    public partial class MemoryUserControl : UserControl
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

        private byte[] flashData = null;
        private Stopwatch m_stopWatch = new Stopwatch();
        private ControlMessage cmg = new ControlMessage();
        private BackgroundWorker m_bgWorker_Read;// 申明后台对象
        private BackgroundWorker m_bgWorker_Write;// 申明后台对象

        public MemoryUserControl()
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

        private void UpdateParamListValue()
        {
            foreach (Model mo in sfl_work_parameterlist)
            {
                if (mo == null) continue;
                flashData[mo.address - start_addr] = mo.byte0.data;
                flashData[1 + mo.address - start_addr] = mo.byte1.data;
                flashData[2 + mo.address - start_addr] = mo.byte2.data;
                flashData[3 + mo.address - start_addr] = mo.byte3.data;
                flashData[4 + mo.address - start_addr] = mo.byte4.data;
                flashData[5 + mo.address - start_addr] = mo.byte5.data;
                flashData[6 + mo.address - start_addr] = mo.byte6.data;
                flashData[7 + mo.address - start_addr] = mo.byte7.data;
                flashData[8 + mo.address - start_addr] = mo.byte8.data;
                flashData[9 + mo.address - start_addr] = mo.byte9.data;
                flashData[10 + mo.address - start_addr] = mo.byte10.data;
                flashData[11 + mo.address - start_addr] = mo.byte11.data;
                flashData[12 + mo.address - start_addr] = mo.byte12.data;
                flashData[13 + mo.address - start_addr] = mo.byte13.data;
                flashData[14 + mo.address - start_addr] = mo.byte14.data;
                flashData[15 + mo.address - start_addr] = mo.byte15.data;
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
            BuildJson(mod.address, 16);
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
            if (uRow > MemoryDG.Items.Count) uRow = (UInt16)(MemoryDG.Items.Count - 1);
            MemoryDG.ScrollIntoView(MemoryDG.Items[uRow]);
        }

        private void readAllBtn_Click(object sender, RoutedEventArgs e)
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
            UpdateParamListValue();
            m_bgWorker_Write.RunWorkerAsync();
        }

        private void exportBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Title = "Save csv File";
            saveFileDialog.Filter = "csv file(*.csv)|*.csv|bin file(*.bin)|*.bin||";
            saveFileDialog.FileName = "hexData";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//FolderMap.m_extension_work_folder;
            if (saveFileDialog.ShowDialog() != true) return;

            int index = saveFileDialog.FileName.LastIndexOf('.');
            string suffix = saveFileDialog.FileName.Substring(index + 1);
            switch (suffix.ToLower())
            {
                case "csv":
                    SaveCsvFile(saveFileDialog.FileName);
                    break;
                case "bin":
                    SaveBinFile(saveFileDialog.FileName);
                    break;
            }
        }

        private void SaveCsvFile(string fullpath)
        {
            StringBuilder str = new StringBuilder();
            using (FileStream file = new FileStream(fullpath, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(file))
                {
                    foreach (Model mo in sfl_work_parameterlist)
                    {
                        if (mo == null) continue;
                        str.Clear();
                        str.AppendFormat("0x{0:x4}", mo.address);
                        str.Append(",");
                        sw.Write(mo.byte0.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte1.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte2.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte3.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte4.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte5.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte6.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte7.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte8.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte9.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte10.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte11.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte12.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte13.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte14.data);
                        str.Append(",");
                        str.AppendFormat("{0:x2}", mo.byte15.data);
                        sw.WriteLine(str.ToString());
                    }
                }
            }
        }
        private void SaveBinFile(string fullpath)
        {
            using (FileStream file = new FileStream(fullpath, FileMode.Create))
            {
                using (BinaryWriter sw = new BinaryWriter(file))
                {
                    foreach (Model mo in sfl_work_parameterlist)
                    {
                        if (mo == null) continue;
                        sw.Write(mo.byte0.data);
                        sw.Write(mo.byte1.data);
                        sw.Write(mo.byte2.data);
                        sw.Write(mo.byte3.data);
                        sw.Write(mo.byte4.data);
                        sw.Write(mo.byte5.data);
                        sw.Write(mo.byte6.data);
                        sw.Write(mo.byte7.data);
                        sw.Write(mo.byte8.data);
                        sw.Write(mo.byte9.data);
                        sw.Write(mo.byte10.data);
                        sw.Write(mo.byte11.data);
                        sw.Write(mo.byte12.data);
                        sw.Write(mo.byte13.data);
                        sw.Write(mo.byte14.data);
                        sw.Write(mo.byte15.data);
                    }
                }
            }
        }

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
            m_bgWorker_Read.CancelAsync();
            while (m_bgWorker_Read.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            UpdateDataGrid();
            cmg.bshow = false;
            m_control_parent.CallWaitControl(cmg);
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
            m_bgWorker_Write.CancelAsync();
            while (m_bgWorker_Write.IsBusy)
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
    }
}
#endregion