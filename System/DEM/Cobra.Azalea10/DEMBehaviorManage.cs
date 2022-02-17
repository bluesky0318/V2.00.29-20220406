using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.Azalea10
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

        private ushort[] m_curbuffer = new ushort[6];

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
        }

        #region YFLASH操作常量定义
        private const int RETRY_COUNTER = 5;

        // YFLASH operation code
        private const byte YFLASH_TESTCTR_NORMAL = 0x00;
        private const byte YFLASH_TESTCTR_READOUT = 0x03;
        private const byte YFLASH_TESTCTR_WRITE_ATE_SECOND_LOCK = 0x05;
        private const byte YFLASH_TESTCTR_WORKMODE = 0x02;

        private const byte YFLASH_WORKMODE_NORMAL = 0x00;
        private const byte YFLASH_WORKMODE_DATA_READ_OUT = 0x02;
        private const byte YFLASH_WORKMODE_BANDGAP = 0x03;
        private const byte YFLASH_WORKMODE_OSCBGTRM = 0x04;
        private const byte YFLASH_WORKMODE_DOCTRM = 0x05;
        private const byte YFLASH_WORKMODE_THMTRM = 0x06;
        private const byte YFLASH_WORKMODE_ADC_TEST = 0x07;
        private const byte YFLASH_WORKMODE_INT_TEMP_OFFSETTRM = 0x08;
        private const byte YFLASH_WORKMODE_CHANNEL_SLOPTRM = 0x09;
        private const byte YFLASH_WORKMODE_DATA_PREPARATION = 0x0a;
        private const byte YFLASH_WORKMODE_ERASE = 0x0b;
        private const byte YFLASH_WORKMODE_PROGRAMMING = 0x0c;
        private const byte YFLASH_WORKMODE_VPP_FUSEBLOW = 0x0d;
        private const byte YFLASH_WORKMODE_CELL_BALANCE_TEST = 0x0e;
        private const byte YFLASH_WORKMODE_OBSERVEINTERNAL_KEYSIGNAL = 0x0f;

        // YFLASH control registers' addresses
        private const byte YFLASH_STR_REG = 0x02;
        private const byte YFLASH_TEST_CTR_REG = 0x04;
        private const byte YFLASH_WORKMODE_REG = 0x05;
        private const byte YFLASH_ATELCK_REG = 0x06;

        // YFLASH Control Flags
        private const UInt16 YFLASH_ATELOCK_MATCHED_FLAG = 0x0400;
        private const UInt16 YFLASH_MAP_FLAG = 0x0010;
        #endregion

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
        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
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

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, UInt16 data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = SharedFormula.HiByte(data);
            pdata[4] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 5);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, UInt16 data)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = SharedFormula.HiByte(data);
            pdata[3] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 4);
        }

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
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
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    bCrc = receivebuf[2];
                    wdata = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    if (bCrc != calc_crc_read(sendbuf[0], sendbuf[1], wdata))
                    {
                        pval = ElementDefine.PARAM_HEX_ERROR;
                        ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    else
                    {
                        pval = wdata;
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    }
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
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
            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        #endregion
        #endregion

        #region YFLASH寄存器操作
        #region YFLASH寄存器父级操作
        internal UInt32 YFLASHReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_DATA_READ_OUT);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnYFLASHReadWord(reg, ref pval);
            }
            return ret;
        }
        #endregion

        #region YFLASH寄存器子级操作
        protected UInt32 OnTestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR ctr)
        {
            byte blow = 0;
            byte bhigh = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadWord(YFLASH_TEST_CTR_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            blow = SharedFormula.LoByte(wdata);
            bhigh = (byte)(SharedFormula.HiByte(wdata) & 0xFC);
            switch (ctr)
            {
                case ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_NORMAL:
                    {
                        bhigh |= YFLASH_TESTCTR_NORMAL;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_TEST_CTR_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_WORKMODE:
                    {
                        bhigh |= YFLASH_TESTCTR_WORKMODE;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_TEST_CTR_REG, wdata);
                        break;
                    }
            }
            return ret;
        }

        protected UInt32 OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM wkm)
        {
            byte blow = 0;
            byte bhigh = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadWord(YFLASH_WORKMODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            blow = (byte)(SharedFormula.LoByte(wdata) & 0xF0);
            bhigh = SharedFormula.HiByte(wdata);
            switch (wkm)
            {
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_NORMAL:
                    {
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, (byte)ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_NORMAL);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_DATA_READ_OUT:
                    {
                        blow |= YFLASH_WORKMODE_DATA_READ_OUT;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_BANDGAP:
                    {
                        blow |= YFLASH_WORKMODE_BANDGAP;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_OSCBGTRM:
                    {
                        blow |= YFLASH_WORKMODE_OSCBGTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_DOCTRM:
                    {
                        blow |= YFLASH_WORKMODE_DOCTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_THMTRM:
                    {
                        blow |= YFLASH_WORKMODE_THMTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_ADC_TEST:
                    {
                        blow |= YFLASH_WORKMODE_ADC_TEST;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_INT_TEMP_OFFSETTRM:
                    {
                        blow |= YFLASH_WORKMODE_INT_TEMP_OFFSETTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_CHANNEL_SLOPTRM:
                    {
                        blow |= YFLASH_WORKMODE_CHANNEL_SLOPTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_DATA_PREPARATION:
                    {
                        blow |= YFLASH_WORKMODE_DATA_PREPARATION;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        Thread.Sleep(10);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_ERASE:
                    {
                        blow |= YFLASH_WORKMODE_ERASE;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        Thread.Sleep(120);
                        ret = OnWaitWorkModeCompleted();
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_PROGRAMMING:
                    {
                        blow |= YFLASH_WORKMODE_PROGRAMMING;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        Thread.Sleep(60);
                        ret = OnWaitWorkModeCompleted();
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_VPP_FUSEBLOW:
                    {
                        blow |= YFLASH_WORKMODE_VPP_FUSEBLOW;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_CELL_BALANCE_TEST:
                    {
                        blow |= YFLASH_WORKMODE_CELL_BALANCE_TEST;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_MAP:
                    {
                        blow = SharedFormula.LoByte(wdata);
                        blow |= SharedFormula.LoByte(YFLASH_MAP_FLAG);
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        ret = OnWaitMapCompleted();
                        break;
                    }
                case ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_OBSERVEINTERNAL_KEYSIGNAL:
                    {
                        blow |= YFLASH_WORKMODE_OBSERVEINTERNAL_KEYSIGNAL;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
            }
            return ret;
        }

        protected UInt32 OnYFLASHReadWord(byte reg, ref UInt16 pval)
        {
            return OnReadWord(reg, ref pval);
        }

        protected UInt32 OnYFLASHWriteWord(byte reg, UInt16 val)
        {
            return OnWriteWord(reg, val);
        }

        protected UInt32 OnWaitWorkModeCompleted()
        {
            byte bdata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                ret = OnReadWord(YFLASH_WORKMODE_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bdata = SharedFormula.LoByte(wdata);
                if ((bdata & 0x0F) == YFLASH_WORKMODE_NORMAL)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }

            // exceed max waiting time
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }

        protected UInt32 OnWaitMapCompleted()
        {
            byte bdata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                ret = OnReadWord(YFLASH_WORKMODE_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bdata = SharedFormula.LoByte(wdata);
                if ((bdata & 0x10) == YFLASH_WORKMODE_NORMAL)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }

            // exceed max waiting time
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }
        #endregion
        #endregion

        #region YFLASH功能操作
        #region YFLASH功能父级操作
        protected UInt32 TestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR ctr)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnTestCTRMode(ctr);
            }
            return ret;
        }

        protected UInt32 WorkMode(ElementDefine.COBRA_AZALEA10_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }

        protected UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
                ret = OnTestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_WORKMODE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to erase voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_ERASE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnTestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_NORMAL);
            }
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
                ret = OnTestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_WORKMODE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_MAP);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnTestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_NORMAL);
            }

            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
                p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = BlockErase(ref msg);
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            return BlockRead();
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                YFLASHReglist.Add(baddress);
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (YFLASHReglist.Count != 0)
            {
                ret = TestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_WORKMODE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = WorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = WorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_MAP);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in YFLASHReglist)
                {
                    ret = YFLASHReadWord(badd, ref wdata);
                    parent.m_YFRegImg[badd].err = ret;
                    parent.m_YFRegImg[badd].val = wdata;
                }

                ret = TestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
 
                if (msg.gm.sflname.Equals("Scan") && badd == 0x2D)
                {
                    int index = 1;
                    m_curbuffer[0] = wdata;
                    for (index = 1; index < 6; index++)
                    {
                        ret = ReadWord(badd, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        m_curbuffer[index] = wdata;
                    }
                    if (index == 6) wdata = maxmin(m_curbuffer);
                }

                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }
            return ret;
        }

        private ushort maxmin(ushort[] buffer)
        {
            string str = String.Empty+"\n";
            UInt32 sum;
            ushort i,max, min;
            ushort wdata;
            sum = max = 0;
            min = buffer[0];
            for (i = 0; i < 6; i++)
            {
                if (max < buffer[i]) max = buffer[i];
                if (min > buffer[i]) min = buffer[i];
                sum = sum + buffer[i];
                str += String.Format("{0}:0x{1:X};",i,buffer[i]);
            }
            str += String.Format("，Sum as 0x{0:X}",sum);
            wdata = (ushort)((sum - max - min) / 4);
            str += String.Format("，Avarge as 0x{0:X}", wdata);
            FolderMap.WriteFile(str);
            return wdata;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                YFLASHReglist.Add(baddress);
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            if (YFLASHReglist.Count != 0)
            {
                ret = TestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_WORKMODE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to erase voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_ERASE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_DATA_PREPARATION);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (byte i = 0; i < ElementDefine.YFLASH_MEMORY_SIZE; i++)
                {
                    ret1 = parent.m_YFRegImg[i].err;
                    ret |= ret1;
                    if (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;

                    ret1 = OnYFLASHWriteWord(i, parent.m_YFRegImg[i].val);
                    parent.m_YFRegImg[i].err = ret1;
                    ret |= ret1;
                }

                msg.gm.message = "Please change to programming voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_PROGRAMMING);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_MAP);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA10_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnTestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
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
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> YFLASHParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            YFLASHParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
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

            if (YFLASHParamList.Count != 0)
            {
                for (int i = 0; i < YFLASHParamList.Count; i++)
                {
                    param = (Parameter)YFLASHParamList[i];
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

            List<Parameter> YFLASHParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            YFLASHParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
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

            if (YFLASHParamList.Count != 0)
            {
                for (int i = 0; i < YFLASHParamList.Count; i++)
                {
                    param = (Parameter)YFLASHParamList[i];
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
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
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
                                if (baddress == 0x05)
                                    OpReglist.Add(p);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            if (OpReglist.Count != 1) return LibErrorCode.IDS_ERR_DEM_PARAM_READ_UNABLE;

            ret = TestCTRMode(ElementDefine.COBRA_AZALEA10_TESTCTR.YFLASH_TESTCTR_WORKMODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WorkMode((ElementDefine.COBRA_AZALEA10_WKM)OpReglist[0].phydata);
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            int ival = 0;
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)SharedFormula.HiByte(wval);
            ival = (int)((SharedFormula.LoByte(wval) & 0x70) >> 4);
            deviceinfor.hwversion = ival;
            switch (ival)
            {
                case 0:
                    shwversion = "A";
                    break;
                case 1:
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt16 wval = 0;

            ret = ReadWord(0x03, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((wval & 0x02) == 0x00)
            {
                msg.sm.gpios[0] = true;
                msg.sm.gpios[1] = true;
            }
            else
            {
                msg.sm.gpios[0] = false;
                msg.sm.gpios[1] = false;
            }

            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
        #endregion
    }
}