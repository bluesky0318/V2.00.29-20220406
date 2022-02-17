using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ26786
{
    internal class DEMDataManage
    {
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
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
            int index = 0;
            double ddata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CHARGE_CURRENT_0x14:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)wdata * p.phyref / p.regref) * 64;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CHARGE_VOL_0x15:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)wdata * p.phyref / p.regref) * 16;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_LEVEL1_ADAPTER_OCP_0x16:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)wdata * p.phyref / p.regref) * 512;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_LEVEL2_ADAPTER_OCP_0x17:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)wdata * p.phyref / p.regref) * 640;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Boost_Current_LMT_3B01:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ddata = (double)((double)wdata * p.phyref / p.regref);
                        p.phydata = (ddata * 10) + 40;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PSYS_Gain_3B08:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 1;
                                break;
                            case 1:
                                p.phydata = 0.5;
                                break;
                            case 2:
                                p.phydata = 0.25;
                                break;
                            case 3:
                                p.phydata = 2;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_BATMIN_TH_3B0B:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 0;
                                break;
                            case 1:
                                p.phydata = 5.6;
                                break;
                            case 2:
                                p.phydata = 6;
                                break;
                            case 3:
                                p.phydata = 6.4;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_BATLOW_TH_3B0E:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 57.5;
                                break;
                            case 1:
                                p.phydata = 62.5;
                                break;
                            case 2:
                                p.phydata = 67.5;
                                break;
                            case 3:
                                p.phydata = 72.5;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Deglitch_ForOCP1_3C00:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 0;
                                break;
                            case 1:
                                p.phydata = 0.1;
                                break;
                            case 2:
                                p.phydata = 0.5;
                                break;
                            case 3:
                                p.phydata = 1;
                                break;
                            case 4:
                                p.phydata = 5;
                                break;
                            case 5:
                                p.phydata = 10;
                                break;
                            case 6:
                                p.phydata = 15;
                                break;
                            case 7:
                                p.phydata = 20;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Deglitch_ForOCP2_3C03:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 0;
                                break;
                            case 1:
                                p.phydata = 10;
                                break;
                            case 2:
                                p.phydata = 100;
                                break;
                            case 3:
                                p.phydata = 300;
                                break;
                            case 4:
                                p.phydata = 500;
                                break;
                            case 5:
                                p.phydata = 750;
                                break;
                            case 6:
                                p.phydata = 1000;
                                break;
                            case 7:
                                p.phydata = 3000;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Normal_Deglitch_3C06:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 50;
                                break;
                            case 1:
                                p.phydata = 100;
                                break;
                            case 2:
                                p.phydata = 500;
                                break;
                            case 3:
                                p.phydata = 1000;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Duration_Time_3C08:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 50;
                                break;
                            case 1:
                                p.phydata = 100;
                                break;
                            case 2:
                                p.phydata = 500;
                                break;
                            case 3:
                                p.phydata = 1000;
                                break;
                            case 4:
                                p.phydata = 5000;
                                break;
                            case 5:
                                p.phydata = 10000;
                                break;
                            case 6:
                                p.phydata = 15000;
                                break;
                            case 7:
                                p.phydata = 20000;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_RAD_3D00:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 10;
                                break;
                            case 1:
                                p.phydata = 20;
                                break;
                            case 2:
                            case 3: 
                                p.phydata = 5;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PreCHG_3D04:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 192;
                                break;
                            case 1:
                                p.phydata = 128;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_RCH_3D08:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 10;
                                break;
                            case 1:
                                p.phydata = 20;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Fpwm_3D0B:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 900;
                                break;
                            case 1:
                                p.phydata = 600;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_IAD_Gain_3D0E:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 20;
                                break;
                            case 1:
                                p.phydata = 10;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VAD_3D0F:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        index = (int)(wdata * p.phyref / p.regref);
                        switch (index)
                        {
                            case 0:
                                p.phydata = 19;
                                break;
                            case 1:
                                p.phydata = 12;
                                break;
                        }
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_Adapter_Current_3F07:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)wdata * p.phyref / p.regref) * 256;
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

        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OpMapElement:
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
                case ElementDefine.OpMapElement:
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
