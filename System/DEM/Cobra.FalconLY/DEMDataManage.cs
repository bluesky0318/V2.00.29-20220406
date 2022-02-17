using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.FalconLY
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            UInt16 wdata = 0;
            Double ddata = 0;
            string tmp = string.Empty;

            if (p == null) return;
            ReadFromRegImg(p, ref wdata);
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN:
                    {
                        switch ((ElementDefine.COBRA_DATA_LEN)p.tsmbBuffer.length)
                        {
                            case ElementDefine.COBRA_DATA_LEN.DATA_LEN_ONE_BYTE:
                                p.phydata = (char)p.tsmbBuffer.bdata[0];
                                break;
                            case ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES:
                                p.phydata = (short)SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[0], p.tsmbBuffer.bdata[1]);
                                break;
                            case ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES:
                                p.phydata = (int)SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[0], p.tsmbBuffer.bdata[1]), SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[2], p.tsmbBuffer.bdata[3]));
                                break;
                        }
                        p.sphydata = String.Format("{0:f1}", (long)p.phydata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_BYTE:
                    {
                        p.sphydata = String.Format("0x{0:X2}", (long)p.phydata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_WORD:
                    {
                        p.sphydata = String.Format("0x{0:X4}", (long)p.phydata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_TEMP:
                    {
                        ddata = (p.phydata - 2730) / 10.0;
                        p.sphydata = String.Format("{0:F1}", ddata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DATA:
                    {
                        p.sphydata = SharedFormula.UInt32ToData((UInt32)p.phydata);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING:
                    {
                        if (p.tsmbBuffer.bdata[0] > ElementDefine.MaxBytesNum) p.tsmbBuffer.bdata[0] = (byte)ElementDefine.MaxBytesNum;
                        byte[] buf = new byte[p.tsmbBuffer.bdata[0]];
                        
                        Array.Copy(p.tsmbBuffer.bdata,1, buf,0, buf.Length);
                        p.sphydata = SharedFormula.HexToASCII(buf);
                        break;
                    }
                default:
                    {
                        p.sphydata = String.Format("{0:F1}", p.phydata);
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_DATA_LEN)p.tsmbBuffer.length)
            {
                case ElementDefine.COBRA_DATA_LEN.DATA_LEN_ONE_BYTE:
                    p.phydata = p.tsmbBuffer.bdata[0];
                    break;
                case ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES:
                    p.phydata = SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[0], p.tsmbBuffer.bdata[1]);
                    break;
                case ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES:
                    p.phydata = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[0], p.tsmbBuffer.bdata[1]),SharedFormula.MAKEWORD(p.tsmbBuffer.bdata[2], p.tsmbBuffer.bdata[3]));
                    break;
            }
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
    }
}
