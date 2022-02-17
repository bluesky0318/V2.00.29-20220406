using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.Trim2Panel
{
    public class ViewMode
    {
        public static UInt16 m_trim_count = 0;
        public static UInt16 m_total_trims = 0;
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

        private AsyncObservableCollection<InPutModel> m_Input_ParameterList = new AsyncObservableCollection<InPutModel>();
        public AsyncObservableCollection<InPutModel> input_parameterlist
        {
            get { return m_Input_ParameterList; }
            set { m_Input_ParameterList = value; }
        }

        private AsyncObservableCollection<OutPutModel> m_Output_ParameterList = new AsyncObservableCollection<OutPutModel>();
        public AsyncObservableCollection<OutPutModel> output_parameterlist
        {
            get { return m_Output_ParameterList; }
            set { m_Output_ParameterList = value; }
        }

        private ParamContainer m_DM_ParameterList = new ParamContainer();
        public ParamContainer dm_parameterlist
        {
            get { return m_DM_ParameterList; }
            set { m_DM_ParameterList = value; }
        }

        private ParamContainer m_DM_Part_ParameterList = new ParamContainer();
        public ParamContainer dm_part_parameterlist
        {
            get { return m_DM_Part_ParameterList; }
            set { m_DM_Part_ParameterList = value; }
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
            UInt32 wdata = 0;
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
                    case "Order":
                        {
                            if (String.IsNullOrEmpty(de.Value.ToString()))
                                model.order = 0;
                            else
                                model.order = Convert.ToUInt16(de.Value.ToString(), 10);
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
                    case "Description":
                        model.description = de.Value.ToString();
                        break;
                    case "DefValue":
                        {
                            if (!Double.TryParse(de.Value.ToString(), out ddata))
                                model.data = 0.0;
                            else
                                model.data = Convert.ToDouble(de.Value.ToString());
                            break;
                        }
                    case "Slope":
                        {
                            wdata = Convert.ToUInt32(de.Value.ToString(), 16);
                            model.slope_relation = GetParameterByGuid(wdata);
                            break;
                        }
                    case "Offset":
                        {
                            wdata = Convert.ToUInt32(de.Value.ToString(), 16);
                            model.offset_relation = GetParameterByGuid(wdata);
                            break;
                        }
                    case "SubType":
                        {
                            if (!UInt16.TryParse(de.Value.ToString(), out udata))
                                model.subType = 0;
                            else
                                model.subType = udata;
                            break;
                        }
                    default:
                        break;
                }
            }
            if (model.subType == 0)
                sfl_parameterlist.Add(model);
        }

        public Parameter GetParameterByGuid(UInt32 guid)
        {
            return dm_parameterlist.GetParameterByGuid(guid);
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

        public UInt32 BuildInputParameterList()
        {
            //Trim Parameter List
            double dval = 0;
            dm_part_parameterlist.parameterlist.Clear();
            foreach (InPutModel mo in input_parameterlist)
            {
                if (mo == null) continue;
                if (!mo.bChecked) continue;
                if(Double.TryParse(mo.input[m_trim_count],out dval))
                    mo.parent.parent.phydata = dval;
                else
                    mo.parent.parent.phydata = 0;
                dm_part_parameterlist.parameterlist.Add(mo.parent.parent);
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 BuildSlopeParameterList()
        {
            dm_part_parameterlist.parameterlist.Clear();
            foreach (OutPutModel mo in output_parameterlist)
            {
                if (mo == null) continue;
                if (!mo.parent.bChecked) continue;
                mo.parent.slope_relation.phydata = Convert.ToByte(mo.sSlope,16);
                dm_part_parameterlist.parameterlist.Add(mo.parent.slope_relation);
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 BuildOffsetParameterList()
        {
            dm_part_parameterlist.parameterlist.Clear();
            foreach (OutPutModel mo in output_parameterlist)
            {
                if (mo == null) continue;
                if (!mo.parent.bChecked) continue;
                mo.parent.offset_relation.phydata = Convert.ToDouble(mo.sOffset);
                dm_part_parameterlist.parameterlist.Add(mo.parent.offset_relation);
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public void ShowSlope()
        {
            foreach (OutPutModel mo in output_parameterlist)
            {
                if (mo == null) continue;
                if (!mo.parent.bChecked) continue;
                mo.sSlope = string.Format("0x{0:x2}", (byte)mo.parent.slope_relation.phydata);
            } 
        }

        public void ShowOffset()
        {
            foreach (OutPutModel mo in output_parameterlist)
            {
                if (mo == null) continue;
                if (!mo.parent.bChecked) continue;
                mo.sOffset = string.Format("{0:N4}", mo.parent.offset_relation.phydata);
                mo.sCode = string.Format("0x{0:x2}", mo.parent.offset_relation.hexdata);
            }
        }
        #endregion
    }
}
