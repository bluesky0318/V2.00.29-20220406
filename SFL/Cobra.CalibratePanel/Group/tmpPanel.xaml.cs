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
    /// Interaction logic for tmpPanel.xaml
    /// </summary>
    public partial class tmpPanel : UserControl
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public tmpPanel(object pParent)
        {
            InitializeComponent();
            parent = (MainControl)pParent;
            init();
        }

        public void init()
        {
            foreach (Model model in parent.viewmode.sfl_temp_parameterlist)
            {
                tmpCalibControl cctl = new tmpCalibControl(this, model);
                cctl.DataContext = model;
                extmpPan.Children.Add(cctl);
            }
        }
    }
}
