using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.UFPSBSPanel
{
    public class ViewMode
    {
        //父对象保存
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

        private AsyncObservableCollection<Model> m_SFL_Dynamic_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_dynamic_parameterlist
        {
            get { return m_SFL_Dynamic_ParameterList; }
            set { m_SFL_Dynamic_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Static_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_static_parameterlist
        {
            get { return m_SFL_Static_ParameterList; }
            set { m_SFL_Static_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Event_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_event_parameterlist
        {
            get { return m_SFL_Event_ParameterList; }
            set { m_SFL_Event_ParameterList = value; }
        }

        private ParamContainer m_DM_ParameterList = new ParamContainer();
        public ParamContainer dm_parameterlist
        {
            get { return m_DM_ParameterList; }
            set { m_DM_ParameterList = value; }
        }

        private ParamContainer m_WR_DM_ParameterList = new ParamContainer();
        public ParamContainer wr_dm_parameterlist
        {
            get { return m_WR_DM_ParameterList; }
            set { m_WR_DM_ParameterList = value; }
        }

        private ParamContainer m_RD_DM_ParameterList = new ParamContainer();
        public ParamContainer rd_dm_parameterlist
        {
            get { return m_RD_DM_ParameterList; }
            set { m_RD_DM_ParameterList = value; }
        }

        private ParamContainer m_RD_One_DM_ParameterList = new ParamContainer();
        public ParamContainer rd_one_dm_parameterlist
        {
            get { return m_RD_One_DM_ParameterList; }
            set { m_RD_One_DM_ParameterList = value; }
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
            UInt16 udata = 0;
            Model model = new Model();
            model.parent = param.sfllist[sflname].parent;
            model.guid = param.guid;
            model.sphydata = string.Empty;
            model.bShow = param.bShow;
            model.listindex = 0;
            model.itemlist = param.itemlist;
            model.showMode = ElementDefine.SBS_PARAM_SHOWMODE.PARAM_DEFAULT;
            //model.order = string.Format("{0:x2}", (param.guid & ElementDefine.CommandMask) >> 8);

            foreach (DictionaryEntry de in param.sfllist[sflname].nodetable)
            {
                switch (de.Key.ToString())
                {
                    case "NickName":
                        model.nickname = de.Value.ToString();
                        break;
                    case "LogName":
                        model.logname = de.Value.ToString();
                        break;
                    case "SubType":
                        model.subType = (ElementDefine.SBS_PARAM_SUBTYPE)UInt16.Parse(de.Value.ToString());
                        switch (model.subType)
                        {
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_DYNAMIC:
                                rd_dm_parameterlist.parameterlist.Add(model.parent);
                                sfl_dynamic_parameterlist.Add(model);
                                model.order = string.Format("{0:d}", sfl_dynamic_parameterlist.Count);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_STATIC:
                                rd_one_dm_parameterlist.parameterlist.Add(model.parent);
                                sfl_static_parameterlist.Add(model);
                                model.order = string.Format("{0:d}", sfl_static_parameterlist.Count);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT:
                                rd_dm_parameterlist.parameterlist.Add(model.parent);
                                sfl_event_parameterlist.Add(model);
                                model.order = string.Format("{0:d}", sfl_event_parameterlist.Count);
                                break;
                        }
                        break;
                    case "EditType":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.editortype = 0;
                            else
                                model.editortype = udata;
                            break;
                        }
                    case "ShowMode":
                        model.showMode = (ElementDefine.SBS_PARAM_SHOWMODE)UInt16.Parse(de.Value.ToString());
                        break;
                    default:
                        break;
                }
            }
            param.PropertyChanged += new PropertyChangedEventHandler(Parameter_PropertyChanged);
            model.waveControl = new WaveControl(model.nickname);
            sfl_parameterlist.Add(model);
        }

        public void Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter p = (Parameter)sender;
            Model model = GetParameterByGuid(p.guid);
            if (model == null) return;
            switch (e.PropertyName.ToString())
            {
                case "sphydata":
                    {
                        model.sphydata = p.sphydata;
                        switch (p.subtype)
                        {
                            case 2: //PARAM_BYTE
                            case 3: //PARAM_WORD
                                model.data = (double)Convert.ToUInt32(model.sphydata, 16);
                                break;
                            case 8: //PARAM_DATA
                            case 9: //PARAM_STRING
                                break;
                            default:
                                model.data = Convert.ToDouble(model.sphydata);
                                break;
                        }
                        break;
                    }
                case "phydata":
                    {
                        switch (p.subtype)
                        {
                            case 3: //PARAM_WORD
                                model.data = p.phydata;
                                model.sphydata = String.Format("0x{0:X4}", (UInt16)p.phydata);
                                break;
                            case 1: //Combobox
                                model.listindex = (byte)p.phydata;
                                model.sphydata = p.phydata.ToString();
                                break;
                            default:
                                model.data = p.phydata;
                                model.sphydata = p.phydata.ToString();
                                break;
                        }
                        break;
                    }
                case "bShow":
                    {
                        model.bShow = p.bShow;
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

        public Model GetParameterByColumName(string nick)
        {
            foreach (Model param in sfl_parameterlist)
            {
                if (string.Compare(param.nickname, nick, true) == 0)
                    return param;
            }
            return null;
        }

        private string GetHashTableValueByKey(string str, Hashtable htable)
        {
            if (htable.ContainsKey(str))
                return htable[str].ToString();
            else
                return "NoSuchKey";
        }
        #endregion
    }
}
