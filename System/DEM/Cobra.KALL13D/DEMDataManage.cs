using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL13D
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
            if (parent.EpParamlist == null) return;
        }

        #region Update parameter from UI
        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter p)
        {
            if (p.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;
            switch (p.guid)
            {
                case ElementDefine.EPROM_ovp_th:
                case ElementDefine.EPROM_uvp_th:
                case ElementDefine.EPROM_doc1p_th:
                case ElementDefine.EPROM_cocp_th:
                case ElementDefine.EPROM_ub_cell_th:
                case ElementDefine.EPROM_cell_open_th:
                case ElementDefine.EPROM_0v_chg_disable_th:
                case ElementDefine.EPROM_eoc_th:
                case ElementDefine.Op_ovp_th:
                case ElementDefine.Op_uvp_th:
                case ElementDefine.Op_doc1p_th:
                case ElementDefine.Op_cocp_th:
                case ElementDefine.Op_ub_cell_th:
                case ElementDefine.Op_cell_open_th:
                case ElementDefine.Op_0v_chg_disable_th:
                case ElementDefine.Op_eoc_th:
                    UpdateEnableType(ref p);
                    break;
                case ElementDefine.EPROM_cotr_hys:
                case ElementDefine.EPROM_cutr_hys:
                case ElementDefine.EPROM_dotr_hys:
                case ElementDefine.EPROM_dutr_hys:
                case ElementDefine.Op_cotr_hys:
                case ElementDefine.Op_cutr_hys:
                case ElementDefine.Op_dotr_hys:
                case ElementDefine.Op_dutr_hys:
                    UpdateHysRange(ref p);
                    break;
                case ElementDefine.EPROM_cut_th:
                case ElementDefine.EPROM_dut_th:
                case ElementDefine.Op_cut_th:
                case ElementDefine.Op_dut_th:
                case ElementDefine.EPROM_cot_th:
                case ElementDefine.EPROM_dot_th:
                case ElementDefine.Op_cot_th:
                case ElementDefine.Op_dot_th:
                    UpdateThType(ref p);
                    break;
            }
            return;
        }

        private void UpdateEnableType(ref Parameter pTH)
        {
            Parameter source = new Parameter();
            switch (pTH.guid)
            {
                case ElementDefine.EPROM_ovp_th:
                    source = parent.pEOV_E;
                    break;
                case ElementDefine.EPROM_uvp_th:
                    source = parent.pEUV_E;
                    break;
                case ElementDefine.EPROM_doc1p_th:
                    source = parent.pEDOC1_E;
                    break;
                case ElementDefine.EPROM_cocp_th:
                    source = parent.pECOC_E;
                    break;
                case ElementDefine.EPROM_ub_cell_th:
                    source = parent.pEUB_E;
                    break;
                case ElementDefine.EPROM_cell_open_th:
                    source = parent.pECOT_E;
                    break;
                case ElementDefine.EPROM_0v_chg_disable_th:
                    source = parent.pE0V_Charge_Prohibit_E;
                    break;
                case ElementDefine.EPROM_eoc_th:
                    source = parent.pEEOC_E;
                    break;
                case ElementDefine.Op_ovp_th:
                    source = parent.pOOV_E;
                    break;
                case ElementDefine.Op_uvp_th:
                    source = parent.pOUV_E;
                    break;
                case ElementDefine.Op_doc1p_th:
                    source = parent.pODOC1_E;
                    break;
                case ElementDefine.Op_cocp_th:
                    source = parent.pOCOC_E;
                    break;
                case ElementDefine.Op_ub_cell_th:
                    source = parent.pOUB_E;
                    break;
                case ElementDefine.Op_cell_open_th:
                    source = parent.pOCOT_E;
                    break;
                case ElementDefine.Op_0v_chg_disable_th:
                    source = parent.pO0V_Charge_Prohibit_E;
                    break;
                case ElementDefine.Op_eoc_th:
                    source = parent.pOEOC_E;
                    break;
            }
            if (source.phydata == 0)
                pTH.phydata = 0;
            else
                pTH.phydata = 1;
        }

        private void UpdateHysRange(ref Parameter pHys)
        {
            Parameter pTH = new Parameter();
            double V1 = 0, V2 = 0, T1 = 0, T2 = 0, deltaV = 0, deltaT = 0;
            byte HEX = 0;
            double maxDeltaT = 0, minDeltaT = 9999;
            ushort maxDeltaTHex = 0, minDeltaTHex = 0;
            int sign = 1;

            switch (pHys.guid)
            {
                case ElementDefine.EPROM_cutr_hys:
                    pTH = parent.pECUT;
                    sign = -1;
                    break;
                case ElementDefine.EPROM_cotr_hys:
                    pTH = parent.pECOT;
                    sign = 1;
                    break;
                case ElementDefine.EPROM_dotr_hys:
                    pTH = parent.pEDOT;
                    sign = 1;
                    break;
                case ElementDefine.EPROM_dutr_hys:
                    pTH = parent.pEDUT;
                    sign = -1;
                    break;
                case ElementDefine.Op_cutr_hys:
                    pTH = parent.pOCUT;
                    sign = -1;
                    break;
                case ElementDefine.Op_cotr_hys:
                    pTH = parent.pOCOT;
                    sign = 1;
                    break;
                case ElementDefine.Op_dotr_hys:
                    pTH = parent.pODOT;
                    sign = 1;
                    break;
                case ElementDefine.Op_dutr_hys:
                    pTH = parent.pODUT;
                    sign = -1;
                    break;
            }

            if (pTH == null || pTH.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;

            T1 = pTH.phydata;
            V1 = TempToVol(T1);
            for (HEX = 0; HEX <= pHys.dbHexMax; HEX++)
            {
                deltaV = Hex2Volt(HEX, pHys.offset, pHys.regref, pHys.phyref);
                V2 = V1 + sign * deltaV;
                T2 = VolToTemp(V2);
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
            if (pHys.phydata > pHys.dbPhyMax) pHys.phydata = pHys.dbPhyMax;
            if (pHys.phydata < pHys.dbPhyMin) pHys.phydata = pHys.dbPhyMin;
        }

        private void UpdateThType(ref Parameter pTH)
        {
            Parameter pEnable = new Parameter();
            double tmp = 0;
            ushort wdata = 0;
            switch (pTH.guid)
            {
                case ElementDefine.Op_cot_th:
                    pEnable = parent.pOCOT_E;
                    break;
                case ElementDefine.EPROM_cot_th:
                    pEnable = parent.pECOT_E;
                    break;
                case ElementDefine.Op_dot_th:
                    pEnable = parent.pODOT_E;
                    break;
                case ElementDefine.EPROM_dot_th:
                    pEnable = parent.pEDOT_E;
                    break;
                case ElementDefine.Op_cut_th:
                    pEnable = parent.pOCUT_E;
                    break;
                case ElementDefine.EPROM_cut_th:
                    pEnable = parent.pECUT_E;
                    break;
                case ElementDefine.Op_dut_th:
                    pEnable = parent.pODUT_E;
                    break;
                case ElementDefine.EPROM_dut_th:
                    pEnable = parent.pEDUT_E;
                    break;
            }
            if (pEnable.phydata == 0)
            {
                wdata = 0;
                tmp = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
                pTH.phydata = VolToTemp(tmp);
            }
            else if (pEnable.phydata == 1)
            {
                wdata = 2;
                tmp = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
                tmp = VolToTemp(tmp);
                if (pTH.phydata == tmp)
                {
                    wdata = 3;
                    tmp = Hex2Volt(wdata, pTH.offset, pTH.regref, pTH.phyref);
                    pTH.phydata = VolToTemp(tmp);
                }
            }
            pTH.hexdata = wdata;
            if (pTH.phydata > pTH.dbPhyMax) pTH.phydata = pTH.dbPhyMax;
            if (pTH.phydata < pTH.dbPhyMin) pTH.phydata = pTH.dbPhyMin;
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
                case ElementDefine.EPROM_cutr_hys:
                    pTH = parent.pECUT;
                    sign = -1;
                    break;
                case ElementDefine.EPROM_cotr_hys:
                    pTH = parent.pECOT;
                    sign = 1;
                    break;
                case ElementDefine.EPROM_dotr_hys:
                    pTH = parent.pEDOT;
                    sign = 1;
                    break;
                case ElementDefine.EPROM_dutr_hys:
                    pTH = parent.pEDUT;
                    sign = -1;
                    break;
                case ElementDefine.Op_cutr_hys:
                    pTH = parent.pOCUT;
                    sign = -1;
                    break;
                case ElementDefine.Op_cotr_hys:
                    pTH = parent.pOCOT;
                    sign = 1;
                    break;
                case ElementDefine.Op_dotr_hys:
                    pTH = parent.pODOT;
                    sign = 1;
                    break;
                case ElementDefine.Op_dutr_hys:
                    pTH = parent.pODUT;
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
            double T1 = VolToTemp(V1);

            ret = ReadFromRegImg(pHys, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                pHys.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                return;
            }
            double deltaV = Hex2Volt(wdata, pHys.offset, pHys.regref, pHys.phyref);
            double V2 = sign * deltaV + V1;
            double T2 = VolToTemp(V2);
            double deltaT = Math.Abs(T1 - T2);
            pHys.phydata = deltaT;
        }

        private void CalcHysHex(ref Parameter pHys)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Parameter pTH = new Parameter();
            int sign = 1;

            switch (pHys.guid)
            {
                case ElementDefine.EPROM_cutr_hys:
                    pTH = parent.pECUT;
                    //isPositive = false;
                    sign = 1;
                    break;
                case ElementDefine.EPROM_cotr_hys:
                    pTH = parent.pECOT;
                    //isPositive = true;
                    sign = -1;
                    break;
                case ElementDefine.EPROM_dotr_hys:
                    pTH = parent.pEDOT;
                    //isPositive = true;
                    sign = -1;
                    break;
                case ElementDefine.EPROM_dutr_hys:
                    pTH = parent.pEDUT;
                    //isPositive = false;
                    sign = 1;
                    break;
                case ElementDefine.Op_cutr_hys:
                    pTH = parent.pOCUT;
                    //isPositive = false;
                    sign = 1;
                    break;
                case ElementDefine.Op_cotr_hys:
                    pTH = parent.pOCOT;
                    //isPositive = true;
                    sign = -1;
                    break;
                case ElementDefine.Op_dotr_hys:
                    pTH = parent.pODOT;
                    //isPositive = true;
                    sign = -1;
                    break;
                case ElementDefine.Op_dutr_hys:
                    pTH = parent.pODUT;
                    //isPositive = false;
                    sign = 1;
                    break;
            }

            double V1 = TempToVol(pTH.phydata);
            double T2 = pTH.phydata + sign * pHys.phydata;
            double V2 = TempToVol(T2);
            double deltaV = Math.Abs(V1 - V2);
            wdata = Volt2Hex(deltaV, pHys.offset, pHys.regref, pHys.phyref);
            ret = WriteToRegImg(pHys, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                WriteToRegImgError(pHys, ret);
        }

        private void CalTHHexZero(Parameter pTH) //When read back hex equal zero
        {
            Parameter pEnable = new Parameter();
            switch (pTH.guid)
            {
                case ElementDefine.Op_cot_th:
                    pEnable = parent.pOCOT_E;
                    break;
                case ElementDefine.EPROM_cot_th:
                    pEnable = parent.pECOT_E;
                    break;
                case ElementDefine.Op_dot_th:
                    pEnable = parent.pODOT_E;
                    break;
                case ElementDefine.EPROM_dot_th:
                    pEnable = parent.pEDOT_E;
                    break;
                case ElementDefine.Op_cut_th:
                    pEnable = parent.pOCUT_E;
                    break;
                case ElementDefine.EPROM_cut_th:
                    pEnable = parent.pECUT_E;
                    break;
                case ElementDefine.Op_dut_th:
                    pEnable = parent.pODUT_E;
                    break;
                case ElementDefine.EPROM_dut_th:
                    pEnable = parent.pEDUT_E;
                    break;
            }
            if (pTH.hexdata == 0)
                pEnable.phydata = 0;
            else
                pEnable.phydata = 1;
        }

        private void CalTHPhyZero(Parameter pTH) //When write zero
        {
            Parameter pEnable = new Parameter();
            switch (pTH.guid)
            {
                case ElementDefine.Op_cot_th:
                    pEnable = parent.pOCOT_E;
                    break;
                case ElementDefine.EPROM_cot_th:
                    pEnable = parent.pECOT_E;
                    break;
                case ElementDefine.Op_dot_th:
                    pEnable = parent.pODOT_E;
                    break;
                case ElementDefine.EPROM_dot_th:
                    pEnable = parent.pEDOT_E;
                    break;
                case ElementDefine.Op_cut_th:
                    pEnable = parent.pOCUT_E;
                    break;
                case ElementDefine.EPROM_cut_th:
                    pEnable = parent.pECUT_E;
                    break;
                case ElementDefine.Op_dut_th:
                    pEnable = parent.pODUT_E;
                    break;
                case ElementDefine.EPROM_dut_th:
                    pEnable = parent.pEDUT_E;
                    break;
            }
            if (pEnable.phydata == 0)
                pTH.hexdata = 0;
        }

        #endregion

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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CELLNUM:
                    {
                        if (p.phydata > 0x04) //Define illegal value
                            wdata = 0x04;
                        else
                            wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TH:
                    {
                        CalTHPhyZero(p);
                        if (p.hexdata == 0)
                            wdata = 0;
                        else
                        {
                            fval = (float)TempToVol(p.phydata);
                            fval -= (float)p.offset;
                            wdata = Physical2Regular(fval, p.regref, p.phyref);
                        }
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_HYS:
                    CalcHysHex(ref p);
                    break;
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        fval = (float)((p.phydata - 25) * 4.0 + 1170);
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
            p.hexdata = wdata;
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
            Double ddata = 0;
            Parameter relate_param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CELLNUM:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if (wdata > 0x04) //Define illegal value
                        {
                            p.phydata = 0x04;
                            if (p.guid == ElementDefine.EPROM_CellNumber)
                                p.errorcode = ElementDefine.IDS_ERR_DEM_CELLNUMBER_OVERRANGE;
                        }
                        else
                            p.phydata = (float)wdata;
                        break;
                    }
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DFET_INCHG_CTRL:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if (wdata > 2) wdata = 0;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TH:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.hexdata = wdata;
                        CalTHHexZero(p);
                        if (wdata == 0) break;
                        ddata = Regular2Physical(wdata, p.regref, p.phyref);
                        ddata += p.offset;
                        p.phydata = VolToTemp(ddata);
                        if (p.phydata > p.dbPhyMax) p.phydata = p.dbPhyMax;
                        if (p.phydata < p.dbPhyMin) p.phydata = p.dbPhyMin;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_HYS:
                    {
                        CalcHysPhy(ref p);
                        if (p.phydata > p.dbPhyMax) p.phydata = p.dbPhyMax;
                        if (p.phydata < p.dbPhyMin) p.phydata = p.dbPhyMin;
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
                        p.phydata = (double)((ddata - 1170) / 4.0 + 25.0);
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
                wVal &= (UInt16)lomask;
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
                case ElementDefine.EPROMElement:
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
                case ElementDefine.EPROMElement:
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

        public double VolToTemp(double resist)
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

        public double TempToVol(double temp)
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
        #endregion   
    }
}
