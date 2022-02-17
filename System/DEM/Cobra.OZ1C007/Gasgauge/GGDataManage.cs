using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ1C007.Gasgauge
{
    internal class GGDataManage
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private GGDeviceManage m_parent;
        public GGDeviceManage parent
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
            parent = (GGDeviceManage)pParent;
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
            double ddata = 0;
            int hdata = 0, ldata = 0;
            string str = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.SW_TEMP_POINT:
                    {
                        ddata = (UInt16)((double)(p.phydata * p.regref) / (double)p.phyref);
                        hdata = (int)(ddata / 100);
                        ldata = (int)(ddata - hdata * 100);
                        str = string.Format("{0:x2}{1:x2}", hdata, ldata);
                        if (!UInt16.TryParse(str, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out wdata))
                            wdata = 0;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
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
            short sdata = 0;
            UInt16 wdata = 0;
            Double ddata = 0;
            string str = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.CONST_DATA_FWEDITION:
                    {
                        p.sphydata = string.Format("0x{0:x8}",SharedFormula.MAKEDWORD
                            (SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[3],p.tsmbBuffer.bdata[2]),
                            SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[1],p.tsmbBuffer.bdata[0])));
                        p.phydata = SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[1], p.tsmbBuffer.bdata[0]);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.CONST_DATA_ATEINFOCRC:
                    {
                        p.sphydata = string.Format("0x{0:x2}-{1:x2}-{2:x2}-{3:x2}-{4:x2}-{5:x2}-{6:x2}-{7:x2}",
                            p.tsmbBuffer.bdata[7], p.tsmbBuffer.bdata[6], p.tsmbBuffer.bdata[5], p.tsmbBuffer.bdata[4],
                            p.tsmbBuffer.bdata[3], p.tsmbBuffer.bdata[2], p.tsmbBuffer.bdata[1], p.tsmbBuffer.bdata[0]);
                        p.phydata = SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[1], p.tsmbBuffer.bdata[0]);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.CONST_DATA_BAT_TABLE_VER:
                    {
                        p.sphydata = SharedFormula.HexToASCII(p.tsmbBuffer.bdata);
                        p.phydata = SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[1], p.tsmbBuffer.bdata[0]);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.EFUSE_PARAM_SIGN:
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.SW_TEMP_POINT:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        str = string.Format("{0:D2}{1:D2}", (byte)(wdata >> 8), (byte)wdata);
                        if (!UInt16.TryParse(str, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out wdata))
                            wdata = 0;
                        p.phydata = (double)((double)wdata * p.phyref / p.regref);
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
        private double Regular2Physical(short sVal, double RegularRef, double PhysicalRef)
        {
            double dval, integer, fraction;

            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
            return dval;
            /*
                        integer = Math.Truncate(dval);
                        fraction = (double)(dval - integer);
                        if (fraction >= 0.5)
                            integer += 1;
                        else if (fraction <= -0.5)
                            integer -= 1;

                        return (double)integer;*/
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

        #region EFUSE数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            { 
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.EFUSEElement:
                    {
                        pval = parent.m_InfoRegImg[reg].val;
                        ret = parent.m_InfoRegImg[reg].err;
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
                case ElementDefine.OperationElement:
                    {
                        parent.m_OpRegImg[reg].val = value;
                        parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.EFUSEElement:
                    {
                        parent.m_InfoRegImg[reg].val = value;
                        parent.m_InfoRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion
    }
}
