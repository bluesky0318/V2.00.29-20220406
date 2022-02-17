using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.Azalea10_Azalea15_Stack
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

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
        }

        #region YFLASH操作常量定义
        private const int RETRY_COUNTER = 5;

        // YFLASH operation code
        private const byte YFLASH_WORKMODE_NORMAL                   = 0x00;
        private const byte YFLASH_WORKMODE_WRITE_ATE_SECOND_LOCK    = 0x01;
        private const byte YFLASH_WORKMODE_READOUT                  = 0x02;
        private const byte YFLASH_WORKMODE_BGVTCTRM                 = 0x03;
        private const byte YFLASH_WORKMODE_OSCTRM                   = 0x04;
        private const byte YFLASH_WORKMODE_DOCTRM                   = 0x05;
        private const byte YFLASH_WORKMODE_THMTRM                   = 0x06;
        private const byte YFLASH_WORKMODE_ADCTEST                  = 0x07;
        private const byte YFLASH_WORKMODE_INTCHOFFSETTRM           = 0x08;
        private const byte YFLASH_WORKMODE_CHSLOPETTRM              = 0x09;
        private const byte YFLASH_WORKMODE_DATA_PREPARATION         = 0x0A;
        private const byte YFLASH_WORKMODE_ERASE                    = 0x0B;
        private const byte YFLASH_WORKMODE_PROGRAMMING              = 0x0C;
        private const byte YFLASH_WORKMODE_VPP_FUSEBLOW             = 0x0D;
        private const byte YFLASH_WORKMODE_CELL_BALANCE_TEST        = 0x0E;
        private const byte YFLASH_WORKMODE_INTKEY_SIGNALS           = 0x0F;

        // YFLASH control registers' addresses
        private const byte YFLASH_STR_REG                           = 0x02;
        private const byte YFLASH_TEST_CTR_REG                      = 0x04;
        private const byte YFLASH_WORKMODE_REG                      = 0x05;
        private const byte YFLASH_ATELCK_REG                        = 0x06;

        // YFLASH Control Flags
        private const UInt16 YFLASH_ATELOCK_MATCHED_FLAG            = 0x0001;
        private const UInt16 YFLASH_MAP_FLAG                        = 0x0010;
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
            byte   bCrc = 0;
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
                    bCrc  = receivebuf[2];
                    wdata = SharedFormula.MAKEWORD(receivebuf[1],receivebuf[0]);
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
            m_Interface.GetLastErrorCode(ref ret);
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
                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_READOUT);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnYFLASHReadWord(reg, ref pval);
            }
            return ret;
        }
        #endregion

        #region YFLASH寄存器子级操作
        protected UInt32 OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM wkm)
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
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL:
                    {
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, (byte)ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_WRITE_ATE_SECOND_LOCK:
                    {
                        blow |= YFLASH_WORKMODE_WRITE_ATE_SECOND_LOCK;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_READOUT:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_READOUT;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_BGVTCTRM:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_BGVTCTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_OSCTRM:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_OSCTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_DOCTRM:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_DOCTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_THMTRM:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_THMTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_ADCTEST:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        
                        blow |= YFLASH_WORKMODE_ADCTEST;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_INTCHOFFSETTRM:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        
                        blow |= YFLASH_WORKMODE_INTCHOFFSETTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_CHSLOPETTRM:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        
                        blow |= YFLASH_WORKMODE_CHSLOPETTRM;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_DATA_PREPARATION:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        
                        blow |= YFLASH_WORKMODE_DATA_PREPARATION;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        Thread.Sleep(10);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_ERASE:
                    {
                        blow |= YFLASH_WORKMODE_ERASE;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        Thread.Sleep(120);
                        ret = OnWaitWorkModeCompleted();
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_PROGRAMMING:
                    {
                        blow |= YFLASH_WORKMODE_PROGRAMMING;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        Thread.Sleep(60);
                        ret = OnWaitWorkModeCompleted();
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_VPP_FUSEBLOW:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED_10);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        
                        blow |= YFLASH_WORKMODE_VPP_FUSEBLOW;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_CELL_BALANCE_TEST:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_CELL_BALANCE_TEST;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_INTKEY_SIGNALS:
                    {
                        ret = OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWaitATELockMatched(true);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blow |= YFLASH_WORKMODE_INTKEY_SIGNALS;
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_MAP:
                    {
                        blow = SharedFormula.LoByte(wdata);
                        blow |= SharedFormula.LoByte(YFLASH_MAP_FLAG);
                        wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        ret = OnWaitMapCompleted();
                        break;
                    }
            }
            return ret;
        }

        protected UInt32 OnATELCK(ElementDefine.COBRA_AZALEA5_ATELCK lck)
        {
            byte blocks = 0;
            byte blockp = 0;
            UInt16 wlock = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_WRITE_ATE_SECOND_LOCK);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            switch (lck)
            {
                case ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_UNMATCHED:
                    {
                        ret = OnReadWord(YFLASH_ATELCK_REG, ref wlock);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blockp = SharedFormula.LoByte((UInt16)(wlock & 0x0030));
                        blocks = (byte)((blockp >> 4) + 1);
                        wlock = (UInt16)SharedFormula.MAKEWORD((byte)(blockp | blocks), SharedFormula.HiByte(wlock));
                        ret = OnWriteWord(YFLASH_ATELCK_REG, wlock);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWaitATELockMatched(false);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED:
                    {
                        ret = OnReadWord(YFLASH_ATELCK_REG, ref wlock);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blockp = SharedFormula.LoByte((UInt16)(wlock & 0x0030));
                        blocks = (byte)(blockp >> 4);
                        wlock = (UInt16)SharedFormula.MAKEWORD((byte)(blockp | blocks), SharedFormula.HiByte(wlock));

                        ret = OnWriteWord(YFLASH_ATELCK_REG, wlock);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        OnWaitATELockMatched(true);

                        break;
                    }
                case ElementDefine.COBRA_AZALEA5_ATELCK.YFLASH_ATELCK_MATCHED_10:
                    {
                        ret = OnReadWord(YFLASH_ATELCK_REG, ref wlock);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        blockp = 0x20;
                        blocks = (byte)(blockp >> 4);
                        wlock = (UInt16)SharedFormula.MAKEWORD((byte)(blockp | blocks), SharedFormula.HiByte(wlock));

                        ret = OnWriteWord(YFLASH_ATELCK_REG, wlock);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        OnWaitATELockMatched(true);

                        break;
                    }
            }
            return ret;
        }

        protected UInt32 OnYFLASHReadWord(byte reg, ref UInt16 pval)
        {
            return OnReadWord(reg,ref pval);
        }

        protected UInt32 OnYFLASHWriteWord(byte reg, UInt16 val)
        {
            return OnWriteWord(reg, val);
	    } 

        protected UInt32 OnWaitATELockMatched(bool bcheck)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                ret = OnReadWord(YFLASH_STR_REG, ref wdata);

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;

                if (bcheck)
                {
                    if ((wdata & YFLASH_ATELOCK_MATCHED_FLAG) == YFLASH_ATELOCK_MATCHED_FLAG)
                        return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else
                {
                    if ((wdata & YFLASH_ATELOCK_MATCHED_FLAG) == 0x0000)
                        return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }

                Thread.Sleep(10);
            }

            // exceed max waiting time
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
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
        protected UInt32 WorkMode(ElementDefine.COBRA_AZALEA5_WKM wkm)
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
                msg.gm.message = "Please change to erase voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_ERASE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL);
            }
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_MAP);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL);
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
                foreach (byte badd in YFLASHReglist)
                {
                    ret = YFLASHReadWord(badd, ref wdata);
                    parent.m_YFRegImg[badd].err = ret;
                    parent.m_YFRegImg[badd].val = wdata;
                }

                ret = WorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
                //Thread.Sleep(10);
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
                msg.gm.message = "Please change to erase voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_ERASE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_DATA_PREPARATION);
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

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_PROGRAMMING);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_MAP);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL);
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

            ret = WorkMode((ElementDefine.COBRA_AZALEA5_WKM)OpReglist[0].phydata);
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            //int ival = 0;
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            //wval = 0x0510;
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            /*deviceinfor.type = (int)SharedFormula.HiByte(wval); 
            ival = (int)((SharedFormula.LoByte(wval) & 0x30) >> 4);
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
            ival = (int)(SharedFormula.LoByte(wval) & 0x01);
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)(SharedFormula.LoByte(wval) & 0x01);

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (SharedFormula.HiByte(type) != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if (((SharedFormula.LoByte(type) & 0x30) >> 4) != deviceinfor.hwversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if ((SharedFormula.LoByte(type) & 0x01) != deviceinfor.hwsubversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }*/
            ret = ReadWord(0x01, ref wval);
            //wval = 0x0510;
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.fwversion = wval.ToString();// = 2;
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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