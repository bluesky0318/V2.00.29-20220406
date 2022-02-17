using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Cobra.Common;
using Cobra.ControlLibrary;

namespace Cobra.ProjectPanel.Param
{
    enum editortype
    {
        TextBox_EditType = 0,
        ComboBox_EditType = 1,
        CheckBox_EditType = 2,
        TextBox1_EditType = 3
    }

    /// <summary>
    /// Interaction logic for ParamUserControl.xaml
    /// </summary>
    public partial class ParamUserControl : SubUserControl
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private ProjFile m_projFile = null;
        public ProjFile projFile
        {
            get { return m_projFile; }
            set { m_projFile = value; }
        }

        private SFLViewMode m_viewmode;
        public SFLViewMode viewmode
        {
            get { return m_viewmode; }
            set { m_viewmode = value; }
        }
        private ListCollectionView GroupedCustomers = null;

        public ParamUserControl()
        {
            InitializeComponent();
        }

        public ParamUserControl(object pParent, ref ProjFile pf)
        {
            InitializeComponent();

            #region 相关初始化
            parent = (MainControl)pParent;
            if (parent == null) return;

            projFile = pf;
            if (projFile == null) return;

            viewmode = new SFLViewMode(pParent, this);
            GroupedCustomers = new ListCollectionView(viewmode.sfl_parameterlist);
            GroupedCustomers.GroupDescriptions.Add(new PropertyGroupDescription("catalog"));
            mDataGrid.ItemsSource = GroupedCustomers;
            #endregion
        }

        public void CopyData()
        {
           foreach (SFLModel model in viewmode.sfl_parameterlist)
            {
               model.parent.phydata = model.data;
            }
        }

        /// <summary>
        /// Opens a file.
        /// </summary>
        /// <param name="fileName">the file name of the file to open</param>
        public override void OpenFile(ProjFile pf)
        {
            viewmode.LoadParameterXML(pf.fullName);
        }

        public override void SaveFile(ProjFile pf)
        {
            viewmode.SaveParameterXML(pf.fullName);
        }

        public override void CloseFile()
        {
            viewmode.sfl_parameterlist.Clear();
        }

        #region Read
        private void ReadBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (sender.GetType().Name)
            {
                case "MenuItem":
                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    parent.msg.gm.controls = "Read One parameter";
                    parent.msg.task_parameterlist.parameterlist = viewmode.dm_part_parameterlist.parameterlist;
                    break;
                case "Button":
                    return;
                default:
                    break;
            }
            parent.Read();
        }
        #endregion

        #region Write
        private void WriteBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (sender.GetType().Name)
            {
                case "MenuItem":
                    var cl = sender as System.Windows.Controls.MenuItem;
                    var cm = cl.Parent as System.Windows.Controls.ContextMenu;
                    viewmode.BuildPartParameterList(cm.PlacementTarget.Uid);
                    parent.msg.gm.controls = "Write One parameter";
                    parent.msg.task_parameterlist.parameterlist = viewmode.dm_part_parameterlist.parameterlist;
                    break;
                case "Button":
                    return;
            }
            parent.write();
        }
        #endregion
    }
}
