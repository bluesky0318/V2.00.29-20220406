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
using Cobra.Common;
using System.ComponentModel;

namespace Cobra.FSBS2Panel
{
    /// <summary>
    /// Interaction logic for EventPanel.xaml
    /// </summary>
    public partial class EventPanel : UserControl
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public EventPanel()
        {
            InitializeComponent();
        }

        public void Init(AsyncObservableCollection<Model> elist)
        {
            eventLB.DataContext = elist;
        }
    }
}
