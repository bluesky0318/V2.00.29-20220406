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
using System.Windows.Shapes;
using Cobra.Common;

namespace Cobra.FSBS2Panel
{
    /// <summary>
    /// Interaction logic for CoinfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        internal Dictionary<string, string> subTask_Dic = new Dictionary<string, string>();
        //父对象保存
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public ConfigWindow()
        {
            InitializeComponent();
        }

        public ConfigWindow(object pParent)
		{
			this.InitializeComponent();

			// 在此点之下插入创建对象所需的代码。
            parent = (MainControl)pParent;
            mDataGrid.ItemsSource = parent.viewmode.setmodel_list;
		}

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                subTask_Dic.Clear();
                subTask_Dic.Add("SFL", parent.sflname);
                foreach (setModel mod in parent.viewmode.setmodel_list)
                {
                    if (mod == null) continue;
                    switch (mod.editortype)
                    {
                        case 0: //Text
                            break;
                        case 1: //Comboboxs
                            mod.sphydata = mod.itemlist[mod.listindex];
                            mod.phydata = mod.listindex;
                            break;
                        case 2: //Checkbox
                            mod.sphydata = mod.bcheck.ToString();
                            break;
                    }
                    subTask_Dic.Add(mod.nickname, mod.sphydata);
                }
                parent.subTaskJson = SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
                Hide();
                Close();
            }
            catch (System.Exception ex)
            {
                Hide();
                Close();
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            subTask_Dic.Clear();
            Hide();
            Close();
        }
    }
}
