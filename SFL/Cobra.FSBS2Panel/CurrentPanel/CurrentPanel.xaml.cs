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
    /// Interaction logic for CurrentPanel.xaml
    /// </summary>
    public partial class CurrentPanel : GroupBox
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private AsyncObservableCollection<cModel> m_ParameterList = new AsyncObservableCollection<cModel>();
        private AsyncObservableCollection<cModel> m_Tmp_ParameterList = new AsyncObservableCollection<cModel>();//保持不变

        public CurrentPanel()
        {
            InitializeComponent();
            m_ParameterList.Clear();
            m_Tmp_ParameterList.Clear();
        }

        public void Init(AsyncObservableCollection<Model> vlist)
        {
            cModel cm = null;
            foreach (Model model in vlist)
            {
                cm = new cModel();
                cm.pParent = model;
                cm.pTip = model.nickname;
                cm.pMaxValue = model.parent.dbPhyMax;
                cm.pMinValue = model.parent.dbPhyMin;
                cm.pValue = model.data * 1000.0;
                cm.pCharge = true;
                cm.pDischarge = false;
                model.PropertyChanged += new PropertyChangedEventHandler(model_PropertyChanged); 
                if (cm.pParent.bShow)
                    m_ParameterList.Add(cm);
                m_Tmp_ParameterList.Add(cm);
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

        private cModel GetSubMode(Model model)
        {
            foreach (cModel cm in m_Tmp_ParameterList)
            {
                if (cm.pParent.Equals(model))
                    return cm;
            }
            return null;
        }

        private void UpdateParameterList(Model mo)
        {
            cModel cm = GetSubMode(mo);
            if (cm == null) return;
            if (!mo.bShow)
                m_ParameterList.Remove(cm);
            else
            {
                if (m_ParameterList.IndexOf(cm) == -1)
                {/*
                    if (m_ParameterList.Count <= cm.pIndex)
                        m_ParameterList.Add(cm);
                    else
                        m_ParameterList.Insert(cm.pIndex, cm);*/
                    m_ParameterList.Add(cm);
                }
            }
            Dispatcher.Invoke(
             new Action(
                  delegate
                  {
                      m_ParameterList.Sort(x => x.pIndex); 
                      //this.DataContext = m_ParameterList;
                  }
             )
             );
            //m_ParameterList.Sort(x => x.pIndex); 
        }
    }
}
