using Cobra.Common;
using Cobra.EM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Cobra.BlackBoxPanel.StatisticLog
{
    class ViewModel
    {
        private Device m_device_parent;
        public Device device_parent
        {
            get { return m_device_parent; }
            set { m_device_parent = value; }
        }

        private string m_SFLname;
        public string sflname
        {
            get { return m_SFLname; }
            set { m_SFLname = value; }
        }

        private ParamContainer m_DM_ParameterList = new ParamContainer();
        public ParamContainer dm_parameterlist
        {
            get { return m_DM_ParameterList; }
            set { m_DM_ParameterList = value; }
        }


        private AsyncObservableCollection<Parameter> m_dm_Statistic_ParameterList = new AsyncObservableCollection<Parameter>();
        public AsyncObservableCollection<Parameter> dm_statistic_parameterlist
        {
            get { return m_dm_Statistic_ParameterList; }
            set { m_dm_Statistic_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_parameterlist
        {
            get { return m_SFL_ParameterList; }
            set { m_SFL_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_Count_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> count_parameterlist
        {
            get { return m_Count_ParameterList; }
            set { m_Count_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_Statistic_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> statistic_parameterlist
        {
            get { return m_Statistic_ParameterList; }
            set { m_Statistic_ParameterList = value; }
        }

        public ViewModel(object pParent, string name)
        {
            #region 相关初始化
            device_parent = (Device)pParent;
            if (device_parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;
            #endregion

            dm_parameterlist = device_parent.GetParamLists(sflname);
            foreach (Parameter param in dm_parameterlist.parameterlist)
            {
                if (param == null) continue;
                InitSFLParameter(param);
            }

            UpdateSFLParameter();

            foreach (Model mode in sfl_parameterlist)
            {
                if (mode == null) continue;
                try
                {
                    phyTostr(mode);
                }
                catch (Exception e)
                {
                }
            }
        }

        #region 参数操作
        private void InitSFLParameter(Parameter param)
        {
            UInt16 udata = 0;
            UInt32 guid = 0;
            Double ddata = 0.0;

            if (!param.sfllist[sflname].nodetable.ContainsKey("Catalog")) return;
            if (!UInt16.TryParse(param.sfllist[sflname].nodetable["Catalog"].ToString(), out udata)) return;
            if ((udata == (UInt16)LOG_TYPE.EVENT_LOG) | (udata == (UInt16)LOG_TYPE.EVENT2_LOG)) return;
            
            Model model = new Model();
            model.catalog = udata;
            model.parent = param.sfllist[sflname].parent;
            model.guid = param.guid;
            model.sphydata = string.Empty;
            foreach (DictionaryEntry de in param.sfllist[sflname].nodetable)
            {
                switch (de.Key.ToString())
                {
                    case "NickName":
                        model.nickname = de.Value.ToString();
                        break;
                    case "Format":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.format = 0;
                            else
                                model.format = udata;
                            break;
                        }
                    case "DefValue":
                        {
                            if (!Double.TryParse(de.Value.ToString(), out ddata))
                                model.data = 0.0;
                            else
                                model.data = Convert.ToDouble(de.Value.ToString());
                            break;
                        }
                    case "Location":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.location = 0;
                            else
                                model.location = udata;
                            break;
                        }
                    case "Upward":
                        {
                            model.upguid = Convert.ToUInt32(de.Value.ToString(), 16);
                            break;
                        }
                    default:
                        break;
                }
            }
            param.PropertyChanged += new PropertyChangedEventHandler(Parameter_PropertyChanged);
            model.PropertyChanged += new PropertyChangedEventHandler(SFL_Parameter_PropertyChanged);
            sfl_parameterlist.Add(model);
        }

        private void UpdateSFLParameter()
        {
            foreach (Model model in sfl_parameterlist)
            {
                if (model == null) continue;
                switch ((LOG_TYPE)model.catalog)
                {
                    case LOG_TYPE.COUNT_LOG:
                        count_parameterlist.Add(model);
                        m_dm_Statistic_ParameterList.Add(model.parent);
                        break;
                    case LOG_TYPE.STATISTIC_LOG:
                        statistic_parameterlist.Add(model);
                        break;
                    case LOG_TYPE.MAXMIN_LOG:
                        {
                            m_dm_Statistic_ParameterList.Add(model.parent);
                            model.upParam = GetParameterByGuid(model.upguid);
                            switch (model.location)
                            {
                                case 0: //Low
                                    model.upParam.minParam = model;
                                    model.upParam.minParam.PropertyChanged += new PropertyChangedEventHandler(minParam_PropertyChanged);
                                    break;
                                case 1: //High
                                    model.upParam.maxParam = model;
                                    model.upParam.maxParam.PropertyChanged += new PropertyChangedEventHandler(maxParam_PropertyChanged);
                                    break;
                            }
                        }
                        break;
                }
            }
        }

        void maxParam_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model p = (Model)sender;
            Model pParent = p.upParam;
            switch (e.PropertyName.ToString())
            {
                case "sphydata":
                    {
                        if (pParent != null)
                            pParent.maxData = p.sphydata;
                        break;
                    }
                default:
                    break;
            }
        }

        void minParam_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model p = (Model)sender;
            Model pParent = p.upParam;
            switch (e.PropertyName.ToString())
            {
                case "sphydata":
                    {
                        if (pParent != null)
                            pParent.minData = p.sphydata;
                        break;
                    }
                default:
                    break;
            }
        }

        internal void phyTostr(Model p)
        {
            UInt16 hour = 0, minutes = 0, second = 0;
            string tmp = "";
            if (p == null) return;
            switch (p.format)
            {
                case 0: //Int
                    tmp = String.Format("{0:D}", (Int32)p.data);
                    break;
                case 1: //float1
                    tmp = String.Format("{0:F1}", p.data);
                    break;
                case 2: //float2
                    tmp = String.Format("{0:F2}", p.data);
                    break;
                case 3: //float3
                    tmp = String.Format("{0:F3}", p.data);
                    break;
                case 4: //float4
                    tmp = String.Format("{0:F4}", p.data);
                    break;
                case 5: //Hex
                    tmp = String.Format("0x{0:X2}", (byte)p.data);
                    break;
                case 6: //Word
                    tmp = String.Format("0x{0:X4}", (UInt16)p.data);
                    break;
                case 7: //DWord
                    tmp = String.Format("0x{0:X8}", (UInt32)p.data);
                    break;
                case 8: //Date
                    hour = (UInt16)(p.data / 3600);
                    minutes = (UInt16)((p.data - hour * 3600) / 60);
                    second = (UInt16)(p.data - hour * 3600 - minutes * 60);
                    tmp = String.Format("{0:D2}H-{1:D2}M-{2:D2}S", hour, minutes, second);//SharedFormula.UInt32ToData((UInt16)p.data);
                    break;
                case 9: //String
                    tmp = p.sphydata;
                    break;
                default:
                    tmp = String.Format("{0}", p.data);
                    break;
            }
            p.sphydata = tmp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SFL_Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model p = (Model)sender;
            switch (e.PropertyName.ToString())
            {
                case "data":
                    {
                        phyTostr(p);
                        break;
                    }
                default:
                    break;
            }
        }

        public void Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter p = (Parameter)sender;
            Model model = GetParameterByGuid(p.guid);
            if (model == null) return;

            switch (e.PropertyName.ToString())
            {
                case "phydata":
                    {
                        model.data = p.phydata;
                        model.errorcode = p.errorcode;
                        break;
                    }
                case "sphydata":
                    {
                        model.sphydata = p.sphydata;
                        model.errorcode = p.errorcode;
                        break;
                    }
                default:
                    break;
            }
        }

        public Model GetParameterByGuid(UInt32 guid)
        {
            foreach (Model param in sfl_parameterlist)
            {
                if (param.guid.Equals(guid))
                    return param;
            }
            return null;
        }

        public Model GetParameterByName(string name)
        {
            foreach (Model param in sfl_parameterlist)
            {
                if (param.nickname.Equals(name))
                    return param;
            }
            return null;
        }
        #endregion
    }
}
