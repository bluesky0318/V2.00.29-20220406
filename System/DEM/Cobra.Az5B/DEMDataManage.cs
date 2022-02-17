using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Az5B
{
    internal class DEMDataManage
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        #endregion

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            if (parent.EFParamlist == null) return;
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter p)
        {
            return;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            UInt16 wdata = 0;
            float fval = 0;
            int thm_crrt = 0;
            double resistor = 0, resistor1 = 0;
            Parameter relate_param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        ret = parent.GetThmCrrtSel(ref thm_crrt);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        resistor = TempToResist(p.phydata);
                        fval = (float)(resistor * thm_crrt / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_OT_TH:
                    {
                        resistor = TempToResist(p.phydata);
                        fval = (float)(resistor * 120 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_UT_TH:
                    {
                        resistor = TempToResist(p.phydata);
                        fval = (float)(resistor * 20 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_DOTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A008);
                        if (relate_param == null) return;
                        resistor1 = TempToResist(relate_param.phydata);
                        resistor = TempToResist(relate_param.phydata - p.phydata);
                        fval = (float)((resistor- resistor1) * 120 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_COTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A108);
                        if (relate_param == null) return;
                        resistor1 = TempToResist(relate_param.phydata);
                        resistor = TempToResist(relate_param.phydata - p.phydata);
                        fval = (float)((resistor - resistor1) * 120 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_DUTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A208);
                        if (relate_param == null) return;
                        resistor1 = TempToResist(relate_param.phydata);
                        resistor = TempToResist(relate_param.phydata + p.phydata);
                        fval = (float)((resistor1 - resistor) * 20 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_CUTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A308);
                        if (relate_param == null) return;
                        resistor1 = TempToResist(relate_param.phydata);
                        resistor = TempToResist(relate_param.phydata + p.phydata);
                        fval = (float)((resistor1 - resistor) * 20 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT1_OTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A408);
                        if (relate_param == null) return;
                        resistor1 = TempToResist(relate_param.phydata);
                        resistor = TempToResist(relate_param.phydata - p.phydata);
                        fval = (float)((resistor - resistor1) * 120 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT1_UTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A508);
                        if (relate_param == null) return;
                        resistor1 = TempToResist(relate_param.phydata);
                        resistor = TempToResist(relate_param.phydata + p.phydata);
                        fval = (float)((resistor1 - resistor) * 20 / 1000.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        wdata -= (UInt16)p.offset;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        //fval = (float)((p.phydata - 23) * 4.345 + 1252.5);
                        fval = (float)((p.phydata - 23) * 4.35 + 1270);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        double Rsense = m_parent.rsense;
                        if (Rsense == 0) Rsense = 2500;

                        fval = (float)((p.phydata * 10 * Rsense / (float)(1000 * 1000)) + 1000);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                    {
                        m_parent.ModifyTemperatureConfig(p, true);
                        break;
                    }
                default:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
            }
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            int thm_crrt = 0;
            Int16 sdata = 0;
            UInt16 wdata = 0;
            Double ddata = 0;
            Parameter relate_param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        ret = parent.GetThmCrrtSel(ref thm_crrt);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);

                        ddata = ddata * 1000 / thm_crrt;
                        p.phydata = ResistToTemp(ddata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_OT_TH:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata = ddata * 1000 / 120;
                        p.phydata = ResistToTemp(ddata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_UT_TH:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata = ddata * 1000 / 20;
                        p.phydata = ResistToTemp(ddata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_DOTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A008);
                        if (relate_param == null) return;
                        Hex2Physical(ref relate_param);

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata += Regular2Physical((UInt16)(relate_param.hexdata + relate_param.offset), relate_param.regref, relate_param.phyref);
                        ddata = ddata * 1000 / 120;
                        p.phydata = relate_param.phydata - ResistToTemp(ddata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_COTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A108);
                        if (relate_param == null) return;
                        Hex2Physical(ref relate_param);

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata += Regular2Physical((UInt16)(relate_param.hexdata + relate_param.offset), relate_param.regref, relate_param.phyref);
                        ddata = ddata * 1000 / 120;
                        p.phydata = relate_param.phydata - ResistToTemp(ddata);

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_DUTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A208);
                        if (relate_param == null) return;
                        Hex2Physical(ref relate_param);

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata = Regular2Physical((UInt16)(relate_param.hexdata + relate_param.offset), relate_param.regref, relate_param.phyref) - ddata;
                        ddata = ddata * 1000 / 20;
                        p.phydata = ResistToTemp(ddata) - relate_param.phydata;

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_CUTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A308);
                        if (relate_param == null) return;
                        Hex2Physical(ref relate_param);

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata = Regular2Physical((UInt16)(relate_param.hexdata + relate_param.offset), relate_param.regref, relate_param.phyref) - ddata;
                        ddata = ddata * 1000 / 20;
                        p.phydata = ResistToTemp(ddata) - relate_param.phydata;

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT1_OTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A408);
                        if (relate_param == null) return;
                        Hex2Physical(ref relate_param);

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata += Regular2Physical((UInt16)(relate_param.hexdata + relate_param.offset), relate_param.regref, relate_param.phyref);
                        ddata = ddata * 1000 / 20;
                        p.phydata = relate_param.phydata - ResistToTemp(ddata);

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT1_UTR_HYS:
                    {
                        relate_param = parent.GetOpParameterByGuid(0x0003A508);
                        if (relate_param == null) return;
                        Hex2Physical(ref relate_param);

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata += (UInt16)p.offset;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata = Regular2Physical((UInt16)(relate_param.hexdata + relate_param.offset), relate_param.regref, relate_param.phyref) - ddata;
                        ddata = ddata * 1000 / 20;
                        p.phydata = ResistToTemp(ddata) - relate_param.phydata;

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ddata = Regular2Physical(sdata, p.regref, p.phyref);
                        //p.phydata = (double)((ddata - 1252.5) / 4.345 + 23.0);
                        p.phydata = (double)((ddata - 1270) / 4.35 + 23.0);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if (sdata == -32768) sdata = 0;  //Fix overflow issue.
                        ddata = Regular2Physical(sdata, p.regref, p.phyref);
                        p.phydata = (double)((ddata * 1000.0 * 1000.0) / m_parent.rsense);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CADC:
                    ret = parent.SetCADCMode(parent.cadc_mode);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (parent.cadc_mode == ElementDefine.CADC_MODE.DISABLE)
                        wdata = 0;
                    else if (parent.cadc_mode == ElementDefine.CADC_MODE.TRIGGER)
                    {
                        wdata = parent.m_OpRegImg[ElementDefine.FinalCadcTriggerData_Reg].val;
                        ret = parent.m_OpRegImg[ElementDefine.FinalCadcTriggerData_Reg].err;
                    }
                    else if (parent.cadc_mode == ElementDefine.CADC_MODE.MOVING)
                    {
                        wdata = parent.m_OpRegImg[ElementDefine.FinalCadcMovingData_Reg].val;
                        ret = parent.m_OpRegImg[ElementDefine.FinalCadcMovingData_Reg].err;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    sdata = (short)wdata;
                    if (sdata == -32768) sdata = 0;  //Fix overflow issue.
                    ddata = Regular2Physical(sdata, p.regref, p.phyref);
                    p.phydata = (double)((ddata * 1000.0 * 1000.0) / m_parent.rsense);
                    break;
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                    {
                        m_parent.ModifyTemperatureConfig(p, false);
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)wdata * p.phyref / p.regref);
                    }
                    break;
            }
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(UInt16 wVal, double RegularRef, double PhysicalRef)
        {
            double dval, integer, fraction;

            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)dval;
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(short sVal, double RegularRef, double PhysicalRef)
        {
            double dval, integer, fraction;

            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)dval;
        }

        /// <summary>
        /// 转换Physical -> Hex
        /// </summary>
        /// <param name="fVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private UInt16 Physical2Regular(float fVal, double RegularRef, double PhysicalRef)
        {
            UInt16 wval;
            double dval, integer, fraction;

            dval = (double)((double)(fVal * RegularRef) / (double)PhysicalRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            if (fraction <= -0.5)
                integer -= 1;
            wval = (UInt16)integer;

            return wval;
        }

        /// <summary>
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadFromRegImg(Parameter p, ref UInt16 pval)
        {
            UInt32 data;
            UInt16 hi = 0, mi = 0, lo = 0;
            Reg regLow = null, regMid = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                {
                    regLow = dic.Value;
                    ret = ReadRegFromImg(regLow.address, p.guid, ref lo);
                    lo <<= (16 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("Mid"))
                {
                    regMid = dic.Value;
                    ret = ReadRegFromImg(regMid.address, p.guid, ref mi);
                    mi <<= (16 - regMid.bitsnumber - regMid.startbit); //align with left
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadRegFromImg(regHi.address, p.guid, ref hi);
                    hi <<= (16 - regHi.bitsnumber - regHi.startbit); //align with left
                    hi >>= (16 - regHi.bitsnumber); //align with right
                }
            }

            if (regMid != null)
            {
                data = ((UInt32)(((UInt16)(mi)) | ((UInt32)((UInt16)(hi))) << 16));
                data >>= (16 - regMid.bitsnumber); //align with right

                data = ((UInt32)(((UInt16)(lo)) | ((UInt32)((UInt16)(data))) << 16));
                data >>= (16 - regLow.bitsnumber); //align with right
            }
            else
            {
                data = ((UInt32)(((UInt16)(lo)) | ((UInt32)((UInt16)(hi))) << 16));
                data >>= (16 - regLow.bitsnumber); //align with right
            }

            pval = (UInt16)data;
            p.hexdata = pval;
            return ret;
        }
        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref Int16 pval)
        {
            UInt16 wdata = 0, tr = 0;
            Int16 sdata;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            if (regHi != null)
                tr = (UInt16)(16 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt16)(16 - regLow.bitsnumber);

            wdata <<= tr;
            sdata = (Int16)wdata;
            sdata = (Int16)(sdata / (1 << tr));

            pval = sdata;
            return ret;
        }


        /// <summary>
        /// 写数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <returns></returns>
        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            UInt16 data = 0, lomask = 0, mimask = 0, himask = 0;
            UInt16 plo, phi, pmi, ptmp;
            //byte hi = 0, lo = 0, tmp = 0;
            Reg regLow = null, regHi = null, regMid = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            p.hexdata = wVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("Mid"))
                    regMid = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            ret = ReadRegFromImg(regLow.address, p.guid, ref data);
            if (regHi == null) //if no high reg,no mid reg,only low reg
            {
                lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                lomask <<= regLow.startbit;
                data &= (UInt16)(~lomask);
                data |= (UInt16)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data);
            }
            else
            {
                if (regMid != null)
                {
                    lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                    plo = (UInt16)(wVal & lomask);

                    mimask = (UInt16)((1 << regMid.bitsnumber) - 1);
                    mimask <<= regLow.bitsnumber;
                    pmi = (UInt16)((wVal & mimask) >> regLow.bitsnumber);

                    himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                    himask <<= (regLow.bitsnumber + regMid.bitsnumber);
                    phi = (UInt16)((wVal & himask) >> (regLow.bitsnumber + regMid.bitsnumber));

                    lomask <<= regLow.startbit;
                    ptmp = (UInt16)(data & ~lomask);
                    ptmp |= (UInt16)(plo << regLow.startbit);
                    WriteRegToImg(regLow.address, p.guid, ptmp);

                    ret |= ReadRegFromImg(regMid.address, p.guid, ref data);
                    mimask = (UInt16)((1 << regMid.bitsnumber) - 1);
                    mimask <<= regMid.startbit;
                    ptmp = (UInt16)(data & ~mimask);
                    ptmp |= (UInt16)(pmi << regMid.startbit);
                    WriteRegToImg(regMid.address, p.guid, ptmp);

                    ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                    himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                    himask <<= regHi.startbit;
                    ptmp = (UInt16)(data & ~himask);
                    ptmp |= (UInt16)(phi << regHi.startbit);
                    WriteRegToImg(regHi.address, p.guid, ptmp);
                }
                else
                {
                    lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                    plo = (UInt16)(wVal & lomask);
                    himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                    himask <<= regLow.bitsnumber;
                    phi = (UInt16)((wVal & himask) >> regLow.bitsnumber);

                    //mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                    lomask <<= regLow.startbit;
                    ptmp = (UInt16)(data & ~lomask);
                    ptmp |= (UInt16)(plo << regLow.startbit);
                    WriteRegToImg(regLow.address, p.guid, ptmp);

                    ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                    himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                    himask <<= regHi.startbit;
                    ptmp = (UInt16)(data & ~himask);
                    ptmp |= (UInt16)(phi << regHi.startbit);
                    WriteRegToImg(regHi.address, p.guid, ptmp);
                }
            }

            return ret;
        }

        /// <summary>
        /// 写有符号数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <param name="pChip"></param>
        /// <returns></returns>
        private UInt32 WriteSignedToRegImg(Parameter p, Int16 sVal)
        {
            UInt16 wdata, tr = 0;
            Int16 sdata;
            Reg regLow = null, regHi = null;

            sdata = sVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }
            if (regHi != null)
                tr = (UInt16)(16 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt16)(16 - regLow.bitsnumber);

            sdata *= (Int16)(1 << tr);
            wdata = (UInt16)sdata;
            wdata >>= tr;

            return WriteToRegImg(p, wdata);
        }

        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EFUSEElement:
                    {
                        pval = parent.m_EFRegImg[reg].val;
                        ret = parent.m_EFRegImg[reg].err;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }

        private void WriteRegToImg(UInt16 reg, UInt32 guid, UInt16 value)
        {
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EFUSEElement:
                    {
                        parent.m_EFRegImg[reg].val = value;
                        parent.m_EFRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        parent.m_OpRegImg[reg].val = value;
                        parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion   

        #region 外部温度转换

        public double ResistToTemp(double resist)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }
            return SharedFormula.ResistToTemp(resist, m_TempVals, m_ResistVals);
        }

        public double TempToResist(double temp)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }

            return SharedFormula.TempToResist(temp, m_TempVals, m_ResistVals);
        }
        #endregion   
    }
}
