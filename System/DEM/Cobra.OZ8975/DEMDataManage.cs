using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ8975
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

        private void UpdateDOTE(ref Parameter pDOT_E)
        {
            Parameter pDOT = new Parameter();
            switch (pDOT_E.guid)
            {
                case ElementDefine.E_DOT_E:
                    pDOT = parent.pE_DOT_TH;
                    break;
                case ElementDefine.O_DOT_E:
                    pDOT = parent.pO_DOT_TH;
                    break;
            }

            if (pDOT_E.phydata == 1)                     //pDOT_E.phydata是1的情况下，DOT变化了，那肯定是在读芯片, pDOT.hexdata已经是准确的了
            {
                if (pDOT.hexdata < 1)
                {
                    pDOT_E.phydata = 1;
                }
                else
                {
                    pDOT_E.phydata = 0;
                }
            }
            else if (pDOT_E.phydata == 0)               //pDOT_E.phydata是0的情况下，DOT变化了，有可能是读芯片，也可能是UI操作
            {
                //如果是读芯片，那么就还是直接使用hexdata
                if (FromHexToPhy)
                {
                    if (pDOT.hexdata < 1)
                    {
                        pDOT_E.phydata = 1;
                    }
                    else
                    {
                        pDOT_E.phydata = 0;
                    }
                }
                //*/
                //如果是UI操作，那么就什么都不用做
            }
        }

        private void UpdateOVP(ref Parameter pOVP_TH)
        {
            Parameter pBAT_TYPE = new Parameter();
            switch (pOVP_TH.guid)
            {
                case ElementDefine.O_OVP_TH:
                    pBAT_TYPE = parent.pO_BAT_TYPE;
                    break;
                case ElementDefine.E_OVP_TH:
                    pBAT_TYPE = parent.pE_BAT_TYPE;
                    break;
            }
            if (pBAT_TYPE.phydata == 0)
            {
                pOVP_TH.offset = 3900;
                pOVP_TH.dbPhyMin = 4000;
                pOVP_TH.dbPhyMax = 4500;
            }
            else if (pBAT_TYPE.phydata == 1)
            {
                pOVP_TH.offset = 3400;
                pOVP_TH.dbPhyMin = 3500;
                pOVP_TH.dbPhyMax = 4000;
            }
        }

        private void UpdateUVP(ref Parameter pUuvp_Hys)
        {
            Parameter param = null;
            switch (pUuvp_Hys.guid)
            {
                case ElementDefine.E_Uuvp_Hys:
                    param = parent.pE_Uuvp;
                    break;
                case ElementDefine.O_Uuvp_Hys:
                    param = parent.pO_Uuvp;
                    break;
            }
            int num = 16 - (int)param.phydata;
            if (num > 8)
                num = 8;
            int diff = pUuvp_Hys.itemlist.Count - num;
            if (diff > 0)
            {
                for (int i = diff; i > 0; i--)
                {
                    pUuvp_Hys.itemlist.RemoveAt(pUuvp_Hys.itemlist.Count - 1);
                }
            }
            else if (diff < 0)
            {
                for (int i = -diff; i > 0; i--)
                {
                    pUuvp_Hys.itemlist.Add(((pUuvp_Hys.itemlist.Count + 1) * 100).ToString() + "mV");
                }
            }
        }

        private void UpdateOVR(ref Parameter pOVR)
        {
            Parameter pBAT_TYPE = new Parameter();
            Parameter pOVP = new Parameter();
            switch (pOVR.guid)
            {
                case ElementDefine.O_OVR_HYS:
                    pBAT_TYPE = parent.pO_BAT_TYPE;
                    pOVP = parent.pO_OVP_TH;
                    break;
                case ElementDefine.E_OVR_HYS:
                    pBAT_TYPE = parent.pE_BAT_TYPE;
                    pOVP = parent.pE_OVP_TH;
                    break;
            }
            if (pBAT_TYPE.phydata == 0)
            {
                if (pOVP.phydata >= 4050)
                {
                    if (!pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Add("400mV");
                    }
                }
                else
                {
                    if (pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Remove("400mV");
                    }
                }

            }
            else if (pBAT_TYPE.phydata == 1)
            {
                if (pOVP.phydata >= 3550)
                {
                    if (!pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Add("400mV");
                    }
                }
                else
                {
                    if (pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Remove("400mV");
                    }
                }
            }
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter pTarget)
        {
            if (pTarget.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;
            Parameter source = new Parameter();
            switch (pTarget.guid)
            {
                case ElementDefine.E_DOT_E:
                case ElementDefine.O_DOT_E:
                    UpdateDOTE(ref pTarget);
                    break;
                case ElementDefine.E_OVP_TH:
                case ElementDefine.O_OVP_TH:
                    UpdateOVP(ref pTarget);
                    break;
                case ElementDefine.O_OVR_HYS:
                case ElementDefine.E_OVR_HYS:
                    UpdateOVR(ref pTarget);
                    break;
                case ElementDefine.E_Uuvp_Hys:
                case ElementDefine.O_Uuvp_Hys:
                    UpdateUVP(ref pTarget);
                    break;
            }
            FromHexToPhy = false;
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
            double dtmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.OVP:
                    dtmp = p.phydata - p.offset;
                    wdata = (UInt16)((double)(dtmp * p.regref) / (double)p.phyref);
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
                case ElementDefine.SUBTYPE.EXPERT_OVP:
                    parent.ResetOvpOffset(ref p);
                    dtmp = p.phydata - p.offset;
                    wdata = (UInt16)((double)(dtmp * p.regref) / (double)p.phyref);
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
                case ElementDefine.SUBTYPE.DOT_TH:
                    Parameter pDOT_E = new Parameter();
                    switch (p.guid)
                    {
                        case ElementDefine.O_DOT_TH:
                            pDOT_E = parent.pO_DOT_E;
                            break;
                        case ElementDefine.E_DOT_TH:
                            pDOT_E = parent.pE_DOT_E;
                            break;
                    }
                    if (pDOT_E.phydata == 1)    //Disable
                    {
                        wdata = 0;
                    }
                    else if (pDOT_E.phydata == 0)   //Enable
                    {
                        wdata = (ushort)(p.phydata + 1);
                    }
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
                case ElementDefine.SUBTYPE.DUPLICATE_FIRST_ITEM:
                    wdata = (ushort)(p.phydata + 1);
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
                default:
                    dtmp = p.phydata - p.offset;
                    wdata = (UInt16)((double)(dtmp * p.regref) / (double)p.phyref);
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
            }
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
                case ElementDefine.SUBTYPE.DOT_TH:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata >= 1)
                        p.phydata = wdata - 1;
                    else
                        p.phydata = 0;
                    break;
                case ElementDefine.SUBTYPE.OVP:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata <= 0x0A) wdata = 0x0A;
                    if (wdata >= 0x3C) wdata = 0x3C;
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = dtmp + p.offset;
                    break;
                case ElementDefine.SUBTYPE.EXPERT_OVP:
                    parent.ResetOvpOffset(ref p);
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata <= 0x0A) wdata = 0x0A;
                    if (wdata >= 0x3C) wdata = 0x3C;
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = dtmp + p.offset;
                    break;
                case ElementDefine.SUBTYPE.DUPLICATE_FIRST_ITEM:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    if (dtmp > 0)
                        p.phydata = (dtmp - 1);
                    else
                        p.phydata = 0;
                    break;
                case ElementDefine.SUBTYPE.OV2_TH_DELTA:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if ((wdata == 0x06) | (wdata == 0x07)) wdata = 0x05;
                    p.phydata = (wdata * 50);
                    break;
                case ElementDefine.SUBTYPE.T1_TH_SEL:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if ((wdata == 0x00) | (wdata == 0x01)) wdata = 0x01;
                    p.phydata = (wdata * 5);
                    break;
                case ElementDefine.SUBTYPE.T2_TH_SEL:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if ((wdata == 0x00) | (wdata == 0x01)) wdata = 0x01;
                    p.phydata = ((wdata-1) * 5) + 45;
                    break;
                case ElementDefine.SUBTYPE.OTUT_DLY_BANK1:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    p.phydata = (wdata + 1) * 2;
                    break;
                case ElementDefine.SUBTYPE.DOTP_TH_SEL_BANK1:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata == 0) p.phydata = 0;
                    else if (wdata == 1) p.phydata = 70;
                    else if (wdata == 2) p.phydata = 80;
                    else p.phydata = 85;
                    break;
                case ElementDefine.SUBTYPE.UVP_TH_BANK1:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if ((wdata >= 0)&&(wdata <=3)) p.phydata = (wdata * 200)+1200;
                    else p.phydata = ((wdata - 4) * 100) + 1900;
                    break;
                case ElementDefine.SUBTYPE.UVR_HYS_BANK1:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata < 8) p.phydata = (wdata + 1)*100;
                    else p.phydata = 800;
                    break;
                case ElementDefine.SUBTYPE.OVR_HYS_BANK1:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    p.phydata = (wdata + 1) * 50;
                    break;
                default:
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
            FromHexToPhy = true;
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

        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region EFUSE数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.SectionMask)
            {
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.EFUSEMapElement:
                    {
                        pval = parent.m_MapRegImg[reg].val;
                        ret = parent.m_MapRegImg[reg].err;
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
                case ElementDefine.OperationElement:
                    {
                        parent.m_OpRegImg[reg].val = value;
                        parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.EFUSEMapElement:
                    {
                        parent.m_MapRegImg[reg].val = value;
                        parent.m_MapRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
    }
}
