using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.LGBigsur
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
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            Int32 sdWdata = 0;
            UInt32 dWdata = 0;
            Double ddata = 0;
            string tmp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DWORD:
                    {
                        ret = ReadFromRegImg(p, ref dWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.sphydata = String.Format("0x{0:X8}", (long)dWdata);
                        p.phydata = dWdata;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref dWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        ddata = ((int)(dWdata - 2730)) / 10.0;
                        p.sphydata = String.Format("{0:F1}", ddata);
                        p.phydata = ddata;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DATE:
                    {
                        ret = ReadFromRegImg(p, ref dWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.sphydata = SharedFormula.UInt32ToData(dWdata);
                        p.phydata = dWdata;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING:
                    {
                        byte[] buf = new byte[p.tsmbBuffer.bdata[0]];
                        Array.Copy(p.tsmbBuffer.bdata, 1, buf, 0, buf.Length);
                        p.sphydata = SharedFormula.HexToASCII(buf);
                        p.phydata = 0.0;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGNED:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.sphydata = String.Format("{0:F1}", Regular2Physical(sdWdata, p.offset, p.regref, p.phyref));
                        p.phydata = Regular2Physical(sdWdata, p.offset, p.regref, p.phyref);
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref dWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.sphydata = String.Format("{0:F1}", Regular2Physical(dWdata, p.offset, p.regref, p.phyref));
                        p.phydata = Regular2Physical(dWdata, p.offset, p.regref, p.phyref);
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
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(Int32 wVal, double Offset, double RegularRef, double PhysicalRef)
        {
            double dval;

            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
            dval += Offset;
            return dval;
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(UInt32 wVal, double Offset, double RegularRef, double PhysicalRef)
        {
            double dval;

            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);
            dval += Offset;
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
        private UInt32 ReadFromRegImg(Parameter p, ref UInt32 pval)
        {
            byte bcmd = 0;
            byte[] bArray = new byte[4];
            UInt32 data = 0;
            Reg regLow = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.SBSElement:
                    {
                        #region  SBS Element
                        switch ((ElementDefine.COBRA_DATA_LEN)p.tsmbBuffer.length)
                        {
                            case ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES:
                                {
                                    bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                                    foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                    {
                                        if (dic.Key.Equals("Low"))
                                        {
                                            regLow = dic.Value;
                                            //data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[0], p.tsmbBuffer.bdata[1]), SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[2], p.tsmbBuffer.bdata[3]));
                                            if (!parent.m_HwMode_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                                            data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_HwMode_Dic[bcmd].bdata[0], parent.m_HwMode_Dic[bcmd].bdata[1]), SharedFormula.MAKEWORD(parent.m_HwMode_Dic[bcmd].bdata[2], parent.m_HwMode_Dic[bcmd].bdata[3]));
                                            data <<= (32 - regLow.bitsnumber - regLow.startbit); //align with left
                                            data >>= (32 - regLow.bitsnumber);
                                        }
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                        pval = data;
                        break;
                        #endregion
                    }
                case ElementDefine.LogElement:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                Array.Copy(parent.logAreaArray,regLow.address,bArray,0,regLow.bitsnumber > bArray.Length ? bArray.Length : regLow.bitsnumber);
                                data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
                            }
                        }
                        pval = data;
                        break;
                    }
            }
            return ret;
        }

        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref Int32 pval)
        {
            UInt32 dwdata = 0;
            Int32 sdwdata;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref dwdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            sdwdata = (Int32)dwdata;
            pval = sdwdata;
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
                case ElementDefine.MTPElement:
                    {
                        pval = parent.m_MTPRegImg[reg].val;
                        ret = parent.m_MTPRegImg[reg].err;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        pval = parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                case ElementDefine.VirtualElement:
                    {
                        pval = parent.m_VirtualRegImg[reg].val;
                        ret = parent.m_VirtualRegImg[reg].err;
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
                case ElementDefine.MTPElement:
                    {
                        parent.m_MTPRegImg[reg].val = value;
                        parent.m_MTPRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
