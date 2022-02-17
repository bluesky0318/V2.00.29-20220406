using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;
using Cobra.Communication;

namespace Cobra.OZ9316
{
    public struct PSW_ZONE
    {
        public UInt16 wdata;
        public bool bdata;
    };

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

        private PSW_ZONE m_ATESecPW;
        private PSW_ZONE m_ATEPrimPW;
        private PSW_ZONE m_UserSecPW;

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

        #region 寄存器操作
        #region 寄存器通用子级操作
        byte[] CRC8Table = {0x00, 0x07, 0x0E, 0x09, 0x1C, 0x1B, 0x12, 0x15, 0x38, 0x3F, 0x36, 0x31, 0x24, 0x23, 0x2A, 0x2D, 
		                0x70, 0x77, 0x7E, 0x79, 0x6C, 0x6B, 0x62, 0x65, 0x48, 0x4F, 0x46, 0x41, 0x54, 0x53, 0x5A, 0x5D, 
		                0xE0, 0xE7, 0xEE, 0xE9, 0xFC, 0xFB, 0xF2, 0xF5, 0xD8, 0xDF, 0xD6, 0xD1, 0xC4, 0xC3, 0xCA, 0xCD, 
		                0x90, 0x97, 0x9E, 0x99, 0x8C, 0x8B, 0x82, 0x85, 0xA8, 0xAF, 0xA6, 0xA1, 0xB4, 0xB3, 0xBA, 0xBD,
		                0xC7, 0xC0, 0xC9, 0xCE, 0xDB, 0xDC, 0xD5, 0xD2, 0xFF, 0xF8, 0xF1, 0xF6, 0xE3, 0xE4, 0xED, 0xEA, 
		                0xB7, 0xB0, 0xB9, 0xBE, 0xAB, 0xAC, 0xA5, 0xA2, 0x8F, 0x88, 0x81, 0x86, 0x93, 0x94, 0x9D, 0x9A, 
		                0x27, 0x20, 0x29, 0x2E, 0x3B, 0x3C, 0x35, 0x32, 0x1F, 0x18, 0x11, 0x16, 0x03, 0x04, 0x0D, 0x0A, 
		                0x57, 0x50, 0x59, 0x5E, 0x4B, 0x4C, 0x45, 0x42, 0x6F, 0x68, 0x61, 0x66, 0x73, 0x74, 0x7D, 0x7A, 
		                0x89, 0x8E, 0x87, 0x80, 0x95, 0x92, 0x9B, 0x9C, 0xB1, 0xB6, 0xBF, 0xB8, 0xAD, 0xAA, 0xA3, 0xA4, 
		                0xF9, 0xFE, 0xF7, 0xF0, 0xE5, 0xE2, 0xEB, 0xEC, 0xC1, 0xC6, 0xCF, 0xC8, 0xDD, 0xDA, 0xD3, 0xD4, 
		                0x69, 0x6E, 0x67, 0x60, 0x75, 0x72, 0x7B, 0x7C, 0x51, 0x56, 0x5F, 0x58, 0x4D, 0x4A, 0x43, 0x44, 
		                0x19, 0x1E, 0x17, 0x10, 0x05, 0x02, 0x0B, 0x0C, 0x21, 0x26, 0x2F, 0x28, 0x3D, 0x3A, 0x33, 0x34, 
		                0x4E, 0x49, 0x40, 0x47, 0x52, 0x55, 0x5C, 0x5B, 0x76, 0x71, 0x78, 0x7F, 0x6A, 0x6D, 0x64, 0x63, 
		                0x3E, 0x39, 0x30, 0x37, 0x22, 0x25, 0x2C, 0x2B, 0x06, 0x01, 0x08, 0x0F, 0x1A, 0x1D, 0x14, 0x13, 
		                0xAE, 0xA9, 0xA0, 0xA7, 0xB2, 0xB5, 0xBC, 0xBB, 0x96, 0x91, 0x98, 0x9F, 0x8A, 0x8D, 0x84, 0x83, 
		                0xDE, 0xD9, 0xD0, 0xD7, 0xC2, 0xC5, 0xCC, 0xCB, 0xE6, 0xE1, 0xE8, 0xEF, 0xFA, 0xFD, 0xF4, 0xF3};

        protected byte crc8_calc(ref byte[] p, UInt16 counter)
        {
            UInt16 index = 0;
            byte crc8 = 0xFF;
            for (; counter > 0; counter--)
            {
                crc8 = CRC8Table[crc8 ^ p[index]];
                index++;
            }
            return (crc8);
        }

        protected UInt32 OnReadWord(byte cmd, byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnSPI_ReadCmd(cmd, reg, ref pval);
            return ret;
        }

        protected UInt32 OnWriteWord(byte cmd, byte reg, UInt16 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnSPI_WriteCmd(cmd, reg, val);
            return ret;
        }

        protected UInt32 OnSPI_ReadCmd(byte Cmd_Len, byte reg, ref UInt16 pWval)
        {
            int wlen = 0, blen = 0, slen = 0;
            UInt16 DataOutLen = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            blen = (int)(Cmd_Len & 0x0F) + 1;
            wlen = blen * 2;
            slen = ElementDefine.CMD_SECTION_SIZE + wlen; //PEC

            byte[] rdbuf = new byte[slen];
            byte[] wrbuf = new byte[slen];

            wrbuf[0] = Cmd_Len;
            wrbuf[1] = reg;

            for (int i = 2; i < slen; i++) wrbuf[i] = 0;
            for (int k = 0; k < ElementDefine.SPI_RETRY_COUNT; k++)
            {
                if (m_Interface.WriteDevice(wrbuf, ref rdbuf, ref DataOutLen, (ushort)(slen - 2)))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }

            if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                if (rdbuf[slen - 1] != crc8_calc(ref rdbuf, (UInt16)(slen - 1)))
                    ret = LibErrorCode.IDS_ERR_SPI_CRC_CHECK;
                else if (rdbuf[1] != wrbuf[1])
                    ret = LibErrorCode.IDS_ERR_SPI_DATA_MISMATCH;
                else if (rdbuf[0] != wrbuf[0])
                    ret = LibErrorCode.IDS_ERR_SPI_CMD_MISMATCH;
                else
                {
                    for (int i = 0; i < (int)blen; i++)
                        pWval = SharedFormula.MAKEWORD(rdbuf[i * 2 + 3], rdbuf[(i + 1) * 2]);
                }
            }
            return ret;
        }

        protected UInt32 OnSPI_WriteCmd(byte Cmd_Len, byte reg, UInt16 wval)
        {
            UInt16 DataOutLen = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            byte[] rdbuf = new byte[ElementDefine.CMD_SECTION_SIZE + 2];
            byte[] wrbuf = new byte[ElementDefine.CMD_SECTION_SIZE + 2];

            wrbuf[0] = Cmd_Len;				// cmd 
            wrbuf[1] = reg;					// reg
            wrbuf[2] = SharedFormula.HiByte(wval);		//HByte
            wrbuf[3] = SharedFormula.LoByte(wval);		//LByte
            wrbuf[4] = crc8_calc(ref wrbuf, 4);	// pec

            for (int i = 0; i < ElementDefine.SPI_RETRY_COUNT; i++)
            {
                if (m_Interface.WriteDevice(wrbuf, ref rdbuf, ref DataOutLen, ElementDefine.CMD_SECTION_SIZE))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }

            if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                if (rdbuf[4] != wrbuf[4])
                    ret = LibErrorCode.IDS_ERR_SPI_CRC_CHECK;
                else if (rdbuf[3] != wrbuf[3])
                    ret = LibErrorCode.IDS_ERR_SPI_DATA_MISMATCH;
                else if (rdbuf[2] != wrbuf[2])
                    ret = LibErrorCode.IDS_ERR_SPI_DATA_MISMATCH;
                else if (rdbuf[1] != wrbuf[1])
                    ret = LibErrorCode.IDS_ERR_SPI_DATA_MISMATCH;
                else if (rdbuf[0] != wrbuf[0])
                    ret = LibErrorCode.IDS_ERR_SPI_CMD_MISMATCH;
            }
            return ret;
        }
        #endregion

        #region EEPROM寄存器操作
        #region EEPROM寄存器父级操作
        internal UInt32 EpReadWord(byte reg, ref UInt16 pval, byte len = 1)
        {
            byte cmd = ElementDefine.EP_RD_CMD;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWaitEpReady();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                cmd |= (byte)(len - 1);
                ret = OnReadWord(cmd, reg, ref pval);
            }
            return ret;
        }

        internal UInt32 EpWriteWord(byte reg, UInt16 pval)
        {
            byte cmd = ElementDefine.EP_WR_CMD;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWaitEpReady();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(cmd, reg, pval);
            }
            return ret;
        }

        protected UInt32 EpBlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnEpBlockErase();
            }
            return ret;
        }

        protected UInt32 EpBlockMap()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnEpBlockMap();
            }
            return ret;
        }

        protected UInt32 OpenEpATEData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnOpenEpATEData();
            }
            return ret;
        }

        protected UInt32 OpenEpUserData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnOpenEpUserData();
            }
            return ret;
        }

        protected UInt32 CloseEpATEData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnCloseEpATEData();
            }
            return ret;
        }

        protected UInt32 CloseEpUserData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnCloseEpUserData();
            }
            return ret;
        }
        #endregion

        #region EEPROM寄存器子级操作
        internal UInt32 OnEpReadWord(byte reg, ref UInt16 pval, byte len = 1)
        {
            byte cmd = ElementDefine.EP_RD_CMD;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnWaitEpReady();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            cmd |= (byte)(len - 1);
            return OnReadWord(cmd, reg, ref pval);
        }

        internal UInt32 OnEpWriteWord(byte reg, UInt16 pval)
        {
            byte cmd = ElementDefine.EP_WR_CMD;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnWaitEpReady();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(cmd, reg, pval);
            return ret;
        }

        internal UInt32 OnWaitEpReady()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < ElementDefine.SPI_RETRY_COUNT; i++)
            {
                ret = OnOpReadWord(ElementDefine.EE_BUSY_FINISH_REG, ref wval);
                if (LibErrorCode.IDS_ERR_SUCCESSFUL != ret) return ret;

                if ((wval & ElementDefine.EEPROM_BUSY_FLAG) == 0)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                Thread.Sleep(10);
            }
            return LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
        }

        internal UInt32 OnEpBlockErase()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnWaitEpReady();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWorkMode(ElementDefine.COBRA_OZ9316_ATEM.ATEMODE_EEPROM_BLOCK_ERASE_REQUESTED);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWaitATEModeFinish();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            return OnWaitEpReady();
        }

        internal UInt32 OnEpBlockMap()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnWaitEpReady();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWorkMode(ElementDefine.COBRA_OZ9316_ATEM.ATEMODE_EEPROM_MAPPING_REQUESTED);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWaitATEModeFinish();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            return OnWaitEpReady();
        }

        internal UInt32 OnOpenEpATEData()
        {
            UInt16 ATEPPw = 0;
            UInt16 ATESPw = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnOpReadWord(ElementDefine.ATE_PRIMARY_PSW_REG, ref ATEPPw);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnOpReadWord(ElementDefine.ATE_SECOND_PSW_REG, ref ATESPw);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((SharedFormula.LoByte(ATEPPw) == SharedFormula.LoByte(ATESPw)) || (SharedFormula.LoByte(ATEPPw) == 0x00) || (SharedFormula.LoByte(ATEPPw) == 0xFF))
            {
                m_ATESecPW.bdata = true; //Don't write back
                return ret;
            }
            else
            {
                ret = OnOpWriteWord(ElementDefine.ATE_SECOND_PSW_REG, ATEPPw);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    m_ATESecPW.wdata = ATESPw;
                    m_ATESecPW.bdata = false;
                    return ret;
                }
            }
            return ret;
        }

        internal UInt32 OnOpenEpUserData()
        {
            UInt16 UserPPw = 0;
            UInt16 UserSPw = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnOpReadWord(ElementDefine.USER_PRIMARY_PSW_REG, ref UserPPw);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnOpReadWord(ElementDefine.USER_SECOND_PSW_REG, ref UserSPw);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((SharedFormula.LoByte(UserPPw) == SharedFormula.LoByte(UserSPw)) || (SharedFormula.LoByte(UserPPw) == 0x00) || (SharedFormula.LoByte(UserPPw) == 0xFF))
            {
                m_UserSecPW.bdata = true; //Don't write back
                return ret;
            }
            else
            {
                ret = OnOpWriteWord(ElementDefine.USER_SECOND_PSW_REG, UserPPw);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    m_UserSecPW.bdata = false;
                    m_UserSecPW.wdata = UserSPw;
                    return ret;
                }
            }
            return ret;
        }

        internal UInt32 OnCloseEpATEData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (m_ATESecPW.bdata) return ret;
            ret = OnOpWriteWord(ElementDefine.ATE_SECOND_PSW_REG, m_ATESecPW.wdata);

            return ret;
        }

        internal UInt32 OnCloseEpUserData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (m_UserSecPW.bdata) return ret;
            ret = OnOpWriteWord(ElementDefine.USER_SECOND_PSW_REG, m_UserSecPW.wdata);

            return ret;
        }
        #endregion
        #endregion

        #region OP寄存器操作
        #region OP寄存器父级操作
        internal UInt32 OpReadWord(byte reg, ref UInt16 pval, byte len = 1)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnOpReadWord(reg, ref pval, len);
            }
            return ret;
        }

        internal UInt32 OpWriteWord(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnOpWriteWord(reg, pval);
            }
            return ret;
        }
        #endregion

        #region OP寄存器子级操作
        internal UInt32 OnOpReadWord(byte reg, ref UInt16 pval, byte len = 1)
        {
            byte cmd = ElementDefine.OR_RD_CMD;
            cmd |= (byte)(len - 1);

            return OnReadWord(cmd, reg, ref pval);
        }

        internal UInt32 OnOpWriteWord(byte reg, UInt16 pval)
        {
            byte cmd = ElementDefine.OR_WR_CMD;
            return OnWriteWord(cmd, reg, pval);
        }
        #endregion
        #endregion

        #region SRAM寄存器操作
        #region SRAM寄存器父级操作
        internal UInt32 SmReadWord(byte reg, ref UInt16 pval, byte len = 1)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSmReadWord(reg, ref pval, len);
            }
            return ret;
        }

        internal UInt32 SmWriteWord(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSmWriteWord(reg, pval);
            }
            return ret;
        }
        #endregion
        #region OP寄存器子级操作
        internal UInt32 OnSmReadWord(byte reg, ref UInt16 pval, byte len = 1)
        {
            byte cmd = ElementDefine.SRAM_RD_CMD;
            cmd |= (byte)(len - 1);

            return OnReadWord(cmd, reg, ref pval);
        }

        internal UInt32 OnSmWriteWord(byte reg, UInt16 pval)
        {
            byte cmd = ElementDefine.SRAM_WR_CMD;
            return OnWriteWord(cmd, reg, pval);
        }
        #endregion
        #endregion
        #endregion

        #region 设备基础操作
        protected UInt32 OnWorkMode(ElementDefine.COBRA_OZ9316_WKM wkm)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnOpReadWord(ElementDefine.WORKMODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata = (UInt16)((wdata & ElementDefine.WORK_MODE_FLAG) | ((UInt16)wkm << 8));
            return OnOpWriteWord(ElementDefine.WORKMODE_REG, wdata);
        }

        protected UInt32 OnWorkMode(ElementDefine.COBRA_OZ9316_ATEM atem)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //Effective just only when ate_secondary_pwd = 0x33 and work_mode = 4’b1111.
            ret = OnWorkMode(ElementDefine.COBRA_OZ9316_WKM.WORKMODE_ATE_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnOpReadWord(ElementDefine.ATE_SECOND_PSW_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata = SharedFormula.MAKEWORD(ElementDefine.ATE_SECOND_PSW_TO_ATEMODE, SharedFormula.HiByte(wdata));
            ret = OnOpWriteWord(ElementDefine.ATE_SECOND_PSW_REG, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnOpReadWord(ElementDefine.ATEMODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            switch (atem)
            {
                case ElementDefine.COBRA_OZ9316_ATEM.ATEMODE_EEPROM_BLOCK_ERASE_REQUESTED:
                    {
                        wdata = (UInt16)((wdata & ElementDefine.ATE_MODE_FLAG) | (UInt16)atem | (UInt16)ElementDefine.EEPROM_ERASE_FLAG);
                        ret = OnOpWriteWord(ElementDefine.ATEMODE_REG, wdata);
                        break;
                    }
                default:
                    {
                        wdata = (UInt16)((wdata & ElementDefine.ATE_MODE_FLAG) | (UInt16)atem);
                        ret = OnOpWriteWord(ElementDefine.ATEMODE_REG, wdata);
                        break;
                    }
            }
            return ret;
        }

        protected UInt32 OnWaitATEModeFinish()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < ElementDefine.SPI_RETRY_COUNT; i++)
            {
                ret = OnOpReadWord(ElementDefine.ATEMODE_REG, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wval & ElementDefine.ATE_MODE_FINISH_FLAG) == 0x00)
                    return ret;

                Thread.Sleep(20);
            }
            return LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
        }
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
                p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = EpBlockErase(ref msg);
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            return EpBlockMap();
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EpReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> SmReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                EpReglist.Add(baddress);
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
                    case ElementDefine.SRAMElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                SmReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EpReglist = EpReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            SmReglist = SmReglist.Distinct().ToList();
            //Read 
            foreach (byte badd in EpReglist)
            {
                //ret = YFLASHReadWord(badd, ref wdata);
                ret = EpReadWord(badd, ref wdata);
                parent.m_EpRegImg[badd].err = ret;
                parent.m_EpRegImg[badd].val = wdata;
            }

            foreach (byte badd in OpReglist)
            {
                ret = OpReadWord(badd, ref wdata);
                //Thread.Sleep(10);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }

            foreach (byte badd in SmReglist)
            {
                ret = SmReadWord(badd, ref wdata);
                //Thread.Sleep(10);
                parent.m_SmRegImg[badd].err = ret;
                parent.m_SmRegImg[badd].val = wdata;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EpATEReglist = new List<byte>();
            List<byte> EpUserReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> SmReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;


            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                if (baddress > ElementDefine.ATE_MAX_DATA)
                                    EpUserReglist.Add(baddress);
                                else
                                    EpATEReglist.Add(baddress);
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
                    case ElementDefine.SRAMElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                SmReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EpATEReglist = EpATEReglist.Distinct().ToList();
            EpUserReglist = EpUserReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            SmReglist = SmReglist.Distinct().ToList();

            //Write 
            if (EpATEReglist.Count != 0)
            {
                ret = OpenEpATEData();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in EpATEReglist)
                {
                    ret = EpWriteWord(badd, parent.m_EpRegImg[badd].val);
                    parent.m_EpRegImg[badd].err = ret;
                }

                ret = CloseEpATEData();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            if (EpUserReglist.Count != 0)
            {
                ret = OpenEpUserData();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in EpUserReglist)
                {
                    ret = EpWriteWord(badd, parent.m_EpRegImg[badd].val);
                    parent.m_EpRegImg[badd].err = ret;
                }
                ret = CloseEpUserData();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = OpWriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            foreach (byte badd in SmReglist)
            {
                ret = SmWriteWord(badd, parent.m_SmRegImg[badd].val);
                parent.m_SmRegImg[badd].err = ret;
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
                ret = OpWriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> EpParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();
            List<Parameter> SmParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
                        {
                            if (p == null) break;
                            EpParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                    case ElementDefine.SRAMElement:
                        {
                            if (p == null) break;
                            SmParamList.Add(p);
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

            if (EpParamList.Count != 0)
            {
                for (int i = 0; i < EpParamList.Count; i++)
                {
                    param = (Parameter)EpParamList[i];
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

            if (SmParamList.Count != 0)
            {
                for (int i = 0; i < SmParamList.Count; i++)
                {
                    param = (Parameter)SmParamList[i];
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

            List<Parameter> EpParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();
            List<Parameter> SmParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
                        {
                            if (p == null) break;
                            EpParamList.Add(p);
                            break;
                        }
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                    case ElementDefine.SRAMElement:
                        {
                            if (p == null) break;
                            SmParamList.Add(p);
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

            if (EpParamList.Count != 0)
            {
                for (int i = 0; i < EpParamList.Count; i++)
                {
                    param = (Parameter)EpParamList[i];
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


            if (SmParamList.Count != 0)
            {
                for (int i = 0; i < SmParamList.Count; i++)
                {
                    param = (Parameter)SmParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            Parameter param = null;
            ParamContainer demparameterlist = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_OZ9316_COMMAND)msg.sub_task)
            {
                case ElementDefine.COBRA_OZ9316_COMMAND.CHECK_SCAN_STATUS:
                    ret = CheckScanStatus();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COBRA_OZ9316_COMMAND.TRIGGER_SCAN_REQ:
                    ret = StopADC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = ClearADCFlag();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = ResumeADC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(10);
                    ret = OpReadWord(ElementDefine.WORKMODE_REG, ref wval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = OpWriteWord(ElementDefine.WORKMODE_REG, (UInt16)(wval | 0x8000));
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(100);
                    ret = StopADC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Thread.Sleep(100);
                    break;
                case ElementDefine.COBRA_OZ9316_COMMAND.ADC_RESUME_REQ:
                    ret = ResumeADC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COBRA_OZ9316_COMMAND.CALIBRATION:
                    {
                        ret = CheckScanStatus();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = SoftScanRequest();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = Read(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ResumeADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ConvertHexToPhysical(ref msg);
                    }
                    break;
                case ElementDefine.COBRA_OZ9316_COMMAND.SCAN_AUTO:
                    ret = CheckScanStatus();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = Read(ref msg);
                    break;
                case ElementDefine.COBRA_OZ9316_COMMAND.SCAN_TRIGGER:
                    ret = CheckScanStatus();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = SoftScanRequest();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = Read(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ResumeADC();
                    break;
                case ElementDefine.COBRA_OZ9316_COMMAND.TRIM:
                    demparameterlist = msg.task_parameterlist;
                    if (demparameterlist == null) return ret;

                    for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                    {
                        param = demparameterlist.parameterlist[i];
                        param.sphydata = String.Empty;
                    }

                    ret = CheckScanStatus();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    //Clear offset
                    for (int i = 0; i < 15; i++)
                    {
                        ret = EpWriteWord((byte)(0x08 + i), 0);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }

                    for (int icode = 0; icode < 64; icode++)
                    {
                        //Clear CADC_Trigger_Finish_Flag in reg.6C
                        ret = OpReadWord(0x6C, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OpWriteWord(0x6C, (UInt16)(wval | 0x2000));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OpReadWord(0x06, ref wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        wval = (UInt16)((wval >> 6) << 6);
                        wval += (UInt16)icode;

                        ret = OpWriteWord(0x06, (UInt16)wval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        #region TrimCode填充
                        foreach (Parameter p in demparameterlist.parameterlist)
                        {
                            //Clear VADC_Trigger_Finish_Flag in reg.6B
                            ret = OpReadWord(0x6B, ref wval);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = OpWriteWord(0x6B, (UInt16)(wval | 0x2000));
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = OpReadWord(0x18, ref wval);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            wval = (UInt16)((wval >> 6) << 6);
                            wval |= (UInt16)icode;

                            ret = OpWriteWord(0x18, (UInt16)wval);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = OpReadWord(0x67, ref wval);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            wval = (UInt16)((wval >> 5) << 5);
                            switch (p.guid)
                            {
                                case ElementDefine.SrCell1:
                                    wval += (UInt16)0x01;
                                    break;
                                case ElementDefine.SrCell2:
                                    wval += (UInt16)0x02;
                                    break;
                                case ElementDefine.SrCell3:
                                    wval += (UInt16)0x03;
                                    break;
                                case ElementDefine.SrCell4:
                                    wval += (UInt16)0x04;
                                    break;
                                case ElementDefine.SrCell5:
                                    wval += (UInt16)0x05;
                                    break;
                                case ElementDefine.SrCell6:
                                    wval += (UInt16)0x06;
                                    break;
                                case ElementDefine.SrCell7:
                                    wval += (UInt16)0x07;
                                    break;
                                case ElementDefine.SrCell8:
                                    wval += (UInt16)0x08;
                                    break;
                                case ElementDefine.SrCell9:
                                    wval += (UInt16)0x09;
                                    break;
                                case ElementDefine.SrCell10:
                                    wval += (UInt16)0x0A;
                                    break;
                                case ElementDefine.SrCell11:
                                    wval += (UInt16)0x0B;
                                    break;
                                case ElementDefine.SrCell12:
                                    wval += (UInt16)0x0C;
                                    break;
                                case ElementDefine.SrCell13:
                                    wval += (UInt16)0x0D;
                                    break;
                                case ElementDefine.SrCell14:
                                    wval += (UInt16)0x0E;
                                    break;
                                case ElementDefine.SrCell15:
                                    wval += (UInt16)0x0F;
                                    break;
                                case ElementDefine.SrCell16:
                                    wval += (UInt16)0x10;
                                    break;
                                case ElementDefine.SrVPack:
                                    wval += (UInt16)0x15;
                                    break;
                                case ElementDefine.SrGPIO1:
                                    wval += (UInt16)0x18;
                                    break;
                            }
                            wval = (UInt16)(wval | 0x8000);
                            ret = OpWriteWord(0x67, wval);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                            /*

                                                        int k = 0;
                                                        for (k = 0; k < ElementDefine.SPI_RETRY_COUNT; k++)
                                                        {
                                                            ret = OpReadWord(0x6B, ref wval);
                                                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                                            if ((wval & 0x2000) == 0x2000) break;
                                                            Thread.Sleep(10);
                                                        }
                                                        if (k == ElementDefine.SPI_RETRY_COUNT)
                                                            return LibErrorCode.IDS_ERR_SPI_EPP_TIMEOUT;

                                                        for (k = 0; k < ElementDefine.SPI_RETRY_COUNT * 2; k++)
                                                        {
                                                            ret = OpReadWord(0x6C, ref wval);
                                                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                                            if ((wval & 0x2000) == 0x2000) break;
                                                            Thread.Sleep(20);
                                                        }
                                                        if (k == ElementDefine.SPI_RETRY_COUNT)
                                                            return LibErrorCode.IDS_ERR_SPI_EPP_TIMEOUT;*/
                        }
                        #endregion
                        ret = Read(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = ConvertHexToPhysical(ref msg);
                        for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                        {
                            param = demparameterlist.parameterlist[i];
                            param.sphydata += param.phydata.ToString() + ",";
                        }
                    }
                    break;
            }
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

            ret = OpReadWord(0x60, ref wval);
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
            UInt16 wdata = 0;
            UInt16 cellnum = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OpReadWord(0x29, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < 6; i++)
                msg.sm.gpios[i] = (wdata & (3 << i*2)) > 0 ? true : false;

            msg.sm.dic.Clear();
            ret = OpReadWord(0x28, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < 16; i++)
                msg.sm.dic.Add((uint)(i), true);

            cellnum = (UInt16)((wdata & 0x000F) + 5);
            for (int i = 0; i < 16 - cellnum; i++)
                msg.sm.dic[(uint)i+1] = false;
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
        #endregion

        #region OZ9316特殊操作
        protected UInt32 CheckScanStatus()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //Read ATE Primary Password
            ret = OpReadWord(ElementDefine.ATE_PRIMARY_PSW_REG, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if ((SharedFormula.LoByte(wval) == ElementDefine.INVALID_PSW_ONE) || (SharedFormula.LoByte(wval) == ElementDefine.INVALID_PSW_TWO))
            {
                ret = LibErrorCode.IDS_ERR_DEM_PASSWORD_INVALID;
                return ret;
            }

            //Read User Primary Password
            ret = OpReadWord(ElementDefine.USER_PRIMARY_PSW_REG, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if ((SharedFormula.LoByte(wval) == ElementDefine.INVALID_PSW_ONE) || (SharedFormula.LoByte(wval) == ElementDefine.INVALID_PSW_TWO))
            {
                ret = LibErrorCode.IDS_ERR_DEM_PASSWORD_INVALID;
                return ret;
            }

            //Check CADC&VADC
            ret = OpReadWord(ElementDefine.SYSCFG_REG, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (((wval & ElementDefine.CADC_SUPPORT_FLAG) != ElementDefine.CADC_SUPPORT_FLAG) || ((wval & ElementDefine.VADC_SUPPORT_FLAG) < ElementDefine.EE_CELL_NUMBER))
                ret = LibErrorCode.IDS_ERR_DEM_ADC_STOPPED;
            return ret;
        }

        protected UInt32 SoftScanRequest()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = StopADC();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ClearADCFlag();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ResumeADC();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            Thread.Sleep(10);

            ret = OpReadWord(0x67, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OpWriteWord(0x67, (UInt16)(wval | 0x8000));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            Thread.Sleep(10);

            ret = StopADC();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Check VADC&CADC complete flag on trigger scan mode		
            int i = 0;
            for (i = 0; i < ElementDefine.SPI_RETRY_COUNT; i++)
            {
                ret = OpReadWord(0x6C, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wval & 0x2000) == 0x2000) break;
                Thread.Sleep(20);
            }
            if (i == ElementDefine.SPI_RETRY_COUNT)
            {
                return LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }

            for (i = 0; i < ElementDefine.SPI_RETRY_COUNT; i++)
            {
                ret = OpReadWord(0x6B, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wval & 0x2000) == 0x2000) break;
                Thread.Sleep(50);
            }
            if (i == ElementDefine.SPI_RETRY_COUNT)
            {
                return LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }

            return ret;
        }

        protected UInt32 ClearADCFlag()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OpReadWord(0x6B, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OpWriteWord(0x6B, (UInt16)(wval | 0x3000));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OpReadWord(0x6C, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OpWriteWord(0x6C, (UInt16)(wval | 0x3000));

            return ret;
        }

        protected UInt32 StopADC()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //Read the Cell Number setting from EEPRom
            ret = OpReadWord(0x28, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Open User map data
            ret = OpenOpUserMapData();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xDFF0;
            //Set Cell Number = 0
            ret = OpWriteWord(0x28, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Close User Map data
            ret = CloseOpUserMapData();
            return ret;
        }

        protected UInt32 ResumeADC()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //Read the Cell Number setting from EEPRom
            ret = EpReadWord(0x28, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Open User map data
            ret = OpenOpUserMapData();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Set Cell Number
            ret = OpWriteWord(0x28, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Close User Map data
            ret = CloseOpUserMapData();
            return ret;
        }

        protected UInt32 OpenOpUserMapData()
        {
            UInt16 UserSPw = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        protected UInt32 CloseOpUserMapData()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
        #endregion
    }
}
