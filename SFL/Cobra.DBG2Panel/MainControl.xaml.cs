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
using System.Globalization;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.DBG2Panel
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        #region variable defination
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

        public string subTaskJson = string.Empty;
        public byte nShowMode = 1;
        public Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();
        public GeneralMessage gm = new GeneralMessage("Debug SFL", "", 0);
        private object m_lock = new object();
        #endregion

        public MainControl(object pParent, string name)
        {
            this.InitializeComponent();
            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;
            msg.PropertyChanged += new PropertyChangedEventHandler(msg_PropertyChanged);
            #endregion            
            #region 初始化ShowMode
            XmlElement root = EMExtensionManage.m_extDescrip_xmlDoc.DocumentElement;
            XmlNode xn = root.SelectSingleNode("descendant::Button[@Label = '" + sflname + "']");
            XmlElement xe = (XmlElement)xn;
            if (!Byte.TryParse(xe.GetAttribute("ShowMode").Trim(), out nShowMode))
                nShowMode = 1;
            #endregion
            viewmode = new ViewMode(pParent, this);
            Init();
        }

        #region 通用控件消息响应
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
                                break;
                            }
                        case COMMON_CONTROL.COMMON_CONTROL_SELECT:
                            {
                                break;
                            }
                    }
                    break;
            }
        }

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

        #region Common UI
        private void rdBtn_Click(object sender, RoutedEventArgs e)
        {
            UInt32[] wBuf = null;
            StringBuilder strB = new StringBuilder();
            try
            {
                subTask_Dic.Clear();
                dataTb.Clear();
                subTask_Dic.Add("address", addTb.Text.Trim().ToString());
                subTask_Dic.Add("command", cmdTb.Text.Trim().ToString());
                subTask_Dic.Add("length", lenTb.Text.Trim().ToString());
                subTask_Dic.Add("crc", crcCb.SelectionBoxItem.ToString());
                subTaskJson = SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);

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

                msg.task = TM.TM_SPEICAL_READDEVICE;
                msg.sub_task_json = subTaskJson;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.flashData != null)
                {
                    strB.Clear();
                    switch (nShowMode)
                    {
                        case 4:
                            wBuf = new UInt32[msg.flashData.Length / 4];
                            Buffer.BlockCopy(msg.flashData, 0, wBuf, 0, wBuf.Length * sizeof(UInt32));
                            for (int n = 0; n < wBuf.Length; n++)
                            {
                                strB.Append(string.Format("{0:x8}", wBuf[n]));
                                strB.Append(' ');
                            }
                            break;
                        case 1:
                            for (int n = 0; n < msg.flashData.Length; n++)
                            {
                                strB.Append(string.Format("{0:x2}", msg.flashData[n]));
                                strB.Append(' ');
                            }
                            break;
                    }
                    dataTb.Text = strB.ToString();
                }
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    parent.bBusy = false;
                    CallWarningControl(gm);
                    return;
                }
                parent.bBusy = false;
                return;
            }
            catch (System.Exception ex)
            {
                dataTb.Text = "Please input correct parameters!";
                return;
            }
        }

        private void wrBtn_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder strB = new StringBuilder();
            try
            {
                subTask_Dic.Clear();
                subTask_Dic.Add("address", addTb.Text.Trim().ToString());
                subTask_Dic.Add("command", cmdTb.Text.Trim().ToString());
                subTask_Dic.Add("data", dataTb.Text.Trim().ToString());
                subTask_Dic.Add("length", lenTb.Text.Trim().ToString());
                subTask_Dic.Add("crc", crcCb.SelectionBoxItem.ToString());
                subTaskJson = SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);

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

                msg.task = TM.TM_SPEICAL_WRITEDEVIE;
                msg.sub_task_json = subTaskJson;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    parent.bBusy = false;
                    CallWarningControl(gm);
                    return;
                }
                parent.bBusy = false;
                return;
            }
            catch (System.Exception ex)
            {
                dataTb.Text = "Please input correct parameters!";
                return;
            }
        }
        #endregion

        #region TabCtrl
        public void Init()
        {
            UserControl uc = null;
            string name = String.Empty;
            string subname = string.Empty;
            string header = string.Empty;
            XmlNodeList nodelist = parent.GetUINodeList(sflname);
            if (nodelist == null) return;

            foreach (XmlNode node in nodelist)
            {
                name = node.Name.ToString();
                switch (name)
                {
                    case "UserControls":
                        {
                            foreach (XmlNode sub in node)
                            {
                                if (sub.Attributes["Name"] == null) continue;
                                subname = sub.Attributes["Name"].Value.ToString();

                                if (sub.Attributes["Header"] == null) continue;
                                header = sub.Attributes["Header"].Value.ToString();
                                switch (subname)
                                {
                                    case "MemoryUserControl":
                                        {
                                            uc = new MemoryUserControl();
                                            (uc as MemoryUserControl).Init(this, sub);
                                            var tab = new TabItem { Header = header };
                                            tab.Content = uc;
                                            tabCtrl.Items.Add(tab);
                                            break;
                                        }
                                    case "TimerUserControl":
                                        {
                                            uc = new TimerUserControl();
                                            (uc as TimerUserControl).Init(this, sub);
                                            var tab = new TabItem { Header = header };
                                            tab.Content = uc;
                                            tabCtrl.Items.Add(tab);
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                }
            }
        }
        #endregion
    }
}
