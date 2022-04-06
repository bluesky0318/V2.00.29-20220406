using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Cobra.EM;
using Cobra.Common;
using Cobra.ProjectPanel.Hex;
using Cobra.ProjectPanel.Param;
using Cobra.ProjectPanel.Table;

namespace Cobra.ProjectPanel
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        #region 变量定义
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

        public ControlMessage cmg = new ControlMessage();
        public GeneralMessage gm = new GeneralMessage("Project SFL", "", 0);
        public Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();
        #endregion

        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();

            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;
            viewmode = new ViewMode(pParent, this);

            gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            msg.gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);

            Init();
            #endregion
        }

        #region UI初始化
        public void Init()
        {
            viewmode.prj_paramContainer.parameterlist.Clear(); //清除工程文件参数列表
            CloseProject();
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
                        case COMMON_CONTROL.COMMON_CONTROL_SELECT:
                            {
                                CallSelectControl(msg.gm);
                                break;
                            }
                    }
                    break;
            }
        }
        #endregion

        #region 工程文件操作
        #region 工程操作
        private void Proj_Click(object sender, RoutedEventArgs e)
        {
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            CallWaitControl(cmg);

            Button cl = sender as Button;
            subTask_Dic.Clear();
            subTask_Dic.Add("Button", (cl.CommandParameter as string));
            switch (cl.CommandParameter as string)
            {
                case "OpenPrj":
                    OpenProject();
                    break;
                case "SavePrj":
                    SaveProject();
                    break;
                case "ClosePrj":
                    CloseProject();
                    break;
                case "NormalDownloadPrj":
                case "FullDownloadPrj":
                    DownloadProject();
                    break;
                case "FullErase":
                    Erase();
                    break;
            }
            cmg.bshow = false;
            CallWaitControl(cmg);
        }

        private void OpenProject()
        {
            string path = string.Empty;
            string fullpath = string.Empty;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Open Project File";
            openFileDialog.Filter = "Project files (*.prj)|*.prj||";
            openFileDialog.FileName = "Project";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "prj";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//FolderMap.m_extension_work_folder;
            if (openFileDialog.ShowDialog() == false) return;

            ClearFileFolder();
            FileInfo openPrj = new FileInfo(openFileDialog.FileName);
            GZipResult zipresult = GZip.Decompress(openPrj.DirectoryName, FolderMap.m_sm_work_folder, openPrj.Name);
            if (zipresult.Errors)
            {
                gm.level = 2;
                gm.controls = "Open project fail";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_SECTION_PROJECT_FILE_UNZIP_ERROR);
                CallWarningControl(gm);
                return;
            }

            viewmode.m_load_prj.name = openPrj.Name;
            //viewmode.m_load_prj.info = string.Format("Size:{0}KB\nLastWriteTime:{1:d}", openPrj.Length, openPrj.LastWriteTime);
            viewmode.m_load_prj.info = string.Format("Size:{0}Bytes", openPrj.Length);
            foreach (ProjFile pf in viewmode.m_empty_prj.projFiles)
            {
                if (pf == null) continue;
                DirectoryInfo dir = new DirectoryInfo(System.IO.Path.Combine(FolderMap.m_sm_work_folder, pf.folder));
                if (!dir.Exists) continue;
                foreach (FileInfo fil in dir.GetFiles())
                {
                    ProjFile pfl = pf.DeepCopy();
                    pfl.bExist = true;
                    pfl.name = fil.Name;
                    pfl.folderPath = fil.DirectoryName;
                    pfl.toolTip = string.Empty;
                    //pfl.info = string.Format("Size:{0}KB\nLastWriteTime:{1:d}", fil.Length, fil.LastWriteTime);
                    pfl.info = string.Format("Size:{0}Bytes", fil.Length);
                    viewmode.m_load_prj.Remove(pf);
                    switch ((FILE_TYPE)pfl.type)
                    {
                        case FILE_TYPE.FILE_HEX:
                            {
                                pfl.bshow = true;
                                pfl.userCtrl = new HexUserControl(this, ref pfl);
                                workPanel.Children.Add(pfl.userCtrl);
                            }
                            break;
                        case FILE_TYPE.FILE_PARAM:
                            pfl.userCtrl = new ParamUserControl(this, ref pfl);
                            break;
                        case FILE_TYPE.FILE_THERMAL_TABLE:
                        case FILE_TYPE.FILE_OCV_TABLE:
                        case FILE_TYPE.FILE_RC_TABLE:
                        case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                        case FILE_TYPE.FILE_FD_TABLE:
                            pfl.userCtrl = new TableUserControl(this, ref pfl);
                            break;
                        case FILE_TYPE.FILE_FGLITE_TABLE:
                            pfl.name = Path.GetFileNameWithoutExtension(fil.DirectoryName);
                            pfl.userCtrl = new FGTableUserControl(this, ref pfl);
                            break;
                    }
                    viewmode.m_load_prj.projFiles.Add(pfl);
                    if (dir.GetFiles().Length != 1) break;
                }
            }
            viewmode.m_load_prj.projFiles = new ObservableCollection<ProjFile>(viewmode.m_load_prj.projFiles.OrderBy(i => i.index));
            viewmode.m_load_prj.bReady = true;
            ProjTitle.DataContext = viewmode.m_load_prj;
            projFiles.ItemsSource = viewmode.m_load_prj.projFiles;
        }

        private void SaveProject()
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Title = "Save Project File";
            saveFileDialog.Filter = "Project file (*.prj)|*.prj||";
            saveFileDialog.FileName = "default";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "prj";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//FolderMap.m_extension_work_folder;
            if (saveFileDialog.ShowDialog() != true) return;

            foreach (ProjFile fl in viewmode.m_load_prj.projFiles)
                fl.userCtrl.SaveFile(fl);

            FileInfo file_prj = new FileInfo(saveFileDialog.FileName);
            string strzipfile = file_prj.Name; // This name should put into tree view and 
            string strzipfolder = file_prj.DirectoryName;
            GZip.Compress(FolderMap.m_sm_work_folder, file_prj.DirectoryName, file_prj.Name);
            viewmode.m_load_prj.name = file_prj.Name;
            viewmode.m_load_prj.info = string.Format("Size:{0}Bytes", file_prj.Length);
        }

        private void CloseProject()
        {
            workPanel.Children.Clear();
            viewmode.m_load_prj = viewmode.m_empty_prj.DeepCopy();
            ProjTitle.DataContext = viewmode.m_empty_prj;
            projFiles.ItemsSource = viewmode.m_empty_prj.projFiles;

            ClearFileFolder();
        }

        private void ClearFileFolder()
        {
            foreach (ProjFile pf in viewmode.m_empty_prj.projFiles)
            {
                if (pf == null) continue;
                DirectoryInfo dir = new DirectoryInfo(System.IO.Path.Combine(FolderMap.m_sm_work_folder, pf.folder));
                if (dir.Exists) dir.Delete(true);
                dir.Create();
            }
        }
        #endregion

        #region 文件操作
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProjFile pf = ((FrameworkElement)sender).Tag as ProjFile;
            OpenFile(pf);
            ShowFile(pf);
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            CallWaitControl(cmg);

            Button cl = sender as Button;
            subTask_Dic.Clear();
            subTask_Dic.Add("Button", (cl.CommandParameter as string));
            ProjFile pf = ((Button)sender).Tag as ProjFile;
            switch (cl.CommandParameter as string)
            {
                case "OpenFile":
                    OpenFile(pf);
                    break;
                case "ShowFile":
                    ShowFile(pf);
                    break;
                case "UploadBtn":
                    UploadFile(pf);
                    break;
                case "DownloadBtn":
                    DownloadFile(pf);
                    break;
            }
            cmg.bshow = false;
            CallWaitControl(cmg);
        }

        private void OpenFile(ProjFile pf)
        {
            bool bready = true;
            string filter = string.Empty;
            string title = string.Empty;
            string fileName = string.Empty;
            ProjFile pfl = null;
            switch ((FILE_TYPE)pf.type)
            {
                case FILE_TYPE.FILE_HEX:
                    fileName = "Hex";
                    title = "Open Hex File";
                    filter = "Hex files (*.hex)|*.hex||";
                    break;
                case FILE_TYPE.FILE_PARAM:
                    fileName = "Parameter";
                    title = "Open Parameter File";
                    filter = "Parameter files (*.xml)|*.xml||";
                    break;
                case FILE_TYPE.FILE_THERMAL_TABLE:
                case FILE_TYPE.FILE_OCV_TABLE:
                case FILE_TYPE.FILE_RC_TABLE:
                case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                case FILE_TYPE.FILE_FD_TABLE:
                    fileName = "Table";
                    title = "Open Table File";
                    filter = "Table files (*.txt)|*.txt||";
                    break;
                case FILE_TYPE.FILE_FGLITE_TABLE:
                    title = "Please select the table folder";
                    break;
            }

            switch ((FILE_TYPE)pf.type)
            {
                case FILE_TYPE.FILE_HEX:
                case FILE_TYPE.FILE_PARAM:
                case FILE_TYPE.FILE_THERMAL_TABLE:
                case FILE_TYPE.FILE_OCV_TABLE:
                case FILE_TYPE.FILE_RC_TABLE:
                case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                case FILE_TYPE.FILE_FD_TABLE:
                    {
                        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                        openFileDialog.Title = title;
                        openFileDialog.Filter = filter;
                        openFileDialog.FileName = fileName;
                        openFileDialog.FilterIndex = 1;
                        openFileDialog.RestoreDirectory = true;
                        openFileDialog.DefaultExt = "hex";
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        if (openFileDialog.ShowDialog() == false) return;

                        #region 将打开文件复制到工程目录下
                        string path = System.IO.Path.Combine(FolderMap.m_sm_work_folder + pf.folder);
                        if (Directory.Exists(path))
                            Directory.Delete(path, true);
                        Directory.CreateDirectory(path);
                        string targetpathfile = System.IO.Path.Combine(path, openFileDialog.SafeFileName);

                        File.Copy(openFileDialog.FileName, targetpathfile, true);
                        #endregion

                        FileInfo fil = new FileInfo(openFileDialog.FileName);
                        pfl = pf.DeepCopy();
                        pfl.bExist = true;
                        pfl.name = fil.Name;
                        pfl.folderPath = fil.DirectoryName;
                        pfl.toolTip = string.Empty;
                        //pfl.info = string.Format("Size:{0}KB\nLastWriteTime:{1:d}", fil.Length, fil.LastWriteTime);
                        pfl.info = string.Format("Size:{0}Bytes", fil.Length);
                    }
                    break;
                case FILE_TYPE.FILE_FGLITE_TABLE:
                    {
                        System.Windows.Forms.FolderBrowserDialog openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
                        openFolderDialog.Description = title;
                        if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                        string path = System.IO.Path.Combine(FolderMap.m_sm_work_folder + pf.folder);
                        if (Directory.Exists(path))
                            Directory.Delete(path, true);
                        Directory.CreateDirectory(path);

                        foreach (string file in System.IO.Directory.GetFiles(openFolderDialog.SelectedPath))
                        {
                            string name = System.IO.Path.GetFileName(file);
                            string dest = System.IO.Path.Combine(path, name);
                            File.Copy(file, dest);//复制文件
                        }

                        pfl = pf.DeepCopy();
                        pfl.bExist = true;
                        pfl.name = Path.GetFileNameWithoutExtension(openFolderDialog.SelectedPath);
                        pfl.folderPath = openFolderDialog.SelectedPath;
                        pfl.toolTip = string.Empty;
                        pfl.info = string.Empty;
                    }
                    break;
            }

            viewmode.m_load_prj.Remove(pf);
            switch ((FILE_TYPE)pfl.type)
            {
                case FILE_TYPE.FILE_HEX:
                    pfl.userCtrl = new HexUserControl(this, ref pfl);
                    break;
                case FILE_TYPE.FILE_PARAM:
                    pfl.userCtrl = new ParamUserControl(this, ref pfl);
                    break;
                case FILE_TYPE.FILE_THERMAL_TABLE:
                case FILE_TYPE.FILE_OCV_TABLE:
                case FILE_TYPE.FILE_RC_TABLE:
                case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                case FILE_TYPE.FILE_FD_TABLE:
                    pfl.userCtrl = new TableUserControl(this, ref pfl);
                    break;
                case FILE_TYPE.FILE_FGLITE_TABLE:
                    pfl.userCtrl = new FGTableUserControl(this, ref pfl);
                    break;
            }
            workPanel.Children.Clear();
            workPanel.Children.Add(pfl.userCtrl);
            viewmode.m_load_prj.projFiles.Add(pfl);
            viewmode.m_load_prj.projFiles = new ObservableCollection<ProjFile>(viewmode.m_load_prj.projFiles.OrderBy(i => i.index));
            foreach (ProjFile fl in viewmode.m_load_prj.projFiles)
                bready &= fl.bExist;
            viewmode.m_load_prj.bReady = bready;
            ProjTitle.DataContext = viewmode.m_load_prj;
            projFiles.ItemsSource = viewmode.m_load_prj.projFiles;
        }

        private void ShowFile(ProjFile pf)
        {
            ProjFile pfl = null;
            for (int i = 0; i < viewmode.m_load_prj.projFiles.Count; i++)
            {
                pfl = viewmode.m_load_prj.projFiles[i];
                if (pfl == null) continue;
                pfl.bshow = false;
            }
            pf.bshow = true;
            workPanel.Children.Clear();
            if (pf.userCtrl != null)
                workPanel.Children.Add(pf.userCtrl);
        }
        #endregion

        private void DownloadProject()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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

            ret = viewmode.WriteDevice();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            msg.brw = false;
            msg.percent = 10;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            msg.flashData = viewmode.m_load_prj.GetFileByType(FILE_TYPE.FILE_HEX).data;
            msg.task_parameterlist.parameterlist = viewmode.parameterlist;
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
            UploadHex2Bin(ref m_Msg);

            msg.percent = 30;
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
                return;
            }

            msg.percent = 50;
            msg.task = TM.TM_COMMAND;
            msg.sub_task_json = BuildJsonTask();
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

        private void Erase()
        {
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Erase Device button";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.task = TM.TM_BLOCK_ERASE;
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

        #region Read
        public void Read()
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            msg.funName = MethodBase.GetCurrentMethod().Name;
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

            msg.brw = true;
            msg.percent = 10;
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
                return;
            }

            msg.percent = 20;
            msg.task = TM.TM_SPEICAL_GETREGISTEINFOR;
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

            msg.percent = 40;
            msg.task = TM.TM_READ;
            msg.sub_task_json = BuildJsonTask();
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

            msg.percent = 60;
            msg.task = TM.TM_BLOCK_MAP;
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

            msg.percent = 80;
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
        #endregion

        #region Write
        public void write()
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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

            ret = viewmode.WriteDevice();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }

            msg.brw = false;
            msg.percent = 10;
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
                return;
            }

            msg.percent = 20;
            msg.task = TM.TM_SPEICAL_GETREGISTEINFOR;
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

            msg.percent = 40;
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
                return;
            }
            //UploadHex2Bin(ref m_Msg);

            msg.percent = 50;
            msg.task = TM.TM_WRITE;
            msg.sub_task_json = BuildJsonTask();
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

            msg.percent = 70;
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

            msg.percent = 80;
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

            msg.percent = 90;
            msg.task = TM.TM_BLOCK_MAP;
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
        #endregion
        #endregion

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
        public void CallSelectControl(GeneralMessage message)
        {
            SelectPopControl.Dispatcher.Invoke(new Action(() =>
            {
                msg.controlmsg.bcancel = SelectPopControl.ShowDialog(message);
            }));
        }
        #endregion

        #region 其他函数
        internal string BuildJsonTask()
        {
            return SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
        }

        private void UploadHex2Bin(ref TASKMessage msg)
        {
            string prjName = System.IO.Path.GetFileNameWithoutExtension(viewmode.m_load_prj.name);
            string fullpath = FolderMap.m_logs_folder + prjName + DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + ".bin";
            var bdata = msg.flashData;
            SaveFile(fullpath, ref bdata);
        }

        internal void SaveFile(string fullpath, ref byte[] bdata)
        {
            FileInfo file = new FileInfo(@fullpath);

            // Open the stream for writing. 
            using (FileStream fs = file.OpenWrite())
                fs.Write(bdata, 0, bdata.Length);// from load hex
        }

        /// <summary>
        /// 复制文件夹及文件
        /// </summary>
        /// <param name="sourceFolder">原文件路径</param>
        /// <param name="destFolder">目标文件路径</param>
        /// <returns></returns>
        public int CopyFolder(string sourceFolder, string destFolder)
        {
            try
            {
                //如果目标路径不存在,则创建目标路径
                if (!System.IO.Directory.Exists(destFolder))
                {
                    System.IO.Directory.CreateDirectory(destFolder);
                }
                //得到原文件根目录下的所有文件
                string[] files = System.IO.Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = System.IO.Path.GetFileName(file);
                    string dest = System.IO.Path.Combine(destFolder, name);
                    System.IO.File.Copy(file, dest);//复制文件
                }
                return 1;
            }
            catch (Exception e)
            {
                return 0;
            }

        }
        #endregion

        private void UploadFile(ProjFile pf)
        {
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            CallWaitControl(cmg);

            switch ((FILE_TYPE)pf.type)
            {
                case FILE_TYPE.FILE_PARAM:
                    msg.gm.controls = "Read all parameters";
                    msg.task_parameterlist.parameterlist = viewmode.parameterlist;
                    break;
                case FILE_TYPE.FILE_FGLITE_TABLE:
                    msg.gm.controls = "Upload FGLite table";
                    msg.task_parameterlist.parameterlist.Clear();
                    msg.sub_task = pf.type;
                    (pf.userCtrl as Table.FGTableUserControl).WriteDevice();
                    break;
            }
            Read();
            switch ((FILE_TYPE)pf.type)
            {
                case FILE_TYPE.FILE_PARAM:
                    break;
                case FILE_TYPE.FILE_FGLITE_TABLE:
                    UpdateLUT(pf, msg.sub_task_json);
                    break;
            }
            cmg.bshow = false;
            CallWaitControl(cmg);
        }

        private void DownloadFile(ProjFile pf)
        {
            cmg.message = "Please waiting....";
            cmg.bshow = true;
            CallWaitControl(cmg);

            switch ((FILE_TYPE)pf.type)
            {
                case FILE_TYPE.FILE_PARAM:
                    msg.gm.controls = "Write all parameters";
                    msg.sub_task = pf.type;
                    msg.task_parameterlist.parameterlist = viewmode.parameterlist;
                    break;
                case FILE_TYPE.FILE_FGLITE_TABLE:
                    msg.gm.controls = "Download FGLite table";
                    msg.sub_task = pf.type;
                    break;
            }
            write();
            cmg.bshow = false;
            CallWaitControl(cmg);
        }

        private void UpdateLUT(ProjFile pf, string subJson)
        {
            (pf.userCtrl as FGTableUserControl).UpdateTable(subJson);
        }
    }
}
