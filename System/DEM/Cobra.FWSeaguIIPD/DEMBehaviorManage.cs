using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using Cobra.Communication;
using Cobra.Common;
using Cobra.FWSeaguIIPD.Behavior;

namespace Cobra.FWSeaguIIPD
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

        public object m_lock = new object();
        public CCommunicateManager m_Interface = new CCommunicateManager();

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

        #region 基础服务功能设计
        public UInt32 Erase(ref TASKMessage msg)
        {
            return Erase();
        }

        public UInt32 Erase()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadStatus();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = HandshakeWrite();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            byte[] FWUCmd = { 0x0F, ElementDefine.m_rom_infor.FWU };
            ret = BlockWrite(0xFC, ref FWUCmd);
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region Read flash and compare with image
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        ret = MainBlockWrite(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
                        ret = MainBlockRead(ref msg);
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
                        ret = MainBlockRead(ref msg);
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
        protected UInt32 BlockRead(byte cmd, ref byte[] buffer)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                ret = OnBlockRead(cmd, ref buffer);
            }
            return ret;
        }
        protected UInt32 BlockWrite(byte cmd, ref byte[] buffer)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, ref buffer);
            }
            return ret;
        }
        protected virtual UInt32 OnBlockRead(byte cmd, ref byte[] buffer)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        protected virtual UInt32 OnBlockWrite(byte cmd, ref byte[] buffer)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadStatus();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = 0x00;
            deviceinfor.shwversion = String.Format("{0:d}", ElementDefine.m_rom_infor.ROM_VER);
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

        #region CRC8
        public byte crc8_calc(ref byte[] pdata, UInt16 n)
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

        public byte calc_crc_read(byte slave_addr, byte reg_addr, byte[] data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = data[0];
            pdata[4] = data[1];

            return crc8_calc(ref pdata, 5);
        }

        public byte calc_crc_block_read(byte slave_addr, byte reg_addr, byte[] data)
        {
            int len = data.Length - 1;
            byte[] pdata = new byte[data.Length + 3];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            Array.Copy(data, 0, pdata, 3, len);

            return crc8_calc(ref pdata, (UInt16)(pdata.Length - 1));
        }

        public byte calc_crc_write(byte slave_addr, byte reg_addr, byte data0, byte data1)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data0;
            pdata[3] = data1;

            return crc8_calc(ref pdata, 4);
        }
        #endregion

        #region 系统函数        
        public UInt32 ReadStatus()
        {
            //CP Status, FWU, ROM hex data show
            //Read S:0x40 0xF0, R: 40 F0 CP FWU ROM CRC
            byte[] bdata = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockRead(0xF0, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ElementDefine.m_rom_infor.CP_STATUS = bdata[0];
            if (!ElementDefine.m_FWU_List.Contains(bdata[1]))
                ElementDefine.m_rom_infor.FWU = 0xA5;
            else
                ElementDefine.m_rom_infor.FWU = bdata[1];
            ElementDefine.m_rom_infor.ROM_VER = bdata[2];
            return ret;
        }
        public UInt32 HandshakeWrite()
        {
            //Write S:0x40 0xF1 0F A5 CRC 所有写操作之前都需要enable write一下
            byte[] bdata = { 0x0F, 0xA5 };
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockWrite(0xF1, ref bdata);
            return ret;
        }
        public UInt32 HandshakeRead()
        {
            //Write S:0x40 0xF1 0F 5A CRC 所有读操作之前都需要enable read一下
            byte[] bdata = { 0xF0, 0x5A };
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockWrite(0xF1, ref bdata);
            return ret;
        }
        public UInt32 FWUpdate(ref byte[] bArray)
        {
            //Read S:0x40 0xF2 AD4....AD1 Data size CRC
            byte[] bdata = new byte[5];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockRead(0xF2, ref bdata);
            bArray = bdata;
            return ret;
        }
        public UInt32 MainBlockWrite(ref TASKMessage msg)
        {
            //Use FWUpdate to get the address and decide to operate which area by address range            
            byte[] bArray = null;
            byte[] btemp = new byte[32]; //Fixed, 128byte don't work
            byte[] bsend = new byte[5];
            byte[] bimage = null;
            UInt32 hwResult = 0, swResult = 0, Result = 0, fwStartAdd = 0, total_size = 0;
            ElementDefine.MEMORY type = ElementDefine.MEMORY.EEPROM;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (msg.flashData.Length < +ElementDefine.EEPROM_START_ADDRESS) return ElementDefine.IDS_ERR_DEM_HEX_FILE_SIZE;

            PrintMessage("Read Status.", msg);
            ret = ReadStatus();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            PrintMessage("Handshake Write.", msg);
            ret = HandshakeWrite();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Thread.Sleep(5);
            PrintMessage("Read FW update start and total size.", msg);
            ret = FWUpdate(ref bArray);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bsend[0] = 0;  //Fixed as 0, 32bytes 
            fwStartAdd = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
            PrintMessage(string.Format("The FW update start address 0x{0:x4}.", fwStartAdd), msg);
            Array.Copy(bArray, 0, bsend, 1, 4);
            switch (bArray[4])
            {
                case 0:
                case 1:
                    total_size = (UInt32)(31 * (bArray[4] + 1) * 1024);
                    break;
                case 2:
                    total_size = (UInt32)(63.5 * 1024);
                    break;
            }
            //Count CRC
            bimage = new byte[total_size];
            Array.Clear(bimage, 0, bimage.Length);
            Array.Copy(msg.flashData, ElementDefine.EEPROM_START_ADDRESS, bimage, 0, ((msg.flashData.Length - ElementDefine.EEPROM_START_ADDRESS) > bimage.Length) ? bimage.Length : (msg.flashData.Length - ElementDefine.EEPROM_START_ADDRESS));
            Result = sw_calc(bimage, 0xC4, bimage.Length, 8);
            bimage[0xC0] = (byte)Result;
            bimage[0xC1] = (byte)(Result >> 8);
            bimage[0xC2] = (byte)(Result >> 16);
            bimage[0xC3] = (byte)(Result >> 24);
            PrintMessage(string.Format("The CRC for 0xC0 is 0x{0:x4}.", Result), msg);
            swResult = sw_calc(bimage, 0, bimage.Length, 8);
            PrintMessage(string.Format("The SW CRC is 0x{0:x4}.", swResult), msg);
            PrintMessage("Begin to erase.", msg);
            ret = Erase();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Thread.Sleep(3000);
            if ((fwStartAdd >= ElementDefine.EEPROM_START_ADDRESS) && (fwStartAdd < ElementDefine.EFLASH_START_ADDRESS))
                type = ElementDefine.MEMORY.EEPROM;
            else
                type = ElementDefine.MEMORY.ExtFlash;
            PrintMessage("Begin to download.", msg);
            switch (type)
            {
                case ElementDefine.MEMORY.EEPROM:
                    ret = BlockWrite(0xF9, ref bsend);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(5);
                    for (int fAddr = 0; fAddr < total_size/* msg.flashData.Length*/; fAddr += btemp.Length) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                    {
                        Array.Copy(bimage, fAddr, btemp, 0, btemp.Length);
                        ret = BlockWrite(0xF7, ref btemp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        PrintMessage(string.Format("Downloaded {0:f2}%", fAddr * 100.00 / total_size), msg);
                    }
                    break;
                case ElementDefine.MEMORY.ExtFlash:
                    ret = BlockWrite(0xF3, ref bsend);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(5);
                    for (int fAddr = 0; fAddr < total_size/* msg.flashData.Length*/; fAddr += btemp.Length) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                    {
                        Array.Copy(bimage, fAddr, btemp, 0, btemp.Length);
                        ret = BlockWrite(0xF5, ref btemp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        PrintMessage(string.Format("Downloaded {0:f2}%", fAddr * 100.00 / total_size), msg);
                    }
                    break;
            }
            PrintMessage("Downloaded 100%", msg);
            ret = hw_calc(fwStartAdd, (fwStartAdd + total_size - 4), ref hwResult);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            PrintMessage(string.Format("The SW CRC is 0x{0:x4},HW CRC is 0x{1:x4}", swResult, hwResult), msg);
            if (swResult != hwResult) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
            return ret;
        }
        public UInt32 MainBlockRead(ref TASKMessage msg)
        {
            //Use FWUpdate to get the address and decide to operate which area by address range            
            byte[] bArray = null;
            byte[] btemp = new byte[32]; //Fixed, 128byte don't work
            byte[] bsend = new byte[5];
            UInt32 hwResult = 0, Result = 0, fwStartAdd = 0, total_size = 0;
            ElementDefine.MEMORY type = ElementDefine.MEMORY.EEPROM;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            PrintMessage("Read Status.", msg);
            ret = ReadStatus();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            PrintMessage("Handshake Read.", msg);
            ret = HandshakeRead();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            PrintMessage("Read FW update start and total size.", msg);
            ret = FWUpdate(ref bArray);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            bsend[0] = 0;  //Fixed as 0, 32bytes 
            fwStartAdd = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
            PrintMessage(string.Format("The FW update start address 0x{0:x4}.", fwStartAdd), msg);
            Array.Copy(bArray, 0, bsend, 1, 4);
            switch (bArray[4])
            {
                case 0:
                case 1:
                    total_size = (UInt32)(31 * (bArray[4] + 1) * 1024);
                    break;
                case 2:
                    total_size = (UInt32)(63.5 * 1024);
                    break;
            }
            byte[] bimage = new byte[total_size];
            if ((fwStartAdd >= ElementDefine.EEPROM_START_ADDRESS) && (fwStartAdd < ElementDefine.EFLASH_START_ADDRESS))
                type = ElementDefine.MEMORY.EEPROM;
            else
                type = ElementDefine.MEMORY.ExtFlash;
            switch (type)
            {
                case ElementDefine.MEMORY.EEPROM:
                    ret = BlockWrite(0xF9, ref bsend);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(5);
                    for (int fAddr = 0; fAddr < total_size/* msg.flashData.Length*/; fAddr += btemp.Length) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                    {
                        ret = BlockRead(0xFB, ref btemp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Array.Copy(btemp, 0, bimage, fAddr, btemp.Length);
                        Thread.Sleep(5);
                    }
                    break;
                case ElementDefine.MEMORY.ExtFlash:
                    ret = BlockWrite(0xF3, ref bsend);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(5);
                    for (int fAddr = 0; fAddr < total_size/* msg.flashData.Length*/; fAddr += btemp.Length) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                    {
                        ret = BlockRead(0xF4, ref btemp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Array.Copy(btemp, 0, bimage, fAddr, btemp.Length);
                        Thread.Sleep(5);
                    }
                    break;
            }
            ret = hw_calc(fwStartAdd, (fwStartAdd + total_size - 4), ref hwResult);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Result = sw_calc(bimage, 0, (int)total_size, 8);
            PrintMessage(string.Format("The SW CRC is 0x{0:x4},HW CRC is 0x{1:x4}", Result, hwResult), msg);
            if (Result != hwResult) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
            msg.flashData = bimage;
            return ret;
        }
        public UInt32 hw_calc(UInt32 startAddr, UInt32 endAddr, ref UInt32 result)
        {
            byte[] bStartAddrCmd = { (byte)startAddr, (byte)(startAddr >> 8), (byte)(startAddr >> 16), (byte)(startAddr >> 24) };
            byte[] bEndAddrCmd = { (byte)endAddr, (byte)(endAddr >> 8), (byte)(endAddr >> 16), (byte)(endAddr >> 24) };
            byte[] bCalCmd = { 0x0F, 0xA5 };
            byte[] bResult = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockWrite(0xE9, ref bStartAddrCmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockWrite(0xEA, ref bEndAddrCmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockWrite(0xEB, ref bCalCmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Thread.Sleep(20);
            ret = BlockRead(0xEC, ref bResult);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            result = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bResult[0], bResult[1]), SharedFormula.MAKEWORD(bResult[2], bResult[3]));
            return ret;
        }

        public UInt32 sw_calc(byte[] pdata, int start, int end, int bits)
        {
            int k = 0;
            byte bdata = 0;
            UInt32 newbit, newword, rl_crc, bit, crc = 0xFFFFFFFF;
            const UInt32 poly = 0x04C11DB6; //spec 04C1 1DB7h
            for (int j = start; j < end; j++)
            {
                bdata = pdata[j];
                for (int i = 0; i < bits; i++)
                {
                    newbit = (UInt32)(((crc >> 31) ^ ((bdata >> i) & 1)) & 1);
                    if (newbit != 0)
                        newword = poly;
                    else
                        newword = 0;
                    rl_crc = (crc << 1) | newbit;
                    crc = rl_crc ^ newword;
                }
            }
            rl_crc = 0;
            crc = ~crc;
            for (int i = 0; i < 32; i++)
            {
                k = 31 - i;
                bit = (crc >> i) & 1;
                rl_crc |= (UInt32)(bit << k);
            }
            return rl_crc;
        }
        public void PrintMessage(string info, TASKMessage msg)
        {
            msg.controlmsg.message = info;
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
        }
        #endregion
    }
}