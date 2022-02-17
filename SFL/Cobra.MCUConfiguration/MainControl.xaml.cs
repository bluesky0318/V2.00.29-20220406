using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.Win32;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using System.Threading;
using System.Windows.Forms;
using Cobra.EM;
using Cobra.Common;

namespace Cobra.MCUConfigurationPanel
{
    enum editortype
    {
        TextBox_EditType = 0,
        ComboBox_EditType = 1,
        CheckBox_EditType = 2
    }

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

        private TASKMessage m_Msg = new TASKMessage();
        public TASKMessage msg
        {
            get { return m_Msg; }
            set { m_Msg = value; }
        }

        private Byte [] m_bFirmware = new Byte[0xA000];
        public Byte [] bFirmware
        {
            get { return m_bFirmware; }
            set { m_bFirmware = value; }
        }

        private Byte[] m_buserparameter = new Byte[0x400];
        public Byte[] buserparameter
        {
            get { return m_buserparameter; }
            set { m_buserparameter = value; }
        }
        private Byte[] m_bFirmware_read = new Byte[0xA000];
        public Byte[] bFirmware_read
        {
            get { return m_bFirmware_read; }
            set { m_bFirmware_read = value; }
        }


        private SFLViewMode m_viewmode;
        public SFLViewMode viewmode
        {
            get { return m_viewmode; }
            set { m_viewmode = value; }
        }

        public GeneralMessage gm = new GeneralMessage("Device Configuration SFL", "", 0);
        private UIConfig m_UI_Config = new UIConfig();
        public UIConfig ui_config
        {
            get { return m_UI_Config; }
            set { m_UI_Config = value; }
        }

        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;

            InitalUI();
            gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            msg.gm.PropertyChanged += new PropertyChangedEventHandler(gm_PropertyChanged);

            viewmode = new SFLViewMode(pParent, this);

            PasswordPopControl.SetParent(mDataGrid);
            WarningPopControl.SetParent(mDataGrid);
            WaitPopControl.SetParent(mDataGrid);
            #endregion

            ListCollectionView GroupedCustomers = new ListCollectionView(viewmode.sfl_parameterlist);
            GroupedCustomers.GroupDescriptions.Add(new PropertyGroupDescription("catalog"));
            mDataGrid.ItemsSource = GroupedCustomers;
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.SFL);
        }

        public void InitalUI()
        {
            string name = String.Empty;
            bool bdata = false;
            UInt16 wdata = 0;
            XmlNodeList nodelist = parent.GetUINodeList(sflname);
            if (nodelist == null) return;
            foreach (XmlNode node in nodelist)
            {
                name = node.Attributes["Name"].Value.ToString();
                switch (name)
                {
                    case "layout":
                        {
                            foreach (XmlNode sub in node)
                            {
                                btnControl btCtrl = new btnControl();
                                btCtrl.btn_name = sub.Attributes["Name"].Value.ToString();
                                if (Boolean.TryParse(sub.Attributes["IsEnable"].Value.ToString(), out bdata))
                                    btCtrl.benable = bdata;
                                else
                                    btCtrl.benable = true;

                                foreach (XmlNode subxn in sub.ChildNodes)
                                {
                                    XmlElement xe = (XmlElement)subxn;
                                    subMenu sm = new subMenu();
                                    System.Windows.Controls.MenuItem btn_cm_mi = new System.Windows.Controls.MenuItem();

                                    sm.header = xe.GetAttribute("Name");
                                    if (UInt16.TryParse(xe.GetAttribute("TYPE"), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out wdata))
                                        sm.type = wdata;
                                    else if (UInt16.TryParse(xe.GetAttribute("SUBTASK"), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out wdata))
                                        sm.type = wdata;

                                    else
                                        sm.type = 0;

                                    btCtrl.btn_menu_control.Add(sm);
                                    btn_cm_mi.Header = sm.header;
                                    btn_cm_mi.CommandParameter = sm.type;
                                    btn_cm_mi.Click += MenuItem_Click;
                                    btCtrl.btn_cm.Items.Add(btn_cm_mi);
                                }
                                ui_config.btn_controls.Add(btCtrl);
                            }
                            break;
                        }
                }
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
                        case COMMON_CONTROL.COMMON_CONTROL_PASSWORD:
                            {
                                CallPasswordControl(msg.controlmsg);
                                break;
                            }
                        case COMMON_CONTROL.COMMON_CONTROL_SELECT:
                            {
                                CallSelectControl(msg.gm);
                                break;
                            }
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

        private void LoadBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
//////////////////////////////////////////////////////////
            msg.owner = this;
            msg.gm.sflname = sflname;
            btnControl btn_ctrl = null;
            switch (sender.GetType().Name)
            {
                case "MenuItem":

                    gm.level = 2;
                    gm.message = "So far, there is no function for this button.";
                    CallWarningControl(gm);
                    parent.bBusy = false;
                    return;

                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    msg.gm.controls = "Read One parameter";
                    msg.task_parameterlist = viewmode.dm_part_parameterlist;
                    break;
                case "Button":
                    msg.gm.controls = ((System.Windows.Controls.Button)sender).Content.ToString();
                    msg.task_parameterlist = viewmode.dm_parameterlist;

                    System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                    btn_ctrl = ui_config.GetBtnControlByName(btn.Name);
                    if (btn_ctrl == null) break;
                    if (btn_ctrl.btn_menu_control.Count == 0) break;

                    btn_ctrl.btn_cm.PlacementTarget = btn;
                    btn_ctrl.btn_cm.IsOpen = true;
                    return;
                default:
                    break;
            }
         //   return;
/////////////////////////////////////////////
            //If we didn't have menu items.
            string fullpath = "";
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Load Firmware file File";
            openFileDialog.Filter = "MerlionPD firmware files (*.hex)|*.hex||";
            openFileDialog.FileName = "default";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "hex";
            openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (openFileDialog.ShowDialog() == true)
            {
                if (parent == null) return;
                {
                    fullpath = openFileDialog.FileName;
                    LoadFile(fullpath, ref m_bFirmware);
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //////////////////////////////////////////////////////////
            msg.owner = this;
            msg.gm.sflname = sflname;
            btnControl btn_ctrl = null;
            switch (sender.GetType().Name)
            {
                case "MenuItem":

                    gm.level = 2;
                    gm.message = "So far, there is no function for this button.";
                    CallWarningControl(gm);
                    parent.bBusy = false;
                    return;

                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    msg.gm.controls = "Read One parameter";
                    msg.task_parameterlist = viewmode.dm_part_parameterlist;
                    break;
                case "Button":
                    msg.gm.controls = ((System.Windows.Controls.Button)sender).Content.ToString();
                    msg.task_parameterlist = viewmode.dm_parameterlist;

                    System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                    btn_ctrl = ui_config.GetBtnControlByName(btn.Name);
                    if (btn_ctrl == null) break;
                    if (btn_ctrl.btn_menu_control.Count == 0) break;

                    btn_ctrl.btn_cm.PlacementTarget = btn;
                    btn_ctrl.btn_cm.IsOpen = true;
                    return;
                default:
                    break;
            }
           // return;
            /////////////////////////////////////////////
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
                    SaveFile(fullpath);
                }
            }
            StatusLabel.Content = fullpath;
        }

        internal void LoadFile(string fullpath, ref Byte [] firmwarebuffer)
        {
            int pos;
            double dval = 0.0;
            string line, tmp;
            UInt32 selfid;
            SFLModel model;
            Byte length = 0, type = 0, checksum = 0, btmp = 0 ;
            UInt16 uaddress = 0;
            Byte[] databuffer;//, firmwarebuffer;
            databuffer = new Byte[32];
            string tempstr;
           // firmwarebuffer = new Byte[0x8000];
           // char[] bin;

            // Clear firmware buffer first.
            for (uaddress = 0; uaddress < 0x8000; uaddress++)
            {
                firmwarebuffer[uaddress] = 0;
            }

                try
                {
                    // Create an instance of StreamReader to read from a file.
                    // The using statement also closes the StreamReader.
                    using (StreamReader sr = new StreamReader(@fullpath))
                    {
                        // Read and display lines from the file until the end of 
                        // the file is reached.
                        while ((line = sr.ReadLine()) != null)
                        {
                            checksum = 0;

                            // First char should be ":"
                            pos = line.IndexOf(':');
                            if (pos == -1) continue;
                            //remove it
                            line = line.Remove(0, 1);
                            // then, next 2 char are length
                            tmp = line.Substring(0, 2);
                            //length = Convert.ToUInt32(tmp, 16);
                            length = Convert.ToByte(tmp, 16);
                            checksum += length;
                            line = line.Remove(0, 2);
                            // Tehn, next 4 char are address offset
                            tmp = line.Substring(0, 4);
                            uaddress = Convert.ToUInt16(tmp, 16);

                            checksum += (Byte)uaddress;
                            checksum += (Byte)(uaddress >> 8);

                            line = line.Remove(0, 4);
                            // then, next 1 char are type "00" means data, "01" means end of file
                            tmp = line.Substring(0, 2);
                            type = Convert.ToByte(tmp, 16);
                            checksum += type;
                            line = line.Remove(0, 2);
                            if (type != 0)
                                continue;
                            // The data according to length. up to 16 (dec)
                            // line in here should be have only data with last check sum.
                            for (int i = 0; i < length; i++)
                            {
                                tmp = line.Substring(0, 2);
                                btmp = Convert.ToByte(tmp, 16);
                                checksum += btmp;
                                databuffer[i] = btmp;
                                line = line.Remove(0, 2);
                            }
                            // the last 1 char is checksum.
                            tmp = line.Substring(0, 2);
                            btmp = Convert.ToByte(tmp, 16);
                            checksum += btmp;
                            // Do checksum calculation for hex file in each line.
                            //  byte checksum = 0;
                            if (checksum == 0)
                            {
                                for (btmp = 0; btmp < length; btmp++)
                                {
                                    //databuffer = new Byte[32];
                                    firmwarebuffer[uaddress + btmp] = databuffer[btmp];
                                }
                            }




                        }
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }

                // calculate firmware checksum.
                checksum = 0;
                for (uaddress = 0; uaddress < 0x8000; uaddress++)
                {
                    checksum += firmwarebuffer[uaddress];
                }

            //    firmwarebuffer[0x8000] = checksum;
            
            //display checksum at status lable
            tempstr = "checksum = 0x" +checksum.ToString("X2") + ", ";
            StatusLabel.Content =tempstr+ fullpath;
        }

        internal void LoadPDCountryDefault(ref Byte[] parameterbuffer)
        {
            for (int i = 0x1F0; i < 0x2F8; i++)
                parameterbuffer[i] = 0;


            //Country codes

            parameterbuffer[0x01F0] = 0x04;

            parameterbuffer[0x01F2] = 0x01;

            parameterbuffer[0x01F4] = 0x4E;
            parameterbuffer[0x01F5] = 0x43;



            parameterbuffer[0x02F6] = 0x00;
            for (int i = 0x1F0; i < 0x2F6; i++)
                parameterbuffer[0x2F6] += parameterbuffer[i];

            parameterbuffer[0x02F7] = 0x55;


            //Country Info

            for (int i = 0x2F8; i < 0x400; i++)
                parameterbuffer[i] = 0;

            parameterbuffer[0x02F8] = 0x04;

            parameterbuffer[0x02FA] = 0x4E;
            parameterbuffer[0x02FB] = 0x43;

            parameterbuffer[0x03FE] = 0x00;
            for (int i = 0x2F8; i < 0x3FE; i++)
                parameterbuffer[0x3FE] += parameterbuffer[i];

            parameterbuffer[0x03FF] = 0x55;


        }
        internal void LoadPDParameterDefault(ref Byte[] parameterbuffer)
        {
            for (int i = 0x80; i < 0x16B; i++)
                parameterbuffer[i] = 0;
            //Array.Clear(parameterbuffer, 0x80, 0x16B-0x80);
            parameterbuffer[0x0080] = 0x03;

            parameterbuffer[0x0081] = 0xE9;
            parameterbuffer[0x0082] = 0x04;
            parameterbuffer[0x0083] = 0x08;
            parameterbuffer[0x0084] = 0x01;
            parameterbuffer[0x0085] = 0x91;
            parameterbuffer[0x0086] = 0x2c;
            parameterbuffer[0x0087] = 0x00;
            parameterbuffer[0x0088] = 0x02;
            parameterbuffer[0x0089] = 0xD1;
            parameterbuffer[0x008A] = 0x18;

            parameterbuffer[0x008B] = 0xc0;
            parameterbuffer[0x008C] = 0x61;
            parameterbuffer[0x008D] = 0x1e;
            parameterbuffer[0x008E] = 0x64;

            parameterbuffer[0x008F] = 0xc0;
            parameterbuffer[0x0090] = 0xdc;
            parameterbuffer[0x0091] = 0x1e;
            parameterbuffer[0x0092] = 0x38;

            //Leave above with 0
            parameterbuffer[0x009F] = 0x0B;
            parameterbuffer[0x00A0] = 0x97;
            parameterbuffer[0x00A1] = 0x00;
            parameterbuffer[0x00A2] = 0x00;

            //Leave above with 0
            parameterbuffer[0x00AD] = 0x85;
            parameterbuffer[0x00AE] = 0x13;


            parameterbuffer[0x00B1] = 0x01;
            parameterbuffer[0x00B2] = 0x97;
            parameterbuffer[0x00B3] = 0x0B;
            parameterbuffer[0x00B4] = 0x53;
            parameterbuffer[0x00B5] = 0x08;

            parameterbuffer[0x00C7] = 0x03;
            parameterbuffer[0x00C9] = 0x19;

            parameterbuffer[0x00D4] = 0x01;
            parameterbuffer[0x00D5] = 0x1A;

            parameterbuffer[0x00D6] = 0x97;
            parameterbuffer[0x00D7] = 0x0B;

            parameterbuffer[0x00D8] = 0x53;
            parameterbuffer[0x00D9] = 0x08;

            //da~ef to do
            parameterbuffer[0x00DA] = 0x4F;
            parameterbuffer[0x00DB] = 0x32;
            parameterbuffer[0x00DC] = 0x6D;
            parameterbuffer[0x00DD] = 0x69;

            parameterbuffer[0x00DE] = 0x63;
            parameterbuffer[0x00DF] = 0x72;
            parameterbuffer[0x00E0] = 0x6F;
            parameterbuffer[0x00E1] = 0x20;

            parameterbuffer[0x00E2] = 0x41;
            parameterbuffer[0x00E3] = 0x4D;
            parameterbuffer[0x00E4] = 0x44;
            parameterbuffer[0x00E5] = 0x30;

            parameterbuffer[0x00E6] = 0x30;
            parameterbuffer[0x00E7] = 0x30;
            parameterbuffer[0x00E8] = 0x30;
            parameterbuffer[0x00E9] = 0x2D;

            parameterbuffer[0x00EA] = 0x56;
            parameterbuffer[0x00EB] = 0x34;
            parameterbuffer[0x00EC] = 0x57;
            parameterbuffer[0x00ED] = 0x32;

            parameterbuffer[0x00EE] = 0x35;
            parameterbuffer[0x00EF] = 0x00;




            parameterbuffer[0x00F0] = 0x04;

            parameterbuffer[0x00F1] = 0x41;
            parameterbuffer[0x00F2] = 0xA0;
            parameterbuffer[0x00F3] = 0x00;
            parameterbuffer[0x00F4] = 0xFF;

            parameterbuffer[0x00F5] = 0x97;
            parameterbuffer[0x00F6] = 0x0B;
            parameterbuffer[0x00F7] = 0x80;
            parameterbuffer[0x00F8] = 0x01;

            parameterbuffer[0x00FF] = 0x53;
            parameterbuffer[0x0100] = 0x08;


            parameterbuffer[0x010D] = 0x02;

            parameterbuffer[0x010E] = 0x42;
            parameterbuffer[0x010F] = 0xA0;
            parameterbuffer[0x0110] = 0x00;
            parameterbuffer[0x0111] = 0xFF;

            parameterbuffer[0x0114] = 0x97;
            parameterbuffer[0x0115] = 0x0B;


            parameterbuffer[0x012A] = 0x01;

            parameterbuffer[0x012B] = 0x83;
            parameterbuffer[0x012C] = 0xA0;
            parameterbuffer[0x012D] = 0x97;
            parameterbuffer[0x012E] = 0x0B;

            parameterbuffer[0x0147] = 0x01;

            parameterbuffer[0x0148] = 0x83;
            parameterbuffer[0x0149] = 0xA0;


            parameterbuffer[0x0169] = 0x00;
            
            for (int i = 0x80; i < 0x169; i++)
                parameterbuffer[0x169] += parameterbuffer[i];

            parameterbuffer[0x016A] = 0x55;


        }
        internal void LoadSystemParameterDefault( ref Byte[] parameterbuffer)
        {
           // for (int i = 0; i < 0x40; i++)
           //     parameterbuffer[i] = 0;
            Array.Clear(parameterbuffer, 0, 0x40);

            parameterbuffer[0x0000] = 0x02;
            parameterbuffer[0x0001] = 0x3E;
            parameterbuffer[0x0002] = 0x03;
            parameterbuffer[0x0003] = 0x64;
            //parameterbuffer[0x0004] = 0x00;
            //parameterbuffer[0x0005] = 0x00;
            //parameterbuffer[0x0006] = 0x00;
            //parameterbuffer[0x0007] = 0x00;
            parameterbuffer[0x0008] = 0xfa;
            parameterbuffer[0x0009] = 0x4e;
            parameterbuffer[0x000A] = 0x20;
            parameterbuffer[0x000B] = 0x0e;
            parameterbuffer[0x000C] = 0x10;
            parameterbuffer[0x000D] = 0x13;
            parameterbuffer[0x000E] = 0x88;

            parameterbuffer[0x0012] = 0xC4;
            parameterbuffer[0x0013] = 0x01;
            parameterbuffer[0x0014] = 0x0B;
           // parameterbuffer[0x003E] = 0x98;//checksum
            parameterbuffer[0x003E] = 0x00;
            for (int i = 0; i < 0x3E; i++)
                parameterbuffer[0x3E] += parameterbuffer[i];

            parameterbuffer[0x003F] = 0x55;





        }
        internal void LoadParameterDefault(UInt16 lentgh, ref Byte[] parameterbuffer)
        {
            if (lentgh != 0x400)
                return;

            LoadSystemParameterDefault( ref parameterbuffer);
            LoadPDParameterDefault(ref parameterbuffer);
            LoadPDCountryDefault(ref parameterbuffer);

        }
 
        internal void LoadParameterFile(string fullpath, ref Byte[] parameterbuffer)
        {
 
            UInt16 uaddress = 0;
            //char [] charbuffer;
            //charbuffer = new char[0x400];
             // Clear firmware buffer first.
            // for (uaddress = 0; uaddress < 0x400; uaddress++)
            //{
             //   parameterbuffer[uaddress] = 0;
            //}
            Array.Clear(parameterbuffer, 0, 0x400);

            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.

                //////////////
                using (FileStream fsSource = new FileStream(fullpath,
                     FileMode.Open, FileAccess.Read))
                {

                    // Read may return anything from 0 to numBytesToRead.
                    int n = fsSource.Read(parameterbuffer, 0, 0x400);
                }
                //////////////
              //  using (StreamReader sr = new StreamReader(@fullpath))
               // {
      
                 //   sr.Read(charbuffer, 0x00, 0x400);
                    //sr.ReadBlock(charbuffer, 0x00, 0x400);
                //}
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
//            StatusLabel.Content = fullpath;
        }

        internal void SaveFile(string fullpath)
        {
            string line, sid, strval;
			string fullpath_checksum;
            byte checksum = 0;
            int uaddress = 0;
            // calculate firmware checksum first.
            checksum = 0;
            for (uaddress = 0; uaddress < 0x8000; uaddress++)
            {
                checksum += m_bFirmware[uaddress];
            }
			
			//Save checksum at file name
          //  string source = "Many mountains are behind many clouds today.";
            // Remove a substring from the middle of the string.
            string toRemove = ".bin";
            string result = string.Empty;
            int i = fullpath.IndexOf(toRemove);
            if (i >= 0)
            {
                fullpath_checksum = fullpath.Remove(i, toRemove.Length);
            }
            else
            {
                fullpath_checksum = fullpath;
            }

            fullpath_checksum = fullpath_checksum + "_0x" + checksum.ToString("X2");

            fullpath_checksum += toRemove;

            //FileInfo file = new FileInfo(@fullpath);
			FileInfo file = new FileInfo(@fullpath_checksum);
			
            
            // Open the stream for writing. 
            using (FileStream fs = file.OpenWrite())
            {
                // Add some information to the file.
                //fs.Write(m_bFirmware, 0, 0xA000);// from load hex
                fs.Write(m_bFirmware, 0, 0x8000);
               

            }
            /*
            fullpath_checksum = fullpath + "_" + checksum.ToString("X2") + "_40K";

           // fullpath_checksum = fullpath_checksum + "_40KB";
            //FileInfo file = new FileInfo(@fullpath);
            FileInfo file_40KB = new FileInfo(@fullpath_checksum);


            // Open the stream for writing. 
            using (FileStream fs = file_40KB.OpenWrite())
            {
                // Add some information to the file.
                fs.Write(m_bFirmware, 0, 0xA000);// from load hex
                

            }
            */

        }
        private void command()
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
            /*
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            StatusLabel.Content = "Device Content";
            */
            /*
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
            */
            /*
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
            */
            /*
            msg.percent = 30;
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
            */
            // We may update parameter from list and save into flash image.
            /*
            msg.percent = 40;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
             
             */
            /*
            msg.percent = 50;
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
            */
            /*
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

            msg.percent = 90;
            msg.task = TM.TM_BLOCK_MAP_EEPROM;
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
              
            */
            msg.percent = 30;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();


            msg.percent = 50;
            msg.task = TM.TM_COMMAND;
            //msg.sub_task = 0;// Use this to separate the command
            //msg.Flashdata_Length = 0x8000;
            //msg.Flashdata = m_bFirmware;
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

            msg.percent = 100;
            msg.task = TM.TM_CONVERT_HEXTOPHYSICAL;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();


            parent.bBusy = false;
        }


        private void ReadBtn_Click(object sender, RoutedEventArgs e)
        {
 


            msg.owner = this;
            msg.gm.sflname = sflname;
            btnControl btn_ctrl = null;
            switch (sender.GetType().Name)
            {
                case "MenuItem":

                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    msg.gm.controls = "Read One parameter";
                    msg.task_parameterlist = viewmode.dm_part_parameterlist;
                    msg.sub_task = 0x80;
                    break;
                case "Button":
                    msg.gm.controls = ((System.Windows.Controls.Button)sender).Content.ToString();
                    msg.task_parameterlist = viewmode.dm_parameterlist;

                    System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                    btn_ctrl = ui_config.GetBtnControlByName(btn.Name);
                    if (btn_ctrl == null) break;
                    if (btn_ctrl.btn_menu_control.Count == 0) break;

                    btn_ctrl.btn_cm.PlacementTarget = btn;
                    btn_ctrl.btn_cm.IsOpen = true;
                    return;
                default:
                    break;
            }
            command();
            //Read();
        }

        private void Read()
        {

            int itmp=0;
            StatusLabel.Content = "Device Content";
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
/*
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
            msg.task = TM.TM_BLOCK_MAP_EEPROM;
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
*/

            msg.percent = 20;
            msg.percent = 30;
            msg.percent = 40;
            //msg.percent = 90;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = 0x20;// Use this to separate the command

            //changes for 2.00.08
            msg.flashData = m_bFirmware;
            //m_bFirmware[itmp] = msg.Flashdata_read[itmp];
            //msg.Flashdata_Length = 0x8000;
           // msg.Flashdata = m_bFirmware;
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

         //   parent.bBusy = false;
            // changes for 2.00.08
            //for (itmp = 0; itmp < 0x8000; itmp++)
            //    m_bFirmware[itmp] = msg.Flashdata_read[itmp];

            parent.bBusy = false;
            return;
        }

        private void WriteBtn_Click(object sender, RoutedEventArgs e)
        {
            msg.owner = this;
            msg.gm.sflname = sflname;
            btnControl btn_ctrl = null;
            switch (sender.GetType().Name)
            {
                case "MenuItem":

           gm.level = 2;
            gm.message = "So far, there is no function for this button.";
            CallWarningControl(gm);
            parent.bBusy = false;
            return;
                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    msg.gm.controls = "Write One parameter";
                    msg.task_parameterlist = viewmode.dm_part_parameterlist;
                    break;
                case "Button":
                    msg.gm.controls = ((System.Windows.Controls.Button)sender).Content.ToString();
                    msg.task_parameterlist = viewmode.dm_parameterlist;

                    System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                    btn_ctrl = ui_config.GetBtnControlByName(btn.Name);
                    if (btn_ctrl == null) break;
                    if (btn_ctrl.btn_menu_control.Count == 0) break;

                    btn_ctrl.btn_cm.PlacementTarget = btn;
                    btn_ctrl.btn_cm.IsOpen = true;
                    return;
            }
            write();
        }
        private void Erase_mian_flash()
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
            /*
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            StatusLabel.Content = "Device Content";
            */
            /*
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
            */
            /*
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
            */
            /*
            msg.percent = 30;
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
            */
            // We may update parameter from list and save into flash image.
            /*
            msg.percent = 40;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
             
             */
            /*
            msg.percent = 50;
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
            */
            /*
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

            msg.percent = 90;
            msg.task = TM.TM_BLOCK_MAP_EEPROM;
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
              
            */

            msg.percent = 90;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = 0x30;// Use this to separate the command
            // changes for 2.00.08
            //msg.Flashdata_Length = 0x8000;
            msg.flashData = m_bFirmware;
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

        private void write()
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
            /*
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                gm.level = 2;
                gm.message = LibErrorCode.GetErrorDescription(ret);
                CallWarningControl(gm);
                parent.bBusy = false;
                return;
            }
            StatusLabel.Content = "Device Content";
            */
            /*
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
            */
            /*
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
            */
            /*
            msg.percent = 30;
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
            */
            // We may update parameter from list and save into flash image.
            /*
            msg.percent = 40;
            msg.task = TM.TM_CONVERT_PHYSICALTOHEX;
            parent.AccessDevice(ref m_Msg);
            while (msg.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
             
             */ 
            /*
            msg.percent = 50;
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
            */
            /*
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

            msg.percent = 90;
            msg.task = TM.TM_BLOCK_MAP_EEPROM;
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
              
            */


            msg.sub_task = 0;// Use this to separate the command
            //msg.Flashdata_Length = 0x8000;
            msg.flashData = m_bFirmware;
            msg.percent = 30;
            msg.task = TM.TM_COMMAND;
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

        private void EraseBtn_Click(object sender, RoutedEventArgs e)
        {

/////////////////////////////////////////
            msg.owner = this;
            msg.gm.sflname = sflname;
            btnControl btn_ctrl = null;
            switch (sender.GetType().Name)
            {
                case "MenuItem":

            gm.level = 2;
            gm.message = "So far, there is no function for this button.";
            CallWarningControl(gm);
            parent.bBusy = false;
            return;

                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    msg.gm.controls = "Read One parameter";
                    msg.task_parameterlist = viewmode.dm_part_parameterlist;
                    break;
                case "Button":
                    msg.gm.controls = ((System.Windows.Controls.Button)sender).Content.ToString();
                    msg.task_parameterlist = viewmode.dm_parameterlist;

                    System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                    btn_ctrl = ui_config.GetBtnControlByName(btn.Name);
                    if (btn_ctrl == null) break;
                    if (btn_ctrl.btn_menu_control.Count == 0) break;

                    btn_ctrl.btn_cm.PlacementTarget = btn;
                    btn_ctrl.btn_cm.IsOpen = true;
                    return;
                default:
                    break;
            }
//            Read();
            Erase_mian_flash();
            return;
/////////////////////////////////////////



        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            UInt16 udata = 0;
            var mi = sender as System.Windows.Controls.MenuItem;
            var cm = mi.Parent as System.Windows.Controls.ContextMenu;
            System.Windows.Controls.Button btn = cm.PlacementTarget as System.Windows.Controls.Button;
            if (UInt16.TryParse(mi.CommandParameter.ToString(), out udata))
                msg.sub_task = udata;
            else
                msg.sub_task = 0;
            switch (btn.Name)
            {
                case "ReadBtn":
                    if (msg.sub_task == 0x20)
                        Read();
                    else
                    {
                        msg.flashData = buserparameter;
                        command();
                    }
                    break;
                case "WriteBtn":
                    if (msg.sub_task == 0x00)
                        write();
                    else
                    {
                        msg.flashData = buserparameter;

                        command();
                    }
                    break;
                case "EraseBtn":

  // msg.sub_task = 0x20;// Use this to separate the command
    //        msg.Flashdata_Length = 0x8000;
  // ;// Use this to separate the command
                    if (msg.sub_task == 0x30)
                        Erase_mian_flash();
                    else
                        command();
                    //Trimming();
                    break;
                    ///////////////////////////////////
                case "LoadBtn":
                    if(msg.sub_task == 80)
                    {
                        ///////////////////////////////////////
            string fullpath = "";
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Load Firmware File";
            openFileDialog.Filter = "OZ8513 firmware files (*.hex)|*.hex||";
            openFileDialog.FileName = "default";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "hex";
            openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (openFileDialog.ShowDialog() == true)
            {
                if (parent == null) return;
                {
                    fullpath = openFileDialog.FileName;
                    LoadFile(fullpath, ref m_bFirmware);
                }
            }
                        ///////////////////////////////////////
                        ;
                    }
                    else if (msg.sub_task == 81)
                    {
                        ///////////////////////////////////////
                        string fullpath = "";
                        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                        openFileDialog.Title = "Load Parameter File";
                        openFileDialog.Filter = "OZ8513 parameter files (*.bin)|*.bin||";
                        openFileDialog.FileName = "default";
                        openFileDialog.FilterIndex = 1;
                        openFileDialog.RestoreDirectory = true;
                        openFileDialog.DefaultExt = "bin";
                        openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
                        if (openFileDialog.ShowDialog() == true)
                        {
                            if (parent == null) return;
                            {
                                fullpath = openFileDialog.FileName;
                               // LoadParameterFile(string fullpath, ref char[] parameterbuffer)
                                LoadParameterFile(fullpath, ref m_buserparameter);
                            }
                        }
                        ///////////////////////////////////////
                        ;
                    }
                    else if (msg.sub_task == 82)
                    {
                        //Load default setting
                        //LoadParameterFile(fullpath, ref m_buserparameter);
                        LoadParameterDefault(0x400, ref m_buserparameter);
                    }

//////////////////////////////////////
                    break;
                case "SaveBtn":
                    if (msg.sub_task == 90)
                    {
                        string fullpath = "";
                        Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                        saveFileDialog.Title = "Save bin File";
                        saveFileDialog.Filter = "Firmware bin files (*.bin)|*.bin||";
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
                                SaveFile(fullpath);
                            }
                        }
                        StatusLabel.Content = fullpath;
                    }
                    else if (msg.sub_task == 91)
                    {
                        string fullpath = "";
                        Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                        saveFileDialog.Title = "Save bin File";
                        saveFileDialog.Filter = "Parameter bin files (*.bin)|*.bin||";
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
                                ///////////
                                FileInfo file = new FileInfo(@fullpath);

                                // Open the stream for writing. 
                                using (FileStream fs = file.OpenWrite())
                                {
                                 // Add some information to the file.
                                     fs.Write(m_buserparameter, 0, 0x400);// from load hex
                                }
                                ////////////
                            //    SaveFile(fullpath);
                            }
                        }
                    }
                    else if (msg.sub_task == 92)
                    {
                        LoadParameterDefault(0x400, ref m_buserparameter);

                        string fullpath = "";
                        Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                        saveFileDialog.Title = "Save bin File";
                        saveFileDialog.Filter = "Parameter bin files (*.bin)|*.bin||";
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
                                ///////////
                                FileInfo file = new FileInfo(@fullpath);

                                // Open the stream for writing. 
                                using (FileStream fs = file.OpenWrite())
                                {
                                    // Add some information to the file.
                                    fs.Write(m_buserparameter, 0, 0x400);// from load hex
                                }
                                ////////////
                                //    SaveFile(fullpath);
                            }
                        }
                    }//if (msg.sub_task == 92)



                    break;
                    //////////////////////
                    //
 
                    //
                    ////////////////////////////////////
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

        public void CallPasswordControl(ControlMessage msg)
        {
            PasswordPopControl.Dispatcher.Invoke(new Action(() =>
            {
                msg.bcancel = PasswordPopControl.ShowDialog();
                msg.password = PasswordPopControl.password;
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
    }
}