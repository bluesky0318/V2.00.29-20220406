using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL16H
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
        public void UpdateEpParamItemList(Parameter pTarget)
        {
            if (pTarget.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL) return;
            Parameter source = new Parameter();
            switch (pTarget.guid)
            {
                case ElementDefine.YFLASH_COV_TH:
                case ElementDefine.YFLASH_MAP_COV_TH:
                    UpdateCOVTh(ref pTarget);
                    break;
                case ElementDefine.YFLASH_COV_DLY:
                case ElementDefine.YFLASH_CUV_DLY:
                case ElementDefine.YFLASH_MAP_COV_DLY:
                case ElementDefine.YFLASH_MAP_CUV_DLY:
                    UpdateCOVUVDLY(ref pTarget);
                    break;
                case ElementDefine.YFLASH_COVR_HYS:
                case ElementDefine.YFLASH_MAP_COVR_HYS:
                    UpdateCOVR(ref pTarget);
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
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
            UInt16 wdata = 0;
            double ddata = 0.0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            
            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                default:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ddata = (double)((double)wdata * p.phyref / p.regref);
                        p.phydata = ddata + p.offset;
                        break;
                    }
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

        #region 数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.YFLASHElement:
                    {
                        pval = parent.m_YFLASHRegImg[reg].val;
                        ret = parent.m_YFLASHRegImg[reg].err;
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
                        parent.m_YFLASHRegImg[reg].val = value;
                        parent.m_YFLASHRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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

        #region parameter update functions
        private void UpdateCOVTh(ref Parameter pCOV_TH)
        {
            Parameter pBAT_TYPE = new Parameter();
            switch (pCOV_TH.guid)
            {
                case ElementDefine.YFLASH_COV_TH:
                    pBAT_TYPE = parent.pYFLASH_BAT_TYPE;
                    break;
                case ElementDefine.YFLASH_MAP_COV_TH:
                    pBAT_TYPE = parent.pYFLASH_MAP_BAT_TYPE;
                    break;
            }
            if (pBAT_TYPE.phydata == 0)
            {
                pCOV_TH.offset = 3900;
                pCOV_TH.dbPhyMin = 4000;
                pCOV_TH.dbPhyMax = 4500;
            }
            else if (pBAT_TYPE.phydata == 1)
            {
                pCOV_TH.offset = 3400;
                pCOV_TH.dbPhyMin = 3500;
                pCOV_TH.dbPhyMax = 4000;
            }
        }
        private void UpdateCOVR(ref Parameter pCOVR)
        {
            Parameter pBAT_TYPE = new Parameter();
            Parameter pOVP = new Parameter();
            switch (pCOVR.guid)
            {
                case ElementDefine.YFLASH_COVR_HYS:
                    pBAT_TYPE = parent.pYFLASH_BAT_TYPE;
                    pOVP = parent.pYFLASH_COV_TH;
                    break;
                case ElementDefine.YFLASH_MAP_COVR_HYS:
                    pBAT_TYPE = parent.pYFLASH_MAP_BAT_TYPE;
                    pOVP = parent.pYFLASH_MAP_COV_TH;
                    break;
            }
            if (pBAT_TYPE.phydata == 0)
            {
                if (pOVP.phydata >= 4050)
                {
                    if (!pCOVR.itemlist.Contains("400mV"))
                        pCOVR.itemlist.Add("400mV");
                }
                else
                {
                    if (pCOVR.itemlist.Contains("400mV"))
                        pCOVR.itemlist.Remove("400mV");
                }

            }
            else if (pBAT_TYPE.phydata == 1)
            {
                if (pOVP.phydata >= 3550)
                {
                    if (!pCOVR.itemlist.Contains("400mV"))
                        pCOVR.itemlist.Add("400mV");
                }
                else
                {
                    if (pCOVR.itemlist.Contains("400mV"))
                        pCOVR.itemlist.Remove("400mV");
                }
            }
        }

        private void UpdateCOVUVDLY(ref Parameter pCOVUVDLY)
        {
            string str = String.Empty;
            Parameter pCOVUV_SCAN = new Parameter();
            switch (pCOVUVDLY.guid)
            {
                case ElementDefine.YFLASH_COV_DLY:
                case ElementDefine.YFLASH_CUV_DLY:
                    pCOVUV_SCAN = parent.pYFLASH_COVUV_SCAN;
                    break;
                case ElementDefine.YFLASH_MAP_COV_DLY:
                case ElementDefine.YFLASH_MAP_CUV_DLY:
                    pCOVUV_SCAN = parent.pYFLASH_MAP_COVUV_SCAN;
                    break;
            }
            pCOVUVDLY.itemlist.Clear();
            for (int i = 0; i < 32; i++)
            {
                if (pCOVUV_SCAN.phydata == 0)
                    str = string.Format("{0:D}ms", 125 * (i + 1));
                else
                    str = string.Format("{0:D}ms", 250 * (i + 1));
                pCOVUVDLY.itemlist.Add(str);
            }
            pCOVUVDLY.itemlist = pCOVUVDLY.itemlist;
        }
        #endregion
    }
}
