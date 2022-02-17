//#define debug
//#if debug
//#define functiontimeout
//#define pec
//#define frozen
//#define dirty
//#define readback
//#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
using System.Windows.Forms;

namespace Cobra.KALL16H
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
        #endregion

        #region EFUSE寄存器操作
        #region EFUSE寄存器父级操作
        internal UInt32 YFLASHReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnYFLASHReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 YFLASHWriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnYFLASHWriteWord(reg, val);
            }
            return ret;
        }
        #endregion

        #region EFUSE寄存器子级操作

        protected UInt32 OnWorkMode(ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnWriteWord(ElementDefine.OR_WR_CMD, ElementDefine.WORKMODE_OFFSET, (byte)wkm);
            return ret;
        }

        protected UInt32 OnYFLASHReadWord(byte reg, ref UInt16 pval)
        {
            return OnReadWord(ElementDefine.EF_RD_CMD, reg, ref pval);
        }

        protected UInt32 OnYFLASHWriteWord(byte reg, UInt16 val)
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
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (YFLASHReglist.Count != 0)
            {
                foreach (byte badd in YFLASHReglist)
                {
                    ret = YFLASHReadWord(badd, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    parent.m_YFLASHRegImg[badd].err = ret;
                    parent.m_YFLASHRegImg[badd].val = wdata;
                }

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)    //因为Efuse是在Expert页面写，所以没有复杂逻辑
        {
            Reg reg = null;
            byte baddress = 0;
            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            foreach (byte badd in YFLASHReglist)
            {
                ret = OnYFLASHWriteWord(badd, parent.m_YFLASHRegImg[badd].val);
                parent.m_YFLASHRegImg[badd].err = ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
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

            List<Parameter> YFLASHParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            YFLASHParamList.Add(p);
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

            List<Parameter> YFLASHParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            YFLASHParamList.Add(p);
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

            if (YFLASHParamList.Count != 0)
            {
                for (int i = 0; i < YFLASHParamList.Count; i++)
                {
                    param = (Parameter)YFLASHParamList[i];
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

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                default:
                    break;
            }
            return ret;
        }
        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
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