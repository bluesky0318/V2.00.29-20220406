using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.NewTon.G2
{
    public class DEMBehaviorManage
    {
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private Random rd = new Random();
        private object m_lock = new object();
        private CCommunicateManager m_Interface = new CCommunicateManager();
        private Dictionary<UInt32, double> dic = new Dictionary<UInt32, double>();
        private Dictionary<string, string> m_Json_Options = null;
        private byte[] m_tempArray = new byte[ElementDefine.BLOCK_OPERATION_BYTES];
        private byte[] m_tempArray128 = new byte[ElementDefine.BLOCK_OPERATION_BYTES * 4];
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
                if (reg >= 0xE0)
                {
                    ret = OnAllowWriteI2CMapReg();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }

        internal UInt32 APBReadWord(UInt16 addr, UInt16 startAddr, ref UInt16 wval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnAPBReadWord((UInt16)(addr + startAddr), ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnLockI2C();
            }
            return ret;
        }

        internal UInt32 APBWriteWord(UInt16 addr, UInt16 startAddr, UInt16 wval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnAPBWriteWord((UInt16)(addr + startAddr), wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnLockI2C();
            }
            return ret;
        }

        internal UInt32 BlockWordRead(UInt16 startAddr, ref UInt16 wval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWordRead(startAddr, ref wval);
            }
            return ret;
        }

        internal UInt32 BlockWordWrite(UInt16 startAddr, UInt16 wval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWordWrite(startAddr, wval);
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

        protected UInt32 BlockWrite(ref TASKMessage msg)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(ref msg);
            }
            return ret;
        }

        internal UInt32 Block4BytesRead(UInt16 startAddr, ref UInt32 dwval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlock4BytesRead(startAddr, ref dwval);
            }
            return ret;
        }

        internal UInt32 Block4BytesWrite(UInt16 startAddr, UInt32 dwval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlock4BytesWrite(startAddr, dwval);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        #region CRC16-CCITT(初始值FFFF，多项式1021，异或值FFFF，异或输出，表逆序，算法逆序)
        internal ushort[] CRC16Table =
            {
                0x0000, 0x1189, 0x2312, 0x329B, 0x4624, 0x57AD, 0x6536, 0x74BF,
                0x8C48, 0x9DC1, 0xAF5A, 0xBED3, 0xCA6C, 0xDBE5, 0xE97E, 0xF8F7,
                0x1081, 0x0108, 0x3393, 0x221A, 0x56A5, 0x472C, 0x75B7, 0x643E,
                0x9CC9, 0x8D40, 0xBFDB, 0xAE52, 0xDAED, 0xCB64, 0xF9FF, 0xE876,
                0x2102, 0x308B, 0x0210, 0x1399, 0x6726, 0x76AF, 0x4434, 0x55BD,
                0xAD4A, 0xBCC3, 0x8E58, 0x9FD1, 0xEB6E, 0xFAE7, 0xC87C, 0xD9F5,
                0x3183, 0x200A, 0x1291, 0x0318, 0x77A7, 0x662E, 0x54B5, 0x453C,
                0xBDCB, 0xAC42, 0x9ED9, 0x8F50, 0xFBEF, 0xEA66, 0xD8FD, 0xC974,
                0x4204, 0x538D, 0x6116, 0x709F, 0x0420, 0x15A9, 0x2732, 0x36BB,
                0xCE4C, 0xDFC5, 0xED5E, 0xFCD7, 0x8868, 0x99E1, 0xAB7A, 0xBAF3,
                0x5285, 0x430C, 0x7197, 0x601E, 0x14A1, 0x0528, 0x37B3, 0x263A,
                0xDECD, 0xCF44, 0xFDDF, 0xEC56, 0x98E9, 0x8960, 0xBBFB, 0xAA72,
                0x6306, 0x728F, 0x4014, 0x519D, 0x2522, 0x34AB, 0x0630, 0x17B9,
                0xEF4E, 0xFEC7, 0xCC5C, 0xDDD5, 0xA96A, 0xB8E3, 0x8A78, 0x9BF1,
                0x7387, 0x620E, 0x5095, 0x411C, 0x35A3, 0x242A, 0x16B1, 0x0738,
                0xFFCF, 0xEE46, 0xDCDD, 0xCD54, 0xB9EB, 0xA862, 0x9AF9, 0x8B70,
                0x8408, 0x9581, 0xA71A, 0xB693, 0xC22C, 0xD3A5, 0xE13E, 0xF0B7,
                0x0840, 0x19C9, 0x2B52, 0x3ADB, 0x4E64, 0x5FED, 0x6D76, 0x7CFF,
                0x9489, 0x8500, 0xB79B, 0xA612, 0xD2AD, 0xC324, 0xF1BF, 0xE036,
                0x18C1, 0x0948, 0x3BD3, 0x2A5A, 0x5EE5, 0x4F6C, 0x7DF7, 0x6C7E,
                0xA50A, 0xB483, 0x8618, 0x9791, 0xE32E, 0xF2A7, 0xC03C, 0xD1B5,
                0x2942, 0x38CB, 0x0A50, 0x1BD9, 0x6F66, 0x7EEF, 0x4C74, 0x5DFD,
                0xB58B, 0xA402, 0x9699, 0x8710, 0xF3AF, 0xE226, 0xD0BD, 0xC134,
                0x39C3, 0x284A, 0x1AD1, 0x0B58, 0x7FE7, 0x6E6E, 0x5CF5, 0x4D7C,
                0xC60C, 0xD785, 0xE51E, 0xF497, 0x8028, 0x91A1, 0xA33A, 0xB2B3,
                0x4A44, 0x5BCD, 0x6956, 0x78DF, 0x0C60, 0x1DE9, 0x2F72, 0x3EFB,
                0xD68D, 0xC704, 0xF59F, 0xE416, 0x90A9, 0x8120, 0xB3BB, 0xA232,
                0x5AC5, 0x4B4C, 0x79D7, 0x685E, 0x1CE1, 0x0D68, 0x3FF3, 0x2E7A,
                0xE70E, 0xF687, 0xC41C, 0xD595, 0xA12A, 0xB0A3, 0x8238, 0x93B1,
                0x6B46, 0x7ACF, 0x4854, 0x59DD, 0x2D62, 0x3CEB, 0x0E70, 0x1FF9,
                0xF78F, 0xE606, 0xD49D, 0xC514, 0xB1AB, 0xA022, 0x92B9, 0x8330,
                0x7BC7, 0x6A4E, 0x58D5, 0x495C, 0x3DE3, 0x2C6A, 0x1EF1, 0x0F78,
               };

        internal int CRC16_CCITT(Byte[] data, int data_Len)
        {
            int crc = 0xffff;
            int i = 0;
            while ((data_Len--) > 0)
            {
                //data[i] = Convert.ToUInt16(data[i]);
                crc = (crc >> 8) ^ CRC16Table[(crc ^ data[i]) & 0xff];
                i++;
            }
            crc ^= 0xFFFF;
            return crc;
        }
        #endregion

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

        protected UInt32 OnAPBReadWord(UInt16 addr, ref UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 4; //Len,2bytes,PEC
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC

            ret = OnWriteWord(0x02, 0x0004);//ElementDefine.BLOCK_OPERATION_BYTES);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(0x01, addr);
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

        protected UInt32 OnAPBWriteWord(UInt16 addr, UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 8;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            byte[] sendbuf = new byte[DataInLen];//Length and PEC
            byte[] receivebuf = new byte[2];

            /*Not need to set the write size. the register is read only
            ret = OnWriteWord(0x02, (UInt16)array.Length);//ElementDefine.BLOCK_OPERATION_BYTES);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

            ret = OnWriteWord(0x01, addr);
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

        protected UInt32 OnBlockWordRead(UInt16 startAddr, ref UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 4; //Len,2bytes,PEC
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC

            lock (m_lock)
            {
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
                        wval = SharedFormula.MAKEWORD(receivebuf[4], receivebuf[3]);
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }

        protected UInt32 OnBlockWordWrite(UInt16 startAddr, UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 8;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            byte[] sendbuf = new byte[DataInLen];//Length and PEC
            byte[] receivebuf = new byte[2];

            lock (m_lock)
            {
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
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            }
            return ret;
        }

        protected UInt32 OnWaitTriggerCompleted()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnUnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER * 10; i++)
            {
                Thread.Sleep(10);
                ret = OnAPBReadWord(ElementDefine.TRIGGER_SCAN_REG + ElementDefine.DFEController_StartAddress, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & 0x0300) == 0x00)
                {
                    ret = OnLockI2C();
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
            }

            ret = OnLockI2C();
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
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

        protected UInt32 OnBlockRead(ref TASKMessage msg)
        {
            UInt32 address = 0;
            UInt32 startAddr = 0;
            UInt32 size = 0;
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 0;
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            address = UInt32.Parse(options["Address"].Trim());
            startAddr = UInt32.Parse(options["StartAddr"].Trim());
            size = UInt32.Parse(options["Size"].Trim());
            if (size > ElementDefine.BLOCK_OPERATION_BYTES)
                DataInLen = ElementDefine.BLOCK_OPERATION_BYTES;
            else
                DataInLen = (UInt16)size;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC

            lock (m_lock)
            {
                ret = OnWriteWord(0x02, DataInLen);//ElementDefine.BLOCK_OPERATION_BYTES);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (UInt32 fAddr = (address - startAddr); fAddr < /*msg.flashData.Length*/(address - startAddr + size); fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    ret = OnWriteWord(0x01, (UInt16)(fAddr / 4 + startAddr));
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
                            Array.Clear(m_tempArray, 0, m_tempArray.Length);
                            Array.Copy(receivebuf, 1, m_tempArray, 0, receivebuf[0]);
                            reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                            Array.Copy(m_tempArray, 0, msg.flashData, fAddr, DataInLen);
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        Thread.Sleep(10);
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            return ret;
        }

        protected UInt32 OnBlockWrite(ref TASKMessage msg)
        {
            UInt32 address = 0;
            UInt32 startAddr = 0;
            UInt32 size = 0;
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)(ElementDefine.BLOCK_OPERATION_BYTES + 4); //len,data, pec, address, command
            byte[] receivebuf = new byte[2];
            byte[] sendbuf = new byte[4 + ElementDefine.BLOCK_OPERATION_BYTES];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            address = UInt32.Parse(options["Address"].Trim());
            startAddr = UInt32.Parse(options["StartAddr"].Trim());
            size = UInt32.Parse(options["Size"].Trim());

            if (startAddr >= ElementDefine.EEPROMController_StartAddress) //Keep the old way to access chip
            {
                lock (m_lock)
                {
                    ret = OnWriteWord(0x02, ElementDefine.BLOCK_OPERATION_BYTES);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    for (UInt32 fAddr = (address - startAddr); fAddr < /*msg.flashData.Length*/(address - startAddr + size); fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                    {
                        ret = OnWriteWord(0x01, (UInt16)(fAddr / 4 + startAddr));
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
                        //Array.Copy(msg.flashData, fAddr, sendbuf, 3, ElementDefine.BLOCK_OPERATION_BYTES);
                        sendbuf[3 + ElementDefine.BLOCK_OPERATION_BYTES] = crc8_calc(ref sendbuf, (UInt16)(DataInLen - 1));
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
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                }
            }
            else
            {
                ushort addr = 0;
                UInt32 dWal = 0;
                int remainder = (int)(size % 128);
                int quotient = (int)(size / 128);

                //128 * n
                for (UInt32 m = 0; m < quotient; m++)
                {
                    Array.Clear(m_tempArray128, 0, m_tempArray128.Length);
                    Array.Copy(msg.flashData, m * 128, m_tempArray128, 0, m_tempArray128.Length);
                    //1. Write RAM,128bytes interna loop
                    for (UInt32 n = 0; n < 32; n++)
                    {
                        dWal = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 0], m_tempArray128[n * 4 + 1]),
                            SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 2], m_tempArray128[n * 4 + 3]));
                        ret = OnBlock4BytesWrite((ushort)(0x83E0 + n), dWal);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    //2.Write the length
                    ret = OnBlockWordWrite(0x703B, 0x1F);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    //3.Trigger the programming
                    addr = (ushort)(startAddr + m * 128 / 4);
                    ret = OnBlockWordWrite(addr, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                if (remainder != 0)
                {
                    Array.Clear(m_tempArray128, 0, m_tempArray128.Length);
                    Array.Copy(msg.flashData, quotient * 128, m_tempArray128, 0, remainder);
                    //1. Write RAM,128bytes interna loop
                    for (UInt32 n = 0; n < 32; n++)
                    {
                        dWal = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 0], m_tempArray128[n * 4 + 1]),
                            SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 2], m_tempArray128[n * 4 + 3]));
                        ret = OnBlock4BytesWrite((ushort)(0x83E0 + n), dWal);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    //2.Write the length
                    ret = OnBlockWordWrite(0x703B, 0x1F);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    //3.Trigger the programming
                    addr = (ushort)(startAddr + quotient * 128 / 4);
                    ret = OnBlockWordWrite(addr, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            return ret;
        }

        protected UInt32 OnBlock4BytesWrite(UInt16 startAddr, UInt32 wval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 8;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            byte[] sendbuf = new byte[DataInLen];//Length and PEC
            byte[] receivebuf = new byte[2];

            lock (m_lock)
            {
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
                sendbuf[3] = SharedFormula.HiByte((UInt16)(wval >> 16));
                sendbuf[4] = SharedFormula.LoByte((UInt16)(wval >> 16));
                sendbuf[5] = SharedFormula.HiByte((UInt16)wval);
                sendbuf[6] = SharedFormula.LoByte((UInt16)wval);
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
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            }
            return ret;
        }

        protected UInt32 OnBlock4BytesRead(UInt16 startAddr, ref UInt32 dwval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 4; //Len,2bytes,PEC
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC

            lock (m_lock)
            {
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
                        dwval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(receivebuf[4], receivebuf[3]),
                            SharedFormula.MAKEWORD(receivebuf[2], receivebuf[1]));
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = SysErase();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.SOFT_RESET);
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte[] bval = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt16 address = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EpTrimReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> I2CReglist = new List<byte>();
            List<byte> eFlashCtrlList = new List<byte>();
            List<byte> I2CRegistersList = new List<byte>();
            List<byte> TimerRegistersList = new List<byte>();
            List<byte> WDTRegistersList = new List<byte>();
            List<byte> UARTRegistersList = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            try
            {
                foreach (Parameter p in demparameterlist.parameterlist)
                {
                    switch (p.guid & ElementDefine.ElementMask)
                    {
                        case ElementDefine.EEPROMTRIMElement:
                            {
                                if (p == null) break;
                                if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    EpTrimReglist.Add(baddress);
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
                        case ElementDefine.I2CElement:
                            {
                                if (p == null) break;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    I2CReglist.Add(baddress);
                                }
                                break;
                            }
                        case ElementDefine.eFlashCtrlElement:
                            {
                                if (p == null) break;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    eFlashCtrlList.Add(baddress);
                                }
                                break;
                            }
                        case ElementDefine.I2CRegistersElement:
                            {
                                if (p == null) break;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    I2CRegistersList.Add(baddress);
                                }
                                break;
                            }
                        case ElementDefine.TimerRegistersElement:
                            {
                                if (p == null) break;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    TimerRegistersList.Add(baddress);
                                }
                                break;
                            }
                        case ElementDefine.WDTRegistersElement:
                            {
                                if (p == null) break;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    WDTRegistersList.Add(baddress);
                                }
                                break;
                            }
                        case ElementDefine.UARTRegistersElement:
                            {
                                if (p == null) break;
                                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                                {
                                    reg = dic.Value;
                                    baddress = (byte)reg.address;
                                    UARTRegistersList.Add(baddress);
                                }
                                break;
                            }
                        case ElementDefine.TemperatureElement:
                            break;
                    }
                }

                EpTrimReglist = EpTrimReglist.Distinct().ToList();
                OpReglist = OpReglist.Distinct().ToList();
                I2CReglist = I2CReglist.Distinct().ToList();
                eFlashCtrlList = eFlashCtrlList.Distinct().ToList();
                I2CRegistersList = I2CRegistersList.Distinct().ToList();
                TimerRegistersList = TimerRegistersList.Distinct().ToList();
                WDTRegistersList = WDTRegistersList.Distinct().ToList();
                UARTRegistersList = UARTRegistersList.Distinct().ToList();
                //Read 
                foreach (byte badd in OpReglist)
                {
                    ret = APBReadWord(badd, ElementDefine.DFEController_StartAddress, ref wdata);
                    parent.m_OpRegImg[badd].err = ret;
                    parent.m_OpRegImg[badd].val = wdata;
                }
                if (I2CReglist.Count != 0)
                {
                    foreach (byte badd in I2CReglist)
                    {
                        ret = ReadWord(badd, ref wdata);
                        parent.m_I2CRegImg[badd].err = ret;
                        parent.m_I2CRegImg[badd].val = wdata;
                    }
                }
                foreach (byte badd in eFlashCtrlList)
                {
                    ret = APBReadWord(badd, ElementDefine.eFlashCtrl_StartAddress, ref wdata);
                    parent.m_eFlashCtrlImage[badd].err = ret;
                    parent.m_eFlashCtrlImage[badd].val = wdata;
                }
                foreach (byte badd in I2CRegistersList)
                {
                    ret = APBReadWord(badd, ElementDefine.I2CRegisters_StartAddress, ref wdata);
                    parent.m_I2CRegistersImage[badd].err = ret;
                    parent.m_I2CRegistersImage[badd].val = wdata;
                }
                foreach (byte badd in TimerRegistersList)
                {
                    ret = APBReadWord(badd, ElementDefine.TimerRegisters_StartAddress, ref wdata);
                    parent.m_TimerRegistersImage[badd].err = ret;
                    parent.m_TimerRegistersImage[badd].val = wdata;
                }
                foreach (byte badd in WDTRegistersList)
                {
                    ret = APBReadWord(badd, ElementDefine.WDTRegisters_StartAddress, ref wdata);
                    parent.m_WDTRegistersImage[badd].err = ret;
                    parent.m_WDTRegistersImage[badd].val = wdata;
                }
                foreach (byte badd in UARTRegistersList)
                {
                    ret = APBReadWord(badd, ElementDefine.UARTRegisters_StartAddress, ref wdata);
                    parent.m_UARTRegistersImage[badd].err = ret;
                    parent.m_UARTRegistersImage[badd].val = wdata;
                }
                if (EpTrimReglist.Count != 0)
                {
                    address = ElementDefine.EpTrim_Offset_ADDR;
                    ret = EpBlockRead((UInt16)(ElementDefine.SystemArea_StartAddress + address), ref bval, ElementDefine.MTP_MEMORY_SIZE);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    for (int i = 0; i < bval.Length / 4; i++)
                    {
                        parent.m_EpTrimRegImg[i].err = ret;
                        parent.m_EpTrimRegImg[i].val = SharedFormula.MAKEWORD(bval[i * 4], bval[i * 4 + 1]);
                    }
                    foreach (byte baddr in parent.m_MTPSpecial_RegDic.Keys)
                    {
                        switch (baddr)
                        {
                            case 0x0F:
                                parent.m_MTPSpecial_RegDic[baddr].bdata[0] = bval[0 + 4 * baddr];
                                parent.m_MTPSpecial_RegDic[baddr].bdata[1] = bval[1 + 4 * baddr];
                                parent.m_MTPSpecial_RegDic[baddr].bdata[2] = bval[2 + 4 * baddr];
                                parent.m_MTPSpecial_RegDic[baddr].bdata[3] = bval[3 + 4 * baddr];
                                break;
                            case 0x13:
                                parent.m_MTPSpecial_RegDic[baddr].bdata[0] = bval[2 + 4 * baddr];
                                parent.m_MTPSpecial_RegDic[baddr].bdata[1] = bval[3 + 4 * baddr];
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FolderMap.WriteFile(e.Message);
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            List<byte> m_bytes_List = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EpTrimReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> I2CReglist = new List<byte>();
            List<byte> eFlashCtrlList = new List<byte>();
            List<byte> I2CRegistersList = new List<byte>();
            List<byte> TimerRegistersList = new List<byte>();
            List<byte> WDTRegistersList = new List<byte>();
            List<byte> UARTRegistersList = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMTRIMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                EpTrimReglist.Add(baddress);
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
                    case ElementDefine.I2CElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                I2CReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.eFlashCtrlElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                eFlashCtrlList.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.I2CRegistersElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                I2CRegistersList.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TimerRegistersElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                TimerRegistersList.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.WDTRegistersElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                WDTRegistersList.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.UARTRegistersElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                UARTRegistersList.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EpTrimReglist = EpTrimReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            I2CReglist = I2CReglist.Distinct().ToList();
            eFlashCtrlList = eFlashCtrlList.Distinct().ToList();
            I2CRegistersList = I2CRegistersList.Distinct().ToList();
            TimerRegistersList = TimerRegistersList.Distinct().ToList();
            WDTRegistersList = WDTRegistersList.Distinct().ToList();
            UARTRegistersList = UARTRegistersList.Distinct().ToList();
            m_bytes_List.Clear();
            //Write 
            if (EpTrimReglist.Count != 0)
            {
                for (UInt32 i = ElementDefine.EpTrim_Offset_ADDR; i < (ElementDefine.EpTrim_Offset_ADDR + ElementDefine.MTP_MEMORY_SIZE); i++)
                {
                    if (parent.m_EpTrimRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        m_bytes_List.Add(0);
                        m_bytes_List.Add(0);
                        m_bytes_List.Add(0);
                        m_bytes_List.Add(0);
                    }
                    else
                    {
                        m_bytes_List.Add((byte)parent.m_EpTrimRegImg[i].val);
                        m_bytes_List.Add((byte)(parent.m_EpTrimRegImg[i].val >> 8));
                        m_bytes_List.Add(0);
                        m_bytes_List.Add(0);
                    }
                }
                foreach (byte baddr in parent.m_MTPSpecial_RegDic.Keys)
                {
                    switch (baddr)
                    {
                        case 0x0F:
                            m_bytes_List[0 + 4 * baddr] = parent.m_MTPSpecial_RegDic[baddr].bdata[0];
                            m_bytes_List[1 + 4 * baddr] = parent.m_MTPSpecial_RegDic[baddr].bdata[1];
                            m_bytes_List[2 + 4 * baddr] = parent.m_MTPSpecial_RegDic[baddr].bdata[2];
                            m_bytes_List[3 + 4 * baddr] = parent.m_MTPSpecial_RegDic[baddr].bdata[3];
                            break;
                        case 0x13:
                            m_bytes_List[2 + 4 * baddr] = parent.m_MTPSpecial_RegDic[baddr].bdata[0];
                            m_bytes_List[3 + 4 * baddr] = parent.m_MTPSpecial_RegDic[baddr].bdata[1];
                            break;
                    }
                }
                ret = EpBlockWrite(ElementDefine.EpTrim_Offset_ADDR + ElementDefine.SystemArea_StartAddress, m_bytes_List.ToArray());
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            }
            if (I2CReglist.Count != 0)
            {
                foreach (byte badd in I2CReglist)
                {
                    if (badd == ElementDefine.RegI2C_Unlock_I2cCfgWrt)
                    {
                        ret = WriteWord(badd, ElementDefine.UnLock_I2C_PSW);
                        parent.m_I2CRegImg[badd].err = ret1;
                        ret |= ret1;
                        continue;
                    }
                    if (badd == ElementDefine.RegI2C_MEMD)
                    {
                        if ((parent.m_I2CRegImg[badd].val & 0x8080) == 0x8080)
                        {
                            ret = WriteWord(badd, (UInt16)(ElementDefine.I2C_MEMD_PWD | (parent.m_I2CRegImg[badd].val & 0x01)));
                            parent.m_I2CRegImg[badd].err = ret1;
                            ret |= ret1;
                            continue;
                        }
                    }
                    if (badd == ElementDefine.RegI2C_AHB_MODE)
                    {
                        ret = WriteWord(badd, (UInt16)(0x6300 | parent.m_I2CRegImg[badd].val));
                        parent.m_I2CRegImg[badd].err = ret1;
                        ret |= ret1;
                        continue;

                    }
                    ret1 = WriteWord(badd, parent.m_I2CRegImg[badd].val);
                    parent.m_I2CRegImg[badd].err = ret1;
                    ret |= ret1;
                }
            }
            foreach (byte badd in OpReglist)
            {
                if ((badd == ElementDefine.RegI2C_Unlock_CfgWrt) | (badd == ElementDefine.RegI2C_Unlock_PwrmdWrt))
                {
                    ret = APBWriteWord(badd, ElementDefine.DFEController_StartAddress, ElementDefine.UnLock_I2C_PSW);
                    parent.m_OpRegImg[badd].err = ret;
                    continue;
                }
                ret = APBWriteWord(badd, ElementDefine.DFEController_StartAddress, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }
            foreach (byte badd in eFlashCtrlList)
            {
                ret = APBWriteWord(badd, ElementDefine.eFlashCtrl_StartAddress, parent.m_eFlashCtrlImage[badd].val);
                parent.m_eFlashCtrlImage[badd].err = ret;
            }
            foreach (byte badd in I2CRegistersList)
            {
                ret = APBWriteWord(badd, ElementDefine.I2CRegisters_StartAddress, parent.m_I2CRegistersImage[badd].val);
                parent.m_I2CRegistersImage[badd].err = ret;
            }
            foreach (byte badd in TimerRegistersList)
            {
                ret = APBWriteWord(badd, ElementDefine.TimerRegisters_StartAddress, parent.m_TimerRegistersImage[badd].val);
                parent.m_TimerRegistersImage[badd].err = ret;
            }
            foreach (byte badd in WDTRegistersList)
            {
                ret = APBWriteWord(badd, ElementDefine.WDTRegisters_StartAddress, parent.m_WDTRegistersImage[badd].val);
                parent.m_WDTRegistersImage[badd].err = ret;
            }
            foreach (byte badd in UARTRegistersList)
            {
                ret = APBWriteWord(badd, ElementDefine.UARTRegisters_StartAddress, parent.m_UARTRegistersImage[badd].val);
                parent.m_UARTRegistersImage[badd].err = ret;
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = demparameterlist.parameterlist[i];
                if (param == null) continue;
                m_parent.Hex2Physical(ref param);
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = demparameterlist.parameterlist[i];
                if (param == null) continue;
                m_parent.Physical2Hex(ref param);
            }
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt16 old_subtype = 0;
            Parameter param = null;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
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
                                    ret = EnableCADC();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = SetGPIOMode();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = DisableAutoScan();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE);
                                }
                                break;
                            case "TrigFour":
                                {
                                    ret = EnableCADC();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = SetGPIOMode();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = DisableAutoScan();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_FOUR_MODE);
                                }
                                break;
                            case "AutoScanFour":
                                ret = EnableCADC();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = SetGPIOMode();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = EnableAutoScan(ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_FOUR_MODE);
                                break;
                            case "AutoScanOne":
                                ret = EnableCADC();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = SetGPIOMode();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = EnableAutoScan(ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_ONE_MODE);
                                break;
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        break;
                    }
                #region SCS SFL Command
                case ElementDefine.COBRA_COMMAND_MODE.SCS_TRIGGER_SCAN_EIGHT_MODE:
                    {
                        ret = EnableCADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = SetGPIOMode();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = DisableAutoScan();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_FOUR_MODE);
                    }
                    break;
                #endregion
                #region Trim SFL Command
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_SLOPE_FOUR_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);
                        #region 准备寄存器初始化
                        ret = ClearTrimAndOffset(msg.task_parameterlist);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = EnableCADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = SetGPIOMode();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = DisableAutoScan();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion

                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_FOUR_MODE);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                                if (param.guid == ElementDefine.TS2GPIOAdcSel) continue;

                                old_subtype = param.subtype;
                                param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;
                                m_parent.Hex2Physical(ref param);
                                param.subtype = old_subtype;
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                            }

                            param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.TS2GPIOAdcSel);
                            if (param != null)
                            {
                                ret = APBReadWord(ElementDefine.GPIOCFG, ElementDefine.DFEController_StartAddress, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                wval &= 0xFFCF;
                                wval |= 0x0070;
                                ret = APBWriteWord(ElementDefine.GPIOCFG, ElementDefine.DFEController_StartAddress, wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_TS2_GPIO);
                                ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                ret = WaitTriggerCompleted();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = APBReadWord(0x4A, ElementDefine.DFEController_StartAddress, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                param.phydata = (double)((double)wval * param.phyref / param.regref);
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
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
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_OFFSET_FOUR_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);
                        #region 准备寄存器初始化
                        ret = EnableCADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = SetGPIOMode();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = DisableAutoScan();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion

                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_FOUR_MODE);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                                if (param.guid == ElementDefine.TS2GPIOAdcSel) continue;

                                old_subtype = param.subtype;
                                param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;
                                m_parent.Hex2Physical(ref param);
                                param.subtype = old_subtype;
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                            }
                            param = msg.task_parameterlist.GetParameterByGuid(ElementDefine.TS2GPIOAdcSel);
                            if (param != null)
                            {
                                ret = APBReadWord(ElementDefine.GPIOCFG, ElementDefine.DFEController_StartAddress, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                wval &= 0xFFCF;
                                wval |= 0x0070;
                                ret = APBWriteWord(ElementDefine.GPIOCFG, ElementDefine.DFEController_StartAddress, wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_TS2_GPIO);
                                ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                ret = WaitTriggerCompleted();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                ret = APBReadWord(0x4A, ElementDefine.DFEController_StartAddress, ref wval);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                param.phydata = (double)((double)wval * param.phyref / param.regref);
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
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
                        if (ElementDefine.TRIM_TIMES == 5)
                            CountSlope(msg.task_parameterlist);
                        else
                            CountSlopeAndOffset(msg.task_parameterlist);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_COUNT_OFFSET:
                    {
                        CountOffset(msg.task_parameterlist);
                        break;
                    }
                #endregion
                #region DEBUG
                case ElementDefine.COBRA_COMMAND_MODE.DEBUG_READ:
                    {
                        ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = BlockRead(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_RUN);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.DEBUG_WRITE:
                    {
                        ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = BlockWrite(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_RUN);
                        break;
                    }
                    #endregion
            }
            #endregion
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)  //Newton
        {
            int ival = 0;
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;

            ret = APBReadWord(0x20, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteWord(0x11, ElementDefine.UnLock_I2C_PSW);
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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

        private void reverseArrayBySize(byte[] flashData, ref byte[] ary, int subSize)
        {
            byte[] bval = new byte[subSize];
            List<byte> splitted = new List<byte>();//This list will contain all the splitted arrays.

            for (int i = 0; i < 64 * 1024; i = i + subSize)
            {
                Array.Copy(flashData, i, bval, 0, subSize);
                Array.Reverse(bval);
                for (int j = 0; j < subSize; j++)
                    splitted.Add(bval[j]);
            }
            ary = splitted.ToArray();
        }

        public UInt32 UnLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
            }
            return ret;
        }

        public UInt32 ReLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnLockI2C();
            }
            return ret;
        }

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
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid)) return param;
            }
            return null;
        }

        private UInt32 EnableAutoScan(ElementDefine.COBRA_COMMAND_MODE smode)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (smode)
            {
                case ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_ONE_MODE:
                    {
                        ret = APBWriteWord(ElementDefine.AUTO_SCAN_REG, ElementDefine.DFEController_StartAddress, ElementDefine.AUTO_SCAN_ONE_MODE);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UpdateCell2();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.AUTO_SCAN_FOUR_MODE:
                    {
                        ret = APBWriteWord(ElementDefine.AUTO_SCAN_REG, ElementDefine.DFEController_StartAddress, ElementDefine.AUTO_SCAN_FOUR_MODE);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UpdateCell2();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
            }
            return ret;
        }

        private UInt32 EnableTriggerScan(ElementDefine.COBRA_COMMAND_MODE smode)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (smode)
            {
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE:
                    {
                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_TS2_GPIO);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_CELL1_CELL2);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();

                        ret = UpdateCell2();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_ISENS);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UpdateISENS();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_INTEL_TEMP);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_EXTTEMP);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VCC);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VD15);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();

                        wval = (ElementDefine.TRIGGER_SCAN_ONE_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_INT_ID);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_FOUR_MODE:
                    {
                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_TS2_GPIO);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_CELL1_CELL2);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UpdateCell2();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_ISENS);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UpdateISENS();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_INTEL_TEMP);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_EXTTEMP);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VCC);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_VD15);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();

                        wval = (ElementDefine.TRIGGER_SCAN_FOUR_MODE | (UInt16)ElementDefine.COBRA_TRIGGER_SCAN_CHANNEL.TRIGGER_SCAN_CHANNEL_INT_ID);
                        ret = APBWriteWord(ElementDefine.TRIGGER_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WaitTriggerCompleted();
                    }
                    break;
            }
            return ret;
        }

        public UInt32 EnableCADC()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = APBReadWord(ElementDefine.CADCCTRL_REG, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval |= 0x01;
            return APBWriteWord(ElementDefine.CADCCTRL_REG, ElementDefine.DFEController_StartAddress, wval);
        }

        public UInt32 SetGPIOMode()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = APBReadWord(ElementDefine.GPIOCFG, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xFFCF;
            wval |= 0x0030;
            return APBWriteWord(ElementDefine.GPIOCFG, ElementDefine.DFEController_StartAddress, wval);
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

            ret = APBReadWord(ElementDefine.AUTO_SCAN_REG, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xFF70;
            return APBWriteWord(ElementDefine.AUTO_SCAN_REG, ElementDefine.DFEController_StartAddress, wval);
        }

        private UInt32 SysErase()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockWordWrite(ElementDefine.I2C_Adress_UNLOCK_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_UNLOCK_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_PAGE_ERASE;
            }

            ret = BlockWordWrite(ElementDefine.I2C_Adress_SYS_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_SYS_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_PAGE_ERASE;
            }
            return ret;
        }
        #endregion

        #region Trim Count
        public UInt32 ClearTrimAndOffset(ParamContainer demparameterlist)
        {
            UInt16 wval = 0;
            Reg regLow = null;
            Parameter param = null;
            Tuple<Parameter, Parameter> slope_offset = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            /*
            for (byte i = 0x06; i <= 0x0D; i++)
                ret = WriteWord((byte)(i + 0xE0), 0);*/
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if (!parent.m_guid_slope_offset.ContainsKey(param.guid)) continue;
                slope_offset = parent.m_guid_slope_offset[param.guid];

                if (!slope_offset.Item1.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item1.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (!slope_offset.Item2.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item2.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }

        private void InitDataPointList(ParamContainer demparameterlist)
        {//建构DataPoint清单，并获取input值
            DataPoint dataPoint = null;
            Parameter param = null;

            if ((ElementDefine.m_trim_count == 0) | (ElementDefine.m_trim_count == ElementDefine.TRIM_TIMES))
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
            DataPoint dataPoint = m_dataPoint_List.Find(delegate (DataPoint item)
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
            Parameter i2cparam = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (m_dataPoint_List.Count == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;
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
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_Cell1_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.Cell2_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_Cell2_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.GPIO:
                        param = GetParameterByGuid(ElementDefine.GPIO_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_GPIO_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VCC:
                        param = GetParameterByGuid(ElementDefine.VCC_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_VCC_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.ExtTemp:
                        param = GetParameterByGuid(ElementDefine.ExtTemp_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_ExtTemp_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VirtualISENS:
                    case ElementDefine.ISENS:
                        param = GetParameterByGuid(ElementDefine.ISENS_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_ISENS_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CADC:
                        param = GetParameterByGuid(ElementDefine.CADC_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_CADC_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.INT_ID:
                        param = GetParameterByGuid(ElementDefine.INT_ID_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_INT_ID_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.TS2_GPIO:
                        param = GetParameterByGuid(ElementDefine.TS2_GPIO_Slope_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_TS2_GPIO_Slope_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                //i2cparam.phydata = param.phydata = Math.Round((slope - 1) * 2048, 0);
                if (i2cparam.guid == ElementDefine.I2C_CADC_Slope_Trim)
                    i2cparam.phydata = param.phydata = Math.Round((1 - slope) * 4096, 0);
                else
                    i2cparam.phydata = param.phydata = Math.Round((1 - slope) * 2048, 0);
                ConvertSlope(ref param);
                i2cparam.phydata = param.phydata;
            }
            return ret;
        }

        private UInt32 CountOffset(ParamContainer demparameterlist)
        {
            double offset = 0;
            DataPoint dataPoint = null;
            Parameter param = null;
            Parameter i2cparam = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (ElementDefine.TRIM_TIMES == 2) return LibErrorCode.IDS_ERR_SUCCESSFUL;
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
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_Cell1_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.Cell2_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_Cell2_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.GPIO:
                        param = GetParameterByGuid(ElementDefine.GPIO_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_GPIO_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VCC:
                        param = GetParameterByGuid(ElementDefine.VCC_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_VCC_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.ExtTemp:
                        param = GetParameterByGuid(ElementDefine.ExtTemp_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_ExtTemp_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VirtualISENS:
                    case ElementDefine.ISENS:
                        param = GetParameterByGuid(ElementDefine.ISENS_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_ISENS_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CADC:
                        param = GetParameterByGuid(ElementDefine.CADC_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_CADC_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.INT_ID:
                        param = GetParameterByGuid(ElementDefine.INT_ID_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_INT_ID_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.TS2_GPIO:
                        param = GetParameterByGuid(ElementDefine.TS2_GPIO_Offset_Trim, demparameterlist.parameterlist);
                        i2cparam = GetParameterByGuid(ElementDefine.I2C_TS2_GPIO_Offset_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                i2cparam.phydata = param.phydata = offset;
                parent.m_dem_dm.Physical2Hex(ref i2cparam);
            }
            return ret;
        }

        private UInt32 CountSlopeAndOffset(ParamContainer demparameterlist)
        {
            double slope = 0, offset = 0;
            DataPoint dataPoint = null;
            Parameter param = null;
            Parameter offsetparam = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (m_dataPoint_List.Count == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                dataPoint = GetDataPointByGuid(param.guid);
                if (dataPoint == null) continue; //Offset Slope parameter no data point
                else
                {
                    ret = dataPoint.GetSlopeAndOffset(ref slope, ref offset);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                switch (param.guid)
                {
                    case ElementDefine.CellVoltage01:
                        param = GetParameterByGuid(ElementDefine.I2C_Cell1_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_Cell1_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.I2C_Cell2_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_Cell2_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.GPIO:
                        param = GetParameterByGuid(ElementDefine.I2C_GPIO_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_GPIO_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VCC:
                        param = GetParameterByGuid(ElementDefine.I2C_VCC_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_VCC_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.ExtTemp:
                        param = GetParameterByGuid(ElementDefine.I2C_ExtTemp_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_ExtTemp_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VirtualISENS:
                    case ElementDefine.ISENS:
                        param = GetParameterByGuid(ElementDefine.I2C_ISENS_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_ISENS_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CADC:
                        param = GetParameterByGuid(ElementDefine.I2C_CADC_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_CADC_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.INT_ID:
                        param = GetParameterByGuid(ElementDefine.I2C_INT_ID_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_INT_ID_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.TS2_GPIO:
                        param = GetParameterByGuid(ElementDefine.I2C_TS2_GPIO_Slope_Trim, demparameterlist.parameterlist);
                        offsetparam = GetParameterByGuid(ElementDefine.I2C_TS2_GPIO_Offset_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                //i2cparam.phydata = param.phydata = Math.Round((slope - 1) * 2048, 0);
                param.phydata = Math.Round((1 - slope) * 2048, 0);
                ConvertSlope(ref param);
                offsetparam.phydata = offset;
                parent.m_dem_dm.Physical2Hex(ref offsetparam);
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
                tr = (UInt16)(regHi.bitsnumber + regLow.bitsnumber - 1);
            else
                tr = (UInt16)(regLow.bitsnumber - 1);

            wdata = (UInt16)(1 << tr);
            wdata |= (UInt16)Math.Abs(slop);
            param.phydata = (double)wdata;
        }

        private UInt32 UpdateISENS()
        {
            UInt16 wval = 0, Cal_Raw_isens = 0, Raw_isens = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = APBReadWord(0x5F, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_isens_struct.raw_vadc_chop0_data = wval;

            ret = APBReadWord(0x60, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_isens_struct.raw_vadc_chop1_data = wval;

            ret = APBReadWord(0x63, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_isens_struct.v600mv_vadc_data = wval;

            ret = APBReadWord(0x47, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_isens_struct.isens_vadc_data = wval;

            ret = ReadWord(0xE8, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_isens_struct.isens_slop_trim = (UInt16)(wval & 0x1F);
            parent.m_isens_struct.isens_slop_trim_bit6 = ((wval & 0x20) == 0) ? false : true;

            ret = ReadWord(0xEB, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_isens_struct.isens_offset = (UInt16)((sbyte)(wval & 0xFF));

            Raw_isens = (UInt16)((parent.m_isens_struct.raw_vadc_chop0_data + parent.m_isens_struct.raw_vadc_chop1_data) / 2 - parent.m_isens_struct.v600mv_vadc_data);
            Cal_Raw_isens = (UInt16)(Raw_isens * parent.m_isens_struct.isens_slop_trim);
            Cal_Raw_isens = (UInt16)(Cal_Raw_isens >> 11);
            if (parent.m_isens_struct.isens_slop_trim_bit6)
                Raw_isens += Cal_Raw_isens;
            else
                Raw_isens -= Cal_Raw_isens;
            parent.m_isens_struct.end_isens = (UInt16)(Raw_isens - parent.m_isens_struct.isens_offset);
            /*
            FolderMap.WriteFile("--------------------ISENS-----------------------------------");
            FolderMap.WriteFile(string.Format("Chop0:0x{0:x4} Chop1:0x{1:x4} V600:0x{2:x4}", parent.m_isens_struct.raw_vadc_chop0_data, parent.m_isens_struct.raw_vadc_chop1_data, parent.m_isens_struct.v600mv_vadc_data));
            FolderMap.WriteFile(string.Format("Slop:0x{0:x2} offset:0x{1:x2}", parent.m_isens_struct.isens_slop_trim, parent.m_isens_struct.isens_offset));
            FolderMap.WriteFile(string.Format("isens_vadc:0x{0:x4} cal_isens_vadc:0x{1:x4}", parent.m_isens_struct.isens_vadc_data, parent.m_isens_struct.end_isens));
            FolderMap.WriteFile("--------------------End-----------------------------------");*/
            return ret;
        }

        private UInt32 UpdateCell2()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = APBReadWord(0x45, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_OpRegImg[0x45].val = wval;
            parent.m_OpRegImg[0x45].err = ret;

            ret = APBReadWord(0x4B, ElementDefine.DFEController_StartAddress, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_OpRegImg[0x4B].val = wval;
            parent.m_OpRegImg[0x4B].err = ret;
            return ret;
        }
        #endregion

        #region EEPROM
        public UInt32 ChipOperation(ElementDefine.CHIP_OPERA_MODE mode)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            switch (mode)
            {
                case ElementDefine.CHIP_OPERA_MODE.SOFT_RESET:
                    ret = BlockWordWrite(0xC6AA, 0x6318);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = BlockWordWrite(0xC6AB, 0x0010);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.CHIP_OPERA_MODE.CPU_HOLD:
                    ret = WriteWord(0x11, ElementDefine.UnLock_I2C_PSW);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = WriteWord(0x12, 0x6303);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = BlockWordWrite(0xC6AA, 0x6318);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = BlockWordWrite(0xC6AB, 0x0000);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.CHIP_OPERA_MODE.CPU_RUN:
                    ret = ReLockI2C();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
            }
            //ret = ReLockI2C();
            return ret;
        }

        public UInt32 EpBlockRead(UInt16 reg, ref byte[] buf, int len = 4)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = ElementDefine.BLOCK_OPERATION_BYTES;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[DataInLen + 2];
            int remainder = (int)(len % ElementDefine.BLOCK_OPERATION_BYTES);
            int quotient = (int)(len / ElementDefine.BLOCK_OPERATION_BYTES);
            List<byte> m_bytes_List = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            m_bytes_List.Clear();
            if (len % 4 != 0) return ElementDefine.IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT;
            sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            for (int k = 0; k < quotient; k++)
            {
                DataInLen = ElementDefine.BLOCK_OPERATION_BYTES;
                ret = OnWriteWord(0x02, ElementDefine.BLOCK_OPERATION_BYTES);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(0x01, (UInt16)(ElementDefine.BLOCK_OPERATION_BYTES / 4 * k + reg));
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                sendbuf[1] = 0xFB;
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 2), 1))
                    {
                        if (receivebuf[DataInLen + 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                        {
                            return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                        }
                        Array.Clear(m_tempArray, 0, m_tempArray.Length);
                        Array.Copy(receivebuf, 1, m_tempArray, 0, receivebuf[0]);
                        reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                        m_bytes_List.AddRange(m_tempArray);
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (remainder != 0)
            {
                DataInLen = (UInt16)(remainder + 2);
                reg += (UInt16)(quotient * ElementDefine.BLOCK_OPERATION_BYTES);
                ret = OnWriteWord(0x02, DataInLen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(0x01, reg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                sendbuf[1] = 0xFB;
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen, 1))
                    {
                        if (receivebuf[DataInLen + 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                        {
                            return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                        }
                        Array.Clear(m_tempArray, 0, m_tempArray.Length);
                        Array.Copy(receivebuf, 1, m_tempArray, 0, receivebuf[0]);
                        reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                        m_bytes_List.AddRange(m_tempArray);
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
            }
            buf = m_bytes_List.ToArray();
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_RUN);
            return ret;
        }
        public UInt32 EpBlockWrite(UInt32 sAddr, byte[] buffer)
        {
            UInt32 dWal = 0;
            ushort addr = 0, len = 0;
            int remainder = (int)(buffer.Length % 128);
            int quotient = (int)(buffer.Length / 128);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //128 * n
            for (UInt32 m = 0; m < quotient; m++)
            {
                Array.Clear(m_tempArray128, 0, m_tempArray128.Length);
                Array.Copy(buffer, m * 128, m_tempArray128, 0, m_tempArray128.Length);
                //1. Write RAM,128bytes interna loop
                for (UInt32 n = 0; n < 32; n++)
                {
                    dWal = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 0], m_tempArray128[n * 4 + 1]), SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 2], m_tempArray128[n * 4 + 3]));
                    ret = OnBlock4BytesWrite((ushort)(0x83E0 + n), dWal);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                //2.Write the length
                ret = OnBlockWordWrite(0x703B, 0x1F);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                //3.Trigger the programming
                addr = (ushort)(sAddr + m * 128 / 4);
                ret = OnBlockWordWrite(addr, 0);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (remainder != 0)
            {
                len = (UInt16)(remainder / 4);
                Array.Clear(m_tempArray128, 0, m_tempArray128.Length);
                Array.Copy(buffer, quotient * 128, m_tempArray128, 0, remainder);
                //1. Write RAM,128bytes interna loop
                for (UInt32 n = 0; n < len; n++)
                {
                    dWal = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 0], m_tempArray128[n * 4 + 1]), SharedFormula.MAKEWORD(m_tempArray128[n * 4 + 2], m_tempArray128[n * 4 + 3]));
                    ret = OnBlock4BytesWrite((ushort)(0x83E0 + n), dWal);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                //2.Write the length
                ret = OnBlockWordWrite(0x703B, len);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                //3.Trigger the programming
                addr = (ushort)(sAddr + quotient * 128 / 4);
                ret = OnBlockWordWrite(addr, 0);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_RUN);
            return ret;
        }
        #endregion
    }
}