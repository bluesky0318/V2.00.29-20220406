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
using System.Collections;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.IO;
using System.Xml;
using System.Data;
using Cobra.EM;
using Cobra.Common;
using Cobra.ControlLibrary;

namespace Cobra.Trim2Panel
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl
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

        private UIConfig m_UI_Config = new UIConfig();
        public UIConfig ui_config
        {
            get { return m_UI_Config; }
            set { m_UI_Config = value; }
        }

        private GeneralMessage gm = new GeneralMessage("Trim SFL", "", 0);
        #endregion

        public MainControl(object pParent, string name)
        {
            InitializeComponent();

            #region 相关初始化
            parent = (Device)pParent;
            if (parent == null) return;

            sflname = name;
            msg.gm.sflname = sflname;
            if (String.IsNullOrEmpty(sflname)) return;

            InitalUI();
            viewmode = new ViewMode(pParent, this);

            Init();
            #endregion
        }

        #region Init
        private void Init()
        {
            BuildCollect();
            outputDataGrid.ItemsSource = viewmode.output_parameterlist;
            inputDataGrid.ItemsSource = viewmode.input_parameterlist;
        }

        public void InitalUI()
        {
            bool bdata = false;
            string name = String.Empty;
            XmlNodeList nodelist = parent.GetUINodeList(sflname);
            if (nodelist == null) return;

            foreach (XmlNode node in nodelist)
            {
                if (node.Attributes["Name"] == null) continue;
                name = node.Attributes["Name"].Value.ToString();

                if (node.Attributes["TrimTimes"] == null) continue;
                ViewMode.m_total_trims = UInt16.Parse(node.Attributes["TrimTimes"].Value);
                switch (name)
                {
                    case "layout":
                        {
                            foreach (XmlNode sub in node)
                            {
                                if (sub.Attributes["Name"] == null) continue;
                                if (sub.Attributes["IsEnable"] == null) continue;
                                if (sub.Attributes["SubTask"] == null) continue;
                                btnControl btCtrl = new btnControl();
                                btCtrl.btn_name = sub.Attributes["Name"].Value.ToString();
                                if (Boolean.TryParse(sub.Attributes["IsEnable"].Value.ToString(), out bdata))
                                    btCtrl.benable = bdata;
                                else
                                    btCtrl.benable = true;

                                btCtrl.subTask = Convert.ToUInt16(sub.Attributes["SubTask"].Value.ToString(), 16);
                                System.Windows.Controls.Button btn = WorkPanel.FindName(btCtrl.btn_name) as System.Windows.Controls.Button;
                                if (btn != null) btn.DataContext = btCtrl;
                                ui_config.btn_controls.Add(btCtrl);
                            }
                            break;
                        }
                }
            }
        }

        private void BuildCollect()
        {
            InPutModel iModel = null;
            OutPutModel oModel = null;

            viewmode.input_parameterlist.Clear();
            viewmode.output_parameterlist.Clear();
            foreach (Model mo in viewmode.sfl_parameterlist)
            {
                if (mo == null) continue;
                oModel = new OutPutModel();
                oModel.parent = mo;
                oModel.nickname = mo.nickname;
                oModel.order = mo.order;
                oModel.sOffset = string.Format("{0:N4}", 0.0);
                oModel.sSlope = string.Format("0x{0:x2}", 0);
                oModel.sCode = string.Format("0x{0:x2}", 0);
                viewmode.output_parameterlist.Add(oModel);

                iModel = new InPutModel();
                iModel.parent = mo;
                iModel.nickname = mo.nickname;
                iModel.order = mo.order;
                for (int i = 0; i < ViewMode.m_total_trims; i++)
                    iModel.input.Add(String.Empty);
                viewmode.input_parameterlist.Add(iModel);
            }
            for (int i = 0; i < ViewMode.m_total_trims; i++)
            {
                DataGridTemplateColumn column = new DataGridTemplateColumn();
                column.Header = "Input " + i;
                column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                DataTemplate dt = new DataTemplate();
                FrameworkElementFactory fef = new FrameworkElementFactory(typeof(TextBox));
                TextBox tb = new TextBox();
                fef.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                fef.SetValue(TextBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);

                Binding bind = new Binding(string.Format("input[{0}]", i));
                bind.Mode = BindingMode.TwoWay;
                bind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                fef.SetBinding(TextBox.TextProperty, bind);
                dt.VisualTree = fef;
                column.CellTemplate = dt;
                inputDataGrid.Columns.Add(column);
            }
            viewmode.output_parameterlist.OrderBy(i => i.order);
            viewmode.input_parameterlist.OrderBy(i => i.order);
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
        #endregion

        #region Button Operation
        private void loadBtn_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = string.Empty;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog.Title = "Load Configuration File..";
            openFileDialog.Filter = "Log File(*.cfg)|*.cfg";
            openFileDialog.FileName = "trim";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = true;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                if (parent == null) return;
                {
                    fullpath = openFileDialog.FileName;
                    LoadFile(fullpath);
                }
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = string.Empty;
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            saveFileDialog.Title = "Save Configuration File..";
            saveFileDialog.Filter = "Log File(*.cfg)|*.cfg";
            saveFileDialog.FileName = "trim";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                fullpath = saveFileDialog.FileName;
                SaveFile(fullpath);
            }
        }
        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (InPutModel im in viewmode.input_parameterlist)
                im.bChecked = true;
        }

        private void SelectNoneBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (InPutModel im in viewmode.input_parameterlist)
                im.bChecked = false;
        }

        private void startSlopeBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            btnControl btn_ctrl = (btnControl)btn.DataContext;
            if (btn_ctrl == null) return;

            if (ViewMode.m_trim_count == ViewMode.m_total_trims)
                ViewMode.m_trim_count = 0;

            viewmode.BuildInputParameterList();
            try
            {
                startSlopeBtn.Content = string.Format("{0} times,Wait..", ViewMode.m_trim_count + 1);
                startSlopeBtn.IsEnabled = false;

                if (parent.bBusy)
                {
                    gm.level = 1;
                    gm.controls = "Trim button";
                    gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                    gm.bupdate = true;
                    CallWarningControl(gm);
                    startSlopeBtn.Content = "Start Slop";
                    startSlopeBtn.IsEnabled = true;
                    return;
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
                    startSlopeBtn.Content = "Start Slop";
                    startSlopeBtn.IsEnabled = true;
                    return;
                }

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
                    startSlopeBtn.Content = "Start Slop";
                    startSlopeBtn.IsEnabled = true;
                    return;
                }

                msg.task = TM.TM_COMMAND;
                msg.gm.sflname = sflname;
                msg.sub_task = btn_ctrl.subTask;
                msg.task_parameterlist = viewmode.dm_part_parameterlist;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    CallWarningControl(gm);
                    parent.bBusy = false;
                    startSlopeBtn.Content = "Start Slop";
                    startSlopeBtn.IsEnabled = true;
                    return;
                }

                parent.bBusy = false;
                startSlopeBtn.Content = "Start Slop";
                startSlopeBtn.IsEnabled = true;
            }
            catch (SystemException exc)
            {
                MessageBox.Show(exc.Message);
                return;
            }
            ViewMode.m_trim_count++;
        }

        private void startOffsetBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            btnControl btn_ctrl = (btnControl)btn.DataContext;
            if (btn_ctrl == null) return;

            if (ViewMode.m_trim_count == ViewMode.m_total_trims)
                ViewMode.m_trim_count = 0;

            viewmode.BuildInputParameterList();
            try
            {
                startOffsetBtn.Content = string.Format("{0} times,Wait..", ViewMode.m_trim_count + 1);
                startOffsetBtn.IsEnabled = false;

                if (parent.bBusy)
                {
                    gm.level = 1;
                    gm.controls = "Trim button";
                    gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                    gm.bupdate = true;
                    CallWarningControl(gm);
                    startOffsetBtn.Content = "Start Offset";
                    startOffsetBtn.IsEnabled = true;
                    return;
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
                    startOffsetBtn.Content = "Start Offset";
                    startOffsetBtn.IsEnabled = true;
                    return;
                }

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
                    startOffsetBtn.Content = "Start Offset";
                    startOffsetBtn.IsEnabled = true;
                    return;
                }

                msg.task = TM.TM_COMMAND;
                msg.gm.sflname = sflname;
                msg.sub_task = btn_ctrl.subTask;
                msg.task_parameterlist = viewmode.dm_part_parameterlist;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    CallWarningControl(gm);
                    parent.bBusy = false;
                    startOffsetBtn.Content = "Start Offset";
                    startOffsetBtn.IsEnabled = true;
                    return;
                }

                parent.bBusy = false;
                startOffsetBtn.Content = "Start Offset";
                startOffsetBtn.IsEnabled = true;
            }
            catch (SystemException exc)
            {
                MessageBox.Show(exc.Message);
                return;
            }
            ViewMode.m_trim_count++;
        }

        private void countSlopeBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            btnControl btn_ctrl = (btnControl)btn.DataContext;
            if (btn_ctrl == null) return;

            countSlopeBtn.Content = "Wait..";
            countSlopeBtn.IsEnabled = false;
            try
            {
                if (parent.bBusy)
                {
                    gm.level = 1;
                    gm.controls = "Count Slope button";
                    gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                    gm.bupdate = true;
                    CallWarningControl(gm);
                    countSlopeBtn.Content = "Count Slope";
                    countSlopeBtn.IsEnabled = true;
                    return;
                }
                else
                    parent.bBusy = true;

                msg.task = TM.TM_COMMAND;
                msg.gm.sflname = sflname;
                msg.sub_task = btn_ctrl.subTask;
                msg.task_parameterlist = viewmode.dm_parameterlist;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    CallWarningControl(gm);
                    parent.bBusy = false;
                    countSlopeBtn.Content = "Count Slope";
                    countSlopeBtn.IsEnabled = true;
                    return;
                }

                viewmode.ShowSlope();
                parent.bBusy = false;
                countSlopeBtn.Content = "Count Slope";
                countSlopeBtn.IsEnabled = true;
            }
            catch (SystemException exc)
            {
                MessageBox.Show(exc.Message);
                return;
            }
        }

        private void countOffsetBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            btnControl btn_ctrl = (btnControl)btn.DataContext;
            if (btn_ctrl == null) return;

            countOffsetBtn.Content = "Wait..";
            countOffsetBtn.IsEnabled = false;
            try
            {
                if (parent.bBusy)
                {
                    gm.level = 1;
                    gm.controls = "Count Offset button";
                    gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                    gm.bupdate = true;
                    CallWarningControl(gm);
                    countOffsetBtn.Content = "Count Offset";
                    countOffsetBtn.IsEnabled = true;
                    return;
                }
                else
                    parent.bBusy = true;

                msg.task = TM.TM_COMMAND;
                msg.gm.sflname = sflname;
                msg.sub_task = btn_ctrl.subTask;
                msg.task_parameterlist = viewmode.dm_parameterlist;
                parent.AccessDevice(ref m_Msg);
                while (msg.bgworker.IsBusy)
                    System.Windows.Forms.Application.DoEvents();
                if (msg.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    gm.level = 2;
                    gm.message = LibErrorCode.GetErrorDescription(msg.errorcode);
                    CallWarningControl(gm);
                    parent.bBusy = false;
                    countOffsetBtn.Content = "Count Offset";
                    countOffsetBtn.IsEnabled = true;
                    return;
                }

                viewmode.ShowOffset();
                parent.bBusy = false;
                countOffsetBtn.Content = "Count Offset";
                countOffsetBtn.IsEnabled = true;
            }
            catch (SystemException exc)
            {
                MessageBox.Show(exc.Message);
                return;
            }

        }

        private void writeSlopeBtn_Click(object sender, RoutedEventArgs e)
        {
            writeSlopeBtn.Content = "Wait..";
            writeSlopeBtn.IsEnabled = false;
            viewmode.BuildSlopeParameterList();
            try
            {
                if (parent.bBusy)
                {
                    gm.level = 1;
                    gm.controls = "Write Slope button";
                    gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                    gm.bupdate = true;
                    CallWarningControl(gm);
                    writeSlopeBtn.Content = "Write Slope";
                    writeSlopeBtn.IsEnabled = true;
                    return;
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
                    writeSlopeBtn.Content = "Write Slope";
                    writeSlopeBtn.IsEnabled = true;
                    return;
                }

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
                    writeSlopeBtn.Content = "Write Slope";
                    writeSlopeBtn.IsEnabled = true;
                    return;
                }


                msg.task = TM.TM_READ;
                msg.task_parameterlist = viewmode.dm_part_parameterlist;
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
                    writeSlopeBtn.Content = "Write Slope";
                    writeSlopeBtn.IsEnabled = true;
                    return;
                }

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
                writeSlopeBtn.Content = "Write Slope";
                writeSlopeBtn.IsEnabled = true;
            }
            catch (SystemException exc)
            {
                MessageBox.Show(exc.Message);
                return;
            }
        }

        private void writeOffsetBtn_Click(object sender, RoutedEventArgs e)
        {
            writeOffsetBtn.Content = "Wait..";
            writeOffsetBtn.IsEnabled = false;
            viewmode.BuildOffsetParameterList();
            try
            {
                if (parent.bBusy)
                {
                    gm.level = 1;
                    gm.controls = "Write Offset button";
                    gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                    gm.bupdate = true;
                    CallWarningControl(gm);
                    writeOffsetBtn.Content = "Write Offset";
                    writeOffsetBtn.IsEnabled = true;
                    return;
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
                    writeOffsetBtn.Content = "Write Offset";
                    writeOffsetBtn.IsEnabled = true;
                    return;
                }

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
                    writeOffsetBtn.Content = "Write Offset";
                    writeOffsetBtn.IsEnabled = true;
                    return;
                }

                msg.task = TM.TM_READ;
                msg.task_parameterlist = viewmode.dm_part_parameterlist;
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
                    writeOffsetBtn.Content = "Write Offset";
                    writeOffsetBtn.IsEnabled = true;
                    return;
                }

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
                writeOffsetBtn.Content = "Write Offset";
                writeOffsetBtn.IsEnabled = true;
            }
            catch (SystemException exc)
            {
                MessageBox.Show(exc.Message);
                return;
            }
        }

        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            btnControl btn_ctrl = (btnControl)btn.DataContext;
            if (btn_ctrl == null) return;

            viewmode.BuildInputParameterList();
            if (parent.bBusy)
            {
                gm.level = 1;
                gm.controls = "Reset button";
                gm.message = LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY);
                gm.bupdate = true;
                CallWarningControl(gm);
                return;
            }
            else
                parent.bBusy = true;

            msg.task = TM.TM_COMMAND;
            msg.gm.sflname = sflname;
            msg.sub_task = btn_ctrl.subTask;
            msg.task_parameterlist = viewmode.dm_part_parameterlist;
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
            ViewMode.m_trim_count = 0;
            startSlopeBtn.Content = "Start Slope";
            startSlopeBtn.IsEnabled = true;
            startOffsetBtn.Content = "Start Offset";
            startOffsetBtn.IsEnabled = true;
        }
        #endregion

        #region File
        private void SaveFile(string path)
        {
            StringBuilder str = new StringBuilder();
            FileStream file = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(file);
            foreach (InPutModel imo in viewmode.input_parameterlist)
            {
                str.Append(imo.nickname);
                str.Append(":");
                str.Append(string.Format("0x{0:x4}", imo.parent.guid));
                str.Append(":");
                str.Append(string.Join(":", imo.input));
                str.Append("\r\n");
                sw.Write(str.ToString());
                str.Clear();
            }
            sw.Close();
            file.Close();
        }

        private void LoadFile(string path)
        {
            UInt32 guid = 0;
            string[] sArray = null;
            string line = string.Empty;
            InPutModel imo = null;
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        sArray = line.Split(':');
                        guid = Convert.ToUInt32(sArray[1], 16);
                        imo = viewmode.input_parameterlist.First(p => p.parent.guid == guid);
                        if (imo == null) continue;
                        for (int i = 0; i < imo.input.Count; i++)
                            imo.input[i] = sArray[i + 2];
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
        #endregion
    }
}
