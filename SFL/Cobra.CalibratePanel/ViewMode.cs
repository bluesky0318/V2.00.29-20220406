using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.CalibratePanel
{
    public class ViewMode
    {//父对象保存
        private MainControl m_control_parent;
        public MainControl control_parent
        {
            get { return m_control_parent; }
            set { m_control_parent = value; }
        }

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

        private AsyncObservableCollection<Model> m_SFL_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_parameterlist
        {
            get { return m_SFL_ParameterList; }
            set { m_SFL_ParameterList = value; }
        }

        public ObservableCollection<Model> sfl_vol_parameterlist = new ObservableCollection<Model>();
        public ObservableCollection<Model> sfl_cur_parameterlist = new ObservableCollection<Model>();
        public ObservableCollection<Model> sfl_temp_parameterlist = new ObservableCollection<Model>();
        public ObservableCollection<Model> sfl_misc_parameterlist = new ObservableCollection<Model>();

        public AsyncObservableCollection<Parameter> vol_parameterlist = new AsyncObservableCollection<Parameter>();
        public AsyncObservableCollection<Parameter> cur_parameterlist = new AsyncObservableCollection<Parameter>();
        public AsyncObservableCollection<Parameter> temp_parameterlist = new AsyncObservableCollection<Parameter>();
        public AsyncObservableCollection<Parameter> misc_parameterlist = new AsyncObservableCollection<Parameter>();

        private ParamContainer m_DM_ParameterList = new ParamContainer();
        public ParamContainer dm_parameterlist
        {
            get { return m_DM_ParameterList; }
            set { m_DM_ParameterList = value; }
        }

        public ViewMode(object pParent, object parent)
        {
            #region 相关初始化
            device_parent = (Device)pParent;
            if (device_parent == null) return;

            control_parent = (MainControl)parent;
            if (control_parent == null) return;

            sflname = control_parent.sflname;
            if (String.IsNullOrEmpty(sflname)) return;
            #endregion

            dm_parameterlist = device_parent.GetParamLists(sflname);
            foreach (Parameter param in dm_parameterlist.parameterlist)
            {
                if (param == null) continue;
                InitSFLParameter(param);
            }
        }

        #region 参数操作
        private void InitSFLParameter(Parameter param)
        {
            UInt32 wdata = 0;
            UInt16 udata = 0;
            Double ddata = 0.0;
            Model model = new Model();

            model.parent = param.sfllist[sflname].parent;
            model.guid = param.guid;

            foreach (DictionaryEntry de in param.sfllist[sflname].nodetable)
            {
                switch (de.Key.ToString())
                {
                    case "NickName":
                        model.nickname = de.Value.ToString();
                        break;
                    case "Name":
                        model.name = de.Value.ToString();
                        break;
                    case "DefValue":
                        {
                            if (!Double.TryParse(de.Value.ToString(), out ddata))
                                model.data = 0.0;
                            else
                                model.data = Convert.ToDouble(de.Value.ToString());
                            break;
                        }
                    case "Type":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.type = 0;
                            else
                                model.type = udata;
                            break;
                        }
                    case "PhyMin":
                        {
                            if (!Double.TryParse(de.Value.ToString(), out ddata))
                                model.data = 0.0;
                            else
                                model.dbPhyMin = Convert.ToDouble(de.Value.ToString());
                            break;
                        }
                    case "PhyMax":
                        {
                            if (!Double.TryParse(de.Value.ToString(), out ddata))
                                model.data = 0.0;
                            else
                                model.dbPhyMax = Convert.ToDouble(de.Value.ToString());
                            break;
                        }
                    default:
                        break;
                }
            }

            switch (model.type)
            {
                case 0: //current
                    cur_parameterlist.Add(param);
                    sfl_cur_parameterlist.Add(model);
                    break;
                case 1: //voltage
                    vol_parameterlist.Add(param);
                    sfl_vol_parameterlist.Add(model);
                    break;
                case 2:
                    temp_parameterlist.Add(param);
                    sfl_temp_parameterlist.Add(model);
                    break;
                case 3: //misc
                    misc_parameterlist.Add(param);
                    sfl_misc_parameterlist.Add(model);
                    break;
            }
            sfl_parameterlist.Add(model);
        }

        public Parameter GetParameterByGuid(UInt32 guid)
        {
            foreach (Parameter param in dm_parameterlist.parameterlist)
            {
                if (param.guid.Equals(guid))
                    return param;
            }
            return null;
        }
        #endregion
    }
}
