using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.SequoiaSBS
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
            byte[] atmp = null;
            UInt32 dWdata = 0;
            Double ddata = 0;
            Double rdata = 0;
            string tmp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DHG_CHG_Threhold:
                    {
                        ret = GetRsenseMain(ref rdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return;

                        ddata = p.phydata * rdata / 1000.0;
                        dWdata = Physical2Regular(ddata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2M:
                    {
                        ret = GetRsenseMain(ref rdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return;

                        ddata = p.phydata * rdata / 1000.0;
                        dWdata = Physical2Regular(ddata, p.offset, p.regref, p.phyref) + 1;
                        ret = WriteToRegImg(p, dWdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DWORD:
                    {
                        dWdata = Physical2Regular(p.phydata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_TEMP:
                    {
                        ddata = p.phydata * 10 + 2730;
                        dWdata = Physical2Regular(ddata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DATE:
                    {
                        dWdata = Physical2Regular(p.phydata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING:
                    {
                        atmp = SharedFormula.StringToASCII(p.sphydata);
                        if (atmp.Length >= ElementDefine.MaxBytesNum)
                        {
                            Array.Copy(atmp, 0, p.tsmbBuffer.bdata, 1, (ElementDefine.MaxBytesNum - 1));
                            p.tsmbBuffer.bdata[0] = (byte)ElementDefine.MaxBytesNum - 1;
                        }
                        else
                        {
                            Array.Copy(atmp, 0, p.tsmbBuffer.bdata, 1, atmp.Length);
                            p.tsmbBuffer.bdata[0] = (byte)atmp.Length;
                        }
                        p.phydata = 0.0;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGNED:
                    {
                        dWdata = Physical2Regular(p.phydata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                default:
                    {
                        dWdata = Physical2Regular(p.phydata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
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
            Int32 sdWdata = 0;
            UInt32 dWdata = 0;
            Double ddata = 0;
            Double rdata = 0;
            string tmp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DHG_CHG_Threhold:
                    {
                        ret = ReadFromRegImg(p, ref dWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        ret = GetRsenseMain(ref rdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return;

                        ddata = Regular2Physical(dWdata, p.offset, p.regref, p.phyref);
                        p.phydata = ddata * 1000.0 / rdata;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOC2M:
                    {
                        ret = ReadFromRegImg(p, ref dWdata);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        ret = GetRsenseMain(ref rdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return;

                        ddata = Regular2Physical((dWdata - 1), p.offset, p.regref, p.phyref);
                        p.phydata = ddata * 1000.0 / rdata;
                        break;
                    }
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
        private UInt32 Physical2Regular(double dVal, double Offset, double RegularRef, double PhysicalRef)
        {
            UInt32 wval;
            double dwval, integer, fraction;

            dwval = (dVal - Offset);
            dwval = (double)((double)(dwval * RegularRef) / (double)PhysicalRef);
            integer = Math.Truncate(dwval);
            fraction = (double)(dwval - integer);
            if (fraction >= 0.5)
                integer += 1;
            if (fraction <= -0.5)
                integer -= 1;
            wval = (UInt32)integer;

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
            UInt16 ucmd = 0;
            UInt32 data = 0;
            Reg regLow = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COBRA_DATA_LEN)p.tsmbBuffer.length)
            {
                case ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES:
                    {
                        switch (p.guid & ElementDefine.ElementMask)
                        {
                            case ElementDefine.SBSElement:
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
                            case ElementDefine.CONFIGElement:
                                {
                                    ucmd = (UInt16)((p.guid & ElementDefine.Command2Mask) >> 4);
                                    if (!parent.m_HwMode_Dic.ContainsKey(ucmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                                    data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_HwMode_Dic[ucmd].bdata[0], parent.m_HwMode_Dic[ucmd].bdata[1]), SharedFormula.MAKEWORD(parent.m_HwMode_Dic[ucmd].bdata[2], parent.m_HwMode_Dic[ucmd].bdata[3]));
                                    break;
                                }
                        }

                    }
                    break;
            }
            pval = data;
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
        public UInt32 WriteToRegImg(Parameter p, UInt32 wVal)
        {
            UInt16 ucmd = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);

            switch ((ElementDefine.COBRA_DATA_LEN)p.tsmbBuffer.length)
            {
                case ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES:
                    {
                        switch (p.guid & ElementDefine.ElementMask)
                        {
                            case ElementDefine.SBSElement:
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    if (dic.Key.Equals("Low"))
                                    {
                                        if (!parent.m_HwMode_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

                                        parent.m_HwMode_Dic[bcmd].bdata[0] = (byte)(wVal & 0xFF);
                                        parent.m_HwMode_Dic[bcmd].bdata[1] = (byte)((wVal & 0xFF00) >> 8);
                                        parent.m_HwMode_Dic[bcmd].bdata[2] = (byte)((wVal & 0xFF0000) >> 16);
                                        parent.m_HwMode_Dic[bcmd].bdata[3] = (byte)((wVal >> 24) & 0xFF);
                                    }
                                }
                                break;
                            case ElementDefine.CONFIGElement:
                                {
                                    ucmd = (UInt16)((p.guid & ElementDefine.Command2Mask) >> 4);
                                    if (!parent.m_HwMode_Dic.ContainsKey(ucmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

                                    parent.m_HwMode_Dic[ucmd].bdata[0] = (byte)(wVal & 0xFF);
                                    parent.m_HwMode_Dic[ucmd].bdata[1] = (byte)((wVal & 0xFF00) >> 8);
                                    parent.m_HwMode_Dic[ucmd].bdata[2] = (byte)((wVal & 0xFF0000) >> 16);
                                    parent.m_HwMode_Dic[ucmd].bdata[3] = (byte)((wVal >> 24) & 0xFF);
                                    break;
                                }
                        }
                    }
                    break;
            }
            return ret;
        }

        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        private UInt32 GetRsenseMain(ref double pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt16 uadd = 0x101;
            if (!parent.m_HwMode_Dic.ContainsKey(uadd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            ret = parent.ReadRsenseMain();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            TSMBbuffer tsmBuffer = parent.m_HwMode_Dic[uadd];
            pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_HwMode_Dic[uadd].bdata[0], parent.m_HwMode_Dic[uadd].bdata[1]), SharedFormula.MAKEWORD(parent.m_HwMode_Dic[uadd].bdata[2], parent.m_HwMode_Dic[uadd].bdata[3]));
            pval = (pval / 1000.0);
            return ret;
        }
    }
}
