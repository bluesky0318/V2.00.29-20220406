using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.Blueway
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
            switch (p.guid)
            {
                case ElementDefine.PARM_BCFG_RSENSEMAIN:
                    UpdateRsenseRelations(p);
                    break;
            }
            return;
        }

        private void UpdateRsenseRelations(Parameter p)
        {
            string str = string.Empty;
            Parameter PARM_BCFG_DSGTH = null;
            Parameter PARM_BCFG_CHGTH = null;
            Parameter PARM_PROT_DOC2MTH = null;

            PARM_BCFG_DSGTH = parent.GetParameterByGuidFromProject(ElementDefine.PARM_BCFG_DSGTH);
            if (PARM_BCFG_DSGTH == null) return;
            PARM_BCFG_CHGTH = parent.GetParameterByGuidFromProject(ElementDefine.PARM_BCFG_CHGTH);
            if (PARM_BCFG_CHGTH == null) return;
            Physical2Hex(ref PARM_BCFG_DSGTH);
            Physical2Hex(ref PARM_BCFG_CHGTH);
            parent.Proj_Rsense = p.phydata * 1000.0;
            Hex2Physical(ref PARM_BCFG_DSGTH);
            Hex2Physical(ref PARM_BCFG_CHGTH);
            PARM_PROT_DOC2MTH = parent.GetParameterByGuidFromProject(ElementDefine.PARM_PROT_DOC2MTH);
            if (PARM_BCFG_CHGTH == null) return;
            PARM_PROT_DOC2MTH.itemlist.Clear();
            PARM_PROT_DOC2MTH.itemlist.Add("Disable");
            for (int i = 1; i < 25; i++)
                PARM_PROT_DOC2MTH.itemlist.Add(string.Format("{0:F2}mA", ((i - 1) * 10 + 20) * 1000.0 / p.phydata));
            PARM_PROT_DOC2MTH.itemlist = PARM_PROT_DOC2MTH.itemlist;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            float fval = 0;
            UInt32 dwval = 0;
            byte[] atmp = null;
            byte[] b16tmp = new byte[16];
            Reg regLow = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGNED:
                    {
                        dwval = (UInt32)Physical2Regular(p.phydata, 0, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dwval);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PRJ_PARAM_OT_UT:
                    {
                        dwval = (UInt32)Physical2Regular(p.phydata, -2730, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dwval);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_RSENSE:
                    {
                        parent.Proj_Rsense = p.phydata * 1000.0;
                        dwval = (UInt32)Physical2Regular(p.phydata, 0, p.regref, p.phyref);
                        ret = WriteToRegImg(p, dwval);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PROJ_CURRENT:
                    {
                        double Rsense = parent.Proj_Rsense;
                        if (Rsense == 0) Rsense = 2500;

                        fval = (float)(p.phydata * Rsense / (float)(1000000.0));
                        dwval = (UInt32)Physical2Regular(fval, 0.125, p.regref, p.phyref);
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING16:
                    {
                        p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                if (p.sphydata == null) break;
                                if (p.sphydata.Length != 32)
                                {
                                    p.errorcode = ElementDefine.IDS_ERR_DEM_AUTHKEY_LEN_ILLEGAL;
                                    break;
                                }
                                if (IsIllegalHexadecimal(p.sphydata))
                                {
                                    p.errorcode = ElementDefine.IDS_ERR_DEM_AUTHKEY_DATA_ILLEGAL;
                                    break;
                                }
                                string[] subtmp = subString(p.sphydata, 2);
                                for (int i = 0; i < subtmp.Length; i++)
                                    b16tmp[i] = Byte.Parse(subtmp[i], System.Globalization.NumberStyles.HexNumber);
                                parent.m_ProjParamImg[regLow.address - ElementDefine.ParameterArea_StartAddress] = 16;
                                Array.Copy(b16tmp, 0, parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress + 1), b16tmp.Length);
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
            double dval = 0;
            Int16 sdata = 0;
            Int32 swdata = 0;
            UInt32 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PROJ_CURRENT:
                    {
                        double Rsense = parent.Proj_Rsense;
                        if (Rsense == 0) Rsense = 2500;

                        ret = ReadSignedFromRegImg(p, ref swdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        dval = Regular2Physical(swdata, 0.125, p.regref, p.phyref);
                        p.phydata = (float)(dval * (float)(1000000.0) / Rsense);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PROJ_DOC2TH:
                    {
                        double Rsense = parent.Proj_Rsense;
                        if (Rsense == 0) Rsense = 2500;

                        p.itemlist.Clear();
                        p.itemlist.Add("Disable");
                        for (int i = 1; i < 25; i++)
                            p.itemlist.Add(string.Format("{0:F2}mA", ((i - 1) * 10 + 20) * 1000000.0 / Rsense));
                        p.itemlist = p.itemlist;

                        ret = ReadFromRegImg(p, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wval, p.offset, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGNED0:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.offset, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGNED:
                    {
                        ret = ReadSignedFromRegImg(p, ref swdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(swdata, p.offset, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PRJ_PARAM_OT_UT:
                    {
                        ret = ReadSignedFromRegImg(p, ref swdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(swdata, -2730, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.SBS_PARAM_OT_UT:
                    {
                        ret = ReadSignedFromRegImg(p, ref swdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(swdata, -273, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_RSENSE:
                    {
                        ret = ReadFromRegImg(p, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wval, p.offset, p.regref, p.phyref);
                        parent.Proj_Rsense = p.phydata * 1000.0;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DATE0:
                    {
                        ret = ReadFromRegImg(p, ref wval);//切记phydata需要刷新，否则UI无数值
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.sphydata = UInt32ToDataHMS(wval);
                        p.phydata = wval;
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
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING16:
                    {
                        p.sphydata = "00112233445566778899AABBCCDDEEFF";
                        p.phydata = 0.0;
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wval, p.offset, p.regref, p.phyref);
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
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref Int16 pval)
        {
            Int16 sdata;
            UInt32 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            sdata = (Int16)wdata;
            pval = sdata;
            return ret;
        }
        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref Int32 pval)
        {
            Int32 sdata;
            UInt32 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            sdata = (Int32)wdata;
            pval = sdata;
            return ret;
        }
        /// <summary>
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadFromRegImg(Parameter p, ref UInt32 pval)
        {
            byte bcmd = 0;
            UInt16 sbcmd = 0;
            Reg regLow = null;
            byte[] bArray = new byte[4];
            TSMBbuffer tsmBuffer = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.SBSElement:
                    {
                        regLow = p.reglist["Low"];
                        bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                        if (!parent.m_SBSMode_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                        if (bcmd != 0xF9)
                        {
                            pval = SharedFormula.MAKEWORD(parent.m_SBSMode_Dic[bcmd].bdata[0], parent.m_SBSMode_Dic[bcmd].bdata[1]);
                            if ((bcmd == 0x03) | (bcmd == 0x16))
                            {
                                pval <<= (32 - regLow.startbit - regLow.bitsnumber);
                                pval >>= (32 - regLow.bitsnumber);
                            }
                        }
                        else
                        {
                            sbcmd = regLow.address;
                            tsmBuffer = parent.m_F9Mode_Dic[sbcmd];
                            switch ((ElementDefine.MF_BLOCK_ACCESS)sbcmd)
                            {
                                case ElementDefine.MF_BLOCK_ACCESS.SE:
                                    {
                                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.m_F9Mode_Dic[sbcmd].bdata[0], parent.m_F9Mode_Dic[sbcmd].bdata[1]),
                                            SharedFormula.MAKEWORD(parent.m_F9Mode_Dic[sbcmd].bdata[2], parent.m_F9Mode_Dic[sbcmd].bdata[3]));
                                        pval <<= (32 - regLow.startbit - regLow.bitsnumber);
                                        pval >>= (32 - regLow.bitsnumber);
                                    }
                                    break;
                            }
                        }
                        break;
                    }
                case ElementDefine.PrjParamElement:
                    {
                        //pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[1], p.tsmbBuffer.bdata[2]),SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[3], p.tsmbBuffer.bdata[4]));
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                Array.Copy(parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress), bArray, 0, bArray.Length);
                            }
                        }
                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
                        break;
                    }
                case ElementDefine.LogElement:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                switch (regLow.bitsnumber)
                                {
                                    case 1:
                                        pval = parent.logAreaArray[regLow.address * 16 + regLow.startbit];
                                        break;
                                    case 2:
                                        pval = SharedFormula.MAKEWORD(parent.logAreaArray[regLow.address * 16 + regLow.startbit], parent.logAreaArray[regLow.address * 16 + regLow.startbit + 1]);
                                        break;
                                    case 4:
                                        pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.logAreaArray[regLow.address * 16 + regLow.startbit], parent.logAreaArray[regLow.address * 16 + regLow.startbit + 1]),
                                        SharedFormula.MAKEWORD(parent.logAreaArray[regLow.address * 16 + regLow.startbit + 2], parent.logAreaArray[regLow.address * 16 + regLow.startbit + 3]));
                                        break;
                                }
                            }
                        }
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
            byte bcmd = 0;
            Reg regLow = null;
            byte[] bArray = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (p.guid & ElementDefine.ElementMask)
            {
                case ElementDefine.SBSElement:
                    {
                        bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                        if (!parent.m_SBSMode_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                        p.tsmbBuffer.length = 2;
                        p.tsmbBuffer.bdata[0] = (byte)wVal;
                        p.tsmbBuffer.bdata[1] = (byte)(wVal >> 8);
                        break;
                    }
                case ElementDefine.LogElement:
                    {
                        break;
                    }
                case ElementDefine.PrjParamElement:
                    {
                        foreach (KeyValuePair<string, Reg> dic in p.reglist)
                        {
                            if (dic.Key.Equals("Low"))
                            {
                                regLow = dic.Value;
                                bArray[0] = (byte)wVal;
                                bArray[1] = (byte)(wVal >> 8);
                                bArray[2] = (byte)(wVal >> 16);
                                bArray[3] = (byte)(wVal >> 24);
                                Array.Copy(bArray, 0, parent.m_ProjParamImg, (regLow.address - ElementDefine.ParameterArea_StartAddress), 4);
                            }
                        }
                        break;
                    }
            }
            return ret;
        }

        public string UInt32ToDataHMS(UInt32 value)
        {
            UInt16 Year;
            byte Mon, Day, Hour, Min, Sec;
            string tempstr = string.Empty;
            Sec = (byte)(value & 0x003F);
            Min = (byte)((value & 0x0FC0) >> 6);
            Hour = (byte)((value & 0x1F000) >> 12);
            Day = (byte)((value & 0x3E0000) >> 17);
            Mon = (byte)((value & 0x3C00000) >> 22);
            Year = (byte)((value & 0xFC000000) >> 26);
            Year += 2000;
            return string.Format("{0:D2}-{1:D2}-{2:D4}", Day, Mon, Year);
        }

        public const string PATTERN = @"([^A-Fa-f0-9]|\s+?)+";
        /// <summary>
        /// 判断十六进制字符串hex是否正确
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>true：不正确，false：正确</returns>
        public bool IsIllegalHexadecimal(string hex)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(hex, PATTERN);
        }

        public string[] subString(string x, int y)//定义函数，输入的字符串和截取的长度
        {
            List<string> d = new List<string>();
            int i = 0, j = x.Length;
            while (i < x.Length && j > y)
            {
                d.Add(x.Substring(i, y));
                i = i + y;
                j = j - y;
            }
            d.Add(x.Substring(i, j));
            return d.ToArray();
        }
    }
}
