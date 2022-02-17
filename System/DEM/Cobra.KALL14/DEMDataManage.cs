using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL14
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

        bool FromHexToPhy = false;
        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {

            parent = (DEMDeviceManage)pParent;
            if (parent.EFParamlist == null) return;
        }

        private void UpdateCTOE(ref Parameter pCTO_E)
        {
            Parameter pCTO = new Parameter();
            switch (pCTO_E.guid)
            {
                case ElementDefine.OCTO_E:
                    pCTO = parent.pOCTO;
                    break;
                case ElementDefine.ECTO_E:
                    pCTO = parent.pECTO;
                    break;
            }

            if (pCTO_E.phydata == 0)                     //pCTO_E.phydata是0的情况下，CTO变化了，那肯定是在读芯片, pCTO.hexdata已经是准确的了
            {
                if (pCTO.hexdata == 0)
                {
                    pCTO_E.phydata = 0;
                }
                else
                {
                    pCTO_E.phydata = 1;
                }
            }
            else if (pCTO_E.phydata == 1)               //pCTO_E.phydata是1的情况下，CTO变化了，有可能是读芯片，也可能是UI操作
            {
                //如果是读芯片，那么就还是直接使用hexdata
                if (FromHexToPhy)
                {
                    if (pCTO.hexdata == 0)
                    {
                        pCTO_E.phydata = 0;
                    }
                    else
                    {
                        pCTO_E.phydata = 1;
                    }
                }
                //*/
                //如果是UI操作，那么就什么都不用做
            }
        }

        private void UpdateCTO(ref Parameter pCTO)
        {
            Parameter pCTO_E = new Parameter();
            switch (pCTO.guid)
            {
                case ElementDefine.OCTO:
                    pCTO_E = parent.pOCTO_E;
                    break;
                case ElementDefine.ECTO:
                    pCTO_E = parent.pECTO_E;
                    break;
            }
            if (pCTO_E.phydata == 0)
            {
                //pCTO.phydata = 0;
            }
            else if (pCTO_E.phydata == 1)
            {
                //pCTO.phydata = 1;
            }
        }

        private void UpdateThType(ref Parameter pTH)
        {
            Parameter pEnable = new Parameter();
            double tmp = 0;
            ushort wdata = 0;
            switch (pTH.guid)
            {
                case ElementDefine.OCUT:
                    pEnable = parent.pOCUT_E;
                    break;
                case ElementDefine.ECUT:
                    pEnable = parent.pECUT_E;
                    break;
                case ElementDefine.ODUT:
                    pEnable = parent.pODUT_E;
                    break;
                case ElementDefine.EDUT:
                    pEnable = parent.pEDUT_E;
                    break;
            }
            if (pEnable.phydata == 0)
            {
                wdata = 0;
                tmp = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
                pTH.phydata = Volt2Temp(tmp);
            }
            else if (pEnable.phydata == 1)
            {
                wdata = 0;
                tmp = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
                tmp = Volt2Temp(tmp);
                if (pTH.phydata == tmp)
                {
                    wdata = 1;
                    tmp = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
                    pTH.phydata = Volt2Temp(tmp);
                }
            }
        }

        private void UpdateEnableType(ref Parameter pEnable)
        {
            Parameter source = new Parameter();
            ushort wdata = 0;
            double tmp = 0;
            switch (pEnable.guid)
            {
                case ElementDefine.OCUT_E:
                    source = parent.pOCUT;
                    break;
                case ElementDefine.ECUT_E:
                    source = parent.pECUT;
                    break;
                case ElementDefine.ODUT_E:
                    source = parent.pODUT;
                    break;
                case ElementDefine.EDUT_E:
                    source = parent.pEDUT;
                    break;
            }

            wdata = 0;
            tmp = Hex2Volt(wdata, source.offset, source.regref, source.phyref);
            tmp = Volt2Temp(tmp);
            if (source.phydata == tmp)
            {
                pEnable.phydata = 0;
            }
            else
                pEnable.phydata = 1;
        }

        private void UpdateHysRange(ref Parameter pHys)
        {
            UInt16 wdata = 0;
            //UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Parameter pTH = new Parameter();

            double V1 = 0, V2 = 0, T1 = 0, T2 = 0, deltaV = 0, deltaT = 0;
            byte HEX = 0;
            double maxDeltaT = 0, minDeltaT = 9999;
            ushort maxDeltaTHex = 0, minDeltaTHex = 0;
            bool isEnabled = true;
            int sign = 1;

            switch (pHys.guid)
            {
                case ElementDefine.ECUT_H:
                    pTH = parent.pECUT;
                    isEnabled = parent.isECUTEnabled;
                    sign = -1;
                    break;
                case ElementDefine.ECOT_H:
                    pTH = parent.pECOT;
                    sign = 1;
                    break;
                case ElementDefine.EDOT_H:
                    pTH = parent.pEDOT;
                    sign = 1;
                    break;
                case ElementDefine.EDUT_H:
                    pTH = parent.pEDUT;
                    isEnabled = parent.isEDUTEnabled;
                    sign = -1;
                    break;
                case ElementDefine.OCUT_H:
                    pTH = parent.pOCUT;
                    isEnabled = parent.isOCUTEnabled;
                    sign = -1;
                    break;
                case ElementDefine.OCOT_H:
                    pTH = parent.pOCOT;
                    sign = 1;
                    break;
                case ElementDefine.ODOT_H:
                    pTH = parent.pODOT;
                    sign = 1;
                    break;
                case ElementDefine.ODUT_H:
                    pTH = parent.pODUT;
                    isEnabled = parent.isODUTEnabled;
                    sign = -1;
                    break;
            }

            if (pTH == null || pTH.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;

            #region 从Physical算出hex

            if (!isEnabled)       //如果Disabled
            {
                wdata = 0;                  //写0禁止此功能
            }
            else                            //如果Enabled
            {
                V1 = Temp2Volt(pTH.phydata);
                wdata = Volt2Hex(V1, pTH.offset, pTH.regref, pTH.phyref);
            }
            #endregion

            V1 = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
            T1 = Volt2Temp(V1);
            for (HEX = 0; HEX <= pHys.dbHexMax; HEX++)
            {
                deltaV = Hex2Volt(HEX, pHys.offset, pHys.regref, pHys.phyref);
                V2 = V1 + sign * deltaV;
                T2 = Volt2Temp(V2);
                deltaT = sign * (T1 - T2);

                if (deltaT > maxDeltaT)
                {
                    maxDeltaT = deltaT;
                    maxDeltaTHex = HEX;
                }
                if (deltaT < minDeltaT)
                {
                    minDeltaT = deltaT;
                    minDeltaTHex = HEX;
                }
            }
            //Issue554-Leon-S
            int temp = (int)(minDeltaT * 10);
            minDeltaT = temp;
            minDeltaT /= 10;
            temp = (int)(maxDeltaT * 10);
            maxDeltaT = temp;
            maxDeltaT /= 10;
            maxDeltaT += 0.1;
            //Issue554-Leon-E
            pHys.dbPhyMin = minDeltaT;
            pHys.dbPhyMax = maxDeltaT;
        }

        private void UpdateEOC(ref Parameter EOC)
        {
            Parameter EOC_E = new Parameter();
            double tmp = 0;
            ushort wdata = 0;
            switch (EOC.guid)
            {
                case ElementDefine.OEOC:
                    EOC_E = parent.pOEOC_E;
                    break;
                case ElementDefine.EEOC:
                    EOC_E = parent.pEEOC_E;
                    break;
            }
            if (EOC_E.phydata == 0)
            {
                wdata = 0;
                tmp = Hex2Volt(wdata, EOC.offset, EOC.regref, EOC.phyref);
                EOC.phydata = tmp;
            }
            else if (EOC_E.phydata == 1)
            {
                wdata = 0;
                tmp = Hex2Volt(wdata, EOC.offset, EOC.regref, EOC.phyref);
                if (EOC.phydata == tmp)
                {
                    wdata = 1;
                    tmp = Hex2Volt(wdata, EOC.offset, EOC.regref, EOC.phyref);
                    EOC.phydata = tmp;
                }
            }
        }

        private void UpdateEOCE(ref Parameter EOC_E)
        {
            Parameter EOC = new Parameter();
            ushort wdata = 0;
            double tmp = 0;
            switch (EOC_E.guid)
            {
                case ElementDefine.OEOC_E:
                    EOC = parent.pOEOC;
                    break;
                case ElementDefine.EEOC_E:
                    EOC = parent.pEEOC;
                    break;
            }

            wdata = 0;
            tmp = Hex2Volt(wdata, EOC.offset, EOC.regref, EOC.phyref);
            if (EOC.phydata == tmp)
            {
                EOC_E.phydata = 0;
            }
            else
                EOC_E.phydata = 1;
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="pTarget"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter pTarget)
        {
            if (pTarget.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;
            switch (pTarget.guid)
            {
                case ElementDefine.OCUT_E:
                case ElementDefine.ECUT_E:
                case ElementDefine.ODUT_E:
                case ElementDefine.EDUT_E:
                    UpdateEnableType(ref pTarget);
                    break;
                case ElementDefine.ECUT_H:
                case ElementDefine.ECOT_H:
                case ElementDefine.EDOT_H:
                case ElementDefine.EDUT_H:
                case ElementDefine.OCUT_H:
                case ElementDefine.OCOT_H:
                case ElementDefine.ODOT_H:
                case ElementDefine.ODUT_H:
                    UpdateHysRange(ref pTarget);
                    break;
                case ElementDefine.ECUT:
                case ElementDefine.EDUT:
                case ElementDefine.OCUT:
                case ElementDefine.ODUT:
                    UpdateThType(ref pTarget);
                    break;
                case ElementDefine.ECTO:
                case ElementDefine.OCTO:
                    UpdateCTO(ref pTarget);
                    break;
                case ElementDefine.OCTO_E:
                case ElementDefine.ECTO_E:
                    UpdateCTOE(ref pTarget);
                    break;
                case ElementDefine.OEOC:
                case ElementDefine.EEOC:
                    UpdateEOC(ref pTarget);
                    break;
                case ElementDefine.OEOC_E:
                case ElementDefine.EEOC_E:
                    UpdateEOCE(ref pTarget);
                    break;
            }
            FromHexToPhy = false;
            return;
        }

        private double Volt2Temp(double volt)
        {
            double Thm_PullupRes = 100, temp;
            volt = (double)((volt * (Thm_PullupRes) * 1000) / (Thm_PullupRes * 20.3 - volt));
            temp = ResistToTemp(volt);
            return temp;
        }
        private double Temp2Volt(double temp)
        {

            double Thm_PullupRes = 100, volt;

            volt = TempToResist(temp);
            volt = volt * Thm_PullupRes * 20.3 / (volt + Thm_PullupRes * 1000);   //20是电流
            return volt;
        }

        private ushort Volt2Hex(double volt, double offset, double regref, double phyref)
        {
            ushort hex;
            volt -= offset;
            volt = volt * regref / phyref;
            hex = (UInt16)Math.Round(volt);
            return hex;
        }
        private double Hex2Volt(ushort hex, double offset, double regref, double phyref)
        {
            double volt;
            volt = (double)((double)hex * phyref / regref);
            volt += offset;//voltage
            return volt;
        }

        private void CalcHysHex(ref Parameter pHys)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Parameter pTH = new Parameter();
            int sign = 1;

            switch (pHys.guid)
            {
                case ElementDefine.ECUT_H:
                    pTH = parent.pECUT;
                    sign = 1;
                    break;
                case ElementDefine.ECOT_H:
                    pTH = parent.pECOT;
                    sign = -1;
                    break;
                case ElementDefine.EDOT_H:
                    pTH = parent.pEDOT;
                    sign = -1;
                    break;
                case ElementDefine.EDUT_H:
                    pTH = parent.pEDUT;
                    sign = 1;
                    break;
                case ElementDefine.OCUT_H:
                    pTH = parent.pOCUT;
                    sign = 1;
                    break;
                case ElementDefine.OCOT_H:
                    pTH = parent.pOCOT;
                    sign = -1;
                    break;
                case ElementDefine.ODOT_H:
                    pTH = parent.pODOT;
                    sign = -1;
                    break;
                case ElementDefine.ODUT_H:
                    pTH = parent.pODUT;
                    sign = 1;
                    break;
            }

            double V1 = Temp2Volt(pTH.phydata);
            double T2 = pTH.phydata + sign * pHys.phydata;
            double V2 = Temp2Volt(T2);
            double deltaV = Math.Abs(V1 - V2);
            wdata = Volt2Hex(deltaV, pHys.offset, pHys.regref, pHys.phyref);
            ret = WriteToRegImg(pHys, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                WriteToRegImgError(pHys, ret);
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
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                #region fixed
                case ElementDefine.SUBTYPE.VOLTAGE:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.SUBTYPE.EXT_TEMP://温度是只读参数，这里用不到
                    {

                        break;
                    }
                case ElementDefine.SUBTYPE.INT_TEMP:
                    {
                        break;
                    }
                case ElementDefine.SUBTYPE.EXT_TEMP_TABLE:
                case ElementDefine.SUBTYPE.INT_TEMP_REFER:
                    {
                        m_parent.ModifyTemperatureConfig(p, true);
                        break;
                    }
                case ElementDefine.SUBTYPE.CURRENT:
                    {
                        break;
                    }
                #endregion
                case ElementDefine.SUBTYPE.XXT_TH:
                    {
                        double volt = Temp2Volt(p.phydata);
                        wdata = Volt2Hex(volt, p.offset, p.regref, p.phyref);

                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.SUBTYPE.XXT_H:
                    {
                        CalcHysHex(ref p);
                        break;
                    }
                case ElementDefine.SUBTYPE.CTO:
                    {
                        Parameter pCTO_E = new Parameter();
                        switch (p.guid)
                        {
                            case ElementDefine.OCTO:
                                pCTO_E = parent.pOCTO_E;
                                break;
                            case ElementDefine.ECTO:
                                pCTO_E = parent.pECTO_E;
                                break;
                        }
                        if (pCTO_E.phydata == 0)
                        {
                            wdata = 0;
                        }
                        else if (pCTO_E.phydata == 1)
                        {
                            wdata = (ushort)(p.phydata + 1);
                        }
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                default:
                    {
                        double tmp = p.phydata - p.offset;
                        tmp = tmp * p.regref;
                        tmp = tmp / p.phyref;
                        double res = tmp % 1;
                        if (res < 0.99)
                            wdata = (UInt16)(tmp);
                        else
                        {
                            wdata = (UInt16)(tmp);
                            wdata += 1;
                        }
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
            }
        }

        private void CalcHysPhy(ref Parameter pHys)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Parameter pTH = new Parameter();
            //bool isPositive = true;
            int sign = 1;

            switch (pHys.guid)
            {
                case ElementDefine.ECUT_H:
                    pTH = parent.pECUT;
                    //isPositive = false;
                    sign = -1;
                    break;
                case ElementDefine.ECOT_H:
                    pTH = parent.pECOT;
                    //isPositive = true;
                    sign = 1;
                    break;
                case ElementDefine.EDOT_H:
                    pTH = parent.pEDOT;
                    //isPositive = true;
                    sign = 1;
                    break;
                case ElementDefine.EDUT_H:
                    pTH = parent.pEDUT;
                    //isPositive = false;
                    sign = -1;
                    break;
                case ElementDefine.OCUT_H:
                    pTH = parent.pOCUT;
                    //isPositive = false;
                    sign = -1;
                    break;
                case ElementDefine.OCOT_H:
                    pTH = parent.pOCOT;
                    //isPositive = true;
                    sign = 1;
                    break;
                case ElementDefine.ODOT_H:
                    pTH = parent.pODOT;
                    //isPositive = true;
                    sign = 1;
                    break;
                case ElementDefine.ODUT_H:
                    pTH = parent.pODUT;
                    //isPositive = false;
                    sign = -1;
                    break;
            }

            ret = ReadFromRegImg(pTH, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                pTH.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                pHys.phydata = (pHys.dbPhyMax - pHys.dbPhyMin) / 2 + pHys.dbPhyMin; //不让p报越界错误
                return;
            }
            double V1 = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
            double T1 = Volt2Temp(V1);


            ret = ReadFromRegImg(pHys, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                pHys.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                return;
            }
            //p.phydata = (double)((double)wdata * p.phyref / p.regref);
            //p.phydata += 10;
            double deltaV = Hex2Volt(wdata, pHys.offset, pHys.regref, pHys.phyref);//deltaV
            //if (!isPositive)
            //    deltaV = -deltaV;

            double V2 = sign * deltaV + V1;

            double T2 = Volt2Temp(V2);

            double deltaT = Math.Abs(T1 - T2);

            pHys.phydata = deltaT;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            UInt16 wdata = 0;
            double dtmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                #region fixed
                case ElementDefine.SUBTYPE.VOLTAGE:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.SUBTYPE.CURRENT:
                    {
                        double Rsense = parent.etrx;
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        short stmp = (short)wdata;
                        stmp = (short)(stmp + (short)2);
                        stmp >>= 2;
                        dtmp = stmp;
                        dtmp = dtmp * 62.5 / Rsense;
                        p.phydata = -dtmp;
                        break;
                    }
                case ElementDefine.SUBTYPE.EXT_TEMP:
                    {
                        double R1 = 100;  //KOhm
                        double Current = 20.3;         //uA    //Issue
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        dtmp = Regular2Physical(wdata, p.regref, p.phyref);     //Voltage
                        dtmp = dtmp / Current;                                  //Rp
                        if(dtmp >= R1)    //Issue572-Leon
                            dtmp = 99999999;
                        else
                        {
                            dtmp = (dtmp * 1000.0 * R1 * 1000.0) / (R1 * 1000 - dtmp * 1000);
                        }
                        p.phydata = ResistToTemp(dtmp);
                        break;
                    }
                case ElementDefine.SUBTYPE.INT_TEMP:
                    {
                        break;
                    }
                case ElementDefine.SUBTYPE.INT_TEMP_REFER:
                case ElementDefine.SUBTYPE.EXT_TEMP_TABLE:
                    {
                        m_parent.ModifyTemperatureConfig(p, false);
                        break;
                    }
                #endregion
                #region XXT_TH
                case ElementDefine.SUBTYPE.XXT_TH:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        dtmp = Hex2Volt(wdata, p.offset, p.regref, p.phyref);
                        p.phydata = Volt2Temp(dtmp);
                        break;
                    }
                #endregion
                case ElementDefine.SUBTYPE.XXT_H:
                    {
                        CalcHysPhy(ref p);
                        break;
                    }
                case ElementDefine.SUBTYPE.CTO:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if(wdata != 0)
                            p.phydata = wdata - 1;
                        else
                            p.phydata = 0;
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
                        dtmp = (double)((double)wdata * p.phyref / p.regref);
                        p.phydata = dtmp + p.offset;
                        break;
                    }
            }
            FromHexToPhy = true;
        }

        #region General functions
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

        #region EFuse数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.SectionMask)
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
            switch (guid & ElementDefine.SectionMask)
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
                if ((ElementDefine.SUBTYPE)p.subtype == ElementDefine.SUBTYPE.EXT_TEMP_TABLE)
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
                if ((ElementDefine.SUBTYPE)p.subtype == ElementDefine.SUBTYPE.EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }

            return SharedFormula.TempToResist(temp, m_TempVals, m_ResistVals);
        }
        #endregion
        #endregion
    }
}
