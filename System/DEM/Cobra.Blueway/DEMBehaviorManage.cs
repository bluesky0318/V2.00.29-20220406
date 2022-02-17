using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using Cobra.Communication;
using Cobra.Common;
using System.Text;

namespace Cobra.Blueway
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
        private Dictionary<string, string> m_Json_Options = null;
        private CCommunicateManager m_Interface = new CCommunicateManager();
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
        protected UInt32 MFBlockAccessRead(byte scmd, ref TSMBbuffer pval)
        {
            byte[] bdata = new byte[] { scmd, 0x00 };
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 2;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockRead(0x16, 0xF9, ref pval);
            return ret;
        }
        internal UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }
        internal UInt32 MTPReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnMTPReadWord(reg, ref pval);
            }
            return ret;
        }
        internal UInt32 BlockRead(ref TASKMessage msg)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(ref msg);
            }
            return ret;
        }
        protected UInt32 BlockRead(byte bus_addr, byte cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(bus_addr, cmd, ref pval);
            }
            return ret;
        }
        internal UInt32 ParameterRead(UInt32 reg, ref TSMBbuffer pval)
        {
            byte[] bdata = new byte[] { 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00 };
            bdata[2] = (byte)reg;
            bdata[3] = (byte)(reg >> 8);
            bdata[4] = (byte)(reg >> 16);
            bdata[5] = (byte)(reg >> 24);
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 6;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockRead(0x16, 0xF9, ref pval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
        internal UInt32 MTPWriteWord(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnMTPWriteWord(reg, pval);
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
        internal UInt32 BlockWrite(ref TASKMessage msg)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(ref msg);
            }
            return ret;
        }
        protected UInt32 BlockWrite(byte bus_addr, byte cmd, TSMBbuffer val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(bus_addr, cmd, val);
            }
            return ret;
        }
        internal UInt32 ParameterWrite(UInt32 reg, byte[] pval)
        {
            byte[] bdata = null;
            if (reg < (ElementDefine.PARAM_MF_StartAddress + ElementDefine.Project_StartAddress))
                bdata = new byte[7 + pval.Length];
            else
                bdata = new byte[6 + pval.Length];
            bdata[0] = 0xAA;
            bdata[1] = 0x00;
            bdata[2] = (byte)reg;
            bdata[3] = (byte)(reg >> 8);
            bdata[4] = (byte)(reg >> 16);
            bdata[5] = (byte)(reg >> 24);
            if (reg < (ElementDefine.PARAM_MF_StartAddress + ElementDefine.Project_StartAddress))
            {
                bdata[6] = (byte)pval.Length;
                Array.Copy(pval, 0, bdata, 7, pval.Length);
            }
            else
                Array.Copy(pval, 0, bdata, 6, pval.Length);
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = (UInt16)bdata.Length;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
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
        protected UInt32 OnBlockRead(byte bus_addr, byte cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = bus_addr;
            sendbuf[1] = cmd;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref t, ref DataOutLen, pval.length))
                {
                    pval.bdata = t;
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }
        protected UInt32 OnBlockRead(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
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
        protected UInt32 OnBlockWrite(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            return ret;
        }
        protected UInt32 OnBlockWrite(byte bus_addr, byte cmd, TSMBbuffer val)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)val.length;
            byte[] sendbuf = new byte[DataInLen + 2];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = bus_addr;
            sendbuf[1] = cmd;
            Array.Copy(val.bdata, 0, sendbuf, 2, val.length);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen)) //valid data and pec
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
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
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (string.Compare(msg.gm.sflname, "BlackBox") == 0)
                ret = ClearLog();
            else
            {
                msg.controlmsg.message = "Begin to handshake...";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                ret = Handshake(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    ret = ExistFWUpdateMode();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = Reset();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = Handshake(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                msg.controlmsg.message = "Begin to mass erase...";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                ret = MassErase();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }
        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        public UInt32 Read(ref TASKMessage msg)
        {
            byte bcmd = 0, sbcmd = 0;
            UInt16 uadd = 0;
            Reg regLow = null;
            TSMBbuffer tsmBuffer = new TSMBbuffer();
            List<byte> SBSReglist = new List<byte>();
            List<byte> F9Cmd_list = new List<byte>();
            List<Parameter> ProjParamlist = new List<Parameter>();
            List<Parameter> LogParamList = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            F9Cmd_list.Clear();
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.PrjParamElement:
                        {
                            if (p == null) break;
                            ProjParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            if (bcmd == 0xF9)
                            {
                                regLow = p.reglist["Low"];
                                sbcmd = (byte)regLow.address;
                                F9Cmd_list.Add(sbcmd);
                            }
                            SBSReglist.Add(bcmd);
                            break;
                        }
                    case ElementDefine.LogElement:
                        {
                            if (p == null) break;
                            LogParamList.Add(p);
                            break;
                        }
                }
            }
            SBSReglist = SBSReglist.Distinct().ToList();
            F9Cmd_list = F9Cmd_list.Distinct().ToList();

            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_SBSMode_Dic.ContainsKey(badd)) continue;
                if (badd == 0xF9)
                {
                    foreach (byte sc in F9Cmd_list)
                    {
                        tsmBuffer = parent.m_F9Mode_Dic[sc];
                        ret = MFBlockAccessRead(sc, ref tsmBuffer);
                    }
                }
                else
                {
                    tsmBuffer = parent.m_SBSMode_Dic[badd];
                    ret = BlockRead(0x16, badd, ref tsmBuffer);
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            if (LogParamList.Count != 0)
                ret = ReadLogArea(0, ref parent.logAreaArray);
            if (ProjParamlist.Count != 0)
            {
                foreach (Parameter p in ProjParamlist)
                {
                    uadd = p.reglist["Low"].address;
                    if (uadd < ElementDefine.PARAM_MF_StartAddress) tsmBuffer.length = 5;
                    else if (uadd == 0x6FEC) continue;
                    else tsmBuffer.length = 32;
                    ret = ParameterRead(uadd + ElementDefine.Project_StartAddress, ref tsmBuffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    Array.Copy(tsmBuffer.bdata, 1, parent.m_ProjParamImg, (uadd - ElementDefine.ParameterArea_StartAddress), (tsmBuffer.length - 1));
                    Array.Copy(tsmBuffer.bdata, 0, p.tsmbBuffer.bdata, 0, tsmBuffer.length);
                    p.tsmbBuffer.length = tsmBuffer.length;
                }
            }
            return ret;
        }
        public UInt32 Write(ref TASKMessage msg)
        {
            byte len = 0;
            byte bcmd = 0;
            UInt16 ucmd = 0;
            byte[] bdata = null;
            TSMBbuffer tsmBuffer = new TSMBbuffer();
            List<byte> SBSReglist = new List<byte>();
            List<UInt16> ProjParamlist = new List<UInt16>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.PrjParamElement:
                        {
                            if (p == null) break;
                            ucmd = (UInt16)(p.guid & ElementDefine.CommandMask1);
                            ProjParamlist.Add(ucmd);
                            break;
                        }
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            SBSReglist.Add(bcmd);
                            break;
                        }
                }
            }
            SBSReglist = SBSReglist.Distinct().ToList();
            ProjParamlist = ProjParamlist.Distinct().ToList();
            foreach (UInt16 uadd in ProjParamlist)
            {
                if (uadd < ElementDefine.PARAM_MF_StartAddress) len = 4;
                else len = (byte)(parent.m_ProjParamImg[(uadd - ElementDefine.ParameterArea_StartAddress)] + 1);
                bdata = new byte[len];
                Array.Copy(parent.m_ProjParamImg, (uadd - ElementDefine.ParameterArea_StartAddress), bdata, 0, len);
                ret = ParameterWrite(uadd + ElementDefine.Project_StartAddress, bdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_SBSMode_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_SBSMode_Dic[badd];
                ret = BlockWrite(0x16, badd, tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            return ret;
        }
        public UInt32 BitOperation(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            Parameter param = null;
            List<Parameter> parameterList = new List<Parameter>();
            List<Parameter> SBSParamList = new List<Parameter>();
            List<Parameter> LogParamList = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            SBSParamList.Add(p);
                            break;
                        }
                    case ElementDefine.LogElement:
                        {
                            if (p == null) break;
                            LogParamList.Add(p);
                            break;
                        }
                    case ElementDefine.PrjParamElement:
                        {
                            if (p == null) break;
                            parameterList.Add(p);
                            break;
                        }
                }
            }

            if (SBSParamList.Count != 0)
            {
                for (int i = 0; i < SBSParamList.Count; i++)
                {
                    param = (Parameter)SBSParamList[i];
                    if (param == null) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (LogParamList.Count != 0)
            {
                for (int i = 0; i < LogParamList.Count; i++)
                {
                    param = (Parameter)LogParamList[i];
                    if (param == null) continue;
                    m_parent.Hex2Physical(ref param);
                }
            }
            if (parameterList.Count != 0)
            {
                for (int i = 0; i < parameterList.Count; i++)
                {
                    param = (Parameter)parameterList[i];
                    if (param == null) continue;
                    m_parent.Hex2Physical(ref param);
                }
            }
            return ret;
        }
        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            List<Parameter> SBSParamList = new List<Parameter>();
            List<Parameter> parameterList = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            SBSParamList.Add(p);
                            break;
                        }
                    case ElementDefine.PrjParamElement:
                        {
                            if (p == null) break;
                            parameterList.Add(p);
                            break;
                        }
                }
            }
            if (SBSParamList.Count != 0)
            {
                for (int i = 0; i < SBSParamList.Count; i++)
                {
                    param = (Parameter)SBSParamList[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            if (parameterList.Count != 0)
            {
                if (parameterList.Count == 1)
                    return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;
                UpdataProjecInformation(msg);
                Array.Clear(m_parent.m_ProjParamImg, 0, m_parent.m_ProjParamImg.Length);
                for (int i = 0; i < parameterList.Count; i++)
                {
                    param = (Parameter)parameterList[i];
                    if (param == null) continue;
                    m_parent.Physical2Hex(ref param);
                    if (param.guid == ElementDefine.AuthenticationKey)
                    {
                        if (param.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL) return param.errorcode;
                    }
                }
                if (msg.flashData != null)
                    Array.Copy(m_parent.m_ProjParamImg, 0, msg.flashData, ElementDefine.ParameterArea_StartAddress, m_parent.m_ProjParamImg.Length);
                CountCheckSum(ref msg);
            }

            return ret;
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            bool bSwitch = false;
            string tmp = string.Empty;
            byte[] sHMAC = null, fHMAC = null;
            StringBuilder strBuild = new StringBuilder();
            byte[] bdata = new byte[ElementDefine.BLOCK_OPERATION_BYTES];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            if (m_Json_Options.ContainsKey("SFL"))
            {
                if ((string.Compare(m_Json_Options["SFL"], "SBS") == 0) && (msg.sub_task == 0x32))
                {
                    if (m_Json_Options["SHA-1 Switch"] != null)
                        tmp = m_Json_Options["SHA-1 Switch"];
                    bSwitch = Convert.ToBoolean(tmp);
                    if (!bSwitch) return LibErrorCode.IDS_ERR_SUCCESSFUL;
                    if (m_Json_Options["SHA-1 Plaintext"] != null)
                        tmp = m_Json_Options["SHA-1 Plaintext"];
                    if (tmp.Length != 32)
                        return ElementDefine.IDS_ERR_DEM_AUTHKEY_LEN_ILLEGAL;
                    if (parent.m_dem_dm.IsIllegalHexadecimal(tmp))
                        return ElementDefine.IDS_ERR_DEM_AUTHKEY_DATA_ILLEGAL;
                    string[] subtmp = parent.m_dem_dm.subString(tmp, 2);
                    for (int i = 0; i < subtmp.Length; i++)
                        parent.m_hMAC.Authen_Key[i] = Byte.Parse(subtmp[i], System.Globalization.NumberStyles.HexNumber);
                    (new Random()).NextBytes(parent.m_hMAC.Random_Key);
                    using (var sha1 = SHA1.Create())
                    {
                        strBuild.Clear();
                        var key = parent.m_hMAC.Authen_Key.Concat(parent.m_hMAC.Random_Key).ToArray();
                        var key1 = sha1.ComputeHash(key);
                        var key2 = parent.m_hMAC.Authen_Key.Concat(key1).ToArray();
                        sHMAC = sha1.ComputeHash(key2);
                        strBuild.Append("sHMAC:");
                        foreach (byte k in sHMAC)
                            strBuild.Append(string.Format("-{0:x2}", k));
                    }
                    ret = Authentication(parent.m_hMAC.Random_Key, ref fHMAC);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    strBuild.Append("\nfHMAC:");
                    foreach (byte k in fHMAC)
                        strBuild.Append(string.Format("-{0:x2}", k));
                    FolderMap.WriteFile(strBuild.ToString());
                    for (int i = 0; i < sHMAC.Length; i++)
                    {
                        if (sHMAC[i] != fHMAC[i + 1]) return ElementDefine.IDS_ERR_DEM_FAILED_AUTHKEY_COMPARE;
                    }
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                return ret;
            }
            if (m_Json_Options.ContainsKey("Button"))
            {
                switch (m_Json_Options["Button"])
                {
                    case "FullDownloadPrj":
                        msg.controlmsg.message = "Begin to handshake...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                        ret = Handshake(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            ret = ExistFWUpdateMode();
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            ret = Reset();
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            ret = Handshake(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }
                        msg.controlmsg.message = "Begin to mass erase...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                        ret = MassErase();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Thread.Sleep(1000);
                        msg.controlmsg.message = "Begin to download FW...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                        for (UInt32 fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.BLOCK_OPERATION_BYTES)
                        {
                            ret = SetupAddress(fAddr + ElementDefine.Project_StartAddress);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            Array.Copy(msg.flashData, fAddr, bdata, 0, bdata.Length);
                            ret = WriteData(bdata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }
                        msg.controlmsg.message = "Begin to verify checksum...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                        ret = CheckSumVerify(msg.flashData);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        msg.controlmsg.message = "Exit update mode...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                        ret = ExistFWUpdateMode();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        msg.controlmsg.message = "Reset chip...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                        ret = Reset();
                        break;
                }
            }
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
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return LibErrorCode.IDS_ERR_SUCCESSFUL;

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
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            byte bcmd = 0;
            byte baddr = 0x16;
            UInt32 wdata = 0;
            Parameter param = null;
            TSMBbuffer tsmBuffer = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if (p == null) continue;
                p.bShow = true;
                p.tsmbBuffer.length = 2;
            }

            param = GetParameterByGuid(ElementDefine.MfgName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = 32;
            param = GetParameterByGuid(ElementDefine.DevName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = 32;
            param = GetParameterByGuid(ElementDefine.DevChem, demparameterlist.parameterlist);
            param.tsmbBuffer.length = 32;
            param = GetParameterByGuid(ElementDefine.MfgData, demparameterlist.parameterlist);
            param.tsmbBuffer.length = 32;
            //0x18~0x1c
            for (int i = 0; i < 5; i++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.DesignCap + (i << 8)), demparameterlist.parameterlist);
                bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
                tsmBuffer = param.tsmbBuffer;
                param.errorcode = ret = BlockRead(baddr, bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }
            //0x20~0x23
            for (int i = 0; i < 4; i++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.MfgName + (i << 8)), demparameterlist.parameterlist);
                bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
                tsmBuffer = param.tsmbBuffer;
                param.errorcode = ret = BlockRead(baddr, bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }
            param = GetParameterByGuid(ElementDefine.FWVersion, demparameterlist.parameterlist);
            ret = FWVersion(ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            param.phydata = (double)wdata;

            param = GetParameterByGuid(ElementDefine.FGVersion, demparameterlist.parameterlist);
            ret = FGVersion(ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            param.phydata = (double)wdata;
            return ret;
        }
        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        public UInt32 ReadDevice(ref TASKMessage msg)
        {
            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
            string[] scmd = json["command"].Split(' ');
            byte[] bcmd = new byte[scmd.Length];
            for (int i = 0; i < bcmd.Length; i++)
                bcmd[i] = byte.Parse(scmd[i], System.Globalization.NumberStyles.HexNumber);

            UInt16 wDataInLength = UInt16.Parse(json["length"]);
            if (string.Compare(json["crc"].ToLower(), "none") != 0)
                wDataInLength++;

            byte[] yDataOut = new byte[wDataInLength];
            byte[] yDataIn = new byte[bcmd.Length + 1];
            yDataIn[0] = baddr;
            Array.Copy(bcmd, 0, yDataIn, 1, bcmd.Length);

            UInt16 wDataOutLength = 0;
            UInt16 wDataInWrite = (UInt16)bcmd.Length;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (!m_Interface.ReadDevice(yDataIn, ref yDataOut, ref wDataOutLength, wDataInLength, wDataInWrite))
                ret = LibErrorCode.IDS_ERR_COM_COM_TIMEOUT;
            else
            {
                msg.flashData = new byte[wDataOutLength];
                Array.Copy(yDataOut, 0, msg.flashData, 0, wDataOutLength);
            }
            return ret;
        }
        public UInt32 WriteDevice(ref TASKMessage msg)
        {
            UInt16 wDataOutLength = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
            string[] scmd = json["command"].Trim().Split(' ');
            string[] sdata = json["data"].Trim().Split(' ');

            UInt16 wDataInLength = (UInt16)(scmd.Length + 1 + sdata.Length);
            if (string.Compare(json["crc"].ToLower(), "none") != 0)
                wDataInLength++;

            byte[] yDataOut = new byte[1];
            byte[] yDataIn = new byte[wDataInLength];
            yDataIn[0] = (byte)baddr;//(byte)ElementDefine.COBRA_CMD.CMD_WRTIE;
            for (int n = 0; n < scmd.Length; n++)
                yDataIn[n + 1] = byte.Parse(scmd[n], System.Globalization.NumberStyles.HexNumber);

            for (int n = 0; n < sdata.Length; n++)
                yDataIn[n + 1 + scmd.Length] = byte.Parse(sdata[n], System.Globalization.NumberStyles.HexNumber);
            switch (json["crc"].ToLower())
            {
                case "crc8":
                    yDataIn[wDataInLength - 1] = crc8_calc(ref yDataIn, (UInt16)(wDataInLength - 1));
                    break;
                case "crc4":
                    break;
            }

            if (!m_Interface.WriteDevice(yDataIn, ref yDataOut, ref wDataOutLength, (UInt16)(wDataInLength - 2)))
                ret = LibErrorCode.IDS_ERR_COM_COM_TIMEOUT;
            return ret;
        }
        #endregion

        #region Log Aread Access
        private UInt32 ReadLogArea(byte nlog, ref byte[] btmp)
        {
            byte[] bdata = null;
            TSMBbuffer rBuffer = new TSMBbuffer();
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            rBuffer.length = 32;
            wBuffer.length = 6;
            bdata = new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Array.Clear(btmp, 0, btmp.Length);
            //latest data #1
            for (int n = 0; n < 4; n++)
            {
                bdata[2] = nlog;
                bdata[3] = (byte)(n + 1);
                wBuffer.bdata = bdata;
                ret = BlockWrite(0x16, 0xF9, wBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = BlockRead(0x16, 0xF9, ref rBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                Array.Copy(rBuffer.bdata, 1, btmp, n * (rBuffer.length - 1), (rBuffer.length - 1));
            }
            bdata[3] = 0x05;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            rBuffer.length = 0x15;
            ret = BlockRead(0x16, 0xF9, ref rBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Array.Copy(rBuffer.bdata, 1, btmp, 4 * (rBuffer.length - 1), (rBuffer.length - 1));
            return ret;
        }
        private UInt32 uploadEntireLogArea()
        {
            byte[] bval = new byte[144];
            string fullpath = FolderMap.m_logs_folder + "LogEntireArea " + DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + ".bin";
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte n = 1; n < 4; n++)
            {
                ret = ReadLogArea(n, ref bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                Array.Copy(bval, 0, parent.logEntireAreaArray, (n - 1) * ElementDefine.Log_Page_Size, ElementDefine.Log_Page_Size);
            }
            SaveFile(fullpath, ref parent.logEntireAreaArray);
            return ret;

        }
        internal void SaveFile(string fullpath, ref byte[] bdata)
        {
            FileInfo file = new FileInfo(@fullpath);

            // Open the stream for writing. 
            using (FileStream fs = file.OpenWrite())
                fs.Write(bdata, 0, bdata.Length);// from load hex
        }
        #endregion

        #region Others
        private UInt32[] crcTable =
{
          0x00000000, 0x04c11db7, 0x09823b6e, 0x0d4326d9, 0x130476dc, 0x17c56b6b, 0x1a864db2, 0x1e475005,
          0x2608edb8, 0x22c9f00f, 0x2f8ad6d6, 0x2b4bcb61, 0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd,
          0x4c11db70, 0x48d0c6c7, 0x4593e01e, 0x4152fda9, 0x5f15adac, 0x5bd4b01b, 0x569796c2, 0x52568b75,
          0x6a1936c8, 0x6ed82b7f, 0x639b0da6, 0x675a1011, 0x791d4014, 0x7ddc5da3, 0x709f7b7a, 0x745e66cd,
          0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039, 0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5,
          0xbe2b5b58, 0xbaea46ef, 0xb7a96036, 0xb3687d81, 0xad2f2d84, 0xa9ee3033, 0xa4ad16ea, 0xa06c0b5d,
          0xd4326d90, 0xd0f37027, 0xddb056fe, 0xd9714b49, 0xc7361b4c, 0xc3f706fb, 0xceb42022, 0xca753d95,
          0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1, 0xe13ef6f4, 0xe5ffeb43, 0xe8bccd9a, 0xec7dd02d,
          0x34867077, 0x30476dc0, 0x3d044b19, 0x39c556ae, 0x278206ab, 0x23431b1c, 0x2e003dc5, 0x2ac12072,
          0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16, 0x018aeb13, 0x054bf6a4, 0x0808d07d, 0x0cc9cdca,
          0x7897ab07, 0x7c56b6b0, 0x71159069, 0x75d48dde, 0x6b93dddb, 0x6f52c06c, 0x6211e6b5, 0x66d0fb02,
          0x5e9f46bf, 0x5a5e5b08, 0x571d7dd1, 0x53dc6066, 0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba,
          0xaca5c697, 0xa864db20, 0xa527fdf9, 0xa1e6e04e, 0xbfa1b04b, 0xbb60adfc, 0xb6238b25, 0xb2e29692,
          0x8aad2b2f, 0x8e6c3698, 0x832f1041, 0x87ee0df6, 0x99a95df3, 0x9d684044, 0x902b669d, 0x94ea7b2a,
          0xe0b41de7, 0xe4750050, 0xe9362689, 0xedf73b3e, 0xf3b06b3b, 0xf771768c, 0xfa325055, 0xfef34de2,
          0xc6bcf05f, 0xc27dede8, 0xcf3ecb31, 0xcbffd686, 0xd5b88683, 0xd1799b34, 0xdc3abded, 0xd8fba05a,
          0x690ce0ee, 0x6dcdfd59, 0x608edb80, 0x644fc637, 0x7a089632, 0x7ec98b85, 0x738aad5c, 0x774bb0eb,
          0x4f040d56, 0x4bc510e1, 0x46863638, 0x42472b8f, 0x5c007b8a, 0x58c1663d, 0x558240e4, 0x51435d53,
          0x251d3b9e, 0x21dc2629, 0x2c9f00f0, 0x285e1d47, 0x36194d42, 0x32d850f5, 0x3f9b762c, 0x3b5a6b9b,
          0x0315d626, 0x07d4cb91, 0x0a97ed48, 0x0e56f0ff, 0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623,
          0xf12f560e, 0xf5ee4bb9, 0xf8ad6d60, 0xfc6c70d7, 0xe22b20d2, 0xe6ea3d65, 0xeba91bbc, 0xef68060b,
          0xd727bbb6, 0xd3e6a601, 0xdea580d8, 0xda649d6f, 0xc423cd6a, 0xc0e2d0dd, 0xcda1f604, 0xc960ebb3,
          0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7, 0xae3afba2, 0xaafbe615, 0xa7b8c0cc, 0xa379dd7b,
          0x9b3660c6, 0x9ff77d71, 0x92b45ba8, 0x9675461f, 0x8832161a, 0x8cf30bad, 0x81b02d74, 0x857130c3,
          0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640, 0x4e8ee645, 0x4a4ffbf2, 0x470cdd2b, 0x43cdc09c,
          0x7b827d21, 0x7f436096, 0x7200464f, 0x76c15bf8, 0x68860bfd, 0x6c47164a, 0x61043093, 0x65c52d24,
          0x119b4be9, 0x155a565e, 0x18197087, 0x1cd86d30, 0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec,
          0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088, 0x2497d08d, 0x2056cd3a, 0x2d15ebe3, 0x29d4f654,
          0xc5a92679, 0xc1683bce, 0xcc2b1d17, 0xc8ea00a0, 0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb, 0xdbee767c,
          0xe3a1cbc1, 0xe760d676, 0xea23f0af, 0xeee2ed18, 0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4,
          0x89b8fd09, 0x8d79e0be, 0x803ac667, 0x84fbdbd0, 0x9abc8bd5, 0x9e7d9662, 0x933eb0bb, 0x97ffad0c,
          0xafb010b1, 0xab710d06, 0xa6322bdf, 0xa2f33668, 0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4
        };
        public uint crc32_calc(byte[] bytes)
        {
            uint iCount = (uint)bytes.Length;
            uint crc = 0xFFFFFFFF;

            for (uint i = 0; i < iCount; i++)
            {
                crc = (crc << 8) ^ crcTable[(crc >> 24) ^ bytes[i]];
            }

            return crc;
        }
        public Parameter GetParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist)
        {
            foreach (Parameter param in parameterlist)
            {
                if (param.guid.Equals(guid))
                    return param;
            }
            return null;
        }
        private void UpdataProjecInformation(TASKMessage msg)
        {
            Parameter param = null;
            param = GetParameterByGuid(ElementDefine.PARM_BCFG_RSENSEMAIN, msg.task_parameterlist.parameterlist);
            if (param != null) parent.Proj_Rsense = param.phydata * 1000;
            else parent.Proj_Rsense = 2500;
        }
        private void CountCheckSum(ref TASKMessage msg)
        {
            uint parameterCheckSum = 0;
            int parameterCheckSumLen = (int)(ElementDefine.OFFSET_PARAM_VALUE_END - ElementDefine.OFFSET_PARAM_VALUE_START);
            byte[] btmp = new byte[parameterCheckSumLen];
            Array.Copy(msg.flashData, (int)ElementDefine.OFFSET_PARAM_VALUE_START, btmp, 0, parameterCheckSumLen);
            parameterCheckSum = crc32_calc(btmp);
            msg.flashData[ElementDefine.OFFSET_PARAM_CHECKSUM] = (byte)parameterCheckSum;
            msg.flashData[ElementDefine.OFFSET_PARAM_CHECKSUM + 1] = (byte)(parameterCheckSum >> 8);
            msg.flashData[ElementDefine.OFFSET_PARAM_CHECKSUM + 2] = (byte)(parameterCheckSum >> 16);
            msg.flashData[ElementDefine.OFFSET_PARAM_CHECKSUM + 3] = (byte)(parameterCheckSum >> 24);
            parent.m_ProjParamImg[(ElementDefine.Parameter_CheckSumAddress - ElementDefine.ParameterArea_StartAddress)] = (byte)parameterCheckSum;
            parent.m_ProjParamImg[(ElementDefine.Parameter_CheckSumAddress - ElementDefine.ParameterArea_StartAddress) + 1] = (byte)(parameterCheckSum >> 8);
            parent.m_ProjParamImg[(ElementDefine.Parameter_CheckSumAddress - ElementDefine.ParameterArea_StartAddress) + 2] = (byte)(parameterCheckSum >> 16);
            parent.m_ProjParamImg[(ElementDefine.Parameter_CheckSumAddress - ElementDefine.ParameterArea_StartAddress) + 3] = (byte)(parameterCheckSum >> 24);
        }
        #endregion

        #region FW Update
        private UInt32 Handshake(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            DateTime totalTime = DateTime.Now;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < 4; i++)
            {
                ret = ReadWord(0x6F, ref wval);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                ret = ElementDefine.IDS_ERR_DEM_RECONNECT_CHARGER;
                msg.controlmsg.message = LibErrorCode.GetErrorDescription(ret);
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
                ret = EnterBootMode(ref msg);
                return ret;
            }
            ret = FWHandshake(ref msg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = EnterBootMode(ref msg);
            return ret;
        }
        private UInt32 FWHandshake(ref TASKMessage msg)
        {
            UInt32 wval = 0;
            byte[] bdata = new byte[] { 0x0F, 0xA5, 0xCD };
            TSMBbuffer wBuffer = new TSMBbuffer();
            TSMBbuffer rBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 3;
            wBuffer.bdata = bdata;
            //The first time
            for (int i = 0; i < 10; i++)
            {
                ret = BlockWrite(0x3C, 0xF8, wBuffer);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    ret = BlockRead(0x3C, 0xF8, ref rBuffer);
                    if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                ret = FWVersion(ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                for (int j = 0; j < 3; j++)
                {
                    ret = FWVersion(ref wval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    ret = FWUpdateMode();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    break;
                }
                break;
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                ret = HWReset();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            //The second time
            ret = EnterBootMode(ref msg);
            return ret;
        }
        private UInt32 MassErase()
        {
            byte[] bdata = new byte[] { 0x0F, 0xA5, 0x66 };
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 3;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x3C, 0xFC, wBuffer);
            return ret;
        }
        private UInt32 SetupAddress(UInt32 wval)
        {
            byte[] bdata = new byte[4];
            bdata[0] = (byte)wval;
            bdata[1] = (byte)(wval >> 8);
            bdata[2] = (byte)(wval >> 16);
            bdata[3] = (byte)(wval >> 24);
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 4;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x3C, 0xF9, wBuffer);
            return ret;
        }
        private UInt32 WriteData(byte[] buff)
        {
            if ((buff == null) | (buff.Length != 32)) return ElementDefine.IDS_ERR_DEM_INVALID_BUFFER;
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 32;
            wBuffer.bdata = buff;
            ret = BlockWrite(0x3C, 0xF7, wBuffer);
            return ret;
        }
        private UInt32 CheckSumVerify(byte[] image)
        {
            UInt32 swSum = 0, hwSum = 0;
            TSMBbuffer rBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            rBuffer.length = 4;
            ret = BlockRead(0x3C, 0xFA, ref rBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            swSum = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(rBuffer.bdata[0], rBuffer.bdata[1]),
                SharedFormula.MAKEWORD(rBuffer.bdata[2], rBuffer.bdata[3]));
            hwSum = crc32_calc(image);
            FolderMap.WriteFile(string.Format("CRC:HW-{0:X4},SW-{1:X4}", hwSum, swSum));
            if (swSum != hwSum) return ElementDefine.IDS_ERR_DEM_CRC16_COMPARE;
            return ret;
        }
        private UInt32 ExistFWUpdateMode()
        {
            byte[] bdata = new byte[] { 0x0F, 0xA5, 0xB0 };
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 3;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x3C, 0xFE, wBuffer);
            return ret;
        }
        private UInt32 Reset()
        {
            byte[] bdata = new byte[] { 0xF0, 0x5A, 0x9C };
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 3;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x3C, 0xF0, wBuffer);
            return ret;
        }
        private UInt32 HWReset()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = APBWriteWord(0x97, 0x7810);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadWord(0x97, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            wval |= 0x0010;
            ret = APBWriteWord(0x97, wval);
            return ret;
        }
        private UInt32 FWVersion(ref UInt32 version)
        {
            byte[] bdata = new byte[] { 0x01, 0x00 };
            TSMBbuffer wBuffer = new TSMBbuffer();
            TSMBbuffer rBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 2;
            wBuffer.bdata = bdata;
            rBuffer.length = 4;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockRead(0x16, 0xF9, ref rBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            version = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(rBuffer.bdata[0], rBuffer.bdata[1]),
                SharedFormula.MAKEWORD(rBuffer.bdata[2], rBuffer.bdata[3]));
            return ret;

        }
        private UInt32 FGVersion(ref UInt32 version)
        {
            byte[] bdata = new byte[] { 0x02, 0x00 };
            TSMBbuffer wBuffer = new TSMBbuffer();
            TSMBbuffer rBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 2;
            wBuffer.bdata = bdata;
            rBuffer.length = 4;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockRead(0x16, 0xF9, ref rBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            version = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(rBuffer.bdata[0], rBuffer.bdata[1]),
                SharedFormula.MAKEWORD(rBuffer.bdata[2], rBuffer.bdata[3]));
            return ret;

        }
        private UInt32 FWUpdateMode()
        {
            byte[] bdata = new byte[] { 0x04, 0x00, 0x4F, 0x50, 0x53, 0x49 };
            TSMBbuffer wBuffer = new TSMBbuffer();
            TSMBbuffer rBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            wBuffer.length = 6;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockRead(0x16, 0xF9, ref rBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;

        }
        private UInt32 EnterBootMode(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            byte[] bdata = new byte[] { 0x0F, 0xA5, 0xCD };
            TSMBbuffer wBuffer = new TSMBbuffer();
            TSMBbuffer rBuffer = new TSMBbuffer();
            wBuffer.length = 3;
            wBuffer.bdata = bdata;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            DateTime beginTime = DateTime.Now;

            do
            {
                ret = BlockWrite(0x3C, 0xF8, wBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                ret = BlockRead(0x3C, 0xF8, ref rBuffer);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    wval = SharedFormula.MAKEWORD(rBuffer.bdata[0], rBuffer.bdata[1]);
                    if (wval != 0x5AF0)
                    {
                        ret = ElementDefine.IDS_ERR_DEM_HANDSHAKE;
                        continue;
                    }
                    return ret;
                }
            } while (DateTime.Now.Subtract(beginTime).TotalMilliseconds < 20000);
            return ret;
        }
        private UInt32 ClearLog()
        {
            DateTime dt = DateTime.Now;
            TSMBbuffer wBuffer = new TSMBbuffer();
            UInt32 RTC = (UInt32)((dt.Year - 2000) << 26 | dt.Month << 22 | dt.Day << 17 | dt.Hour << 12 | dt.Minute << 6 | dt.Second);
            byte[] bdata = new byte[] { 0x06, 0x00, 0x6D, 0x69, 0x69, 0x6F, 0x00, 0x00, 0x00, 0x00 };
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            bdata[9] = (byte)(RTC >> 24);
            bdata[8] = (byte)(RTC >> 16);
            bdata[7] = (byte)(RTC >> 8);
            bdata[6] = (byte)RTC;
            wBuffer.length = (UInt16)bdata.Length;
            wBuffer.bdata = bdata;
            ret = BlockWrite(0x16, 0xF9, wBuffer);
            return ret;
        }

        private UInt32 Authentication(byte[] message, ref byte[] hMAC)
        {
            TSMBbuffer tsmBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            tsmBuffer.length = (UInt16)(message.Length + 1);
            tsmBuffer.bdata[0] = 20;
            Array.Copy(message, 0, tsmBuffer.bdata, 1, message.Length);
            ret = BlockWrite(0x16, 0x2F, tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Thread.Sleep(100);
            Array.Clear(tsmBuffer.bdata, 0, tsmBuffer.bdata.Length);
            ret = BlockRead(0x16, 0x2F, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            hMAC = tsmBuffer.bdata;
            return ret;
        }
        #endregion
    }
}