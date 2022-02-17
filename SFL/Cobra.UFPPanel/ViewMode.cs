using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.UFPPanel
{
    public class ViewMode
    {
        internal const UInt32 CommandMask = 0x00FF0000;
        internal const UInt32 CommandMask1 = 0x00FFF800;
        internal const UInt32 CommandMask2 = 0x00FFFF00;
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
            bool bdata = false;
            string stmp = string.Empty;
            Model model = GetModelByType(param);
            subModel smodel = new subModel();
            smodel.parent = param;
            foreach (DictionaryEntry de in param.sfllist[sflname].nodetable)
            {
                switch (de.Key.ToString())
                {
                    case "NickName":
                        smodel.nickname = de.Value.ToString();
                        break;
                    case "SubNickName":
                        smodel.subnickname = de.Value.ToString();
                        break;
                    case "bRead":
                        if (!Boolean.TryParse(de.Value.ToString(), out bdata))
                            smodel.bRead = true;
                        else
                            smodel.bRead = bdata;
                        model.bRead |= smodel.bRead;
                        break;
                    case "bWrite":
                        if (!Boolean.TryParse(de.Value.ToString(), out bdata))
                            smodel.bWrite = true;
                        else
                            smodel.bWrite = bdata;
                        model.bWrite |= smodel.bWrite;
                        break;
                    case "Catalog":
                        model.catalog = de.Value.ToString();
                        break;
                    case "Description":
                        model.description = de.Value.ToString();
                        break;
                    case "TPList":
                        {
                            AsyncObservableCollection<string> xl = (de.Value as AsyncObservableCollection<string>);
                            for (int i = 0; i < xl.Count;i++)
                            {
                                AsyncObservableCollection<TPModel> tpms = new AsyncObservableCollection<TPModel>();
                                Regex regex = new Regex("Bit");
                                string[] ar=regex.Split(xl[i]);
                                foreach (string arr in ar)
                                {
                                    if (arr == String.Empty) continue;
                                    string[] arrr = arr.Split('|');
                                    TPModel tpm = new TPModel();
                                    tpm.nickname = "Bit"+arrr[0];
                                    tpm.description = arrr[1];
                                    stmp = tpm.catalog = arrr[2];
                                    tpm.startbit = Convert.ToUInt16(arrr[3]);
                                    tpm.bitsnumber = Convert.ToUInt16(arrr[4]);
                                    tpms.Add(tpm);
                                }
                                smodel.caption = "Fixed supply";
                                smodel.tps_List.Add(stmp, tpms);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            param.PropertyChanged += new PropertyChangedEventHandler(Parameter_PropertyChanged);
            smodel.PropertyChanged += new PropertyChangedEventHandler(SFL_Parameter_PropertyChanged);
            model.AddSmodel(ref smodel);
        }

        public Model GetModelByType(Parameter p)
        {
            Model model = null;
            UInt32 flag = 0;
            switch (p.subsection)
            {
                case 0:
                case 2:
                case 3:
                case 4:
                    flag = (p.guid & CommandMask) >> 16;
                    break;
                case 1:
                    flag = (p.guid & CommandMask1) >> 8;
                    break;
            }

            foreach (Model param in sfl_parameterlist)
            {
                if (param.flag.Equals(flag))
                    return param;
            }
            model = new Model();
            model.flag = flag;
            model.dataType = (byte)((p.guid & CommandMask) >> 16);
            model.parent = p.sfllist[sflname].parent;
            sfl_parameterlist.Add(model);
            return model;
        }

        public void BuildPartParameterList(object model)
        {
            if (model is Model)
            {
                foreach (subModel sm in (model as Model).subModel_List)
                    dm_part_parameterlist.parameterlist.Add(sm.parent);
            }
            else
                dm_part_parameterlist.parameterlist.Add((model as subModel).parent);
        }

        public void Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter p = (Parameter)sender;
            subModel model = GetParameterByGuid(p.guid);
            if (model == null) return;
            model.PropertyChanged -= SFL_Parameter_PropertyChanged;
            switch (e.PropertyName.ToString())
            {
                case "phydata":
                    {
                        model.data = p.phydata;
                        model.sphydata = String.Format("0x{0:X8}", (UInt32)model.data);
                        break;
                    }
                case "sphydata":
                    {
                        break;
                    }
                default:
                    break;
            }
            model.PropertyChanged += SFL_Parameter_PropertyChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SFL_Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            subModel model = (subModel)sender;
            if (model == null) return;
            //model.parent.PropertyChanged -= Parameter_PropertyChanged;
            switch (e.PropertyName.ToString())
            {
                case "sphydata":
                    {
                        model.data = model.parent.phydata = (Double)Convert.ToInt32(model.sphydata, 16);
                        break;
                    }
                default:
                    break;
            }
            //model.parent.PropertyChanged += Parameter_PropertyChanged;
        }

        public subModel GetParameterByGuid(UInt32 guid)
        {
            foreach (Model model in sfl_parameterlist)
            {
                foreach (subModel sm in model.subModel_List)
                {
                    if (sm.parent.guid.Equals(guid))
                        return sm;
                }
            }
            return null;
        }
        #endregion
    }
}
