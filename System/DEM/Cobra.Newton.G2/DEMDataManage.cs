using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.NewTon.G2
{
    public class DEMDataManage
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
            if (parent.MTPParamlist == null) return;
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
            Int16 sdata = 0;
            UInt16 wdata = 0;
            float fval = 0;
            double dval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        sdata = (short)Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = parent.GetINT25Ref(ref dval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }
                        fval = (float)((p.phydata - 23) * 2 + dval);
                        sdata = (short)Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_MTP_SPECIAL_DWORD:
                    {
                        byte badd = (byte)p.reglist["Low"].address;
                        UInt32 dwval = (UInt32)p.phydata;
                        parent.m_MTPSpecial_RegDic[badd].bdata[3] = (byte)(dwval >> 24);
                        parent.m_MTPSpecial_RegDic[badd].bdata[2] = (byte)(dwval >> 16);
                        parent.m_MTPSpecial_RegDic[badd].bdata[1] = (byte)(dwval >> 8);
                        parent.m_MTPSpecial_RegDic[badd].bdata[0] = (byte)(dwval);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_MTP_SPECIAL_HI_WORD:
                    {
                        byte badd = (byte)p.reglist["Low"].address;
                        fval = (float)p.phydata;
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        parent.m_MTPSpecial_RegDic[badd].bdata[1] = (byte)(wdata >> 8);
                        parent.m_MTPSpecial_RegDic[badd].bdata[0] = (byte)wdata;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG:
                    {
                        fval = (float)p.phydata;
                        sdata = (short)Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
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
            short sdata = 0;
            UInt16 wdata = 0;
            Double ddata = 0;
            UInt16 cell1_vadc_data = 0, cell2_vadc_data = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
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
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CELL_VOL:
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
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CELL2_VOL:
                        {
                            cell1_vadc_data = parent.m_OpRegImg[0x45].val;
                            cell2_vadc_data = parent.m_OpRegImg[0x4B].val;
                            ret = (parent.m_OpRegImg[0x45].err | parent.m_OpRegImg[0x4B].err);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            {
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                break;
                            }
                            sdata = (short)(cell2_vadc_data/2 + cell1_vadc_data/ 2);
                            p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                            break;
                        }
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                        {
                            float Current = 6;         //uA
                            ret = ReadFromRegImg(p, ref wdata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            {
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                break;
                            }

                            ddata = Regular2Physical(wdata, p.regref, p.phyref);     //Voltage
                            ddata = ddata * 1000 / Current;                                  //Rp
                            p.phydata = ResistToTemp(ddata);
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
                            ret = parent.GetINT25Ref(ref ddata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            {
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                break;
                            }
                            p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                            p.phydata = (p.phydata - ddata) / 2 + 23;
                            break;
                        }
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CUR:
                        {
                            ret = ReadSignedFromRegImg(p, ref sdata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            {
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                break;
                            }
                            ddata = Regular2Physical(sdata, p.regref, p.phyref);
                            p.phydata = ddata * 1000 * 1000 / m_parent.rsense;
                            break;
                        }
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_ISENS:
                        {
                            if (p.guid == 0x0007F700)
                            {
                                p.phydata = parent.m_isens_struct.isens_vadc_data;
                                break;
                            }
                            else if (p.guid == 0x0007F800)
                            {
                                p.phydata = parent.m_isens_struct.end_isens;
                                break;
                            }
                            else if (p.guid == ElementDefine.VirtualISENS)
                            {
                                ddata = Regular2Physical((short)parent.m_isens_struct.end_isens, p.regref, p.phyref);
                                p.phydata = ddata * 1000 * 1000 / m_parent.rsense;
                                break;
                            }
                            ret = ReadSignedFromRegImg(p, ref sdata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            {
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                break;
                            }
                            ddata = Regular2Physical(sdata, p.regref, p.phyref);
                            p.phydata = ddata * 1000 * 1000 / m_parent.rsense;
                            break;
                        }
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_MTP_SPECIAL_DWORD:
                        {
                            byte badd = (byte)p.reglist["Low"].address; 
                            p.phydata = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_MTPSpecial_RegDic[badd].bdata[0], parent.m_MTPSpecial_RegDic[badd].bdata[1]),
                                         SharedFormula.MAKEWORD(parent.m_MTPSpecial_RegDic[badd].bdata[2], parent.m_MTPSpecial_RegDic[badd].bdata[3]));
                            break;
                        }
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_MTP_SPECIAL_HI_WORD:
                        {
                            byte badd = (byte)p.reglist["Low"].address;
                            sdata = (short)SharedFormula.MAKEWORD(parent.m_MTPSpecial_RegDic[badd].bdata[0], parent.m_MTPSpecial_RegDic[badd].bdata[1]);
                            p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                            break;
                        }
                    case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG:
                        {
                            if (p.guid == ElementDefine.VirtualISENS)
                            {
                                p.phydata = Regular2Physical((short)parent.m_isens_struct.end_isens, p.regref, p.phyref);
                                return;
                            }
                            if(p.guid == ElementDefine.CellVoltage02)
                            {
                                cell1_vadc_data = parent.m_OpRegImg[0x45].val;
                                cell2_vadc_data = parent.m_OpRegImg[0x4B].val;
                                ret = (parent.m_OpRegImg[0x45].err | parent.m_OpRegImg[0x4B].err);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                {
                                    p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                    break;
                                }
                                sdata = (short)(cell2_vadc_data/2 + cell1_vadc_data/2);
                                p.phydata = (double)((double)sdata * p.phyref / p.regref);
                                return;
                            }
                            ret = ReadSignedFromRegImg(p, ref sdata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            {
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                break;
                            }
                            p.phydata = (double)((double)sdata * p.phyref / p.regref);
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
            catch (Exception e)
            {
                FolderMap.WriteFile(e.Message);
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
            double dval;
            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
            return dval;
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
            double dval;
            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
            return dval;
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
        public UInt32 ReadFromRegImg(Parameter p, ref UInt16 pval)
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

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EEPROMTRIMElement:
                    {
                        pval = parent.m_EpTrimRegImg[reg].val;
                        ret = parent.m_EpTrimRegImg[reg].err;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.I2CElement:
                    {
                        pval = parent.m_I2CRegImg[reg].val;
                        ret = parent.m_I2CRegImg[reg].err;
                        break;
                    }
                case ElementDefine.eFlashCtrlElement:
                    {
                        pval = parent.m_eFlashCtrlImage[reg].val;
                        ret = parent.m_eFlashCtrlImage[reg].err;
                        break;
                    }
                case ElementDefine.I2CRegistersElement:
                    {
                        pval = parent.m_I2CRegistersImage[reg].val;
                        ret = parent.m_I2CRegistersImage[reg].err;
                        break;
                    }
                case ElementDefine.TimerRegistersElement:
                    {
                        pval = parent.m_TimerRegistersImage[reg].val;
                        ret = parent.m_TimerRegistersImage[reg].err;
                        break;
                    }
                case ElementDefine.WDTRegistersElement:
                    {
                        pval = parent.m_WDTRegistersImage[reg].val;
                        ret = parent.m_WDTRegistersImage[reg].err;
                        break;
                    }
                case ElementDefine.UARTRegistersElement:
                    {
                        pval = parent.m_UARTRegistersImage[reg].val;
                        ret = parent.m_UARTRegistersImage[reg].err;
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
                case ElementDefine.EEPROMTRIMElement:
                    {
                        parent.m_EpTrimRegImg[reg].val = value;
                        parent.m_EpTrimRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        parent.m_OpRegImg[reg].val = value;
                        parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.I2CElement:
                    {
                        parent.m_I2CRegImg[reg].val = value;
                        parent.m_I2CRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.eFlashCtrlElement:
                    {
                        parent.m_eFlashCtrlImage[reg].val = value;
                        parent.m_eFlashCtrlImage[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.I2CRegistersElement:
                    {
                        parent.m_I2CRegistersImage[reg].val = value;
                        parent.m_I2CRegistersImage[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.TimerRegistersElement:
                    {
                        parent.m_TimerRegistersImage[reg].val = value;
                        parent.m_TimerRegistersImage[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.WDTRegistersElement:
                    {
                        parent.m_WDTRegistersImage[reg].val = value;
                        parent.m_WDTRegistersImage[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.UARTRegistersElement:
                    {
                        parent.m_UARTRegistersImage[reg].val = value;
                        parent.m_UARTRegistersImage[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
