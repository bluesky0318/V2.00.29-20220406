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

namespace Cobra.BlackBoxPanel.StatisticLog
{
    /// <summary>
    /// StatisticLogUC.xaml 的交互逻辑
    /// </summary>
    public partial class StatisticLogUC : UserControl
    {
        private ViewModel m_viewmodel;
        public StatisticLogUC()
        {
            InitializeComponent();
        }
        public void init(object pParent, string name)
        {
            m_viewmodel = new ViewModel(pParent, name);
            if (m_viewmodel.count_parameterlist.Count == 0) counterDataGrid.Visibility = Visibility.Collapsed;
            if (m_viewmodel.statistic_parameterlist.Count == 0) maxminDataGrid.Visibility = Visibility.Collapsed;
            counterDataGrid.ItemsSource = m_viewmodel.count_parameterlist;
            maxminDataGrid.ItemsSource = m_viewmodel.statistic_parameterlist;
        }
    }
}
