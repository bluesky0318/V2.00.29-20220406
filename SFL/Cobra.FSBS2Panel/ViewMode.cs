using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.FSBS2Panel
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

        #region UI显示数据集合
        #region SCAN配置参数列表
        private AsyncObservableCollection<setModel> m_setmodel_list = new AsyncObservableCollection<setModel>();
        public AsyncObservableCollection<setModel> setmodel_list
        {
            get { return m_setmodel_list; }
            set { m_setmodel_list = value; }
        }
        #endregion

        #region DG形式展现
        private AsyncObservableCollection<Model> m_SFL_DG_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_dg_parameterlist //展现除envent bit其他全部参数
        {
            get { return m_SFL_DG_ParameterList; }
            set { m_SFL_DG_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Dynamic_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_dynamic_parameterlist  //DG分组展现电压，电流，温度以及其他项目
        {
            get { return m_SFL_Dynamic_ParameterList; }
            set { m_SFL_Dynamic_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Static_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_static_parameterlist  //DG分组展现静态项目
        {
            get { return m_SFL_Static_ParameterList; }
            set { m_SFL_Static_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Event_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_event_parameterlist  //DG分组展现事件项目
        {
            get { return m_SFL_Event_ParameterList; }
            set { m_SFL_Event_ParameterList = value; }
        }
        private AsyncObservableCollection<Model> m_SFL_WR_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_wr_parameterlist  //DG分组展现事件项目
        {
            get { return m_SFL_WR_ParameterList; }
            set { m_SFL_WR_ParameterList = value; }
        }
        #endregion

        #region 图形式UI呈现
        private AsyncObservableCollection<Model> m_SFL_Vol_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_vol_parameterlist  //电压
        {
            get { return m_SFL_Vol_ParameterList; }
            set { m_SFL_Vol_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Cur_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_cur_parameterlist  //电流
        {
            get { return m_SFL_Cur_ParameterList; }
            set { m_SFL_Cur_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Temp_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_temp_parameterlist //温度
        {
            get { return m_SFL_Temp_ParameterList; }
            set { m_SFL_Temp_ParameterList = value; }
        }
        #endregion
        #endregion

        #region SFL参数包括整体参数，log参数，scan参数列表
        private AsyncObservableCollection<Model> m_SFL_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_parameterlist
        {
            get { return m_SFL_ParameterList; }
            set { m_SFL_ParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_SFL_Log_ParameterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> sfl_log_parameterlist
        {
            get { return m_SFL_Log_ParameterList; }
            set { m_SFL_Log_ParameterList = value; }
        }

        private ParamContainer m_Scan_ParameterList = new ParamContainer();
        public ParamContainer scan_parameterlist
        {
            get { return m_Scan_ParameterList; }
            set { m_Scan_ParameterList = value; }
        }
        #endregion

        #region DM参数列表
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
        #endregion

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
            UpdateSFLParameter();
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
            model.data = model.parent.phydata;
            model.itemlist = param.itemlist;
            model.order = string.Format("0x{0:x2}", (param.guid & ElementDefine.CommandMask) >> 8);
            model.nickname = GetHashTableValueByKey("NickName", param.sfllist[sflname].nodetable);
            model.logname = GetHashTableValueByKey("LogName", param.sfllist[sflname].nodetable);

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
                    case "Index":
                        model.index = UInt16.Parse(de.Value.ToString());
                        break;
                    case "Catalog":
                        model.catalog = de.Value.ToString();
                        break;
                    case "SubType":
                        model.subType = (ElementDefine.SBS_PARAM_SUBTYPE)UInt16.Parse(de.Value.ToString());
                        switch (model.subType)
                        {   //Dynamic,Event,Vol,Cur,Tmp ----Log
                            //Dynamic,Event,EventBit,Vol,Cur,Tmp ---Scan
                            //Dynamic,Event,Vol,Cur,Tmp,static ---dg Show
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_DYNAMIC:
                                model.waveControl = new WaveControl(model.nickname);
                                sfl_dynamic_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_STATIC:
                                sfl_static_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_WR:
                                model.bWrite = false;
                                model.bEnable = true;
                                sfl_wr_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT_WR:
                                model.bWrite = false;
                                model.bEnable = true;
                                sfl_event_parameterlist.Add(model);
                                sfl_wr_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT:
                                model.bEnable = true;
                                sfl_event_parameterlist.Add(model);
                                //sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_EVENT_BIT:
                                scan_parameterlist.parameterlist.Add(model.parent);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_VOL:
                                model.bEnable = true;
                                model.waveControl = new WaveControl(model.nickname);
                                sfl_vol_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                sfl_dynamic_parameterlist.Add(model);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_CUR:
                                model.bEnable = true;
                                model.waveControl = new WaveControl(model.nickname);
                                sfl_cur_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                sfl_dynamic_parameterlist.Add(model);
                                break;
                            case ElementDefine.SBS_PARAM_SUBTYPE.PARAM_TEMP:
                                model.bEnable = true;
                                model.waveControl = new WaveControl(model.nickname);
                                sfl_temp_parameterlist.Add(model);
                                sfl_dg_parameterlist.Add(model);

                                sfl_log_parameterlist.Add(model);
                                scan_parameterlist.parameterlist.Add(model.parent);
                                sfl_dynamic_parameterlist.Add(model);
                                break;
                        }
                        break;
                    case "Relations":
                        {
                            AsyncObservableCollection<string> list = (AsyncObservableCollection<string>)de.Value;
                            foreach (string tmp in list)
                            {
                                if (String.IsNullOrEmpty(tmp)) continue;
                                model.relations.Add(Convert.ToUInt32(tmp, 16));
                            }
                            break;
                        }
                    case "EditType":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.editortype = 0;
                            else
                                model.editortype = udata;
                            break;
                        }
                    case "Format":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.format = 0;
                            else
                                model.format = udata;
                            break;
                        }
                    default:
                        break;
                }
            }
            param.PropertyChanged += new PropertyChangedEventHandler(Parameter_PropertyChanged);
            sfl_parameterlist.Add(model);
        }

        public void UpdateSFLParameter()
        {
            Model smod = null;
            foreach (Model mod in m_SFL_Event_ParameterList)
            {
                foreach (UInt32 guid in mod.relations)
                {
                    smod = GetParameterByGuid(guid);
                    if (smod == null) continue;
                    mod.relation_params.Add(smod);
                }
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
                        phyTostr(ref model);
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

        internal void phyTostr(ref Model p)
        {
            string tmp = "";
            if (p == null) return;

            switch (p.editortype)
            {
                case 0:
                case 3:
                    {
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
                            case 5: //Byte
                                tmp = String.Format("0x{0:X2}", (byte)p.data);
                                break;
                            case 6: //Word
                                tmp = String.Format("0x{0:X4}", (UInt16)p.data);
                                break;
                            case 7: //DWord
                                tmp = String.Format("0x{0:X8}", (UInt32)p.data);
                                break;
                            default:
                                tmp = String.Format("{0}", p.data);
                                break;
                        }
                        p.sphydata = tmp;
                        break;
                    }
                case 1:
                    {
                        switch (p.format)
                        {
                            case 0:
                                p.listindex = (UInt16)p.data;
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case 9:
                    p.sphydata = p.parent.sphydata;
                    break;
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
            string searchObj = string.Empty;
            foreach (Model param in sfl_log_parameterlist)
            {
                if (String.Compare(param.logname, ElementDefine.NoKeyDefined) == 0)
                    searchObj = param.nickname;
                else
                    searchObj = param.logname;
                if (string.Compare(searchObj,nick,true) == 0)
                    return param;
            }
            return null;
        }

        public setModel GetSetModelByName(string name)
        {
            foreach (setModel param in setmodel_list)
            {
                if (string.Compare(param.nickname, name, true) == 0)
                    return param;
            }
            return null;
        }

        private string GetHashTableValueByKey(string str, Hashtable htable)
        {
            if (htable.ContainsKey(str))
                return htable[str].ToString();
            else
                return ElementDefine.NoKeyDefined;
        }
        /*
        public void BuildJson()
        {
            try
            {
                subTask_Dic.Clear();
                foreach (setModel mod in parent.viewmode.setmodel_list)
                {
                    if (mod == null) continue;
                    subTask_Dic.Add(mod.nickname, mod.m_Item_dic[mod.itemlist[(UInt16)mod.phydata]]);
                }
                parent.subTaskJson = SharedAPI.SerializeDictionaryToJsonString(subTask_Dic);
            }
            catch (System.Exception ex)
            {
            }
        }*/
        #endregion
    }
}
