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
using Cobra.CalibratePanel.SubGroup;

namespace Cobra.CalibratePanel.Group
{
    /// <summary>
    /// Interaction logic for volPanel.xaml
    /// </summary>
    public partial class volPanel : UserControl
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public volPanel(object pParent)
        {
            InitializeComponent();
            parent = (MainControl)pParent;
            init();
        }

        public void init()
        {
            foreach (Model model in parent.viewmode.sfl_vol_parameterlist)
            {
                volCalibControl cctl = new volCalibControl(this, model);
                cctl.DataContext = model;
                volPan.Children.Add(cctl);
            }
        }
    }
}
