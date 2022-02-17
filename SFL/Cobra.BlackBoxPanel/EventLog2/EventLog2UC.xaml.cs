using System;
using System.Collections.Generic;
using System.Data;
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

namespace Cobra.BlackBoxPanel.EventLog2
{
    /// <summary>
    /// EventLog2UC.xaml 的交互逻辑
    /// </summary>
    public partial class EventLog2UC : UserControl
    {
        private ViewModel m_viewmodel;
        private DataTable m_EventDT = new DataTable();
        public DataTable eventDt
        {
            get { return m_EventDT; }
            set { m_EventDT = value; }
        }
        public EventLog2UC()
        {
            InitializeComponent();
        }

        public void init(object pParent, string name)
        {
            m_viewmodel = new ViewModel(pParent, name);
            if (m_viewmodel.event_parameterlist.Count == 0)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }
            DataColumn col;
            eventDt.Clear();
            eventDt.Columns.Clear();
            foreach (Model mo in m_viewmodel.event_parameterlist)
            {
                col = new DataColumn();
                col.DataType = System.Type.GetType("System.String");
                col.ColumnName = mo.nickname;
                col.AutoIncrement = false;
                col.ReadOnly = false;
                col.Unique = false;
                eventDt.Columns.Add(col);
            }
            eventDataGrid.ItemsSource = eventDt.DefaultView;
            eventDataGrid.GridLinesVisibility = DataGridGridLinesVisibility.None;
        }
    }
}
