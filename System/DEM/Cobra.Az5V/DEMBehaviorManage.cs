//#define sim
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.Az5D
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
        private Dictionary<UInt32, double> dic = new Dictionary<UInt32, double>();

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
        }

        #region EFUSE操作常量定义
        private const int RETRY_COUNTER = 5;
        private const byte EFUSE_DATA_OFFSET = 0x80;
        private const byte EFUSE_MAP_OFFSET = 0x90;

        // EFUSE operation code
        private const byte EFUSE_WORKMODE_NORMAL = 0x00;
        private const byte EFUSE_WORKMODE_WRITE_MAP_CTRL = 0x01;
        private const byte EFUSE_WORKMODE_PROGRAMMING = 0x02;

        // EFUSE control registers' addresses
        private const byte EFUSE_WORKMODE_REG = 0x18;
        private const byte EFUSE_TESTCTR_REG = 0x19;
        private const byte EFUSE_MAP_REG = 0x08;
        private const byte EFUSE_ATE_FROZEN_REG = 0x0D;
        private const byte EFUSE_USER_FROZEN_REG = 0x0F;

        // EFUSE Control Flags
        private const UInt16 EFUSE_ATE_FROZEN_FLAG = 0x0080;
        private const UInt16 EFUSE_MAP_FLAG = 0x0010;
        private const UInt16 ALLOW_WR_FLAG = 0x8000;
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

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
#if sim
                pval = (UInt16)new Random().Next(15);
#else
                ret = OnReadWord(reg, ref pval);
#endif
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
#if sim
                return ret;
#else
                ret = OnWriteWord(reg, val);
#endif
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

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
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
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
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
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
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
            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
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
        protected UInt32 OnWorkMode(ElementDefine.COBRA_AZALEA5V_WKM wkm)
        {
            byte blow = 0;
            byte bhigh = 0;
            UInt16 wdata = 0;
            UInt16 wdata1 = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (wkm == ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_NORMAL)
            {
                ret = OnReadWord(EFUSE_TESTCTR_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnReadWord(EFUSE_WORKMODE_REG, ref wdata1);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wdata & ElementDefine.TestCtrl_FLAG) == 0)
                {
                    wdata1 &= ElementDefine.ALLOW_WR_CLEAR_FLAG;
                    ret = OnWriteWord(EFUSE_WORKMODE_REG, wdata1);
                    return ret;
                }
                return ret;
            }

            ret = OnReadWord(EFUSE_WORKMODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteWord(EFUSE_WORKMODE_REG, (UInt16)(wdata | ALLOW_WR_FLAG));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnReadWord(EFUSE_WORKMODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            blow = (byte)(SharedFormula.LoByte(wdata) & 0xFC);
            bhigh = (byte)SharedFormula.HiByte(wdata);
            blow |= (byte)wkm;
            wdata = (UInt16)SharedFormula.MAKEWORD(blow, bhigh);
            ret = OnWriteWord(EFUSE_WORKMODE_REG, wdata);
            return ret;
        }

        protected UInt32 OnTestCtrl(ElementDefine.COBRA_AZALEA5V_TESTCTRL ctrl)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (ctrl == ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_NORMAL)
            {
                ret = OnReadWord(EFUSE_WORKMODE_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wdata &= ElementDefine.ALLOW_WR_CLEAR_FLAG;
                ret = OnWriteWord(EFUSE_WORKMODE_REG, wdata);
                return ret;

            }
            ret = OnWorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnReadWord(EFUSE_TESTCTR_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata &= 0xFFF0;
            wdata |= (UInt16)ctrl;
            ret = OnWriteWord(EFUSE_TESTCTR_REG, (UInt16)wdata);
            return ret;
        }

        protected UInt32 OnEFUSEReadWord(byte reg, ref UInt16 pval)
        {
            return OnReadWord(reg, ref pval);
        }

        protected UInt32 OnEFUSEWriteWord(byte reg, UInt16 val)
        {
            return OnWriteWord(reg, val);
        }

        protected UInt32 OnWaitMapCompleted()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                ret = OnReadWord(EFUSE_MAP_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & EFUSE_MAP_FLAG) == EFUSE_WORKMODE_NORMAL)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }
        #endregion
        #endregion

        #region EFUSE功能操作
        protected UInt32 WorkMode(ElementDefine.COBRA_AZALEA5V_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }

        protected UInt32 TestCtrl(ElementDefine.COBRA_AZALEA5V_TESTCTRL ctrl)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnTestCtrl(ctrl);
            }
            return ret;
        }

        protected UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
                ret = OnReadWord(EFUSE_MAP_REG, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(EFUSE_MAP_REG, (UInt16)(wdata | EFUSE_MAP_FLAG));
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWaitMapCompleted();
            }

            return ret;
        }
        #endregion

        #region 基础服务功能设计
        byte crc7_calc(byte[] pp, int len)
        {
            byte i;
            int j = 0;
            byte crc = 0;
            while (len-- > 0)
            {
                for (i = 0x80; i != 0; i >>= 1)
                {
                    //判断第7位是否为1
                    if ((crc & 0x40) != 0)
                    {
                        crc <<= 1;
                        crc ^= 9;
                    }
                    else
                    {
                        crc <<= 1;
                    }

                    if ((pp[j] & i) != 0)
                    {
                        crc ^= 9;
                    }
                }
                j++;
            }

            return (byte)(crc & 0x7F);
        }

        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            return BlockRead();
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
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
                foreach (byte badd in EFUSEReglist)
                {
                    ret = ReadWord((byte)(badd + EFUSE_DATA_OFFSET), ref wdata);
                    parent.m_EFRegImg[badd].err = ret;
                    parent.m_EFRegImg[badd].val = wdata;
                }
            }

            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> OpEfuseReglist = new List<byte>();
            List<byte> OpMapReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
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

            //Write 
            if (EFUSEReglist.Count != 0)
            {
                byte max = EFUSEReglist.Max();
                byte min = EFUSEReglist.Min();
                if (max <= EFUSE_ATE_FROZEN_REG)
                {
                    ret = ReadWord((byte)(EFUSE_ATE_FROZEN_REG + EFUSE_DATA_OFFSET), ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((wdata & EFUSE_ATE_FROZEN_FLAG) == EFUSE_ATE_FROZEN_FLAG)
                    {
                        msg.gm.message = "ATE zone had been forzen,can not be written!";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        return ret;
                    }
                }
                else if (min > EFUSE_ATE_FROZEN_REG)
                {
                    ret = ReadWord((byte)(EFUSE_USER_FROZEN_REG + EFUSE_DATA_OFFSET), ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((wdata & EFUSE_ATE_FROZEN_FLAG) == EFUSE_ATE_FROZEN_FLAG)
                    {
                        msg.gm.message = "User zone had been forzen,can not be written!";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        return ret;
                    }
                }
                else
                {
                    ret = ReadWord((byte)(EFUSE_ATE_FROZEN_REG + EFUSE_DATA_OFFSET), ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((wdata & EFUSE_ATE_FROZEN_FLAG) == EFUSE_ATE_FROZEN_FLAG)
                    {
                        msg.gm.message = "ATE zone had been forzen,can not be written!";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        return ret;
                    }

                    ret = ReadWord((byte)(EFUSE_USER_FROZEN_REG + EFUSE_DATA_OFFSET), ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((wdata & EFUSE_ATE_FROZEN_FLAG) == EFUSE_ATE_FROZEN_FLAG)
                    {
                        msg.gm.message = "User zone had been forzen,can not be written!";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        return ret;
                    }
                }

                msg.gm.message = "Please change to program voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in EFUSEReglist)
                {
                    ret = EFUSEWriteWord((byte)(badd + EFUSE_DATA_OFFSET), parent.m_EFRegImg[badd].val);
                    parent.m_EFRegImg[badd].err = ret;
                }

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            if (OpEfuseReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in OpEfuseReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            if (OpMapReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in OpMapReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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

            List<Parameter> EFUSEParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
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

        public UInt32 Command(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            switch ((ElementDefine.COBRA_AZALEA5V_TESTCTRL)msg.sub_task)
            {
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_NORMAL:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_VR26V_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_OSC512K_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_DOC_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_THM_RESISTOR_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_LEVEL_SHIFTR_TEST:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_INTVTS_OFFSET_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_SLOPE_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_CELL_BALANCE_TRIM:
                case ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_KEY_SIGNAL:
                    {
                        ret = TestCtrl((ElementDefine.COBRA_AZALEA5V_TESTCTRL)msg.sub_task);
                    }
                    break;
            }
            #region 支持command指令
            switch ((ElementDefine.COBRA_COMMAND_MODE)msg.sub_task)
            {
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_TRIGGER_SCAN_EIGHT_MODE:
                    {
                        foreach (Parameter p in demparameterlist.parameterlist)
                            p.sphydata = string.Empty;

                        #region 准备寄存器初始化
                        ret = ClearTrimAndOffset();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        #region Trim 1 tims
                        for (UInt16 code = 0; code < 32; code++)
                        {
                            ret = WriteTrimCode(code);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            dic.Clear();//清除上一次缓存数据量
                            for (int n = 0; n < ElementDefine.nTrim_Times; n++)
                            {
                                ret = Read(ref msg);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                param = demparameterlist.GetParameterByGuid(ElementDefine.OP_Packc_8);
                                if (param != null) param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;

                                param = demparameterlist.GetParameterByGuid(ElementDefine.OP_THM20UA_8);
                                if (param != null) param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;

                                ret = ConvertHexToPhysical(ref msg);
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                param = demparameterlist.GetParameterByGuid(ElementDefine.OP_Packc_8);
                                if (param != null) param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT;

                                param = demparameterlist.GetParameterByGuid(ElementDefine.OP_THM20UA_8);
                                if (param != null) param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_20UA;

                                for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                                {
                                    param = (Parameter)demparameterlist.parameterlist[i];
                                    if (param == null) continue;
                                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                                    if (!dic.Keys.Contains(param.guid))
                                        dic.Add(param.guid, param.phydata);
                                    else
                                        dic[param.guid] += param.phydata;
                                }
                            }

                            foreach (UInt32 key in dic.Keys)
                            {
                                param = msg.task_parameterlist.GetParameterByGuid(key);
                                param.sphydata += string.Format("{0:F4},", dic[key] / ElementDefine.nTrim_Times);
                            }
                        }
                        #endregion
                    }
                    break;
            }
            #endregion
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
            ival = (int)(SharedFormula.LoByte(wval) & 0x03);
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)(SharedFormula.LoByte(wval) & 0x03);

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (SharedFormula.HiByte(type) != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if (((SharedFormula.LoByte(type) & 0x30) >> 4) != deviceinfor.hwversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if ((SharedFormula.LoByte(type) & 0x03) != deviceinfor.hwsubversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            #region 支持command指令
            switch ((ElementDefine.COBRA_COMMAND_MODE)msg.sub_task)
            {
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_CTO_MODE:
                    {
                        ShowTriggerOneParameter(demparameterlist.parameterlist, false);
                        ShowTriggerEightParameter(demparameterlist.parameterlist,false);
                    }
                    ShowTriggerCTOParameter(demparameterlist.parameterlist,true);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ALL_MODE:
                    ShowAllParameter(demparameterlist.parameterlist, true);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_ONE_MODE:
                    {
                        ShowTriggerCTOParameter(demparameterlist.parameterlist, false);
                        ShowTriggerEightParameter(demparameterlist.parameterlist, false);
                    }
                    ShowTriggerOneParameter(demparameterlist.parameterlist,true);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_EIGHT_MODE:
                    {
                        ShowTriggerCTOParameter(demparameterlist.parameterlist, false);
                        ShowTriggerOneParameter(demparameterlist.parameterlist, false);
                    }
                    ShowTriggerEightParameter(demparameterlist.parameterlist);
                    break;
            }
            #endregion
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
        #endregion

        #region UI参数显示
        private void ShowAllParameter(AsyncObservableCollection<Parameter> paramList, bool bshow)
        {
            foreach (Parameter p in paramList)
                p.bShow = bshow;
        }

        private void ShowTriggerOneParameter(AsyncObservableCollection<Parameter> paramList, bool bshow = true)
        {
            UInt32 guid = ElementDefine.OperationElement;
            for (int i = 0; i < 6; i++)
            {
                guid = (UInt32)((ElementDefine.OP_INTMP >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList,bshow);
            }
            ShowParameterByGuid(ElementDefine.OP_THM20UA, paramList,bshow);
            ShowParameterByGuid(ElementDefine.OP_THM120UA, paramList,bshow);
            ShowParameterByGuid(ElementDefine.OP_Packc, paramList,bshow);
            for (int i = 0; i < 4; i++)
            {
                guid = (UInt32)((ElementDefine.OP_Packv >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList, bshow);
            }
        }

        private void ShowTriggerEightParameter(AsyncObservableCollection<Parameter> paramList,bool bshow = true)
        {
            UInt32 guid = ElementDefine.OperationElement;
            for (int i = 0; i < 6; i++)
            {
                guid = (UInt32)((ElementDefine.OP_INTMP_8 >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList, bshow);
            }
            ShowParameterByGuid(ElementDefine.OP_THM20UA_8, paramList, bshow);
            ShowParameterByGuid(ElementDefine.OP_THM120UA_8, paramList, bshow);
            ShowParameterByGuid(ElementDefine.OP_Packc_8, paramList, bshow);
            for (int i = 0; i < 4; i++)
            {
                guid = (UInt32)((ElementDefine.OP_Packv_8 >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList, bshow);
            }
        }

        private void ShowTriggerCTOParameter(AsyncObservableCollection<Parameter> paramList,bool bshow = true)
        {
            UInt32 guid = ElementDefine.OperationElement;
            for (int i = 0; i < 5; i++)
            {
                guid = (UInt32)((ElementDefine.OP_CELL1V_CTO >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList, bshow);
            }
        }
        #endregion

        public UInt32 ClearTrimAndOffset()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = TestCtrl(ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_SLOPE_TRIM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Clear 0x93
            ret = ReadWord((byte)(0x03 + EFUSE_MAP_OFFSET), ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WriteWord((byte)(0x03 + EFUSE_MAP_OFFSET), (UInt16)(wval & 0xF000));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (byte i = 0x04; i <= 0x08; i++)
            {
                ret = WriteWord((byte)(i + EFUSE_MAP_OFFSET), 0);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }

            for (byte i = 0x09; i <= 0x0C; i++)
            {
                ret = WriteWord((byte)(i + EFUSE_MAP_OFFSET), 0);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        private UInt32 WriteTrimCode(UInt16 code)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = TestCtrl(ElementDefine.COBRA_AZALEA5V_TESTCTRL.EFUSE_TESTCTRL_SLOPE_TRIM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            code = (UInt16)(code << 1);
            //Write 0x93
            ret = ReadWord((byte)(0x03 + EFUSE_MAP_OFFSET), ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xF000;
            wval |= (UInt16)((code << 6) | code);
            ret = WriteWord((byte)(0x03 + EFUSE_MAP_OFFSET), wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Write 0x94
            ret = ReadWord((byte)(0x04 + EFUSE_MAP_OFFSET), ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xF000;
            wval |= (UInt16)((code << 6) | code);
            ret = WriteWord((byte)(0x04 + EFUSE_MAP_OFFSET), wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Write 0x95
            ret = WriteWord((byte)(0x05 + EFUSE_MAP_OFFSET), (UInt16)code);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Write 0x97
            ret = ReadWord((byte)(0x07 + EFUSE_MAP_OFFSET), ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xF000;
            wval |= (UInt16)((code << 6) | code);
            ret = WriteWord((byte)(0x07 + EFUSE_MAP_OFFSET), wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //Write 0x98
            ret = ReadWord((byte)(0x08 + EFUSE_MAP_OFFSET), ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wval &= 0xFFC0;
            wval |= (UInt16)code;
            ret = WriteWord((byte)(0x08 + EFUSE_MAP_OFFSET), wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WorkMode(ElementDefine.COBRA_AZALEA5V_WKM.EFUSE_WORKMODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            /*for (byte i = 0x06; i <= 0x15; i++)
                ret = MapWriteWord((byte)(i + 0xD0), 0);*/
            return ret;
        }

        public void ShowParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist,bool bshow = true)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param == null) continue;
                if (param.guid.Equals(guid))
                {
                    if (param.bShow != bshow)
                        param.bShow = bshow;
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
    }
}