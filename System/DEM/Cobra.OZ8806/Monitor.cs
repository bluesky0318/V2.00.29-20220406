using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.OZ8806
{
    class Monitor
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMBehaviorManage m_parent;
        public DEMBehaviorManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private bool m_PecEnable;
        public bool pecenable
        {
            get { return m_PecEnable; }
            set { m_PecEnable = value; }
        }

        private byte trim_buf = 0xaa;

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
                parent.parent.m_MonOpRegImg[i] = new COBRA_HWMode_Reg();
                parent.parent.m_MonOpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                parent.parent.m_MonOpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }


        public void Rectify()
        {
            //uint errorcode = 0;
            byte trim0 = 0, trim1 = 0, trim = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnFetchPec();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return;
            OnReadByte(ElementDefine.TRIM_REG, ref trim);

            if (trim == trim_buf)
                return;

            OnReadByte(ElementDefine.TRIM0_REG, ref trim0);
            OnReadByte(ElementDefine.TRIM1_REG, ref trim1);
            trim &= 0xfc;
            trim |= (byte)((trim0 & 0x01) + ((trim1 >> 6) & 0x02));
            trim_buf = trim;
            OnWriteByte(ElementDefine.TRIM_REG, trim);


            //-------------------------------------------------------------------

            OnReadByte(ElementDefine.TRIM_REG, ref trim0);

            /*if(trim!=trim0)
            {
                System.Windows.MessageBox.Show("Failed! Please try again!");
                return;
            }*/
            //-------------------------------------------------------------------
        }
        #endregion

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        public UInt32 ReadByte(List<Parameter> OpParamList)
        {
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();
            if (OpParamList.Count == 0)
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnFetchPec();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            foreach (Parameter p in OpParamList)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.OperationElement:
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
                    case ElementDefine.TemperatureElement:
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
                parent.parent.m_MonOpRegImg[badd].err = ret;
                parent.parent.m_MonOpRegImg[badd].val = (UInt16)bdata;
            }
            return ret;
        }

        public UInt32 WriteByte(List<Parameter> OpParamList)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();

            if (OpParamList.Count == 0)
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnFetchPec();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            foreach (Parameter p in OpParamList)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.OperationElement:
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            foreach (byte badd in OpReglist)
            {

                lock (parent.m_lock)
                {
                    ret |= OnWriteByte(badd, (byte)parent.parent.m_MonOpRegImg[badd].val);
                }
                parent.parent.m_MonOpRegImg[badd].err = ret;
            }

            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected byte crc8_calc(ref byte[] pdata, UInt16 n)
        {
            byte crc = 0;
            byte crcdata;
            UInt16 i, j;

            for (i = 0; i < n; i++)
            {
                crcdata = pdata[i];
                for (j = 0x80; j != 0; j >>= 1)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc <<= 1;
                        crc ^= 0x07;
                    }
                    else
                        crc <<= 1;

                    if ((crcdata & j) != 0)
                        crc ^= 0x07;
                }
            }
            return crc;
        }

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = data;

            return crc8_calc(ref pdata, 4);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data;

            return crc8_calc(ref pdata, 3);
        }

        protected UInt32 OnFetchPec()
        {
            byte pec = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            /*m_PecEnable = false;
            ret = OnReadByte(0x08, ref pec);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            m_PecEnable = ((pec & 0x04) != 0) ? true : false;*/
            return ret;
        }

        public UInt32 OnReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            //sendbuf[0] = 0x5E;
            sendbuf[1] = reg;

            if (pecenable)
            {
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                    {
                        if (receivebuf[1] != calc_crc_read(sendbuf[0], sendbuf[1], receivebuf[0]))
                        {
                            return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                        }
                        pval = receivebuf[0];
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                    {
                        pval = receivebuf[0];
                        break;
                    }
                }
            }
            Thread.Sleep(10);

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[4];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = val;

            if (pecenable)
            {
                sendbuf[3] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2]);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                        break;
                }
            }
            else
            {
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen))
                        break;
                }
            }
            Thread.Sleep(10);

            parent.m_Interface.GetLastErrorCode(ref ret);
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
            Int16 sdata = 0;
            UInt16 wdata = 0;
            float fval = 0;
            double resistor = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        sdata = (short)Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        double Rsense = parent.parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        fval = (float)(p.phydata * Rsense / (float)(1000));
                        sdata = (short)Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CAR:
                    {
                        double Rsense = parent.parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        fval = (float)(p.phydata * Rsense / (float)(1000));
                        sdata = (short)Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteSignedToRegImg(p, sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        resistor = parent.TempToResist(p.phydata);
                        fval = (float)((m_parent.parent.etrx * 1000 * 1800) / (m_parent.parent.pullupR * 1000));
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
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
            Int16 sdata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                        //if (p.guid == 0x00030c84)
                        //p.phydata = 138;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        double dval = Regular2Physical(sdata, p.regref, p.phyref);
                        double Rsense = parent.parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        p.phydata = (dval * 1000.0) / Rsense;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CAR:
                    {
                        ret = ReadSignedFromRegImg(p, ref sdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        double dval = Regular2Physical(sdata, p.regref, p.phyref);
                        double Rsense = parent.parent.etrx;
                        if (Rsense == 0) Rsense = 20;

                        p.phydata = (dval * 1000.0) / Rsense;
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = p.phydata * 1000 * parent.parent.pullupR * 1000 / (1800 * 1000 - p.phydata * 1000 - p.phydata * parent.parent.pullupR * 1000);
                        p.phydata = parent.ResistToTemp(p.phydata);
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
            /*integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)integer;*/
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
            double dval, integer, fraction;

            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
            /*integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)integer;*/
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

        public UInt32 WorkMode(ElementDefine.COBRA_BLUEWHALE_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (parent.m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }

        protected UInt32 OnWorkMode(ElementDefine.COBRA_BLUEWHALE_WKM wkm)
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadByte(ElementDefine.YFLASH_WORKMODE_REG, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bdata = (byte)(bdata & 0xFC);
            bdata |= 0x48;
            switch (wkm)
            {
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_NORMAL:
                    {
                        ret = OnWriteByte(ElementDefine.YFLASH_WORKMODE_REG, (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_NORMAL);
                        break;
                    }
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ANALOG_TESTMODE:
                    {
                        bdata |= (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ANALOG_TESTMODE;
                        ret = OnWriteByte(ElementDefine.YFLASH_WORKMODE_REG, bdata);
                        break;
                    }
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_DIGITAL_TESTMODE:
                    {
                        bdata |= (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_DIGITAL_TESTMODE;
                        ret = OnWriteByte(ElementDefine.YFLASH_WORKMODE_REG, bdata);
                        break;
                    }
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ADC_FFTMODE:
                    {
                        bdata |= (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ADC_FFTMODE;
                        ret = OnWriteByte(ElementDefine.YFLASH_WORKMODE_REG, bdata);
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        pval = parent.parent.m_MonOpRegImg[reg].val;
                        ret = parent.parent.m_MonOpRegImg[reg].err;
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
                        parent.parent.m_MonOpRegImg[reg].val = value;
                        parent.parent.m_MonOpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion
    }
}
