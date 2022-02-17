//#define SIMULATION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.SequoiaSBS
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

        private BatteryMode batteryMode;
        private Random rd = new Random();
        private object m_lock = new object();
        private byte[] m_tempArray = new byte[ElementDefine.BLOCK_OPERATION_BYTES];
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
                if (reg >= 0xE0)
                {
                    ret = OnAllowWriteI2CMapReg();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }

        internal UInt32 APBReadWord(UInt16 startAddr, ref UInt16 wval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnAPBReadWord(startAddr, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnLockI2C();
            }
            return ret;
        }

        internal UInt32 APBWriteWord(UInt16 startAddr, UInt16 wval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnAPBWriteWord(startAddr, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnLockI2C();
            }
            return ret;
        }

        internal UInt32 AHBReadWord(UInt16 startAddr, ref UInt16 hwval, ref UInt16 lwval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnAHBReadWord(startAddr, ref hwval, ref lwval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnLockI2C();
            }
            return ret;
        }

        internal UInt32 AHBWriteWord(UInt16 startAddr, UInt16 hwval = 0, UInt16 lwval = 0)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnAHBWriteWord(startAddr, hwval, lwval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnLockI2C();
            }
            return ret;
        }

        protected UInt32 BlockRead(byte cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
#if SIMULATION
                rd.NextBytes(pval.bdata);
#else
                ret = OnBlockRead(cmd, ref pval);
#endif
            }
            return ret;
        }

        protected UInt32 BlockRead(UInt16 cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(cmd, ref pval);
            }
            return ret;
        }

        protected UInt32 BlockWrite(byte cmd, TSMBbuffer val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, val);
            }
            return ret;
        }

        protected UInt32 BlockWrite(UInt16 cmd, TSMBbuffer val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, val);
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

        protected byte calc_crc_block_read(byte slave_addr, byte reg_addr, byte[] data)
        {
            int len = data.Length - 1;
            byte[] pdata = new byte[data.Length + 3];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            Array.Copy(data, 0, pdata, 3, len);

            return crc8_calc(ref pdata, (UInt16)(pdata.Length - 1));
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

        private UInt32 OnUnLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnWriteWord(0x11, ElementDefine.UnLock_I2C_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(0x12, ElementDefine.I2C_AHB_MODE_Enable_PSW);
            return ret;
        }

        private UInt32 OnLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnWriteWord(0x20, 0x00);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(0x12, ElementDefine.I2C_AHB_MODE_Default_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(0x11, ElementDefine.ReLock_I2C_PSW);
            return ret;
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
            m_Interface.GetLastErrorCode(ref ret);
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
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnAPBReadWord(UInt16 startAddr, ref UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 4; //Len,2bytes,PEC
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC

            ret = OnWriteWord(0x02, 0x0004);//ElementDefine.BLOCK_OPERATION_BYTES);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(0x01, (UInt16)(startAddr + ElementDefine.DFEController_StartAddress));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0xFB;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 2), 1))
                {
                    if (receivebuf[DataInLen + 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                    {
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    wval = SharedFormula.MAKEWORD(receivebuf[4], receivebuf[3]);
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnAPBWriteWord(UInt16 startAddr, UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 8;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            byte[] sendbuf = new byte[DataInLen];//Length and PEC
            byte[] receivebuf = new byte[2];

            /*Not need to set the write size. the register is read only
            ret = OnWriteWord(0x02, (UInt16)array.Length);//ElementDefine.BLOCK_OPERATION_BYTES);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

            ret = OnWriteWord(0x01, (UInt16)(startAddr + ElementDefine.DFEController_StartAddress));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0xFB;
            sendbuf[2] = 4;
            sendbuf[3] = 00;
            sendbuf[4] = 00;
            sendbuf[5] = SharedFormula.HiByte(wval);
            sendbuf[6] = SharedFormula.LoByte(wval);
            sendbuf[7] = crc8_calc(ref sendbuf, 7);

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen - 2)))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnAHBReadWord(UInt16 startAddr, ref UInt16 hwval, ref UInt16 lwval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 4; //Len,2bytes,PEC
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC

            ret = OnWriteWord(0x02, 0x0004);//ElementDefine.BLOCK_OPERATION_BYTES);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(0x01, startAddr);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0xFB;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 2), 1))
                {
                    if (receivebuf[DataInLen + 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                    {
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    lwval = SharedFormula.MAKEWORD(receivebuf[4], receivebuf[3]);
                    hwval = SharedFormula.MAKEWORD(receivebuf[2], receivebuf[1]);
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnAHBWriteWord(UInt16 startAddr, UInt16 hwval, UInt16 lwval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 8;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            byte[] sendbuf = new byte[DataInLen];//Length and PEC
            byte[] receivebuf = new byte[2];

            /*Not need to set the write size. the register is read only
            ret = OnWriteWord(0x02, (UInt16)array.Length);//ElementDefine.BLOCK_OPERATION_BYTES);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

            ret = OnWriteWord(0x01, startAddr);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0xFB;
            sendbuf[2] = 4;
            sendbuf[3] = SharedFormula.HiByte(hwval);
            sendbuf[4] = SharedFormula.LoByte(hwval);
            sendbuf[5] = SharedFormula.HiByte(lwval);
            sendbuf[6] = SharedFormula.LoByte(lwval);

            sendbuf[7] = crc8_calc(ref sendbuf, 7);

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen - 2)))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnBlockRead(byte cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = 0x16;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
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

        protected UInt32 OnBlockWrite(byte cmd, TSMBbuffer val)
        {
            bool bPEC = true;
            bool bsuc = false;
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)val.length;
            byte[] sendbuf = null;//new byte[DataInLen + 3]; //I2C, CMD,PEC
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            bPEC = parent.m_busoption.GetOptionsByGuid(BusOptions.I2CPECMODE_GUID).SelectLocation.Code > 0 ? true : false;
            if (bPEC)
                sendbuf = new byte[DataInLen + 3]; //I2C, CMD,PEC
            else
                sendbuf = new byte[DataInLen + 2]; //I2C, CMD

            try
            {
                sendbuf[0] = 0x16;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;

            for (int i = 0; i < val.length; i++)
                sendbuf[2 + i] = val.bdata[i];

            if (bPEC)
                sendbuf[val.length + 2] = crc8_calc(ref sendbuf, (UInt16)(DataInLen + 2));

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (bPEC)
                    bsuc = m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 1)); //valid data and pec
                else
                    bsuc = m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen);

                if (bsuc)
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

        protected UInt32 OnBlockRead(UInt16 cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = 0x16;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0xF9;
            sendbuf[2] = 0xBB;
            sendbuf[3] = SharedFormula.HiByte(cmd);
            sendbuf[4] = SharedFormula.LoByte(cmd);

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)3)) //valid data and pec
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

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

        protected UInt32 OnBlockWrite(UInt16 cmd, TSMBbuffer val)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)val.length;
            byte[] sendbuf = new byte[DataInLen + 5];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = 0x16;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0xF9;
            sendbuf[2] = 0xAA;
            sendbuf[3] = SharedFormula.HiByte(cmd);
            sendbuf[4] = SharedFormula.LoByte(cmd);

            for (int i = 0; i < val.length; i++)
                sendbuf[5 + i] = val.bdata[i];

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 3))) //valid data and pec
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnAllowWriteI2CMapReg()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadWord(ElementDefine.RegI2C_MEMD, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((wdata & 0x80) == 0x80) return LibErrorCode.IDS_ERR_SUCCESSFUL;
            wdata |= 0x8000;
            ret = OnWriteWord(ElementDefine.RegI2C_MEMD, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            wdata |= 0x8001;
            ret = OnWriteWord(ElementDefine.RegI2C_MEMD, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = eFlashMainBlockErase();
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            byte bcmd = 0;
            UInt16 ucmd = 0;
            TSMBbuffer tsmBuffer = null;
            List<byte> SBSReglist = new List<byte>();
            List<UInt16> ConfigReglist = new List<UInt16>();
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
                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            SBSReglist.Add(bcmd);

                            break;
                        }
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;

                            ucmd = (UInt16)((p.guid & ElementDefine.Command2Mask) >> 4);
                            ConfigReglist.Add(ucmd);
                            break;
                        }
                }
            }

            SBSReglist = SBSReglist.Distinct().ToList();
            ConfigReglist = ConfigReglist.Distinct().ToList();

            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[badd];
#if SIMULATION
                rd.NextBytes(tsmBuffer.bdata);
#else
                ret = BlockRead(badd, ref tsmBuffer);
#endif
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }

            foreach (UInt16 uadd in ConfigReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(uadd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[uadd];
                ret = BlockRead(uadd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            byte bcmd = 0;
            UInt16 ucmd = 0;
            TSMBbuffer tsmBuffer = null;
            List<byte> SBSReglist = new List<byte>();
            List<UInt16> ConfigReglist = new List<UInt16>();
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
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                                SBSReglist.Add(bcmd);
                            }
                            break;
                        }
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;

                            ucmd = (UInt16)((p.guid & ElementDefine.Command2Mask) >> 4);
                            ConfigReglist.Add(ucmd);
                            break;
                        }
                }
            }

            SBSReglist = SBSReglist.Distinct().ToList();
            ConfigReglist = ConfigReglist.Distinct().ToList();
            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[badd];
#if SIMULATION
                rd.NextBytes(tsmBuffer.bdata);
#else
                ret = BlockWrite(badd, tsmBuffer);
#endif
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }

            foreach (UInt16 uadd in ConfigReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(uadd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[uadd];
                ret = BlockWrite(uadd, tsmBuffer);
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
            List<Parameter> SBSParamList = new List<Parameter>();
            List<Parameter> ConfigReglist = new List<Parameter>();
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
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;
                            ConfigReglist.Add(p);
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
            if (ConfigReglist.Count != 0)
            {
                for (int i = 0; i < ConfigReglist.Count; i++)
                {
                    param = (Parameter)ConfigReglist[i];
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
            List<Parameter> ConfigReglist = new List<Parameter>();
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
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;
                            ConfigReglist.Add(p);
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
            if (ConfigReglist.Count != 0)
            {
                for (int i = 0; i < ConfigReglist.Count; i++)
                {
                    param = (Parameter)ConfigReglist[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt16 mPages = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region Read flash and compare with image
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        mPages = (UInt16)(msg.flashData.Length / ElementDefine.ONE_PAGE_SIZE);
                        for (UInt16 pages = 0; pages < mPages; pages++)
                        {
                            ret = eFlashMainPageErase(pages);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }

                        ret = eFlashEnableWrite();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = eFlashMainBlockWrite(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = eFlashRemap();
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
                        ret = eflashMainBlockRead(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_COMPARE:
                    {
                        int len = 0;
                        len = msg.flashData.Length;
                        Array.Clear(ElementDefine.interBuffer, 0, ElementDefine.interBuffer.Length);
                        Array.Copy(msg.flashData, ElementDefine.interBuffer, len);

                        msg.controlmsg.message = "Begin upload data..";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        ret = eflashMainBlockRead(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        msg.controlmsg.message = "Begin verify data..";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        for (int n = 0; n < len; n++)
                        {
                            if (msg.flashData[n] == ElementDefine.interBuffer[n]) continue;
                            return LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                        }
                        break;
                    }
                #endregion
                default:
                    break;
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

            ret = APBReadWord(0x1F, ref wval);
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
            deviceinfor.shwversion = string.Format("{0:d}\n{1:d}\n{2:d}\n{3:d}",0,0,0,ival);
            deviceinfor.ateversion = "0.0";
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
            byte bcmd = 0;
            byte cellnumber = 0;
            byte totalCellNumber = 0;
            Parameter param = null;
            bool bTHM0Enabled = false, bTHM1Enabled = false, bTHM2Enabled = false, bTHM3Enabled = false;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if (p == null) continue;
                p.bShow = true;
                p.tsmbBuffer.length = 4;
            }

            param = GetParameterByGuid(ElementDefine.MfgName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            param = GetParameterByGuid(ElementDefine.DevName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            param = GetParameterByGuid(ElementDefine.DevChem, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            param = GetParameterByGuid(ElementDefine.MfgData, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;

            #region Read Static Parameter
            param = GetParameterByGuid(ElementDefine.MfgAccess, demparameterlist.parameterlist);
            bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
            TSMBbuffer tsmBuffer = param.tsmbBuffer;
            param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.Hex2Physical(ref param);

            #region BatteryMode
            param = GetParameterByGuid(ElementDefine.BatteryMode, demparameterlist.parameterlist);
            bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
            tsmBuffer = param.tsmbBuffer;
            param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.Hex2Physical(ref param);
#if SIMULATION
            tsmBuffer.bdata[0] = 0xBA;
            cellnumber = (byte)(tsmBuffer.bdata[0] & 0x0F);
            bTHM0Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 4)&0x01) > 0 ? true : false;
            bTHM1Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 5)&0x01) > 0 ? true : false;
            bTHM2Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 6)&0x01) > 0 ? true : false;
            bTHM3Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 7)&0x01) > 0 ? true : false;
#else
            cellnumber = (byte)(tsmBuffer.bdata[0] & 0x0F);
            bTHM0Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 4) & 0x01) > 0 ? true : false;
            bTHM1Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 5) & 0x01) > 0 ? true : false;
            bTHM2Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 6) & 0x01) > 0 ? true : false;
            bTHM3Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 7) & 0x01) > 0 ? true : false;
#endif
            param = GetParameterByGuid(ElementDefine.CellVoltMV14, demparameterlist.parameterlist);
            if (param == null) totalCellNumber = 14;
            else totalCellNumber = 14;

            for (int n = 0; n < totalCellNumber; n++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.CellVoltMV01 + (n << 8)), demparameterlist.parameterlist);
                if (param == null) continue;
                if ((n < (cellnumber - 1)) || (n == (totalCellNumber - 1))) param.bShow = true;
                else param.bShow = false;
            }
            if (!bTHM0Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK1, demparameterlist.parameterlist);
                param.bShow = false;
            }
            if (!bTHM1Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK2, demparameterlist.parameterlist);
                param.bShow = false;
            }
            if (!bTHM2Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK3, demparameterlist.parameterlist);
                param.bShow = false;
            }
            if (!bTHM3Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK4, demparameterlist.parameterlist);
                param.bShow = false;
            }
            #endregion

            //0x18~0x1c
            for (int i = 0; i < 5; i++)
            {
                if (i == 2)
                    param = GetParameterByGuid(ElementDefine.SpecInfo, demparameterlist.parameterlist);
                else
                    param = GetParameterByGuid((UInt32)(ElementDefine.DesignCap + (i << 8)), demparameterlist.parameterlist);
                bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
                tsmBuffer = param.tsmbBuffer;
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }
            //0x20~0x23
            for (int i = 0; i < 4; i++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.MfgName + (i << 8)), demparameterlist.parameterlist);
                bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
                tsmBuffer = param.tsmbBuffer;
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }
            #endregion
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion

        #region 其他
        public void HideParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid))
                {
                    param.bShow = false;
                    break;
                }
            }
            return;
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

        public UInt32 ReadRsenseMain()
        {
            UInt16 uadd = 0x101;
            if (!parent.m_HwMode_Dic.ContainsKey(uadd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            TSMBbuffer tsmBuffer = parent.m_HwMode_Dic[uadd];
            return BlockRead(uadd, ref tsmBuffer);
        }

        #region Main Eflash操作
        public UInt32 eFlashRemap()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = AHBWriteWord(0xCA02, 0, 0x0003);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = AHBWriteWord(0xC9AA, 0, 0x7918);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = AHBWriteWord(0xC9AB, 0, 0x0010);
            return ret;
        }

        public UInt32 eFlashEnableWrite()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = AHBWriteWord(0x7000, 0, 0x8000);
            return ret;
        }

        public UInt32 eFlashMainBlockErase()
        {
            UInt16 hwval = 0, lwval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = AHBWriteWord(0x7001, 0, 0x00);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = AHBWriteWord(0x7000, 0, 0x01);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = AHBReadWord(0x7001, ref hwval, ref lwval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((lwval & 0x0001) == 0x0001)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        public UInt32 eFlashMainSectorErase(UInt16 addr)
        {
            UInt32 waddr = 0;
            UInt16 hwval = 0, lwval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            waddr = (UInt32)(0x100 * addr);
            waddr = (UInt32)((waddr << 18) | 0x0101);
            hwval = (UInt16)(waddr >> 16);
            lwval = (UInt16)waddr;

            ret = AHBWriteWord(0x7001, 0, 0x00);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = AHBWriteWord(0x7000, hwval, lwval);// 0, (UInt16)((addr << 18) | 0x0101));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = AHBReadWord(0x7001, ref hwval, ref lwval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((lwval & 0x0001) == 0x0001)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        public UInt32 eFlashMainPageErase(UInt16 addr)
        {
            UInt32 waddr = 0;
            UInt16 hwval = 0, lwval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            waddr = (UInt32)(0x100 * addr);
            waddr = (UInt32)((waddr << 18) | 0x0201);
            hwval = (UInt16)(waddr >> 16);
            lwval = (UInt16)waddr;

            ret = AHBWriteWord(0x7001, 0, 0x00);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = AHBWriteWord(0x7000, hwval, lwval);//0, (UInt16)((addr << 18) | 0x0201));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = AHBReadWord(0x7001, ref hwval, ref lwval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((lwval & 0x0001) == 0x0001)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        public UInt32 eFlashMainInit()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = AHBWriteWord(0x7006, 0, 0x0777);
            return ret;
        }

        public UInt32 eFlashMainBlockWrite(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 4 + ElementDefine.BLOCK_OPERATION_BYTES;
            StringBuilder sb = new StringBuilder();
            byte[] receivebuf = new byte[1];
            byte[] sendbuf = new byte[DataInLen]; //len + valid data + pec
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    ret = OnWriteWord(0x01, (UInt16)(fAddr / 4));
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    try
                    {
                        sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xFB;
                    sendbuf[2] = (byte)ElementDefine.BLOCK_OPERATION_BYTES;
                    Array.Clear(m_tempArray, 0, m_tempArray.Length);
                    Array.Copy(msg.flashData, fAddr, m_tempArray, 0, ElementDefine.BLOCK_OPERATION_BYTES);
                    reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                    Array.Copy(m_tempArray, 0, sendbuf, 3, m_tempArray.Length);
                    sendbuf[3 + ElementDefine.BLOCK_OPERATION_BYTES] = crc8_calc(ref sendbuf, (UInt16)(3 + ElementDefine.BLOCK_OPERATION_BYTES));

                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen - 2)))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }

                ret = OnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }

        public UInt32 eflashMainBlockRead(ref TASKMessage msg)
        {
            UInt32 size = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];

            UInt16 DataInLen = ElementDefine.BLOCK_OPERATION_BYTES;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(0x02, ElementDefine.BLOCK_OPERATION_BYTES);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (UInt32 fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    ret = OnWriteWord(0x01, (UInt16)(fAddr / 4));
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    try
                    {
                        sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xFB;
                    Array.Clear(m_tempArray, 0, m_tempArray.Length);
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 2), 1))
                        {
                            if (receivebuf[DataInLen + 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                            {
                                return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                            }
                            Array.Copy(receivebuf, 1, m_tempArray, 0, ElementDefine.BLOCK_OPERATION_BYTES);
                            reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                            Array.Copy(m_tempArray, 0, msg.flashData, fAddr, ElementDefine.BLOCK_OPERATION_BYTES);
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        Thread.Sleep(10);
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                ret = OnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }
        #endregion

        #region 其他        
        private void reverseArrayBySize(ref byte[] ary, int subSize)
        {
            byte[] bval = new byte[subSize];
            List<byte> splitted = new List<byte>();//This list will contain all the splitted arrays.

            for (int i = 0; i < ElementDefine.BLOCK_OPERATION_BYTES; i = i + subSize)
            {
                if (ElementDefine.BLOCK_OPERATION_BYTES < i + 4)
                    subSize = ElementDefine.BLOCK_OPERATION_BYTES - i;
                Array.Copy(m_tempArray, i, bval, 0, subSize);
                Array.Reverse(bval);
                for (int j = 0; j < subSize; j++)
                    splitted.Add(bval[j]);
            }
            ary = splitted.ToArray();
        }
        #endregion
        #endregion
    }
}