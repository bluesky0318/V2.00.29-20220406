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
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.HexEditorPanel
{
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

        private TASKMessage m_Msg = new TASKMessage();
        public TASKMessage msg
        {
            get { return m_Msg; }
            set { m_Msg = value; }
        }

        private ViewModel m_viewmode;
        public ViewModel viewmode
        {
            get { return m_viewmode; }
            set { m_viewmode = value; }
        }

        private UIConfig m_UI_Config = new UIConfig();
        public UIConfig ui_config
        {
            get { return m_UI_Config; }
            set { m_UI_Config = value; }
        }

        public byte bMask = 0; //是否采用Order排序模式
        public ControlMessage cmg = new ControlMessage();
        public GeneralMessage gm = new GeneralMessage("Project SFL", "", 0);
        private ObservableCollection<Infor> m_Debug_Infors = new ObservableCollection<Infor>();
        private Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();

        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;

            InitalUI();
            viewmode = new ViewModel(pParent, this);
            projFiles.ItemsSource = viewmode.sfl_parameterlist;
            DebugListBox.ItemsSource = m_Debug_Infors;

            m_Debug_Infors.Clear();
            m_Debug_Infors.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Infors_CollectionChanged);
            m_Debug_Infors.Add(new Infor("Welcome to hex editor SFL...."));
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            msg.gm.sflname = sflname;
            #endregion
        }

        public void InitalUI()
        {
            bool bdata = false;
            string name = String.Empty;
            string ctrlName = string.Empty;
            ObservableCollection<string> itemList = new ObservableCollection<string>();
            XmlNodeList nodelist = parent.GetUINodeList(sflname);
            if (nodelist == null) return;

            foreach (XmlNode node in nodelist)
            {
                if (node.Attributes["Name"] == null) continue;
                name = node.Attributes["Name"].Value.ToString();
                switch (name)
                {
                    case "layout":
                        {
                            if (node.Attributes["bMask"] != null)
                            {
                                if (bool.TryParse(node.Attributes["bMask"].Value.ToString(), out bdata))
                                    bMask = (byte)(bdata ? 0xFF : 0);
                                else
                                    bMask = 0;
                            }
                            foreach (XmlNode sub in node)
                            {
                                if (sub.Attributes["Name"] == null) continue;
                                ctrlName = sub.Attributes["Name"].Value.Trim();
                                switch (ctrlName)
                                {
                                    case "selectCB":
                                        {
                                            itemList.Clear();
                                            foreach (XmlNode xn in sub.ChildNodes)
                                                itemList.Add(xn.InnerText.Trim());
                                            selectCB.ItemsSource = itemList;
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                }
            }
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
                        case COMMON_CONTROL.COMMON_CONTROL_SELECT:
                            {
                                CallSelectControl(msg.gm);
                                break;
                            }
                    }
                    break;
            }
        }

        #region 文件操作
        private void CloseFile()
        {
            Model pfl = null;
            pfl = viewmode.GetModelByGuid(ElementDefine.HexFileElement);
            pfl.name = "Hex File";
            pfl.bExist = false;
            pfl.folderPath = string.Empty;
            pfl.info = string.Empty;
            pfl.used = string.Empty;
            viewmode.sfl_parameterlist.Clear();
            viewmode.sfl_parameterlist.Add(pfl);
            projFiles.ItemsSource = viewmode.sfl_parameterlist;
        }

        private void OpenFile()
        {
            Model pfl = null;
            string fileName = "Hex";
            string title = "Open Hex File";
            string filter = "Hex files (*.hex)|*.hex|Bin files(*.bin)|*.bin||";

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = title;
            openFileDialog.Filter = filter;
            openFileDialog.FileName = fileName;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "hex";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (openFileDialog.ShowDialog() == false) return;

            FileInfo fil = new FileInfo(openFileDialog.FileName);
            pfl = viewmode.GetModelByGuid(ElementDefine.HexFileElement);
            pfl.name = fil.Name;
            pfl.ext = fil.Name.Substring(fil.Name.LastIndexOf(".") + 1, (fil.Name.Length - fil.Name.LastIndexOf(".") - 1));
            pfl.bExist = true;
            pfl.folderPath = fil.DirectoryName;
            pfl.info = string.Format("Size:{0:F2}KB", fil.Length / 1000.0);

            switch (pfl.ext.ToLower())
            {
                case "hex":
                    ParseHexFile(ref pfl);
                    break;
                case "bin":
                    ParseBinFile(ref pfl);
                    break;
            }
            pfl.used = string.Format("Flash Used:{0:F2}KB", pfl.szBin.Length / 1000.0);
            viewmode.sfl_parameterlist.Clear();
            viewmode.sfl_parameterlist.Add(pfl);
            projFiles.ItemsSource = viewmode.sfl_parameterlist;
        }

        public void ParseHexFile2(ref Model pf)
        {
            int pos;
            string line, tmp; 
            double dval, integer, fraction;
            UInt16 extaddress = 0, comaddress = 0;
            UInt32 uaddress = 0, MaxAddress = 0;
            Byte[] databuffer = new Byte[32];
            Byte length = 0, type = 0, btmp = 0;

            for (uaddress = 0; uaddress < ElementDefine.MTP_MAX_CAP; uaddress++)
                ElementDefine.m_MTP_Memory[uaddress] = bMask;//0x00;

            try
            {
                using (StreamReader sr = new StreamReader(@pf.fullName))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        pos = line.IndexOf(':');        // First char should be ":"
                        if (pos == -1) continue;
                        line = line.Remove(0, 1);       //remove it
                        tmp = line.Substring(0, 2);     // then, next 2 char are length
                        length = Convert.ToByte(tmp, 16);
                        line = line.Remove(0, 2);
                        tmp = line.Substring(0, 4);
                        //uaddress =  Convert.ToUInt32(tmp, 16);
                        comaddress = Convert.ToUInt16(tmp, 16);

                        line = line.Remove(0, 4);
                        tmp = line.Substring(0, 2); // then, next 1 char are type "00" means data, "01" means end of file
                        type = Convert.ToByte(tmp, 16);
                        line = line.Remove(0, 2);
                        switch(type)
                        {
                            case 00:
                                break;
                            case 01: //EOF
                                continue;
                            case 02:
                                continue;
                            case 04:
                                tmp = line.Substring(0, 4);
                                extaddress = Convert.ToUInt16(tmp, 16); 
                                continue;
                            case 05:
                                continue;
                        }
                        uaddress = SharedFormula.MAKEDWORD(comaddress, extaddress);
                        //if (type != 0) continue;
                        for (int i = 0; i < length; i++)
                        {
                            tmp = line.Substring(0, 2);
                            btmp = Convert.ToByte(tmp, 16);
                            databuffer[i] = btmp;
                            line = line.Remove(0, 2);
                        }
                        tmp = line.Substring(0, 2);
                        btmp = Convert.ToByte(tmp, 16);

                        for (btmp = 0; btmp < length; btmp++)
                            ElementDefine.m_MTP_Memory[uaddress + btmp] = databuffer[btmp];
                        if (uaddress > MaxAddress) MaxAddress = uaddress;
                    }
                }
                dval = (double)((double)MaxAddress/(double)1024.0);
                integer = Math.Truncate(dval);
                fraction = (double)(dval - integer);
                if (fraction != 0.0)
                    integer += 1;

                UInt16 nk = (UInt16)integer;
                pf.szBin = new byte[nk*1024];
                Array.Copy(ElementDefine.m_MTP_Memory, pf.szBin, nk*1024);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        public void ParseHexFile(ref Model pf)
        {
            int pos;
            string line, tmp;
            bool bType0 = false;
            double dval, integer, fraction;
            UInt16 address = 0;
            Byte length = 0, type = 0, btmp = 0;
            Byte[] databuffer = new Byte[256];
            byte[] total_image = null;
            List<MemoryControl> bufList = new List<MemoryControl>();
            UInt32 startAddress = 0, endAddress = 0, total_len = 0;
            UInt32 extaddress = 0, lineaddress = 0, uaddress = 0, MaxAddress = 0;
            MemoryControl m_Buffer_Control = null ;
            try
            {
                using (StreamReader sr = new StreamReader(@pf.fullName))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        pos = line.IndexOf(':');        // First char should be ":"
                        if (pos == -1) continue;
                        line = line.Remove(0, 1);       //remove it
                        tmp = line.Substring(0, 2);     // then, next 2 char are length
                        length = Convert.ToByte(tmp, 16);
                        line = line.Remove(0, 2);
                        tmp = line.Substring(0, 4);
                        lineaddress = Convert.ToUInt16(tmp, 16);

                        line = line.Remove(0, 4);
                        tmp = line.Substring(0, 2); // then, next 1 char are type "00" means data, "01" means end of file
                        type = Convert.ToByte(tmp, 16);
                        line = line.Remove(0, 2);
                        switch (type)
                        {
                            case 00:
                                total_len += length;
                                break;
                            case 01: //EOF
                                continue;
                            case 02:
                                bType0 = false;
                                tmp = line.Substring(0, 4);
                                address = Convert.ToUInt16(tmp, 16);
                                extaddress = (UInt32)address << 4;
                                if (bufList.Count != 0)
                                    m_Buffer_Control.totalSize = total_len;
                                total_len = 0;
                                continue;
                            case 03:
                                continue;
                            case 04:
                                bType0 = false;
                                tmp = line.Substring(0, 4);
                                address = Convert.ToUInt16(tmp, 16);
                                extaddress = (UInt32)address << 16;
                                if (bufList.Count != 0)
                                    m_Buffer_Control.totalSize = total_len;
                                total_len = 0;
                                continue;
                            case 05:
                                continue;
                        }
                        endAddress = MaxAddress;
                        uaddress = (UInt32)(extaddress + lineaddress);
                        if (!bType0)
                        {
                            bType0 = true;
                            startAddress = uaddress; 
                            if (bufList.Count != 0)
                            {
                                m_Buffer_Control.endAddress = endAddress;
                                m_Buffer_Control.Update();
                                Array.Copy(ElementDefine.m_MTP_Memory, 0, m_Buffer_Control.buffer, 0, m_Buffer_Control.totalSize);
                            }
                            m_Buffer_Control = new MemoryControl();
                            m_Buffer_Control.startAddress = startAddress;
                            endAddress = MaxAddress = 0;
                            bufList.Add(m_Buffer_Control);
                            Array.Clear(ElementDefine.m_MTP_Memory, 0, ElementDefine.m_MTP_Memory.Length);
                        }
                        for (int i = 0; i < length; i++)
                        {
                            tmp = line.Substring(0, 2);
                            btmp = Convert.ToByte(tmp, 16);
                            databuffer[i] = btmp;
                            line = line.Remove(0, 2);
                        }
                        tmp = line.Substring(0, 2);
                        btmp = Convert.ToByte(tmp, 16);
                        Array.Copy(databuffer, 0, ElementDefine.m_MTP_Memory, (uaddress - startAddress), length);
                        if (uaddress > MaxAddress) MaxAddress = uaddress;
                    }
                }
                m_Buffer_Control.endAddress = MaxAddress;
                m_Buffer_Control.totalSize = total_len;
                m_Buffer_Control.Update();
                Array.Copy(ElementDefine.m_MTP_Memory, 0, m_Buffer_Control.buffer, 0, m_Buffer_Control.totalSize);
                BuildEntireMemory(bufList, ref total_image);
                dval = (double)((double)total_image.Length / (double)1024.0);
                integer = Math.Truncate(dval);
                fraction = (double)(dval - integer);
                if (fraction != 0.0)
                    integer += 1;

                UInt16 nk = (UInt16)integer;
                pf.szBin = new byte[nk * 1024];
                pf.bufferList = bufList;
                Array.Copy(total_image, pf.szBin, total_image.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        public void ParseBinFile(ref Model pf)
        {
            double dval, integer, fraction;
            long MaxAddress = 0;
            UInt32 uaddress = 0;

            for (uaddress = 0; uaddress < ElementDefine.MTP_MAX_CAP; uaddress++)
                ElementDefine.m_MTP_Memory[uaddress] = bMask;// 0x00;

            try
            {
                using (FileStream fs = new FileStream(@pf.fullName, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(fs);
                    Array.Copy(br.ReadBytes((int)fs.Length), ElementDefine.m_MTP_Memory, fs.Length);
                    MaxAddress = fs.Length;
                }
                dval = (double)((double)MaxAddress / (double)1024.0);
                integer = Math.Truncate(dval);
                fraction = (double)(dval - integer);
                if (fraction != 0.0)
                    integer += 1;

                UInt16 nk = (UInt16)integer;
                pf.szBin = new byte[nk * 1024];
                Array.Copy(ElementDefine.m_MTP_Memory, pf.szBin, nk * 1024);
            }
            catch (Exception e)
            {
            }/*
            var bt = pf.szBin;
            SaveFile(@"E:\Test\test.bin", ref bt);*/
        }
        #endregion

        #region 按钮操作
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenFile();
        }

        private void CloseFileBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseFile();
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void FileDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            Model pfl = null;
            pfl = viewmode.GetModelByGuid(ElementDefine.HexFileElement);
            switch (pfl.ext.ToLower())
            {
                case "hex":
                    ParseHexFile(ref pfl);
                    break;
                case "bin":
                    ParseBinFile(ref pfl);
                    break;
            }
            
            m_Debug_Infors.Add(new Infor("Begin to Download..."));
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Write To Device button!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_Debug_Infors.Add(new Infor(gm.message));
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.percent = 30;
            msg.task = TM.TM_COMMAND;
			msg.sub_task = (UInt16)0x10;
            msg.sub_task_json = BuildJsonTask("TM_COMMAND", "Download");
            msg.flashData = pfl.szBin;//viewmode.GetModelByGuid(ElementDefine.HexFileElement).szBin;
            msg.bufferList = pfl.bufferList;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                m_Debug_Infors.Add(new Infor(gm.message));
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            m_Debug_Infors.Add(new Infor("Download successfully..."));
        }

        private void FileUploadBtn_Click(object sender, RoutedEventArgs e)
        {
            m_Debug_Infors.Add(new Infor("Begin to Upload..."));
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Read From Device button!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_Debug_Infors.Add(new Infor(gm.message));
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.percent = 30;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = (UInt16)0x20 ;
            msg.sub_task_json = BuildJsonTask("TM_COMMAND", "Upload");
            msg.flashData = new byte[viewmode.GetModelByGuid(ElementDefine.HexFileElement).szBin.Length];
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                m_Debug_Infors.Add(new Infor(gm.message));
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            m_Debug_Infors.Add(new Infor("Upload successfully..."));
            UploadHex2Bin(ref m_Msg);
        }

        private void DumpBtn_Click(object sender, RoutedEventArgs e)
        {
            UInt16 dumpsize = 32;
            if (string.IsNullOrEmpty(dumpSizeTb.Text.Trim()))
            {
                gm.level = 2;
                gm.message = "Please input dump size!";
                CallWarningControl(gm);
                return;
            }
            if (!UInt16.TryParse(dumpSizeTb.Text.Trim(),out dumpsize))
            {
                gm.level = 2;
                gm.message = "Please input valid dump size!";
                CallWarningControl(gm);
                return;
            }

            m_Debug_Infors.Add(new Infor(string.Format("Begin to Upload {0}KB...",dumpsize)));
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Dump button!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_Debug_Infors.Add(new Infor(gm.message));
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.percent = 30;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = (UInt16)0x20;
            msg.sub_task_json = BuildJsonTask("TM_COMMAND", "Upload");
            msg.flashData = new byte[dumpsize*1024];
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                m_Debug_Infors.Add(new Infor(gm.message));
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            m_Debug_Infors.Add(new Infor("Upload successfully..."));
            UploadHex2Bin(ref m_Msg);
        }

        private void EraseBtn_Click(object sender, RoutedEventArgs e)
        {
            msg.gm.message = "you are ready to erase entirely area,please be care!";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
            if (!msg.controlmsg.bcancel) return;

            m_Debug_Infors.Add(new Infor("Begin to erase..."));
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Erase To Device button!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                m_Debug_Infors.Add(new Infor(gm.message));
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.percent = 30;
            msg.task = TM.TM_BLOCK_ERASE;
            msg.sub_task_json = BuildJsonTask("TM_BLOCK_ERASE", "ERASE");
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                m_Debug_Infors.Add(new Infor(gm.message));
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            m_Debug_Infors.Add(new Infor("Erase successfully..."));
        }

        private void ReadSignBtn_Click(object sender, RoutedEventArgs e)
        {
            m_Debug_Infors.Add(new Infor("Begin to read device signature..."));
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Read signature button!";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY); 
                m_Debug_Infors.Add(new Infor(gm.message));
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

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
                m_Debug_Infors.Add(new Infor(gm.message));
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            parent.bBusy = false;
            m_Debug_Infors.Add(new Infor(string.Format(("Device ID：0x{0:x4}"), parent.device_infor.type).ToUpper()));
            m_Debug_Infors.Add(new Infor(string.Format(("BootLoader Version：{0}"), parent.device_infor.ateversion)));
            string[] arr = parent.device_infor.shwversion.Split('\n');
            m_Debug_Infors.Add(new Infor(string.Format("Serial Number:0x{0}-0x{1}-0x{2}-0x{3}", arr[0],arr[1],arr[2],arr[3])));
        }

        private void Hex2BtinBtn_Click(object sender, RoutedEventArgs e)
        {
            Model pfl = null;
            pfl = viewmode.GetModelByGuid(ElementDefine.HexFileElement);
            switch (pfl.ext.ToLower())
            {
                case "hex":
                    ParseHexFile(ref pfl);
                    break;
                case "bin":
                    ParseBinFile(ref pfl);
                    break;
            }

            m_Debug_Infors.Add(new Infor("Begin to convert hex to bin file..."));
            string fullpath = "";
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Title = "Save hex File";
            saveFileDialog.Filter = "Firmware bin files (*.bin)|*.bin|Firmware 32K bin files (*.bin)|*.bin||";
            saveFileDialog.FileName = "default";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "bin";
            saveFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (saveFileDialog.ShowDialog() == true)
            {
                if (parent == null) return;
                else
                {
                    fullpath = saveFileDialog.FileName;
                    var bdata = viewmode.GetModelByGuid(ElementDefine.HexFileElement).szBin;
                    switch(saveFileDialog.FilterIndex)
                    {
                        case 1:
                            SaveFile(fullpath, ref bdata);
                            break;
                        case 2:
                            SaveFile(fullpath, ref bdata, 32 * 1024);
                            break;
                    }
                }
            }
            m_Debug_Infors.Add(new Infor("Successful to convert hex to bin file..."));
        }

        private void UploadHex2Bin(ref TASKMessage msg)
        {
            m_Debug_Infors.Add(new Infor("Begin to convert hex to bin file..."));
            string fullpath = "";
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Title = "Save hex File";
            saveFileDialog.Filter = "Firmware hex files (*.bin)|*.bin||";
            saveFileDialog.FileName = "default";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "bin";
            saveFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (saveFileDialog.ShowDialog() == true)
            {
                if (parent == null) return;
                else
                {
                    fullpath = saveFileDialog.FileName;
                    var bdata = msg.flashData;
                    SaveFile(fullpath, ref bdata);
                }
            }
            m_Debug_Infors.Add(new Infor("Successful to convert hex to bin file..."));
        }

        internal void SaveFile(string fullpath, ref byte[] bdata, Int32 fileSize = 0)
        {
            using (var fs = new FileStream(fullpath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(fileSize > bdata.Length ? fileSize : bdata.Length);
                fs.Write(bdata, 0, bdata.Length);
                fs.Seek(0, SeekOrigin.End);
            }
        }

        internal string BuildJsonTask(string key, string value)
        {
            subTask_Dic.Clear();
            subTask_Dic.Add("SFL", sflname);
            subTask_Dic.Add(key, value);
            subTask_Dic.Add("selectCB", selectCB.SelectionBoxItem.ToString());
            subTask_Dic.Add("dumpSizeTb", dumpSizeTb.Text.Trim());
            subTask_Dic.Add("vap_Cb", vap_Cb.IsChecked == true ? "true" : "false");
            subTask_Dic.Add("eaf_Cb", eaf_Cb.IsChecked == true ? "true" : "false");
            return SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
        }
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

        private void ClearRecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            m_Debug_Infors.Clear();
        }

        private void BuildEntireMemory(List<MemoryControl> bufList, ref byte[] totalImage)
        {
            UInt32 slAddr = 0, elAddr = 0, mSize = 0, totalSize = 0;
            if (bufList == null | bufList.Count == 0) return;
            slAddr = elAddr = bufList[0].startAddress;
            foreach (MemoryControl mc in bufList)
            {
                if (slAddr >= mc.startAddress) slAddr = mc.startAddress;
                if (elAddr <= mc.startAddress)
                {
                    elAddr = mc.startAddress;
                    mSize = mc.totalSize;
                }
            }
            totalSize = (elAddr - slAddr) + mSize;
            totalImage = new byte[totalSize];
            foreach (MemoryControl mc in bufList)
            {
                Array.Copy(mc.buffer, 0, totalImage, (mc.startAddress - slAddr), mc.totalSize);
            }
        }
    }
}
