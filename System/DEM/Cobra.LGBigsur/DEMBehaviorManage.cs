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

namespace Cobra.LGBigsur
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

        private Random rd = new Random();
        private object m_lock = new object();
        private byte[] m_tempArray = new byte[ElementDefine.BLOCK_OPERATION_BYTES];
        private CCommunicateManager m_Interface = new CCommunicateManager();
        private Dictionary<string, string> m_Json_Options = null;

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

        internal UInt32 BlockRead(ref TASKMessage msg)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(ref msg);
            }
            return ret;
        }

        internal UInt32 BlockRead(UInt16 startAddr, ref byte[] buffer)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(startAddr, ref buffer);
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

        internal UInt32 Block4BytesRead(UInt16 startAddr, ref UInt32 dwval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlock4BytesRead(startAddr, ref dwval);
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

        internal UInt32 BlockWrite(ref TASKMessage msg)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(ref msg);
            }
            return ret;
        }

        internal UInt32 BlockWrite(UInt16 startAddr, byte[] buffer)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(startAddr, buffer);
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
                            Array.Copy(receivebuf, 1, msg.flashData, fAddr, DataInLen);
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

        protected UInt32 OnBlockRead(UInt16 startAddr, ref byte[] buffer)
        {
            UInt32 size = 0;
            UInt16 DataOutLen = 0;
            size = (UInt32)buffer.Length;
            byte[] sendbuf = new byte[2];

            UInt16 DataInLen = ElementDefine.BLOCK_OPERATION_BYTES;
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                ret = OnWriteWord(0x02, ElementDefine.BLOCK_OPERATION_BYTES);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (UInt32 fAddr = 0; fAddr < size; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
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
                            Array.Copy(receivebuf, 1, m_tempArray, 0, DataInLen);
                            reverseArrayBySize(ref m_tempArray, 4);
                            Array.Copy(m_tempArray, 0, buffer, fAddr, DataInLen);
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

        protected UInt32 OnBlockWrite(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)(ElementDefine.BLOCK_OPERATION_BYTES + 4); //len,data, pec, address, command
            byte[] receivebuf = new byte[2];
            byte[] sendbuf = new byte[4 + ElementDefine.BLOCK_OPERATION_BYTES];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                ret = OnWriteWord(0x02, ElementDefine.BLOCK_OPERATION_BYTES);
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
            return ret;
        }

        protected UInt32 OnBlockWrite(UInt16 startAddr, byte[] buffer)
        {
            UInt32 size = 0;
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)(ElementDefine.BLOCK_OPERATION_BYTES + 4); //len,data, pec, address, command
            byte[] receivebuf = new byte[2];
            byte[] sendbuf = new byte[4 + ElementDefine.BLOCK_OPERATION_BYTES];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            size = (UInt32)buffer.Length;
            lock (m_lock)
            {
                for (UInt32 fAddr = 0; fAddr < size; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
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
                    Array.Copy(buffer, fAddr, m_tempArray, 0, ElementDefine.BLOCK_OPERATION_BYTES);
                    reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                    Array.Copy(m_tempArray, 0, sendbuf, 3, m_tempArray.Length);
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

        private UInt32 OnBlockCheck(byte count)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnReadWord(0x04, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (((wval & 0x8000) != 0) | ((wval & 0x1000) != 0) | ((wval & 0xFF) != count))
                ret = ElementDefine.IDS_ERR_DEM_BLK_ACCESS;
            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = NormalErase();
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
            TSMBbuffer tsmBuffer = null;
            List<byte> SBSReglist = new List<byte>();
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
                    case ElementDefine.LogElement:
                        {
                            ret = ReadLogArea();
                            return ret;
                        }
                }
            }

            SBSReglist = SBSReglist.Distinct().ToList();
            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[badd];
                ret = BlockRead(badd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            #region Project
            if (m_Json_Options.ContainsKey("Button"))
            {
                switch (m_Json_Options["Button"])
                {
                    case "NormalDownloadPrj":
                        ret = NormalErase();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UnLockI2C();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockWrite(ref msg);
                        break;
                    case "FullDownloadPrj":
                        ret = MainBlockErase();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = UnLockI2C();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockWrite(ref msg);
                        break;
                }
            }
            #endregion
            #region DEBUG
            switch ((ElementDefine.COBRA_COMMAND_MODE)msg.sub_task)
            {
                case ElementDefine.COBRA_COMMAND_MODE.DEBUG_READ:
                    {
                        ret = UnLockI2C();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockRead(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.DEBUG_WRITE:
                    {
                        ret = UnLockI2C();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockWrite(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.MAIN_BLOCK_PROGRAM:
                    {
                        ret = BlockWrite(ref msg);
                        break;
                    }
            }
            #endregion
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)  //Bigsur
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            byte bcmd = 0;
            byte cellnumber = 0;
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
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;/*
            param = GetParameterByGuid(ElementDefine.UC, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;*/

            #region Read Static Parameter
            #region BatteryMode
            param = GetParameterByGuid(ElementDefine.BatteryMode, demparameterlist.parameterlist);
            bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
            TSMBbuffer tsmBuffer = param.tsmbBuffer;
            param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.Hex2Physical(ref param);

            cellnumber = (byte)(tsmBuffer.bdata[0] & 0x1F);
            bTHM0Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 5) & 0x01) > 0 ? true : false;
            bTHM1Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 6) & 0x01) > 0 ? true : false;
            bTHM2Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 7) & 0x01) > 0 ? true : false;
            bTHM3Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 8) & 0x01) > 0 ? true : false;

            param = GetParameterByGuid(ElementDefine.CellVoltMV02, demparameterlist.parameterlist);
            if (cellnumber == 2) param.bShow = true;
            else param.bShow = false;

            if (!bTHM0Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK1, demparameterlist.parameterlist);
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

        private UInt32 UnLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteWord(0x11, ElementDefine.UnLock_I2C_PSW);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(0x12, ElementDefine.I2C_AHB_MODE_Enable_PSW);
            }
            return ret;
        }

        private UInt32 ReLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WriteWord(0x11, ElementDefine.ReLock_I2C_PSW);
            return ret;
        }

        private UInt32 BlockSize(byte rSize = 4, byte wSize = 4)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                wval = SharedFormula.MAKEWORD(rSize, wSize);
                ret = OnWriteWord(0x02, wval);
            }
            return ret;
        }

        private UInt32 MainBlockErase()
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
                ret = ElementDefine.IDS_ERR_DEM_UNLOCK_ERASE;
            }

            ret = BlockWordWrite(ElementDefine.I2C_Adress_MAIN_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_MAIN_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_MAINBLOCK_ERASE;
            }
            return ret;
        }

        private UInt32 PageErase()
        {
            byte Xbcode = 0;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = BlockWordRead(ElementDefine.I2C_Adress_O2BLPROT, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Xbcode = (byte)(wval & 0x01F);

            for (int i = Xbcode + 1; i < 63; i++)
            {
                ret = BlockWordWrite(ElementDefine.I2C_Adress_UNLOCK_ERASE, ElementDefine.Unlock_Erase_PSW);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = BlockWordRead(ElementDefine.I2C_Adress_UNLOCK_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) != 0x01) return ElementDefine.IDS_ERR_DEM_UNLOCK_ERASE;

                wval = SharedFormula.MAKEWORD(0xCD, (byte)i);
                ret = BlockWordWrite(ElementDefine.I2C_Adress_PAGE_ERASE, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
                {
                    Thread.Sleep(20);
                    ret = BlockWordRead(ElementDefine.I2C_Adress_PAGE_ERASE, ref wval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((wval & 0x01) == 0x01)
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    ret = ElementDefine.IDS_ERR_DEM_PAGE_ERASE;
                }
            }

            return ret;
        }

        private UInt32 InforErase()
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

            ret = BlockWordWrite(ElementDefine.I2C_Adress_INFO_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_INFO_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_INFO_ERASE;
            }

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

            ret = BlockWordWrite(ElementDefine.I2C_Adress_MAIN_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_MAIN_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_MAINBLOCK_ERASE;
            }
            return ret;
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

            ret = BlockWordWrite(ElementDefine.I2C_Adress_MAIN_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_MAIN_ERASE, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_MAINBLOCK_ERASE;
            }
            return ret;
        }

        private UInt32 CheckSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE type, UInt16 startAddr, UInt16 endAddr, byte[] buf)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (type)
            {
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash:
                    wval = startAddr;
                    break;
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash:
                    wval = (UInt16)(startAddr | (UInt16)(0x01 << 13));
                    break;
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash:
                    wval = (UInt16)(startAddr | (UInt16)(0x02 << 13));
                    break;
            }

            ret = BlockWordWrite(ElementDefine.I2C_Adress_Start_Address, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = BlockWordWrite(ElementDefine.I2C_Adress_End_Address, endAddr);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = BlockWordWrite(ElementDefine.I2C_Adress_DO_CRC16, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = BlockWordRead(ElementDefine.I2C_Adress_DO_CRC16, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(ElementDefine.I2C_Adress_DO_CRC16, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_CRC16_DONE;
            }

            ret = BlockWordRead(ElementDefine.I2C_Adress_CRC16_Result, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //Compare
            int sCrc = CRC16_CCITT(buf, buf.Length);
            if (sCrc != wval) return ElementDefine.IDS_ERR_DEM_CRC16_COMPARE;

            ret = WriteWord(ElementDefine.I2C_Adress_STATUS, ElementDefine.Unlock_Erase_PSW);
            return ret;
        }

        private UInt32 NormalErase()
        {
            bool bval = false;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = FlashFresh(ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (bval) //active high to indicate eflash is fresh.
                ret = MainBlockErase();
            else
                ret = PageErase();
            ret = ReLockI2C();
            return ret;
        }

        private UInt32 FlashFresh(ref bool bval)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(ElementDefine.I2C_Adress_STATUS, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((wval & 0x0080) == 0x0080)
                bval = false;
            else
                bval = true;
            return ret;
        }

        private UInt32 ForceErase()
        {
            bool bval = false;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = FlashFresh(ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (bval) //active high to indicate eflash is fresh.
                ret = MainBlockErase();
            else
            {
                ret = BlockWordRead(ElementDefine.I2C_Adress_O2BLPROT, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x01) == 0x01)
                {
                    ret = BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = SysErase();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash);
                }
                else
                {
                    ret = BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = InforErase();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash);
                }
            }

            ret = ReLockI2C();
            return ret;
        }

        private UInt32 BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE type)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            switch (type)
            {
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash:
                    Array.Clear(parent.SysFlash_Buffer, 0, parent.SysFlash_Buffer.Length);
                    ret = BlockRead(ElementDefine.SyseFlash_StartAddress, ref parent.SysFlash_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash:
                    Array.Clear(parent.InfoeFlash_Buffer, 0, parent.InfoeFlash_Buffer.Length);
                    ret = BlockRead(ElementDefine.InfoFlash_StartAddress, ref parent.InfoeFlash_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
            }
            ret = ReLockI2C();
            return ret;
        }

        private UInt32 ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE type)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (type)
            {
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash:
                    ret = BlockWrite(ElementDefine.SyseFlash_StartAddress, parent.SysFlash_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash:
                    ret = BlockWrite(ElementDefine.InfoFlash_StartAddress, parent.InfoeFlash_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
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
            //return BlockRead(uadd, ref tsmBuffer);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region Log Aread Access
        private UInt32 ReadLogArea()
        {
            byte num = 0;
            UInt32 eCount1 = 0;
            UInt32 eCount2 = 0;
            UInt32 eCount3 = 0;
            byte[] bArray = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //3K log Area but only show 1k by compare the erase counter
            ret = Block4BytesRead(ElementDefine.LogFlash_EraseCounter1, ref eCount1);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesRead(ElementDefine.LogFlash_EraseCounter2, ref eCount2);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesRead(ElementDefine.LogFlash_EraseCounter3, ref eCount3);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (eCount1 == 0xFF)
            {
                Array.Clear(parent.logAreaArray, 0, parent.logAreaArray.Length);
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }

            if ((eCount1 < eCount2)&&(eCount2 != 0xFF))
            {
                eCount1 = eCount2;
                num = 1;
            }
            if ((eCount1 < eCount3) && (eCount3 != 0xFF))
            {
                eCount1 = eCount3;
                num = 2;
            }

            ret = BlockRead((UInt16)(ElementDefine.LogFlash_StartAddress + num * 1024/4), ref parent.logAreaArray);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReLockI2C();
            return ret;
        }
        #endregion
    }
}