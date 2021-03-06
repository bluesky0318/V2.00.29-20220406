using System;
using System.Collections.Generic;
using System.Linq;
//#define DEBUG_LOG
//#define DATA_PACKAGE_LEN 32

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using O2Micro.Cobra.Communication;
using O2Micro.Cobra.Common;
using System.Text.RegularExpressions;

namespace O2Micro.Cobra.I2CFWBigsur
{
    public class BigSur6 : O2Chip
    {
        //父对象保存
        private DEMBehaviorManage m_parent;
        public DEMBehaviorManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        private object m_lock = new object();
        private Dictionary<string, string> m_Json_Options = null;

        public BigSur6(object pParent)
        {
            parent = (DEMBehaviorManage)pParent;
        }

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
                if (parent.m_chip_version < 0xB2) //Bootloader chagned after B2
                {
                    if (((i / 4) & 0x07) != 0x07)
                        crc = (crc << 8) ^ crcTable[(crc >> 24) ^ bytes[i]];
                }
                else
                    crc = (crc << 8) ^ crcTable[(crc >> 24) ^ bytes[i]];
            }

            return crc;
        }

        #region 基础服务功能设计
        public override UInt32 Erase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = HandShake();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            switch (parent.m_flash_operate)
            {
                case ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_A:
                    {
                        ret = parent.WriteWord(0xFC, 0xA50F);
                        break;
                    }
                case ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_B:
                    ret = parent.WriteWord(0xF6, 0xA50F);
                    break;
            }
            ret = EndProgram();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ResetChip();
            return ret;
        }

        public override UInt32 Download(ref TASKMessage msg)
        {
            UInt32 dwCRC = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            switch (m_Json_Options["selectCB"])
            {
                case "FlashA":
                    parent.m_flash_operate = ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_A;
                    break;
                case "FlashB":
                    parent.m_flash_operate = ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_B;
                    break;
            }

            ret = HandShake();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = MainBlockWrite(ref msg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = CRC32Check(ref dwCRC);//获取CRC值
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (dwCRC == crc32_calc(msg.flashData))
            {//CRC PASS or Fail
                msg.controlmsg.message = "Pass to do crc check..";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            }
            else
            {
                msg.controlmsg.message = "Fail to do crc check..";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
            }


            ret = EndProgram();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ResetChip();
            return ret;
        }

        public override UInt32 Upload(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            switch (m_Json_Options["selectCB"])
            {
                case "FlashA":
                    parent.m_flash_operate = ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_A;
                    break;
                case "FlashB":
                    parent.m_flash_operate = ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_B;
                    break;
            }

            ret = HandShake();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = MainBlockRead(ref msg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = EndProgram();
            return ret;
        }
        #endregion

        #region 系统功能
        public UInt32 MainBlockWrite(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (parent.m_flash_operate)
            {
                case ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_A:
                    {
                        ret = parent.WriteDWord(0xF9, ElementDefine.MTPStartAddress);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = FlashABlockWrite(ref msg);
                        break;

                    }
                case ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_B:
                    ret = FlashBBlockWrite(ref msg);
                    break;
            }
            return ret;
        }

        #region BlockWrite子函数
        public UInt32 FlashABlockWrite(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            StringBuilder sb = new StringBuilder();
            byte[] receivebuf = new byte[1];
            byte[] sendbuf = new byte[2 + ElementDefine.THIRTYTWO_BLOCK_OPERATION];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.THIRTYTWO_BLOCK_OPERATION) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xF7;
                    Array.Copy(msg.flashData, fAddr, sendbuf, 2, ElementDefine.THIRTYTWO_BLOCK_OPERATION);

                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.THIRTYTWO_BLOCK_OPERATION))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            return ret;
        }

        public UInt32 FlashBBlockWrite(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            StringBuilder sb = new StringBuilder();
            byte[] receivebuf = new byte[1];
            byte[] sendbuf = new byte[2 + ElementDefine.THIRTYTWO_BLOCK_OPERATION];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.THIRTYTWO_BLOCK_OPERATION) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xF3;
                    sendbuf[2] = 0x20;
                    sendbuf[3] = SharedFormula.LoByte((UInt16)SharedFormula.LoWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));
                    sendbuf[4] = SharedFormula.HiByte((UInt16)SharedFormula.LoWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));
                    sendbuf[5] = SharedFormula.LoByte((UInt16)SharedFormula.HiWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));
                    sendbuf[6] = SharedFormula.HiByte((UInt16)SharedFormula.HiWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));

                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 5))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }
                    Array.Clear(sendbuf, 0, sendbuf.Length);
                    Array.Clear(receivebuf, 0, receivebuf.Length);
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xF5;
                    Array.Copy(msg.flashData, fAddr, sendbuf, 2, ElementDefine.THIRTYTWO_BLOCK_OPERATION);

                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.THIRTYTWO_BLOCK_OPERATION))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            return ret;
        }
        #endregion

        public UInt32 MainBlockRead(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (parent.m_flash_operate)
            {
                case ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_A:
                    {
                        ret = parent.WriteDWord(0xF9, ElementDefine.MTPStartAddress);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = FlashABlockRead(ref msg);

                        break;
                    }
                case ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_B:
                    ret = FlashBBlockRead(ref msg);
                    break;
            }
            return ret;
        }

        #region BlockRead 子函数
        public UInt32 FlashABlockRead(ref TASKMessage msg)
        {
            StringBuilder strB = new StringBuilder();
            string temp = string.Empty;
            UInt16 DataOutLen = 4;
            byte[] receivebuf = new byte[ElementDefine.DWORD_OPERATION_BYTES];
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.DWORD_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xFB;
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.DWORD_OPERATION_BYTES))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        Thread.Sleep(10);
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Array.Copy(receivebuf, 0, msg.flashData, fAddr, ElementDefine.DWORD_OPERATION_BYTES);
                }
            }
            return ret;
        }

        public UInt32 FlashBBlockRead(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            StringBuilder sb = new StringBuilder();
            byte[] receivebuf = new byte[2 + ElementDefine.THIRTYTWO_BLOCK_OPERATION];
            byte[] sendbuf = new byte[ElementDefine.THIRTYTWO_BLOCK_OPERATION];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.THIRTYTWO_BLOCK_OPERATION) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {

                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xF3;
                    sendbuf[2] = 0x20;
                    sendbuf[3] = SharedFormula.LoByte((UInt16)SharedFormula.LoWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));
                    sendbuf[4] = SharedFormula.HiByte((UInt16)SharedFormula.LoWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));
                    sendbuf[5] = SharedFormula.LoByte((UInt16)SharedFormula.HiWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));
                    sendbuf[6] = SharedFormula.HiByte((UInt16)SharedFormula.HiWord((int)(ElementDefine.FLASHBStartAddress + fAddr)));

                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 5))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }

                    Array.Clear(sendbuf, 0, sendbuf.Length);
                    Array.Clear(receivebuf, 0, receivebuf.Length);

                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0xF4;
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.THIRTYTWO_BLOCK_OPERATION))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        Thread.Sleep(10);
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Array.Copy(receivebuf, 0, msg.flashData, fAddr, ElementDefine.THIRTYTWO_BLOCK_OPERATION);
                }
            }
            return ret;
        }
        #endregion

        private UInt32 ResetChip()
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = 3;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
                try
                {
                    sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                }
                catch (System.Exception ex)
                {
                    return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                }
                sendbuf[1] = 0xF0;
                sendbuf[2] = 0x0F;
                sendbuf[3] = 0xA5;
                sendbuf[4] = 0x9C;

                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(60);
                }
            }
            return ret;
        }

        private UInt32 HandShake()
        {
            UInt16 wval = 0xA50F;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = parent.WriteWord(0xF8, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                Thread.Sleep(2);
                ret = parent.ReadWord(0xF8, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (wval == 0x5AF0)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                else
                    ret = ElementDefine.IDS_ERR_DEM_HANDSHAKE;
            }

            if (parent.m_flash_operate == ElementDefine.COBRA_FLASH_OPERATE.COBRA_FLASH_B)
                ret = parent.WriteWord(0xF1, 0xA50F);
            return ret;
        }

        private UInt32 EndProgram()
        {
            UInt16 wval = 0xA50F;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = parent.WriteWord(0xFE, wval);
            return ret;
        }

        private UInt32 CRC32Check(ref UInt32 dwCRC)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            ret = parent.ReadDWord(0xFA, ref dwCRC);
            return ret;
        }

        private UInt32 EraseCheck(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            switch (m_Json_Options["eaf_Cb"])
            {
                case "true":
                    ret = Erase(ref msg);
                    break;
                case "false":
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
            }
            return ret;
        }

        private UInt32 CompareCheck(ref TASKMessage msg)
        {
            int len = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            switch (m_Json_Options["vap_Cb"])
            {
                case "true":
                    len = msg.flashData.Length;
                    Array.Clear(ElementDefine.interBuffer, 0, ElementDefine.interBuffer.Length);
                    Array.Copy(msg.flashData, ElementDefine.interBuffer, len);

                    msg.controlmsg.message = "Begin upload data..";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                    ret = HandShake();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = MainBlockRead(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = EndProgram();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    msg.controlmsg.message = "Begin verify data..";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                    for (int n = 0; n < len; n++)
                    {
                        if (msg.flashData[n] == ElementDefine.interBuffer[n]) continue;
                        return LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                    }
                    break;
                case "false":
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
            }
            return ret;
        }
        #endregion
    }
}
