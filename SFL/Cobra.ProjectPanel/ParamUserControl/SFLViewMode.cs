using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.ProjectPanel.Param
{
    public class SFLViewMode
    {
        //父对象保存
        private ParamUserControl m_control_parent;
        public ParamUserControl control_parent
        {
            get { return m_control_parent; }
            set { m_control_parent = value; }
        }

        private ObservableCollection<SFLModel> m_SFL_ParameterList = new ObservableCollection<SFLModel>();
        public ObservableCollection<SFLModel> sfl_parameterlist
        {
            get { return m_SFL_ParameterList; }
            set { m_SFL_ParameterList = value; }
        }

        private ParamContainer m_DM_One_ParameterList = new ParamContainer();
        public ParamContainer dm_part_parameterlist
        {
            get { return m_DM_One_ParameterList; }
            set { m_DM_One_ParameterList = value; }
        }

        private UInt16 order = 0;
        public SFLViewMode(object pParent, object parent)
        {
            #region 相关初始化
            control_parent = (ParamUserControl)parent;
            if (control_parent == null) return;
            #endregion

            control_parent.parent.viewmode.parameterlist.Clear();
            sfl_parameterlist.Clear();
            LoadParameterXML(control_parent.projFile.fullName);
        }

        #region 参数操作
        public bool LoadParameterXML(string xmlfile)
        {
            try
            {
                XElement rootNode = XElement.Load(xmlfile);
                IEnumerable<XElement> myTargetNodes = from myTarget in rootNode.Elements("Element") where myTarget.HasElements select myTarget;
                foreach (XElement snode in myTargetNodes)
                {
                    SFLModel model = new SFLModel(this, snode);
                    sfl_parameterlist.Add(model);
                    control_parent.parent.viewmode.parameterlist.Add(model.parent);
                    model.order = order;
                    order++;
                }
                control_parent.parent.viewmode.UpdatePrjParameters();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return true;
        }

        public bool SaveParameterXML(string xmlfile)
        {
            UInt32 guid = 0;
            SFLModel param = null;

            XElement rootNode = XElement.Load(xmlfile);
            IEnumerable<XElement> myTargetNodes = from myTarget in rootNode.Elements("Element") where myTarget.HasElements select myTarget;
            foreach (XElement snode in myTargetNodes)
            {
                try
                {
                    guid = Convert.ToUInt32(snode.Attribute("Guid").Value, 16);
                    param = GetParameterByGuid(guid);
                    if (param == null) continue;
                    XElement alt = snode.Elements("PhysicalData").First();
                    if (alt == null) continue;
                    if (param.brange)
                        alt.Value = param.data.ToString();

                    alt = snode.Elements("Private").Elements("SFL").Where(x => (string)x.Attribute("Name") == "Project").Elements("DefValue").First();
                    if (alt == null) continue;
                    if (param.brange)
                        alt.Value = param.data.ToString();
                    else
                        alt.Value = param.sphydata;
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            rootNode.Save(xmlfile);
            return true;
        }


        public SFLModel GetParameterByGuid(UInt32 guid)
        {
            foreach (SFLModel param in sfl_parameterlist)
            {
                if (param.parent.guid.Equals(guid))
                    return param;
            }
            return null;
        }

        internal void phyTostr(SFLModel p)
        {
            string tmp = "";
            if (p == null) return;

            p.PropertyChanged -= SFL_Parameter_PropertyChanged;
            if ((p.data > p.maxvalue) || (p.data < p.minvalue))
            {
                p.berror = true;
                p.errorcode = LibErrorCode.IDS_ERR_SECTION_DEVICECONFSFL_PARAM_INVALID;
            }
            else
                p.berror = false;

            if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                p.berror = true;

            switch (p.editortype)
            {
                case 0:
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
                                tmp = SharedFormula.UInt32ToData((UInt16)p.data);
                                break;
                            case 9: //String
                                tmp = p.sphydata;
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
                case 2:
                    {
                        if (p.data > 0.0)
                            p.bcheck = true;
                        else
                            p.bcheck = false;
                        break;
                    }
                default:
                    break;
            }
            p.PropertyChanged += SFL_Parameter_PropertyChanged;
        }

        internal void strTophy(ref SFLModel p)
        {
            double ddata = 0.0;
            p.berror = false;
            if (p == null)
            {
                p.errorcode = LibErrorCode.IDS_ERR_PARAM_INVALID_HANDLER;
                return;
            }

            switch (p.editortype)
            {
                #region 定义编辑类型
                case 0:
                    {
                        switch (p.format)
                        {
                            case 0: //Int     
                            case 1: //float1
                            case 2: //float2
                            case 3: //float3
                            case 4: //float4
                                {
                                    if (!Double.TryParse(p.sphydata, out ddata))
                                        p.errorcode = LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL;
                                    else
                                        p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                    break;
                                }
                            case 5: //Hex
                            case 6: //Word
                                {
                                    try
                                    {
                                        ddata = (Double)Convert.ToInt32(p.sphydata, 16);
                                    }
                                    catch (Exception e)
                                    {
                                        p.errorcode = LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL;
                                        break;
                                    }
                                    p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                    break;
                                }
                            case 7: //DWord
                                {
                                    try
                                    {
                                        ddata = (Double)Convert.ToUInt32(p.sphydata, 16);
                                    }
                                    catch (Exception e)
                                    {
                                        p.errorcode = LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL;
                                        break;
                                    }
                                    p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                    break;
                                }
                            case 8: //Date
                                {
                                    try
                                    {
                                        ddata = SharedFormula.DateToUInt32(p.sphydata);
                                    }
                                    catch (Exception e)
                                    {
                                        p.errorcode = LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL;
                                        break;
                                    }
                                    p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                    break;
                                }
                            case 9: //String
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                #endregion
                #region 定义复选类型
                case 1:
                    {
                        switch (p.format)
                        {
                            case 0:
                                {
                                    ddata = (double)p.listindex;
                                    p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                    break;
                                }
                            case 1:
                                {
                                    if (!Double.TryParse(p.itemlist[p.listindex], out ddata))
                                        p.errorcode = LibErrorCode.IDS_ERR_PARAM_DATA_ILLEGAL;
                                    else
                                        p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                    break;
                                }
                        }
                    }
                    break;
                #endregion
                #region 定义点击类型
                case 2:
                    {
                        if (p.bcheck)
                            ddata = 1.0;
                        else
                            ddata = 0.0;
                        p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    }
                    break;
                #endregion
                default:
                    break;
            }
            if (p.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
                p.data = ddata;
        }


        /// <summary>
        /// 更新参数相关属性
        /// </summary>
        /// <param name="param"></param>
        private void UpdateParam(ref SFLModel param)
        {
            SFLModel target = null;
            if (param == null) return;
            //针对编辑状态更新
            if (param.relations.Count == 0) return;
            foreach (uint guid in param.relations)
            {
                target = GetParameterByGuid(guid);     //获取目标参数
                if (target == null) return;
                target.parent.PropertyChanged -= Parameter_PropertyChanged;
                target.parent.phydata = target.data;
                target.parent.PropertyChanged += Parameter_PropertyChanged;
            }
            param.parent.PropertyChanged -= Parameter_PropertyChanged;
            param.parent.phydata = param.data;
            param.parent.PropertyChanged += Parameter_PropertyChanged;
            control_parent.parent.parent.UpdataDEMParameterList(param.parent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SFL_Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SFLModel p = (SFLModel)sender;
            switch (e.PropertyName.ToString())
            {
                case "sphydata":
                case "bcheck":
                case "listindex":
                    {
                        strTophy(ref p);
                        if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            phyTostr(p);
                            return;
                        }

                        break;
                    }
                case "data":
                    {
                        phyTostr(p);
                        UpdateParam(ref p);
                        break;
                    }
                default:
                    break;
            }
        }

        public void Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter p = (Parameter)sender;
            SFLModel model = GetParameterByGuid(p.guid);
            if (model == null) return;
            switch (e.PropertyName.ToString())
            {
                case "phydata":
                    {
                        if ((p.phydata > model.maxvalue) || (p.phydata < model.minvalue) || (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL))
                            model.berror = true;
                        else
                            model.berror = false;

                        model.data = p.phydata;
                        model.errorcode = p.errorcode;
                        //UpdateParam(ref model);
                        break;
                    }
                case "itemlist":
                    {
                        if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            model.berror = true;
                        else
                            model.berror = false;

                        model.listindex = model.listindex; //触发Combobox选择
                        model.errorcode = p.errorcode;
                        break;
                    }
                case "sphydata":
                    {
                        if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            model.berror = true;
                        else
                            model.berror = false;

                        model.sphydata = p.sphydata;
                        model.errorcode = p.errorcode;
                        break;
                    }
                case "dbPhyMin":
                    {
                        if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            model.berror = true;
                        else
                            model.berror = false;

                        model.minvalue = p.dbPhyMin;
                        model.errorcode = p.errorcode;
                        break;
                    }
                case "dbPhyMax":
                    {
                        if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            model.berror = true;
                        else
                            model.berror = false;

                        model.maxvalue = p.dbPhyMax;
                        model.errorcode = p.errorcode;
                        break;
                    }
                default:
                    break;
            }
        }

        public UInt32 WriteDevice()
        {
            foreach (SFLModel model in sfl_parameterlist)
            {
                if (model.berror && (model.errorcode & LibErrorCode.IDS_ERR_SECTION_DEVICECONFSFL) == LibErrorCode.IDS_ERR_SECTION_DEVICECONFSFL)
                    return LibErrorCode.IDS_ERR_SECTION_DEVICECONFSFL_PARAM_INVALID;

                Parameter param = model.parent;
                if (model.brange)
                    param.phydata = model.data;
                else
                    param.sphydata = model.sphydata;
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 BuildPartParameterList(string guid)
        {
            UInt32 uid = 0;
            dm_part_parameterlist.parameterlist.Clear();
            if (!UInt32.TryParse(guid, out uid)) return LibErrorCode.IDS_ERR_COM_INVALID_PARAMETER;

            SFLModel param = GetParameterByGuid(uid);
            if (param == null) return LibErrorCode.IDS_ERR_COM_INVALID_PARAMETER;
            dm_part_parameterlist.parameterlist.Add(param.parent);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion
    }
}
