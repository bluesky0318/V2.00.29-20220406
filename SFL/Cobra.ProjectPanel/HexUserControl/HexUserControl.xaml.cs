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
using Cobra.Common;

namespace Cobra.ProjectPanel.Hex
{
    /// <summary>
    /// Interaction logic for HexUserControl.xaml
    /// </summary>
    public partial class HexUserControl : SubUserControl
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

        public HexUserControl()
        {
            InitializeComponent();
        }

        public HexUserControl(object pParent, ref ProjFile pf)
        {
            InitializeComponent();

            #region 相关初始化
            parent = (MainControl)pParent;
            if (parent == null) return;

            projFile = pf;
            if (projFile == null) return;

            viewmode = new SFLViewMode(pParent, this);
            mDataGrid.ItemsSource = viewmode.sfl_parameterlist;
            #endregion
        }

        /// <summary>
        /// Opens a file.
        /// </summary>
        /// <param name="fileName">the file name of the file to open</param>
        public override void OpenFile(ProjFile pf)
        {
        }

        public override void SaveFile(ProjFile pf)
        {
        }

        public override void CloseFile()
        {
        }
    }
}
