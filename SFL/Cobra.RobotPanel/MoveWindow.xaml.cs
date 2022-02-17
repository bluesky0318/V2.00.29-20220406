using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cobra.RobotPanel
{
    /// <summary>
    /// MoveWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MoveWindow : Window
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private ObservableCollection<int> m_MoveIndex_Collection = new ObservableCollection<int>();
        public ObservableCollection<int> moveIndex_Collection
        {
            get { return m_MoveIndex_Collection; }
            set
            {
                m_MoveIndex_Collection = value;
            }
        }

        private void MoveWindow_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < parent.viewmode.robot_commands.Count; i++)
                moveIndex_Collection.Add(i);
            moveCB.ItemsSource = moveIndex_Collection;
            moveCB.SelectedIndex = 0;
        }

        public MoveWindow(object pParent)
        {
            this.InitializeComponent();
            parent = (MainControl)pParent;
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            parent.m_moveToIndex = moveCB.SelectedIndex;
            this.DialogResult = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Hide();
            Close();
        }
    }
}
