using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.RebaG
{
    class DEMDataManage
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

        #region 硬件模式下相关参数数据初始化
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
        }
        #endregion


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
            Int16 sdata = 0;
            float fval = 0;
            double resistor = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;

            ElementDefine.SECTION ec= ElementDefine.SECTION.CHARGER;
            switch (p.subsection)
            {
                case 2:
                    {
                        ec = ElementDefine.SECTION.MONITOR;
                        break;
                    }
                case 3:
                    {
                        ec = ElementDefine.SECTION.CHARGER;
                        break;
                    }
            }

            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                #region charger
                #endregion
                #region monitor
                #endregion
                #region charger
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_ILMT:
                    {
                        if (p.phydata == 0)
                            wdata = 0x00;
                        else if (p.phydata == 1)
                            wdata = 0x01;
                        else if (p.phydata == 2)
                            wdata = 0x02;
                        else if (p.phydata == 3)
                            wdata = 0x05;
                        else if (p.phydata == 4)
                            wdata = 0x06;
                        else if (p.phydata == 5)
                            wdata = 0x0a;
                        else if (p.phydata == 6)
                            wdata = 0x0d;
                        else if (p.phydata == 7)
                            wdata = 0x0f;

                        ret = WriteToRegImg(p, wdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                #endregion
                #region monitor
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        sdata = (short)Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        double Rsense = parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        fval = (float)(p.phydata * Rsense / (float)(1000));
                        sdata = (short)Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CAR:
                    {
                        double Rsense = parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        fval = (float)(p.phydata * Rsense / (float)(1000));
                        sdata = (short)Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        resistor = TempToResist(p.phydata);
                        fval = (float)((parent.etrx * 1000 * 1800) / (parent.pullupR * 1000));
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                #endregion
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                    {
                        parent.ModifyTemperatureConfig(p, true);
                        break;
                    }
                default:
                    {
                        wdata = (UInt16)((double)(p.phydata * p.regref) / (double)p.phyref);
                        ret = WriteToRegImg(p, wdata, ec);
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
            Int16 sdata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;

            ElementDefine.SECTION ec = ElementDefine.SECTION.CHARGER;
            switch (p.subsection)
            {
                case 2:
                    {
                        ec = ElementDefine.SECTION.MONITOR;
                        break;
                    }
                case 3:
                    {
                        ec = ElementDefine.SECTION.CHARGER;
                        break;
                    }
            }

            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                #region charger
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_ILMT:
                    {
                        ret = ReadFromRegImg(p, ref wdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        switch (wdata)
                        {
                            case 0x00: p.phydata = 0; break;
                            case 0x01: p.phydata = 1; break;
                            case 0x02: p.phydata = 2; break;
                            case 0x05: p.phydata = 3; break;
                            case 0x06: p.phydata = 4; break;
                            case 0x0a: p.phydata = 5; break;
                            case 0x0d: p.phydata = 6; break;
                            case 0x0f: p.phydata = 7; break;
                        }

                        break;
                    }
                #endregion

                #region monitor
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        double dval = Regular2Physical(sdata, p.regref, p.phyref);
                        double Rsense = parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        p.phydata = (dval * 1000.0) / Rsense;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CAR:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        double dval = Regular2Physical(sdata, p.regref, p.phyref);
                        double Rsense = parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        p.phydata = (dval * 1000.0) / Rsense;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        byte NTC_Type = 0;
                        float ext_therm = 0;
                        //float cur_constant = 0;
                        ret = GetNTCTypeFromImg(ref NTC_Type);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            WriteToRegImgError(p, ret);
                            break;
                        }

                        ret = ReadFromRegImg(p, ref wdata, ec);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);

                        if (NTC_Type == 0)
                        {
                            ext_therm = 100;
                            //cur_constant = 10;
                        }
                        else
                        {
                            ext_therm = 10;
                            //cur_constant = 100;
                        }

                        /*p.phydata = (double)p.phydata / cur_constant;
                        p.phydata = (double)(ext_therm * p.phydata) /(ext_therm - p.phydata);
                        p.phydata = (double)(p.phydata * m_parent.parent.pullupR * 1000 / (1800 - p.phydata));
                        p.phydata = parent.ResistToTemp(p.phydata);*/
                        if (IsCharging())
                        {
                            p.phydata = p.phydata * ext_therm / (1 - p.phydata / 1000);
                            p.phydata = ResistToTemp(p.phydata);
                        }
                        else
                        {
                            p.phydata = p.phydata * ext_therm * 1000 * parent.pullupR * 1000 / (1800 * ext_therm * 1000 - p.phydata * ext_therm * 1000 - p.phydata * parent.pullupR * 1000);
                            p.phydata = ResistToTemp(p.phydata);
                        }
                        break;
                    }
                #endregion

                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                    {
                        parent.ModifyTemperatureConfig(p, false);
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref wdata, ec);
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
        private UInt32 ReadFromRegImg(Parameter p, ref UInt16 pval, ElementDefine.SECTION ec)
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
                    ret = ReadRegFromImg(regLow.address, p.guid, ref lo, ec);
                    lo <<= (16 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadRegFromImg(regHi.address, p.guid, ref hi, ec);
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
        private UInt32 ReadSignedFromRegImg(Parameter p, ref short pval, ElementDefine.SECTION ec)
        {
            UInt16 wdata = 0, tr = 0;
            Int16 sdata;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref wdata, ec);
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
        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal, ElementDefine.SECTION ec)
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

            ret = ReadRegFromImg(regLow.address, p.guid, ref data, ec);
            if (regHi == null)
            {
                lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                lomask <<= regLow.startbit;
                data &= (UInt16)(~lomask);
                data |= (UInt16)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data, ec);
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
                WriteRegToImg(regLow.address, p.guid, ptmp, ec);

                ret |= ReadRegFromImg(regHi.address, p.guid, ref data, ec);
                himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                himask <<= regHi.startbit;
                ptmp = (UInt16)(data & ~himask);
                ptmp |= (UInt16)(phi << regHi.startbit);
                WriteRegToImg(regHi.address, p.guid, ptmp, ec);

            }

            return ret;
        }

        /// <summary>
        /// 写有符号数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <param name="pChip"></param>
        /// <returns></returns>
        private UInt32 WriteSignedToRegImg(Parameter p, Int16 sVal, ElementDefine.SECTION ec)
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

            return WriteToRegImg(p, wdata, ec);
        }

        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval, ElementDefine.SECTION ec)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        if (ec == ElementDefine.SECTION.CHARGER)
                        {
                            pval = parent.m_ChgOpRegImg[reg].val;
                            ret = parent.m_ChgOpRegImg[reg].err;
                        }
                        else if (ec == ElementDefine.SECTION.MONITOR)
                        {
                            pval = parent.m_MonOpRegImg[reg].val;
                            ret = parent.m_MonOpRegImg[reg].err;
                        }
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }

        private void WriteRegToImg(UInt16 reg, UInt32 guid, UInt16 value, ElementDefine.SECTION ec)
        {
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        if (ec == ElementDefine.SECTION.CHARGER)
                        {
                            parent.m_ChgOpRegImg[reg].val = value;
                            parent.m_ChgOpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        }
                        else if (ec == ElementDefine.SECTION.MONITOR)
                        {
                            parent.m_MonOpRegImg[reg].val = value;
                            parent.m_MonOpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        }
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



        private UInt32 GetNTCTypeFromImg(ref byte pval)
        {
            //(A150806)Francis, if not successful, read first to get its value
            byte yVal = 0;
            UInt32 u32Ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (this.parent.m_MonOpRegImg[0x30].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                u32Ret = parent.ChgReadOneByte(0x30, ref yVal);
                parent.m_MonOpRegImg[0x30].err = u32Ret;
                parent.m_MonOpRegImg[0x30].val = (UInt16)yVal;
            }
            //(E150806)
            pval = (byte)(this.parent.m_MonOpRegImg[0x30].val & 0x01);
            return this.parent.m_MonOpRegImg[0x30].err;
        }

        private bool IsCharging()
        {
            //(A150806)Francis, read VBus status everytime to sync with chip's Vbus status
            byte yVal = 0;
            UInt32 u32Ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //if (this.parent.m_charger_device.parent.parent.m_MonOpRegImg[0x40].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                u32Ret = parent.ChgReadOneByte(0x40, ref yVal);
                parent.m_MonOpRegImg[0x40].err = u32Ret;
                parent.m_MonOpRegImg[0x40].val = (UInt16)yVal;
            }
            //(E150806)

            return (this.parent.m_MonOpRegImg[0x40].val & 0x20) == 0x20;
        }
    }
}
