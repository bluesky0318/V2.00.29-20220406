//#define DEBUG_LOG
//#define DATA_PACKAGE_LEN 32

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;


namespace Cobra.OZ8513
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
            Init_log();
        }

        #region Command 操作常量定义
        private const int RETRY_COUNTER = 5;
        #region OZ8513 flash register
        private const Byte FLASH_I2C_COMMAND_REG = 0x01;
        private const Byte FLASH_COMMAND_NOT_THING = 0x00;
        private const Byte FLASH_COMMAND_WRITE_MAIN = 0x01;
        private const Byte FLASH_COMMAND_WRITE_INFO = 0x02;
        private const Byte FLASH_COMMAND_WRITE_FUSE = 0x03;
        private const Byte FLASH_COMMAND_ERASE_INFO_0 = 0x08;
        private const Byte FLASH_COMMAND_ERASE_INFO_1 = 0x09;
        private const Byte FLASH_COMMAND_ERASE_FUSE = 0x10;
        private const Byte FLASH_COMMAND_ERASE_MAIN = 0x20;
        private const Byte FLASH_COMMAND_DONE = 0x5A;
        private const Byte FLASH_CHECKSUM_TEST = 0x80;

        private const Byte FLASH_I2C_COMMAND_CHECKSUM = 0x02;

        private const Byte FLASH_I2C_COMMAND_STATUS = 0x03;
        private const Byte STATUS_BUSY = 0x01;
        private const Byte STATUS_ERASE_MAIN_DONE = 0x08;
        private const Byte STATUS_ERASE_INFO_DONE = 0x04;

        private const Byte FLASH_I2C_COMMAND_DUMP_FLASH_HIGH = 0x05;
        private const Byte FLASH_I2C_COMMAND_DUMP_FLASH_LOW = 0x06;

        private const Byte FLASH_I2C_COMMAND_TEST_MODE = 0x07;
        private const Byte TEST_MODE_NORMAL = 0x00;

        private const Byte FLASH_I2C_COMMAND_DUMP_INFO_FLASH_HIGH = 0x08;


        private const Byte FLASH_I2C_COMMAND_INFO0_ENB = 0x0F;
        private const Byte ENABLE_INFO0_PATTEN = 0x73;

        private const Byte FLASH_I2C_COMMAND_CPU_HOLD = 0x17;
        private const Byte FLAG_CPU_HOLD = 0x01;

        private const Byte FLASH_I2C_COMMAND_TM_SET = 0x94;
        private const Byte FLAG_TM_SET = 0x01;
        #endregion


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
        public FileStream m_RecordFile;
        public StreamWriter m_stream_writer;
        public string m_logs_folder = "";
        public string data_log = "";

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        protected UInt32 ReadBlock(byte[] reg_list, Byte length_to_slave, Byte[] databuffer,ref Byte length_to_read)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                //ret = OnReadWord(reg, ref pval);
                ret = OnReadBlock(reg_list, length_to_slave, databuffer, ref length_to_read);
            }
            return ret;
        }
        protected UInt32 ReadBlock_SMB(byte I2CAddress,byte[] reg_list, Byte length_to_slave, Byte[] databuffer, ref Byte length_to_read)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                //ret = OnReadWord(reg, ref pval);

                ret = OnReadBlock_SMB(I2CAddress,reg_list, length_to_slave, databuffer, ref length_to_read);
            }
            return ret;
        }


        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }
        protected UInt32 ReadByte(byte reg, ref Byte pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref pval);
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
        protected UInt32 WriteByte(byte reg,  Byte val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }
        protected UInt32 WriteBlock(byte reg, Byte length,  Byte [] databuffer)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {

                ret = OnWriteBlock(reg, length,databuffer);
            }

            return ret;
        }

        protected UInt32 WriteBlock_SMB(byte Address,byte reg, Byte length,  Byte [] databuffer)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                //ret = OnWriteBlock(reg, length,databuffer);
                //ret = OnWriteBlock(reg, length,databuffer);
                ret = OnWriteBlock_SMB(Address,reg, length,databuffer);
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

        protected UInt32 OnReadBlock(byte[] reg_list, Byte length_to_slave, Byte[] databuffer,ref Byte length_to_read)
       // protected UInt32 OnReadBlock(byte reg, ref UInt16 pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[64];
            byte[] receivebuf = new byte[64];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //sendbuf[0] = (byte)((I2CBusOptions)parent.m_busoption).TmpDeviceAddress.value;
            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            data_log = sendbuf[0].ToString("X2") + " ";
            for (bCrc = 0; bCrc < length_to_slave; bCrc++)
            {
                sendbuf[1+bCrc] = reg_list[bCrc];
                data_log += sendbuf[1 + bCrc].ToString("X2") + " ";
            }
            data_log += "R ";
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                //if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3)) We may send to length_to_slave
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, length_to_read, length_to_slave))
                {

                    for (bCrc = 0; bCrc < length_to_read; bCrc++)
                    databuffer[bCrc] = receivebuf[bCrc];


                    for (bCrc = 0; bCrc < length_to_read; bCrc++)
                    {
                        data_log += " ";
                        data_log += databuffer[bCrc].ToString("X2");// = databuffer[i];
                    }

                    data_log += "\r\n";
                    WriteFile(data_log);
                    data_log = "";

                    /*
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
                     */
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        protected UInt32 OnReadBlock(byte I2CAddress,byte[] reg_list, Byte length_to_slave, Byte[] databuffer, ref Byte length_to_read)
        // protected UInt32 OnReadBlock(byte reg, ref UInt16 pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[64];
            byte[] receivebuf = new byte[64];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = (byte)I2CAddress;
            data_log = sendbuf[0].ToString("X2") + " ";
            for (bCrc = 0; bCrc < length_to_slave; bCrc++)
            {
                sendbuf[1 + bCrc] = reg_list[bCrc];
                data_log += reg_list[bCrc].ToString("X2") + " ";
            }
            //data_log = sendbuf[0].ToString("X2") + " ";// +sendbuf[1].ToString("X2") + " " + (sendbuf[0] + 1).ToString("X2") + " ";// +" " + sendbuf[2];
            data_log += "R ";
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                //if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3)) We may send to length_to_slave
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, length_to_read, length_to_slave))
                {

                    for (bCrc = 0; bCrc < length_to_read; bCrc++)
                        databuffer[bCrc] = receivebuf[bCrc];

                    /*
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
                     */
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    //data_log += pval.ToString("X2");

                    for (bCrc = 0; bCrc < length_to_read; bCrc++)
                    {
                        data_log += " ";
                        data_log += databuffer[bCrc].ToString("X2");// = databuffer[i];
                    }
                    
                    data_log += "\r\n";
                    WriteFile(data_log);
                    data_log = "";


                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnReadBlock_SMB(byte I2CAddress,byte[] reg_list, Byte length_to_slave, Byte[] databuffer, ref Byte length_to_read)
        // protected UInt32 OnReadBlock(byte reg, ref UInt16 pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[64];
            byte[] receivebuf = new byte[64];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            Byte bLentgh_to_read = (byte)(length_to_read+1);

            ret = OnReadBlock(I2CAddress, reg_list, length_to_slave, databuffer, ref bLentgh_to_read);
            return ret;
        }



        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            byte   bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //sendbuf[0] = (byte)((I2CBusOptions)parent.m_busoption).TmpDeviceAddress.value;
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

        protected UInt32 OnReadByte(byte reg, ref Byte pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;


            //sendbuf[0] = (byte)((I2CBusOptions)parent.m_busoption).TmpDeviceAddress.value;
            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            data_log = sendbuf[0].ToString("X2") + " " + sendbuf[1].ToString("X2") + " " + "R " + " ";// +" " + sendbuf[2];
#if DEBUG_LOG
            //reg_addr.ToString("X2")
#else

            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    pval = receivebuf[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    /*
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
                     */ 
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
#endif

            data_log += pval.ToString("X2");
/*
            for (int i = 0; i < length; i++)
            {
                data_log += " ";
                data_log += sendbuf[2 + i].ToString("X2");// = databuffer[i];
            }
 */ 
            data_log += "\r\n";
            WriteFile(data_log);
            data_log = "";


            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        public void Init_log()
        {
                 if (Directory.Exists(FolderMap.m_logs_folder + "MerlionPD" +"\\") == false)
                 {
                     Directory.CreateDirectory(FolderMap.m_logs_folder + "MerlionPD" + "\\");
                 }
                 string path = FolderMap.m_logs_folder + "MerlionPD" + "\\" + "I2C Log" + DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + ".log";
                 m_RecordFile = new FileStream(path, FileMode.OpenOrCreate);
                 m_stream_writer = new StreamWriter(m_RecordFile);
            data_log = "";
            // string decription_str = "";

        }
        public void WriteFile(string info)
        {
            // info += ": " + DateTime.Now.ToString() + ":" + DateTime.Now.Millisecond.ToString() + "\r\n";
            m_stream_writer.Write(info);
            m_stream_writer.Flush();
        }
        public void Debug_Data_log(ulong ulID, byte func, UInt64 task_num)
        {
            
            WriteFile(data_log);
            data_log = "";

        }


        protected UInt32 OnWriteBlock(byte reg, byte length, byte [] databuffer)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[64];
            byte[] receivebuf = new byte[64];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //sendbuf[0] = (byte)((I2CBusOptions)parent.m_busoption).TmpDeviceAddress.value;
            
            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                //sendbuf[0] = 0x10;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            //sendbuf[0] = 0x10;
            sendbuf[1] = reg;

            for (int i = 0; i < length; i++)
            {
                sendbuf[2+i] = databuffer[i]; 
            }


            data_log = sendbuf[0].ToString("X2") + " WRITE(" +"0x"+ sendbuf[1].ToString("X2");// +" " + sendbuf[2];
            for (int i = 0; i < length; i++)
            {
                data_log += ",0x";
                data_log += sendbuf[2 + i].ToString("X2");// = databuffer[i];
            }
            data_log += ")\r\n";
            WriteFile(data_log);
            data_log = "";

#if DEBUG_LOG
            //reg_addr.ToString("X2");
#else

           // sendbuf[2] = SharedFormula.HiByte(val);
           // sendbuf[3] = SharedFormula.LoByte(val);
           // sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            
            for (int i = 0; i < RETRY_COUNTER; i++)
            {

                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, length))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
#endif

            m_Interface.GetLastErrorCode(ref ret);

            return ret;
        }


        protected UInt32 OnWriteBlock_SMB(byte I2CAddress, byte reg, byte length, byte[] databuffer)
        {
           // UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[64];
           // byte[] receivebuf = new byte[64];
            //UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = length;
            for (int i = 0; i < length; i++)
            {
                sendbuf[1 + i] = databuffer[i];
            }


            // In smbus, we have to send length as 1st byte.So, attach length into 1st data buffer
            return OnWriteBlock(I2CAddress, reg, (byte)(length + 1), sendbuf);

        }


        protected UInt32 OnWriteBlock(byte I2CAddress,byte reg, byte length, byte[] databuffer)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[64];
            byte[] receivebuf = new byte[64];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = I2CAddress;
            sendbuf[1] = reg;

            for (int i = 0; i < length; i++)
            {
                sendbuf[2 + i] = databuffer[i];
            }


            data_log = sendbuf[0].ToString("X2") + " " + sendbuf[1].ToString("X2");// +" " + sendbuf[2];
            for (int i = 0; i < length; i++)
            {
                data_log += " ";
                data_log += sendbuf[2 + i].ToString("X2");// = databuffer[i];
            }
            data_log += "\r\n";
            WriteFile(data_log);
            data_log = "";

#if DEBUG_LOG
            //reg_addr.ToString("X2");
#else

            // sendbuf[2] = SharedFormula.HiByte(val);
            // sendbuf[3] = SharedFormula.LoByte(val);
            // sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, length))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
#endif
            m_Interface.GetLastErrorCode(ref ret);

            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

           // sendbuf[0] = (byte)((I2CBusOptions)parent.m_busoption).TmpDeviceAddress.value;
            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = val;
           // sendbuf[3] = SharedFormula.LoByte(val);
           // sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            data_log = sendbuf[0].ToString("X2") + " " + sendbuf[1].ToString("X2") + " " + sendbuf[2].ToString("X2");
            data_log += "\r\n";
            WriteFile(data_log);
            data_log = "";

#if DEBUG_LOG
            //reg_addr.ToString("X2");
#else

            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                data_log = sendbuf[0].ToString("X2") + " " + sendbuf[1].ToString("X2") + " " + sendbuf[2].ToString("X2");
                data_log += "\r\n";
                WriteFile(data_log);
                data_log = "";
                //m_dbg_log_module.Debug_Data_log_i2c_access(ulID, bFunc, 4, sendbuf[1], val, sendbuf[3]);
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
#endif
            m_Interface.GetLastErrorCode(ref ret);

            return ret;
        }



        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //sendbuf[0] = (byte)((I2CBusOptions)parent.m_busoption).TmpDeviceAddress.value;
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
      //      sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
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
             //   ret = OnWorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_READOUT);
              //  if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnYFLASHReadWord(reg, ref pval);
            }
            return ret;
        }
        #endregion

        #region YFLASH寄存器子级操作
        protected UInt32 OnWorkMode()
        {
            
            return 0;
        }

        protected UInt32 OnATELCK()
        {
            return 0;
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
            
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            
            return ret;
        }

        protected UInt32 OnWaitWorkModeCompleted()
        {
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        protected UInt32 OnWaitFirmwareCompleted(Byte bWaitReg, Byte bPatten)
        {
            byte bdata = 0;
            // UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                ret = OnReadByte(bWaitReg, ref bdata);
                // ret = OnReadWord(YFLASH_WORKMODE_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
#if DEBUG_LOG
                bdata = 0x6B;
#endif
                // bdata = SharedFormula.LoByte(wdata);
                if ((bdata) == bPatten)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }


            // exceed max waiting time
            //return LibErrorCode.IDS_ERR_TRIMMING_TIMEOUT;IDS_ERR_SECTION_MERLIONPD_FLASH
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }



        protected UInt32 OnWaitTrimCompleted(Byte bWaitReg)
        {
            byte bdata = 0;
           // UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                ret = OnReadByte(bWaitReg, ref bdata);
               // ret = OnReadWord(YFLASH_WORKMODE_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

               // bdata = SharedFormula.LoByte(wdata);
                if ((bdata) == 0)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }


            // exceed max waiting time
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
            //return LibErrorCode.IDS_ERR_TRIMMING_TIMEOUT;
        }

        protected UInt32 OnWaitMapCompleted()
        {
            return  LibErrorCode.IDS_ERR_SUCCESSFUL;
 
           
        }
        #endregion
        #endregion

        #region YFLASH功能操作
        #region YFLASH功能父级操作
        protected UInt32 WorkMode()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
             //   ret = OnWorkMode(wkm);
            }
            return ret;
        }

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
            return 0;
        }

        public UInt32 EpBlockRead()
        {
            return 0;
            
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            //return 0;
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            Byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.PARAElement:
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
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (YFLASHReglist.Count != 0)
            {
               // ret = WorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_READOUT);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in YFLASHReglist)
                {
                    //ret = YFLASHReadWord(badd, ref wdata);
                    ret = ReadWord(badd, ref wdata);
                    parent.m_TrimRegImg[badd].err = ret;
                    parent.m_TrimRegImg[badd].val = wdata;
                }

                //ret = WorkMode(ElementDefine.COBRA_AZALEA5_WKM.YFLASH_WORKMODE_NORMAL);
                //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }



            //Check OPreglist count

            if (OpReglist.Count != 0)
            {

                foreach (byte badd in OpReglist)
                {
                    //ret = ReadWord(badd, ref wdata);
                    ret = ReadByte(badd, ref bdata);
                    //Thread.Sleep(10);
                    parent.m_OpRegImg[badd].err = ret;
                    parent.m_OpRegImg[badd].val = bdata;
                }
                if (OpReglist.Count > 1)// Not only access 1 operation register
                {
                    //Change after 2.00.09
                    if (msg.sub_task != 0)
                    {
                        if ((msg.sub_task == 0x22) || (msg.sub_task == 0x23) || (msg.sub_task == 0x43))
                        ret = Command(ref msg);
                    }
                }
            }
            return ret;
        }

        public UInt32 Read_INFO_Flash(ushort flash_Block,UInt16 length, Byte[] bDatabuffer)
        {
            //UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Byte[] datatmp = new Byte[64];
            Byte[] datatmp_read = new Byte[64];

            Byte data_length = 32;
            Byte data_length_read = (Byte)(data_length + 2);
            Byte btmp = 0;
            ushort usaddress = 0;


            
            //ret = ReadByte(0x03, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // Set test mode Flash
            //ret = WriteByte(0x07, 0x00);
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, TEST_MODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
  //          ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, FLAG_CPU_HOLD);
  //          if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //for (ushort address = 0; address < 0x100; address += data_length)
            for (ushort address = 0; address < length; address += data_length)
            {
                //ret = WriteByte(0x07, 0x00);
                // Append flash command and data address to it.
               // datatmp[0] = 0x08;
                datatmp[0] = FLASH_I2C_COMMAND_DUMP_INFO_FLASH_HIGH;
                
                usaddress = (ushort)(1024 * flash_Block);
                usaddress += address;
                datatmp[1] = SharedFormula.HiByte(usaddress);
                //datatmp[1] = 0x55;// SharedFormula.HiByte(address);
                datatmp[2] = SharedFormula.LoByte(usaddress);

                // datatmp[1] = 0;
                // datatmp[2] = 0;

                //    while (true)
                {
                    ret = OnReadBlock(datatmp, 3, datatmp_read, ref data_length_read);
                    // if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                }
                // ret = ReadBlock(0x08, (byte)(data_length + 3), datatmp)
                // ret = WriteBlock(0x01, (byte)(data_length + 3), datatmp);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                for (int offset = 0; offset < data_length; offset++)
                {
                    bDatabuffer[address + offset] = datatmp_read[offset + 2];
                }
                
                //Read checksum
                Byte bChecksum = 0;
                //ret = ReadByte(0x02, ref bChecksum);
                ret = ReadByte(FLASH_I2C_COMMAND_CHECKSUM, ref bChecksum);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                // We may do somecheck here.
                Thread.Sleep(1);
            }
         //   ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, 0);
          //  if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //ret = WriteByte(0x01, 0x5A);
            ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_DONE);
            
            return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;

        }



        // 2016.01.25. This function is for MerlionPD flash read with databuffer.
        
        public UInt32 Read_Flash(ref TASKMessage msg ,UInt16 length, Byte[] bDatabuffer)
        {
            //UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Byte[] datatmp = new Byte[64];
            Byte[] datatmp_read = new Byte[64];

            Byte data_length = 32;
            Byte data_length_read = (Byte)(data_length + 2);
            Byte btmp = 0;
            Byte bdatachecksum = 0;

            //ret = ReadByte(0x02, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_CHECKSUM, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // Set test mode Flash
            //ret = WriteByte(0x07, 0x00);
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, TEST_MODE_NORMAL);

            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
     //       Thread.Sleep(11);
    //        ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, FLAG_CPU_HOLD);
    //        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //for (ushort address = 0; address < 0x100; address += data_length)
            for (ushort address = 0; address < length; address += data_length)
            {
                //ret = WriteByte(0x07, 0x00);
                // Append flash command and data address to it.
                //datatmp[0] = 0x05;
                datatmp[0] = FLASH_I2C_COMMAND_DUMP_FLASH_HIGH;
                datatmp[1] = SharedFormula.HiByte(address);
                //datatmp[1] = 0x55;// SharedFormula.HiByte(address);
                datatmp[2] = SharedFormula.LoByte(address);

                msg.gm.message = "Read Flash";
                msg.percent = ((address ) * 100) / 0x8000;
                msg.bgworker.ReportProgress(msg.percent, msg.gm.message);

                {
                    ret = OnReadBlock(datatmp, 3, datatmp_read, ref data_length_read);
                    // if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                }

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                bdatachecksum = 0;
                for (int offset = 0; offset < data_length; offset++)
                {
                    bDatabuffer[address + offset] = datatmp_read[offset + 2];
                    bdatachecksum += datatmp_read[offset + 2]; ;
                }

                //Read checksum
                Byte bChecksum = 0;
                //ret = ReadByte(0x02, ref bChecksum);
                ret = ReadByte(FLASH_I2C_COMMAND_CHECKSUM, ref bChecksum);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                // We may do somecheck here.
                Thread.Sleep(1);
            }
            // After read, use reset command to restart CPU
            //ret = WriteByte(0x01, 0x5A);
          //  ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, 0);
          //  if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
         //   ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_DONE);

         //   return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_DONE);
           // ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_DONE);

            return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;

        }
        public UInt32 Erase_INFO_Flash(byte Block)
        {
            Byte btmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            //m_Interface.SetO2I2CDelayTime();
            /*
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, 0x06);
            for (ret = 0; ret < 0x80; ret++)
            {
                Thread.Sleep(100);
                WriteByte(0x84, (byte)ret);
                Thread.Sleep(100);
            }
            return 0;
            */
            //ret = ReadByte(FLASH_I2C_COMMAND_TM_SET, ref btmp);

            //ret = ReadByte(0x03, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Add for C0 version chip
            //ret = WriteByte(0x17, 0x01);
            ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, FLAG_CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;



            // Erase INFO Flash
            //ret = WriteByte(0x07, 0x00);
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, TEST_MODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //        Thread.Sleep(1);

            if (Block == 0)
            {
                //ret = WriteByte(0x0F, 0x73);
                ret = WriteByte(FLASH_I2C_COMMAND_INFO0_ENB, ENABLE_INFO0_PATTEN);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                //ret = WriteByte(0x01, 0x08);
                ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_ERASE_INFO_0);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

  
            }
            else if (Block == 1)
            {
                //ret = WriteByte(0x01, 0x09);
                 ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_ERASE_INFO_1);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            }
            // According to flash spec., we should have 1200ms to let erase complete its procedure.
            int i = 0;
            for (i = 0; i < 200; i++)
            {
                //ret = ReadByte(0x03, ref btmp);
                ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((btmp & STATUS_ERASE_INFO_DONE) != 00)
                //if ((btmp & 0x04) != 00)
                    break;
                Thread.Sleep(10);
            }
            if (i == 200)
            {
                ret = LibErrorCode.IDS_ERR_ERASE_INFO_FLASH_TIMEOUT;
                return ret;
            }
            //ret = ReadByte(0x03, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);

            return ret;
        }

        protected UInt32 Trimmingdatacheck(Byte[] databuffer)
        {
            //type=1;trim

            UInt32 ret = 0;
            return ret;
        }

        protected UInt32 ImagefromParameterList(Byte[] databuffer)
        {
            //type=1;parameter

            UInt32 ret = 0;
            Byte bchecksum = 0;
            UInt32 checksum_error = LibErrorCode.IDS_ERR_WRITE_INFO_FLASH_CHECKSUM; ;
            UInt32 image_error = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
            return ret;
        }
        protected UInt32 Parametercheck( Byte[] databuffer)
        {
            //type=1;parameter
            
            UInt32 ret = 0;
            Byte bchecksum = 0;
            UInt32 checksum_error = LibErrorCode.IDS_ERR_WRITE_INFO_FLASH_CHECKSUM; ;
            UInt32 image_error = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;



            //System parameter 0x00~0x3F
            if  (databuffer[0x3F] == 0x55)
            {
                bchecksum = 0;
                for (ret = 0; ret < 0x3E; ret++)
                    bchecksum += databuffer[ret];

                if (bchecksum != databuffer[0x3E])
                    return checksum_error;//checksum error
            }
            else
                return image_error;//Pattern, header, fail//invaid image

            //PD parameter 0x80~0x16A
            if  (databuffer[0x16a] == 0x55)
            {
                bchecksum = 0;
                for (ret = 0x80; ret < 0x169; ret++)
                    bchecksum += databuffer[ret];

                if (bchecksum != databuffer[0x169])
                    return checksum_error;//checksum error
            }
            else
                return image_error;//Pattern, header, fail//invaid image

            //PD country code parameter 0x1F0~0x2F7
            if (databuffer[0x2F7] == 0x55)
            {
                bchecksum = 0;
                for (ret = 0x1F0; ret < 0x2F6; ret++)
                    bchecksum += databuffer[ret];

                if (bchecksum != databuffer[0x2F6])
                    return checksum_error;//checksum error
            }
            else
                return image_error;//Pattern, header, fail//invaid image


            //PD country code parameter 0x2F8~0x3FF
            if (databuffer[0x3FF] == 0x55)
            {
                bchecksum = 0;
                for (ret = 0x2F8; ret < 0x3eF; ret++)
                    bchecksum += databuffer[ret];

                if (bchecksum != databuffer[0x3FE])
                    return checksum_error;//checksum error
            }
            else
                return image_error;//Patter, header, fail//invaid image





            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        protected UInt32 ImageBuffercheck(Byte type, Byte[] databuffer)
        {
            //type=1;parameter
            //type=2;trimming
            UInt32 ret = 0;
            if(type == 1)
                ret = Parametercheck(databuffer);

            if (type == 0)
                ret =  Trimmingdatacheck(databuffer);


            return ret;
        }
        public UInt32 Erase_Mian_Flash()
        {
            Byte btmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            //m_Interface.SetO2I2CDelayTime();

            //ret = ReadByte(0x03, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Add for C0 version chip
            //ret = WriteByte(0x17, 0x01);
            ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD,FLAG_CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // Erase Flash
            //ret = WriteByte(0x07, 0x00);internal const Byte  = 0x07;
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, TEST_MODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //        Thread.Sleep(1);

            //ret = WriteByte(0x01, 0x20);
            ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_ERASE_MAIN);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            // According to flash spec., we should have 1200ms to let erase complete its procedure.
            int i = 0;
            for (i = 0; i < 200; i++)
            {
                ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
                //ret = ReadByte(0x03, ref btmp);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((btmp & STATUS_ERASE_MAIN_DONE) != 00)
                //if ((btmp & 0x08) != 00)
                    break;
                Thread.Sleep(10);
            }
            if (i == 200)
            {
                ret = LibErrorCode.IDS_ERR_ERASE_MAIN_FLASH_TIMEOUT;
                return ret;
            }


            return ret;
        }

        public UInt32 Write_INFO_Flash(ushort Block,UInt16 length, Byte[] bDatabuffer)
        {
            //UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Byte[] datatmp = new Byte[64];

            Byte data_length = 16;
            Byte btmp = 0;
            Byte bDataWriteChecksum = 0;
            ushort usaddress = 0;
            //m_Interface.SetO2I2CDelayTime(500);
            // In default, the byte between I2C adapter is about 46us
            //m_Interface.SetO2I2CDelayTime();

            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
            //ret = ReadByte(0x03, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //   ret = Erase_Mian_Flash();

            //return ret;     

            //ret = WriteByte(0x07, 0x00);
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, TEST_MODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Test for check flash status
            //ret = ReadByte(0x03, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (Block == 0)
            {
                //ret = WriteByte(0x0F, 0x73);
                ret = WriteByte(FLASH_I2C_COMMAND_INFO0_ENB, ENABLE_INFO0_PATTEN);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            //SetO2I2CDelayTime();
            //for (ushort address = 0; address < 0x400; address += data_length)
            for (ushort address = 0; address < length; address += data_length)
            {
                //Add for C0 version chip
                //ret = WriteByte(0x17, 0x01);
                ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, FLAG_CPU_HOLD);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;


                // Append flash command and data address to it.
               // datatmp[0] = 0x02;
                datatmp[0] = FLASH_COMMAND_WRITE_INFO;
                usaddress = (ushort)(1024 * Block);
                usaddress += address;
                datatmp[1] = SharedFormula.HiByte(usaddress);
                datatmp[2] = SharedFormula.LoByte(usaddress);
                bDataWriteChecksum = 0;
                for (int offset = 0; offset < data_length; offset++)
                {
                    datatmp[offset + 3] = bDatabuffer[address + offset];
                    bDataWriteChecksum += bDatabuffer[address + offset];
                }
                /*
                 ret = WriteByte(0x07, 0x00);
                 if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                     return ret;
                */
                /*
                 while (true)
                 {

                     ret = WriteByte(0x07, 0x00);
                     if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                         break;

                 }*/
                //ret = WriteBlock(0x01, (byte)(data_length + 3), datatmp);
                ret = WriteBlock(FLASH_I2C_COMMAND_REG, (byte)(data_length + 3), datatmp);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                Byte bChipChecksum = 0;
                // return ret;
                //Read checksum
                if (data_length != 1)
                {
                    //     Byte bChecksum = 0;
                    //ret = ReadByte(0x02, ref bChipChecksum);
                    ret = ReadByte(FLASH_I2C_COMMAND_CHECKSUM, ref bChipChecksum);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    if (bDataWriteChecksum != bChipChecksum)
                    {
                        ret = LibErrorCode.IDS_ERR_WRITE_INFO_FLASH_CHECKSUM;
                        return ret;
                    }
                }


            }
            // Finish
//            ret = WriteByte(0x01, 0x5A);
            ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_DONE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        
        // 2016.01.15. This function is for MerlionPD flash write with databuffer.

        public UInt32 Write_Flash(ref TASKMessage msg,UInt16 length, Byte[] bDatabuffer)
        {
            //UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Byte[] datatmp = new Byte[64];

            Byte data_length = 32;
            Byte btmp = 0;
            Byte bDataWriteChecksum = 0;

            //m_Interface.SetO2I2CDelayTime(500);
            // In default, the byte between I2C adapter is about 46us
            //m_Interface.SetO2I2CDelayTime();

            //   ret = Erase_Mian_Flash();

            //return ret;     

            //ret = WriteByte(0x07, 0x00);
            ret = WriteByte(FLASH_I2C_COMMAND_TEST_MODE, TEST_MODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Test for check flash status
            //ret = ReadByte(0x03, ref btmp);
            ret = ReadByte(FLASH_I2C_COMMAND_STATUS, ref btmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, FLAG_CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;


            //SetO2I2CDelayTime();
            //for (ushort address = 0; address < 0x400; address += data_length)
            double timestamp0 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            string s;

            for (ushort block = 0; block < 32; block++)
            {
                data_length = 54;
                //data_length = 16;
                for (ushort address = 0; address < 1024; )//Use 1K bytes based write.
                {
                    //ret = WriteByte(0x17, 0x01);
                    ret = WriteByte(FLASH_I2C_COMMAND_CPU_HOLD, FLAG_CPU_HOLD);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((address + data_length) > 1024)
                        data_length = (byte)(1024 - address);
                    // else

                    double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                    // Append flash command and data address to it.
                   // datatmp[0] = 0x01;
                    datatmp[0] = FLASH_COMMAND_WRITE_MAIN;
                    datatmp[1] = SharedFormula.HiByte((ushort)(address + block * 1024));
                    datatmp[2] = SharedFormula.LoByte((ushort)(address + block * 1024));
                    bDataWriteChecksum = 0;

                    //for testing
                    //ret = WriteByte(0x10, datatmp[1]);
                    //ret = WriteByte(0x11, datatmp[2]);

                    for (int offset = 0; offset < data_length; offset++)
                    {
                        datatmp[offset + 3] = bDatabuffer[address + block * 1024 + offset];
                        bDataWriteChecksum += bDatabuffer[address + block * 1024 + offset];
                    }
                    msg.gm.message = "Write Flash";
                    msg.percent = ((address + block * 1024 + data_length) * 100) / 0x8000;
                    msg.bgworker.ReportProgress(msg.percent, msg.gm.message);

                    ret = WriteBlock(FLASH_I2C_COMMAND_REG, (byte)(data_length + 3), datatmp);
                    //ret = WriteBlock(0x01, (byte)(data_length + 3), datatmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    Byte bChipChecksum = 0;
                    //Read checksum

                    if (data_length != 1)
                    {
                        //     Byte bChecksum = 0;
                      

                        ret = ReadByte(FLASH_I2C_COMMAND_CHECKSUM, ref bChipChecksum);
                        //ret = ReadByte(0x02, ref bChipChecksum);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        if (bDataWriteChecksum != bChipChecksum)
                        {
                      
                            ret = LibErrorCode.IDS_ERR_WRITE_MAIN_FLASH_CHECKSUM;
                            return ret;
                        }
                    }

                    // We may do somecheck here.
                    double timestamp1 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                    timestamp1 -= timestamp;

                    s = timestamp1.ToString() + "\r\n";
                    WriteFile(s);
                    address += data_length;

                }
            }
            //string s;
            double timestamptotal = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            timestamptotal -= timestamp0;
            s = timestamptotal.ToString() + "\r\n";
            WriteFile(s);
            // Finish
            //ret = WriteByte(0x01, 0x5A);
            ret = WriteByte(FLASH_I2C_COMMAND_REG, FLASH_COMMAND_DONE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
 
        public UInt32 Write(ref TASKMessage msg)
        {
            //return 0;
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
                    case ElementDefine.PARAElement:
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
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            if (YFLASHReglist.Count != 0)
            {
                for (byte i = 0; i < ElementDefine.TRIM_MEMORY_SIZE; i++)
                {
                    ret1 = parent.m_TrimRegImg[i].err;
                    ret |= ret1;
                    if (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;

                    ret1 = OnYFLASHWriteWord(i, (ushort)parent.m_TrimRegImg[i].val);
                    parent.m_TrimRegImg[i].err = ret1;
                    ret |= ret1;
                }

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            if (OpReglist.Count != 0)
            {

                foreach (byte badd in OpReglist)
                {
                    ret = WriteByte(badd, (Byte)parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                if (OpReglist.Count > 1)// Not only access 1 operation register
                    ret = Command(ref msg);
            }


            return ret;
        }

        public UInt32 BitOperation(ref TASKMessage msg)
        {
            return 0;
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
                ret = WriteWord(badd, (ushort)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> TrimParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();
            List<Parameter> ParaParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.PARAElement:
                        {
                            if (p == null) break;
                            ParaParamList.Add(p);
                            break;
                        }
                    case ElementDefine.TrimElement:
                        {
                            if (p == null) break;
                            TrimParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                }
            }

            if (TrimParamList.Count != 0)
            {
                for (int i = 0; i < TrimParamList.Count; i++)
                {
                    param = (Parameter)TrimParamList[i];
                    if (param == null) continue;
                    //if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (ParaParamList.Count != 0)
            {
                for (int i = 0; i < ParaParamList.Count; i++)
                {
                    param = (Parameter)ParaParamList[i];
                    if (param == null) continue;
                    //if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;
                    //if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }

            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> TrimParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();
            List<Parameter> ParaParamList = new List<Parameter>();
            
            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.PARAElement:
                        {
                            if (p == null) break;
                            ParaParamList.Add(p);
                            break;
                        }

                    case ElementDefine.TrimElement:
                        {
                            if (p == null) break;
                            TrimParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                }
            }

            if (TrimParamList.Count != 0)
            {
                for (int i = 0; i < TrimParamList.Count; i++)
                {
                    param = (Parameter)TrimParamList[i];
                    if (param == null) continue;
                    // if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            if (ParaParamList.Count != 0)
            {
                for (int i = 0; i < ParaParamList.Count; i++)
                {
                    param = (Parameter)ParaParamList[i];
                    if (param == null) continue;
//                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;
  //                  if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            return ret;
        }



        public UInt32 Process_Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            // We may do some user define command here.
            Reg reg = null;
            byte baddress = 0;
            byte [] trimming_table = new Byte[64];
            byte[] bDataBuffer = new Byte[64];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt16 u16tmp = 0;
            /*
            msg.gm.message = "Please set TM_SET = 1";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
            if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
            */
            //Init_log();

            List<Parameter> OpReglist = new List<Parameter>();
            List<Parameter> TrimParamList = new List<Parameter>();

            List<Parameter> UserParamList = new List<Parameter>();

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
                          //      if (baddress == 0x05)
                                    OpReglist.Add(p);
                            }
                            break;
                        }
                    case ElementDefine.TrimElement:
                        {
                            if (p == null) break;
                            TrimParamList.Add(p);
                           // trimming_table[] = p.;
                            break;
                        }
                    case ElementDefine.PARAElement:
                    case ElementDefine.PARAElement1:
                    case ElementDefine.PARAElement2:
                    case ElementDefine.PARAElement3:
                        {
                            if (p == null) break;
                            UserParamList.Add(p);
                            // trimming_table[] = p.;
                            break;
                        }
                }
            }

            // Check the target is exist or not.
            if(msg.sub_task < 0xF0)
            {
            ret = OnReadByte(0x10, ref baddress);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            /*
            if (OpReglist.Count != 1) return LibErrorCode.IDS_ERR_DEM_PARAM_READ_UNABLE;

            ret = WorkMode((ElementDefine.COBRA_AZALEA5_WKM)OpReglist[0].phydata);
             */
            /*
            msg.percent = 90;
            msg.task = TM.TM_COMMAND;
            msg.sub_task = 0;// Use this to separate the command
            msg.Flashdata_Length = 0x8000;
            msg.Flashdata = m_bFirmware;
            */
            /*
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

            */
            switch (msg.sub_task)
            {
                case 0:
                    {
                        WriteFile("Write main flash start===================\r\n");

                        msg.percent = 75;
                        msg.controlmsg.message = "Writing flash";
                        double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);

                        ret = Write_Flash(ref msg, 0x8000, msg.flashData);
                        
                        double timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        timestamp2 -= timestamp;
                        string s;
                        s = timestamp2 + "\r\n";
                        WriteFile(s);
                        WriteFile("Write main flash end===================\r\n");
                        break;
                    }
                case 1:// For Vref 1.2V
                    {
                        WriteFile("Trim Vref 1.2V start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[0].val;
                        bDataBuffer[1] = (Byte)parent.m_TrimRegImg[1].val;
                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                       ret = WriteByte(0x07, (Byte)msg.sub_task);
                       //ret = WriteByte(0x07, (Byte)msg.sub_task);
                       ret = WriteByte(0x10, bDataBuffer[0]);
                       ret = WriteByte(0x11, bDataBuffer[1]);
                       ret = WriteByte(0x12, 0x5A);
                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim Vref 1.2V end===================\r\n");
                        break;
                    }

                case 2:// For ADC_Buffer
                    {
                        WriteFile("Trim ADC Buffer start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[7].val;
                        bDataBuffer[1] = 0;//(Byte)parent.m_TrimRegImg[1].val;
                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 3, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Read I2C reg3 and 4 when I2C Reg5=0x6B

                        ret = OnWaitFirmwareCompleted(0x15,0x6B);
                            ret = OnReadByte(0x13, ref baddress);
                            parent.m_OpRegImg[0x13].val = baddress;

                            ret = OnReadByte(0x14, ref baddress);
                            parent.m_OpRegImg[0x14].val = baddress;


                       // parent.m_OpRegImg[0x13].val = 0x55;
                       // parent.m_OpRegImg[0x14].val = 0xAA;
                        ret = WriteByte(0x15, 0);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim ADC Buffer end===================\r\n");
                        break;
                    }

                case 3:// For Current AmplifierOPCS
                    {
                        WriteFile("Trim Current Amplifier OPCS start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[5].val;
                        bDataBuffer[1] = 0;// (Byte)parent.m_TrimRegImg[1].val;
                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 3, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim Current Amplifier OPCS end===================\r\n");
                        break;
                    }
                case 4:// For IREF
                    {
                        WriteFile("Trim IREF start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[2].val;
                        bDataBuffer[1] = 0;// (Byte)parent.m_TrimRegImg[1].val;
                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 3, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim IREF end===================\r\n");
                        break;
                    }
                case 6:// For 20MHz
                    {
                        WriteFile("Trim 20MHz start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[3].val;
                        bDataBuffer[1] = (Byte)parent.m_TrimRegImg[4].val;
                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 3, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim 20Mhz end===================\r\n");
                        break;
                    }
                case 7:// For 32Khz
                    {
                        WriteFile("Trim 32Khz start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[6].val;
                        bDataBuffer[1] = 0;

                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 3, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim 32Khz end===================\r\n");
                        break;
                    }
                case 9:// For DA_I triming
                    {
                        WriteFile("DA_I trim start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[6].val;
                        bDataBuffer[1] = 0;

                        bDataBuffer[2] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 3, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x12);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("DA_I trim end===================\r\n");
                        break;
                    }

                case 10:// For SR
                    {
                        WriteFile("Trim SR start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)parent.m_TrimRegImg[13].val;
                        bDataBuffer[1] = (Byte)parent.m_TrimRegImg[8].val;
                        bDataBuffer[2] = (Byte)parent.m_TrimRegImg[9].val;
                        bDataBuffer[3] = (Byte)parent.m_TrimRegImg[10].val;
                        bDataBuffer[4] = 0;// (Byte)parent.m_TrimRegImg[1].val;

                        bDataBuffer[5] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        //ret = WriteBlock(0x10, 6, bDataBuffer);
                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, bDataBuffer[2]);
                        ret = WriteByte(0x13, bDataBuffer[3]);
                        ret = WriteByte(0x14, bDataBuffer[4]);

                        ret = WriteByte(0x15, 0x5A);

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x15);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Trim SR end===================\r\n");
                        break;
                    }
                case 149:// For Vbus Output Testing
                    {
                        WriteFile("Vbus Output testing start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)SharedFormula.LoByte((ushort)parent.m_TrimRegImg[0x10].val);
                        bDataBuffer[1] = (Byte)SharedFormula.HiByte((ushort)parent.m_TrimRegImg[0x0D].val);
                        bDataBuffer[2] = (Byte)SharedFormula.LoByte((ushort)parent.m_TrimRegImg[0x0D].val);
                        bDataBuffer[3] = (Byte)SharedFormula.HiByte((ushort)parent.m_TrimRegImg[0x0E].val);
                        bDataBuffer[4] = (Byte)SharedFormula.LoByte((ushort)parent.m_TrimRegImg[0x0E].val);
                        
                        //  bDataBuffer[1] = (Byte)parent.m_TrimRegImg[8].val;

                        bDataBuffer[5] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);
                        

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x15);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Vbus output testing end===================\r\n");
                        break;
                    }
                case 150:// Type-C Interface Testing
                    {
                        WriteFile("Type-C Interface Testing start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)SharedFormula.LoByte((ushort)parent.m_TrimRegImg[0x0F].val);
                        bDataBuffer[1] = 0;// (Byte)SharedFormula.LoByte(parent.m_TrimRegImg[0x0D].val);
                        bDataBuffer[2] = 0;// (Byte)SharedFormula.HiByte(parent.m_TrimRegImg[0x0E].val);
                        bDataBuffer[3] = 0;// (Byte)SharedFormula.LoByte(parent.m_TrimRegImg[0x0E].val);

                        //  bDataBuffer[1] = (Byte)parent.m_TrimRegImg[8].val;


                        bDataBuffer[4] = 0;
                        bDataBuffer[5] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);

                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, bDataBuffer[2]);
                        ret = WriteByte(0x13, bDataBuffer[3]);
                        ret = WriteByte(0x14, bDataBuffer[4]);
                        ret = WriteByte(0x15, bDataBuffer[5]);
                        

                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x15);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Type-C Interface testing end===================\r\n");
                        break;
                    }
                case 11:// Vsense trimming
                    {
                        WriteFile("Vsense trimming start===================\r\n");
                        //Prepare the register list for trimming data
                        bDataBuffer[0] = (Byte)SharedFormula.LoByte((ushort)parent.m_TrimRegImg[0x0F].val);
                        bDataBuffer[1] = 0;// (Byte)SharedFormula.LoByte(parent.m_TrimRegImg[0x0D].val);
                        bDataBuffer[2] = 0;// (Byte)SharedFormula.HiByte(parent.m_TrimRegImg[0x0E].val);
                        bDataBuffer[3] = 0;// (Byte)SharedFormula.LoByte(parent.m_TrimRegImg[0x0E].val);

                        //  bDataBuffer[1] = (Byte)parent.m_TrimRegImg[8].val;


                        bDataBuffer[4] = 0;
                        bDataBuffer[5] = 0x5A;

                        // Start the trimming
                        ret = WriteByte(0x07, (Byte)msg.sub_task);

                        ret = WriteByte(0x10, bDataBuffer[0]);
                        ret = WriteByte(0x11, bDataBuffer[1]);
                        ret = WriteByte(0x12, bDataBuffer[2]);
                        ret = WriteByte(0x13, bDataBuffer[3]);
                        ret = WriteByte(0x14, bDataBuffer[4]);
                        ret = WriteByte(0x15, bDataBuffer[5]);


                        // Wait for completed
                        ret = OnWaitTrimCompleted(0x15);

                        // Restore back to normal mode
                        //ret = WriteByte(0x07, 0);
                        WriteFile("Type-C Interface testing end===================\r\n");
                        break;
                    }


                case 0x20:// Read flash
                    {
                        WriteFile("Read flash start===================\r\n");
                       // Byte [] data = new Byte[0x8000];
                       // ret = Read_Flash(0x8000, data);
                        Byte [] data = new Byte[0x8000];
                        for (ret = 0; ret < 0x8000; ret++)
                            data[ret] = 0x00;
                        msg.percent = 95;
                        msg.controlmsg.message = "Prepare read flash";
                        //msg.
                       // msg.controlmsg.percent = 5;
                        double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        //ret = Read_Flash(ref msg,0x8000, data);
                        //2.00.08 changes
                        ret = Read_Flash(ref msg, 0x8000, msg.flashData);
                        //ret = Read_Flash(0x8000, data);
                        double timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        timestamp2 -= timestamp;
                        string s;
                        s = timestamp2 + "\r\n";
                        WriteFile(s);
   

                            /*for (u16tmp = 0; u16tmp < 0x8000; u16tmp++)
                                msg.Flashdata_read[u16tmp] = data[u16tmp];*/
                        //msg.flashData = data;
                        //ret = 0;
                        //Prepare the register list for trimming data
                        WriteFile("Read flash end===================\r\n");
                        break;
                    }
                case 0x21:// Read information flash block 0
                    {
                        WriteFile("Read inforamtion flash block 0 start===================\r\n");
                        // Byte [] data = new Byte[0x8000];
                        // ret = Read_Flash(0x8000, data);
                        Byte[] data = new Byte[0x400];
                        for (ret = 0; ret < 0x400; ret++)
                            data[ret] = 0x00;
                        msg.percent = 95;
                        msg.controlmsg.message = "Prepare read INFO flash block 0";
                        //msg.
                        // msg.controlmsg.percent = 5;
                        //ret = Read_Flash(0x8000, data);
                        //ret = Read_Flash(0x400, data);
                        ret = Read_INFO_Flash(0, 0x80, data);

                        Byte bchecksum = 0;
						//(M160219)Francis, Info Block 0 has re-constructed
                        //if (data[0x0C] == 0x55)
						if(data[0x29] == 0x55)
                        {
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
							//for (u16tmp = 0; u16tmp < 0x17; u16tmp++)
                            for (u16tmp = 0; u16tmp < 0x28; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0x28])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {
                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
								for (u16tmp = 0; u16tmp < 0x28; u16tmp++)
                                {
                                    parent.m_TrimRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_TrimRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }

                                //0x1F is 4 bytes.In current design, we just can handle 2 bytes with low,high
                                //parent.m_TrimRegImg[0x1F].val = data[0x1F];
                                ushort uslo = SharedFormula.MAKEWORD(data[0x24],data[0x23]);
                                ushort ushi = SharedFormula.MAKEWORD(data[0x22],data[0x21]);

                                parent.m_TrimRegImg[0x21].val = SharedFormula.MAKEDWORD(uslo, ushi);


                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }
                        /*
                        for (ret = 0; ret < 0x8000; ret++)
                            msg.Flashdata_read[ret] = data[ret];
                         **/ 
                        
                        //Prepare the register list for trimming data
                        WriteFile("Read information flash block 0 end===================\r\n");
                        break;
                    }
                case 0x22:// Read information flash block 1
                    {
                        WriteFile("Read inforamtion flash block 1 start===================\r\n");
                        Byte[] data = new Byte[0x400];
                        Array.Clear(data,0,0x400);
                        //for (ret = 0; ret < 0x400; ret++)
                         //   data[ret] = 0x00;
                        msg.percent = 95;
                        msg.controlmsg.message = "Prepare read INFO flash block 1";
                        //msg.
                        // msg.controlmsg.percent = 5;
                        //ret = Read_Flash(0x8000, data);
                        //ret = Read_Flash(0x400, data);
                        ret = Read_INFO_Flash(1, 0x400, data);
                        ret = Parametercheck(data);
//                        msg.flashData
                        Byte bchecksum = 0;

                        //Put read value to register parameter list
/*                        
#if 0                       
                        if (data[0x1E] == 0x55)
                        {
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                            for (u16tmp = 0; u16tmp < 0x1D; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0x1D])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {

                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                                for (u16tmp = 0; u16tmp < 0x1D; u16tmp++)
                                {
                                    parent.m_ParaRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_ParaRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }
                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }

                        // PD parameter 
                        if (data[0x60] == 0x55)
                        {
                            bchecksum = 0;
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                            for (u16tmp = 0x40; u16tmp < 0x5F; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0x5F])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {


                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                                for (u16tmp = 0x40; u16tmp < 0x5F; u16tmp++)
                                {
                                    parent.m_ParaRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_ParaRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }
                                ushort uslo = SharedFormula.MAKEWORD(data[0x44], data[0x43]);
                                ushort ushi = SharedFormula.MAKEWORD(data[0x42], data[0x41]);

                                parent.m_ParaRegImg[0x41].val = SharedFormula.MAKEDWORD(uslo, ushi);

                                uslo = SharedFormula.MAKEWORD(data[0x48], data[0x47]);
                                ushi = SharedFormula.MAKEWORD(data[0x46], data[0x45]);

                                parent.m_ParaRegImg[0x45].val = SharedFormula.MAKEDWORD(uslo, ushi);

                                uslo = SharedFormula.MAKEWORD(data[0x4C], data[0x4B]);
                                ushi = SharedFormula.MAKEWORD(data[0x4A], data[0x49]);

                                parent.m_ParaRegImg[0x49].val = SharedFormula.MAKEDWORD(uslo, ushi);
                                uslo = SharedFormula.MAKEWORD(data[0x50], data[0x4F]);
                                ushi = SharedFormula.MAKEWORD(data[0x4E], data[0x4D]);

                                parent.m_ParaRegImg[0x4D].val = SharedFormula.MAKEDWORD(uslo, ushi);
                                uslo = SharedFormula.MAKEWORD(data[0x54], data[0x53]);
                                ushi = SharedFormula.MAKEWORD(data[0x52], data[0x51]);

                                parent.m_ParaRegImg[0x51].val = SharedFormula.MAKEDWORD(uslo, ushi);
                                uslo = SharedFormula.MAKEWORD(data[0x58], data[0x57]);
                                ushi = SharedFormula.MAKEWORD(data[0x56], data[0x55]);

                                parent.m_ParaRegImg[0x55].val = SharedFormula.MAKEDWORD(uslo, ushi);
                                uslo = SharedFormula.MAKEWORD(data[0x5C], data[0x5B]);
                                ushi = SharedFormula.MAKEWORD(data[0x5A], data[0x59]);

                                parent.m_ParaRegImg[0x59].val = SharedFormula.MAKEDWORD(uslo, ushi);


                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }

                        // PD parameter 
                        if (data[0x95] == 0x55)
                        {
                            bchecksum = 0;
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                            for (u16tmp = 0x70; u16tmp < 0x94; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0x94])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {

                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                                for (u16tmp = 0x70; u16tmp < 0x94; u16tmp++)
                                {
                                    parent.m_ParaRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_ParaRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }
                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }


                        // PD parameter 
                        if (data[0xCA] == 0x55)
                        {
                            bchecksum = 0;
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                            for (u16tmp = 0xA0; u16tmp < 0xC9; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0xC9])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {

                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                                for (u16tmp = 0xA0; u16tmp < 0xC9; u16tmp++)
                                {
                                    parent.m_ParaRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_ParaRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }
                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }

*/

                        Array.Copy(data, msg.flashData, 0x400);
                        //for (u16tmp = 0; u16tmp < 0x400; u16tmp++)
                         //   msg.flashData[u16tmp] = data[u16tmp];
                        


                        WriteFile("Read information flash block 1 end===================\r\n");
                        break;
                    }
                case 0x23:
                    {//trimming area
                        WriteFile("Write INFO flash block 0 start===================\r\n");
                        Byte[] data = new Byte[0x400];
                        Byte bChecksum = 0;
                        //(M160218)Francis, modify checksum calculation range, having ADC Gain/offset, DA_V, DA_I gain/offset
                        //for (ret = 0; ret < 0x0B; ret++)
                        for (ret = 0; ret < 0x28; ret++)
                        {
                            data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                           // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }

                        // 2016.04.01 Terry update parameter list
                        // 2016.04.08 Terry update parameter list
                        data[0x21] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x21].val));
                        data[0x22] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x21].val));
                        data[0x23] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x21].val));
                        data[0x24] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x21].val));

                        for (ret = 0; ret < 0x28; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }

                        //data[0x0B] = bChecksum;
                        //data[0x0C] = 0x55;
                        data[0x28] = bChecksum;
                        data[0x29] = 0x55;
                        //(E160218)
                        //msg.percent = 75;
                        // msg.controlmsg.message = "Writing flash";
                        ret = Write_INFO_Flash(0, 0x80, data);
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        WriteFile("Write INFO flash block 0 end===================\r\n");
                        break;
                    }
                case 78:// Write parameter list into information block 1 from file
                    {//user parameter write from parameter list
                        WriteFile("Write INFO flash block 1 parameter start===================\r\n");
                        Byte[] data = new Byte[0x400];
                        Array.Clear(data, 0, 0x400);

                        //May need to check parameter area checksum.
                        ret = ImageBuffercheck(1, data);
                        if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            ret = Erase_INFO_Flash(1);
                            ret = Write_INFO_Flash(1, 0x400, data);

                        }
                        WriteFile("Write INFO flash block 1 file end===================\r\n");
                        break;
                    }
                case 79:// Write parameter list into information block 1 from file
                    {//user parameter write from file
                        WriteFile("Write INFO flash block 1 file start===================\r\n");
                        //May need to check parameter area checksum first.
                        ret = ImageBuffercheck(1,msg.flashData);
                        if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            ret = Erase_INFO_Flash(1);
                            ret = Write_INFO_Flash(1, 0x400, msg.flashData);

                        }
                        WriteFile("Write INFO flash block 1 file end===================\r\n");
                        break;
                    }
                case 0x24:// Write parameter list into information block 1
                    {
                        WriteFile("Write INFO flash block 1 start===================\r\n");
/*
                        WriteFile("Loop for main flash update start===================\r\n");
                        //for (int i = 0; i < 100; i++)
                        for (int i = 0; i < 300; i++)
                        {
                            Thread.Sleep(100);
                            //step 1 erase and write 0x55, then read back
                            double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            ret = Erase_Mian_Flash();
                            double timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;
                            string s;
                            s = timestamp2 + "\r\n";
                            WriteFile(s);
                            Thread.Sleep(200);

                            /////////////////////////////////////

                            timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                            ret = Write_Flash(ref msg, msg.Flashdata_Length, msg.Flashdata);

                            timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;

                            s = timestamp2 + "\r\n";
                            WriteFile(s);
                            Thread.Sleep(200);

                            ////////////////////////////////////


                            ////////////////////////
                            Byte[] data = new Byte[0x8000];
                            for (ret = 0; ret < 0x8000; ret++)
                                data[ret] = 0x00;
                            msg.percent = 95;
                            msg.controlmsg.message = "Prepare read flash";
                            //msg.
                            // msg.controlmsg.percent = 5;
                            timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            ret = Read_Flash(ref msg, 0x8000, data);
                            //ret = Read_Flash(0x8000, data);
                            timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;

                            s = timestamp2 + "\r\n";
                            WriteFile(s);
                            //s="";
                            byte bfail = 0;
                            for (ret = 0; ret < 0x8000; ret++)
                            {
                                if (data[ret] != msg.Flashdata[ret])
                                {
                                    s = "Compare firmware fail :" + (i + 1) + "at" + ret + "\r\n";
                                    bfail = 1;
                                    break;

                                }
                            }
                            if (bfail == 0)
                            {
                                s = "Compare firmware success :" + (i + 1) + "\r\n";
                            }
                            WriteFile(s);
                            Thread.Sleep(100);
                            /////////////////////////



                        }
                        ret = 0;
*/
                        
                        Byte[] data = new Byte[0x400];
                        Byte bChecksum = 0;

                        for (ret = 0; ret < 0x1D; ret++)
                        {
                            data[ret] = (Byte)parent.m_ParaRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }
                        bChecksum = 0;
                        for (ret = 0; ret < 0x1D; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }
                        data[0x1D] = bChecksum;
                        data[0x1E] = 0x55;

                        for (ret = 0x70; ret < 0x94; ret++)
                        {
                            data[ret] = (Byte)parent.m_ParaRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }
                        bChecksum = 0;
                        for (ret = 0x70; ret < 0x94; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }
                        data[0x94] = bChecksum;
                        data[0x95] = 0x55;

                        for (ret = 0xA0; ret < 0xC9; ret++)
                        {
                            data[ret] = (Byte)parent.m_ParaRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }
                        bChecksum = 0;
                        for (ret = 0xA0; ret < 0xC9; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }
                        data[0xC9] = bChecksum;
                        data[0xCA] = 0x55;



                        // Prepare list into block list
                        // Block 1 System parameter
                        data[0x40] = (byte)parent.m_ParaRegImg[0x40].val;

                        data[0x41] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x41].val));
                        data[0x42] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x41].val));
                        data[0x43] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x41].val));
                        data[0x44] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x41].val));

                        data[0x45] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x45].val));
                        data[0x46] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x45].val));
                        data[0x47] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x45].val));
                        data[0x48] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x45].val));

                        data[0x49] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x49].val));
                        data[0x4A] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x49].val));
                        data[0x4B] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x49].val));
                        data[0x4C] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x49].val));

                        data[0x4D] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x4D].val));
                        data[0x4E] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x4D].val));
                        data[0x4F] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x4D].val));
                        data[0x50] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x4D].val));

                        data[0x51] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x51].val));
                        data[0x52] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x51].val));
                        data[0x53] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x51].val));
                        data[0x54] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x51].val));

                        data[0x55] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x55].val));
                        data[0x56] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x55].val));
                        data[0x57] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x55].val));
                        data[0x58] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x55].val));

                        data[0x59] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x59].val));
                        data[0x5A] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x59].val));
                        data[0x5B] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x59].val));
                        data[0x5C] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x59].val));

                        data[0x5D] = (byte)parent.m_ParaRegImg[0x5D].val;
                        data[0x5E] = (byte)parent.m_ParaRegImg[0x5E].val;

                        bChecksum = 0;
                        for (ret = 0x40; ret < 0x5F; ret++)
                        {
                            bChecksum += (Byte)data[ret];
                        }
                        data[0x5F] = bChecksum;
                        data[0x60] = 0x55;
                        // msg.controlmsg.message = "Writing flash";
        //                ret = Write_INFO_Flash(1, 0x400, data);
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);

                        WriteFile("Write INFO flash block 1 end===================\r\n");
                        
                        break;
                    }

                case 0x30://Erasee Mian flash
                    {
                        WriteFile("Erase main flash start===================\r\n");
                        // Byte [] data = new Byte[0x8000];
                        // ret = Read_Flash(0x8000, data);
                        msg.percent = 65;
                        msg.controlmsg.message = "Prepare erase flash";
                        double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        ret = Erase_Mian_Flash();
                        double timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        timestamp2 -= timestamp;
                        string s;
                        s = timestamp2 + "\r\n";
                        WriteFile(s);
                        

                        //ret = Read_Flash(0x8000, data);
                        //Prepare the register list for trimming data
                        WriteFile("Erase main flash end===================\r\n");
                        break;
                    }
                case 0x31://Erasee INFO flash block 0
                    {
                        WriteFile("Erase Information flash block 0 start===================\r\n");
                        // Byte [] data = new Byte[0x8000];
                        // ret = Read_Flash(0x8000, data);
                        msg.percent = 65;
                        msg.controlmsg.message = "Prepare erase flash";

                        ret = Erase_INFO_Flash(0);

                        //ret = Read_Flash(0x8000, data);
                        //Prepare the register list for trimming data
                        WriteFile("Erase Information flash block 0 end===================\r\n");
                        break;
                    }
                case 0x32://Erasee INFO flash block 1
                    {
                        
                        WriteFile("Erase Information flash block 1 start===================\r\n");
                        // Byte [] data = new Byte[0x8000];
                        // ret = Read_Flash(0x8000, data);
                        msg.percent = 65;
                        msg.controlmsg.message = "Prepare erase flash";

                        ret = Erase_INFO_Flash(1);

                        //ret = Read_Flash(0x8000, data);
                        //Prepare the register list for trimming data
                        WriteFile("Erase Information flash block 1 end===================\r\n");
                        break;
                        /*
                        WriteFile("Loop for main flash update start===================\r\n");
                        //for (int i = 0; i < 100; i++)
                            for (int i = 0; i < 2; i++)
                        {
                            //step 1 erase and write 0x55, then read back
                            double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            ret = Erase_Mian_Flash();
                            double timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;
                            string s;
                            s = timestamp2 + "\r\n";
                            WriteFile(s);


/////////////////////////////////////
                            for (ret = 0; ret < 0x8000; ret++)
                                msg.Flashdata[ret] = 0x55;

                        timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        ret = Write_Flash(ref msg,msg.Flashdata_Length, msg.Flashdata);
                        
                        timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        timestamp2 -= timestamp;
                        
                        s = timestamp2 + "\r\n";
                        WriteFile(s);
                        Thread.Sleep(100);

////////////////////////////////////


////////////////////////
                            Byte[] data = new Byte[0x8000];
                            for (ret = 0; ret < 0x8000; ret++)
                                data[ret] = 0x00;
                            msg.percent = 95;
                            msg.controlmsg.message = "Prepare read flash";
                            //msg.
                            // msg.controlmsg.percent = 5;
                            timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            ret = Read_Flash(ref msg, 0x8000, data);
                            //ret = Read_Flash(0x8000, data);
                            timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;
                            
                            s = timestamp2 + "\r\n";
                            WriteFile(s);
                            //s="";
                            byte bfail=0;
                            for (ret = 0; ret < 0x8000; ret++)
                            {
                                if(data[ret] !=  msg.Flashdata[ret])
                                {
                                    s = "Compare 55 fail :"+(i + 1) +"at"+ ret + "\r\n";
                                    bfail =1;
                                    break;

                                }
                            }
                            if(bfail==0)
                            {
                                    s="Compare 55 success :"+(i+1)+"\r\n";
                            }
                                 WriteFile(s);
                                 Thread.Sleep(100);
/////////////////////////



                            //step 1 erase and write 0xaa, then read back and compare
                            timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            ret = Erase_Mian_Flash();
                            timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;

                            s = timestamp2 + "\r\n";
                            WriteFile(s);

/////////////////////////////////////
                            for (ret = 0; ret < 0x8000; ret++)
                                msg.Flashdata[ret] = 0xaa;
Thread.Sleep(100);
                        timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        ret = Write_Flash(ref msg,msg.Flashdata_Length, msg.Flashdata);
                        
                        timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        timestamp2 -= timestamp;
                        
                        s = timestamp2 + "\r\n";
                        WriteFile(s);

////////////////////////////////////


////////////////////////
                  Thread.Sleep(100);          
                            for (ret = 0; ret < 0x8000; ret++)
                                data[ret] = 0x00;
                            msg.percent = 95;
                            msg.controlmsg.message = "Prepare read flash";
                            //msg.
                            // msg.controlmsg.percent = 5;
                            timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            ret = Read_Flash(ref msg, 0x8000, data);
                            //ret = Read_Flash(0x8000, data);
                            timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                            timestamp2 -= timestamp;
                            
                            s = timestamp2 + "\r\n";
                            WriteFile(s);

                            bfail = 0;
                            for (ret = 0; ret < 0x8000; ret++)
                            {
                                if (data[ret] != msg.Flashdata[ret])
                                {
                                    s = "Compare AA fail :" + ret + "\r\n";
                                    bfail = 1;
                                    break;

                                }
                            }
                            if (bfail == 0)
                            {
                                s = "Compare AA success :" + (i+1) + "\r\n";
                            }
                            WriteFile(s);

                            Thread.Sleep(100);
/////////////////////////





                        }
                            ret = 0;
                         * */
                        WriteFile("Loop for main flash update end===================\r\n");
                        break;
                    }

                case 0x33:// For B0 version chip. we add erase command before write flash
                    {
                        WriteFile("Write main flash with erase start===================\r\n");

                        msg.percent = 50;
                        msg.controlmsg.message = "Writing flash";
                        double timestamp = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

                        ret = Erase_Mian_Flash();

                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        //changes for 2.00.08
                        //ret = Write_Flash(ref msg,(ushort)msg.flashData.Length, msg.flashData);
                        ret = Write_Flash(ref msg, 0x8000, msg.flashData);
                        double timestamp2 = (DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                        timestamp2 -= timestamp;
                        string s;
                        s = timestamp2 + "\r\n";
                        WriteFile(s);
                        WriteFile("Write main flash with erase end===================\r\n");
                        break;
                    }

                case 0x34:
                    {
                        WriteFile("Write INFO flash block 0 with erase start===================\r\n");
                        Byte[] data = new Byte[0x400];
                        Byte bChecksum = 0;
                        //(M160218)Francis, modify checksum calculation range, having ADC Gain/offset, DA_V, DA_I gain/offset
                        //for (ret = 0; ret < 0x0B; ret++)

                        ret = Erase_INFO_Flash(0);

                        for (ret = 0; ret < 0x28; ret++)
                        {
                            data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }

                        // 2016.04.01 Terry update parameter list
                        // 2016.04.08 Terry update parameter list
                        data[0x21] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x21].val));
                        data[0x22] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x21].val));
                        data[0x23] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x21].val));
                        data[0x24] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x21].val));

                        for (ret = 0; ret < 0x28; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }

                        //data[0x0B] = bChecksum;
                        //data[0x0C] = 0x55;
                        data[0x28] = bChecksum;
                        data[0x29] = 0x55;
                        //(E160218)
                        //msg.percent = 75;
                        // msg.controlmsg.message = "Writing flash";
                        ret = Write_INFO_Flash(0, 0x80, data);
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        WriteFile("Write INFO flash block 0 end===================\r\n");
                        break;
                    }

                case 0x35:// Write parameter list into information block 1
                    {
                        WriteFile("Write INFO flash block 1 with erase start===================\r\n");
                        Byte[] data = new Byte[0x400];
                        Byte bChecksum = 0;


                        ret = Erase_INFO_Flash(1);

                        for (ret = 0; ret < 0x1D; ret++)
                        {
                            data[ret] = (Byte)parent.m_ParaRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }
                        /*
                        data[0x17] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x18] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x19] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x1A] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x17].val));
                        */
                        bChecksum = 0;
                        for (ret = 0; ret < 0x1D; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }
                        data[0x1D] = bChecksum;
                        data[0x1E] = 0x55;

                        for (ret = 0x70; ret < 0x94; ret++)
                        {
                            data[ret] = (Byte)parent.m_ParaRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }
                        /*
                        data[0x17] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x18] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x19] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x1A] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x17].val));
                        */
                        bChecksum = 0;
                        for (ret = 0x70; ret < 0x94; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }
                        data[0x94] = bChecksum;
                        data[0x95] = 0x55;

                        for (ret = 0xA0; ret < 0xC9; ret++)
                        {
                            data[ret] = (Byte)parent.m_ParaRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }
                        /*
                        data[0x17] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x18] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x19] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x17].val));
                        data[0x1A] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x17].val));
                        */
                        bChecksum = 0;
                        for (ret = 0xA0; ret < 0xC9; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }
                        data[0xC9] = bChecksum;
                        data[0xCA] = 0x55;



                        // Prepare list into block list
                        // Block 1 System parameter
                        data[0x40] = (byte)parent.m_ParaRegImg[0x40].val;

                        data[0x41] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x41].val));
                        data[0x42] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x41].val));
                        data[0x43] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x41].val));
                        data[0x44] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x41].val));

                        data[0x45] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x45].val));
                        data[0x46] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x45].val));
                        data[0x47] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x45].val));
                        data[0x48] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x45].val));

                        data[0x49] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x49].val));
                        data[0x4A] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x49].val));
                        data[0x4B] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x49].val));
                        data[0x4C] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x49].val));

                        data[0x4D] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x4D].val));
                        data[0x4E] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x4D].val));
                        data[0x4F] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x4D].val));
                        data[0x50] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x4D].val));

                        data[0x51] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x51].val));
                        data[0x52] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x51].val));
                        data[0x53] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x51].val));
                        data[0x54] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x51].val));

                        data[0x55] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x55].val));
                        data[0x56] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x55].val));
                        data[0x57] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x55].val));
                        data[0x58] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x55].val));

                        data[0x59] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x59].val));
                        data[0x5A] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_ParaRegImg[0x59].val));
                        data[0x5B] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x59].val));
                        data[0x5C] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_ParaRegImg[0x59].val));

                        data[0x5D] = (byte)parent.m_ParaRegImg[0x5D].val;
                        data[0x5E] = (byte)parent.m_ParaRegImg[0x5E].val;

                        bChecksum = 0;
                        for (ret = 0x40; ret < 0x5F; ret++)
                        {
                            bChecksum += (Byte)data[ret];
                        }
                        data[0x5F] = bChecksum;
                        data[0x60] = 0x55;
                        // msg.controlmsg.message = "Writing flash";
                        ret = Write_INFO_Flash(1, 0x200, data);
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);

                        WriteFile("Write INFO flash block 1 end===================\r\n");
                        break;
                    }

                case 0x40://For C0 version trimming data area
                    {
                        WriteFile("Write INFO flash block 0 with erase start===================\r\n");
                        Byte[] data = new Byte[0x400];
                       

                        ret = Erase_INFO_Flash(0);

                        for (ret = 0; ret < 0x39; ret++)
                        {
                            data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }

                        // 2017.02.14 Terry update parameter list
                        data[0x0E] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x0E].val));
                        data[0x0F] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x0E].val));

                        data[0x12] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x12].val));
                        data[0x13] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x12].val));

                        data[0x16] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x16].val));
                        data[0x17] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x16].val));

                        data[0x1A] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1A].val));
                        data[0x1B] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1A].val));

                        data[0x1E] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1E].val));
                        data[0x1F] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1E].val));

                        data[0x20] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x20].val));
                        data[0x21] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x20].val));

                        // 2016.04.08 Terry update parameter list
                        data[0x28] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x28].val));
                        data[0x29] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x28].val));
                        data[0x2A] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x28].val));
                        data[0x2B] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x28].val));

                        Byte bChecksum = 0;
                        data[0x00] = 0x01;
                        data[0x01] = 0x39;

                        for (ret = 0; ret < 0x39; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }

                        //data[0x0B] = bChecksum;
                        //data[0x0C] = 0x55;
                        data[0x39]= bChecksum;
                        data[0x3A] = 0x55;
                        //(E160218)
                        //msg.percent = 75;
                        // msg.controlmsg.message = "Writing flash";
                        ret = Write_INFO_Flash(0, 0x80, data);
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        WriteFile("Write INFO flash block 0 end===================\r\n");
                        break;
                    }

                case 0x41://For C0 version, Read information flash block 0
                    {
                        WriteFile("Read inforamtion flash block 0 start===================\r\n");
                        // Byte [] data = new Byte[0x8000];
                        // ret = Read_Flash(0x8000, data);
                        Byte[] data = new Byte[0x400];
                        for (ret = 0; ret < 0x400; ret++)
                            data[ret] = 0x00;
                        msg.percent = 95;
                        msg.controlmsg.message = "Prepare read INFO flash block 0";
                        //msg.
                        // msg.controlmsg.percent = 5;
                        //ret = Read_Flash(0x8000, data);
                        //ret = Read_Flash(0x400, data);
                        ret = Read_INFO_Flash(0, 0x80, data);

                        Byte bchecksum = 0;
                        //(M160219)Francis, Info Block 0 has re-constructed
                        //if (data[0x0C] == 0x55)
                        if (data[0x3A] == 0x55)
                        {
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                            //for (u16tmp = 0; u16tmp < 0x17; u16tmp++)
                            for (u16tmp = 0; u16tmp < 0x39; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0x39])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {
                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                                for (u16tmp = 0; u16tmp < 0x39; u16tmp++)
                                {
                                    parent.m_TrimRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_TrimRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }

                                //0x1F is 4 bytes.In current design, we just can handle 2 bytes with low,high
                                //parent.m_TrimRegImg[0x1F].val = data[0x1F];
                                ushort uslo = SharedFormula.MAKEWORD(data[0x2B], data[0x2A]);
                                ushort ushi = SharedFormula.MAKEWORD(data[0x29], data[0x28]);

                                parent.m_TrimRegImg[0x28].val = SharedFormula.MAKEDWORD(uslo, ushi);


                                // 2017.02.14 Terry update parameter list
                                uslo = SharedFormula.MAKEWORD(data[0x0F], data[0x0E]);
                                Int16 i16_lo=(Int16)uslo;
                                Int16 i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;
                                
                                parent.m_TrimRegImg[0x0E].val = (uint)i16_lo;
                                parent.m_TrimRegImg[0x0F].val = (uint)i16_hi;


                                uslo = SharedFormula.MAKEWORD(data[0x13], data[0x12]);
                                i16_lo=(Int16)uslo;
                                i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;
                                
                                parent.m_TrimRegImg[0x12].val = (uint)(i16_lo);
                                parent.m_TrimRegImg[0x13].val = (uint)(i16_hi);

                                uslo = SharedFormula.MAKEWORD(data[0x17], data[0x16]);
                                i16_lo=(Int16)uslo;
                                i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;
                                
                                parent.m_TrimRegImg[0x16].val = (uint)(i16_lo);
                                parent.m_TrimRegImg[0x17].val = (uint)(i16_hi);

                                uslo = SharedFormula.MAKEWORD(data[0x1B], data[0x1A]);
                                i16_lo=(Int16)uslo;
                                i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;
                                
                                parent.m_TrimRegImg[0x1A].val = (uint)(i16_lo);
                                parent.m_TrimRegImg[0x1B].val = (uint)(i16_hi);


                                uslo = SharedFormula.MAKEWORD(data[0x1F], data[0x1E]);
                                i16_lo=(Int16)uslo;
                                i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;
                                
                                parent.m_TrimRegImg[0x1E].val = (uint)(i16_lo);
                                parent.m_TrimRegImg[0x1F].val = (uint)(i16_hi);


                                uslo = SharedFormula.MAKEWORD(data[0x21], data[0x20]);
                                i16_lo=(Int16)uslo;
                                i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;
                                
                                parent.m_TrimRegImg[0x20].val = (uint)(i16_lo);
                                parent.m_TrimRegImg[0x21].val = (uint)(i16_hi);


                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }
                        /*
                        for (ret = 0; ret < 0x8000; ret++)
                            msg.Flashdata_read[ret] = data[ret];
                         **/

                        //Prepare the register list for trimming data
                        WriteFile("Read information flash block 0 end===================\r\n");
                        break;
                    }
                case 0x42://Write trimming area, For D0 version trimming data area
                    {
                        WriteFile("Write INFO flash block 0 with erase start for D0===================\r\n");
                        Byte[] data = new Byte[0x400];


                        ret = Erase_INFO_Flash(0);

                        for (ret = 0; ret < ElementDefine.TRIM_MEMORY_SIZE; ret++)
                        {
                            data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            // bChecksum += (Byte)parent.m_TrimRegImg[ret].val;
                        }

                        // 2017.02.14 Terry update parameter list
                      //  data[0x0E] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x0E].val));
                      //  data[0x0F] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x0E].val));

                     //   data[0x12] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x12].val));
                     //   data[0x13] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x12].val));

                      //  data[0x16] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x16].val));
                      //  data[0x17] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x16].val));

                      //  data[0x1A] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1A].val));
                      //  data[0x1B] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1A].val));

                      //  data[0x1E] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1E].val));
                      //  data[0x1F] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x1E].val));

                       // data[0x20] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x20].val));
                       // data[0x21] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x20].val));

                        // 2016.04.08 Terry update parameter list
                        data[0x28] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x28].val));
                        data[0x29] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x28].val));
                        data[0x2A] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x28].val));
                        data[0x2B] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x28].val));

                        // 2018.02.05 Terry update parameter list
                        data[0x67] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x67].val));
                        data[0x68] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x67].val));
                        data[0x69] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x67].val));
                        data[0x6A] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x67].val));

                        // 2018.02.05 Terry update parameter list
                        data[0x6B] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x6B].val));
                        data[0x6C] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x6B].val));
                        data[0x6D] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x6B].val));
                        data[0x6E] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x6B].val));

                        // 2018.02.05 Terry update parameter list
                        data[0x6F] = SharedFormula.HiByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x6F].val));
                        data[0x70] = SharedFormula.LoByte((ushort)SharedFormula.HiWord((int)parent.m_TrimRegImg[0x6F].val));
                        data[0x71] = SharedFormula.HiByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x6F].val));
                        data[0x72] = SharedFormula.LoByte((ushort)SharedFormula.LoWord((int)parent.m_TrimRegImg[0x6F].val));

                        //Update string field

                        Byte bChecksum = 0;
                        data[0x00] = 0x01;
                        data[0x01] = 0xC0;

                        for (ret = 0; ret < 0xC0; ret++)
                        {
                            //data[ret] = (Byte)parent.m_TrimRegImg[ret].val;
                            bChecksum += data[ret];
                        }

                        data[0xc0] = bChecksum;
                        data[0xc1] = 0x55;
                        //(E160218)
                        //msg.percent = 75;
                        // msg.controlmsg.message = "Writing flash";
                        ret = Write_INFO_Flash(0, 0x100, data);
                        //ret = Write_Flash(msg.Flashdata_Length, msg.Flashdata);
                        WriteFile("Write INFO flash block 0 end===================\r\n");
                        break;
                    }
                case 0x43://For D0 version, Read information flash block 0
                    {
                        WriteFile("Read inforamtion flash block 0 start for D0===================\r\n");
                        // Byte [] data = new Byte[0x8000];
                        // ret = Read_Flash(0x8000, data);
                        Byte[] data = new Byte[0x400];
                        for (ret = 0; ret < 0x400; ret++)
                            data[ret] = 0x00;
                        msg.percent = 95;
                        msg.controlmsg.message = "Prepare read INFO flash block 0";
                        //msg.
                        // msg.controlmsg.percent = 5;
                        //ret = Read_Flash(0x8000, data);
                        //ret = Read_Flash(0x400, data);
                        ret = Read_INFO_Flash(0, 0x100, data);

                        Byte bchecksum = 0;
                        //(M160219)Francis, Info Block 0 has re-constructed
                        //if (data[0x0C] == 0x55)
                        if (data[0xC1] == 0x55)
                        {
                            //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                            //for (u16tmp = 0; u16tmp < 0x17; u16tmp++)
                            for (u16tmp = 0; u16tmp < 0xC0; u16tmp++)
                                bchecksum += data[u16tmp];

                            if (bchecksum != data[0xC0])
                            {
                                ret = LibErrorCode.IDS_ERR_READ_INFO_FLASH_CHECKSUM;
                            }
                            else
                            {
                                //for (u16tmp = 0; u16tmp < 0x0B; u16tmp++)
                                for (u16tmp = 0; u16tmp < 0xC0; u16tmp++)
                                {
                                    parent.m_TrimRegImg[u16tmp].val = data[u16tmp];
                                    parent.m_TrimRegImg[u16tmp].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                }

                                //Handle int16 to int32
                                // 2017.02.14 Terry update parameter list
                                ushort uslo = 0;
                                ushort ushi = 0;// SharedFormula.MAKEWORD(data[0x29], data[0x28]);

                                /*
                                uslo = SharedFormula.MAKEWORD(data[0x0F], data[0x0E]);
                                Int16 i16_lo = (Int16)uslo;
                                Int16 i16_hi = 0;
                                if (i16_lo < 0)
                                    i16_hi = -1;

                                parent.m_TrimRegImg[0x0E].val = (uint)i16_lo;
                                parent.m_TrimRegImg[0x0F].val = (uint)i16_hi;
                                */
                                //0x1F is 4 bytes.In current design, we just can handle 2 bytes with low,high
                                //parent.m_TrimRegImg[0x1F].val = data[0x1F];
                                uslo = SharedFormula.MAKEWORD(data[0x2B], data[0x2A]);
                                ushi = SharedFormula.MAKEWORD(data[0x29], data[0x28]);

                                parent.m_TrimRegImg[0x28].val = SharedFormula.MAKEDWORD(uslo, ushi);


                                uslo = SharedFormula.MAKEWORD(data[0x6A], data[0x69]);
                                ushi = SharedFormula.MAKEWORD(data[0x68], data[0x67]);
                                parent.m_TrimRegImg[0x67].val = SharedFormula.MAKEDWORD(uslo, ushi);

                                uslo = SharedFormula.MAKEWORD(data[0x6E], data[0x6D]);
                                ushi = SharedFormula.MAKEWORD(data[0x6C], data[0x6B]);
                                parent.m_TrimRegImg[0x6B].val = SharedFormula.MAKEDWORD(uslo, ushi);

                                uslo = SharedFormula.MAKEWORD(data[0x72], data[0x71]);
                                ushi = SharedFormula.MAKEWORD(data[0x70], data[0x6F]);
                                parent.m_TrimRegImg[0x6F].val = SharedFormula.MAKEDWORD(uslo, ushi);


                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            }
                        }
                        else
                        {
                            // Info block maybe empty or something wrong
                            ret = LibErrorCode.IDS_ERR_INVALID_INFO_FLASH_DATA;
                        }
                        /*
                        for (ret = 0; ret < 0x8000; ret++)
                            msg.Flashdata_read[ret] = data[ret];
                         **/

                        //Prepare the register list for trimming data
                        WriteFile("Read information flash block 0  for D0 end===================\r\n");
                        break;
                    }
                case 0xF0://User define command
                    {
                        WriteFile("User define command 0xF0 start===================\r\n");
                         byte [] databuffer = new byte[64];
                        // ret = Read_Flash(0x8000, data);
                        //msg.percent = 65;
                        //msg.controlmsg.message = "Prepare erase flash";
                         for (int i = 0; i < msg.sm.misc[2]; i++)
                         {
                             databuffer[i] = (byte)msg.sm.misc[3 + i];
                         }

                         ret = WriteBlock_SMB((byte)msg.sm.misc[0], (byte)msg.sm.misc[1], (byte)msg.sm.misc[2], databuffer);

                        //ret = Erase_INFO_Flash(1);

                        //ret = Read_Flash(0x8000, data);
                        //Prepare the register list for trimming data
                        WriteFile("User define command 0xF0 end===================\r\n");
                        break;
                    }
                case 0xF8://User define command
                    {
                        WriteFile("User define command 0xF8 start===================\r\n");
                         Byte [] dataregister = new Byte[0x10];
                        Byte [] databuffer = new Byte[0x40];
                        // ret = Read_Flash(0x8000, data);
                       // msg.percent = 65;
                        //msg.controlmsg.message = "Prepare erase flash";
                        dataregister[0] = (byte)msg.sm.misc[1];
                         //ret = WriteBlock_SMB((byte)msg.sm.misc[0], (byte)msg.sm.misc[1], (byte)msg.sm.misc[2], databuffer);
                        Byte length_to_read = (byte)msg.sm.misc[2];
                       // msg.sm.misc[2] = length;
                        ret = ReadBlock_SMB((byte)msg.sm.misc[0], dataregister, 1, databuffer, ref length_to_read);
                        //ret = Erase_INFO_Flash(1);
                         for (int i = 0; i < databuffer[0]; i++)
                         {
                             msg.sm.misc[i] = databuffer[i+1]; 
                         }

                        //ret = Read_Flash(0x8000, data);
                        //Prepare the register list for trimming data
                        WriteFile("User define command 0xF8 end===================\r\n");
                        break;
                    }


                //case ElementDefine.TemperatureElement:
                default:
                    break;
            }




            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            return 0;
            int ival = 0;
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)SharedFormula.HiByte(wval); 
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
        #endregion
    }
}