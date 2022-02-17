using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cobra.Communication;
using Cobra.Common;
using System.Threading.Tasks;

namespace Cobra.SeaguIIPD
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
        private readonly object m_lock = new object();
        private Dictionary<string, string> m_Json_Options = null;
        private CCommunicateManager m_Interface = new CCommunicateManager();
        private struct EEPROM_DOWNLOAD_STRUCT
        {
            public UInt32 startAddr;
            public UInt32 endAddr;
            public int startPage;
            public int endPage;
            public int size;
        }
        private EEPROM_DOWNLOAD_STRUCT m_EpDownload_info;

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
        internal UInt32 BlockRead(UInt32 reg, ref byte[] buf, int len = 4)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(reg, ref buf, len);
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
        internal UInt32 BlockWrite(UInt32 reg, byte[] buf)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(reg, buf);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected byte crc8_calc(byte[] pdata, UInt16 n)
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
        protected byte XOR(byte[] pdata, UInt16 n)
        {
            byte crc = pdata[0];
            for (int i = 1; i < n; i++)
                crc ^= pdata[i];
            return crc;
        }

        protected UInt32 XOR32(byte[] pdata, UInt16 n)
        {
            UInt32 dwal = 0;
            UInt32[] wval_array = new UInt32[n / 4];
            for (int i = 0; i < n / 4; i++)
            {
                dwal = 0;
                for (int j = 0; j < 4; ++j)
                    dwal |= (UInt32)(pdata[j + i * 4] << (j * 8));//左移8位  
                wval_array[i] = dwal;
            }
            dwal = wval_array[0];
            for (int i = 1; i < wval_array.Length; i++)
                dwal ^= wval_array[i];
            return dwal;
        }
        protected UInt32 OnBlockRead(ref TASKMessage msg)
        {
            byte[] bdata = null;
            UInt32 address = 0, startAddr = 0, size = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            address = UInt32.Parse(options["Address"].Trim());
            startAddr = UInt32.Parse(options["StartAddr"].Trim());
            size = UInt32.Parse(options["Size"].Trim());
            ret = OnBlockRead(startAddr, ref bdata, (int)size);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Array.Copy(bdata, msg.flashData, bdata.Length);
            return ret;
        }
        internal UInt32 OnBlockRead(UInt32 reg, ref byte[] buf, int len)
        {
            UInt16 DataInLen = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] btmp = new byte[4];
            byte[] btmp32 = new byte[32 + 8];
            List<byte> m_bytes_List = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            m_bytes_List.Clear();
            if (len % 4 != 0) return ElementDefine.IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT;
            int cycle = len / 4;
            int rTimes = len / ElementDefine.BLOCK_OPERATION_BYTES;
            byte[] receivebuf = new byte[len + cycle];

            sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            for (int k = 0; k < rTimes; k++)
            {
                DataInLen = (UInt16)(ElementDefine.BLOCK_OPERATION_BYTES + ElementDefine.BLOCK_OPERATION_BYTES / 4);
                sendbuf[1] = (byte)(reg >> 24);
                sendbuf[2] = (byte)(reg >> 16);
                sendbuf[3] = (byte)(reg >> 8);
                sendbuf[4] = (byte)(reg);
                reg += (UInt16)ElementDefine.BLOCK_OPERATION_BYTES;
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.ReadDevice(sendbuf, ref btmp32, ref DataOutLen, DataInLen, 4)) //notice the last data 4.
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        Array.Copy(btmp32, 0, receivebuf, k * DataInLen, DataInLen);
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(2);
                }
            }
            if (len % ElementDefine.BLOCK_OPERATION_BYTES != 0)
            {
                DataInLen = (UInt16)(len % ElementDefine.BLOCK_OPERATION_BYTES + cycle);
                reg += (UInt16)(rTimes * ElementDefine.BLOCK_OPERATION_BYTES);
                sendbuf[1] = (byte)(reg >> 24);
                sendbuf[2] = (byte)(reg >> 16);
                sendbuf[3] = (byte)(reg >> 8);
                sendbuf[4] = (byte)(reg);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.ReadDevice(sendbuf, ref btmp32, ref DataOutLen, DataInLen, 4)) //notice the last data 4.
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        Array.Copy(btmp32, 0, receivebuf, rTimes * (ElementDefine.BLOCK_OPERATION_BYTES + ElementDefine.BLOCK_OPERATION_BYTES / 4), DataInLen);
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    Thread.Sleep(2);
                }
            }
            for (int j = 0; j < cycle; j++)
            {
                Array.Copy(receivebuf, j * 5, btmp, 0, btmp.Length);
                if (receivebuf[(j + 1) * 5 - 1] != XOR(btmp, 4))
                    return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                m_bytes_List.AddRange(btmp);
            }
            m_Interface.GetLastErrorCode(ref ret);
            buf = m_bytes_List.ToArray();
            return ret;
        }
        internal UInt32 OnBlockWrite(UInt32 reg, byte[] buf)
        {
            byte[] btmp32 = new byte[32 + 8];
            List<byte> m_bytes_List = new List<byte>();
            if (buf.Length % 4 != 0) return ElementDefine.IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT;
            int totalBytes = (buf.Length + buf.Length / 4);
            byte[] bdata = new byte[5];
            byte[] sendbuf = new byte[totalBytes];
            byte[] receivebuf = new byte[1];
            UInt16 wDataOutLength = 0;
            UInt16 wDataInLength = (ElementDefine.BLOCK_OPERATION_BYTES + ElementDefine.BLOCK_OPERATION_BYTES / 4);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int n = 0; n < buf.Length / 4; n++)
            {
                Array.Copy(buf, n * 4, bdata, 0, 4);
                bdata[4] = (byte)XOR(bdata, 4);
                Array.Copy(bdata, 0, sendbuf, n * bdata.Length, bdata.Length);
            }
            int rTimes = totalBytes / (ElementDefine.BLOCK_OPERATION_BYTES + ElementDefine.BLOCK_OPERATION_BYTES / 4);
            for (int k = 0; k < rTimes; k++)
            {
                m_bytes_List.Clear();
                m_bytes_List.Add((byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code);
                m_bytes_List.Add((byte)(reg >> 24));
                m_bytes_List.Add((byte)(reg >> 16));
                m_bytes_List.Add((byte)(reg >> 8));
                m_bytes_List.Add((byte)(reg));
                Array.Copy(sendbuf, k * wDataInLength, btmp32, 0, wDataInLength);
                m_bytes_List.AddRange(btmp32);
                if (reg != (ElementDefine.EEPROM_CTRL_PROG_FIFO_ADDR + ElementDefine.AHB2APB_BASIC_ADDR))
                    reg += (UInt16)ElementDefine.BLOCK_OPERATION_BYTES;
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.WriteDevice(m_bytes_List.ToArray(), ref receivebuf, ref wDataOutLength, (UInt16)(wDataInLength + 5 - 2)))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                    Thread.Sleep(2);
                }
            }
            if (totalBytes % (ElementDefine.BLOCK_OPERATION_BYTES + ElementDefine.BLOCK_OPERATION_BYTES / 4) != 0)
            {
                wDataInLength = (UInt16)(totalBytes % (ElementDefine.BLOCK_OPERATION_BYTES + ElementDefine.BLOCK_OPERATION_BYTES / 4));
                reg += (UInt16)(rTimes * ElementDefine.BLOCK_OPERATION_BYTES);
                m_bytes_List.Clear();
                m_bytes_List.Add((byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code);
                m_bytes_List.Add((byte)(reg >> 24));
                m_bytes_List.Add((byte)(reg >> 16));
                m_bytes_List.Add((byte)(reg >> 8));
                m_bytes_List.Add((byte)(reg));
                Array.Copy(sendbuf, rTimes * wDataInLength, btmp32, 0, wDataInLength);
                for (int i = 0; i < wDataInLength; i++)
                    m_bytes_List.Add(sendbuf[rTimes * wDataInLength + i]);
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (m_Interface.WriteDevice(m_bytes_List.ToArray(), ref receivebuf, ref wDataOutLength, (UInt16)(wDataInLength + 5 - 2)))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                    Thread.Sleep(2);
                }
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (msg.gm.sflname)
            {
                case "HexEditor":
                    GetEpDownloadInfo(msg);
                    ret = EEPROMPageErase(m_EpDownload_info.startPage, m_EpDownload_info.endPage, ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case "EEPROMConfig":
                    ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = EpTestCtrl(ElementDefine.EP_CHIP_ERASE_CMD);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.SOFT_RESET);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case "Port1SystemConfig":
                    ret = EEPROMPageErase(506, 506, ref msg);
                    break;
                case "Port2SystemConfig":
                    ret = EEPROMPageErase(507, 507, ref msg);
                    break;
                case "Port1BuckBoostConfig":
                    ret = EEPROMPageErase(504, 504, ref msg);
                    break;
                case "Port2BuckBoostConfig":
                    ret = EEPROMPageErase(505, 505, ref msg);
                    break;
                case "Port1PD2Config":
                    ret = EEPROMPageErase(500, 500, ref msg);
                    break;
                case "Port2PD2Config":
                    ret = EEPROMPageErase(501, 501, ref msg);
                    break;
                case "Port1PDConfig":
                    ret = EEPROMPageErase(502, 502, ref msg);
                    break;
                case "Port2PDConfig":
                    ret = EEPROMPageErase(503, 503, ref msg);
                    break;
            }
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 uaddress = 0;
            byte[] bval = null;
            UInt32 address = 0;
            UInt32 offset = 0, swCrc = 0, hwCrc = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> m_tmp_List = new List<byte>();

            List<byte> EpTrimReglist = new List<byte>();
            List<byte> OTPTrimReglist = new List<byte>();
            List<byte> ExpertOTPReglist = new List<byte>();
            List<byte> Port1SysReglist = new List<byte>();
            List<byte> Port2SysReglist = new List<byte>();
            List<byte> Port1BuckBoostReglist = new List<byte>();
            List<byte> Port2BuckBoostReglist = new List<byte>();
            List<byte> Port1PD2Reglist = new List<byte>();
            List<byte> Port2PD2Reglist = new List<byte>();
            List<byte> Port1PDReglist = new List<byte>();
            List<byte> Port2PDReglist = new List<byte>();
            List<UInt32> BBCTRLReglist = new List<UInt32>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.ElementMask) == ElementDefine.VirtualElement)
                {
                    if(p.guid == ElementDefine.VirtualSWCRC)
                        CountOTPCRC();
                    continue;
                }
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
                    case ElementDefine.OTPTRIMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                OTPTrimReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.OTPExpertElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                ExpertOTPReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1SystemElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1SysReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2SystemElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2SysReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1BuckBoostElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1BuckBoostReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2BuckBoostElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2BuckBoostReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1PDElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1PDReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1PD2Element:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1PD2Reglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2PDElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2PDReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2PD2Element:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2PD2Reglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.BBCTRLSystemElement:
                    case ElementDefine.PDCTRLExpertElement:
                    case ElementDefine.ARMExpertElement:
                    case ElementDefine.APBExpertElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                uaddress = (UInt32)reg.u32Address;
                                BBCTRLReglist.Add(uaddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EpTrimReglist = EpTrimReglist.Distinct().ToList();
            ExpertOTPReglist = ExpertOTPReglist.Distinct().ToList();
            OTPTrimReglist = OTPTrimReglist.Distinct().ToList();
            Port1SysReglist = Port1SysReglist.Distinct().ToList();
            Port2SysReglist = Port2SysReglist.Distinct().ToList();
            Port1BuckBoostReglist = Port1BuckBoostReglist.Distinct().ToList();
            Port2BuckBoostReglist = Port2BuckBoostReglist.Distinct().ToList();
            Port1PDReglist = Port1PDReglist.Distinct().ToList();
            Port2PDReglist = Port2PDReglist.Distinct().ToList();
            Port1PD2Reglist = Port1PD2Reglist.Distinct().ToList();
            Port2PD2Reglist = Port2PD2Reglist.Distinct().ToList();
            BBCTRLReglist = BBCTRLReglist.Distinct().ToList();
            if (EpTrimReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_TRIM_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_TRIM_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (OTPTrimReglist.Count != 0)
            {
                address = ElementDefine.OTP_TRIM_PAGE_ADDR;
                ret = BlockRead(address, ref bval, 64);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_OTPRegImg[i].err = ret;
                    parent.m_OTPRegImg[i].val = bval[i];
                }
                for (int i = 0; i < 60; i++)
                {
                    if (!((i > 7) && (i < 16)))
                    {
                        if (parent.m_OTPRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_tmp_List.Add(0);
                        else
                            m_tmp_List.Add((byte)parent.m_OTPRegImg[i].val);
                    }
                }
                swCrc = XOR32(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count);
                hwCrc = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bval[60], bval[61]), SharedFormula.MAKEWORD(bval[62], bval[63]));
                FolderMap.WriteFile(string.Format("CRC:HW-{0:X4},SW-{1:X4}", hwCrc, swCrc));
                if (hwCrc != swCrc) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
            }
            if (ExpertOTPReglist.Count != 0)
            {
                address = ElementDefine.OTP_TRIM_PAGE_ADDR;
                ret = BlockRead(address, ref bval, 64);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_OTPRegImg[i].err = ret;
                    parent.m_OTPRegImg[i].val = bval[i];
                }
            }
            if (Port1SysReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P1SYS_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P1SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port2SysReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P2SYS_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P2SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port1BuckBoostReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P1BuckBoost_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P1BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port2BuckBoostReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P2BuckBoost_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P2BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port1PDReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P1PD_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P1PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port1PD2Reglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P1PD2_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P1PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port2PDReglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P2PD_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P2PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (Port2PD2Reglist.Count != 0)
            {
                address = ElementDefine.EEPROM_P2PD2_PAGE_ADDR;
                offset = (ElementDefine.EEPROM_P2PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                ret = EpBlockRead(address, ref bval, ref msg, 128);
                for (int i = 0; i < bval.Length; i++)
                {
                    parent.m_EFRegImg[i + offset].err = ret;
                    parent.m_EFRegImg[i + offset].val = bval[i];
                }
            }
            if (BBCTRLReglist.Count != 0)
            {
                foreach (UInt32 addr in BBCTRLReglist)
                {
                    ret = BlockRead(addr, ref bval, 4);
                    if (parent.m_OpImage_Dic.ContainsKey(addr))
                    {
                        parent.m_OpImage_Dic[addr].err = ret;
                        parent.m_OpImage_Dic[addr].wval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bval[0], bval[1]), SharedFormula.MAKEWORD(bval[2], bval[3]));
                    }
                }
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 uaddress = 0, address = 0, xor32 = 0;
            byte[] bval = null;
            byte[] btemp = new byte[4];
            List<byte> m_bytes_List = new List<byte>();
            List<byte> m_tmp_List = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OTPTrimReglist = new List<byte>();
            List<byte> EpTrimReglist = new List<byte>();
            List<byte> ExpertOTPReglist = new List<byte>();
            List<byte> Port1SysReglist = new List<byte>();
            List<byte> Port2SysReglist = new List<byte>();
            List<byte> Port1BuckBoostReglist = new List<byte>();
            List<byte> Port2BuckBoostReglist = new List<byte>();
            List<byte> Port1PDReglist = new List<byte>();
            List<byte> Port2PDReglist = new List<byte>();
            List<byte> Port1PD2Reglist = new List<byte>();
            List<byte> Port2PD2Reglist = new List<byte>();
            List<UInt32> BBCTRLReglist = new List<UInt32>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMTRIMElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                EpTrimReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.OTPTRIMElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                OTPTrimReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.OTPExpertElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                ExpertOTPReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1SystemElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1SysReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2SystemElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2SysReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1BuckBoostElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1BuckBoostReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2BuckBoostElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2BuckBoostReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1PDElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1PDReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2PDElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2PDReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port1PD2Element:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port1PD2Reglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.Port2PD2Element:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                Port2PD2Reglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.BBCTRLSystemElement:
                    case ElementDefine.PDCTRLExpertElement:
                    case ElementDefine.ARMExpertElement:
                    case ElementDefine.APBExpertElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                uaddress = (UInt32)reg.u32Address;
                                BBCTRLReglist.Add(uaddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            OTPTrimReglist = OTPTrimReglist.Distinct().ToList();
            ExpertOTPReglist = ExpertOTPReglist.Distinct().ToList();
            EpTrimReglist = EpTrimReglist.Distinct().ToList();
            Port1SysReglist = Port1SysReglist.Distinct().ToList();
            Port2SysReglist = Port2SysReglist.Distinct().ToList();
            Port1BuckBoostReglist = Port1BuckBoostReglist.Distinct().ToList();
            Port2BuckBoostReglist = Port2BuckBoostReglist.Distinct().ToList();
            Port1PDReglist = Port1PDReglist.Distinct().ToList();
            Port2PDReglist = Port2PDReglist.Distinct().ToList();
            Port1PD2Reglist = Port1PD2Reglist.Distinct().ToList();
            Port2PD2Reglist = Port2PD2Reglist.Distinct().ToList();
            BBCTRLReglist = BBCTRLReglist.Distinct().ToList();
            m_bytes_List.Clear();
            m_tmp_List.Clear();
            if (EpTrimReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_TRIM_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 120))
                    {
                        xor32 = XOR32(m_bytes_List.ToArray(), 120);
                        m_bytes_List.Add((byte)xor32);
                        m_bytes_List.Add((byte)(xor32 >> 8));
                        m_bytes_List.Add((byte)(xor32 >> 16));
                        m_bytes_List.Add((byte)(xor32 >> 24));
                        i = (address + 123);
                        continue;
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (OTPTrimReglist.Count != 0)
            {
                ret = BlockRead(0x4000000C, ref bval, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bval[0] = 0x01;
                bval[1] = 0x0A5;
                ret = BlockWrite(0x4000000C, bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please keep the VBAT=12V, add the power supply to COMP1 pin to 7.6V±0.1V(50mA), then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
                address = ElementDefine.OTP_TRIM_PAGE_ADDR;
                for (UInt32 i = address; i < (address + ElementDefine.OTP_PAGE_SIZE); i++)
                {
                    if (i == (address + 60))
                    {
                        xor32 = XOR32(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count);
                        m_bytes_List.Add((byte)xor32);
                        m_bytes_List.Add((byte)(xor32 >> 8));
                        m_bytes_List.Add((byte)(xor32 >> 16));
                        m_bytes_List.Add((byte)(xor32 >> 24));
                        i = (address + 63);
                        continue;
                    }
                    if (parent.m_OTPRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_OTPRegImg[i].val);

                    if (!((i > (address + 7) && i < (address + 16))))
                    {
                        if (parent.m_OTPRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_tmp_List.Add(0);
                        else
                            m_tmp_List.Add((byte)parent.m_OTPRegImg[i].val);
                    }
                }
                ret = BlockRead(0x40008004, ref bval, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bval[0] = 0x22;
                bval[1] = 0x01;
                bval[2] = 0x5A;
                bval[3] = 0x00;
                ret = BlockWrite(0x40008004, bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = BlockWrite(address, m_bytes_List.ToArray());
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                Thread.Sleep(10);
                ret = BlockRead(0x40000010, ref bval, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bval[0] = 0x01;
                ret = BlockWrite(0x40000010, bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
            }
            if (ExpertOTPReglist.Count != 0)
            {
                ret = BlockRead(0x4000000C, ref bval, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bval[0] = 0x01;
                bval[1] = 0x0A5;
                ret = BlockWrite(0x4000000C, bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = BlockRead(0x40008004, ref bval, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bval[0] = 0x22;
                bval[1] = 0x01;
                bval[2] = 0x5A;
                bval[3] = 0x00;
                ret = BlockWrite(0x40008004, bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in ExpertOTPReglist)
                {
                    for (int i = 0; i < 4; i++)
                        btemp[i] = (byte)parent.m_OTPRegImg[badd + i].val;
                    ret = BlockWrite(badd, btemp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(10);
                }
                ret = BlockRead(0x40000010, ref bval, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                bval[0] = 0x01;
                ret = BlockWrite(0x40000010, bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port1SysReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P1SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 42))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 42));
                        continue;
                    }
                    if (i == (address + 98))
                    {
                        m_bytes_List.Add(crc8_calc(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count));
                        continue;
                    }
                    if ((i >= (address + 64)) && (i < (address + 98)))
                    {
                        if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_tmp_List.Add(0);
                        else
                            m_tmp_List.Add((byte)parent.m_EFRegImg[i].val);
                    }

                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port2SysReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P2SYS_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 42))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 42));
                        continue;
                    }
                    if (i == (address + 98))
                    {
                        m_bytes_List.Add(crc8_calc(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count));
                        continue;
                    }
                    if ((i >= (address + 64)) && (i < (address + 98)))
                    {
                        if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_tmp_List.Add(0);
                        else
                            m_tmp_List.Add((byte)parent.m_EFRegImg[i].val);
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port1BuckBoostReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P1BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 26))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 26));
                        continue;
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port2BuckBoostReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P2BuckBoost_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 26))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 26));
                        continue;
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port1PDReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P1PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 126))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 126));
                        continue;
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port2PDReglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P2PD_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 126))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 126));
                        continue;
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port1PD2Reglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P1PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 62))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 62));
                        continue;
                    }
                    if (i == (address + 126))
                    {
                        m_bytes_List.Add(crc8_calc(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count));
                        continue;
                    }
                    if ((i >= (address + 64)) && (i < (address + 126)))
                    {
                        if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_tmp_List.Add(0);
                        else
                            m_tmp_List.Add((byte)parent.m_EFRegImg[i].val);
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (Port2PD2Reglist.Count != 0)
            {
                address = (ElementDefine.EEPROM_P2PD2_PAGE_ADDR - ElementDefine.EEPROM_START_ADDR);
                for (UInt32 i = address; i < (address + ElementDefine.EP_PAGE_SIZE); i++)
                {
                    if (i == (address + 62))
                    {
                        m_bytes_List.Add(crc8_calc(m_bytes_List.ToArray(), 62));
                        continue;
                    }
                    if (i == (address + 126))
                    {
                        m_bytes_List.Add(crc8_calc(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count));
                        continue;
                    }
                    if ((i >= (address + 64)) && (i < (address + 126)))
                    {
                        if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_tmp_List.Add(0);
                        else
                            m_tmp_List.Add((byte)parent.m_EFRegImg[i].val);
                    }
                    if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_bytes_List.Add(0);
                    else
                        m_bytes_List.Add((byte)parent.m_EFRegImg[i].val);
                }
                ret = EpBlockWrite(address, m_bytes_List.ToArray(), ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (BBCTRLReglist.Count != 0)
            {
                foreach (UInt32 addr in BBCTRLReglist)
                {
                    m_bytes_List.Clear();
                    if (parent.m_OpImage_Dic.ContainsKey(addr))
                    {
                        m_bytes_List.Add((byte)parent.m_OpImage_Dic[addr].wval);
                        m_bytes_List.Add((byte)(parent.m_OpImage_Dic[addr].wval >> 8));
                        m_bytes_List.Add((byte)(parent.m_OpImage_Dic[addr].wval >> 16));
                        m_bytes_List.Add((byte)(parent.m_OpImage_Dic[addr].wval >> 24));
                        ret = BlockWrite(addr, m_bytes_List.ToArray());
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                }
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

            List<Parameter> OTPTrimParamlist = new List<Parameter>();
            List<Parameter> EpTrimParamlist = new List<Parameter>();
            List<Parameter> Port1SysParamlist = new List<Parameter>();
            List<Parameter> Port2SysParamlist = new List<Parameter>();
            List<Parameter> Port1BuckBoostParamlist = new List<Parameter>();
            List<Parameter> Port2BuckBoostParamlist = new List<Parameter>();
            List<Parameter> Port1PDParamlist = new List<Parameter>();
            List<Parameter> Port2PDParamlist = new List<Parameter>();
            List<Parameter> Port1PD2Paramlist = new List<Parameter>();
            List<Parameter> Port2PD2Paramlist = new List<Parameter>();
            List<Parameter> BBCTRLParamlist = new List<Parameter>();
            List<Parameter> OTPExpertParamlist = new List<Parameter>();
            List<Parameter> VirtualParamlist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMTRIMElement:
                        {
                            if (p == null) break;
                            EpTrimParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.OTPTRIMElement:
                        {
                            if (p == null) break;
                            OTPTrimParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1SystemElement:
                        {
                            if (p == null) break;
                            Port1SysParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2SystemElement:
                        {
                            if (p == null) break;
                            Port2SysParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1BuckBoostElement:
                        {
                            if (p == null) break;
                            Port1BuckBoostParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2BuckBoostElement:
                        {
                            if (p == null) break;
                            Port2BuckBoostParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1PDElement:
                        {
                            if (p == null) break;
                            Port1PDParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2PDElement:
                        {
                            if (p == null) break;
                            Port2PDParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1PD2Element:
                        {
                            if (p == null) break;
                            Port1PD2Paramlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2PD2Element:
                        {
                            if (p == null) break;
                            Port2PD2Paramlist.Add(p);
                            break;
                        }
                    case ElementDefine.OTPExpertElement:
                        {
                            if (p == null) break;
                            OTPExpertParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.BBCTRLSystemElement:
                    case ElementDefine.PDCTRLExpertElement:
                    case ElementDefine.ARMExpertElement:
                    case ElementDefine.APBExpertElement:
                        {
                            if (p == null) break;
                            BBCTRLParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        {
                            param = p;
                            m_parent.Hex2Physical(ref param);
                            break;
                        }
                    case ElementDefine.VirtualElement:
                        {
                            if (p == null) break;
                            VirtualParamlist.Add(p);
                            break;
                        }
                }
            }

            if (EpTrimParamlist.Count != 0)
            {
                for (int i = 0; i < EpTrimParamlist.Count; i++)
                {
                    param = (Parameter)EpTrimParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (OTPTrimParamlist.Count != 0)
            {
                for (int i = 0; i < OTPTrimParamlist.Count; i++)
                {
                    param = (Parameter)OTPTrimParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (Port1SysParamlist.Count != 0)
            {
                for (int i = 0; i < Port1SysParamlist.Count; i++)
                {
                    param = (Parameter)Port1SysParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x00070000) param.phydata = 16;
                    if (param.guid == 0x00070008) param.phydata = 42;
                    if (param.guid == 0x00072810) param.phydata = 10;
                    if (param.guid == 0x00072818) param.phydata = 85;
                    if (param.guid == 0x00074000) param.phydata = 17;
                    if (param.guid == 0x00074008) param.phydata = 34;
                    if (param.guid == 0x00076018) param.phydata = 85;
                }
            }
            if (Port2SysParamlist.Count != 0)
            {
                for (int i = 0; i < Port2SysParamlist.Count; i++)
                {
                    param = (Parameter)Port2SysParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x000A0000) param.phydata = 32;
                    if (param.guid == 0x000A0008) param.phydata = 42;
                    if (param.guid == 0x000A2810) param.phydata = 10;
                    if (param.guid == 0x000A2818) param.phydata = 85;
                    if (param.guid == 0x000A4000) param.phydata = 33;
                    if (param.guid == 0x000A4008) param.phydata = 34;
                    if (param.guid == 0x000A6018) param.phydata = 85;
                }
            }
            if (Port1BuckBoostParamlist.Count != 0)
            {
                for (int i = 0; i < Port1BuckBoostParamlist.Count; i++)
                {
                    param = (Parameter)Port1BuckBoostParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x00080000) param.phydata = 18;
                    if (param.guid == 0x00080008) param.phydata = 26;
                    if (param.guid == 0x00081818) param.phydata = 85;
                }
            }
            if (Port2BuckBoostParamlist.Count != 0)
            {
                for (int i = 0; i < Port2BuckBoostParamlist.Count; i++)
                {
                    param = (Parameter)Port2BuckBoostParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x000B0000) param.phydata = 34;
                    if (param.guid == 0x000B0008) param.phydata = 26;
                    if (param.guid == 0x000B1818) param.phydata = 85;
                }
            }
            if (Port1PDParamlist.Count != 0)
            {
                for (int i = 0; i < Port1PDParamlist.Count; i++)
                {
                    param = (Parameter)Port1PDParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x00090000) param.phydata = 24;
                    if (param.guid == 0x00090008) param.phydata = 126;
                    if (param.guid == 0x00097C18) param.phydata = 85;
                }
            }
            if (Port2PDParamlist.Count != 0)
            {
                for (int i = 0; i < Port2PDParamlist.Count; i++)
                {
                    param = (Parameter)Port2PDParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x000C0000) param.phydata = 40;
                    if (param.guid == 0x000C0008) param.phydata = 126;
                    if (param.guid == 0x000C7C18) param.phydata = 85;
                }
            }
            if (Port1PD2Paramlist.Count != 0)
            {
                for (int i = 0; i < Port1PD2Paramlist.Count; i++)
                {
                    param = (Parameter)Port1PD2Paramlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x00090000) param.phydata = 24;
                    if (param.guid == 0x00090008) param.phydata = 126;
                    if (param.guid == 0x00097C18) param.phydata = 85;
                }
            }
            if (Port2PD2Paramlist.Count != 0)
            {
                for (int i = 0; i < Port2PD2Paramlist.Count; i++)
                {
                    param = (Parameter)Port2PD2Paramlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                    if (param.guid == 0x000C0000) param.phydata = 40;
                    if (param.guid == 0x000C0008) param.phydata = 126;
                    if (param.guid == 0x000C7C18) param.phydata = 85;
                }
            }
            if (BBCTRLParamlist.Count != 0)
            {
                for (int i = 0; i < BBCTRLParamlist.Count; i++)
                {
                    param = (Parameter)BBCTRLParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (OTPExpertParamlist.Count != 0)
            {
                for (int i = 0; i < OTPExpertParamlist.Count; i++)
                {
                    param = (Parameter)OTPExpertParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (VirtualParamlist.Count != 0)
            {
                for (int i = 0; i < VirtualParamlist.Count; i++)
                {
                    param = (Parameter)VirtualParamlist[i];
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

            List<Parameter> OTPTrimParamlist = new List<Parameter>();
            List<Parameter> EpTrimParamlist = new List<Parameter>();
            List<Parameter> Port1SysParamlist = new List<Parameter>();
            List<Parameter> Port2SysParamlist = new List<Parameter>();
            List<Parameter> Port1BuckBoostParamlist = new List<Parameter>();
            List<Parameter> Port2BuckBoostParamlist = new List<Parameter>();
            List<Parameter> Port1PDParamlist = new List<Parameter>();
            List<Parameter> Port2PDParamlist = new List<Parameter>();
            List<Parameter> Port1PD2Paramlist = new List<Parameter>();
            List<Parameter> Port2PD2Paramlist = new List<Parameter>();
            List<Parameter> BBCTRLParamlist = new List<Parameter>();
            List<Parameter> OTPExpertParamlist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMTRIMElement:
                        {
                            if (p == null) break;
                            EpTrimParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.OTPTRIMElement:
                        {
                            if (p == null) break;
                            OTPTrimParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1SystemElement:
                        {
                            if (p == null) break;
                            Port1SysParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2SystemElement:
                        {
                            if (p == null) break;
                            Port2SysParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1BuckBoostElement:
                        {
                            if (p == null) break;
                            Port1BuckBoostParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2BuckBoostElement:
                        {
                            if (p == null) break;
                            Port2BuckBoostParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1PDElement:
                        {
                            if (p == null) break;
                            Port1PDParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2PDElement:
                        {
                            if (p == null) break;
                            Port2PDParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port1PD2Element:
                        {
                            if (p == null) break;
                            Port1PD2Paramlist.Add(p);
                            break;
                        }
                    case ElementDefine.Port2PD2Element:
                        {
                            if (p == null) break;
                            Port2PD2Paramlist.Add(p);
                            break;
                        }
                    case ElementDefine.OTPExpertElement:
                        {
                            if (p == null) break;
                            OTPExpertParamlist.Add(p);
                            break;
                        }
                    case ElementDefine.BBCTRLSystemElement:
                    case ElementDefine.PDCTRLExpertElement:
                    case ElementDefine.ARMExpertElement:
                    case ElementDefine.APBExpertElement:
                        {
                            if (p == null) break;
                            BBCTRLParamlist.Add(p);
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
            if (EpTrimParamlist.Count != 0)
            {
                for (int i = 0; i < EpTrimParamlist.Count; i++)
                {
                    param = (Parameter)EpTrimParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            if (OTPTrimParamlist.Count != 0)
            {
                for (int i = 0; i < OTPTrimParamlist.Count; i++)
                {
                    param = (Parameter)OTPTrimParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port1SysParamlist.Count != 0)
            {
                for (int i = 0; i < Port1SysParamlist.Count; i++)
                {
                    param = (Parameter)Port1SysParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x00070000) param.phydata = 16;
                    if (param.guid == 0x00070008) param.phydata = 42;
                    if (param.guid == 0x00072810) param.phydata = 10;
                    if (param.guid == 0x00072818) param.phydata = 85;
                    if (param.guid == 0x00074000) param.phydata = 17;
                    if (param.guid == 0x00074008) param.phydata = 34;
                    if (param.guid == 0x00076018) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port2SysParamlist.Count != 0)
            {
                for (int i = 0; i < Port2SysParamlist.Count; i++)
                {
                    param = (Parameter)Port2SysParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x000A0000) param.phydata = 32;
                    if (param.guid == 0x000A0008) param.phydata = 42;
                    if (param.guid == 0x000A2810) param.phydata = 10;
                    if (param.guid == 0x000A2818) param.phydata = 85;
                    if (param.guid == 0x000A4000) param.phydata = 33;
                    if (param.guid == 0x000A4008) param.phydata = 34;
                    if (param.guid == 0x000A6018) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port1BuckBoostParamlist.Count != 0)
            {
                for (int i = 0; i < Port1BuckBoostParamlist.Count; i++)
                {
                    param = (Parameter)Port1BuckBoostParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x00080000) param.phydata = 18;
                    if (param.guid == 0x00080008) param.phydata = 26;
                    if (param.guid == 0x00081818) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port2BuckBoostParamlist.Count != 0)
            {
                for (int i = 0; i < Port2BuckBoostParamlist.Count; i++)
                {
                    param = (Parameter)Port2BuckBoostParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x000B0000) param.phydata = 34;
                    if (param.guid == 0x000B0008) param.phydata = 26;
                    if (param.guid == 0x000B1818) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port1PDParamlist.Count != 0)
            {
                for (int i = 0; i < Port1PDParamlist.Count; i++)
                {
                    param = (Parameter)Port1PDParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x00090000) param.phydata = 24;
                    if (param.guid == 0x00090008) param.phydata = 126;
                    if (param.guid == 0x00097C18) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port2PDParamlist.Count != 0)
            {
                for (int i = 0; i < Port2PDParamlist.Count; i++)
                {
                    param = (Parameter)Port2PDParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x000C0000) param.phydata = 40;
                    if (param.guid == 0x000C0008) param.phydata = 126;
                    if (param.guid == 0x000C7C18) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port1PD2Paramlist.Count != 0)
            {
                for (int i = 0; i < Port1PD2Paramlist.Count; i++)
                {
                    param = (Parameter)Port1PD2Paramlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x00090000) param.phydata = 24;
                    if (param.guid == 0x00090008) param.phydata = 126;
                    if (param.guid == 0x00097C18) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (Port2PD2Paramlist.Count != 0)
            {
                for (int i = 0; i < Port2PD2Paramlist.Count; i++)
                {
                    param = (Parameter)Port2PD2Paramlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    if (param.guid == 0x000C0000) param.phydata = 40;
                    if (param.guid == 0x000C0008) param.phydata = 126;
                    if (param.guid == 0x000C7C18) param.phydata = 85;
                    m_parent.Physical2Hex(ref param);
                }
            }
            if (BBCTRLParamlist.Count != 0)
            {
                for (int i = 0; i < BBCTRLParamlist.Count; i++)
                {
                    param = (Parameter)BBCTRLParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            if (OTPExpertParamlist.Count != 0)
            {
                for (int i = 0; i < OTPExpertParamlist.Count; i++)
                {
                    param = (Parameter)OTPExpertParamlist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            return ret;
        }
        public UInt32 ReadDevice(ref TASKMessage msg)
        {
            UInt32 dwal = 0;
            byte[] btmp = new byte[4];
            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
            string[] scmd = json["command"].Split(' ');
            byte[] bcmd = new byte[scmd.Length * 4];
            for (int i = 0; i < scmd.Length; i++)
            {
                dwal = UInt32.Parse(scmd[i], System.Globalization.NumberStyles.HexNumber);
                bcmd[i * 4] = (byte)(dwal >> 24);
                bcmd[i * 4 + 1] = (byte)(dwal >> 16);
                bcmd[i * 4 + 2] = (byte)(dwal >> 8);
                bcmd[i * 4 + 3] = (byte)(dwal);
            }

            UInt16 wDataInLength = UInt16.Parse(json["length"]);
            wDataInLength = (UInt16)(wDataInLength + wDataInLength / 4);
            byte[] yDataOut = new byte[wDataInLength];
            byte[] yDataIn = new byte[bcmd.Length + 1];
            yDataIn[0] = baddr;
            Array.Copy(bcmd, 0, yDataIn, 1, bcmd.Length);

            int nPackage = 0;
            UInt16 wDataOutLength = 0;
            UInt16 wDataInWrite = (UInt16)bcmd.Length;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (!m_Interface.ReadDevice(yDataIn, ref yDataOut, ref wDataOutLength, wDataInLength, wDataInWrite))
                ret = LibErrorCode.IDS_ERR_COM_COM_TIMEOUT;
            else
            {
                if (yDataOut.Length % 5 != 0)
                {
                    ret = ElementDefine.IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT;
                    msg.flashData = null;
                }
                else
                {
                    nPackage = yDataOut.Length / 5;
                    msg.flashData = new byte[nPackage * 4];
                    for (int i = 0; i < nPackage; i++)
                    {
                        Array.Copy(yDataOut, i * 5, btmp, 0, btmp.Length);
                        Array.Copy(btmp, 0, msg.flashData, i * 4, btmp.Length);
                        if (yDataOut[(i + 1) * 5 - 1] != XOR(btmp, 4))
                        {
                            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                            break;
                        }
                    }
                }
            }
            return ret;
        }
        public UInt32 WriteDevice(ref TASKMessage msg)
        {
            UInt32 dwal = 0;
            byte[] bdata = new byte[5];
            string[] stmp = new string[4];
            UInt16 wDataOutLength = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
            string[] scmd = json["command"].Trim().Split(' ');
            byte[] bcmd = new byte[scmd.Length * 4];
            for (int i = 0; i < scmd.Length; i++)
            {
                dwal = UInt32.Parse(scmd[i], System.Globalization.NumberStyles.HexNumber);
                bcmd[i * 4] = (byte)(dwal >> 24);
                bcmd[i * 4 + 1] = (byte)(dwal >> 16);
                bcmd[i * 4 + 2] = (byte)(dwal >> 8);
                bcmd[i * 4 + 3] = (byte)(dwal);
            }
            string[] sdata = json["data"].Trim().Split(' ');
            UInt16 wDataInLength = (UInt16)(bcmd.Length + 1 + sdata.Length * 4 + sdata.Length);
            byte[] yDataOut = new byte[1];
            byte[] yDataIn = new byte[wDataInLength];
            yDataIn[0] = (byte)baddr;
            Array.Copy(bcmd, 0, yDataIn, 1, bcmd.Length);

            for (int n = 0; n < sdata.Length; n++)
            {
                dwal = UInt32.Parse(sdata[n], System.Globalization.NumberStyles.HexNumber);
                bdata[0] = (byte)(dwal);
                bdata[1] = (byte)(dwal >> 8);
                bdata[2] = (byte)(dwal >> 16);
                bdata[3] = (byte)(dwal >> 24);
                bdata[4] = (byte)XOR(bdata, 4);
                Array.Copy(bdata, 0, yDataIn, bcmd.Length + 1 + n * bdata.Length, bdata.Length);
            }
            if (!m_Interface.WriteDevice(yDataIn, ref yDataOut, ref wDataOutLength, (UInt16)(wDataInLength - 2)))
                m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            UInt16 udata = 0;
            UInt32 total_addr = 0;
            byte[] total_image = null;
            byte[] bupload = null;
            byte[] bimage = null;
            byte[] bArray = null;
            string[] bsArray = null;
            List<byte> m_bytes_List = new List<byte>();
            UInt32 hwResult = 0, Result = 0;
            Parameter param = null;
            double d1, d2, d3, d4;

            UInt32 address = 0, xor32 = 0;
            List<byte> m_tmp_List = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            #region 支持command指令
            #region Project
            if (m_Json_Options.ContainsValue("HexEditor"))
            {
                GetEpDownloadInfo(msg);
                switch (m_Json_Options["TM_COMMAND"])
                {
                    case "Download":
                        ret = EEPROMPageErase(m_EpDownload_info.startPage, m_EpDownload_info.endPage, ref msg);
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        foreach (MemoryControl mc in msg.bufferList)
                        {
                            if (total_addr < mc.endAddress)
                                total_addr = (UInt32)(mc.startAddress + mc.buffer.Length);
                        }
                        total_image = new byte[total_addr];
                        foreach (MemoryControl mc in msg.bufferList)
                        {
                            Array.Copy(mc.buffer, 0, total_image, mc.startAddress, mc.buffer.Length);
                        }
                        m_bytes_List.Clear();
                        for (int i = (int)(ElementDefine.EEPROM_START_ADDR + m_EpDownload_info.startAddr); i < (ElementDefine.EEPROM_START_ADDR + m_EpDownload_info.endAddr); i++)
                        {/*
                            if (i >= msg.flashData.Length) m_bytes_List.Add(0);
                            else m_bytes_List.Add(msg.flashData[i]);*/

                            if (i >= total_image.Length) m_bytes_List.Add(0);
                            else m_bytes_List.Add(total_image[i]);
                        }
                        bimage = m_bytes_List.ToArray();
                        if (m_EpDownload_info.startAddr == 0)
                        {
                            PrintMessage("Count SW CRC from 0xC4.", msg);
                            Result = sw_calc(bimage, 0xC4, bimage.Length, 8);
                            bimage[0xC0] = (byte)Result;
                            bimage[0xC1] = (byte)(Result >> 8);
                            bimage[0xC2] = (byte)(Result >> 16);
                            bimage[0xC3] = (byte)(Result >> 24);
                        }
                        PrintMessage("Block write EEPROM.", msg);
                        ret = EpBlockWriteForHexDownload(m_EpDownload_info.startAddr, bimage, ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        if (m_EpDownload_info.startAddr != 0)
                            Result = hwResult = 0;
                        else
                        {
                            ret = EnableCRCClock(true);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            ret = hw_calc(ElementDefine.EEPROM_START_ADDR, (UInt32)(ElementDefine.EEPROM_START_ADDR + bimage.Length - 4), ref hwResult);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            Result = sw_calc(bimage, 0, bimage.Length, 8);
                            PrintMessage(string.Format("The SW CRC is 0x{0:x4},HW CRC is 0x{1:x4}", Result, hwResult), msg);
                        }
                        if (Boolean.Parse(m_Json_Options["vap_Cb"]))
                        {
                            PrintMessage("Begin upload data..", msg);
                            ret = EpBlockRead((ElementDefine.EEPROM_START_ADDR + m_EpDownload_info.startAddr), ref bupload, ref msg, m_EpDownload_info.size);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            PrintMessage("Begin verify data..", msg);
                            for (int n = 0; n < m_EpDownload_info.size; n++)
                            {
                                if (bimage[n] == bupload[n]) continue;
                                PrintMessage(string.Format("At 0x{0:x4},donwload is 0x{1:x4},upload is 0x{2:x4}", n, bimage[n], bupload[n]), msg);
                                ret = LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                            }
                        }
                        else
                            if (Result != hwResult) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                        PrintMessage("Reset CPU", msg);
                        ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.SOFT_RESET);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_RUN);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    case "ERASE":
                        PrintMessage(string.Format("EEPROM page erase from {0} to {1}.", m_EpDownload_info.startPage, m_EpDownload_info.endPage), msg);
                        ret = EEPROMPageErase(m_EpDownload_info.startPage, m_EpDownload_info.endPage, ref msg);
                        break;
                    case "Upload":
                        Array.Clear(ElementDefine.m_ROM_EP_Buf, 0, ElementDefine.m_ROM_EP_Buf.Length);
                        ret = EpBlockRead((ElementDefine.EEPROM_START_ADDR + m_EpDownload_info.startAddr), ref bupload, ref msg, m_EpDownload_info.size);
                        Array.Copy(bupload, 0, ElementDefine.m_ROM_EP_Buf, m_EpDownload_info.startAddr, m_EpDownload_info.size);
                        msg.flashData = ElementDefine.m_ROM_EP_Buf;
                        break;
                }
            }
            #endregion
            switch ((ElementDefine.COBRA_COMMAND_MODE)msg.sub_task)
            {
                #region DEBUG
                case ElementDefine.COBRA_COMMAND_MODE.DEBUG_READ:
                    {
                        ret = BlockRead(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.DEBUG_WRITE:
                    {
                        //ret = BlockWrite(ref msg);
                        break;
                    }
                #endregion

                #region Robot
                case ElementDefine.COBRA_COMMAND_MODE.Robot_Read:
                    {
                        for (int i = 0; i < msg.task_parameterlist.parameterlist.Count; i++)
                        {
                            param = msg.task_parameterlist.parameterlist[i];
                            if (param == null) continue;
                            Reg reg = param.reglist["Low"];
                            if (reg == null) continue;
                            reg.bitsnumber = 16;
                            ret = BlockRead(reg.u32Address, ref bArray, 4);
                            param.errorcode = ret;
                            param.u32hexdata = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bArray[0], bArray[1]), SharedFormula.MAKEWORD(bArray[2], bArray[3]));
                        }
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.Robot_Write:
                    {
                        for (int i = 0; i < msg.task_parameterlist.parameterlist.Count; i++)
                        {
                            param = msg.task_parameterlist.parameterlist[i];
                            if (param == null) continue;
                            Reg reg = param.reglist["Low"];
                            if (reg == null) continue;
                            m_bytes_List.Clear();
                            m_bytes_List.Add((byte)param.u32hexdata);
                            m_bytes_List.Add((byte)(param.u32hexdata >> 8));
                            m_bytes_List.Add((byte)(param.u32hexdata >> 16));
                            m_bytes_List.Add((byte)(param.u32hexdata >> 24));
                            ret = BlockWrite(reg.u32Address, m_bytes_List.ToArray());
                            param.errorcode = ret;
                        }
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.Robot_Formula:
                    {
                        msg.sub_task_json = SharedAPI.SerializeDictionaryToJsonString(parent.m_formula_dic);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.Robot_Count:
                    {
                        if (!parent.m_formula_dic.ContainsKey(msg.funName))
                        {
                            PrintMessage(string.Format("Formula{0} is not exist,please check.", msg.funName), msg);
                            break;
                        }
                        if (msg.task_parameterlist.parameterlist.Count == 0)
                        {
                            PrintMessage("No parameters to refer", msg);
                            break;
                        }
                        param = msg.task_parameterlist.parameterlist[0];
                        if (string.IsNullOrEmpty(param.sphydata))
                        {
                            PrintMessage("Please input the parameters", msg);
                            break;
                        }
                        bsArray = param.sphydata.Split(',');
                        if ((bsArray == null) | (bsArray.Length != parent.m_formula_dic[msg.funName].Item1))
                        {
                            PrintMessage(string.Format("The parameters {0} are illegal,please check.", param.sphydata), msg);
                            break;
                        }
                        switch (msg.funName)
                        {
                            case "OPCS":
                                double gain = 0, VOS = 0;
                                if (!double.TryParse(bsArray[0], out d1))
                                {
                                    PrintMessage("Failed to convert the parameter Vcs1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[1], out d2))
                                {
                                    PrintMessage("Failed to convert the parameter Vcs2.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[2], out d3))
                                {
                                    PrintMessage("Failed to convert the parameter V1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[3], out d4))
                                {
                                    PrintMessage("Failed to convert the parameter V2.", msg);
                                    break;
                                }
                                gain = (d2 - d1) / (d4 - d3);
                                VOS = (d1 * d4 - d2 * d3) / (d4 - d3);
                                param.itemlist.Clear();
                                param.itemlist.Add(string.Format("{0:F3},  {1:F3}", gain, VOS));
                                param.itemlist = param.itemlist;
                                break;
                            case "ADC":
                                double adc_gain = 0, adc_offset = 0;
                                if (!double.TryParse(bsArray[0], out d1))
                                {
                                    PrintMessage("Failed to convert the parameter V1(mV).", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[1], out d2))
                                {
                                    PrintMessage("Failed to convert the parameter V2(mV).", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[2], out d3))
                                {
                                    PrintMessage("Failed to convert the parameter ADC1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[3], out d4))
                                {
                                    PrintMessage("Failed to convert the parameter ADC2.", msg);
                                    break;
                                }
                                adc_gain = (d2 - d1) * 4096.0 / 0.6 / (d4 - d3);
                                adc_offset = (d2 * d3 - d1 * d4) / (d2 - d1);
                                if (adc_offset < 0)
                                    udata = (UInt16)(Math.Pow(2, 12) + adc_offset);
                                else
                                    udata = (UInt16)adc_offset;
                                param.itemlist.Clear();
                                param.itemlist.Add(string.Format("{0:F1}(0x{1:X4}), {2:F1}(0x{3:X4})", adc_gain, (UInt16)adc_gain, adc_offset, udata));
                                param.itemlist = param.itemlist;
                                break;
                            case "DACV":
                                double dacv_gain = 0, dacv_offset = 0;
                                if (!double.TryParse(bsArray[0], out d1))
                                {
                                    PrintMessage("Failed to convert the parameter VOUT1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[1], out d2))
                                {
                                    PrintMessage("Failed to convert the parameter VOUT2.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[2], out d3))
                                {
                                    PrintMessage("Failed to convert the parameter DACV1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[3], out d4))
                                {
                                    PrintMessage("Failed to convert the parameter DACV2.", msg);
                                    break;
                                }
                                dacv_gain = (d4 - d3) / (d2 - d1) * 4096.0 * 6.0;
                                dacv_offset = (d1 * d4 - d2 * d3) / (d4 - d3) / 6;
                                if (dacv_offset < 0)
                                    udata = (UInt16)(Math.Pow(2, 12) + dacv_offset);
                                else
                                    udata = (UInt16)dacv_offset;
                                param.itemlist.Clear();
                                param.itemlist.Add(string.Format("{0:F1}(0x{1:X4}), {2:F1}(0x{3:X4})", dacv_gain, (UInt16)dacv_gain, dacv_offset, udata));
                                param.itemlist = param.itemlist;
                                break;
                            case "DACC1":
                                double dacc_gain = 0, dacc_offset = 0;
                                if (!double.TryParse(bsArray[0], out d1))
                                {
                                    PrintMessage("Failed to convert the parameter ViOUT1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[1], out d2))
                                {
                                    PrintMessage("Failed to convert the parameter ViOUT2.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[2], out d3))
                                {
                                    PrintMessage("Failed to convert the parameter DACC1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[3], out d4))
                                {
                                    PrintMessage("Failed to convert the parameter DACC2.", msg);
                                    break;
                                }
                                dacc_gain = (d4 - d3) / (d2 - d1) * 4096.0 * 9.6 / 200.0;
                                dacc_offset = (d1 * d4 - d2 * d3) / (d4 - d3) * 200.0 / 9.6;
                                if (dacc_offset < 0)
                                    udata = (UInt16)(Math.Pow(2, 10) + dacc_offset);
                                else
                                    udata = (UInt16)dacc_offset;
                                param.itemlist.Clear();
                                param.itemlist.Add(string.Format("{0:F1}(0x{1:X4}), {2:F1}(0x{3:X4})", dacc_gain, (UInt16)dacc_gain, dacc_offset, udata));
                                param.itemlist = param.itemlist;
                                break;
                            case "DACC2":
                                if (!double.TryParse(bsArray[0], out d1))
                                {
                                    PrintMessage("Failed to convert the parameter IOUT1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[1], out d2))
                                {
                                    PrintMessage("Failed to convert the parameter IOUT2.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[2], out d3))
                                {
                                    PrintMessage("Failed to convert the parameter DACC1.", msg);
                                    break;
                                }
                                if (!double.TryParse(bsArray[3], out d4))
                                {
                                    PrintMessage("Failed to convert the parameter DACC2.", msg);
                                    break;
                                }
                                dacc_gain = (d4 - d3) / (d2 - d1) * 4096.0 * 9.6;
                                dacc_offset = (d1 * d4 - d2 * d3) / (d4 - d3) / 9.6;
                                if (dacc_offset < 0)
                                    udata = (UInt16)(Math.Pow(2, 10) + dacc_offset);
                                else
                                    udata = (UInt16)dacc_offset;
                                param.itemlist.Clear();
                                param.itemlist.Add(string.Format("{0:F1}(0x{1:X4}), {2:F1}(0x{3:X4})", dacc_gain, (UInt16)dacc_gain, dacc_offset, udata));
                                param.itemlist = param.itemlist;
                                break;
                        }
                        break;
                    }
                #endregion
            }
            #endregion
            return ret;
        }

        public void PrintMessage(string info, TASKMessage msg)
        {
            msg.controlmsg.message = info;
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
        }

        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            int ival = 0;
            byte[] dwal = null;
            string shwversion = String.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = BlockRead(0x40000000, ref dwal);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = 0;
            ival = 0;
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
            ival = 0;
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)dwal[1];

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (SharedFormula.HiByte(type) != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if (((SharedFormula.LoByte(type) & 0x30) >> 4) != deviceinfor.hwversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if ((SharedFormula.LoByte(type) & 0x03) != (deviceinfor.hwsubversion >> 4))
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

        #region EEPROM
        public void GetEpDownloadInfo(TASKMessage msg)
        {
            m_Json_Options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            switch (m_Json_Options["selectCB"].Trim())
            {
                case "0-63.875K":
                    m_EpDownload_info.startAddr = 0;
                    m_EpDownload_info.endAddr = 0xFF80;
                    m_EpDownload_info.startPage = 0;
                    m_EpDownload_info.endPage = 510;
                    m_EpDownload_info.size = 0xFF80;
                    break;
                case "0-63.5K":
                    m_EpDownload_info.startAddr = 0;
                    m_EpDownload_info.endAddr = 0xFE00;
                    m_EpDownload_info.startPage = 0;
                    m_EpDownload_info.endPage = 507;
                    m_EpDownload_info.size = 0xFE00;
                    break;
                case "0-62K":
                    m_EpDownload_info.startAddr = 0;
                    m_EpDownload_info.endAddr = 0xF800;
                    m_EpDownload_info.startPage = 0;
                    m_EpDownload_info.endPage = 495;
                    m_EpDownload_info.size = 0xF800;
                    break;
                case "0-31K":
                    m_EpDownload_info.startAddr = 0;
                    m_EpDownload_info.endAddr = 0x7C00;
                    m_EpDownload_info.startPage = 0;
                    m_EpDownload_info.endPage = 199;
                    m_EpDownload_info.size = 0x7C00;
                    break;
                case "62K-63.5K":
                    m_EpDownload_info.startAddr = 0xF800;
                    m_EpDownload_info.endAddr = 0xFE00;
                    m_EpDownload_info.startPage = 495;
                    m_EpDownload_info.endPage = 507;
                    m_EpDownload_info.size = 0x600;
                    break;
                case "63.5K-63.875K":
                    m_EpDownload_info.startAddr = 0xFE00;
                    m_EpDownload_info.endAddr = 0xFF80;
                    m_EpDownload_info.startPage = 507;
                    m_EpDownload_info.endPage = 510;
                    m_EpDownload_info.size = 384;
                    break;
            }
            ElementDefine.m_ROM_EP_Buf = new byte[m_EpDownload_info.endAddr];
        }
        public UInt32 ChipOperation(ElementDefine.CHIP_OPERA_MODE mode)
        {
            byte bClk20M_Sel = 0;
            byte[] tval = null;
            byte[] rval = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
            byte[] bval = new byte[4] { 0x01, 0x00, 0x00, 0x00 };
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (mode)
            {
                case ElementDefine.CHIP_OPERA_MODE.SOFT_RESET:
                    ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.AHB2APB_SOFT_RESET, bval);
                    ret = BlockRead(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.AHB2APB_SOFT_RESET, ref tval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    if (tval[0] != 0) return ElementDefine.IDS_ERR_DEM_SW_RESET;
                    break;
                case ElementDefine.CHIP_OPERA_MODE.CPU_HOLD:
                    ret = BlockRead(0x50018100, ref tval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    bClk20M_Sel = (byte)(tval[1] & 0x03);
                    if ((bClk20M_Sel == 0x01) | (bClk20M_Sel == 0x02))
                    {
                        tval[1] &= 0xF3;
                        tval[1] |= (byte)(bClk20M_Sel << 2);
                        ret = BlockWrite(0x50018100, tval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    ret = BlockRead(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.AHB2APB_CPU_SLEEP, ref tval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    tval[0] &= 0xFE;
                    ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.AHB2APB_CPU_SLEEP, tval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.AHB2APB_CPU_HOLD, bval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.CHIP_OPERA_MODE.CPU_RUN:
                    ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.AHB2APB_CPU_HOLD, rval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
            }
            return ret;
        }

        public UInt32 EpBlockRead(UInt32 reg, ref byte[] buf, ref TASKMessage msg, int len = 4)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            PrintMessage("Hold CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = BlockRead(reg, ref buf, len);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            PrintMessage("Reset CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.SOFT_RESET);
            return ret;
        }
        public UInt32 EpBlockWrite(UInt32 sAddr, byte[] buffer, ref TASKMessage msg)
        {
            List<byte> m_bytes_List = new List<byte>();
            int cycle = (int)(buffer.Length / ElementDefine.EP_FIFO_SIZE);
            int remainder = (int)(buffer.Length % ElementDefine.EP_FIFO_SIZE);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            PrintMessage("Hold CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < cycle; i++)
            {
                Array.Clear(ElementDefine.m_FIFO_Buf, 0, ElementDefine.m_FIFO_Buf.Length);
                ret = EpOperaAddr(sAddr);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                Array.Copy(buffer, i * ElementDefine.m_FIFO_Buf.Length, ElementDefine.m_FIFO_Buf, 0, ElementDefine.m_FIFO_Buf.Length);
                ret = EpProgFIFO(ElementDefine.m_FIFO_Buf);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = EpOperaMode((UInt32)ElementDefine.EEPROM_OPERA_MODE.PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                sAddr += ElementDefine.EP_FIFO_SIZE;
                PrintMessage(string.Format("Downloaded {0:f2}%", i * 100.00 / cycle), msg);
            }
            if (remainder != 0)
            {
                m_bytes_List.Clear();
                ret = EpOperaAddr(sAddr);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (int i = cycle * ElementDefine.m_FIFO_Buf.Length; i < buffer.Length; i++)
                    m_bytes_List.Add(buffer[i]);
                ret = EpProgFIFO(m_bytes_List.ToArray());
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = EpOperaMode((UInt32)ElementDefine.EEPROM_OPERA_MODE.PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            PrintMessage("Downloaded 100%", msg);
            Thread.Sleep(2);
            PrintMessage("Reset CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.SOFT_RESET);
            return ret;
        }
        public UInt32 EpBlockWriteForHexDownload(UInt32 sAddr, byte[] buffer, ref TASKMessage msg) //After Count CRC to reset
        {
            List<byte> m_bytes_List = new List<byte>();
            int cycle = (int)(buffer.Length / ElementDefine.EP_FIFO_SIZE);
            int remainder = (int)(buffer.Length % ElementDefine.EP_FIFO_SIZE);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            PrintMessage("Hold CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < cycle; i++)
            {
                Array.Clear(ElementDefine.m_FIFO_Buf, 0, ElementDefine.m_FIFO_Buf.Length);
                ret = EpOperaAddr(sAddr);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                Array.Copy(buffer, i * ElementDefine.m_FIFO_Buf.Length, ElementDefine.m_FIFO_Buf, 0, ElementDefine.m_FIFO_Buf.Length);
                ret = EpProgFIFO(ElementDefine.m_FIFO_Buf);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = EpOperaMode((UInt32)ElementDefine.EEPROM_OPERA_MODE.PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                sAddr += ElementDefine.EP_FIFO_SIZE;
                PrintMessage(string.Format("Downloaded {0:f2}%", i * 100.00 / cycle), msg);
            }
            if (remainder != 0)
            {
                m_bytes_List.Clear();
                ret = EpOperaAddr(sAddr);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (int i = cycle * ElementDefine.m_FIFO_Buf.Length; i < buffer.Length; i++)
                    m_bytes_List.Add(buffer[i]);
                ret = EpProgFIFO(m_bytes_List.ToArray());
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = EpOperaMode((UInt32)ElementDefine.EEPROM_OPERA_MODE.PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            PrintMessage("Downloaded 100%", msg);
            return ret;
        }
        public UInt32 EpOperaMode(UInt32 mode)
        {
            bool bbusy = true;
            byte[] tmp = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            tmp[0] = (byte)(mode);
            tmp[1] = (byte)(mode >> 8);
            tmp[2] = (byte)(mode >> 16);
            tmp[3] = (byte)(mode >> 24);
            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.EEPROM_CTRL_OPERA_MODE, tmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = EEPROMBusy(ref bbusy);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (bbusy) return ElementDefine.IDS_ERR_DEM_EEPROM_BUSY;
            return ret;
        }
        public UInt32 EpOperaAddr(UInt32 address)
        {
            byte[] tmp = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            address >>= 2;
            tmp[0] = (byte)(address);
            tmp[1] = (byte)(address >> 8);
            tmp[2] = (byte)(address >> 16);
            tmp[3] = (byte)(address >> 24);
            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.EEPROM_CTRL_OPERA_ADDR, tmp);
            return ret;
        }
        public UInt32 EpProgFIFO(byte[] buf)
        {
            bool bbusy = true;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.EEPROM_CTRL_PROG_FIFO_ADDR, buf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = EEPROMBusy(ref bbusy);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (bbusy) return ElementDefine.IDS_ERR_DEM_EEPROM_BUSY;
            return ret;
        }
        public UInt32 EpTestCtrl(UInt32 tCtrl)
        {
            bool bbusy = true;
            byte[] tmp = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            tmp[0] = (byte)tCtrl;
            tmp[1] = (byte)(tCtrl >> 8);
            tmp[2] = (byte)(tCtrl >> 16);
            tmp[3] = (byte)(tCtrl >> 24);
            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.EEPROM_CTRL_TEST_CTRL_ADDR, tmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = EpTestCtrlBusy(ref bbusy);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (bbusy) return ElementDefine.IDS_ERR_DEM_EEPROM_BUSY;
            return ret;
        }
        public UInt32 EpTestCtrlBusy(ref bool bval)
        {
            bval = true;
            byte[] btmp = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(2);
                ret = BlockRead(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.EEPROM_CTRL_TEST_CTRL_ADDR, ref btmp, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((btmp[0] & 0x02) == 0x00)
                {
                    bval = false;
                    break;
                }
            }
            return ret;
        }
        public UInt32 EEPROMPageErase(int sPageNum, int ePageNum, ref TASKMessage msg)
        {
            UInt32 erase_basic_addr = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            PrintMessage("Hold CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.CPU_HOLD);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = sPageNum; i <= ePageNum; i++)
            {
                erase_basic_addr = (UInt32)(i * ElementDefine.EP_PAGE_SIZE);
                ret = EpOperaAddr(erase_basic_addr);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = EpOperaMode((UInt32)ElementDefine.EEPROM_OPERA_MODE.PAGE_ERASE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                PrintMessage(string.Format("Erased {0:f2}%", i * 100.00 / ePageNum), msg);
            }

            PrintMessage("Reset CPU", msg);
            ret = ChipOperation(ElementDefine.CHIP_OPERA_MODE.SOFT_RESET);
            return ret;
        }
        public UInt32 EEPROMBusy(ref bool bval)
        {
            bval = true;
            byte[] btmp = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(2);
                ret = BlockRead(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.EEPROM_CTRL_OPERA_MODE, ref btmp, 4);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((btmp[0] & 0x01) == 0x00)
                {
                    bval = false;
                    break;
                }
            }
            return ret;
        }
        #endregion

        #region CRC Check
        public UInt32 EnableCRCClock(bool bval)
        {
            byte[] val = null;
            UInt32 addr = 0x4000002C;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = BlockRead(addr, ref val, 4);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            val[2] &= 0xFE;
            val[2] |= (byte)(bval ? 1 : 0);
            ret = BlockWrite(addr, val);
            return ret;
        }

        public UInt32 hw_calc(UInt32 startAddr, UInt32 endAddr, ref UInt32 result)
        {
            byte[] bStartAddrCmd = { (byte)startAddr, (byte)(startAddr >> 8), (byte)(startAddr >> 16), (byte)(startAddr >> 24) };
            byte[] bEndAddrCmd = { (byte)endAddr, (byte)(endAddr >> 8), (byte)(endAddr >> 16), (byte)(endAddr >> 24) };
            byte[] bCalCmd = { 0x01, 0x00, 0x00, 0x00 };
            byte[] bResult = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.CRC_START_ADDR, bStartAddrCmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.CRC_END_ADDR, bEndAddrCmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockWrite(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.CRC_CTRL_ADDR, bCalCmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER * 3; i++)
            {
                Thread.Sleep(2);
                ret = BlockRead(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.CRC_CTRL_ADDR, ref bResult);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if (bResult[0] == 0x02)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = ElementDefine.IDS_ERR_DEM_HW_CRC_TIMEOUT;
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = BlockRead(ElementDefine.AHB2APB_BASIC_ADDR + ElementDefine.CRC_RESULT_ADDR, ref bResult);
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

        public UInt32 CountOTPCRC()
        {
            UInt32 address = 0, xor32 = 0;
            List<byte> m_tmp_List = new List<byte>();
            List<byte> m_bytes_List = new List<byte>();

            address = ElementDefine.OTP_TRIM_PAGE_ADDR;
            for (UInt32 i = address; i < (address + ElementDefine.OTP_PAGE_SIZE); i++)
            {
                if (i == (address + 60))
                {
                    xor32 = XOR32(m_tmp_List.ToArray(), (UInt16)m_tmp_List.Count);
                    m_bytes_List.Add((byte)xor32);
                    m_bytes_List.Add((byte)(xor32 >> 8));
                    m_bytes_List.Add((byte)(xor32 >> 16));
                    m_bytes_List.Add((byte)(xor32 >> 24));
                    i = (address + 63);
                    continue;
                }
                if (parent.m_OTPRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    m_bytes_List.Add(0);
                else
                    m_bytes_List.Add((byte)parent.m_OTPRegImg[i].val);

                if (!((i > (address + 7) && i < (address + 16))))
                {
                    if (parent.m_OTPRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        m_tmp_List.Add(0);
                    else
                        m_tmp_List.Add((byte)parent.m_OTPRegImg[i].val);
                }
            }
            parent.m_OTPRegImg[0x10 * 4].val = (byte)(xor32);
            parent.m_OTPRegImg[0x10 * 4].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
            parent.m_OTPRegImg[0x10 * 4 + 1].val = (byte)(xor32 >> 8);
            parent.m_OTPRegImg[0x10 * 4 + 1].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
            parent.m_OTPRegImg[0x10 * 4 + 2].val = (byte)(xor32 >> 16);
            parent.m_OTPRegImg[0x10 * 4 + 2].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
            parent.m_OTPRegImg[0x10 * 4 + 3].val = (byte)(xor32 >> 24);
            parent.m_OTPRegImg[0x10 * 4 + 3].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return xor32;
        }
        #endregion
    }
}