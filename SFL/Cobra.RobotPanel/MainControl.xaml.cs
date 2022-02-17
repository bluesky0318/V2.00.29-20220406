using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using Cobra.EM;
using Cobra.Common;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Cobra.RobotPanel
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

        public int m_moveToIndex = -1;
        private const byte m_rwTask = 0x40;
        private byte m_readTask = 0x50;
        private byte m_writeTask = 0x51;
        private byte m_formulaTask = 0x52;
        private byte m_countTask = 0x53;

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

        private delegate Point GetPosition(IInputElement element);
        private int rowIndex = -1;
        private BackgroundWorker m_bgWorker;// 申明后台对象
        public GeneralMessage gm = new GeneralMessage("Robot SFL", "", 0);
        public ControlMessage cm = new ControlMessage();
        private ObservableCollection<Infor> m_Debug_Infors = new ObservableCollection<Infor>();
        internal Dictionary<string, Tuple<int, string, string>> m_formula_dic = new Dictionary<string, Tuple<int, string, string>>();

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

            viewmode = new ViewMode(pParent, this);
            mDataGrid.ItemsSource = viewmode.robot_commands;
            DebugListBox.ItemsSource = m_Debug_Infors;
            m_Debug_Infors.Clear();
            m_Debug_Infors.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Infors_CollectionChanged);
            m_Debug_Infors.Add(new Infor("Welcome to Robot SFL...."));
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            msg.gm.sflname = sflname;
            InitBWork();
            InitalUI();
            InitalDG();
        }

        #region DataGrid
        public void InitalDG()
        {
            mDataGrid.PreviewMouseRightButtonDown += MDataGrid_PreviewMouseRightButtonDown; ;
        }

        private void MDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DataGrid grid = sender as DataGrid;
                if (grid == null) return;
                if (grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    if (dgr == null) return;
                    if (!dgr.IsMouseOver)
                    {
                        (dgr as DataGridRow).IsSelected = false;
                    }
                }
            }
            catch
            {

            }
        }
        #endregion

        #region background work
        public void InitBWork()
        {
            m_bgWorker = new BackgroundWorker(); // 实例化后台对象
            m_bgWorker.WorkerReportsProgress = true; // 设置可以通告进度
            m_bgWorker.WorkerSupportsCancellation = true; // 设置可以取消
            m_bgWorker.DoWork += new DoWorkEventHandler(DoWork);
            m_bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CompletedWork);
        }

        public void InitalUI()
        {
            byte bval = 0;
            string str = String.Empty;
            XmlNodeList nodelist = parent.GetUINodeList(sflname);
            if (nodelist == null) return;

            foreach (XmlNode node in nodelist)
            {
                if (node.Attributes["ReadTask"] == null) continue;
                str = node.Attributes["ReadTask"].Value.ToString();
                if (byte.TryParse(str, out bval)) m_readTask = bval;

                if (node.Attributes["WriteTask"] == null) continue;
                str = node.Attributes["WriteTask"].Value.ToString();
                if (byte.TryParse(str, out bval)) m_writeTask = bval;

                if (node.Attributes["FormulaTask"] == null) continue;
                str = node.Attributes["FormulaTask"].Value.ToString();
                if (byte.TryParse(str, out bval)) m_formulaTask = bval;

                if (node.Attributes["CountTask"] == null) continue;
                str = node.Attributes["CountTask"].Value.ToString();
                if (byte.TryParse(str, out bval)) m_countTask = bval;
            }
        }
        #endregion

        void DoWork(object sender, DoWorkEventArgs e)
        {
            Model mo = null;
            ushort cur_Task = 0;
            string access_mode = string.Empty;
            BackgroundWorker bw = sender as BackgroundWorker;

            for (int i = 0; i < viewmode.execute_Commands.Count; i++)
            {
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                mo = viewmode.execute_Commands[i];
                if (mo == null) continue;
                viewmode.dm_parameterlist.parameterlist.Clear();
                viewmode.dm_parameterlist.parameterlist.Add(mo.pParent);
                switch (mo.type)
                {
                    case 0:
                        cur_Task = m_readTask;
                        access_mode = "Read";
                        break;
                    case 1:
                        cur_Task = m_writeTask;
                        access_mode = "Write";
                        break;
                    case 2:
                        cur_Task = m_rwTask;
                        access_mode = "RW";
                        break;
                }
                switch (cur_Task)
                {
                    case m_rwTask:
                        mo.PropertyChanged -= mo.Model_PropertyChanged;
                        AccessDevice(mo, "Read", m_readTask);
                        mo.PropertyChanged += mo.Model_PropertyChanged;
                        Thread.Sleep(1);
                        ConvertTargetToOriginal(ref mo);
                        AccessDevice(mo, "Write", m_writeTask);
                        break;
                    default:
                        AccessDevice(mo, access_mode, cur_Task);
                        break;
                }
            }
        }

        void CompletedWork(object sender, RunWorkerCompletedEventArgs e)
        {
            runBtn.Content = "Start";
            runBtn.IsChecked = false;
            m_bgWorker.CancelAsync();
            while (m_bgWorker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
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

        #region listbox
        private void ClearRecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            m_Debug_Infors.Clear();
        }

        private void Infors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DebugListBox.ItemsSource = m_Debug_Infors;
            DebugListBox.UpdateLayout();
            if (DebugListBox.Items.Count != 0)
                DebugListBox.ScrollIntoView(this.DebugListBox.Items[this.DebugListBox.Items.Count - 1]);
        }
        private void msg_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TASKMessage msg = sender as TASKMessage;
            switch (e.PropertyName)
            {
                case "controlreq":
                    switch (msg.controlreq)
                    {
                        case COMMON_CONTROL.COMMON_CONTROL_DEFAULT:
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    m_Debug_Infors.Add(new Infor(msg.controlmsg.message));
                                }));
                                break;
                            }
                    }
                    break;
            }
        }
        #endregion

        #region button click
        private void SingleBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            ushort cur_Task = 0;
            string access_mode = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            viewmode.dm_parameterlist.parameterlist.Clear();
            ret = preRun();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 1;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                gm.bupdate = true;
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            viewmode.execute_Commands.Sort((x, y) =>
            {
                return x.id.CompareTo(y.id);
            });
            mo = viewmode.execute_Commands.Find(x => x.bResult == -1);
            if (mo == null)
            {
                m_Debug_Infors.Add(new Infor("No command to execute!"));
                parent.bBusy = false;
                return;
            }
            viewmode.dm_parameterlist.parameterlist.Add(mo.pParent);
            switch (mo.type)
            {
                case 0:
                    cur_Task = m_readTask;
                    access_mode = "Read";
                    break;
                case 1:
                    cur_Task = m_writeTask;
                    access_mode = "Write";
                    break;
                case 2:
                    cur_Task = m_rwTask;
                    access_mode = "RW";
                    break;
            }
            switch (cur_Task)
            {
                case m_rwTask:
                    mo.PropertyChanged -= mo.Model_PropertyChanged;
                    AccessDevice(mo, "Read", m_readTask);
                    mo.PropertyChanged += mo.Model_PropertyChanged;
                    Thread.Sleep(1);
                    ConvertTargetToOriginal(ref mo);
                    AccessDevice(mo, "Write", m_writeTask);
                    break;
                default:
                    AccessDevice(mo, access_mode, cur_Task);
                    break;
            }
        }

        private void AccessDevice(Model mo, string access_mode, UInt16 task)
        {
            msg.task_parameterlist = viewmode.dm_parameterlist;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = task;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                Dispatcher.Invoke(new Action(() =>
                {
                    m_Debug_Infors.Add(new Infor(string.Format("Fail to {0} register 0x{1:x8}.", access_mode, mo.address)));
                }));
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            Dispatcher.Invoke(new Action(() =>
            {
                m_Debug_Infors.Add(new Infor(string.Format("Success to {0} register 0x{1:x8},Value 0x{2:x8}.", access_mode, mo.address, mo.pParent.u32hexdata)));
            }));
        }

        private void SequentialBtn_Click(object sender, RoutedEventArgs e)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (runBtn.IsChecked == true)
            {
                ret = preRun();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    m_Debug_Infors.Add(new Infor(LibErrorCode.GetErrorDescription(ret)));
                    runBtn.Content = "Start";
                    runBtn.IsChecked = false;
                    parent.bBusy = false;
                    return;
                }
                viewmode.execute_Commands.Sort((x, y) =>
                {
                    return x.id.CompareTo(y.id);
                });
                if (viewmode.execute_Commands.Count == 0)
                {
                    m_Debug_Infors.Add(new Infor("No command to execute!"));
                    runBtn.Content = "Start";
                    runBtn.IsChecked = false;
                    parent.bBusy = false;
                    return;
                }
                runBtn.Content = "Stop";
                m_bgWorker.RunWorkerAsync();
            }
            else
            {
                runBtn.Content = "Start";
                runBtn.IsChecked = false;
                m_bgWorker.CancelAsync();
                while (m_bgWorker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
            }
            parent.bBusy = false;
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.Title = "Load File";
            openFileDialog.Filter = "Command file (*.txt)|*.txt||";
            openFileDialog.DefaultExt = "txt";
            openFileDialog.FileName = "command";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (openFileDialog.ShowDialog() == true)
            {
                fullpath = openFileDialog.FileName;
                ret = LoadFile(fullpath);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.message = "Load failed! Please check the file format.";
                    CallWarningControl(gm);
                }
            }
            else
                return;

        }
        private UInt32 LoadFile(string path)
        {
            string strLin = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (System.IO.File.Exists(path) == false)
                return LibErrorCode.IDS_ERR_SBSSFL_LOAD_FILE;

            try
            {
                viewmode.robot_commands.Clear();
                viewmode.execute_Commands.Clear();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (StreamReader sw = new StreamReader(fs))
                {
                    strLin = sw.ReadToEnd();
                    viewmode.robot_commands = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Model>>(strLin);
                }
                if (viewmode.robot_commands == null)
                    return LibErrorCode.IDS_ERR_SBSSFL_LOAD_FILE;
                foreach (Model mo in viewmode.robot_commands)
                {
                    foreach (bitModel bmo in mo.bitModel_List)
                    {
                        bmo.model_Parent = mo;
                        bmo.model_Parent.PropertyChanged += bmo.MoInBit_PropertyChanged;
                    }
                    foreach (formulaModel fmo in mo.formulaModel_List)
                        fmo.model_Parent = mo;
                    if (mo.bSelect)
                        viewmode.execute_Commands.Add(mo);
                }
                mDataGrid.ItemsSource = viewmode.robot_commands;
            }
            catch (System.Exception ex)
            {

            }
            return ret;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            saveFileDialog.FileName = "command";
            saveFileDialog.Title = "Save File";       //Issue1513 Leon
            saveFileDialog.Filter = "Command file (*.txt)|*.txt||";
            saveFileDialog.DefaultExt = "txt";

            saveFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (saveFileDialog.ShowDialog() == true)
            {
                if (parent == null) return;
                else
                {
                    fullpath = saveFileDialog.FileName;
                    SaveFile(fullpath);
                }
            }
            else return;
        }

        private void SaveFile(string path)
        {
            string str = string.Empty;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    str = JsonConvert.SerializeObject(viewmode.robot_commands, Newtonsoft.Json.Formatting.Indented);
                    sw.WriteLine(str);
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        private void commmandCB_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as CheckBox;
            Model mo = item.DataContext as Model;
            if (mo == null) return;
            if (mo.bSelect)
                viewmode.execute_Commands.Add(mo);
            else
                viewmode.execute_Commands.Remove(mo);
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            for (int i = 0; i < viewmode.robot_commands.Count; i++)
            {
                mo = viewmode.robot_commands[i];
                if (mo == null) continue;
                mo.bResult = -1;
            }
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            viewmode.execute_Commands.Clear();
            for (int i = 0; i < viewmode.robot_commands.Count; i++)
            {
                mo = viewmode.robot_commands[i];
                if (mo == null) continue;
                mo.bSelect = true;
                viewmode.execute_Commands.Add(mo);
            }
        }

        private void SelectNoneBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = null;
            for (int i = 0; i < viewmode.robot_commands.Count; i++)
            {
                mo = viewmode.robot_commands[i];
                if (mo == null) continue;
                mo.bSelect = false;
            }
            viewmode.execute_Commands.Clear();
        }

        private void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            bitModel bmo = btn.DataContext as bitModel;
            if (bmo == null) return;
            ConvertTargetToOriginal(ref bmo);
        }


        private void ConvertTargetToOriginal(ref Model mo)
        {
            bool bret = false;
            byte nbits = 0;
            bitModel bmo = null;
            string tmp = string.Empty;
            UInt32 udata = 0, mask = 0, wval = 0;

            if (mo.bHexDec)
            {
                if (mo.sudata.Contains(ElementDefine.prefix))
                    tmp = mo.sudata.Substring(ElementDefine.prefix.Length);
                else
                    tmp = mo.sudata;
                if (!UInt32.TryParse(tmp, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out wval)) return;
            }
            else
            {
                tmp = mo.sudata;
                if (!UInt32.TryParse(tmp, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out wval)) return;
            }
            for (int i = 0; i < mo.bitModel_List.Count; i++)
            {
                bmo = mo.bitModel_List[i];
                if (bmo == null) continue;

                if (bmo.bTargetHexDec)
                {
                    if (bmo.suTarget.Contains(ElementDefine.prefix))
                        tmp = bmo.suTarget.Substring(ElementDefine.prefix.Length);
                    else
                        tmp = bmo.suTarget;
                    bret = UInt32.TryParse(tmp, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out udata);
                }
                else
                {
                    tmp = bmo.suTarget;
                    bret = UInt32.TryParse(tmp, out udata);
                }
                if (!bret)
                {
                    m_Debug_Infors.Add(new Infor(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL)));
                    return;
                }
                if ((bmo.bitHn - bmo.bitLn + 1) == 32)
                    wval = udata;
                else
                {
                    nbits = (byte)(bmo.bitHn - bmo.bitLn + 1);
                    mask = (UInt32)((1 << (bmo.bitHn - bmo.bitLn + 1)) - 1);
                    mask <<= bmo.bitLn;
                    wval &= (UInt32)(~mask);
                    udata = (udata << bmo.bitLn);
                    udata &= mask;
                    wval |= udata;
                }
            }
            if (mo.bHexDec)
                mo.sudata = string.Format("0x{0:x8}", wval);
            else
                mo.sudata = string.Format("{0}", wval);
        }

        private void ConvertTargetToOriginal(ref bitModel mo)
        {
            bool bret = false;
            byte nbits = 0;
            string tmp = string.Empty;
            UInt32 udata = 0, mask = 0, wval = 0;

            if (mo.bTargetHexDec)
            {
                if (mo.suTarget.Contains(ElementDefine.prefix))
                    tmp = mo.suTarget.Substring(ElementDefine.prefix.Length);
                else
                    tmp = mo.suTarget;
                bret = UInt32.TryParse(tmp, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out udata);
            }
            else
            {
                tmp = mo.suTarget;
                bret = UInt32.TryParse(tmp, out udata);
            }

            mo.model_Parent.sudata = mo.model_Parent.sudata; //Support RW command
            if (!bret)
            {
                m_Debug_Infors.Add(new Infor(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL)));
                return;
            }
            if ((mo.bitHn - mo.bitLn + 1) == 32)
                wval = udata;
            else
            {
                nbits = (byte)(mo.bitHn - mo.bitLn + 1);
                mask = (UInt32)((1 << (mo.bitHn - mo.bitLn + 1)) - 1);
                mask <<= mo.bitLn;
                wval = mo.model_Parent.udata;
                wval &= (UInt32)(~mask);
                udata = (udata << mo.bitLn);
                udata &= mask;
                wval |= udata;
            }
            if (mo.model_Parent.bHexDec)
                mo.model_Parent.sudata = string.Format("0x{0:x8}", wval);
            else
                mo.model_Parent.sudata = string.Format("{0}", wval);
        }

        private void retryBtn_Click(object sender, RoutedEventArgs e)
        {
            ushort cur_Task = 0;
            string access_mode = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            Model mo = (sender as Button).DataContext as Model;
            if (mo == null) return;
            viewmode.dm_parameterlist.parameterlist.Clear();
            ret = preRun();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 1;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                gm.bupdate = true;
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            viewmode.dm_parameterlist.parameterlist.Add(mo.pParent);

            switch (mo.type)
            {
                case 0:
                    cur_Task = m_readTask;
                    access_mode = "Read";
                    break;
                case 1:
                    cur_Task = m_writeTask;
                    access_mode = "Write";
                    break;
                case 2:
                    cur_Task = m_rwTask;
                    access_mode = "RW";
                    break;
            }
            switch (cur_Task)
            {
                case m_rwTask:
                    mo.PropertyChanged -= mo.Model_PropertyChanged;
                    AccessDevice(mo, "Read", m_readTask);
                    mo.PropertyChanged += mo.Model_PropertyChanged;
                    Thread.Sleep(1);
                    ConvertTargetToOriginal(ref mo);
                    AccessDevice(mo, "Write", m_writeTask);
                    break;
                default:
                    AccessDevice(mo, access_mode, cur_Task);
                    break;
            }
        }

        private void hexDecCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            Model mo = checkBox.DataContext as Model;
            if (mo == null) return;
            if (mo.bHexDec)
                mo.sudata = string.Format("0x{0:x8}", mo.udata);
            else
                mo.sudata = string.Format("{0}", mo.udata);
        }

        private void hexDecTargetCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            bitModel mo = checkBox.DataContext as bitModel;
            if (mo == null) return;
            if (mo.bTargetHexDec)
                mo.suTarget = string.Format("0x{0:x8}", mo.utarget);
            else
                mo.suTarget = string.Format("{0}", mo.utarget);
        }
        private void bitAddBtn_Click(object sender, RoutedEventArgs e)
        {
            bitModel bmo = null;
            Button btn = sender as Button;
            Model mo = btn.DataContext as Model;
            if (mo == null) return;
            bmo = new bitModel(mo);
            mo.bitModel_List.Add(bmo);
        }

        private void bitCommandDeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            bitModel bmo = btn.DataContext as bitModel;
            if (bmo == null) return;
            bmo.model_Parent.bitModel_List.Remove(bmo);
        }

        private void formulaAddBtn_Click(object sender, RoutedEventArgs e)
        {
            formulaModel fModel = null;
            Button btn = sender as Button;
            Model mo = btn.DataContext as Model;
            m_formula_dic.Clear();
            if (mo == null) return;
            if (parent.bBusy) return;

            parent.bBusy = true;
            msg.task_parameterlist = viewmode.dm_parameterlist;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = m_formulaTask;
            msg.sub_task_json = string.Empty;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            parent.bBusy = false;

            m_formula_dic = SharedAPI.DeserializeStringToDictionary<string, Tuple<int, string, string>>(msg.sub_task_json);
            if (m_formula_dic.Count == 0) return;

            fModel = new formulaModel(mo, m_formula_dic);
            mo.formulaModel_List.Add(fModel);
        }
        private void formulaDeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            formulaModel fmo = btn.DataContext as formulaModel;
            if (fmo == null) return;
            fmo.model_Parent.formulaModel_List.Remove(fmo);
        }

        private void countFormulaBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            formulaModel fmo = btn.DataContext as formulaModel;
            if (fmo == null) return;

            viewmode.dm_parameterlist.parameterlist.Clear();
            viewmode.dm_parameterlist.parameterlist.Add(fmo.pParent);
            parent.bBusy = true;
            msg.task_parameterlist = viewmode.dm_parameterlist;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = m_countTask;
            msg.sub_task_json = string.Empty;
            msg.funName = fmo.curFormulaModel.formula;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            parent.bBusy = false;
        }
        #endregion

        #region method
        private UInt32 preRun()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (parent.bBusy)
            {
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
                ret = msg.errorcode;
                return ret;
            }
            return ret;
        }
        #endregion

        #region MenuItem
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Model mo = new Model(viewmode.robot_commands.Count);
            viewmode.robot_commands.Add(mo);
        }
        private void MoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = mDataGrid.SelectedItem;
            Model mo = item as Model;
            if (mo == null)
            {
                gm.message = "Please select one row to delete!";
                gm.level = 1;
                CallWarningControl(gm);
                return;
            }
            MoveWindow moveWindow = new MoveWindow(this);
            moveWindow.Owner = Window.GetWindow(this);
            if (moveWindow.ShowDialog() != true) return;
            viewmode.robot_commands.Move(mo.id, m_moveToIndex);
            for (int i = 0; i < viewmode.robot_commands.Count; i++)
            {
                mo = viewmode.robot_commands[i];
                if (mo == null) continue;
                mo.id = i;
            }
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewmode.robot_commands.Clear();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = mDataGrid.SelectedItem;
            Model mo = item as Model;
            if (mo == null)
            {
                gm.message = "Please select one row to delete!";
                gm.level = 1;
                CallWarningControl(gm);
                return;
            }
            viewmode.robot_commands.Remove(mo);
            for (int i = 0; i < viewmode.robot_commands.Count; i++)
            {
                mo = viewmode.robot_commands[i];
                if (mo == null) continue;
                mo.id = i;
            }
        }

        private void InsertMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            var item = mDataGrid.SelectedItem;
            Model mo = item as Model;
            if (mo == null)
            {
                mo = new Model(viewmode.robot_commands.Count);
                viewmode.robot_commands.Add(mo);
                return;
            }
            index = mo.id + 1;
            mo = new Model(index);
            viewmode.robot_commands.Insert(index, mo);
            for (int i = 0; i < viewmode.robot_commands.Count; i++)
            {
                mo = viewmode.robot_commands[i];
                if (mo == null) continue;
                mo.id = i;
            }
        }
        #endregion
    }
}
