using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.FSBS2Panel
{
    /// <summary>
    /// Interaction logic for SBSControl.xaml
    /// </summary>
    public partial class SBSControl : UserControl
    {
        public MainControl parent { get; set; }
        public SBSControl()
        {
            InitializeComponent();
        }
 
        public void Init(object pParent)
        {
            parent = (MainControl)pParent;

            ListCollectionView collectionView = new ListCollectionView(parent.viewmode.sfl_dg_parameterlist);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("catalog"));
            collectionView.SortDescriptions.Add(new SortDescription("catalog", ListSortDirection.Ascending));
            collectionView.SortDescriptions.Add(new SortDescription("guid", ListSortDirection.Ascending));

            dynamicDG.ItemsSource = collectionView;
            ePnl.Init(parent.viewmode.sfl_event_parameterlist);
            vPnl.Init(parent.viewmode.sfl_vol_parameterlist);
            tPnl.Init(parent.viewmode.sfl_temp_parameterlist);
            cPnl.Init(parent.viewmode.sfl_cur_parameterlist);
        }

        //UI初始化
        public void Reset()
        {
            foreach (Model mode in parent.viewmode.sfl_dynamic_parameterlist)
            {
                mode.waveControl.Reset();
            }
        }

        //数据更新
        public void update()
        {
            foreach (Model mode in parent.viewmode.sfl_dynamic_parameterlist)
            {
                mode.waveControl.Update(mode.data);
            }
        }

        private void DetailBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Model obj = btn.DataContext as Model;

            switch (obj.subType)
            {
                case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_VOL:
                case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_CUR:
                case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_TEMP:
                    if (!obj.waveControl.IsActive)
                        obj.waveControl.Show();
                    else
                        obj.waveControl.Hide();
                    break;
                case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT:
                    {
                        break;
                    }
                default:
                    break;
            }

        }
        private void WriteBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Model obj = btn.DataContext as Model;

            parent.msg.gm.controls = "Write One parameter";
            parent.msg.task_parameterlist.parameterlist.Clear();
            parent.msg.task_parameterlist.parameterlist.Add(obj.parent);
            parent.write();
        }
    }
}
