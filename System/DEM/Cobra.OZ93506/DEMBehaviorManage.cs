using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ93506
{
    internal class DEMBehaviorManage
    {
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private object m_lock = new object();
        private CCommunicateManager m_Interface = new CCommunicateManager();
        private Dictionary<UInt32, double> dic = new Dictionary<UInt32, double>();
        private List<DataPoint> m_dataPoint_List = new List<DataPoint>();

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
        }

        #region 端口操作
        public bool CreateInterface()
        {
            bool bdevice = EnumerateInterface();
            if (!bdevice) return false;

            return m_Interface.OpenDevice(ref parent.m_busoption);
        }

        public bool DestroyInterface()
        {
            return m_Interface.CloseDevice();
        }

        public bool EnumerateInterface()
        {
            return m_Interface.FindDevices(ref parent.m_busoption);
        }
        #endregion

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        internal UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }

        internal UInt32 APBWriteWord(byte reg, UInt16 pval)
        {
            UInt16 memory_addr = 0;
            UInt16 lwdata = 0, hwdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSetMemMode(ElementDefine.COBRA_MEMD.APB_MEMD_DIRECT_W);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (reg % 2 == 0) //偶数
                {
                    memory_addr = (UInt16)(reg * 2);
                    if (reg == 0xB2) //ID:1069
                        lwdata = 0;
                    else
                    {
                        ret = OnReadWord((byte)(reg + 1), ref lwdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }

                    ret = OnWriteWord(ElementDefine.MEM_DATA_LO_REG, lwdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = OnWriteWord(ElementDefine.MEM_DATA_HI_REG, pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                else
                {
                    memory_addr = (UInt16)((reg - 1) * 2);
                    if (reg == 0xB3) //ID:1069
                        hwdata = 0;
                    else
                    {
                        ret = OnReadWord((byte)(reg - 1), ref hwdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }

                    ret = OnWriteWord(ElementDefine.MEM_DATA_LO_REG, pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = OnWriteWord(ElementDefine.MEM_DATA_HI_REG, hwdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }

                UInt16 opcode = (UInt16)((memory_addr >> 2) << 8);
                opcode |= (UInt16)ElementDefine.COBRA_MEMORY_OP_REQ.MEMORY_OP_REQ_APB_WRITE;

                ret = OnWriteWord(ElementDefine.MEM_ADDR_REG, opcode); //i2c_reg(8'hc2), .pkt_in({{4'd0,mtp_reg_adr},8'h02} 
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWaitOpCompleted();
                //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                //ret = OnCloseMemMode();
            }
            return ret;
        }

        internal UInt32 CtrlWriteWord(byte reg, UInt16 pval)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadWord(ElementDefine.MEM_MODE_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (((wdata & 0x8000) != 0x8000) || ((wdata & 0x0003) == 0))
                {
                    ret = OnSetMemMode(ElementDefine.COBRA_MEMD.MPT_MEMD_DIRECT_W);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                ret = OnWriteWord(reg, pval);
            }
            return ret;
        }

        internal UInt32 MapWriteWord(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSetMemMode(ElementDefine.COBRA_MEMD.MPT_MEMD_DIRECT_W);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(reg, pval);
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

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, byte[] data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = data[0];
            pdata[4] = data[1];

            return crc8_calc(ref pdata, 5);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, byte data0, byte data1)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data0;
            pdata[3] = data1;

            return crc8_calc(ref pdata, 4);
        }

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    if (receivebuf[2] != calc_crc_read(sendbuf[0], sendbuf[1], receivebuf))
                    {
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    pval = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = SharedFormula.HiByte(val);
            sendbuf[3] = SharedFormula.LoByte(val);

            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3]);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                    break;
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }

            return ret;
        }

        protected UInt32 OnReadWord1(byte reg, ref UInt16 pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = 0x3C;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    if (receivebuf[2] != calc_crc_read(sendbuf[0], sendbuf[1], receivebuf))
                    {
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    pval = SharedFormula.MAKEWORD(receivebuf[0], receivebuf[1]);
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWriteWord1(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = 0x3C;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = SharedFormula.LoByte(val);
            sendbuf[3] = SharedFormula.HiByte(val);

            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3]);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                    break;
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }

            return ret;
        }
        #endregion
        #endregion

        #region MTP寄存器操作
        #region MTP寄存器父级操作
        internal UInt32 MTPReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WakeUpMPTINFO();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            lock (m_lock)
            {
                ret = OnMTPReadWord1(reg, ref pval);
            }
            return ret;
        }

        internal UInt32 MTPWriteWord(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WakeUpMPTINFO();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            lock (m_lock)
            {
                ret = OnMTPWriteWord1(reg, pval);
            }
            return ret;
        }

        protected UInt32 WaitMappingCompleted()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = ReadWord(0xA1, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & 0x0200) == 0x0200) return LibErrorCode.IDS_ERR_SUCCESSFUL;
                Thread.Sleep(10);
            }
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }
        #endregion

        #region MTP寄存器子级操作
        internal UInt32 OnMTPReadWord1(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadWord1(reg, ref pval);
            return ret;
        }

        internal UInt32 OnMTPWriteWord1(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnWriteWord1(reg, pval);
            return ret;
        }

        internal UInt32 OnMTPReadWord(byte reg, ref UInt16 pval)
        {
            UInt16 memory_addr = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnSetMemMode(ElementDefine.COBRA_MEMD.MPT_MEMD_INDIRECT_WR);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (reg % 2 == 0) //偶数
            {
                memory_addr = (UInt16)(reg / 2);
                UInt16 opcode = (UInt16)(memory_addr << 8);
                opcode |= (UInt16)ElementDefine.COBRA_MEMORY_OP_REQ.MEMORY_OP_REQ_MTP_READ;

                ret = OnWriteWord(ElementDefine.MEM_ADDR_REG, opcode); //i2c_reg(8'hc2), .pkt_in({{4'd0,mtp_reg_adr},8'h01}
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWaitOpCompleted();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnReadWord(ElementDefine.MEM_DATA_HI_REG, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            else
            {
                memory_addr = (UInt16)((reg - 1) / 2);
                UInt16 opcode = (UInt16)(memory_addr << 8);
                opcode |= (UInt16)ElementDefine.COBRA_MEMORY_OP_REQ.MEMORY_OP_REQ_MTP_READ;

                ret = OnWriteWord(ElementDefine.MEM_ADDR_REG, opcode); //i2c_reg(8'hc2), .pkt_in({{4'd0,mtp_reg_adr},8'h01}
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWaitOpCompleted();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnReadWord(ElementDefine.MEM_DATA_LO_REG, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            //ret = OnCloseMemMode();

            return ret;
        }

        internal UInt32 OnMTPWriteWord(byte reg, UInt16 pval)
        {
            UInt16 memory_addr = 0;
            UInt16 lwdata = 0, hwdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnSetMemMode(ElementDefine.COBRA_MEMD.MPT_MEMD_INDIRECT_WR);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (reg % 2 == 0) //偶数
            {
                memory_addr = (UInt16)(reg / 2);
                ret = OnMTPReadWord((byte)(reg + 1), ref lwdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(ElementDefine.MEM_DATA_HI_REG, pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(ElementDefine.MEM_DATA_LO_REG, lwdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            else
            {
                memory_addr = (UInt16)((reg - 1) / 2);
                ret = OnMTPReadWord((byte)(reg - 1), ref hwdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(ElementDefine.MEM_DATA_HI_REG, hwdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(ElementDefine.MEM_DATA_LO_REG, pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            UInt16 opcode = (UInt16)(memory_addr << 8);
            opcode |= (UInt16)ElementDefine.COBRA_MEMORY_OP_REQ.MEMORY_OP_REQ_MTP_WRITE;

            ret = OnWriteWord(ElementDefine.MEM_ADDR_REG, opcode); //i2c_reg(8'hc2), .pkt_in({{4'd0,mtp_reg_adr},8'h02} 
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWaitOpCompleted();
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //ret = OnCloseMemMode();

            return ret;
        }

        protected UInt32 OnSetMemMode(ElementDefine.COBRA_MEMD mode)
        {
            UInt16 wdata1 = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnReadWord(ElementDefine.MEM_MODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata1 = (UInt16)((wdata & 0xFFFC) | (UInt16)mode);
            if ((wdata & 0x8000) == 0x8000)//已经allow_wr=1不切换回default模式以免清除0xC4~0xCB
            {
                if (wdata == wdata1) return ret;
                ret = OnWriteWord(ElementDefine.MEM_MODE_REG, wdata1); //i2c_reg(8'hc3), .pkt_in({8'h80,8'h03}
                return ret;
            }

            wdata |= ElementDefine.ALLOW_WRT;
            wdata &= ElementDefine.MEM_MODE_MASK;
            ret = OnWriteWord(ElementDefine.MEM_MODE_REG, wdata); //i2c_reg(8'hc3), .pkt_in({8'h80,8'h00}
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(ElementDefine.MEM_MODE_REG, (UInt16)(wdata | (UInt16)mode)); //i2c_reg(8'hc3), .pkt_in({8'h80,8'h03}
            return ret;
        }

        /*不要再用了，每次都导致0xCB置空
        protected UInt32 OnCloseMemMode()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnReadWord(ElementDefine.MEM_MODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata &= ElementDefine.ALLOW_WRT_MASK;
            wdata &= ElementDefine.MEM_MODE_MASK;
            ret = OnWriteWord(ElementDefine.MEM_MODE_REG, wdata);
            return ret;
        }*/

        protected UInt32 OnWaitOpCompleted()
        {
            byte bdata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnReadWord(ElementDefine.MEM_ADDR_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bdata = SharedFormula.LoByte(wdata);
                if ((bdata & ElementDefine.MEM_OP_REQ_FLAG) == (byte)ElementDefine.COBRA_MEMORY_OP_REQ.MEMORY_OP_REQ_DEFAULT)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }

            // exceed max waiting time
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }

        protected UInt32 OnWaitTriggerCompleted()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnReadWord(ElementDefine.TRIGGER_SCAN_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & 0x0300) == 0x00) return LibErrorCode.IDS_ERR_SUCCESSFUL;
                Thread.Sleep(10);
            }
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }
        #endregion
        #endregion

        #region MTP功能操作
        #region MTP功能父级操作
        protected UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
            }
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
            }

            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            UInt16 wdata = 0; 
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0xA1, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata |= 0x0700; //Clear mapping flag and send mapping request
            ret = APBWriteWord(0xA1, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WaitMappingCompleted();
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> MTPReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.MTPElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                MTPReglist.Add(baddress);
                            }
                            break;
                        }
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
                    case ElementDefine.VirtualElement:
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            MTPReglist = MTPReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (MTPReglist.Count != 0)
            {
                foreach (byte badd in MTPReglist)
                {
                    ret = MTPReadWord(badd, ref wdata);
                    parent.m_MTPRegImg[badd].err = ret;
                    parent.m_MTPRegImg[badd].val = wdata;
                }
            }

            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> MTPReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.MTPElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                MTPReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.OperationElement:
                    case ElementDefine.VirtualElement:
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

            MTPReglist = MTPReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            if (MTPReglist.Count != 0)
            {
                foreach (byte badd in MTPReglist)
                {
                    if (msg.gm.sflname == "Trim2")
                    {
                        if ((badd == 0x0c) || (badd == 0x14)) //Keep GPIO equal THM 
                            parent.m_MTPRegImg[badd].val = SharedFormula.MAKEWORD(SharedFormula.HiByte(parent.m_MTPRegImg[badd].val),
                                SharedFormula.HiByte(parent.m_MTPRegImg[badd].val));
                    }
                    
                    ret1 = MTPWriteWord(badd, parent.m_MTPRegImg[badd].val);
                    parent.m_MTPRegImg[badd].err = ret1;
                    ret |= ret1;
                }
            }

            foreach (byte badd in OpReglist)
            {
                if (badd < 0xC0)
                {
                    if ((badd >= 0x80) && (badd < 0x95))
                    {
                        ret = APBWriteWord(0x95, ElementDefine.PASSWORD);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    if (badd == 0x97)
                    {
                        ret = APBWriteWord(0x97, ElementDefine.PASSWORD);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        /*//ret = APBWriteWord(badd, (UInt16)(parent.m_OpRegImg[badd].val|0x0100));
                        ret = APBWriteWord(badd, (UInt16)parent.m_OpRegImg[badd].val);
                        break;*/
                    }
                    ret = APBWriteWord(badd, parent.m_OpRegImg[badd].val);
                }
                else if ((badd >= 0xC4) && (badd <= 0xCB))
                    ret = CtrlWriteWord(badd, parent.m_OpRegImg[badd].val);
                else if ((badd >= 0xD0) && (badd <= 0xE9))
                    ret = MapWriteWord(badd, parent.m_OpRegImg[badd].val);
                else
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 BitOperation(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.OperationElement:
                    case ElementDefine.VirtualElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;

                                parent.m_OpRegImg[baddress].val = 0x00;
                                parent.WriteToRegImg(p, 1);
                                OpReglist.Add(baddress);

                            }
                            break;
                        }
                }
            }

            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            foreach (byte badd in OpReglist)
            {
                ret = APBWriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MTPParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.MTPElement:
                        {
                            if (p == null) break;
                            MTPParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
                    case ElementDefine.VirtualElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        {
                            param = p;
                            m_parent.Hex2Physical(ref param);
                            break;
                        }
                }
            }

            if (MTPParamList.Count != 0)
            {
                for (int i = 0; i < MTPParamList.Count; i++)
                {
                    param = (Parameter)MTPParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }

            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MTPParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.MTPElement:
                        {
                            if (p == null) break;
                            MTPParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
                    case ElementDefine.VirtualElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        {
                            param = p;
                            m_parent.Physical2Hex(ref param);
                            break;
                        }
                }
            }

            if (MTPParamList.Count != 0)
            {
                for (int i = 0; i < MTPParamList.Count; i++)
                {
                    param = (Parameter)MTPParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt16 old_subtype = 0;
            UInt16 wval = 0;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            #region  支持基础读写指令
            if (msg.sub_task >= 0x8000)
            {
                switch ((SUB_TM)msg.sub_task)
                {
                    case SUB_TM.SUB_TM_READ:
                        {
                            switch (msg.sm.tsmbBuffer.bdata[0])
                            {
                                case 0: //Operation
                                    {
                                        ret = ReadWord((byte)msg.sm.tsmbBuffer.bdata[1], ref wval);
                                        msg.sm.tsmbBuffer.length = 2;
                                        msg.sm.tsmbBuffer.bdata[2] = SharedFormula.LoByte(wval);
                                        msg.sm.tsmbBuffer.bdata[3] = SharedFormula.HiByte(wval);
                                        break;
                                    }
                                case 1://MTP
                                    {
                                        ret = MTPReadWord((byte)msg.sm.tsmbBuffer.bdata[1], ref wval);
                                        msg.sm.tsmbBuffer.length = 2;
                                        msg.sm.tsmbBuffer.bdata[2] = SharedFormula.LoByte(wval);
                                        msg.sm.tsmbBuffer.bdata[3] = SharedFormula.HiByte(wval);
                                        break;
                                    }
                            }
                            break;
                        }
                    case SUB_TM.SUB_TM_WRITE:
                        {
                            wval = SharedFormula.MAKEWORD(msg.sm.tsmbBuffer.bdata[2], msg.sm.tsmbBuffer.bdata[3]);
                            switch (msg.sm.tsmbBuffer.bdata[0])
                            {
                                case 0: //Operation
                                    {
                                        ret = APBWriteWord((byte)msg.sm.tsmbBuffer.bdata[1], wval);
                                        msg.sm.tsmbBuffer.length = 2;
                                        break;
                                    }
                                case 1://MTP
                                    {
                                        ret = MTPWriteWord((byte)msg.sm.tsmbBuffer.bdata[1], wval);
                                        msg.sm.tsmbBuffer.length = 2;
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            #endregion
            #region 支持command指令
            switch ((ElementDefine.COBRA_COMMAND_MODE)msg.sub_task)
            {
                case ElementDefine.COBRA_COMMAND_MODE.INVALID_COMMAND:
                    {
                        var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                        #region Scan Mode
                        switch (json["ScanMode"])
                        {
                            case "TrigOne":
                                {
                                    ret = DisableAutoScan();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE,
                                         ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_SAFETY_CHECK);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    #region  添加INTEMP
                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_INTEL_TEMP);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = ReadWord(0x71, ref wval);
                                    parent.m_VirtualRegImg[0x11].val = wval;
                                    parent.m_VirtualRegImg[0x11].err = ret;
                                    #endregion

                                    #region  添加PA5，请求来自国艳，测试使用
                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_PA5);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = ReadWord(0x71, ref wval);
                                    parent.m_VirtualRegImg[0x1D].val = wval;
                                    parent.m_VirtualRegImg[0x1D].err = ret;
                                    #endregion

                                    #region  添加VBATT
                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VBATT);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = ReadWord(0x71, ref wval);
                                    parent.m_VirtualRegImg[0x14].val = wval;
                                    parent.m_VirtualRegImg[0x14].err = ret;
                                    #endregion
                                }
                                break;
                            case "TrigEight":
                                {
                                    ret = DisableAutoScan();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_SAFETY_CHECK);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    #region  添加INTEMP
                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_INTEL_TEMP);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = ReadWord(0x71, ref wval);
                                    parent.m_VirtualRegImg[0x11].val = wval;
                                    parent.m_VirtualRegImg[0x11].err = ret;
                                    #endregion

                                    #region  添加PA5，请求来自国艳，测试使用
                                    /*
                                    ret = ReadWord(0x0D, ref wval);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    wval &= 0xF3FF;
                                    wval |= 0x0400; //pa5 as ADC5
                                    ret = APBWriteWord(0x0D, wval);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_PA5);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = ReadWord(0x71, ref wval);
                                    parent.m_VirtualRegImg[0x1D].val = wval;
                                    parent.m_VirtualRegImg[0x1D].err = ret;
                                    #endregion

                                    #region  添加VBATT
                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                        ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VBATT);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = ReadWord(0x71, ref wval);
                                    parent.m_VirtualRegImg[0x14].val = wval;
                                    parent.m_VirtualRegImg[0x14].err = ret;
                                    #endregion
                                }
                                break;
                            case "AutoScanEight":
                                ret = EnableAutoScan(ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_EIGHT_MODE);
                                break;
                            case "AutoScanOne":
                                ret = EnableAutoScan(ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_ONE_MODE);
                                break;
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion

                        #region CADC
                        param = msg.task_parameterlist.GetParameterByGuid(0x00036500);
                        if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                        switch (json["CADCMode"])
                        {
                            case "Disable":
                                ret = SetCADCMode(ElementDefine.CADC_MODE.DISABLE);
                                break;
                            case "Trigger":
                                ret = SetCADCMode(ElementDefine.CADC_MODE.TRIGGER);
                                break;
                            case "Moving":
                                ret = SetCADCMode(ElementDefine.CADC_MODE.MOVING);
                                break;
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        break;
                    }
                #region Trim SFL Command
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_SLOPE_EIGHT_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);
                        #region 准备寄存器初始化
                        ret = ClearTrimAndOffset(msg.task_parameterlist);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = DisableAutoScan();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ReadWord(0x0D, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval &= 0xF3FF;
                        wval |= 0x0400; //pa5 as ADC5
                        ret = APBWriteWord(0x0D, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion

                        ret = SetCADCMode(ElementDefine.CADC_MODE.MOVING);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_SAFETY_CHECK);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = WaitTriggerCompleted();
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                                if (param.guid == ElementDefine.VirVBattAdcSel) continue;
                                if (param.guid == ElementDefine.VirPA5AdcSel) continue;

                                old_subtype = param.subtype;
                                param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;
                                m_parent.Hex2Physical(ref param);
                                param.subtype = old_subtype;
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                                //param.sphydata = string.Format("{0:F4}", param.phydata);
                            }

                            param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirPA5AdcSel);
                            if (param != null)
                            {
                                ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_PA5);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = OnWaitTriggerCompleted();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = ReadWord(0x71, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                //param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirPA5AdcSel);
                                param.phydata = (double)((double)wval * param.phyref / param.regref);
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                                //param.sphydata = string.Format("{0:F4}", param.phydata);
                            }

                            param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirVBattAdcSel);
                            if (param != null)
                            {
                                ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VBATT);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = WaitTriggerCompleted();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = ReadWord(0x71, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                //param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirVBattAdcSel);
                                param.phydata = (double)((double)wval * param.phyref / param.regref);
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;

                                //param.sphydata = string.Format("{0:F4}", param.phydata);
                            }
                        }
                        foreach (UInt32 key in dic.Keys)
                        {
                            DataPoint dataPoint = GetDataPointByGuid(key);
                            dataPoint.SetOutput(dic[key] / 8);
                        }
                        ElementDefine.m_trim_count++;
                        #endregion
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_OFFSET_EIGHT_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);
                        #region 准备寄存器初始化
                        ret = DisableAutoScan();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ReadWord(0x0D, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval &= 0xF3FF;
                        wval |= 0x0400; //pa5 as ADC5
                        ret = APBWriteWord(0x0D, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        ret = SetCADCMode(ElementDefine.CADC_MODE.MOVING);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_SAFETY_CHECK);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = WaitTriggerCompleted();
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                                if (param.guid == ElementDefine.VirVBattAdcSel) continue;
                                if (param.guid == ElementDefine.VirPA5AdcSel) continue;

                                old_subtype = param.subtype;
                                param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;
                                m_parent.Hex2Physical(ref param);
                                param.subtype = old_subtype;
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                                //param.sphydata = string.Format("{0:F4}", param.phydata);
                            }
                            param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirPA5AdcSel);
                            if (param != null)
                            {
                                ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_PA5);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = OnWaitTriggerCompleted();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = ReadWord(0x71, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                //param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirPA5AdcSel);
                                param.phydata = (double)((double)wval * param.phyref / param.regref);
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                                //param.sphydata = string.Format("{0:F4}", param.phydata);
                            }
                            param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirVBattAdcSel);
                            if (param != null)
                            {
                                ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                                ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VBATT);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = WaitTriggerCompleted();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = ReadWord(0x71, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                //param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.VirVBattAdcSel);
                                param.phydata = (double)((double)wval * param.phyref / param.regref);
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;

                                //param.sphydata = string.Format("{0:F4}", param.phydata);
                            }
                        }
                        foreach (UInt32 key in dic.Keys)
                        {
                            DataPoint dataPoint = GetDataPointByGuid(key);
                            dataPoint.SetOutput(dic[key] / 8);
                        }
                        ElementDefine.m_trim_count++;
                        #endregion
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_COUNT_SLOPE:
                    {
                        CountSlope(msg.task_parameterlist);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_COUNT_OFFSET:
                    {
                        CountOffset(msg.task_parameterlist);
                        break;
                    }
                #endregion
                #region SCS SFL Command
                case ElementDefine.COBRA_COMMAND_MODE.SCS_TRIGGER_SCAN_EIGHT_MODE:
                    {
                        ret = DisableAutoScan();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE,
                            ElementDefine.TRIGGER_SCAN_REQ_SINGLE, (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_PA5);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
                #endregion
                #region TestCtrl
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_NORMAL_MODE:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_VR25_VD33_VTS:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_OSC32K_xtal32k:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_OSC16M:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_THMx_20uA_VADC:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_VREF_VADC:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_CADC:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_VREF_CADC:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_Main_INDSG_DOC2_SC:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_Slave_DOC2_SC:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_Main_DSG_CHG:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_Charger_In_Slave_FET:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_IOT:
                case ElementDefine.COBRA_COMMAND_MODE.TEST_CTRL_Internal_Critical_Signal:
                    ret = MapWriteWord(0xCB, msg.sub_task);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                #endregion
                #region Work Mode
                case ElementDefine.COBRA_COMMAND_MODE.WORK_MODE_POWER_DOWN:
                    ret = EnterPowerDown();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                #endregion
            }
            #endregion
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)  //Bigsur
        {
            int ival = 0;
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x6F, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)SharedFormula.HiByte(wval);
            ival = (int)((SharedFormula.LoByte(wval) & 0x70) >> 4);
            deviceinfor.hwversion = ival;
            switch (ival)
            {
                case 2:
                    shwversion = "A";
                    break;
                case 3:
                    shwversion = "B";
                    break;
                case 4:
                    shwversion = "C";
                    break;
                default:
                    shwversion = "B";
                    break;
            }
            ival = (int)(SharedFormula.LoByte(wval) & 0x07);
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)(SharedFormula.LoByte(wval) & 0x07);

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (SharedFormula.HiByte(type) != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if (((SharedFormula.LoByte(type) & 0x70) >> 4) != deviceinfor.hwversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if ((SharedFormula.LoByte(type) & 0x07) != deviceinfor.hwsubversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            byte bval = 0;
            UInt32 guid = ElementDefine.OperationElement;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            //Cell Number
            ret = ReadWord(0x8A, ref wval); //Cell number
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)((wval & 0xE000) >> 13);
            if (bval <= 2) bval = 2;
            else if (bval >= 6) bval = 6;
            GetParameterByGuid(ElementDefine.CellNumber, demparameterlist.parameterlist).phydata = bval;
            for (int i = 0; i < bval - 1; i++)
            {
                guid = (UInt32)((ElementDefine.CellVoltage01 >> 8) + i) << 8;
                HideParameterByGuid(guid, demparameterlist.parameterlist,true);
            }
            for (int i = bval - 1; i < ElementDefine.TotalCellNum - 1; i++)
            {
                guid = (UInt32)((ElementDefine.CellVoltage01 >> 8) + i) << 8;
                HideParameterByGuid(guid, demparameterlist.parameterlist);
            }

            //Ext Number 03/05/0B/0D  
            ret = ReadWord(0x03, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bval = (byte)((wval & 0x0C00) >> 10);
            GetParameterByGuid(ElementDefine.THM0Config, demparameterlist.parameterlist).phydata = bval;
            if (bval != 0x02)
                HideParameterByGuid(ElementDefine.ExternalTemperature0, demparameterlist.parameterlist);
            else
                HideParameterByGuid(ElementDefine.ExternalTemperature0, demparameterlist.parameterlist,true);

            ret = ReadWord(0x05, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bval = (byte)((wval & 0x0C00) >> 10);
            GetParameterByGuid(ElementDefine.THM1Config, demparameterlist.parameterlist).phydata = bval;
            if (bval != 0x02)
                HideParameterByGuid(ElementDefine.ExternalTemperature1, demparameterlist.parameterlist);
            else
                HideParameterByGuid(ElementDefine.ExternalTemperature1, demparameterlist.parameterlist, true);

            ret = ReadWord(0x0B, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bval = (byte)((wval & 0x0C00) >> 10);
            GetParameterByGuid(ElementDefine.THM2Config, demparameterlist.parameterlist).phydata = bval;
            if (bval != 0x02)
                HideParameterByGuid(ElementDefine.ExternalTemperature2, demparameterlist.parameterlist);
            else
                HideParameterByGuid(ElementDefine.ExternalTemperature2, demparameterlist.parameterlist, true);

            ret = ReadWord(0x0D, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bval = (byte)((wval & 0x0C00) >> 10);
            GetParameterByGuid(ElementDefine.THM3Config, demparameterlist.parameterlist).phydata = bval;
            if (bval != 0x02)
            {
                HideParameterByGuid(ElementDefine.ExternalTemperature3, demparameterlist.parameterlist);
                HideParameterByGuid(ElementDefine.PA5, demparameterlist.parameterlist,true);
            }
            else
            {
                HideParameterByGuid(ElementDefine.ExternalTemperature3, demparameterlist.parameterlist, true);
                HideParameterByGuid(ElementDefine.PA5, demparameterlist.parameterlist);
            }

            //Get sisens enable
            ret = ReadWord(0x01, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_OpRegImg[0x01].val = wval;
            parent.m_OpRegImg[0x01].err = ret;
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion

        #region 其他
        public void HideParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist, bool bval = false)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid))
                {
                    param.bShow = bval;
                    break;
                }
            }
            return;
        }

        public Parameter GetParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid)) return param;
            }
            return null;
        }

        public UInt32 EnableAutoScan(ElementDefine.COBRA_COMMAND_MODE smode)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(ElementDefine.AUTO_SCAN_REG, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            switch (smode)
            {
                case ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_ONE_MODE:
                    {
                        ret = APBWriteWord(ElementDefine.AUTO_SCAN_REG, (UInt16)(((wval >> 4) << 4) | ElementDefine.AUTO_SCAN_ONE_MODE));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_EIGHT_MODE:
                    {
                        ret = APBWriteWord(ElementDefine.AUTO_SCAN_REG, (UInt16)(((wval >> 4) << 4) | ElementDefine.AUTO_SCAN_EIGHT_MODE));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
            }
            return ret;
        }

        public UInt32 EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE smode, UInt16 scan_req = 0, UInt16 scan_channel = 0)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(ElementDefine.TRIGGER_SCAN_REG, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            switch (smode)
            {
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE:
                    {
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, (UInt16)(((wval >> 15) << 15) | ElementDefine.TRIGGER_SCAN_ONE_MODE | scan_req | scan_channel));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE:
                    {
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, (UInt16)(((wval >> 15) << 15) | ElementDefine.TRIGGER_SCAN_EIGHT_MODE | scan_req | scan_channel));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
            }
            return ret;
        }

        public UInt32 WaitTriggerCompleted()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWaitTriggerCompleted();
            }
            return ret;
        }

        public UInt32 DisableAutoScan()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(ElementDefine.AUTO_SCAN_REG, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xFFFE;
            return APBWriteWord(ElementDefine.AUTO_SCAN_REG, wval);
        }

        public UInt32 ClearTrimAndOffset(ParamContainer demparameterlist)
        {
            UInt16 wval = 0;
            Reg regLow = null;
            Parameter param = null;
            Tuple<Parameter, Parameter> slope_offset = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            /*for (byte i = 0x06; i <= 0x15; i++)
                ret = MapWriteWord((byte)(i + 0xD0), 0);*/
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if (!parent.m_guid_slope_offset.ContainsKey(param.guid)) continue;
                slope_offset = parent.m_guid_slope_offset[param.guid];

                if (!slope_offset.Item1.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item1.reglist["Low"];
                ret = ReadWord((byte)(regLow.address + 0xD0), ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                //ret = WriteWord((byte)regLow.address, wval);
                ret = MapWriteWord((byte)(regLow.address + 0xD0), wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (!slope_offset.Item2.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item2.reglist["Low"];
                ret = ReadWord((byte)(regLow.address + 0xD0), ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                //ret = WriteWord((byte)regLow.address, wval);
                ret = MapWriteWord((byte)(regLow.address + 0xD0), wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }

        public UInt32 EnterPowerDown()
        {
            UInt16 memory_addr = 0x12c;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = APBWriteWord(0x97, ElementDefine.PASSWORD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            lock (m_lock)
            {
                ret = OnSetMemMode(ElementDefine.COBRA_MEMD.APB_MEMD_DIRECT_W);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(ElementDefine.MEM_DATA_LO_REG, 0xE3F4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(ElementDefine.MEM_DATA_HI_REG, 0xC1D2);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                UInt16 opcode = (UInt16)((memory_addr >> 2) << 8);
                opcode |= (UInt16)ElementDefine.COBRA_MEMORY_OP_REQ.MEMORY_OP_REQ_APB_WRITE;

                ret = OnWriteWord(ElementDefine.MEM_ADDR_REG, opcode); //i2c_reg(8'hc2), .pkt_in({{4'd0,mtp_reg_adr},8'h02} 
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }

        public UInt32 WakeUpMPTINFO()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteWord1(0x20, ElementDefine.WakeUpMPTINFOWriteCode);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    ret = OnReadWord1(0x20, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if (wdata == ElementDefine.WakeUpMPTINFOCheckCode) return LibErrorCode.IDS_ERR_SUCCESSFUL;
                    Thread.Sleep(10);
                }
                return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
        }
        #endregion

        #region CADC Mode  
        private UInt32 SetCADCMode(ElementDefine.CADC_MODE mode)       //MP version new method. Do 4 time average by HW, and we can also have the trigger flag and coulomb counter work at the same time.
        {
            ushort temp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (mode)
            {
                case ElementDefine.CADC_MODE.DISABLE:
                    ret = APBWriteWord(ElementDefine.CADCRTL_REG, 0x00);           //clear all
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.CADC_MODE.MOVING:
                    ret = ReadWord(0xB2, ref temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    temp |= 0x4000;
                    ret = APBWriteWord(0xB2, temp);                              //Clear cadc_moving_flag
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = APBWriteWord(ElementDefine.CADCRTL_REG, 0x98);           //Set cc_always_enable, moving_average_enable, sw_cadc_ctrl=0b00 Open dither
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    for (byte i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        Thread.Sleep(150);
                        ret = ReadWord(0xB2, ref temp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        if ((temp & 0x4000) == 0x4000)
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReadWord(ElementDefine.FinalCadcMovingData_Reg, ref temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    parent.m_OpRegImg[ElementDefine.FinalCadcMovingData_Reg].err = ret;
                    parent.m_OpRegImg[ElementDefine.FinalCadcMovingData_Reg].val = temp;
                    break;
                case ElementDefine.CADC_MODE.TRIGGER:
                    ret = ReadWord(0xB2, ref temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    temp |= 0x2000;
                    ret = APBWriteWord(0xB2, temp);                              //Clear cadc_moving_flag
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = APBWriteWord(ElementDefine.CADCRTL_REG, 0x06);        //Set cadc_one_or_four, sw_cadc_ctrl=0b10
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    for (byte i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        Thread.Sleep(150);
                        ret = ReadWord(0xB2, ref temp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        if ((temp & 0x2000) == 0x2000) return ret;
                        ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReadWord(ElementDefine.FinalCadcTriggerData_Reg, ref temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    parent.m_OpRegImg[ElementDefine.FinalCadcTriggerData_Reg].err = ret;
                    parent.m_OpRegImg[ElementDefine.FinalCadcTriggerData_Reg].val = temp;
                    break;
            }

            return ret;
        }
        #endregion

        #region Trim Count
        private void InitDataPointList(ParamContainer demparameterlist)
        {//建构DataPoint清单，并获取input值
            DataPoint dataPoint = null;
            Parameter param = null;

            if ((ElementDefine.m_trim_count == 0) | (ElementDefine.m_trim_count == 5))
            {
                ElementDefine.m_trim_count = 0;
                m_dataPoint_List.Clear();
            }
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                dataPoint = GetDataPointByGuid(param.guid);
                if (dataPoint == null)
                {
                    dataPoint = new DataPoint(param);
                    dataPoint.SetInput(param.phydata);
                    m_dataPoint_List.Add(dataPoint);
                }
                else
                    dataPoint.SetInput(param.phydata);
            }
        }

        private DataPoint GetDataPointByGuid(UInt32 guid)
        {
            DataPoint dataPoint = m_dataPoint_List.Find(delegate(DataPoint item)
            {
                return item.parent.guid.Equals(guid);
            }
            );
            if (dataPoint != null) return dataPoint;
            else return null;
        }

        private UInt32 CountSlope(ParamContainer demparameterlist)
        {
            double slope = 0;
            DataPoint dataPoint = null;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                dataPoint = GetDataPointByGuid(param.guid);
                if (dataPoint == null) continue; //Offset Slope parameter no data point
                else
                {
                    ret = dataPoint.GetSlope(ref slope);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                switch (param.guid)
                {
                    case ElementDefine.CellVoltage01:
                        param = GetParameterByGuid(ElementDefine.Cell1_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.Cell2_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage03:
                        param = GetParameterByGuid(ElementDefine.Cell3_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage04:
                        param = GetParameterByGuid(ElementDefine.Cell4_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage05:
                        param = GetParameterByGuid(ElementDefine.Cell5_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage06:
                        param = GetParameterByGuid(ElementDefine.Cell6_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage07:
                        param = GetParameterByGuid(ElementDefine.Cell7_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage08:
                        param = GetParameterByGuid(ElementDefine.Cell8_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage09:
                        param = GetParameterByGuid(ElementDefine.Cell9_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage10:
                        param = GetParameterByGuid(ElementDefine.Cell10_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.MainPackCur:
                        param = GetParameterByGuid(ElementDefine.MainPackCur_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.FinalPackCur:
                        param = GetParameterByGuid(ElementDefine.FinalPackCur_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VBATT:
                        param = GetParameterByGuid(ElementDefine.VBATT_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.PA5:
                        param = GetParameterByGuid(ElementDefine.PA5_Slope_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                param.phydata = Math.Round((slope - 1)*2048,0);
                ConvertSlope(ref param);
            }
            return ret;
        }

        private UInt32 CountOffset(ParamContainer demparameterlist)
        {
            double offset = 0;
            DataPoint dataPoint = null;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                dataPoint = GetDataPointByGuid(param.guid);
                if (dataPoint == null) continue; //Offset Slope parameter no data point
                else
                {
                    ret = dataPoint.GetOffset(ref offset);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                switch (param.guid)
                {
                    case ElementDefine.CellVoltage01:
                        param = GetParameterByGuid(ElementDefine.Cell1_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.Cell2_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage03:
                        param = GetParameterByGuid(ElementDefine.Cell3_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage04:
                        param = GetParameterByGuid(ElementDefine.Cell4_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage05:
                        param = GetParameterByGuid(ElementDefine.Cell5_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage06:
                        param = GetParameterByGuid(ElementDefine.Cell6_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage07:
                        param = GetParameterByGuid(ElementDefine.Cell7_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage08:
                        param = GetParameterByGuid(ElementDefine.Cell8_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage09:
                        param = GetParameterByGuid(ElementDefine.Cell9_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage10:
                        param = GetParameterByGuid(ElementDefine.Cell10_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.MainPackCur:
                        param = GetParameterByGuid(ElementDefine.MainPackCur_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.FinalPackCur:
                        param = GetParameterByGuid(ElementDefine.FinalPackCur_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VBATT:
                        param = GetParameterByGuid(ElementDefine.VBATT_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.PA5:
                        param = GetParameterByGuid(ElementDefine.PA5_Offset_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                param.phydata = offset;
            }
            return ret;
        }

        private void ConvertSlope(ref Parameter param)
        {
            double slop = param.phydata; 
            UInt16 wdata, tr = 0;
            Reg regLow = null, regHi = null;

            if (slop >= 0) return;
            foreach (KeyValuePair<string, Reg> dic in param.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }
            if (regHi != null)
                tr = (UInt16)(regHi.bitsnumber + regLow.bitsnumber-1);
            else
                tr = (UInt16)(regLow.bitsnumber-1);

            
            wdata = (UInt16)(1 << tr);
            wdata |= (UInt16)Math.Abs(slop);
            param.phydata = (double)wdata;
        }
        #endregion
    }
}