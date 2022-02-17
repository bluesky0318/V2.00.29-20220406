﻿//#define debug
//#if debug
//#define functiontimeout
//#define pec
//#define frozen
//#define dirty
//#define readback
//#endif
//#define Kall17
#define Kall17D
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
using System.Windows.Forms;

namespace Cobra.KALL17D
{
    internal class DEMBehaviorManage
    {
        private byte calATECRC;
        private byte calUSRCRC;
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private const UInt16 ALLOW_WR_FLAG = 0x8000;
        private const byte EFUSE_DATA_OFFSET = 0x80;
        private const byte EFUSE_MAP_OFFSET = 0xA0;
        private List<DataPoint> m_dataPoint_List = new List<DataPoint>();
        //private Dictionary<UInt32, Dictionary<int,double>> dic = new Dictionary<UInt32, Dictionary<int, double>>(); //<guid,<code,data>>
        private Dictionary<(UInt32, int), double> dic = new Dictionary<(UInt32, int), double>(); //<guid,<code,data>>
        private Dictionary<UInt32, double> dic_offset = new Dictionary<UInt32, double>();
        private object m_lock = new object();
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
        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(ElementDefine.OR_RD_CMD, reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteWord(ElementDefine.OR_WR_CMD, reg, val);
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

        protected UInt32 OnReadWord(byte cmd, byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnSPI_ReadCmd(cmd, reg, ref pval);
            return ret;
        }

        protected UInt32 OnWriteWord(byte cmd, byte reg, UInt16 val)
        {
#if debug
            return 0;
#else
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnSPI_WriteCmd(cmd, reg, val);
            return ret;
#endif
        }

        protected UInt32 OnSPI_ReadCmd(byte Cmd_Len, byte reg, ref UInt16 pWval)
        {
#if debug
            pWval = 1;
            return 0;
#else
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
#endif
        }

        protected UInt32 OnSPI_WriteCmd(byte Cmd_Len, byte reg, UInt16 wval)
        {
#if debug
            return 0;
#else
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
#endif
        }
        #endregion
        #endregion

        #region EFUSE寄存器操作
        #region EFUSE寄存器父级操作
        internal UInt32 EFUSEReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnEFUSEReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 EFUSEWriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnEFUSEWriteWord(reg, val);
            }
            return ret;
        }
        #endregion

        #region EFUSE寄存器子级操作
        protected UInt32 OnWorkMode(ElementDefine.WORK_MODE wkm)
        {
            byte blow = 0, bhigh = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnReadWord(ElementDefine.EF_RD_CMD, ElementDefine.WORKMODE_OFFSET, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(ElementDefine.EF_WR_CMD, ElementDefine.WORKMODE_OFFSET, (UInt16)(wdata | ALLOW_WR_FLAG));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnReadWord(ElementDefine.EF_RD_CMD, ElementDefine.WORKMODE_OFFSET, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            blow = (byte)(SharedFormula.LoByte(wdata) & 0xFC);
            bhigh = (byte)SharedFormula.HiByte(wdata);
            blow |= (byte)wkm;
            wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
            ret = OnWriteWord(ElementDefine.EF_WR_CMD, ElementDefine.WORKMODE_OFFSET, wdata);
            return ret;
        }

        protected UInt32 OnEFUSEReadWord(byte reg, ref UInt16 pval)
        {
            return OnReadWord(ElementDefine.EF_RD_CMD, reg, ref pval);
        }

        protected UInt32 OnEFUSEWriteWord(byte reg, UInt16 val)
        {
            uint ret = OnWriteWord(ElementDefine.EF_WR_CMD, reg, val);
            Thread.Sleep(5);
            return ret;
        }

        #endregion
        #endregion

        #region EFUSE功能操作
        #region EFUSE功能父级操作

        protected UInt32 WorkMode(ElementDefine.WORK_MODE wkm)
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
            return ret;
        }

        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            bool bsim = true;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EFUSEElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                EFUSEReglist.Add(baddress);
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

            EFUSEReglist = EFUSEReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (EFUSEReglist.Count != 0)
            {
                /*List<byte> EFATEList = new List<byte>();
                List<byte> EFUSRList = new List<byte>();
                foreach (byte addr in EFUSEReglist)
                {
                    if (addr <= 0x26 && addr >= 0x20)
                        EFATEList.Add(addr);
                    else if (addr <= 0x2f && addr >= 0x27)
                        EFUSRList.Add(addr);
                }
                if (EFATEList.Count != 0)
                {
                    ret = CheckATECRC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }

                if (EFUSRList.Count != 0)
                {
                    ret = CheckUSRCRC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }*/
                foreach (byte badd in EFUSEReglist)
                {
                    ret = EFUSEReadWord(badd, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    parent.m_EFRegImg[badd].err = ret;
                    parent.m_EFRegImg[badd].val = wdata;
                }

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                /*if (EFUSEReglist.Count != 0)
                {
                    ret = CheckCRC();   //这个函数除了检查CRC，也读到了寄存器的内容
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }*/
            }

            foreach (byte badd in OpReglist)
            {
                //else
                {
                    ret = ReadWord(badd, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    parent.m_OpRegImg[badd].err = ret;
                    parent.m_OpRegImg[badd].val = wdata;
                }
            }
            return ret;
        }

        private bool isATEFRZ()
        {
            return (parent.m_EFRegImgEX[0x0f].val & 0x8000) == 0x8000;
        }

        private UInt32 CheckCRC()
        {
            //UInt16 len = 8;
            //byte tmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte[] atebuf = new byte[31];

            ret = ReadATECRCRefReg();   //这边已经读到了寄存器的内容
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (!isATEFRZ())        //如果没有Freeze，就不需要检查CRC
                return LibErrorCode.IDS_ERR_SUCCESSFUL;

            GetATECRCRef(ref atebuf);
            calATECRC = CalEFUSECRC(atebuf, 31);

            byte readATECRC = 0;
            ret = ReadATECRC(ref readATECRC);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (readATECRC == calATECRC)
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            else
            {
                parent.m_EFRegImgEX[0x0f].err = LibErrorCode.IDS_ERR_DEM_ATE_CRC_ERROR;
                return LibErrorCode.IDS_ERR_DEM_ATE_CRC_ERROR;
            }
        }

        private UInt32 ReadATECRC(ref byte crc)
        {
            ushort wdata = 0;

            parent.m_EFRegImg[0x8f].val &= 0xff00;
            parent.m_EFRegImg[0x8f].val |= calATECRC;    //Deliver calCRC to AMT

            parent.m_EFRegImg[0x8f].err = ReadWord(0x8f, ref wdata);
            if (parent.m_EFRegImg[0x8f].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_EFRegImg[0x8f].err;
            parent.m_EFRegImg[0x8f].val = wdata;
            crc = (byte)(wdata & 0x00ff);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        private UInt32 ReadATECRCRefReg()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte i = 0x80; i <= 0x8f; i++)
            {
                ushort wdata = 0;
                parent.m_EFRegImg[i].err = ReadWord(i, ref wdata);
                //if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                //return parent.m_EFRegImg[i].err;
                parent.m_EFRegImg[i].val = wdata;
                ret |= parent.m_EFRegImg[i].err;
            }
            return ret;
        }
        private void GetATECRCRef(ref byte[] buf)
        {
            //byte[] dat = new byte[0x0b];
            //byte[] tmp = new byte[27];
            /*for (byte i = 0; i < 20; i++)
            {
                byte shiftdigit = (byte)((i % 4) * 4);
                int reg = i / 4;
                buf[i] = (byte)((parent.m_EFRegImgEX[reg].val & (0x0f << shiftdigit)) >> shiftdigit);
            }
            buf[20] = (byte)((parent.m_EFRegImgEX[5].val & 0x00f0) >> 4);
            buf[21] = (byte)((parent.m_EFRegImgEX[5].val & 0x0f00) >> 8);
            buf[22] = (byte)((parent.m_EFRegImgEX[5].val & 0xf000) >> 12);


            buf[23] = (byte)(parent.m_EFRegImgEX[5].val & 0x000f);
            buf[24] = (byte)((parent.m_EFRegImgEX[5].val & 0x00f0) >> 4);
            buf[25] = (byte)((parent.m_EFRegImgEX[5].val & 0x0f00) >> 8);
            buf[26] = (byte)((parent.m_EFRegImgEX[5].val & 0xf000) >> 12);*/
            for (ushort i = 0; i < 15; i++)
            {
                buf[i * 2] = (byte)(parent.m_EFRegImgEX[i].val & 0x00ff);
                buf[i * 2 + 1] = (byte)((parent.m_EFRegImgEX[i].val & 0xff00) >> 8);
            }
            buf[30] = (byte)((parent.m_EFRegImgEX[0x0f].val & 0xff00) >> 8);

        }

        private byte CalEFUSECRC(byte[] buf, UInt16 len)
        {
            return crc8_calc(buf, len);
        }

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

        private UInt32 UnLockCfgArea()
        {
            ushort tmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0x56, ref tmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            if ((tmp & 0x0001) == 0x0001)
                return ret;
            ret = WriteWord(0x56, 0x7717);
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)    //因为Efuse是在Expert页面写，所以没有复杂逻辑
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            UInt16[] EFUSEATEbuf = new UInt16[16];
            List<byte> OpReglist = new List<byte>();
            List<byte> OpEfuseReglist = new List<byte>();
            List<byte> OpMapReglist = new List<byte>();
            UInt16[] pdata = new UInt16[6];

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            if (msg.gm.sflname == "Register Config")
            {
                ret = UnLockCfgArea();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EFUSEElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                EFUSEReglist.Add(baddress);
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
                                if (baddress >= EFUSE_MAP_OFFSET)
                                    OpMapReglist.Add(baddress);
                                else if ((baddress < EFUSE_MAP_OFFSET) && (baddress >= EFUSE_DATA_OFFSET))
                                    OpEfuseReglist.Add(baddress);
                                else
                                    OpReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EFUSEReglist = EFUSEReglist.Distinct().ToList();
            OpMapReglist = OpMapReglist.Distinct().ToList();
            OpEfuseReglist = OpEfuseReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            foreach (byte badd in EFUSEReglist)
            {
                ret = OnEFUSEWriteWord(badd, parent.m_EFRegImg[badd].val);
                parent.m_EFRegImg[badd].err = ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            if (OpEfuseReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.WORK_MODE.PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in OpEfuseReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.WORK_MODE.NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            if (OpMapReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.WORK_MODE.INTERNAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in OpMapReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.WORK_MODE.NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
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

            List<Parameter> EFUSEParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EFUSEElement:
                        {
                            if (p == null) break;
                            EFUSEParamList.Add(p);
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

            if (EFUSEParamList.Count != 0)
            {
                for (int i = 0; i < EFUSEParamList.Count; i++)
                {
                    param = (Parameter)EFUSEParamList[i];
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

            List<Parameter> EFUSEParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EFUSEElement:
                        {
                            if (p == null) break;
                            EFUSEParamList.Add(p);
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

            if (EFUSEParamList.Count != 0)
            {
                for (int i = 0; i < EFUSEParamList.Count; i++)
                {
                    param = (Parameter)EFUSEParamList[i];
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
        #region SAR
        private uint ReadAvrage(ref TASKMessage msg)
        {
            uint ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<double[]> llt = new List<double[]>();
            List<double> avr = new List<double>();
            foreach (Parameter param in msg.task_parameterlist.parameterlist)
            {
                llt.Add(new double[5]);
                avr.Add(0);
            }
#if Kall17
            for (int i = 0; i < 5; i++)
            {
                ret = ClearSarTriggerScanFlag();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                ///
                ret = ChangeSarScanMode(ElementDefine.SAR_MODE.TRIGGER_8);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                ///
                ret = WaitForSarScanComplete();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                ret = Read(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
                ret = ConvertHexToPhysical(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
                for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
                {
                    llt[j][i] = msg.task_parameterlist.parameterlist[j].phydata;
                    avr[j] += llt[j][i];
                }
                Thread.Sleep(100);
            }
#elif Kall17D
            byte badd = 0;
            UInt16 wdata = 0;
            Parameter param1 = null;
            //foreach (Parameter param in msg.task_parameterlist.parameterlist)
            for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
            {
                param1 = msg.task_parameterlist.parameterlist[j];
                if (param1 == null) continue;
                for (int i = 0; i < 5; i++)
                {
                    ret = ClearSarTriggerScanFlag();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = ReadWord(0x5f, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    wdata &= 0xff20;
                    wdata |= 0x0080;
                    wdata |= (UInt16)((param1.guid & 0x0000FF00) >> 8);
                    ret = WriteWord(0x5f, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WaitForSarScanComplete();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    badd = (byte)((param1.guid & 0x0000FF00) >> 8);
                    ret = ReadWord(badd, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    short s = (short)wdata;
                    param1.phydata = s * param1.phyref / param1.regref;

                    llt[j][i] = param1.phydata;
                    avr[j] += llt[j][i];
                    Thread.Sleep(2);
                }
            }
#endif

            for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
            {
                //llt[j][i] = msg.task_parameterlist.parameterlist[j].phydata;
                avr[j] /= 5;
                int minIndex = 0;
                double err = 999;
                for (int i = 0; i < 5; i++)
                {
                    if (err > Math.Abs(llt[j][i] - avr[j]))
                    {
                        err = Math.Abs(llt[j][i] - avr[j]);
                        minIndex = i;
                    }
                }
                msg.task_parameterlist.parameterlist[j].phydata = llt[j][minIndex];
            }
            return ret;
        }
        UInt32 ChangeSarScanMode(ElementDefine.SAR_MODE scanmode)
        {
            ushort wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (scanmode)
            {
                case ElementDefine.SAR_MODE.AUTO_1:
                    ret = ReadWord(0x58, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    wdata &= 0xfff8;
                    wdata |= 0x0005;
                    ret = WriteWord(0x58, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.SAR_MODE.AUTO_8:
                    ret = ReadWord(0x58, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    wdata &= 0xfff8;
                    wdata |= 0x0007;
                    ret = WriteWord(0x58, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.SAR_MODE.TRIGGER_1:
                    ret = ReadWord(0x5f, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    wdata &= 0xff20;
                    wdata |= 0x009f;
                    ret = WriteWord(0x5f, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.SAR_MODE.TRIGGER_8:
                    ret = ReadWord(0x5f, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    wdata &= 0xff20;
                    wdata |= 0x00df;
                    ret = WriteWord(0x5f, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.SAR_MODE.TRIGGER_8_TIME_CURRENT_SCAN:
                    ret = ReadWord(0x5f, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    wdata &= 0xff20;
                    wdata |= 0x00d2;
                    ret = WriteWord(0x5f, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
            }
            return ret;
        }

        public UInt32 TriggerChannel(ElementDefine.SAR_MODE scanmode,int nchannel)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ClearSarTriggerScanFlag();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadWord(0x5f, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata &= 0xff20;
            wdata |= 0x0080;
            wdata |= (UInt16)((UInt16)scanmode << 6);
            wdata |= (UInt16)nchannel;
            ret = WriteWord(0x5f, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = WaitForSarScanComplete();
            return ret;
        }

        UInt32 ClearSarTriggerScanFlag()
        {
            return WriteWord(0x5e, 0x0020);
        }

        UInt32 WaitForSarScanComplete()
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#else
            ushort wdata = 0;
            ushort retry_count = 200;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            while ((wdata & 0x0020) != 0x0020)
            {
                retry_count--;
                if (retry_count == 0)
                    return ElementDefine.IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT;
                ret = ReadWord(0x5e, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            return ret;
#endif
        }

        void GetExtTemp()
        {
            for (byte i = 0; i < 5; i++)
            {
                if (parent.thms[i].ADC2 <= 32700)   //120uA档是正确值
                {
                    parent.m_OpRegImg[0x13 + i].val = parent.thms[i].ADC2;
                    parent.thms[i].thm_crrt = 120;
                }
                else if (parent.thms[i].ADC1 <= 32700)   //20uA档是正确值 
                {
                    parent.m_OpRegImg[0x13 + i].val = parent.thms[i].ADC1;
                    parent.thms[i].thm_crrt = 20;
                }
                else     //10uA档是正确值 
                {
                    parent.m_OpRegImg[0x13 + i].val = parent.thms[i].ADC3;
                    parent.thms[i].thm_crrt = 10;
                }

                parent.m_OpRegImg[0x13 + i].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
        }
        private UInt32 ReadSAR(ref TASKMessage msg, ElementDefine.SAR_MODE scanmode)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (scanmode == ElementDefine.SAR_MODE.DISABLE)  return ret;

            TASKMessage sarmsg = new TASKMessage();  //only contains sar adc parameters
            TASKMessage tmpmsg = new TASKMessage();  //only contains temperature parameters

            foreach (Parameter p in msg.task_parameterlist.parameterlist)
            {
                if (p.guid != ElementDefine.BASIC_CADC && p.guid != ElementDefine.TRIGGER_CADC && p.guid != ElementDefine.MOVING_CADC)
                    sarmsg.task_parameterlist.parameterlist.Add(p);
            }

            ushort thm_crrt_sel = 0;
            ret = ReadWord(0x5a, ref thm_crrt_sel); //保存原始值
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            ret = WriteWord(0x5a, 0x0001);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            ret = ClearSarTriggerScanFlag();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = ChangeSarScanMode(scanmode);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            if (scanmode == ElementDefine.SAR_MODE.TRIGGER_1 || scanmode == ElementDefine.SAR_MODE.TRIGGER_8)
            {
                ret = WaitForSarScanComplete();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            else
            {
                Thread.Sleep(40);
            }

            for (int i = 1; i < 0x1E; i++)
            {
                ret = TriggerChannel(scanmode, i);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = Read(ref sarmsg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            //Issue 1169
            ushort tmp = 0;
            ret = ReadWord(0x52, ref tmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            if ((tmp & 0x1000) == 0x1000)   //read Vwkup
            //if(true)
            {
                parent.m_Vwkup.val = parent.m_OpRegImg[0x1a].val;
                parent.m_OpRegImg[0x1a].val = ElementDefine.PARAM_HEX_ERROR;
            }
            else                            //read Vpack
            {
                parent.m_Vwkup.val = ElementDefine.PARAM_HEX_ERROR;
            }

            for (byte i = 0; i < 5; i++)
            {
                parent.thms[i].ADC1 = parent.m_OpRegImg[0x13 + i].val;  //20uA时的电压值
            }

            #region 120uA
            ret = WriteWord(0x5a, 0x0002);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            ret = ClearSarTriggerScanFlag();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = ChangeSarScanMode(ElementDefine.SAR_MODE.TRIGGER_1);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WaitForSarScanComplete();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
         
            for (int i = 0x13; i < 0x18; i++)
            {
                ret = TriggerChannel(ElementDefine.SAR_MODE.TRIGGER_1, i);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (Parameter p in sarmsg.task_parameterlist.parameterlist)
            {
                if (p.subtype == (ushort)ElementDefine.SUBTYPE.EXT_TEMP)
                    tmpmsg.task_parameterlist.parameterlist.Add(p);
            }
            ret = Read(ref tmpmsg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            for (byte i = 0; i < 5; i++)
            {
                parent.thms[i].ADC2 = parent.m_OpRegImg[0x13 + i].val;  //120uA时的电压值
            }
            #endregion

            #region 10uA
            ret = WriteWord(0x5a, 0x0003);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            ret = ClearSarTriggerScanFlag();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = ChangeSarScanMode(ElementDefine.SAR_MODE.TRIGGER_1);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WaitForSarScanComplete();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            for (int i = 0x13; i < 0x18; i++)
            {
                ret = TriggerChannel(ElementDefine.SAR_MODE.TRIGGER_1, i);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (Parameter p in sarmsg.task_parameterlist.parameterlist)
            {
                if (p.subtype == (ushort)ElementDefine.SUBTYPE.EXT_TEMP)
                    tmpmsg.task_parameterlist.parameterlist.Add(p);
            }
            ret = Read(ref tmpmsg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            for (byte i = 0; i < 5; i++)
            {
                parent.thms[i].ADC3 = parent.m_OpRegImg[0x13 + i].val;  //120uA时的电压值
            }
            #endregion

            GetExtTemp();

            ret = WriteWord(0x5a, thm_crrt_sel);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            return ret;
        }
#endregion

        private UInt32 ReadCADC(ElementDefine.CADC_MODE mode)       //MP version new method. Do 4 time average by HW, and we can also have the trigger flag and coulomb counter work at the same time.
        {
            parent.cadc_mode = mode;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort temp = 0;
            switch (mode)
            {
                case ElementDefine.CADC_MODE.DISABLE:
#region disable
                    ret = WriteWord(0x30, 0x00);        //clear all
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
#endregion
                    break;
                case ElementDefine.CADC_MODE.MOVING:
#region moving mode
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    bool cadc_moving_flag = false;
                    {
                        ret = WriteWord(0x61, 0x0004);        //Clear cadc_moving_flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = WriteWord(0x30, 0x98);        //Set cc_always_enable, moving_average_enable, sw_cadc_ctrl=0b00
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        for (byte i = 0; i < ElementDefine.CADC_RETRY_COUNT; i++)
                        {
                            Thread.Sleep(20);
                            ret = ReadWord(0x61, ref temp);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
#if debug
                    cadc_moving_flag = true;
                    break;
#else
                            if ((temp & 0x0004) == 0x0004)
                            {
                                cadc_moving_flag = true;
                                break;
                            }
#endif
                        }
                        if (cadc_moving_flag)   //转换完成
                        {
#if debug
                    temp = 15;
#else
                            ret = ReadWord(0x39, ref temp);
#endif
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                        }
                        else
                        {
                            ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;
                        }
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    parent.m_OpRegImg[0x39].err = ret;
                    parent.m_OpRegImg[0x39].val = temp;
#endregion
                    break;
                case ElementDefine.CADC_MODE.TRIGGER:
#region trigger mode
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    bool cadc_trigger_flag = false;
                    {
                        ret = WriteWord(0x5e, 0x8000);        //Clear cadc_trigger_flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = WriteWord(0x30, 0x06);        //Set cadc_one_or_four, sw_cadc_ctrl=0b10
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        for (byte i = 0; i < ElementDefine.CADC_RETRY_COUNT; i++)
                        {
                            Thread.Sleep(20);
                            ret = ReadWord(0x5e, ref temp);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
#if debug
                            cadc_trigger_flag = true;
                    break;
#else
                            if ((temp & 0x8000) == 0x8000)
                            {
                                cadc_trigger_flag = true;
                                break;
                            }
#endif
                        }
                        if (cadc_trigger_flag)   //转换完成
                        {
#if debug
                    temp = 15;
#else
                            ret = ReadWord(0x38, ref temp);
#endif
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                        }
                        else
                        {
                            ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;
                        }
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    parent.m_OpRegImg[0x38].err = ret;
                    parent.m_OpRegImg[0x38].val = temp;
#endregion
                    break;
            }

            return ret;
        }               //trigger mode with 4 time average
        private void TRIGGERCADCHex2Physical(ref Parameter CADC)
        {
            short s = (short)parent.m_OpRegImg[0x38].val;
            CADC.phydata = s * CADC.phyref;// * 1000; // parent.etrx; //需要带符号
        }
        private UInt32 ActiveModeCheck()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort tmp = 0;
            ret = ReadWord(0x57, ref tmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            if ((tmp & 0x0080) != 0x0080)
            {
                ret = ElementDefine.IDS_ERR_DEM_ACTIVE_MODE_ERROR;
            }
            return ret;
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            TASKMessage MSG = new TASKMessage(); Parameter Current = new Parameter(); double AverageHex = 0; ushort wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            TASKMessage tmpmsg = new TASKMessage();  //only contains temperature parameters
            ushort tmp = 0;

            int nRetry = 5;
            Parameter param = null;
            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
#region Scan SFL commands
                case ElementDefine.COMMAND.OPTIONS:
                    var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                    switch (options["SAR ADC Mode"])
                    {
                        case "Disable":
                            ret = ReadSAR(ref msg, ElementDefine.SAR_MODE.DISABLE);
                            break;
                        case "1_Time":
                            ret = ReadSAR(ref msg, ElementDefine.SAR_MODE.TRIGGER_1);
                            break;
                        case "8_Time_Average":
                            ret = ReadSAR(ref msg, ElementDefine.SAR_MODE.TRIGGER_8);
                            break;
                        case "Auto_1":
                            ret = ReadSAR(ref msg, ElementDefine.SAR_MODE.AUTO_1);
                            break;
                        case "Auto_8":
                            ret = ReadSAR(ref msg, ElementDefine.SAR_MODE.AUTO_8);
                            break;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    switch (options["CADC Mode"])
                    {
                        case "Disable":
                            ret = ReadCADC(ElementDefine.CADC_MODE.DISABLE);
                            break;
                        case "Trigger":
                            ret = ReadCADC(ElementDefine.CADC_MODE.TRIGGER);
                            break;
                        case "Consecutive":
                            ret = ReadCADC(ElementDefine.CADC_MODE.MOVING);
                            break;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
#endregion

#region Trim SFL Command
                case ElementDefine.COMMAND.TRIM_SLOPE_EIGHT_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);
                        ret = ClearTrimAndOffset(msg.task_parameterlist);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

#region Trim 8 tims
                        for(int code = 0; code < 256; code++)
                        {
                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                switch (param.guid)
                                {
                                    case ElementDefine.OP_VBATT:
                                        nRetry = 5;
                                        if (code > 4) continue;
                                        break;
                                    case ElementDefine.OP_PACK_CUR:
                                        nRetry = 5;
                                        if (code > 32) continue;
                                        break;
                                    case ElementDefine.OP_CADC:
                                        nRetry = 1;
                                        break;
                                    default:
                                        nRetry = 5;
                                        if (code > 16) continue;
                                        break;
                                }
                                for (int n = 0; n < nRetry; n++)
                                {
                                    param = (Parameter)demparameterlist.parameterlist[i];
                                    if (param == null) continue;
                                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                                    ret = ReadParametersForSlope(code,ref param);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    if(!dic.ContainsKey((param.guid,code)))
                                        dic.Add((param.guid, code), param.phydata);
                                    else
                                        dic[(param.guid, code)] += param.phydata;                                    
                                }
                                dic[(param.guid, code)] /= nRetry;
                                DataPoint dataPoint = GetDataPointByGuid(param.guid);
                                dataPoint.SetOutput(code, dic[(param.guid, code)]);
                            }
                        }
                        ElementDefine.m_trim_count++;
#endregion
                    }
                    break;
                case ElementDefine.COMMAND.TRIM_OFFSET_EIGHT_MODE:
                    {
                        dic_offset.Clear();
                        InitDataPointList(msg.task_parameterlist);
#region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                                ret = ReadParametersForOffset(ref param);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                if (!dic_offset.Keys.Contains(param.guid))
                                    dic_offset.Add(param.guid, param.phydata);
                                else
                                    dic_offset[param.guid] += param.phydata;
                            }
                        }
                        foreach (UInt32 key in dic_offset.Keys)
                        {
                            DataPoint dataPoint = GetDataPointByGuid(key);
                            dataPoint.SetOutput(0,dic_offset[key] / 8);
                        }
                        ElementDefine.m_trim_count++;
#endregion
                    }
                    break;
                case ElementDefine.COMMAND.TRIM_COUNT_SLOPE:
                    {
                        CountSlope(msg.task_parameterlist);
                        break;
                    }
                case ElementDefine.COMMAND.TRIM_COUNT_OFFSET:
                    {
                        CountOffset(msg.task_parameterlist);
                        break;
                    }
                case ElementDefine.COMMAND.TRIM_RESET:
                    {
                        for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                        {
                            param = demparameterlist.parameterlist[i];
                            if (param == null) continue;
                            DataPoint dataPoint = GetDataPointByGuid(param.guid);
                            if (dataPoint == null) continue;
                            dataPoint.Reset();
                        }
                        break;
                    }
#endregion

#region Action buttons
                case ElementDefine.COMMAND.STANDBY_MODE:
                    ret = WriteWord(0x57, 0x7717);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = WriteWord(0x57, 0x0003);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    break;
                case ElementDefine.COMMAND.ACTIVE_MODE:
                    ret = WriteWord(0x57, 0x7717);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteWord(0x57, 0x0005);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.SHUTDOWN_MODE:
                    ret = WriteWord(0x57, 0x7717);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteWord(0x57, 0x000a);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.CFET_ON:
                    tmp = 0;
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x59, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    tmp |= 0x0002;
                    ret = WriteWord(0x59, tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x5b, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    if ((tmp & 0x0200) != 0x0200)
                    {
                        ret = ElementDefine.IDS_ERR_DEM_CFET_ON_FAILED;
                        return ret;
                    }
                    break;
                case ElementDefine.COMMAND.DFET_ON:
                    tmp = 0;
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x59, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    tmp |= 0x0001;
                    ret = WriteWord(0x59, tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x5b, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    if ((tmp & 0x0100) != 0x0100)
                    {
                        ret = ElementDefine.IDS_ERR_DEM_DFET_ON_FAILED;
                        return ret;
                    }
                    break;
                case ElementDefine.COMMAND.CFET_OFF:
                    tmp = 0;
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x59, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    tmp &= 0xfffd;
                    ret = WriteWord(0x59, tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x5b, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    if ((tmp & 0x0200) != 0x0000)
                    {
                        ret = ElementDefine.IDS_ERR_DEM_CFET_OFF_FAILED;
                        return ret;
                    }
                    break;
                case ElementDefine.COMMAND.DFET_OFF:
                    tmp = 0;
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x59, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    tmp &= 0xfffe;
                    ret = WriteWord(0x59, tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ReadWord(0x5b, ref tmp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    if ((tmp & 0x0100) != 0x0000)
                    {
                        ret = ElementDefine.IDS_ERR_DEM_DFET_OFF_FAILED;
                        return ret;
                    }
                    break;
                case ElementDefine.COMMAND.ATE_CRC_CHECK:
                    ret = CheckCRC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.STANDBY_THEN_ACTIVE_100MS:
                    ret = STA(100);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.ACTIVE_THEN_STANDBY_100MS:
                    ret = ATS(100);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.STANDBY_THEN_ACTIVE_50MS:
                    ret = STA(50);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.ACTIVE_THEN_STANDBY_50MS:
                    ret = ATS(50);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.STANDBY_THEN_ACTIVE_30MS:
                    ret = STA(30);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.ACTIVE_THEN_STANDBY_30MS:
                    ret = ATS(30);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.STANDBY_THEN_ACTIVE_20MS:
                    ret = STA(20);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.ACTIVE_THEN_STANDBY_20MS:
                    ret = ATS(20);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
#endregion

#region CurrentScan
                case ElementDefine.COMMAND.TRIGGER_8_CURRENT_4:


                    foreach (var p in msg.task_parameterlist.parameterlist)
                    {
                        if ((ElementDefine.SUBTYPE)p.subtype == ElementDefine.SUBTYPE.CURRENT)
                        {
                            MSG.task_parameterlist.parameterlist.Add(p);
                            Current = p;
                            break;
                        }
                    }
                    if (Current == null)
                        break;

                    for (int i = 1; i < 5; i++)
                    {
                        FolderMap.WriteFile("\r\n第" + i.ToString() + "次读取");
                        ret = ClearSarTriggerScanFlag();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = ChangeSarScanMode(ElementDefine.SAR_MODE.TRIGGER_8_TIME_CURRENT_SCAN);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = WaitForSarScanComplete();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        //ret = Read(ref MSG);

                        ret = ReadWord(0x12, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        parent.m_OpRegImg[0x12].err = ret;
                        parent.m_OpRegImg[0x12].val = wdata;

                        parent.ReadFromRegImg(Current, ref wdata);

                        AverageHex += (short)Current.hexdata;
                        FolderMap.WriteFile("Reg" + Current.reglist["Low"].address.ToString("X2") + " Hex Value is " + ((short)(Current.hexdata)).ToString());
                    }
                    AverageHex /= 4;

                    FolderMap.WriteFile("\r\n\t\tReg" + Current.reglist["Low"].address.ToString("X2") + " Average Hex Value is " + AverageHex.ToString());
                    decimal dtemp = Math.Round((decimal)AverageHex, 0, MidpointRounding.AwayFromZero);
                    short stemp = Convert.ToInt16(dtemp);
                    Current.hexdata = (ushort)stemp;

                    FolderMap.WriteFile("\t\tReg" + Current.reglist["Low"].address.ToString("X2") + " Average Hex Rounding Value is " + ((short)(Current.hexdata)).ToString());

                    parent.WriteToRegImg(Current, Current.hexdata);

                    ret = ReadCADC(ElementDefine.CADC_MODE.MOVING);
                    break;
                case ElementDefine.COMMAND.TRIGGER_8_CURRENT_8:


                    foreach (var p in msg.task_parameterlist.parameterlist)
                    {
                        if ((ElementDefine.SUBTYPE)p.subtype == ElementDefine.SUBTYPE.CURRENT)
                        {
                            MSG.task_parameterlist.parameterlist.Add(p);
                            Current = p;
                            break;
                        }
                    }
                    if (Current == null)
                        break;

                    for (int i = 1; i < 9; i++)
                    {
                        FolderMap.WriteFile("\r\n第" + i.ToString() + "次读取");
                        ret = ClearSarTriggerScanFlag();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = ChangeSarScanMode(ElementDefine.SAR_MODE.TRIGGER_8_TIME_CURRENT_SCAN);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = WaitForSarScanComplete();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        //ret = Read(ref MSG);

                        ret = ReadWord(0x12, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        parent.m_OpRegImg[0x12].err = ret;
                        parent.m_OpRegImg[0x12].val = wdata;

                        parent.ReadFromRegImg(Current, ref wdata);

                        AverageHex += (short)Current.hexdata;
                        FolderMap.WriteFile("Reg" + Current.reglist["Low"].address.ToString("X2") + " Hex Value is " + ((short)(Current.hexdata)).ToString());
                    }
                    AverageHex /= 8;

                    FolderMap.WriteFile("\r\n\t\tReg" + Current.reglist["Low"].address.ToString("X2") + " Average Hex Value is " + AverageHex.ToString());
                    dtemp = Math.Round((decimal)AverageHex, 0, MidpointRounding.AwayFromZero);
                    stemp = Convert.ToInt16(dtemp);
                    Current.hexdata = (ushort)stemp;

                    FolderMap.WriteFile("\t\tReg" + Current.reglist["Low"].address.ToString("X2") + " Average Hex Rounding Value is " + ((short)(Current.hexdata)).ToString());

                    parent.WriteToRegImg(Current, Current.hexdata);

                    ret = ReadCADC(ElementDefine.CADC_MODE.MOVING);
                    break;
                case ElementDefine.COMMAND.TRIGGER_8_CURRENT_1:

                    ret = ClearSarTriggerScanFlag();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = ChangeSarScanMode(ElementDefine.SAR_MODE.TRIGGER_8_TIME_CURRENT_SCAN);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WaitForSarScanComplete();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = ReadWord(0x12, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    parent.m_OpRegImg[0x12].err = ret;
                    parent.m_OpRegImg[0x12].val = wdata;

                    ret = ReadCADC(ElementDefine.CADC_MODE.MOVING);
                    break;
#endregion
            }

            return ret;
        }

        private uint ATS(int v)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteWord(0x57, 0x7717);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteWord(0x57, 0x0005);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            Thread.Sleep(v);

            ret = WriteWord(0x57, 0x7717);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            ret = WriteWord(0x57, 0x0003);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            return ret;
        }

        private uint STA(int v)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WriteWord(0x57, 0x7717);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteWord(0x57, 0x0003);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            Thread.Sleep(v);

            ret = WriteWord(0x57, 0x7717);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteWord(0x57, 0x0005);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            ushort wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0x58, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteWord(0x58, (ushort)(wdata | 0x0100));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            return ret;
        }
#endregion

#region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#else
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.hwversion = 0;
            deviceinfor.type = wval;

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (type != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }

            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                LibErrorCode.UpdateDynamicalErrorDescription(ret, new string[] { deviceinfor.shwversion });

            return ret;
#endif
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            msg.sm.dic.Clear();
            UInt32 cellnum = (UInt32)parent.CellNum.phydata + 10;    //0~7 means 7~14
            if (cellnum == 17)
            {
                for (byte i = 0; i < 17; i++)
                    msg.sm.dic.Add((uint)(i), true);
            }
            else
            {
                for (byte i = 0; i < 17; i++)
                {
                    if (i < cellnum - 1)
                        msg.sm.dic.Add((uint)i, true);
                    else if (i == cellnum - 1)
                        msg.sm.dic.Add(16, false);
                    else if (i < 16)
                        msg.sm.dic.Add((uint)i, false);
                    else if (i == 16)
                        msg.sm.dic.Add(cellnum - 1, true);
                }
            }

            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
#endregion

#region Trim Function
        private void InitDataPointList(ParamContainer demparameterlist)
        {//建构DataPoint清单，并获取input值
            DataPoint dataPoint = null;
            Parameter param = null;

            if ((ElementDefine.m_trim_count == 0) | (ElementDefine.m_trim_count == 5))
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
        public UInt32 ClearTrimAndOffset(ParamContainer demparameterlist)
        {
            UInt16 wval = 0;
            Reg regLow = null;
            Parameter param = null;
            Tuple<Parameter, Parameter> slope_offset = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WorkMode(ElementDefine.WORK_MODE.INTERNAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if (!parent.m_guid_slope_offset.ContainsKey(param.guid)) continue;
                slope_offset = parent.m_guid_slope_offset[param.guid];

                if (slope_offset.Item1 == null) continue;
                if (!slope_offset.Item1.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item1.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (slope_offset.Item2 == null) continue;
                if (!slope_offset.Item2.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item2.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = WorkMode(ElementDefine.WORK_MODE.NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        public UInt32 ReadParametersForSlope(int code,ref Parameter param)
        {
            byte badd = 0;
            UInt16 wdata = 0;
            Reg regLow = null;
            Tuple<Parameter, Parameter> slope_offset = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WorkMode(ElementDefine.WORK_MODE.INTERNAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (!parent.m_guid_slope_offset.ContainsKey(param.guid)) return LibErrorCode.IDS_ERR_SUCCESSFUL;
            slope_offset = parent.m_guid_slope_offset[param.guid];

            if (slope_offset.Item1 == null) return LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (!slope_offset.Item1.reglist.ContainsKey("Low")) return LibErrorCode.IDS_ERR_SUCCESSFUL;
            regLow = slope_offset.Item1.reglist["Low"];
            ret = ReadWord((byte)regLow.address, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            wdata &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
            wdata |= (UInt16)(code << regLow.startbit);
            ret = WriteWord((byte)regLow.address, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WorkMode(ElementDefine.WORK_MODE.NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (param.guid == ElementDefine.OP_CADC)
            {
                ret = ReadCADC(ElementDefine.CADC_MODE.TRIGGER);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wdata = parent.m_OpRegImg[0x38].val;
            }
            else
            {
                ret = ClearSarTriggerScanFlag();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = ReadWord(0x5f, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wdata &= 0xff20;
                wdata |= 0x0080;
                wdata |= (UInt16)((param.guid & 0x0000FF00) >> 8);
                ret = WriteWord(0x5f, wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = WaitForSarScanComplete();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                badd = (byte)((param.guid & 0x0000FF00) >> 8);

                ret = ReadWord(badd, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            short s = (short)wdata;
            param.phydata = s * param.phyref / param.regref;

            Thread.Sleep(2);
            return ret;
        }

        public UInt32 ReadParametersForOffset(ref Parameter param)
        {
            byte badd = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (param.guid == ElementDefine.OP_CADC)
            {
                ret = ReadCADC(ElementDefine.CADC_MODE.TRIGGER);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wdata = parent.m_OpRegImg[0x38].val;
            }
            else
            {
                ret = ClearSarTriggerScanFlag();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = ReadWord(0x5f, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wdata &= 0xff20;
                wdata |= 0x0080;
                wdata |= (UInt16)((param.guid & 0x0000FF00) >> 8);
                ret = WriteWord(0x5f, wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                ret = WaitForSarScanComplete();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                badd = (byte)((param.guid & 0x0000FF00) >> 8);

                ret = ReadWord(badd, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            short s = (short)wdata;
            param.phydata = s * param.phyref / param.regref;

            Thread.Sleep(2);
            return ret;
        }


        public Parameter GetParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid)) return param;
            }
            return null;
        }

        private UInt32 CountSlope(ParamContainer demparameterlist)
        {
            int slope = 0;
            DataPoint dataPoint = null;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
                    case ElementDefine.OP_CELL1V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL1V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL2V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL2V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL3V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL3V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL4V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL4V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL5V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL5V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL6V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL6V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL7V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL7V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL8V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL8V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL9V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL9V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL10V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL10V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL11V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL11V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL12V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL12V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL13V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL13V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL14V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL14V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL15V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL15V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL16V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL16V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL17V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL17V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_PACK_CUR:
                        param = GetParameterByGuid(ElementDefine.OP_ISENSE_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_VAUX:
                        param = GetParameterByGuid(ElementDefine.OP_VAUX_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_VBATT:
                        param = GetParameterByGuid(ElementDefine.OP_VBATT_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CADC:
                        param = GetParameterByGuid(ElementDefine.OP_CADC_SLOP, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                param.phydata = slope;
            }
            return ret;
        }

        private UInt32 CountOffset(ParamContainer demparameterlist)
        {
            double offset = 0;
            DataPoint dataPoint = null;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
                    case ElementDefine.OP_CELL1V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL1V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL2V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL2V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL3V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL3V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL4V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL4V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL5V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL5V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL6V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL6V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL7V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL7V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL8V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL8V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL9V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL9V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL10V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL10V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL11V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL11V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL12V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL12V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL13V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL13V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL14V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL14V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL15V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL15V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL16V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL16V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL17V:
                        param = GetParameterByGuid(ElementDefine.OP_CELL17V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_PACK_CUR:
                        param = GetParameterByGuid(ElementDefine.OP_ISENSE_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_VAUX:
                        param = GetParameterByGuid(ElementDefine.OP_VAUX_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_VBATT:
                        param = null;
                        break;
                    case ElementDefine.OP_CADC:
                        param = GetParameterByGuid(ElementDefine.OP_CADC_OFFSET, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                if(param.guid == ElementDefine.OP_ISENSE_OFFSET)
                    param.phydata = -offset;
                else
                    param.phydata = offset;
                parent.Physical2Hex(ref param);
            }
            return ret;
        }
#endregion
    }
}