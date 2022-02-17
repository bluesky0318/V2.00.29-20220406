using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.SD77206.SBS
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
        private bool bFresh = false;
        private bool bProtect = false;
        private byte logNum = 0;
        private SHA256 sha256 = new SHA256CryptoServiceProvider();//建立一個SHA256

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

        protected UInt32 BlockRead(byte cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(cmd, ref pval);
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

        protected UInt32 BlockWrite(byte cmd, TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, pval);
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
        UInt16 calc_crc16_byu32(UInt32 data, UInt16 crcin)
        {
            UInt16 data_temp_hi, data_temp_lo;
            UInt16 crc16;// = data ^ crcin;
            byte index;//, jindex;

            data_temp_hi = (UInt16)((data & 0xFFFF0000) >> 16);
            data_temp_lo = (UInt16)((data & 0x0000FFFF));
            crc16 = (UInt16)(crcin ^ data_temp_hi);
            for (index = 0; index < 16; index++)				/* Prepare to rotate 16 bits */
            {
                if ((crc16 & 0x8000) == 0x8000)   						/* b15 is set... */
                    crc16 = (UInt16)((crc16 << 1) ^ ElementDefine.CRC16_POLY);		/* rotate and XOR with polynomic */
                else                          				/* b15 is clear... */
                    crc16 = (UInt16)((crc16 << 1));					/* just rotate */
            }												/* Loop for 16 bits */
            crc16 = (UInt16)(crc16 ^ data_temp_lo);
            for (index = 0; index < 16; index++)				/* Prepare to rotate 16 bits */
            {
                if ((crc16 & 0x8000) == 0x8000)   					/* b15 is set... */
                    crc16 = (UInt16)((crc16 << 1) ^ ElementDefine.CRC16_POLY);		/* rotate and XOR with polynomic */
                else                          				/* b15 is clear... */
                    crc16 = (UInt16)((crc16 << 1));					/* just rotate */
            }												/* Loop for 16 bits */

            return (crc16);									/* Return updated CRC */
        }

        internal int CRC16_CCITT(Byte[] data, int data_Len)
        {
            UInt16 crc = 0;
            byte[] barray = new byte[4];
            UInt32 dval = 0;
            for (UInt32 fAddr = 0; fAddr < data_Len; fAddr += 4) //必须从上传下来的是1024的整数倍，多余的填写0xFF
            {
                Array.Copy(data, fAddr, barray, 0, 4);
                dval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(barray[0], barray[1]), SharedFormula.MAKEWORD(barray[2], barray[3]));
                crc = calc_crc16_byu32(dval, crc);
            }
            return crc;
        }

        internal int CRC16_CCITT(Byte[] data, UInt32 startAddr, int data_Len)
        {
            UInt16 crc = 0;
            byte[] barray = new byte[4];
            UInt32 dval = 0;
            for (UInt32 fAddr = startAddr; fAddr < (startAddr + data_Len); fAddr += 4) //必须从上传下来的是1024的整数倍，多余的填写0xFF
            {
                Array.Copy(data, fAddr, barray, 0, 4);
                dval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(barray[0], barray[1]), SharedFormula.MAKEWORD(barray[2], barray[3]));
                crc = calc_crc16_byu32(dval, crc);
            }
            return crc;
        }
        #endregion

        #region CRC8
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
        #endregion

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
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    pval = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
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
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    wval = SharedFormula.MAKEWORD(receivebuf[4], receivebuf[3]);
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
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

        protected UInt32 OnBlockRead(byte cmd, ref TSMBbuffer pval)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInlen = (UInt16)(pval.length + 1);
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[pval.length + 1];
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
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInlen))
                {
                    if ((cmd >= 0x20) & (cmd <= 0x23))
                    {
                        if (!SpecialCmdPEC(sendbuf, receivebuf))
                        {
                            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                            continue;
                        }
                    }
                    else
                    {
                        if (receivebuf[DataInlen - 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                        {
                            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                            continue;
                        }
                    }
                    Array.Copy(receivebuf, pval.bdata, pval.length);
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
                            Array.Copy(receivebuf, 1, buffer, fAddr, DataInLen);
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

        protected UInt32 OnBlockWrite(byte cmd, TSMBbuffer pval)
        {
            UInt16 DataOutLen = (UInt16)(pval.length + 3);
            byte[] sendbuf = new byte[pval.length + 3];
            byte[] receivebuf = new byte[1];
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
            Array.Copy(pval.bdata, 0, sendbuf, 2, pval.length);
            sendbuf[pval.length + 2] = crc8_calc(ref sendbuf, (UInt16)(sendbuf.Length - 1));
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)   
            {
                //if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(sendbuf.Length -2)))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
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
            try
            {
                address = UInt32.Parse(options["Address"].Trim());
                startAddr = UInt32.Parse(options["StartAddr"].Trim());
                size = UInt32.Parse(options["Size"].Trim());
            }
            catch
            {
                address = 0;
                startAddr = 0;
                size = (uint)msg.flashData.Length;
            }

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
                    /*
                    Array.Clear(m_tempArray, 0, m_tempArray.Length);
                    Array.Copy(buffer, fAddr, m_tempArray, 0, ElementDefine.BLOCK_OPERATION_BYTES);
                    reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                    Array.Copy(m_tempArray, 0, sendbuf, 3, m_tempArray.Length);*/
                    Array.Copy(buffer, fAddr, sendbuf, 3, ElementDefine.BLOCK_OPERATION_BYTES);
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
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 BlockErase(ref TASKMessage msg)
        {
            string path = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = sysEraseProtect(ref path);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = infoEraseProtect();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = RemapEflash();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = MainBlockErase();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (!bFresh & bProtect)
            {
                ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = RemapEflash();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ResetMcu();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReLockI2C();
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
            List<Parameter> LogParamList = new List<Parameter>();
            List<Parameter> ProjParamList = new List<Parameter>();
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
                            if (p == null) break;
                            LogParamList.Add(p);
                            break;
                        }
                    case ElementDefine.ProParaElement:
                        {
                            if (p == null) break;
                            ProjParamList.Add(p);
                            break;
                        }
                    case ElementDefine.VirtualElement:
                        {
                            if (p == null) break;
                            if ((p.guid == ElementDefine.Virtual_Charger_Cur) | (p.guid == ElementDefine.Virtual_DisCharger_Cur))
                            {
                                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                                SBSReglist.Add(bcmd);
                            }
                            break;
                        }
                }
            }

            SBSReglist = SBSReglist.Distinct().ToList();
            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_SBS_CMD_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_SBS_CMD_Dic[badd];
                ret = BlockRead(badd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            if (LogParamList.Count != 0)
                ret = ReadLogArea();
            if (ProjParamList.Count != 0)
                ret = BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_PARAMETER);
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
                    case ElementDefine.VirtualElement:
                        {
                            if (p == null) break;
                            if ((p.guid == ElementDefine.Virtual_Charger_Cur) | (p.guid == ElementDefine.Virtual_DisCharger_Cur))
                                SBSParamList.Add(p);
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
                if (LogParamList.Find((Parameter p) => p.guid == 0x0006F484) != null)
                {
                    ConvertEventLog(ref msg, ref LogParamList);
                    return ret;
                }
                for (int i = 0; i < LogParamList.Count; i++)
                {
                    param = (Parameter)LogParamList[i];
                    if (param == null) continue;
                    m_parent.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        private void ConvertEventLog(ref TASKMessage msg, ref List<Parameter> LogParamList)
        {
            Parameter param = null;
            param = LogParamList.Find((Parameter p) => p.guid == 0x0006F484);
            if (param == null) return;
            m_parent.Hex2Physical(ref param);
            if ((param.phydata == 65535) | (logNum == 26))
            {
                for (int i = 0; i < LogParamList.Count; i++)
                {
                    param = (Parameter)LogParamList[i];
                    if (param == null) continue;
                    param.reglist["Low"].address -= (UInt16)(logNum * 32);
                }
                msg.brw = true; //had read all
                logNum = 0;
                return;
            }
            else
            {
                logNum++;
                for (int i = 0; i < LogParamList.Count; i++)
                {
                    param = (Parameter)LogParamList[i];
                    if (param == null) continue;
                    m_parent.Hex2Physical(ref param);
                    param.reglist["Low"].address += 0x20;
                }
            }
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            List<Parameter> parameterList = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.ProParaElement:
                        {
                            if (p == null) break;
                            parameterList.Add(p);
                            break;
                        }
                }
            }

            if (parameterList.Count != 0)
            {
                UpdataProjecInformation(msg);
                Array.Clear(m_parent.m_ProjParamImg, 0, m_parent.m_ProjParamImg.Length);
                for (int i = 0; i < parameterList.Count; i++)
                {
                    param = (Parameter)parameterList[i];
                    if (param == null) continue;
                    m_parent.Physical2Hex(ref param);
                }
                Array.Copy(m_parent.m_ProjParamImg, 0, msg.flashData, ElementDefine.ParameterArea_StartAddress, m_parent.m_ProjParamImg.Length);
                RecoverCalData(ref msg);
                CountCheckSum(ref msg);
            }

            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            bool bSwitch = false;
            TSMBbuffer tsm = null;
            string tmp = string.Empty;
            string path = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            #region Calibration SFL
            if (m_Json_Options.ContainsKey("SFL"))
            {
                if (string.Compare(m_Json_Options["SFL"], "Calibrate") == 0)
                {
                    ret = UnLockI2C();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = infoEraseProtect();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = UnLockParameterArea();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_PARAMETER);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ParameterAreaErase();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    switch (m_Json_Options["TM_CALIBRATION"])
                    {
                        case "Current":
                            curCalib(ref msg);
                            break;
                        case "Temperature":
                            tmpCalib(ref msg);
                            break;
                        case "Voltage":
                            volCalib(ref msg);
                            break;
                    }
                    ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_PARAMETER);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = InforErase();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = LockParameterArea();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ExtendCommand(ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Reload_SWOffset);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReLockI2C();
                }
                if (string.Compare(m_Json_Options["SFL"], "SBS") == 0)
                {
                    if (m_Json_Options["SH256 Switch"] != null)
                        tmp = m_Json_Options["SH256 Switch"];
                    bSwitch = Convert.ToBoolean(tmp);
                    if (!bSwitch) return LibErrorCode.IDS_ERR_SUCCESSFUL;
                    if (m_Json_Options["SH256 Plaintext"] != null)
                        tmp = m_Json_Options["SH256 Plaintext"];
                    byte[] source = Encoding.Default.GetBytes(tmp);//將字串轉為Byte[]
                    tsm = parent.m_SBS_CMD_Dic[ElementDefine.SBS_UNSEAL_CHIP];
                    tsm.bdata = sha256.ComputeHash(source);//進行SHA256加密
                    ret = BlockWrite(ElementDefine.SBS_UNSEAL_CHIP, tsm);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            #endregion
            #region Project
            if (m_Json_Options.ContainsKey("Button"))
            {
                switch (m_Json_Options["Button"])
                {
                    case "NormalDownloadPrj":
                        ret = BootloaderMode();
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = Dummy();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = Handshake();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WriteOnBootLoaderMode(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ResetBootLoader();
                        break;
                    case "FullDownloadPrj":
                        ret = UnLockI2C();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = sysEraseProtect(ref path);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = infoEraseProtect();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = RemapEflash();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = MainBlockErase();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockWrite(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = CheckSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash, 0, (UInt32)msg.flashData.Length, msg.flashData);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        /*
                        ret = CountSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash, ElementDefine.OFFSET_TABLE_VALUE_START, ElementDefine.OFFSET_TABLE_VALUE_END, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Console.WriteLine("Table checksum " + string.Format("{0:x4}", wval));

                        ret = CountSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash, ElementDefine.OFFSET_PARAM_VALUE_START, ElementDefine.OFFSET_PARAM_VALUE_END, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Console.WriteLine("Parameter checksum " + string.Format("{0:x4}", wval));

                        ret = CountSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash, ElementDefine.OFFSET_O2BOOTLOADER_START, ElementDefine.OFFSET_O2BOOTLOADER_END, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Console.WriteLine("Bootload checksum " + string.Format("{0:x4}", wval));

                        ret = CountSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash, ElementDefine.OFFSET_O2CODE_START, ElementDefine.OFFSET_CODE_END, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Console.WriteLine("Code checksum " + string.Format("{0:x4}", wval));*/

                        if (!bFresh & bProtect)
                        {
                            ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }

                        ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WriteSysInfoAreaCleareFlashFreshBit();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = RemapEflash();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ResetMcu();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ReLockI2C();
                        if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) return ElementDefine.IDS_ERR_DEM_DOWNLOAD_SUCCESS;
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
                        ret = UnLockI2C();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = sysEraseProtect(ref path);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = infoEraseProtect();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = RemapEflash();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = MainBlockErase();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockWrite(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = CheckSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash, 0, (UInt32)msg.flashData.Length, msg.flashData);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        if (!bFresh & bProtect)
                        {
                            ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }

                        ret = ReStoreFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = WriteSysInfoAreaCleareFlashFreshBit();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = RemapEflash();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ResetMcu();
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.ENTER_SLEEP_MODE:
                    ret = ExtendCommand(ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Sleep_Mode);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.ENTER_DEEP_SLEEP_MODE:
                    ret = ExtendCommand(ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Deep_Sleep_Mode);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.RESET_CHIP:
                    ret = ExtendCommand(ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Reset_Chip);
                    break;
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

            ret = APBReadWord(0x20, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)SharedFormula.HiByte(wval);
            ival = (int)((SharedFormula.LoByte(wval) & 0x70) >> 4);
            deviceinfor.hwversion = ival;
            switch (ival)
            {
                case 1:
                    shwversion = "A";
                    break;
                case 2:
                    shwversion = "B";
                    break;
                case 3:
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
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            for (int i = 0; i < 15; i++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.GGMEM0 + (i << 8)), demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            }
            /*
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

            for (int i = 0; i < 3; i++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.ChipVer + (i << 8)), demparameterlist.parameterlist);
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

        private UInt32 UnLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnUnLockI2C();
            }
            return ret;
        }

        private UInt32 ReLockI2C()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnLockI2C();
            }
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
            for (int n = 0; n < ElementDefine.RETRY_COUNTER * 2; n++)
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
            Xbcode = (byte)(wval & 0x03F);

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
            return ret;
        }

        private string SysBackUp(ref string filePath)
        {
            FileStream fs = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            filePath = Path.Combine(FolderMap.m_logs_folder, DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + ".o2.sys");
            try
            {
                fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                using (StreamWriter m_stream_writer = new StreamWriter(fs))
                {
                    fs = null;
                    // Code here
                    m_stream_writer.Write(string.Join(",", parent.SysFlash_Buffer));
                    m_stream_writer.Flush();
                }
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }
            return filePath;
        }

        private UInt32 ParameterAreaErase()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = BlockWordWrite(ElementDefine.I2C_Adress_UNLOCK_ERASE, ElementDefine.Unlock_Erase_PSW);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = BlockWordRead(ElementDefine.I2C_Adress_UNLOCK_ERASE, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if ((wval & 0x01) != 0x01) return ElementDefine.IDS_ERR_DEM_UNLOCK_ERASE;

            wval = SharedFormula.MAKEWORD(0xCD, 0x3C);
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
            return ret;
        }

        private UInt32 CheckSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE type, UInt32 startAddr, UInt32 endAddr, byte[] buf)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            startAddr = (UInt16)(startAddr / 4);
            endAddr = (UInt16)(endAddr / 4 - 1);
            switch (type)
            {
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash:
                    wval = (UInt16)startAddr;
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

            ret = BlockWordWrite(ElementDefine.I2C_Adress_End_Address, (UInt16)endAddr);
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
            return ret;
        }

        private UInt32 CountSum(ElementDefine.COBRA_NEWTON_MEMORY_TYPE type, UInt32 startAddr, UInt32 endAddr, ref UInt16 wresult)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            startAddr = (UInt16)(startAddr / 4);
            endAddr = (UInt16)(endAddr / 4 - 1);
            switch (type)
            {
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_MaineFlash:
                    wval = (UInt16)startAddr;
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

            ret = BlockWordWrite(ElementDefine.I2C_Adress_End_Address, (UInt16)endAddr);
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

            ret = BlockWordRead(ElementDefine.I2C_Adress_CRC16_Result, ref wresult);
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

            if ((wval & 0x0100) == 0x0100)
                bval = true;
            else
                bval = false;
            return ret;
        }

        private UInt32 O2bootloaderEraseProtect(ref bool bProtect)
        {
            UInt32 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = Block4BytesRead(ElementDefine.I2C_O2_BLPROT, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            FolderMap.WriteFile(string.Format("Check 0x607E register:{0:x4}", wval));

            //if ((wval & 0x8000) == 0x8000)
            if (wval == 0x89ABCDEF)
                bProtect = true;
            else
                bProtect = false;
            return ret;
        }

        private UInt32 sysEraseProtect(ref string path)
        {
            bool bval = false;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = FlashFresh(ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bFresh = bval; //Record the init flash status.
            if (bval) return LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = O2bootloaderEraseProtect(ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bProtect = bval;

            if (bProtect)
            {
                FolderMap.WriteFile("Begin to operate the System area including erase and backup");
                ret = BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ElementDefine.IDS_ERR_DEM_SYS_BACKUP;//return ret;

                SysBackUp(ref path);
                ret = SysErase();
            }
            return ret;
        }

        private UInt32 infoEraseProtect()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(ElementDefine.I2C_Adress_STATUS, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((wval & 0x0080) == 0x0080) return ElementDefine.IDS_ERR_DEM_INFO_ERASE;
            ret = BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = InforErase();
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

        private UInt32 UnLockParameterArea()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = Block4BytesWrite(0x50FF, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FE, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FD, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FC, 0x00008890);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FB, 0x000091B4);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FA, 0x000035FC);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50F8, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50F7, 0x00003DFF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = RemapEflash();
            return ret;
        }

        private UInt32 LockParameterArea()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = Block4BytesWrite(0x50FF, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FE, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FD, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FC, 0x00008890);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FB, 0x000091B4);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FA, 0x0000B5BC);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50F8, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50F7, 0x00003DFF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = RemapEflash();
            return ret;
        }

        private UInt32 WriteSysInfoAreaCleareFlashFreshBit()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            /*
            ret = Block4BytesWrite(0x607E, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x607D, 0x00008087);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x600F, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/
            ret = Block4BytesWrite(0x50FF, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FE, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FD, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //ret = Block4BytesWrite(0x50FC, 0x00008890);
            ret = Block4BytesWrite(0x50FC, 0x00008090);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FB, 0x000091B4);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50FA, 0x0000B5BC);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50F8, 0x89ABCDEF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = Block4BytesWrite(0x50F7, 0x00003DFF);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        private UInt32 ResetMcu()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            /*
            ret = APBWriteWord(0xD6, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = APBWriteWord(0xD7, 0);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

            ret = Block4BytesWrite(0xC6AA, 0x6318);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = Block4BytesWrite(0xC6AB, 0x0010);
            return ret;
        }

        private UInt32 RemapEflash()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = Block4BytesWrite(0x7001, 0x0000ABCD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int n = 0; n < ElementDefine.RETRY_COUNTER; n++)
            {
                Thread.Sleep(20);
                ret = BlockWordRead(0x7001, ref wval);
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

        private UInt32 ExtendCommand(ElementDefine.COBRA_EXTENDED_COMMAND cmd)
        {
            UInt16 wval = 0;
            TSMBbuffer tsm = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (cmd)
            {
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Dummy:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Sleep_Mode:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Deep_Sleep_Mode:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_CLEAR_ALL_LOG:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Feed_Watchdog:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Reload_SWOffset:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Jump_To_Bootloader:
                case ElementDefine.COBRA_EXTENDED_COMMAND.COBRA_EXTENDED_COMMAND_Reset_Chip:
                    {
                        tsm = parent.m_SBS_CMD_Dic[ElementDefine.SBS_EXTENDEDCOMMAND];
                        tsm.bdata[1] = (byte)cmd;
                        tsm.bdata[0] = 1;
                        ret = BlockWrite(ElementDefine.SBS_EXTENDEDCOMMAND, tsm);
                        break;
                    }
            }
            return ret;
        }

        private UInt32 BackUpFlash(ElementDefine.COBRA_NEWTON_MEMORY_TYPE type)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            //ret = UnLockI2C();
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            switch (type)
            {
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_SyseFlash:
                    Array.Clear(parent.SysFlash_Buffer, 0, parent.SysFlash_Buffer.Length);
                    ret = BlockRead(ElementDefine.SyseFlash_StartAddress, ref parent.SysFlash_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    //0x0F
                    parent.SysFlash_Buffer[0x0F * 4] = 0xFF;
                    parent.SysFlash_Buffer[0x0F * 4 + 1] = 0xFF;
                    parent.SysFlash_Buffer[0x0F * 4 + 2] = 0xFF;
                    parent.SysFlash_Buffer[0x0F * 4 + 3] = 0xFF;
                    //0x7D
                    parent.SysFlash_Buffer[0x7D * 4] = 0xFF;
                    parent.SysFlash_Buffer[0x7D * 4 + 1] = 0xFF;
                    parent.SysFlash_Buffer[0x7D * 4 + 2] = 0xFF;
                    parent.SysFlash_Buffer[0x7D * 4 + 3] = 0xFF;
                    //0x7E
                    parent.SysFlash_Buffer[0x7E * 4] = 0xFF;
                    parent.SysFlash_Buffer[0x7E * 4 + 1] = 0xFF;
                    parent.SysFlash_Buffer[0x7E * 4 + 2] = 0xFF;
                    parent.SysFlash_Buffer[0x7E * 4 + 3] = 0xFF;
                    break;
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_InfoeFlash:
                    Array.Clear(parent.InfoeFlash_Buffer, 0, parent.InfoeFlash_Buffer.Length);
                    ret = BlockRead(ElementDefine.InfoFlash_StartAddress, ref parent.InfoeFlash_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_PARAMETER:
                    Array.Clear(parent.ParameterPage_Buffer, 0, parent.ParameterPage_Buffer.Length);
                    ret = BlockRead((UInt16)ElementDefine.ParameterPage_StartAddress, ref parent.ParameterPage_Buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
            }
            //ret = ReLockI2C();
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
                case ElementDefine.COBRA_NEWTON_MEMORY_TYPE.MEMORY_TYPE_PARAMETER:
                    ret = BlockWrite((UInt16)ElementDefine.ParameterPage_StartAddress, parent.ParameterPage_Buffer);
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

        private void UpdataProjecInformation(TASKMessage msg)
        {
            int index = 0;
            int pointNum = 0;
            Parameter param = null;
            param = GetParameterByGuid(ElementDefine.ProRsense, msg.task_parameterlist.parameterlist);
            if (param != null) parent.Proj_Rsense = param.phydata * 1000;
            else parent.Proj_Rsense = 2500;

            pointNum = msg.flashData[ElementDefine.THM_PointNum_Position];
            if (pointNum == 0) return;
            parent.m_TempVals.Clear();
            parent.m_ResistVals.Clear();
            for (int i = (pointNum - 1); i >= 0; i--)
            {
                parent.m_ResistVals.Add(index, SharedFormula.MAKEWORD(msg.flashData[ElementDefine.THM_Points_Start + i * 4], msg.flashData[ElementDefine.THM_Points_Start + i * 4 + 1]));
                parent.m_TempVals.Add(index, ((Int16)SharedFormula.MAKEWORD(msg.flashData[ElementDefine.THM_Points_Start + (i * 4 + 2)], msg.flashData[ElementDefine.THM_Points_Start + (i * 4 + 3)])) * 0.1);
                index++;
            }
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

            if ((eCount1 < eCount2) && (eCount2 != 0xFFFFFFFF))
            {
                eCount1 = eCount2;
                num = 1;
            }
            if ((eCount1 < eCount3) && (eCount3 != 0xFFFFFFFF))
            {
                eCount1 = eCount3;
                num = 2;
            }

            ret = BlockRead((UInt16)(ElementDefine.LogFlash_StartAddress + num * 1024 / 4), ref parent.logAreaArray);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            byte[] bval = new byte[4];
            List<byte> splitted = new List<byte>();//This list will contain all the splitted arrays.

            for (int i = 0; i < parent.logAreaArray.Length; i = i + 4)
            {
                Array.Copy(parent.logAreaArray, i, bval, 0, 4);
                Array.Reverse(bval);
                for (int j = 0; j < 4; j++)
                    splitted.Add(bval[j]);
            }
            Array.Copy(splitted.ToArray(), parent.logAreaArray, parent.logAreaArray.Length);
            ret = uploadEntireLogArea();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = ReLockI2C();
            return ret;
        }

        private void CountCheckSum(ref TASKMessage msg)
        {
            int tableCheckSum = 0, codeCheckSum = 0, FDTableChecksum = 0, bootloaderCheckSum = 0, parameterCheckSum = 0;
            int tableCheckSumLen = (int)(ElementDefine.OFFSET_TABLE_VALUE_END - ElementDefine.OFFSET_TABLE_VALUE_START); //(0xEFFF - 0xD404 + 1);
            int codeCheckSumLen = (int)(ElementDefine.OFFSET_CODE_END - ElementDefine.OFFSET_O2CODE_START); //(0xD3FF - 0x2000 + 1);
            //int fdCheckSumLen = (0xEF10 - 0xEE44 + 1);
            int bootloaderCheckSumLen = (int)(ElementDefine.OFFSET_O2BOOTLOADER_END - ElementDefine.OFFSET_O2BOOTLOADER_START); //(0x1FFF - 0x0000 + 1);
            int parameterCheckSumLen = (int)(ElementDefine.OFFSET_PARAM_VALUE_END - ElementDefine.OFFSET_PARAM_VALUE_START); //(0xF3FF - 0xF184 + 1);

            //FDTableChecksum = CRC16_CCITT(msg.flashData, 0xEE44, fdCheckSumLen);//CRC16_CCITT(msg.flashData, 0xD404, tableCheckSumLen);
            //msg.flashData[0xEE40] = (byte)FDTableChecksum;
            //msg.flashData[0xEE41] = (byte)(FDTableChecksum >> 8);
            codeCheckSum = CRC16_CCITT(msg.flashData, ElementDefine.OFFSET_O2CODE_START, codeCheckSumLen); //CRC16_CCITT(msg.flashData, 0x2000, codeCheckSumLen); //code checksum必须先算
            msg.flashData[ElementDefine.OFFSET_CODE_CHECKSUM] = (byte)codeCheckSum;
            msg.flashData[ElementDefine.OFFSET_CODE_CHECKSUM + 1] = (byte)(codeCheckSum >> 8);
            bootloaderCheckSum = CRC16_CCITT(msg.flashData, ElementDefine.OFFSET_O2BOOTLOADER_START, bootloaderCheckSumLen);//CRC16_CCITT(msg.flashData, 0xD404, tableCheckSumLen);
            msg.flashData[ElementDefine.OFFSET_O2BL_CHECKSUM] = (byte)bootloaderCheckSum;
            msg.flashData[ElementDefine.OFFSET_O2BL_CHECKSUM + 1] = (byte)(bootloaderCheckSum >> 8);
            parameterCheckSum = CRC16_CCITT(msg.flashData, ElementDefine.OFFSET_PARAM_VALUE_START, parameterCheckSumLen);//CRC16_CCITT(msg.flashData, 0xD404, tableCheckSumLen);
            msg.flashData[ElementDefine.OFFSET_PARAM_CHECKSUM] = (byte)parameterCheckSum;
            msg.flashData[ElementDefine.OFFSET_PARAM_CHECKSUM + 1] = (byte)(parameterCheckSum >> 8);
            tableCheckSum = CRC16_CCITT(msg.flashData, ElementDefine.OFFSET_TABLE_VALUE_START, tableCheckSumLen);//CRC16_CCITT(msg.flashData, 0xD404, tableCheckSumLen);
            msg.flashData[ElementDefine.OFFSET_TABLE_CHECKSUM] = (byte)tableCheckSum;
            msg.flashData[ElementDefine.OFFSET_TABLE_CHECKSUM + 1] = (byte)(tableCheckSum >> 8);
        }

        private void RecoverCalData(ref TASKMessage msg)
        {
            foreach (UInt32 addr in parent.m_SBS2Offset_Mermory_Map.Values)
            {//ElementDefine.ParameterPage_StartAddress * 4 = 0xF000
                msg.flashData[ElementDefine.ParameterPage_StartAddress * 4 + addr + 3] = parent.ParameterPage_Buffer[addr + 3];
                msg.flashData[ElementDefine.ParameterPage_StartAddress * 4 + addr + 2] = parent.ParameterPage_Buffer[addr + 2];
                msg.flashData[ElementDefine.ParameterPage_StartAddress * 4 + addr + 1] = parent.ParameterPage_Buffer[addr + 1];
                msg.flashData[ElementDefine.ParameterPage_StartAddress * 4 + addr + 0] = parent.ParameterPage_Buffer[addr + 0];
            }
        }

        private bool SpecialCmdPEC(byte[] sendbuf, byte[] recv)
        {
            bool bval = false;
            int len = recv[0] + 2;
            byte[] buf = new byte[len];
            Array.Copy(recv, buf, len);
            if (buf[len - 1] != calc_crc_block_read(sendbuf[0], sendbuf[1], buf))
                bval = false;
            else
                bval = true;
            return bval;
        }

        private UInt32 uploadEntireLogArea()
        {
            byte[] bval = new byte[4];
            List<byte> splitted = new List<byte>();//This list will contain all the splitted arrays.
            string fullpath = FolderMap.m_logs_folder + "LogEntireArea " + DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + ".bin";
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockRead((UInt16)(ElementDefine.LogFlash_StartAddress), ref parent.logEntireAreaArray);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < parent.logEntireAreaArray.Length; i = i + 4)
            {
                Array.Copy(parent.logEntireAreaArray, i, bval, 0, 4);
                Array.Reverse(bval);
                for (int j = 0; j < 4; j++)
                    splitted.Add(bval[j]);
            }
            Array.Copy(splitted.ToArray(), parent.logEntireAreaArray, parent.logEntireAreaArray.Length);
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

        #region Calibration
        public UInt32 volCalib(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 imageAddr = 0;
            UInt32 uwval = 0;
            double dtarget_val = 0, doriginal_val =0,  doffset_val = 0;
            double[] doriginal_val_array = new double[ElementDefine.RETRY_COUNTER];

            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (!Double.TryParse(m_Json_Options["Point1"], out dtarget_val))
                return LibErrorCode.IDS_ERR_COM_INVALID_PARAMETER;
            param = msg.task_parameterlist.parameterlist[0];
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = Read(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = ConvertHexToPhysical(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                //doriginal_val += param.phydata;
                doriginal_val_array[i]= param.phydata;
            }
            Array.Sort(doriginal_val_array);
            doriginal_val = doriginal_val_array[(int)(ElementDefine.RETRY_COUNTER/2)];//doriginal_val / ElementDefine.RETRY_COUNTER;
            ret = GetCellOffset();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            switch (param.guid)
            {
                case ElementDefine.CellVoltMV01:
                    //uwval = (UInt32)(doriginal_val - dtarget_val - parent.Cell1_Offset.Cell_2nd_offset);
                    uwval = (UInt32)(doriginal_val - dtarget_val);
                    break;
                case ElementDefine.CellVoltMV02:
                    uwval = (UInt32)(doriginal_val - dtarget_val);
                    break;
            }
            imageAddr = parent.m_SBS2Offset_Mermory_Map[param.guid];
            uwval += SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 3], parent.ParameterPage_Buffer[imageAddr + 2]),
                SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 1], parent.ParameterPage_Buffer[imageAddr + 0]));
            parent.ParameterPage_Buffer[imageAddr + 3] = (byte)uwval;
            parent.ParameterPage_Buffer[imageAddr + 2] = (byte)(uwval >> 8);
            parent.ParameterPage_Buffer[imageAddr + 1] = (byte)(uwval >> 16);
            parent.ParameterPage_Buffer[imageAddr + 0] = (byte)(uwval >> 24);
            m_Json_Options.Add("Offset", string.Format("{0:F4}", (doriginal_val - dtarget_val)));
            msg.sub_task_json = SharedAPI.SerializeDictionaryToJsonString(m_Json_Options);
            return ret;
        }

        public UInt32 GetCellOffset()
        {
            UInt32 dwal = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = Block4BytesRead(0x6009, ref dwal);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.Cell1_Offset.Cell_1nd_hex = (UInt16)(dwal & 0x7F);
            parent.Cell1_Offset.Cell_1nd_offset = (((short)(parent.Cell1_Offset.Cell_1nd_hex << 9)) >> 9) * 2124.0 / 10000.0;
            parent.Cell1_Offset.Cell_2nd_hex = (UInt16)((dwal >> 16) & 0x7F);
            parent.Cell1_Offset.Cell_2nd_offset = (((short)(parent.Cell1_Offset.Cell_2nd_hex << 9)) >> 9) * 2124.0 / 10000.0;
            parent.Cell2_Offset.Cell_1nd_hex = (UInt16)((dwal >> 8) & 0x7F);
            parent.Cell2_Offset.Cell_1nd_offset = (((short)(parent.Cell2_Offset.Cell_1nd_hex << 9)) >> 9) * 2124.0 / 10000.0;
            parent.Cell2_Offset.Cell_2nd_hex = (UInt16)((dwal >> 23) & 0x1FF);
            parent.Cell2_Offset.Cell_2nd_offset = (((short)(parent.Cell2_Offset.Cell_2nd_hex << 7)) >> 7) * 2124.0 / 10000.0;
            return ret;
        }

        public UInt32 curCalib(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 imageAddr = 0;
            UInt32 uwval = 0, lastSlope = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            double dtarget_val = 0, doriginal_val = 0, dlastSlope = 0;
            double dslope_val = 0, doffset_val = 0;
            double[] doriginal_val_array = new double[ElementDefine.RETRY_COUNTER];

            param = msg.task_parameterlist.parameterlist[0];
            imageAddr = parent.m_SBS2Offset_Mermory_Map[param.guid];
            if (!Double.TryParse(m_Json_Options["Point1"], out dtarget_val))
                return LibErrorCode.IDS_ERR_COM_INVALID_PARAMETER;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = Read(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = ConvertHexToPhysical(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                //doriginal_val += msg.task_parameterlist.parameterlist[0].phydata;
                doriginal_val_array[i] = msg.task_parameterlist.parameterlist[0].phydata;
            }
            Array.Sort(doriginal_val_array);
            doriginal_val = doriginal_val_array[(int)(ElementDefine.RETRY_COUNTER / 2)];//doriginal_val = doriginal_val / ElementDefine.RETRY_COUNTER;
            KeyValuePair<double, double> m_cur_cal1 = new KeyValuePair<double, double>(doriginal_val, dtarget_val);
            m_Json_Options.Add("Point1(ADC):", string.Format("{0:F4}", doriginal_val));

            Array.Clear(doriginal_val_array, 0, doriginal_val_array.Length);
            doriginal_val = 0;
            dtarget_val = 0;
            msg.gm.message = "Please supply the second calibrate point!!!";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
            if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

            if (!Double.TryParse(m_Json_Options["Point2"], out dtarget_val))
                return LibErrorCode.IDS_ERR_COM_INVALID_PARAMETER;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = Read(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = ConvertHexToPhysical(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                //doriginal_val += msg.task_parameterlist.parameterlist[0].phydata;
                doriginal_val_array[i] = msg.task_parameterlist.parameterlist[0].phydata;
            }
            Array.Sort(doriginal_val_array);
            doriginal_val = doriginal_val_array[(int)(ElementDefine.RETRY_COUNTER / 2)];//doriginal_val = doriginal_val / ElementDefine.RETRY_COUNTER;
            m_Json_Options.Add("Point2(ADC):", string.Format("{0:F4}", doriginal_val));
            KeyValuePair<double, double> m_cur_cal2 = new KeyValuePair<double, double>(doriginal_val, dtarget_val);
            dslope_val = ((m_cur_cal1.Key - m_cur_cal2.Key) / (m_cur_cal1.Value - m_cur_cal2.Value));
            doffset_val = doriginal_val - dslope_val * dtarget_val;

            lastSlope = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 3], parent.ParameterPage_Buffer[imageAddr + 2]),
                SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 1], parent.ParameterPage_Buffer[imageAddr + 0]));
            if ((lastSlope == 0) | (lastSlope == 0xFFFFFFFF)) dlastSlope = 1;
            else dlastSlope = (double)(lastSlope / 10000.0);
            dslope_val = (dslope_val * dlastSlope);
            uwval = (UInt32)(dslope_val * 10000.0);
            parent.ParameterPage_Buffer[imageAddr + 3] = (byte)uwval;
            parent.ParameterPage_Buffer[imageAddr + 2] = (byte)(uwval >> 8);
            parent.ParameterPage_Buffer[imageAddr + 1] = (byte)(uwval >> 16);
            parent.ParameterPage_Buffer[imageAddr + 0] = (byte)(uwval >> 24);
            m_Json_Options.Add("Slope", string.Format("{0:F4}", dslope_val));

            uwval = (UInt32)(doffset_val * dlastSlope);
            imageAddr += 4;//offset 偏移4个
            uwval += SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 3], parent.ParameterPage_Buffer[imageAddr + 2]),
                SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 1], parent.ParameterPage_Buffer[imageAddr + 0]));
            parent.ParameterPage_Buffer[imageAddr + 3] = (byte)uwval;
            parent.ParameterPage_Buffer[imageAddr + 2] = (byte)(uwval >> 8);
            parent.ParameterPage_Buffer[imageAddr + 1] = (byte)(uwval >> 16);
            parent.ParameterPage_Buffer[imageAddr + 0] = (byte)(uwval >> 24);
            m_Json_Options.Add("Offset", string.Format("{0:F4}", (doffset_val * dlastSlope)));
            msg.sub_task_json = SharedAPI.SerializeDictionaryToJsonString(m_Json_Options);
            return ret;
        }

        public UInt32 tmpCalib(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 imageAddr = 0;
            UInt32 uwval = 0;
            double dtarget_val = 0, doriginal_val = 0, doffset_val = 0;
            double[] doriginal_val_array = new double[ElementDefine.RETRY_COUNTER];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (!Double.TryParse(m_Json_Options["Point1"], out dtarget_val))
                return LibErrorCode.IDS_ERR_COM_INVALID_PARAMETER;
            param = msg.task_parameterlist.parameterlist[0];
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = Read(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = ConvertHexToPhysical(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                //doriginal_val += param.phydata;
                doriginal_val_array[i] = param.phydata;
            }
            Array.Sort(doriginal_val_array);
            doriginal_val = doriginal_val_array[(int)(ElementDefine.RETRY_COUNTER / 2)];//doriginal_val = doriginal_val / ElementDefine.RETRY_COUNTER;
            doffset_val = doriginal_val - dtarget_val;
            uwval = (UInt32)(doffset_val * 10);
            imageAddr = parent.m_SBS2Offset_Mermory_Map[param.guid];
            uwval += SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 3], parent.ParameterPage_Buffer[imageAddr + 2]),
                SharedFormula.MAKEWORD(parent.ParameterPage_Buffer[imageAddr + 1], parent.ParameterPage_Buffer[imageAddr + 0]));
            parent.ParameterPage_Buffer[imageAddr + 3] = (byte)uwval;
            parent.ParameterPage_Buffer[imageAddr + 2] = (byte)(uwval >> 8);
            parent.ParameterPage_Buffer[imageAddr + 1] = (byte)(uwval >> 16);
            parent.ParameterPage_Buffer[imageAddr + 0] = (byte)(uwval >> 24);
            m_Json_Options.Add("Offset", string.Format("{0:F4}", (doriginal_val - dtarget_val)));
            msg.sub_task_json = SharedAPI.SerializeDictionaryToJsonString(m_Json_Options);
            return ret;
        }
        #endregion

        #region Bootloader Mode
        private UInt32 BootloaderMode()
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                sendbuf[0] = 0x16;
                sendbuf[1] = 0xF9;
                sendbuf[2] = 0x01;
                sendbuf[3] = 0x25;
                sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3]);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        Thread.Sleep(100);
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(10);
                }
            }
            return ret;
        }

        private UInt32 Dummy()
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                sendbuf[0] = 0x60;
                sendbuf[1] = 0xFA;
                sendbuf[2] = 0x01;
                sendbuf[3] = 0x70;
                sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3]);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                    {
                        if ((ret = OnCheckError(0)) != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(20);
                }
            }
            return ret;
        }

        private UInt32 Handshake()
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[9];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                sendbuf[0] = 0x60;
                sendbuf[1] = 0xFA;
                sendbuf[2] = 0x05;
                sendbuf[3] = 0x71;
                sendbuf[4] = 0x89;
                sendbuf[5] = 0xAB;
                sendbuf[6] = 0xCD;
                sendbuf[7] = 0xEF;
                sendbuf[8] = crc8_calc(ref sendbuf, 8);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER * 2; i++)
                {
                    if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 7))
                    {
                        for (i = 0; i < ElementDefine.RETRY_COUNTER * 2; i++)
                        {
                            Thread.Sleep(100);
                            if ((ret = OnCheckError(1)) != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                            return ret;
                        }
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(300);
                }
            }
            return ret;
        }

        private UInt32 WriteOnBootLoaderMode(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)(ElementDefine.BOOTLOAD_OPERATION_BYTES + 7); //addr,comd,offset,data, pec
            byte[] receivebuf = new byte[3];
            byte[] sendbuf = new byte[7 + ElementDefine.BOOTLOAD_OPERATION_BYTES];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                sendbuf[0] = 0x60;
                sendbuf[1] = 0xFA;
                sendbuf[2] = ElementDefine.BOOTLOAD_OPERATION_BYTES + 3;// 0x13;
                sendbuf[3] = 0x72;
                for (UInt32 fAddr = ElementDefine.USER_CODE_START_ADDRE; fAddr < (msg.flashData.Length - 3 * 0x400); fAddr += ElementDefine.BOOTLOAD_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    sendbuf[4] = SharedFormula.HiByte((UInt16)fAddr);
                    sendbuf[5] = SharedFormula.LoByte((UInt16)fAddr);
                    /*
                    Array.Clear(m_tempArray, 0, m_tempArray.Length);
                    Array.Copy(buffer, fAddr, m_tempArray, 0, ElementDefine.BLOCK_OPERATION_BYTES);
                    reverseArrayBySize(ref m_tempArray, 4);//分割后的子块数组
                    Array.Copy(m_tempArray, 0, sendbuf, 3, m_tempArray.Length);*/
                    Array.Copy(msg.flashData, fAddr, sendbuf, 6, ElementDefine.BOOTLOAD_OPERATION_BYTES);
                    sendbuf[6 + ElementDefine.BOOTLOAD_OPERATION_BYTES] = crc8_calc(ref sendbuf, (UInt16)(DataInLen - 1));
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen - 2)))
                        {
                            if ((ret = OnCheckError(2)) != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(100);
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                }
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    sendbuf[0] = 0x60;
                    sendbuf[1] = 0xFA;
                    sendbuf[2] = ElementDefine.BOOTLOAD_OPERATION_BYTES + 3; //0x13;
                    sendbuf[3] = 0x72;
                    for (int j = 0; j < sendbuf[2] - 1; j++)
                        sendbuf[j + 4] = 0xFF;
                    sendbuf[6 + ElementDefine.BOOTLOAD_OPERATION_BYTES] = crc8_calc(ref sendbuf, (UInt16)(DataInLen - 1));
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen - 2)))
                        {
                            if ((ret = OnCheckError(2)) != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }
                }
                return ret;
            }
        }

        private UInt32 ResetBootLoader()
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                sendbuf[0] = 0x60;
                sendbuf[1] = 0xFA;
                sendbuf[2] = 0x01;
                sendbuf[3] = 0x73;
                sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3]);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(10);
                }
            }
            return ret;
        }

        private UInt32 OnCheckError(byte bindex)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = 0x60;
            sendbuf[1] = 0xFA;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER * 2; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    if (receivebuf[1] != (0x80 | bindex))
                    {
                        ret = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + receivebuf[1];
                        Thread.Sleep(10);
                        continue;
                    }
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }
        #endregion
    }
}