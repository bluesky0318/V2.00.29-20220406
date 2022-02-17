using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Azalea10_Azalea15_Stack
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

        private List<UInt32> m_OcDelayList  = new List<UInt32>();
        private List<UInt32> m_ScDelayList  = new List<UInt32>();
        private List<UInt16> m_OcRegList    = new List<UInt16>();
        private List<UInt16> m_ScRegList    = new List<UInt16>();

        #endregion

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            if (parent.YFParamlist == null) return;
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
            double resistor = 0;
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
                        float Thm_PullupRes = 0;
                        ret = GetThmPullupResistorFromImg(ref Thm_PullupRes);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.hexdata = ElementDefine.PARAM_HEX_ERROR;
                            break;
                        }

                        resistor = TempToResist(p.phydata);
                        fval = (float)((5000 * resistor + Thm_PullupRes * 1000 * 5000) / ((m_parent.pullupR + Thm_PullupRes) * 1000 + resistor));
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        fval = (float)((p.phydata + 50) * 16.0);
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
                        wdata = (UInt16)((double)(p.phydata * p.regref) / (double)p.phyref);
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
            short  sdata = 0;
            UInt16 wdata = 0;
            Double ddata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        float Thm_PullupRes = 0;
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;

                        ret = GetThmPullupResistorFromImg(ref Thm_PullupRes);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        //p.phydata = (double)((p.phydata * (m_parent.pullupR + Thm_PullupRes) * 1000 - Thm_PullupRes * 1000 * 2500) / (2500 - p.phydata));
                        p.phydata = (double)((p.phydata * (Thm_PullupRes) * 1000) / (2500 - p.phydata));
                        p.phydata = ResistToTemp(p.phydata);//*/
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = (double)(p.phydata / 16 - 50);
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
                        p.phydata = wdata;
                        p.phydata = Regular2Physical((short)wdata, p.regref, p.phyref);

                        //p.phydata = (double)((p.phydata - 1000.0) * 1000 * 1000 / (9.8 * m_parent.rsense))*(-1.0);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOCTH:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        switch (wdata)
                        {
                            case 0:
                                ddata = 10;
                                break;
                            case 1:
                                ddata = 80;
                                break;
                            case 2:
                                ddata = 100;
                                break;
                            case 3:
                                ddata = 120;
                                break;
                            default:
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                return;
                        }
                        p.phydata = (double)(ddata * 1000 / m_parent.rsense);
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

	        dval = (double)((double)(wVal*PhysicalRef)/(double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
	        if(fraction >= 0.5)
		        integer += 1;
	        else if(fraction <= -0.5)
		        integer -= 1;

	        return (double)integer;
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

            return (double)integer;
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

	        dval = (double)((double)(fVal*RegularRef)/(double)PhysicalRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
	        if(fraction>=0.5)
		        integer += 1;
	        if(fraction<=-0.5)
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
            UInt16 data = 0, mask;
            byte hi = 0, lo = 0, tmp = 0;
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
                mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                mask <<= regLow.startbit;
                data &= (UInt16)(~mask);
                data |= (UInt16)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data);
            }
            else
            {
                if (p.guid == ElementDefine.YFLDO33voffset)
                {
                    wVal <<= 4;
                    lo = SharedFormula.LoByte(wVal);
                    hi = SharedFormula.HiByte(wVal);
                    lo >>= 4;
                    hi <<= 4;

                    tmp = (byte)data;
                    mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                    mask <<= regLow.startbit;
                    data &= (UInt16)(~mask);
                    tmp |= lo;
                    WriteRegToImg(regLow.address,p.guid, tmp);

                    ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                    tmp = (byte)data;
                    mask = (UInt16)((1 << regHi.bitsnumber) - 1);
                    mask <<= regHi.startbit;
                    tmp &= (byte)(~mask);
                    tmp |= hi;
                    WriteRegToImg(regHi.address, p.guid, tmp);
                }
                else
                {
                    wVal <<= regLow.startbit;
                    lo = SharedFormula.LoByte(wVal);
                    hi = SharedFormula.HiByte(wVal);
                    hi <<= regHi.startbit;

                    tmp = (byte)data;
                    mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                    mask <<= regLow.startbit;
                    tmp &= (byte)(~mask);
                    tmp |= lo;
                    WriteRegToImg(regLow.address, p.guid, tmp);

                    ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                    tmp = (byte)data;
                    mask = (UInt16)((1 << regHi.bitsnumber) - 1);
                    mask <<= regHi.startbit;
                    tmp &= (byte)(~mask);
                    tmp |= hi;
                    WriteRegToImg(regHi.address,p.guid, tmp);
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
                case ElementDefine.YFLASHElement:
                    {
                        pval = parent.m_YFRegImg[reg].val;
                        ret = parent.m_YFRegImg[reg].err;
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
                case ElementDefine.YFLASHElement:
                    {
                        parent.m_YFRegImg[reg].val = value;
                        parent.m_YFRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
        private double ResistToTemp(double resist)
        {
            Int32 idx;
            for (idx = 0; idx < parent.m_ResistVals.Count; idx++)
            {
                if (parent.m_ResistVals[idx] <= resist)
                    break;
            }

            if(idx == 0)
                return parent.m_TempVals[0];
            else if (idx >= parent.m_ResistVals.Count)
                idx--;
            else if ((parent.m_ResistVals[idx] < resist) && (parent.m_ResistVals[idx - 1] > resist))
            {
                float slope = (float)((float)parent.m_TempVals[idx] - (float)parent.m_TempVals[idx - 1]) / (float)((float)parent.m_ResistVals[idx] - (float)parent.m_ResistVals[idx - 1]);

                return parent.m_TempVals[idx] - ((float)slope * (float)(parent.m_ResistVals[idx] - resist));
            }

            return parent.m_TempVals[idx];
        }

        private double TempToResist(double temp)
        {
            Int32 idx;
            for (idx = 0; idx < parent.m_TempVals.Count; idx++)
            {
                if (parent.m_TempVals[idx] >= temp)
                    break;
            }

            if(idx == 0)
                return parent.m_ResistVals[0];
            else if (idx >= parent.m_TempVals.Count)
                idx--;
            else if ((parent.m_TempVals[idx] > temp) && (parent.m_TempVals[idx - 1] < temp))
            {
                double slope = (double)((double)parent.m_ResistVals[idx] - (double)parent.m_ResistVals[idx - 1]) / (double)((double)parent.m_TempVals[idx] - (double)parent.m_TempVals[idx - 1]);
                return (double)((double)parent.m_ResistVals[idx] - (double)((double)slope * (double)((double)parent.m_TempVals[idx] - (double)temp)));
            }

            return parent.m_ResistVals[idx];
        }
        #endregion   
    }
}
