using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ89725
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
            if (parent.EFParamlist == null) return;
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
            if (pOVP_TH.phydata > pOVP_TH.dbPhyMax) pOVP_TH.phydata = pOVP_TH.dbPhyMax;
            if (pOVP_TH.phydata < pOVP_TH.dbPhyMin) pOVP_TH.phydata = pOVP_TH.dbPhyMin;
        }

        private void UpdateUVP(ref Parameter pUuvp_Hys)
        {
            Parameter pBAT_TYPE = new Parameter();
            Parameter param = null;
            switch (pUuvp_Hys.guid)
            {
                case ElementDefine.E_Uuvp_Hys:
                    pBAT_TYPE = parent.pE_BAT_TYPE;
                    param = parent.pE_Uuvp;
                    break;
                case ElementDefine.O_Uuvp_Hys:
                    pBAT_TYPE = parent.pO_BAT_TYPE;
                    param = parent.pO_Uuvp;
                    break;
            }
            int num = 16 - (int)param.phydata;
            if (num > 8) num = 8;
            int diff = pUuvp_Hys.itemlist.Count - num;
            if (pBAT_TYPE.phydata == 1)
            {
                if (diff > 0)
                {
                    for (int i = diff; i > 0; i--)
                    {
                        pUuvp_Hys.itemlist.RemoveAt(pUuvp_Hys.itemlist.Count - 1);
                        if (pUuvp_Hys.phydata >= pUuvp_Hys.itemlist.Count) pUuvp_Hys.phydata = (pUuvp_Hys.itemlist.Count - 1);
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
            else if (pBAT_TYPE.phydata == 0)
            {
                for (int i = pUuvp_Hys.itemlist.Count; i < 8; i++)
                {
                    pUuvp_Hys.itemlist.Add(((i + 1) * 100).ToString() + "mV");
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
                        if (pOVR.phydata >= pOVR.itemlist.Count) pOVR.phydata =(pOVR.itemlist.Count-1);
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
                        if (pOVR.phydata >= pOVR.itemlist.Count) pOVR.phydata = (pOVR.itemlist.Count-1);
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
                case ElementDefine.SUBTYPE.CONFIG_CELL:
                    wdata = (UInt16)((double)(p.phydata * p.regref) / (double)p.phyref);
                    wdata += 1;
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
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
                case ElementDefine.SUBTYPE.VDDA_TH_FOR_CTO:
                    dtmp = p.phydata - p.offset;
                    wdata = (UInt16)((double)(dtmp * p.regref) / (double)p.phyref);
                    if (wdata != 9)
                    {
                        wdata = 9;
                        p.phydata = 9.0;
                        p.errorcode = ElementDefine.IDS_ERR_DEM_ERROR_WRITE_VDDA_TH_FOR_CTO;
                    }
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
                case ElementDefine.SUBTYPE.CONFIG_CELL:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata == 0) wdata = 1;
                    p.phydata = wdata - 1;
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
                case ElementDefine.SUBTYPE.CONFIG_UVR_HYS:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata >= p.itemlist.Count) wdata = 0;
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = dtmp + p.offset;
                    break;
                case ElementDefine.SUBTYPE.VDDA_TH_FOR_CTO:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata != 9)
                    {
                        wdata = 9;
                        WriteToRegImg(p, wdata);
                        p.errorcode = ElementDefine.IDS_ERR_DEM_ERROR_READ_VDDA_TH_FOR_CTO;
                    }
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = dtmp + p.offset;
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
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        public UInt32 ReadFromVerifyImg(Parameter p, ref UInt16 pval)
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
                    ret = ReadVerifyFromImg(regLow.address, p.guid, ref lo);
                    lo <<= (16 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadVerifyFromImg(regHi.address, p.guid, ref hi);
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
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.EEPROMElement:
                    {
                        pval = parent.m_EEPROMRegImg[reg].val;
                        ret = parent.m_EEPROMRegImg[reg].err;
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }

        private UInt32 ReadVerifyFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.EEPROMElement:
                    {
                        pval = parent.m_EEPROMVerifyImg[reg].val;
                        ret = parent.m_EEPROMVerifyImg[reg].err;
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
                case ElementDefine.EEPROMElement:
                    {
                        parent.m_EEPROMRegImg[reg].val = value;
                        parent.m_EEPROMRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
