using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.MPT
{
    public class Adc7745
    {
        #region 定义参数subtype枚举类型
        
        //父对象保存
        private DEMBehaviorManage m_parent;
        public DEMBehaviorManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private byte m_BusAddress = 0x90;
        public byte busaddress
        {
            get { return m_BusAddress; }
            set { m_BusAddress = value; }
        }

        private UInt32 m_ZeroPoint;
        public UInt32 zeropoint
        {
            get { return m_ZeroPoint; }
            set { m_ZeroPoint = value; }
        }

        private UInt32 m_RefPoint;
        public UInt32 refpoint
        {
            get { return m_RefPoint; }
            set { m_RefPoint = value; }
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
        public UInt32 ReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (parent.m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        public UInt32 ReadByte(byte reg, byte[] pval, UInt16 wDataInLength = 1)
        {
            byte[] bval = new byte[wDataInLength];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (parent.m_lock)
            {
                ret = OnReadByte(reg, bval,wDataInLength);
            }
            return ret;
        }

        public UInt32 WriteByte(byte reg, byte val)
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
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = busaddress;
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    pval = receivebuf[0];
                    break;
                }
                Thread.Sleep(10);
            }

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnReadByte(byte reg, byte[] pval, UInt16 wDataInLength = 1)
        {
            UInt16 DataOutLen = wDataInLength;
            byte[] sendbuf = new byte[wDataInLength];
            byte[] receivebuf = new byte[wDataInLength];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = busaddress;
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, wDataInLength))
                {
                    receivebuf.CopyTo(pval, 0);
                    break;
                }
                Thread.Sleep(10);
            }

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = busaddress;
            sendbuf[1] = reg;
            sendbuf[2] = val;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                    break;
                Thread.Sleep(10);
            }

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        #endregion
        #endregion

        #region 芯片数据操作
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        fval = (float)((p.phydata + 50) * 16.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
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
            UInt16 wdata  = 0;
            UInt32 dwdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = ReadFromRegImg(p, ref dwdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        dwdata = (dwdata & 0x7FFFFF);
                        p.phydata = Regular2Physical(dwdata, p.regref, p.phyref)*10;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = (double)(p.phydata / 16 - 50);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                    {
                        m_parent.parent.ModifyTemperatureConfig(p, false);
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

            return (double)integer;
        }

        private double Regular2Physical(UInt32 dwVal, double RegularRef, double PhysicalRef)
        {
            double dval, integer, fraction;

            dval = (double)((double)(dwVal * PhysicalRef) / (double)RegularRef);
            //integer = Math.Truncate(dval);
            //fraction = (double)(dval - integer);
            //if (fraction >= 0.5)
             //   integer += 1;
            //else if (fraction <= -0.5)
             //   integer -= 1;

           // return (double)integer;
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

        private UInt32 ReadFromRegImg(Parameter p, ref UInt32 pval)
        {
            UInt32 data;
            UInt16 hi = 0,mid = 0, lo = 0;
            Reg regLow = null,regMid = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                {
                    regLow = dic.Value;
                    ret = ReadRegFromImg(regLow.address, p.guid, ref lo);
                    lo <<= (16 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("Middle"))
                {
                    regMid = dic.Value;
                    ret = ReadRegFromImg(regMid.address, p.guid, ref mid);
                    mid <<= (16 - regMid.bitsnumber - regMid.startbit); //align with left
                    mid >>= (16 - regMid.bitsnumber); //align with right
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadRegFromImg(regHi.address, p.guid, ref hi);
                    hi <<= (16 - regHi.bitsnumber - regHi.startbit); //align with left
                    hi >>= (16 - regHi.bitsnumber); //align with right
                }
            }
            data = ((UInt32)(((UInt16)(lo)) | ((UInt32)((UInt16)(mid))) << 16));
            data >>= (16 - regLow.bitsnumber); //align with right
            data = SharedFormula.MAKEDWORD((UInt16)data, hi);

            pval = data;
            p.hexdata = (UInt16)data;
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

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        pval = m_OpRegImg[reg].val;
                        ret = m_OpRegImg[reg].err;
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
                        m_OpRegImg[reg].val = value;
                        m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion
        #endregion

        #region 芯片功能操作
        public UInt32 Init()
        {
            byte bval = 0;
            UInt32 val = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            // read status
            ret = ReadByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_STATUS, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // enable voltage only
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CAP_SETUP, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_VT_SETUP, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_EXC_SETUP, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CONFIGURATION, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            Thread.Sleep(30);
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CAP_SETUP, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)(ElementDefine.COBRA_ADC7745_VTSETUP.ADC_VTEN | ElementDefine.COBRA_ADC7745_VTSETUP.ADC_VTMD0 | ElementDefine.COBRA_ADC7745_VTSETUP.ADC_VTMD1 |
                                                    ElementDefine.COBRA_ADC7745_VTSETUP.ADC_EXTREF | ElementDefine.COBRA_ADC7745_VTSETUP.ADC_VTCHOP);
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_VT_SETUP, bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            
            bval = (byte)(ElementDefine.COBRA_ADC7745_CONTROL.ADC_VTFS1 | ElementDefine.COBRA_ADC7745_CONTROL.ADC_VTFS0 | ElementDefine.COBRA_ADC7745_CONTROL.ADC_MD0);
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CONFIGURATION, bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_EXC_SETUP, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // disable DACs
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CAP_DAC_A, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CAP_DAC_B, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = GetValue(val);
            return ret;
        }

        //5次去掉最高最低值，求平均
        private UInt32 GetValue(UInt32 p_value)
        {
            UInt32 val = 0;
            UInt32 sum = 0;
            UInt32 minval = 0;
            UInt32 maxval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = GetSubValue(val);
                minval = Math.Min(val, minval);
                maxval = Math.Max(val, maxval);
                sum += val;
            }
            sum -= minval + maxval;
            p_value = sum / 3;
            return ret;
        }

        //获取单电压/温度通道值
        private UInt32 GetSubValue(UInt32 val)
        {
            int i;
            byte bval = 0;
            byte[] buffer = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CONFIGURATION, (byte)ElementDefine.COBRA_ADC7745_CONTROL.ADC_MD1);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Thread.Sleep(21);

            for (i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = ReadByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_STATUS, ref bval);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    if ((bval & (byte)ElementDefine.COBRA_ADC7745_CONTROL.ADC_STATUS_RDYVT) == 0) break;
                    else ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                }
                Thread.Sleep(10);
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_VT_DATA_H,buffer, 3);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            val = (UInt32)(buffer[2] + (buffer[1] << 8) + (buffer[0] << 16));

            return ret;
        }

        private UInt32 ExternalInit(ref byte p_block)
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            // read status
            ret = ReadByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_STATUS, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // enable voltage only
            bval = p_block;
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CAP_SETUP, bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)(p_block + 1);
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_VT_SETUP, bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)(p_block + 2);
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_EXC_SETUP, bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)(p_block + 3);
            ret = WriteByte((byte)ElementDefine.COBRA_ADC7745_REG.ADC_CONFIGURATION, bval);

            return ret;
        }

        //复位ADC7745,下指令到0xBF
        private UInt32 Reset()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WriteByte(0xBF, 0x11);
            return ret;
        }
        #endregion
    }
}
