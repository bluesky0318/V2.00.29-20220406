using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ9316
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

        private List<UInt32> m_OcDelayList = new List<UInt32>();
        private List<UInt32> m_ScDelayList = new List<UInt32>();
        private List<UInt16> m_OcRegList = new List<UInt16>();
        private List<UInt16> m_ScRegList = new List<UInt16>();

        #endregion

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            if (parent.EpParamlist == null) return;
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter p)
        {
            if (p == null) return;
            switch (p.guid)
            {
                case ElementDefine.EpRsense:
                    {
                        CreateCOCTHValItems(p.phydata);
                        CreateDOC1THValItems(p.phydata);
                        CreateDOC2THValItems(p.phydata);
                        CreateBldAccuracyValItems(p.phydata);
                        CreateInChgValItems(p.phydata);
                        CreateInDsgValItems(p.phydata);
                    }
                    break;
                default:
                    break;
            }
            return;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            short sdata = 0;
            UInt16 wdata = 0;
            double resistor, dval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        sdata = (short)Physical2Regular(p.phydata, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        resistor = TempToResist(p.phydata);
                        dval = (3300 * resistor) / (resistor + 1000 * parent.pullupR);
                        sdata = (short)Physical2Regular(dval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = GetITV0FromImg(ref dval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }
                        dval = (p.phydata - 22) * parent.ITSlope + dval;
                        sdata = (short)Physical2Regular(dval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_RSENSE:
                    {
                        wdata = (UInt16)(p.phydata * 1000);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2_SLOP:
                    {
                        wdata = (UInt16)(Math.Floor(p.phydata * 10000 + 0.5));
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2_OFFSET:
                    {
                        sdata = (short)Physical2Regular(dval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CELLNUM:
                    {
                        wdata = (UInt16)(p.phydata + 3.0);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_BLD_ACC:
                    {
                        wdata = Physical2Regular(dval, p.regref, p.phyref);
                        wdata >>= 2;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2_TH:
                    {
                        UInt16 wdata1 = 0;

                        ret = GetRsenseFromImg(ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }
                        dval = p.phydata * wdata / 1000;
                        ret = GetDoc2OffsetFromImg(ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }
                        ret = GetDoc2SlopeFromImg(ref wdata1);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }

                        dval = (dval * wdata1 / 10000.0 + sdata * 10);
                        wdata = Physical2Regular(dval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = GetRsenseFromImg(ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }
                        p.phydata = (p.phydata * wdata) / (1000 * 10000);
                        sdata = (short)Physical2Regular(p.phydata, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                    {
                        m_parent.ModifyTemperatureConfig(p, true);
                        break;
                    }
                default:
                    {
                        wdata = Physical2Regular(p.phydata, p.regref, p.phyref);
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
            short sdata = 0;
            UInt16 wdata = 0;
            Double ddata = 0;
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
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                        p.phydata = (float)(p.phydata * parent.pullupR) / ((float)3300 - p.phydata) * (float)1000;
                        p.phydata = ResistToTemp(p.phydata);
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
                        ret = GetITV0FromImg(ref ddata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                        p.phydata = (p.phydata - ddata) / parent.ITSlope + 22;

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_RSENSE:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if (wdata == 0) p.phydata = 2.5;
                        else p.phydata = (float)wdata / (float)1000;

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2_SLOP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (float)wdata / (float)10000;

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2_OFFSET:
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CELLNUM:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if (wdata < 0x04) //Define illegal value
                            p.phydata = 0;
                        else
                            p.phydata = (float)(wdata - 3);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_BLD_ACC:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        wdata <<= 2;
                        wdata |= 0x0003;
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2_TH:
                    {
                        UInt16 wdata1 = 0;

                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        ret = GetDoc2OffsetFromImg(ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ret = GetDoc2SlopeFromImg(ref wdata1);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        wdata = (UInt16)(wdata - sdata);
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);

                        ddata = (ddata * 10000.0) / wdata1;
                        ret = GetRsenseFromImg(ref wdata1);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = ddata * 1000 / (float)wdata1;

                        break;
                    }

                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        ret = GetRsenseFromImg(ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)(p.phydata * 1000 * 1000 / wdata);
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
        {/*
            double dval, integer, fraction;

            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)integer;*/
            return (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(short sVal, double RegularRef, double PhysicalRef)
        {/*
            double dval, integer, fraction;

            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)integer;*/
            return (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
        }

        /// <summary>
        /// 转换Physical -> Hex
        /// </summary>
        /// <param name="fVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private UInt16 Physical2Regular(double dVal, double RegularRef, double PhysicalRef)
        {
            UInt16 wval;
            double dval, integer, fraction;

            dval = (double)((double)(dVal * RegularRef) / (double)PhysicalRef);
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
            UInt16 hi = 0, lo = 0;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                {
                    regLow = dic.Value;
                    ret = ReadRegFromImg(regLow.address, p.guid, ref lo);
                    lo <<= (16 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadRegFromImg(regHi.address, p.guid, ref hi);
                    hi <<= (16 - regHi.bitsnumber - regHi.startbit); //align with left
                    hi >>= (16 - regHi.bitsnumber); //align with right
                }
            }

            data = ((UInt32)(((UInt16)(lo)) | ((UInt32)((UInt16)(hi))) << 16));
            data >>= (16 - regLow.bitsnumber); //align with right

            pval = (UInt16)data;
            p.hexdata = pval;
            return ret;
        }

        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref short pval)
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
            UInt16 data = 0, lomask = 0, himask = 0;
            UInt16 plo, phi, ptmp;
            //byte hi = 0, lo = 0, tmp = 0;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            p.hexdata = wVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            ret = ReadRegFromImg(regLow.address, p.guid, ref data);
            if (regHi == null)
            {
                lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                lomask <<= regLow.startbit;
                data &= (UInt16)(~lomask);
                data |= (UInt16)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data);
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

        #region 特殊参数操作
        private UInt32 GetITV0FromImg(ref double dval)
        {
            UInt16 wval = 0;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            param = parent.GetEpParameterByGuid(ElementDefine.EpITV0);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            ret = ReadFromRegImg(param, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            dval = ((float)(wval & 0x1FFF) * 0.75);
            return ret;
        }

        private UInt32 GetDoc2SlopeFromImg(ref UInt16 pva)
        {
            UInt16 wdata = 0;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            param = parent.GetEpParameterByGuid(ElementDefine.EpDoc2Slop);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            ret = ReadFromRegImg(param, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (wdata == 0) pva = 10000;
            else pva = wdata;
            return ret;
        }

        private UInt32 GetDoc2OffsetFromImg(ref short pval)
        {
            UInt16 wdata;
            short sdata = 0;
            UInt16 offset = 0;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            param = parent.GetEpParameterByGuid(ElementDefine.EpDoc2Offset);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            ret = ReadSignedFromRegImg(param, ref sdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata = offset;
            wdata <<= 12;
            sdata = (short)wdata;
            sdata /= 4096;
            pval = sdata;
            return ret;
        }

        private UInt32 GetRsenseFromImg(ref UInt16 pval)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            param = parent.GetEpParameterByGuid(ElementDefine.EpRsense);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            ret = ReadFromRegImg(param, ref pval);
            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) || (pval == 0))
                pval = 2500;
            return ret;
        }

        private UInt32 CreateCOCTHValItems(double rsense)
        {
            double tfval;
            string tmp;
            Parameter param = null;

            if (rsense == 0) rsense = 2.5;
            param = parent.GetEpParameterByGuid(ElementDefine.EpCOCTH);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            param.itemlist.Clear();
            for (UInt16 i = 0; i < 0x100; i++)
            {
                tfval = ((double)((float)(i << 6) + 0x3F) * (double)7.8125) / ((double)rsense * 1000);
                tmp = String.Format("{0:F2}", tfval);
                param.itemlist.Add(tmp);
            }
            param.itemlist = param.itemlist;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 CreateDOC1THValItems(double rsense)
        {
            double tfval;
            string tmp;
            Parameter param = null;

            if (rsense == 0) rsense = 2.5;
            param = parent.GetEpParameterByGuid(ElementDefine.EpDOC1TH);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            param.itemlist.Clear();
            for (UInt16 i = 0; i < 0x100; i++)
            {
                tfval = ((double)((float)(i << 7) + 0x7F) * (double)7.8125) / ((double)rsense * 1000);
                tmp = String.Format("{0:F2}", tfval);
                param.itemlist.Add(tmp);
            }
            param.itemlist = param.itemlist;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 CreateDOC2THValItems(double rsense)
        {
            double tfval;
            string tmp;
            Parameter param = null;

            if (rsense == 0) rsense = 2.5;
            param = parent.GetEpParameterByGuid(ElementDefine.EpDOC2TH);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            param.itemlist.Clear();
            for (UInt16 i = 0; i < 58; i++)
            {
                tfval = ((double)((float)(i + 5) * 10)) / (double)rsense;
                tmp = String.Format("{0:F2}", tfval);
                param.itemlist.Add(tmp);
            }
            param.itemlist = param.itemlist;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 CreateBldAccuracyValItems(double rsense)
        {
            double tfval;
            string tmp;
            Parameter param = null;

            if (rsense == 0) rsense = 2.5;
            param = parent.GetEpParameterByGuid(ElementDefine.EpBldAcc);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            param.itemlist.Clear();
            for (int i = 0; i < 16; i++)
            {
                tfval = (float)(((i << 2) + 0x03) * 1.5);
                tmp = String.Format("{0:F2}", tfval);
                param.itemlist.Add(tmp);
            }
            param.itemlist = param.itemlist;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 CreateInChgValItems(double rsense)
        {
            double tfval;
            string tmp;
            Parameter param = null;

            if (rsense == 0) rsense = 2.5;
            param = parent.GetEpParameterByGuid(ElementDefine.EpInChg);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            param.itemlist.Clear();
            for (UInt16 i = 0; i < 0x100; i++)
            {
                tfval = ((double)((float)(i << 2) + 0x03) * (double)7.8125) / (double)rsense;
                tmp = String.Format("{0:F2}", tfval);
                param.itemlist.Add(tmp);
            }
            param.itemlist = param.itemlist;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 CreateInDsgValItems(double rsense)
        {
            double tfval;
            string tmp;
            Parameter param = null;

            if (rsense == 0) rsense = 2.5;
            param = parent.GetEpParameterByGuid(ElementDefine.EpInDsg);
            if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            param.itemlist.Clear();
            for (UInt16 i = 0; i < 0x100; i++)
            {
                tfval = ((double)((float)(i << 2) + 0x03) * (double)7.8125) / (double)rsense;
                tmp = String.Format("{0:F2}", tfval);
                param.itemlist.Add(tmp);
            }
            param.itemlist = param.itemlist;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EEPROMElement:
                    {
                        pval = parent.m_EpRegImg[reg].val;
                        ret = parent.m_EpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.SRAMElement:
                    {
                        pval = parent.m_SmRegImg[reg].val;
                        ret = parent.m_SmRegImg[reg].err;
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
                case ElementDefine.EEPROMElement:
                    {
                        parent.m_EpRegImg[reg].val = value;
                        parent.m_EpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        parent.m_OpRegImg[reg].val = value;
                        parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.SRAMElement:
                    {
                        parent.m_SmRegImg[reg].val = value;
                        parent.m_SmRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                default:
                    break;
            }
        }

        private UInt32 GetThmPullupResistorFromImg(ref float pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte val = 0;

            val = (byte)(SharedFormula.LoByte(parent.m_OpRegImg[0x03].val) & 0x03);
            switch (val)
            {
                case 0:
                    pval = 3;
                    break;
                case 1:
                    pval = 60;
                    break;
                default:
                    pval = 0;
                    break;
            }

            ret = parent.m_OpRegImg[0x03].err;
            return ret;
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
