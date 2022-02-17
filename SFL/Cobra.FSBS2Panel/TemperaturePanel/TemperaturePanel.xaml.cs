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
    public partial class TemperaturePanel : GroupBox
    {
        private MainControl m_parent;
        public MainControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private AsyncObservableCollection<tModel> m_ParameterList = new AsyncObservableCollection<tModel>();//动态变化
        private AsyncObservableCollection<tModel> m_Tmp_ParameterList = new AsyncObservableCollection<tModel>();//保持不变
        public TemperaturePanel()
        {
            InitializeComponent();
        }

        public void Init(AsyncObservableCollection<Model> vlist)
        {
            tModel tm = null;
            foreach (Model model in vlist)
            {
                tm = new tModel();
                tm.pParent = model;
                tm.pIndex = model.index;
                tm.pLabel = model.nickname;
                tm.pMaxValue = model.parent.dbPhyMax;
                tm.pMinValue = model.parent.dbPhyMin;
                tm.pTip = model.nickname;
                tm.pValue = model.data;
                model.PropertyChanged += new PropertyChangedEventHandler(model_PropertyChanged);
                if (tm.pParent.bShow)
                    m_ParameterList.Add(tm);
                m_Tmp_ParameterList.Add(tm);
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

        private tModel GetSubMode(Model model)
        {
            foreach (tModel tm in m_Tmp_ParameterList)
            {
                if (tm.pParent.Equals(model))
                    return tm;
            }
            return null;
        }

        private void UpdateParameterList(Model mo)
        {
            tModel tm = GetSubMode(mo);
            if (tm == null) return;
            if (!mo.bShow)
                m_ParameterList.Remove(tm);
            else
            {/*
                if (m_ParameterList.IndexOf(tm) == -1)
                {
                    if (m_ParameterList.Count < tm.pIndex)
                        m_ParameterList.Add(tm);
                    else
                        m_ParameterList.Insert(tm.pIndex, tm);
                }*/
                if (m_ParameterList.IndexOf(tm) == -1)
                    m_ParameterList.Add(tm);

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
            //m_ParameterList.Sort(x => x.pIndex);
        }
    }
}
