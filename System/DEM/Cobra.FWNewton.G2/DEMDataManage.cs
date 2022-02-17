using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.FWNewTon.G2
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
            float fval = 0;
            UInt32 dWdata = 0;
            double resistor = 0;
            UInt32 dwval = 0;
            byte[] atmp = null;
            Reg regLow = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DATE:
                    {
                        dWdata = Physical2Regular(p.phydata, p.offset, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dWdata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        resistor = SharedFormula.TempToResist(p.phydata, parent.m_TempVals, parent.m_ResistVals);
                        dwval = Physical2Regular(resistor, 0, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dwval);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PROJ_CURRENT:
                    {
                        double Rsense = parent.Proj_Rsense;
                        if (Rsense == 0) Rsense = 2500;

                        fval = (float)(p.phydata * Rsense / (float)(1000));
                        dwval = (UInt32)Physical2Regular(fval, 0, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dwval);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                if (p.sphydata == null) break;
                                atmp = SharedFormula.StringToASCII(p.sphydata);
                                parent.m_ProjParamImg[regLow.address - ElementDefine.ParameterArea_StartAddress] = (byte)atmp.Length;
                                Array.Copy(atmp, 0, parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress + 1), atmp.Length);
                            }
                        }
                        break;
                    }
                default:
                    {
                        dwval = (UInt32)Physical2Regular(p.phydata, 0, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dwval);
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
            double ddata = 0;
            UInt32 dwval = 0;
            StringBuilder str = new StringBuilder();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DATE:
                    {
                        ret = ReadFromRegImg(p, ref dwval);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.sphydata = SharedFormula.UInt32ToData(dwval);
                        p.phydata = dwval;
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING1:
                    {
                        str.Clear();
                        str.Append("0x");
                        for (int i = 0; i < p.tsmbBuffer.length; i++)
                        {
                            str.Append(string.Format("{0:x2}", p.tsmbBuffer.bdata[i]));
                            str.Append("-");
                        }
                        p.sphydata = str.ToString();
                        p.phydata = 0.0;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref dwval);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        ddata = Regular2Physical(dwval, p.offset, p.regref, p.phyref);
                        p.phydata = (ddata - 2730) / 10;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGNED:
                    {
                        ret = ReadFromRegImg(p, ref dwval);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical((Int32)dwval, p.offset, p.regref, p.phyref);
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref dwval);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(dwval, p.offset, p.regref, p.phyref);
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
        private double Regular2Physical(UInt32 wVal, double Offset, double RegularRef, double PhysicalRef)
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
        private double Regular2Physical(Int32 wVal, double Offset, double RegularRef, double PhysicalRef)
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
            UInt32 data = 0;
            Reg regLow = null;
            byte[] bArray = new byte[4];
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
                                            if (!parent.m_SBS_CMD_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                                            data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_SBS_CMD_Dic[bcmd].bdata[0], parent.m_SBS_CMD_Dic[bcmd].bdata[1]), SharedFormula.MAKEWORD(parent.m_SBS_CMD_Dic[bcmd].bdata[2], parent.m_SBS_CMD_Dic[bcmd].bdata[3]));
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
                case ElementDefine.ProParaElement:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                Array.Copy(parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress), bArray, 0, 4);
                                if (regLow.address == ElementDefine.ParameterArea_StartAddress)
                                    regLow.bitsnumber = 16;
                                data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
                                data <<= (32 - regLow.bitsnumber - regLow.startbit); //align with left
                                data >>= (32 - regLow.bitsnumber);
                            }
                        }
                        pval = data;
                        break;
                    }
                case ElementDefine.VirtualElement:
                    {
                        #region  SBS Element
                        if ((p.guid == ElementDefine.Virtual_Charger_Cur) | (p.guid == ElementDefine.Virtual_DisCharger_Cur))
                        {
                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                if (dic.Key.Equals("Low"))
                                {
                                    regLow = dic.Value;
                                    if (!parent.m_SBS_CMD_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                                    data = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_SBS_CMD_Dic[bcmd].bdata[0], parent.m_SBS_CMD_Dic[bcmd].bdata[1]), SharedFormula.MAKEWORD(parent.m_SBS_CMD_Dic[bcmd].bdata[2], parent.m_SBS_CMD_Dic[bcmd].bdata[3]));
                                    data <<= (32 - regLow.bitsnumber - regLow.startbit); //align with left
                                    data >>= (32 - regLow.bitsnumber);
                                }
                            }
                            pval = data;
                        }
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
                                Array.Copy(parent.logAreaArray, regLow.address, bArray, 0, regLow.bitsnumber > bArray.Length ? bArray.Length : regLow.bitsnumber);
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
        /// 写数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <returns></returns>
        public UInt32 WriteToRegImg(Parameter p, UInt32 wVal)
        {
            Reg regLow = null;
            UInt32 rawdata = 0;
            byte[] bArray = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.ProParaElement:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                wVal <<= (32 - regLow.bitsnumber); //align with left
                                wVal >>= (32 - regLow.bitsnumber - regLow.startbit);
                                Array.Copy(parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress), bArray, 0, 4);
                                rawdata = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
                                wVal |= rawdata;
                                bArray[0] = (byte)wVal;
                                bArray[1] = (byte)(wVal >> 8);
                                bArray[2] = (byte)(wVal >> 16);
                                bArray[3] = (byte)(wVal >> 24);
                                Array.Copy(bArray, 0, parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress), 4);
                            }
                        }
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }
    }
}
