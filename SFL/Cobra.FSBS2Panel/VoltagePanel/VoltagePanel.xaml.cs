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
using System.Windows.Threading;
using Cobra.Common;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.EM;

namespace Cobra.FSBS2Panel
{
    /// <summary>
    /// Interaction logic for VoltagePanel.xaml
    /// </summary>
    public partial class VoltagePanel : GroupBox
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private AsyncObservableCollection<vModel> m_ParameterList = new AsyncObservableCollection<vModel>();//动态变化
        private AsyncObservableCollection<vModel> m_Tmp_ParameterList = new AsyncObservableCollection<vModel>();//保持不变
        public VoltagePanel()
        {
            InitializeComponent();
            m_ParameterList.Clear();
            m_Tmp_ParameterList.Clear();
        }

        public void Init(AsyncObservableCollection<Model> vlist)
        {
            vModel vm = null;
            foreach (Model model in vlist)
            {
                vm = new vModel();
                vm.pParent = model;
                vm.pIndex = model.index;
                vm.pMaxValue = model.parent.dbPhyMax;
                vm.pMinValue = model.parent.dbPhyMin;
                vm.pTip = model.nickname;
                vm.pValue = model.data;
                model.PropertyChanged += new PropertyChangedEventHandler(model_PropertyChanged);
                if (vm.pParent.bShow)
                    m_ParameterList.Add(vm);
                m_Tmp_ParameterList.Add(vm);
            }
            m_ParameterList.Sort(x => x.pIndex);
            this.DataContext = m_ParameterList;
        }

        private void model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model model = (Model)sender;
            switch (e.PropertyName.ToString())
            {
                case "data":
                    GetSubMode(model).pValue = model.data;
                    break;
                case "bShow":
                    UpdateParameterList(model);
                    break;
                default:
                    break;
            }
        }

        private vModel GetSubMode(Model model)
        {
            foreach (vModel vm in m_Tmp_ParameterList)
            {
                if (vm.pParent.Equals(model))
                    return vm;
            }
            return null;
        }

        private void UpdateParameterList(Model mo)
        {
            vModel vm = GetSubMode(mo);
            if (vm == null) return;
            if (!mo.bShow)
            {
                if (m_ParameterList.IndexOf(vm) != -1)
                    m_ParameterList.Remove(vm);
            }
            else
            {/*
                if (m_ParameterList.IndexOf(vm) == -1)
                {
                    if (m_ParameterList.Count < vm.pIndex)
                        m_ParameterList.Add(vm);
                    else
                        m_ParameterList.Insert(vm.pIndex - 1, vm);
                }*/
                if (m_ParameterList.IndexOf(vm) == -1)
                    m_ParameterList.Add(vm);
            }
            //m_ParameterList.Sort(x => x.pIndex);           
            Dispatcher.Invoke(
             new Action(
                  delegate
                  {
                      m_ParameterList.Sort(x => x.pIndex);
                      //this.DataContext = m_ParameterList;
                  }
             )
             );
        }
    }
}
