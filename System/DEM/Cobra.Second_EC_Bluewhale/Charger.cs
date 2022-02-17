using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.Second_EC_Bluewhale
{
    class Charger
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMBehaviorManage m_parent;
        public DEMBehaviorManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        #endregion

        #region 硬件模式下相关参数数据初始化
        public void Init(object pParent)
        {
            parent = (DEMBehaviorManage)pParent;
            InitialImgReg();
        }

        //操作寄存器初始化
        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }
        #endregion

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        public UInt32 ReadByte(List<Parameter> OpParamList)
        {
            if (OpParamList.Count == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;

            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();

            foreach (Parameter p in OpParamList)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                OpReglist.Add(baddress);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            foreach (byte badd in OpReglist)
            {
                lock (parent.m_lock)
                {
                    ret = OnReadByte(badd, ref bdata);
                }
                m_OpRegImg[badd].err = ret;
                m_OpRegImg[badd].val = (UInt16)bdata;
            }
            return ret;
        }

        public UInt32 WriteByte(List<Parameter> OpParamList)
        {
            if (OpParamList.Count == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;

            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OpReglist = new List<byte>();

            foreach (Parameter p in OpParamList)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                OpReglist.Add(baddress);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            foreach (byte badd in OpReglist)
            {

                lock (parent.m_lock)
                {
                    ret |= OnWriteByte(badd, (byte)m_OpRegImg[badd].val);
                }
                m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ReadOneByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (parent.m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        public UInt32 WriteOneByte(byte reg, byte val)
        {
            UInt32 ret = 0;
            lock (parent.m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 5;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[5];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = 0x33;
            sendbuf[1] = 0x05;
            sendbuf[2] = reg;
            sendbuf[3] = 0;
            sendbuf[4] = (byte)(00 - sendbuf[1] - sendbuf[2] - sendbuf[3]);

            FolderMap.WriteFile(String.Format("Charger read:{0:x2},{1:x2},{2:x2},{3:x2},{4:x2}", sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3], sendbuf[4]));
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 5))
                {
                    ret = ElementDefine.CheckData(receivebuf);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    pval = receivebuf[3];
                    break;
                }

            }
            Thread.Sleep(10);

            FolderMap.WriteFile(String.Format("Charger receive:{0:x2},{1:x2},{2:x2},{3:x2},{4:x2}", receivebuf[0], receivebuf[1], receivebuf[2], receivebuf[3], receivebuf[4]));
            parent.m_Interface.GetLastErrorCode(ref ret1);
            ret |= ret1;
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 5;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[5];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = 0x33;
            sendbuf[1] = 0x04;
            sendbuf[2] = reg;
            sendbuf[3] = val;
            sendbuf[4] = (byte)(0x00 - sendbuf[1] - sendbuf[2] - sendbuf[3]);

            FolderMap.WriteFile(String.Format("Charger write:{0:x2},{1:x2},{2:x2},{3:x2},{4:x2}", sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3], sendbuf[4]));
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 5))
                {
                    ret = ElementDefine.CheckData(receivebuf);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    break;
                }
            }

            FolderMap.WriteFile(String.Format("Charger receive:{0:x2},{1:x2},{2:x2},{3:x2},{4:x2}", receivebuf[0], receivebuf[1], receivebuf[2], receivebuf[3], receivebuf[4]));
            Thread.Sleep(10);

            parent.m_Interface.GetLastErrorCode(ref ret1);
            ret |= ret1;
            return ret;
        }
        #endregion
        #endregion

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
            UInt16 wdata = 0;
            float fval = 0;
            double resistor = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        resistor = parent.TempToResist(p.phydata);
                        fval = (float)((5000 * resistor + m_parent.parent.etrx * 1000 * 5000) / ((m_parent.parent.pullupR + m_parent.parent.etrx) * 1000 + resistor));
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        fval = (float)((p.phydata + 50) * 16.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);

                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_CV:
                    {
                        wdata = (UInt16)p.phydata;
                        wdata += 0xA0;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_WAKEUP_V:
                    {
                        wdata = (UInt16)p.phydata;
                        wdata += 0x0F;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_VSM:
                    {
                        wdata = (UInt16)p.phydata;
                        wdata += 0x09;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_CC:
                    {
                        wdata = (UInt16)p.phydata;
                        wdata = (UInt16)((wdata * 2) + 4);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_WAKEUP_C:
                    {
                        wdata = (UInt16)p.phydata;
                        wdata = (UInt16)((wdata * 2) + 0x0a);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_EOC:
                    {
                        wdata = (UInt16)(p.phydata * 2);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_VICL:
                    {
                        if (p.phydata == 0)
                            wdata = 1;
                        else if (p.phydata == 1)
                            wdata = 5;
                        else if (p.phydata == 2)
                            wdata = 9;
                        else
                            wdata = (UInt16)(p.phydata + 10);

                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_WKT:
                    {
                        wdata = (UInt16)p.phydata;
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                    {
                        m_parent.parent.ModifyTemperatureConfig(p, true);
                        break;
                    }
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);

                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = (double)((p.phydata * (m_parent.parent.pullupR + m_parent.parent.etrx) * 1000 - m_parent.parent.etrx * 1000 * 5000) / (5000 - p.phydata));
                        p.phydata = parent.ResistToTemp(p.phydata);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = (double)(p.phydata / 16 - 50);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_CV:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0xB8) wdata = 0xB8;
                        if (wdata < 0xA0) wdata = 0xA0;

                        p.phydata = (float)(wdata - 0xA0);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_WAKEUP_V:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x1E) wdata = 0x1E;
                        if (wdata < 0x0F) wdata = 0x0F;

                        p.phydata = (float)(wdata - 0x0F);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_VSM:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x12) wdata = 0x12;
                        if (wdata < 0x09) wdata = 0x09;

                        p.phydata = (float)(wdata - 0x09);
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_CC:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x28) wdata = 0x28;
                        if (wdata < 0x06) wdata = 0x03;

                        p.phydata = (wdata - 0x03) / 2;
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_WAKEUP_C:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x28) wdata = 0x28;
                        if (wdata < 0x0a) wdata = 0x0a;

                        p.phydata = (wdata - 0x0a) / 2;
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_EOC:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x20) wdata = 0x20;

                        p.phydata = wdata / 2;
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_VICL:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x14) wdata = 0x14;
                        if (wdata < 0x01) wdata = 0x01;

                        if (wdata <= 0x03)
                            p.phydata = 0;
                        else if (wdata <= 0x07)
                            p.phydata = 1;
                        else if (wdata <= 0x0b)
                            p.phydata = 2;
                        else if (wdata <= 0x0d)
                            p.phydata = 3;
                        else
                            p.phydata = wdata - 10;
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_WKT:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (wdata > 0x07) wdata = 0x07;

                        p.phydata = (float)wdata;
                        break;
                    }
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                case ElementDefine.COBRA_CHARGER_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                    {
                        m_parent.parent.ModifyTemperatureConfig(p, false);
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref wdata);

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
        private UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            UInt16 data = 0, mask;
            byte hi = 0, lo = 0, tmp = 0;
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
                mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                mask <<= regLow.startbit;
                data &= (UInt16)(~mask);
                data |= (UInt16)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data);
            }
            else
            {

                wVal <<= regLow.startbit;
                lo = SharedFormula.LoByte(wVal);
                hi = SharedFormula.HiByte(wVal);
                hi <<= regHi.startbit;

                tmp = (byte)data;
                mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                mask <<= regLow.startbit;
                tmp &= (byte)(~mask);
                tmp |= lo;
                WriteRegToImg(regLow.address, p.guid, tmp);

                ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                tmp = (byte)data;
                mask = (UInt16)((1 << regHi.bitsnumber) - 1);
                mask <<= regHi.startbit;
                tmp &= (byte)(~mask);
                tmp |= hi;
                WriteRegToImg(regHi.address, p.guid, tmp);

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
            pval = m_OpRegImg[reg].val;
            ret = m_OpRegImg[reg].err;
            return ret;
        }

        private void WriteRegToImg(UInt16 reg, UInt32 guid, UInt16 value)
        {
            m_OpRegImg[reg].val = value;
            m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;

        }
        #endregion
    }
}
