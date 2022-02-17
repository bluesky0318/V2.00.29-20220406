using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.SeaguIIPD
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
            UInt32 CRC = 0;
            if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;

            switch (p.guid)
            {
                case ElementDefine.UserConfig_REF_CV:
                    UpdateREFCV();
                    break;
                case ElementDefine.UserConfig_REF_CC:
                    UpdateREFCC();
                    break;
                case ElementDefine.UserConfig_CHANNEL6:
                    UpdateChannel6();
                    break;
                case ElementDefine.UserConfig_CHANNEL7:
                    UpdateChannel7();
                    break;
            }
            return;
        }

        #region From SeaguIIPDJr
        private void UpdateREFCV()
        {
            if (parent.pREFCV.phydata > 21000.0)
                parent.pREFCV.sMessage = string.Format("Careful,the value is bigger than 21V");
            else
                parent.pREFCV.sMessage = string.Empty;
        }

        private void UpdateREFCC()
        {
            double dlsb = 0;
            UInt16 ohex = 0;
            int OPCS1 = 0, OPCS2 = 0;

            switch ((int)parent.pUCSA_GAIN_SEL.phydata)
            {
                case 0:
                    OPCS1 = 25; OPCS2 = 25;
                    break;
                case 1:
                    OPCS1 = 50; OPCS2 = 25;
                    break;
                case 2:
                    OPCS1 = 25; OPCS2 = 50;
                    break;
                case 3:
                    OPCS1 = 50; OPCS2 = 50;
                    break;
            }
            ohex = (UInt16)(parent.pREFCC.phydata / parent.pREFCC.phyref);
            if (parent.pUOTG.phydata > 0)
                dlsb = parent.pREFCC.phyref = 2.40176 / (parent.pURcs2.phydata * OPCS2) * (50 / OPCS2) * 1000;
            else
                dlsb = parent.pREFCC.phyref = 2.40176 / (parent.pURcs1.phydata * OPCS1) * (50 / OPCS1) * 1000;
            parent.pREFCC.dbPhyMax = dlsb * 1023;
            parent.pREFCC.phydata = ohex * dlsb;
            parent.pREFCC.sMessage = string.Format("Step:1LSB={0:F1}mA", dlsb);
        }

        private void UpdateChannel6()
        {
            double dlsb = 0;
            UInt16 ohex = 0;
            int OPCS1 = 0, OPCS2 = 0;

            switch ((int)parent.pUCSA_GAIN_SEL.phydata)
            {
                case 0:
                    OPCS1 = 25; OPCS2 = 25;
                    break;
                case 1:
                    OPCS1 = 50; OPCS2 = 25;
                    break;
                case 2:
                    OPCS1 = 25; OPCS2 = 50;
                    break;
                case 3:
                    OPCS1 = 50; OPCS2 = 50;
                    break;
            }
            ohex = (UInt16)(parent.pCHANNEL6.phydata / parent.pCHANNEL6.phyref);
            dlsb = parent.pCHANNEL6.phyref = 0.6 / parent.pURcs1.phydata / OPCS1 * 1000;
            parent.pCHANNEL6.dbPhyMax = dlsb * 4095;
            parent.pCHANNEL6.phydata = ohex * dlsb;
            parent.pCHANNEL6.sMessage = string.Format("Step:1LSB={0:F1}mA", dlsb);
        }


        private void UpdateChannel7()
        {
            double dlsb = 0;
            UInt16 ohex = 0;
            int OPCS1 = 0, OPCS2 = 0;

            switch ((int)parent.pUCSA_GAIN_SEL.phydata)
            {
                case 0:
                    OPCS1 = 25; OPCS2 = 25;
                    break;
                case 1:
                    OPCS1 = 50; OPCS2 = 25;
                    break;
                case 2:
                    OPCS1 = 25; OPCS2 = 50;
                    break;
                case 3:
                    OPCS1 = 50; OPCS2 = 50;
                    break;
            }
            ohex = (UInt16)(parent.pCHANNEL7.phydata / parent.pCHANNEL7.phyref);
            dlsb = parent.pCHANNEL7.phyref = 0.6 / parent.pURcs2.phydata / OPCS2 * 1000;
            parent.pCHANNEL7.dbPhyMax = dlsb * 4095;
            parent.pCHANNEL7.phydata = ohex * dlsb;
            parent.pCHANNEL7.sMessage = string.Format("Step:1LSB={0:F1}mA", dlsb);
        }
        #endregion

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            UInt32 wdata = 0;
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
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
            Int32 sdata = 0;
            UInt32 wdata = 0;
            double dtmp = 0.0;
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        dtmp = Regular2Physical(wdata, p.regref, p.phyref); //Vt
                        p.phydata = (dtmp - 414) / 1.81 - 40.0;

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
        /// 转换Physical -> Hex
        /// </summary>
        /// <param name="fVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private UInt32 Physical2Regular(double fVal, double RegularRef, double PhysicalRef)
        {
            UInt32 wval;
            double dval, integer, fraction;

            dval = (double)((double)(fVal * RegularRef) / (double)PhysicalRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            if (fraction <= -0.5)
                integer -= 1;
            wval = (UInt32)integer;

            return wval;
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(Int32 sVal, double RegularRef, double PhysicalRef)
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
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(UInt32 wVal, double RegularRef, double PhysicalRef)
        {
            double dval;
            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
            return dval;
        }

        /// <summary>
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadFromRegImg(Parameter p, ref UInt32 pval)
        {
            UInt32 lo = 0;
            Reg regLow = null;
            COBRA_HWMode_Reg hw_Reg = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.BBCTRLSystemElement:
                case ElementDefine.PDCTRLExpertElement:
                case ElementDefine.ARMExpertElement:
                case ElementDefine.APBExpertElement:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                if (!parent.m_OpImage_Dic.ContainsKey(regLow.u32Address)) continue;
                                hw_Reg = parent.m_OpImage_Dic[regLow.u32Address];
                                ret = hw_Reg.err;
                                lo = hw_Reg.wval;
                                lo <<= (32 - regLow.bitsnumber - regLow.startbit); //align with left
                            }
                        }
                        break;
                    }
                default:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                ret = ReadRegFromImg(regLow.address, p.guid, ref lo);
                                lo <<= (32 - regLow.bitsnumber - regLow.startbit); //align with left
                            }
                        }
                        break;
                    }
            }
            lo >>= (32 - regLow.bitsnumber); //align with right
            pval = lo;
            p.u32hexdata = lo;
            return ret;
        }

        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref Int32 pval)
        {
            int tr = 0;
            UInt32 wdata = 0;
            Int32 sdata;
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
                tr = (UInt16)(32 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt16)(32 - regLow.bitsnumber);

            wdata <<= tr;
            sdata = (Int32)wdata;
            sdata = (Int32)(sdata / (1 << tr));

            pval = sdata;
            return ret;
        }

        public UInt32 WriteToRegImg(Parameter p, UInt32 wVal)
        {
            Reg regLow = null;
            COBRA_HWMode_Reg hw_Reg = null;
            UInt32 data = 0, lomask = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            p.u32hexdata = wVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;
            }
            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.BBCTRLSystemElement:
                case ElementDefine.PDCTRLExpertElement:
                case ElementDefine.ARMExpertElement:
                case ElementDefine.APBExpertElement:
                    {
                        if (!parent.m_OpImage_Dic.ContainsKey(regLow.u32Address)) break;
                        hw_Reg = parent.m_OpImage_Dic[regLow.u32Address];
                        ret = hw_Reg.err;
                        data = hw_Reg.wval;
                        break;
                    }
                default:
                    {

                        ret = ReadRegFromImg(regLow.address, p.guid, ref data);
                        break;
                    }
            }
            lomask = (UInt32)((1L << regLow.bitsnumber) - 1);
            lomask <<= regLow.startbit;
            data &= (UInt32)(~lomask);
            data |= (UInt32)(wVal << regLow.startbit);
            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.BBCTRLSystemElement:
                case ElementDefine.PDCTRLExpertElement:
                case ElementDefine.ARMExpertElement:
                case ElementDefine.APBExpertElement:
                    {
                        if (!parent.m_OpImage_Dic.ContainsKey(regLow.u32Address)) break;
                        parent.m_OpImage_Dic[regLow.u32Address].wval = data;
                        break;
                    }
                default:
                    {
                        WriteRegToImg(regLow.address, p.guid, data);
                        break;
                    }
            }
            return ret;
        }
        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt32 pval)
        {
            UInt32 offset = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if(guid == ElementDefine.VirtualSWCRC)
            {
                pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_OTPRegImg[reg * 4].val, (byte)parent.m_OTPRegImg[reg * 4 + 1].val),
                                            SharedFormula.MAKEWORD((byte)parent.m_OTPRegImg[reg * 4 + 2].val, (byte)parent.m_OTPRegImg[reg * 4 + 3].val));
                ret = (parent.m_OTPRegImg[reg * 4].err | parent.m_OTPRegImg[reg * 4 + 1].err | parent.m_OTPRegImg[reg * 4 + 2].err | parent.m_OTPRegImg[reg * 4 + 3].err);
            }
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EEPROMTRIMElement:
                    {
                        offset = (ElementDefine.EEPROM_TRIM_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                    SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.OTPTRIMElement:
                case ElementDefine.OTPExpertElement:
                    {
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_OTPRegImg[reg * 4].val, (byte)parent.m_OTPRegImg[reg * 4 + 1].val),
                                                    SharedFormula.MAKEWORD((byte)parent.m_OTPRegImg[reg * 4 + 2].val, (byte)parent.m_OTPRegImg[reg * 4 + 3].val));
                        ret = (parent.m_OTPRegImg[reg * 4].err | parent.m_OTPRegImg[reg * 4 + 1].err | parent.m_OTPRegImg[reg * 4 + 2].err | parent.m_OTPRegImg[reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port1SystemElement:
                    {
                        offset = (ElementDefine.EEPROM_P1SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port2SystemElement:
                    {
                        offset = (ElementDefine.EEPROM_P2SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port1BuckBoostElement:
                    {
                        offset = (ElementDefine.EEPROM_P1BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port2BuckBoostElement:
                    {
                        offset = (ElementDefine.EEPROM_P2BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port1PDElement:
                    {
                        offset = (ElementDefine.EEPROM_P1PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port2PDElement:
                    {
                        offset = (ElementDefine.EEPROM_P2PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port1PD2Element:
                    {
                        offset = (ElementDefine.EEPROM_P1PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                case ElementDefine.Port2PD2Element:
                    {
                        offset = (ElementDefine.EEPROM_P2PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 1].val),
                                                                            SharedFormula.MAKEWORD((byte)parent.m_EFRegImg[offset + reg * 4 + 2].val, (byte)parent.m_EFRegImg[offset + reg * 4 + 3].val));
                        ret = (parent.m_EFRegImg[offset + reg * 4].err | parent.m_EFRegImg[offset + reg * 4 + 1].err | parent.m_EFRegImg[offset + reg * 4 + 2].err | parent.m_EFRegImg[offset + reg * 4 + 3].err);
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }
        private void WriteRegToImg(UInt16 reg, UInt32 guid, UInt32 value)
        {
            UInt32 offset = 0;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EEPROMTRIMElement:
                    {
                        offset = (ElementDefine.EEPROM_TRIM_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.OTPTRIMElement:
                case ElementDefine.OTPExpertElement:
                    {
                        parent.m_OTPRegImg[reg * 4].val = (byte)(value);
                        parent.m_OTPRegImg[reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_OTPRegImg[reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_OTPRegImg[reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_OTPRegImg[reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_OTPRegImg[reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_OTPRegImg[reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_OTPRegImg[reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port1SystemElement:
                    {
                        offset = (ElementDefine.EEPROM_P1SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port2SystemElement:
                    {
                        offset = (ElementDefine.EEPROM_P2SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port1BuckBoostElement:
                    {
                        offset = (ElementDefine.EEPROM_P1BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port2BuckBoostElement:
                    {
                        offset = (ElementDefine.EEPROM_P2BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port1PDElement:
                    {
                        offset = (ElementDefine.EEPROM_P1PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port2PDElement:
                    {
                        offset = (ElementDefine.EEPROM_P2PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port1PD2Element:
                    {
                        offset = (ElementDefine.EEPROM_P1PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.Port2PD2Element:
                    {
                        offset = (ElementDefine.EEPROM_P2PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                        parent.m_EFRegImg[offset + reg * 4].val = (byte)(value);
                        parent.m_EFRegImg[offset + reg * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 1].val = (byte)(value >> 8);
                        parent.m_EFRegImg[offset + reg * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 2].val = (byte)(value >> 16);
                        parent.m_EFRegImg[offset + reg * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        parent.m_EFRegImg[offset + reg * 4 + 3].val = (byte)(value >> 24);
                        parent.m_EFRegImg[offset + reg * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
