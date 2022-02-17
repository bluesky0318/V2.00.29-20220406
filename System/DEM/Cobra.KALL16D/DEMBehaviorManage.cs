using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cobra.Communication;
using Cobra.Common;
using System.IO;
using System.Text;

namespace Cobra.KALL16D
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
        private List<DataPoint> m_dataPoint_List = new List<DataPoint>();
        private Dictionary<string, string> Verify_Dic = new Dictionary<string, string>();
        UInt16[] EFUSEUSRbuf = new UInt16[ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1];      //Used for read back check

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
        }

        #region EFUSE操作常量定义
        private const int RETRY_COUNTER = 5;
        private const byte EFUSE_DATA_OFFSET = 0x80;
        private const byte EFUSE_MAP_OFFSET = 0xA0;
        private const byte EFUSE_USERFRZ_REG = 0x1E;

        // EFUSE operation code
        private const byte EFUSE_WORKMODE_NORMAL = 0x00;
        private const byte EFUSE_WORKMODE_WRITE_MAP_CTRL = 0x01;
        private const byte EFUSE_WORKMODE_PROGRAMMING = 0x02;

        // EFUSE control registers' addresses
        private const byte EFUSE_WORKMODE_REG = 0x70;
        private const byte EFUSE_TESTCTR_REG = 0x71;
        private const byte EFUSE_MAP_REG = 0x56;
        private const byte EFUSE_ATE_FROZEN_REG = 0x10;
        private const byte OP_RSVDR = 0x55;

        // EFUSE Control Flags
        private const UInt16 EFUSE_CHECK_FLAG = 0xC000;
        private const UInt16 EFUSE_FROZEN_FLAG = 0x8000;
        private const UInt16 EFUSE_MAP_FLAG = 0x0001;
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
                ret = OnReadWord(reg, ref pval);
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
        protected UInt32 OnWorkMode(ElementDefine.COBRA_KALL14D_WKM wkm)
        {
            byte blow = 0;
            byte bhigh = 0;
            UInt16 wdata = 0;
            UInt16 wdata1 = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (wkm == ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_NORMAL)
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

            if (wkm == ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_PROGRAM)
            {
                ret = OnReadWord(0x5B, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wdata &= 0xFFFE;
                ret = OnWriteWord(0x5B, wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (int i = 0; i < RETRY_COUNTER; i++)
                {
                    ret = OnReadWord(0x68, ref wdata1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    if ((wdata & 0x0100) == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
                return ret;
            }
            return ret;
        }

        protected UInt32 OnEFUSEReadWord(byte reg, ref UInt16 pval)
        {
            return OnReadWord(reg, ref pval);
        }

        protected UInt32 OnEFUSEWriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
        protected UInt32 WorkMode(ElementDefine.COBRA_KALL14D_WKM wkm)
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
                    case ElementDefine.EPROMElement:
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

            try
            {
                if ((string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "RegisterConfig".ToLower()) == 0)
                    | (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "EPROMConfig".ToLower()) == 0))
                {
                    if (msg.task_parameterlist.parameterlist.Count == 1) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                        return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;
                }
            }
            catch { }
            //Read 
            if (EFUSEReglist.Count != 0)
            {
                foreach (byte badd in EFUSEReglist)
                {
                    ret = ReadWord(badd, ref wdata);
                    parent.m_EFRegImg[badd].err = ret;
                    parent.m_EFRegImg[badd].val = wdata;
                }
            }

            if (OpReglist.Count != 0)
            {
                foreach (byte badd in OpReglist)
                {
                    ret = ReadWord(badd, ref wdata);
                    parent.m_OpRegImg[badd].err = ret;
                    parent.m_OpRegImg[badd].val = wdata;
                }
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> OpMapReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EPROMElement:
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
                                if ((baddress < (EFUSE_MAP_OFFSET + 0x20)) && (baddress >= EFUSE_MAP_OFFSET))
                                    OpMapReglist.Add(baddress);
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
            OpReglist = OpReglist.Distinct().ToList();

            try
            {
                if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "EPROMConfig".ToLower()) == 0)
                {
                    if (msg.task_parameterlist.parameterlist.Count == 1) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                        return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;
                }
                if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "RegisterConfig".ToLower()) == 0)
                {
                    if (msg.task_parameterlist.parameterlist.Count == 1) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                        return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

                    ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    foreach (byte badd in OpMapReglist)
                    {
                        ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                        parent.m_OpRegImg[badd].err = ret;
                    }
                    ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_NORMAL);
                    return ret;
                }
            }
            catch { }

            //Write 
            if (EFUSEReglist.Count != 0)
            {
                ret = ReadWord(EFUSE_USERFRZ_REG + EFUSE_DATA_OFFSET, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & EFUSE_CHECK_FLAG) == EFUSE_FROZEN_FLAG)
                {
                    msg.gm.message = "User zone had been forzen,can not be written!";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                    return ret;
                }

                ret = WriteWord(OP_RSVDR, 0x0002);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please change to program voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                foreach (byte badd in EFUSEReglist)
                {
                    ret = EFUSEWriteWord(badd, parent.m_EFRegImg[badd].val);
                    parent.m_EFRegImg[badd].err = ret;
                    Thread.Sleep(10);
                }

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            foreach (byte badd in OpMapReglist)
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

            List<Parameter> EFUSEParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EPROMElement:
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
                    case ElementDefine.EPROMElement:
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
            UInt16 wval = 0;
            UInt16 old_subtype = 0;
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            #region 支持command指令
            switch ((ElementDefine.COBRA_COMMAND_MODE)msg.sub_task)
            {
                #region Scan SFL commands
                case ElementDefine.COBRA_COMMAND_MODE.INVALID_COMMAND:
                    {
                        #region Scan Mode
                        switch (parent.scan_mode)
                        {
                            case ElementDefine.SCAN_MODE.TRIGGER:
                                {
                                    ret = DisableAutoScan();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = WaitTriggerCompleted();
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                                    ret = EnableTriggerScan();
                                }
                                break;
                            case ElementDefine.SCAN_MODE.AUTO:
                                {
                                    ret = EnableAutoScan();
                                }
                                break;
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_UI:
                    {
                        var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                        #region CADC
                        switch (json["CADCMode"])
                        {
                            case "Disable":
                                parent.cadc_mode = ElementDefine.CADC_MODE.DISABLE;
                                break;
                            case "Trigger":
                                parent.cadc_mode = ElementDefine.CADC_MODE.TRIGGER;
                                break;
                            case "Moving":
                                parent.cadc_mode = ElementDefine.CADC_MODE.MOVING;
                                break;
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        switch (json["fs_osr"])
                        {
                            case "3xOSR for one ADC data":
                                wval &= 0xFFFB;
                                break;
                            case "4xOSR for one ADC data":
                                wval |= 0x0004;
                                break;
                        }
                        switch (json["isens_osr"])
                        {
                            case "14bits with OSR=128":
                                wval &= 0xFFFD;
                                break;
                            case "16bits with OSR=256":
                                wval |= 0x0002;
                                break;
                        }
                        switch (json["volt_osr"])
                        {
                            case "14bits with OSR=128":
                                wval &= 0xFFFE;
                                break;
                            case "16bits with OSR=256":
                                wval |= 0x0001;
                                break;
                        }
                        switch (json["ScanMode"])
                        {
                            case "TrigMode":
                                parent.scan_mode = ElementDefine.SCAN_MODE.TRIGGER;
                                parent.trigger_sw_osr = (UInt16)(wval << 8);
                                break;
                            case "AutoMode":
                                parent.scan_mode = ElementDefine.SCAN_MODE.AUTO;
                                parent.auto_sw_osr = (UInt16)(wval << 11);
                                break;
                        }
                    }
                    break;
                #endregion
                #region SCS SFL Command
                case ElementDefine.COBRA_COMMAND_MODE.SCS_TRIGGER_SCAN_EIGHT_MODE:
                    {
                        ret = SetCADCMode(ElementDefine.CADC_MODE.TRIGGER);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
                #endregion
                #region Trim SFL Command
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_SLOPE_EIGHT_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);
                        #region 准备寄存器初始化
                        ret = ClearTrimAndOffset(msg.task_parameterlist);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion

                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = EnableTriggerScan(ElementDefine.DEFAULT_OSR, true);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = WaitTriggerCompleted();
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                                if (param.guid == ElementDefine.CADC)
                                {
                                    ret = SetCADCMode(ElementDefine.CADC_MODE.TRIGGER);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                }
                                old_subtype = param.subtype;
                                param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;
                                m_parent.Hex2Physical(ref param);
                                param.subtype = old_subtype;
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                            }
                        }
                        foreach (UInt32 key in dic.Keys)
                        {
                            DataPoint dataPoint = GetDataPointByGuid(key);
                            dataPoint.SetOutput(dic[key] / 8);
                        }
                        ElementDefine.m_trim_count++;
                        #endregion
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_OFFSET_EIGHT_MODE:
                    {
                        dic.Clear();
                        InitDataPointList(msg.task_parameterlist);

                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = EnableTriggerScan(ElementDefine.DEFAULT_OSR, true);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = WaitTriggerCompleted();
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                                if (param.guid == ElementDefine.CADC)
                                {
                                    ret = SetCADCMode(ElementDefine.CADC_MODE.TRIGGER);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                }
                                old_subtype = param.subtype;
                                param.subtype = (UInt16)ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SIGN_ORG;
                                m_parent.Hex2Physical(ref param);
                                param.subtype = old_subtype;
                                if (!dic.Keys.Contains(param.guid))
                                    dic.Add(param.guid, param.phydata);
                                else
                                    dic[param.guid] += param.phydata;
                            }
                        }
                        foreach (UInt32 key in dic.Keys)
                        {
                            DataPoint dataPoint = GetDataPointByGuid(key);
                            dataPoint.SetOutput(dic[key] / 8);
                        }
                        ElementDefine.m_trim_count++;
                        #endregion
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_COUNT_SLOPE:
                    {
                        CountSlope(msg.task_parameterlist);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.TRIM_COUNT_OFFSET:
                    {
                        CountOffset(msg.task_parameterlist);
                        break;
                    }
                #endregion
                #region Mass Production SFL
                case ElementDefine.COBRA_COMMAND_MODE.ATE_EMPTY_CHECK:
                    ret = ATEEmptyCheck();
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.FROZEN_BIT_CHECK:
                    ret = FrozenBitCheck();
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.DIRTY_CHIP_CHECK:
                    ret = DirtyChipCheck();
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.DOWNLOAD_WITH_POWER_CONTROL:
                    ret = Download(msg.sm.efusebindata, true);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.DOWNLOAD_WITHOUT_POWER_CONTROL:
                    ret = Download(msg.sm.efusebindata, false);
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.READ_BACK_CHECK:
                    ret = ReadBackCheck();
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.ATE_CRC_CHECK:
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.SAVE_EFUSE_HEX:
                    {
                        InitEfuseData();
                        ret = ConvertPhysicalToHex(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        PrepareHexData();
                        ret = GetEfuseHexData(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        FileStream hexfile = new FileStream(msg.sub_task_json, FileMode.Create);
                        StreamWriter hexsw = new StreamWriter(hexfile);
                        hexsw.Write(msg.sm.efusehexdata);
                        hexsw.Close();
                        hexfile.Close();
                        ret = GetEfuseBinData(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        string binfilename = Path.Combine(Path.GetDirectoryName(msg.sub_task_json),
                            Path.GetFileNameWithoutExtension(msg.sub_task_json) + ".bin");
                        Encoding ec = Encoding.UTF8;
                        using (BinaryWriter bw = new BinaryWriter(File.Open(binfilename, FileMode.Create), ec))
                        {
                            foreach (var b in msg.sm.efusebindata)
                                bw.Write(b);
                            bw.Close();
                        }
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.BIN_FILE_CHECK:
                    {
                        string binFileName = msg.sub_task_json;
                        var blist = SharedAPI.LoadBinFileToList(binFileName);
                        if (blist.Count == 0)
                            ret = LibErrorCode.IDS_ERR_DEM_LOAD_BIN_FILE_ERROR;
                        else
                            ret = CheckBinData(blist);
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.GET_MAX_VALUE:
                    {
                        param = msg.task_parameterlist.parameterlist[0];
                        double maxvalue = 0;
                        for (long i = param.dbHexMin; i <= param.dbHexMax; i++)
                        {
                            parent.WriteToRegImg(param, (ushort)i);
                            parent.Hex2Physical(ref param);
                            if (maxvalue < param.phydata)
                                maxvalue = param.phydata;
                        }
                        param.dbPhyMax = maxvalue;
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.GET_MIN_VALUE:
                    {
                        param = msg.task_parameterlist.parameterlist[0];
                        double minvalue = 99999;
                        for (long i = param.dbHexMin; i <= param.dbHexMax; i++)
                        {
                            parent.WriteToRegImg(param, (ushort)i);
                            parent.Hex2Physical(ref param);
                            if (minvalue > param.phydata)
                                minvalue = param.phydata;
                        }
                        param.dbPhyMin = minvalue;
                        break;
                    }
                case ElementDefine.COBRA_COMMAND_MODE.VERIFICATION:    //先从文件中读出数据到EFUSEBuffer,然后读回检查
                    {
                        Dictionary<string, ushort> EFRegImg = LoadEFRegImgFromEFUSEBin(msg.sm.efusebindata);
                        WriteToEFUSEBuffer(EFRegImg);
                        ret = ReadBackCheck();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    }
                    #endregion
            }
            #endregion
            return ret;
        }
        #endregion

        #region Trim Count
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

        private UInt32 CountSlope(ParamContainer demparameterlist)
        {
            double slope = 0;
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
                    case ElementDefine.CellVoltage01:
                        param = GetParameterByGuid(ElementDefine.Cell1_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.Cell2_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage03:
                        param = GetParameterByGuid(ElementDefine.Cell3_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage04:
                        param = GetParameterByGuid(ElementDefine.Cell4_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage05:
                        param = GetParameterByGuid(ElementDefine.Cell5_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage06:
                        param = GetParameterByGuid(ElementDefine.Cell6_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage07:
                        param = GetParameterByGuid(ElementDefine.Cell7_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage08:
                        param = GetParameterByGuid(ElementDefine.Cell8_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage09:
                        param = GetParameterByGuid(ElementDefine.Cell9_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage10:
                        param = GetParameterByGuid(ElementDefine.Cell10_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage11:
                        param = GetParameterByGuid(ElementDefine.Cell11_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage12:
                        param = GetParameterByGuid(ElementDefine.Cell12_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage13:
                        param = GetParameterByGuid(ElementDefine.Cell13_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage14:
                        param = GetParameterByGuid(ElementDefine.Cell14_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage15:
                        param = GetParameterByGuid(ElementDefine.Cell15_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage16:
                        param = GetParameterByGuid(ElementDefine.Cell16_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.Isens:
                        param = GetParameterByGuid(ElementDefine.Isens_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VBATT:
                        param = GetParameterByGuid(ElementDefine.VBATT_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.TS:
                        param = GetParameterByGuid(ElementDefine.TS_Slope_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CADC:
                        param = GetParameterByGuid(ElementDefine.CADC_Slope_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                if (param.guid == ElementDefine.CADC_Slope_Trim)
                    param.phydata = Math.Round((1 - slope) * 4096, 0);
                else
                    param.phydata = Math.Round((1 - slope) * 2048, 0);
                switch (param.guid)
                {
                    case ElementDefine.Cell1_Slope_Trim:
                    case ElementDefine.Cell2_Slope_Trim:
                    case ElementDefine.Cell3_Slope_Trim:
                    case ElementDefine.Cell4_Slope_Trim:
                    case ElementDefine.Cell5_Slope_Trim:
                    case ElementDefine.Cell6_Slope_Trim:
                    case ElementDefine.Cell7_Slope_Trim:
                    case ElementDefine.Cell8_Slope_Trim:
                    case ElementDefine.Cell9_Slope_Trim:
                    case ElementDefine.Cell10_Slope_Trim:
                    case ElementDefine.Cell11_Slope_Trim:
                    case ElementDefine.Cell12_Slope_Trim:
                    case ElementDefine.Cell13_Slope_Trim:
                    case ElementDefine.Cell14_Slope_Trim:
                    case ElementDefine.Cell15_Slope_Trim:
                    case ElementDefine.Cell16_Slope_Trim:
                    case ElementDefine.TS_Slope_Trim:
                        if (param.phydata > 15) param.phydata = 15;
                        if (param.phydata < -15) param.phydata = -15;
                        break;
                    case ElementDefine.Isens:
                        if (param.phydata > 63) param.phydata = 63;
                        if (param.phydata < -63) param.phydata = -63;
                        break;
                    case ElementDefine.VBATT:
                    case ElementDefine.CADC:
                        if (param.phydata > 127) param.phydata = 127;
                        if (param.phydata < -127) param.phydata = -127;
                        break;
                }
                ConvertSlope(ref param);
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
                    case ElementDefine.CellVoltage01:
                        param = GetParameterByGuid(ElementDefine.Cell1_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage02:
                        param = GetParameterByGuid(ElementDefine.Cell2_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage03:
                        param = GetParameterByGuid(ElementDefine.Cell3_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage04:
                        param = GetParameterByGuid(ElementDefine.Cell4_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage05:
                        param = GetParameterByGuid(ElementDefine.Cell5_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage06:
                        param = GetParameterByGuid(ElementDefine.Cell6_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage07:
                        param = GetParameterByGuid(ElementDefine.Cell7_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage08:
                        param = GetParameterByGuid(ElementDefine.Cell8_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage09:
                        param = GetParameterByGuid(ElementDefine.Cell9_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage10:
                        param = GetParameterByGuid(ElementDefine.Cell1O_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage11:
                        param = GetParameterByGuid(ElementDefine.Cell11_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage12:
                        param = GetParameterByGuid(ElementDefine.Cell12_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage13:
                        param = GetParameterByGuid(ElementDefine.Cell13_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage14:
                        param = GetParameterByGuid(ElementDefine.Cell14_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage15:
                        param = GetParameterByGuid(ElementDefine.Cell15_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CellVoltage16:
                        param = GetParameterByGuid(ElementDefine.Cell16_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.Isens:
                        param = GetParameterByGuid(ElementDefine.Isens_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.VBATT:
                        param = GetParameterByGuid(ElementDefine.VBATT_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.TS:
                        param = GetParameterByGuid(ElementDefine.TS_Offset_Trim, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.CADC:
                        param = GetParameterByGuid(ElementDefine.CADC_Offset_Trim, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                if (offset > param.dbPhyMax) offset = param.dbPhyMax;
                if (offset < param.dbPhyMin) offset = param.dbPhyMin;
                param.phydata = offset;
                parent.Physical2Hex(ref param);
            }
            return ret;
        }

        private void ConvertSlope(ref Parameter param)
        {
            double slop = param.phydata;
            UInt16 wdata, tr = 0;
            Reg regLow = null, regHi = null;

            if (slop >= 0) return;
            foreach (KeyValuePair<string, Reg> dic in param.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }
            if (regHi != null)
                tr = (UInt16)(regHi.bitsnumber + regLow.bitsnumber - 1);
            else
                tr = (UInt16)(regLow.bitsnumber - 1);


            wdata = (UInt16)(1 << tr);
            wdata |= (UInt16)Math.Abs(slop);
            param.phydata = (double)wdata;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)wval;
            deviceinfor.shwversion = "A0";

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (type != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            byte bval = 0;
            UInt32 guid = ElementDefine.OperationElement;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            //Cell Number
            ret = ReadWord(0xBE, ref wval); //Cell number
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)((wval & 0x0070) >> 4);
            bval += 9;
            for (int i = 0; i < bval - 1; i++)
            {
                guid = (UInt32)((ElementDefine.CellVoltage01 >> 8) + i) << 8;
                HideParameterByGuid(guid, demparameterlist.parameterlist, true);
            }
            for (int i = bval - 1; i < ElementDefine.TotalCellNum - 1; i++)
            {
                guid = (UInt32)((ElementDefine.CellVoltage01 >> 8) + i) << 8;
                HideParameterByGuid(guid, demparameterlist.parameterlist);
            }
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        public UInt32 Verification(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 bwval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EPROMReglist = new List<byte>();
            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EPROMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                EPROMReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EPROMReglist = EPROMReglist.Distinct().ToList();
            //Write 
            if (EPROMReglist.Count != 0)
            {
                foreach (byte baddr in EPROMReglist)
                {
                    ret = ReadWord(baddr, ref bwval);
                    parent.m_EEPROMVerifyImg[baddr].err = ret;
                    parent.m_EEPROMVerifyImg[baddr].val = bwval;
                }
                ret = SearchExceptionEpParameter(ref msg);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }
        #endregion

        #region 其他
        public void HideParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist, bool bval = false)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid))
                {
                    param.bShow = bval;
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

        public UInt32 SetCADCMode(ElementDefine.CADC_MODE mode)       //MP version new method. Do 4 time average by HW, and we can also have the trigger flag and coulomb counter work at the same time.
        {
            ushort temp = 0;
            bool flag = false;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (mode)
            {
                case ElementDefine.CADC_MODE.DISABLE:
                    ret = WriteWord(ElementDefine.CADCRTL_REG, 0x00);           //clear all
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                #region moving mode
                case ElementDefine.CADC_MODE.MOVING:
                    ret = WriteWord(ElementDefine.INTR2_REG, 0x000C);        //Clear cadc_moving_flag
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(ElementDefine.CADCRTL_REG, 0x89);        //Set cc_always_enable, moving_average_enable, sw_cadc_ctrl=0b00
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    /*for (byte i = 0; i < ElementDefine.RETRY_COUNT * 20; i++)
                    {
                        Thread.Sleep(20);
                        ret = ReadWord(ElementDefine.INTR2_REG, ref temp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        if ((temp & 0x000C) == 0x000C)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)   //转换完成
                    {
                        ret = ReadWord(ElementDefine.FinalCadcMovingData_Reg, ref temp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    else
                        ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;

                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/
                    ret = ReadWord(0xBC, ref temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    temp |= 0x0080;
                    ret = WriteWord(0xBC, temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = ReadWord(ElementDefine.FinalCadcMovingData_Reg, ref temp);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    parent.m_OpRegImg[ElementDefine.FinalCadcMovingData_Reg].err = ret;
                    parent.m_OpRegImg[ElementDefine.FinalCadcMovingData_Reg].val = temp;
                    break;
                #endregion
                #region trigger mode
                case ElementDefine.CADC_MODE.TRIGGER:
                    ret = WriteWord(ElementDefine.INTR2_REG, 0x000C);        //Clear cadc_trigger_flag
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(ElementDefine.CADCRTL_REG, 0xB8);        //Set cadc_one_or_four, sw_cadc_ctrl=0b10
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    for (byte i = 0; i < ElementDefine.RETRY_COUNT * 20; i++)
                    {
                        Thread.Sleep(20);
                        ret = ReadWord(ElementDefine.INTR2_REG, ref temp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        if ((temp & 0x0004) == 0x0004)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)   //转换完成
                    {
                        ret = ReadWord(ElementDefine.FinalCadcTriggerData_Reg, ref temp);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    else
                        ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;

                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    parent.m_OpRegImg[ElementDefine.FinalCadcTriggerData_Reg].err = ret;
                    parent.m_OpRegImg[ElementDefine.FinalCadcTriggerData_Reg].val = temp;
                    break;
                    #endregion
            }
            return ret;
        }

        public UInt32 ClearTrimAndOffset(ParamContainer demparameterlist)
        {
            UInt16 wval = 0;
            Reg regLow = null, regHi = null;
            Parameter param = null;
            Tuple<Parameter, Parameter> slope_offset = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
            {
                param = (Parameter)demparameterlist.parameterlist[i];
                if (param == null) continue;
                if (!parent.m_guid_slope_offset.ContainsKey(param.guid)) continue;
                slope_offset = parent.m_guid_slope_offset[param.guid];

                if (!slope_offset.Item1.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item1.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;


                if (!slope_offset.Item1.reglist.ContainsKey("High")) continue;
                regHi = slope_offset.Item1.reglist["High"];
                ret = ReadWord((byte)regHi.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= (UInt16)(~(((1 << regHi.bitsnumber) - 1) << regHi.startbit));
                ret = WriteWord((byte)regHi.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (!slope_offset.Item2.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item2.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        public UInt32 WaitTriggerCompleted()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWaitTriggerCompleted();
            }
            return ret;
        }

        protected UInt32 OnWaitTriggerCompleted()
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < RETRY_COUNTER * 10; i++)
            {
                Thread.Sleep(10);
                ret = ReadWord(0x40, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & 0x00C0) == 0x0000)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }

        public UInt32 DisableAutoScan()
        {
            UInt16 pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0x59, ref pval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //Disable auto scan
            pval |= 0x0001;
            ret = WriteWord(0x59, pval);
            return ret;
        }

        private UInt32 EnableAutoScan()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = ReadWord(0xBE, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            wval &= 0xC7FF;
            wval |= (UInt16)(parent.auto_sw_osr & ~0xC7FF);
            ret = WriteWord(0xBE, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadWord(0x59, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //Keep auto scan
            wval &= 0xFFFE;
            //Update the channel
            wval |= 0x0004;
            ret = WriteWord(0x59, wval);
            return ret;
        }

        private UInt32 EnableTriggerScan(UInt16 val = 0, bool bSet = false)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0x40, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //Clear bit6 - 10
            wval &= 0xF83F;
            if (!bSet)
                wval |= (UInt16)(parent.trigger_sw_osr & ~0xF83F);
            else
                wval |= val;
            wval |= 0x0080;
            ret = WriteWord(0x40, wval);
            return ret;
        }

        private UInt32 SearchExceptionEpParameter(ref TASKMessage msg)
        {
            ushort rdata = 0, wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            Verify_Dic.Clear();
            foreach (Parameter param in demparameterlist.parameterlist)
            {
                if ((param.guid & ElementDefine.ElementMask) != ElementDefine.EPROMElement) continue;
                parent.m_dem_dm.ReadFromVerifyImg(param, ref rdata);
                parent.m_dem_dm.ReadFromRegImg(param, ref wdata);
                if (rdata != wdata)
                    Verify_Dic.Add(param.guid.ToString(), string.Format("Write is 0x{0:x4},Read back is 0x{1:x4}", wdata, rdata));
            }
            if (Verify_Dic.Count != 0)
            {
                msg.sub_task_json = SharedAPI.SerializeDictionaryToJsonString(Verify_Dic);
                ret = LibErrorCode.IDS_ERR_SECTION_DEVICECONFSFL_PARAM_VERIFY;
            }
            return ret;
        }
        #endregion

        #region Mass Production
        protected UInt32 PowerOn()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnPowerOn();
            }
            return ret;
        }

        protected UInt32 CheckProgramVoltage()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnCheckProgramVoltage();
            }
            return ret;
        }

        protected UInt32 PowerOff()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnPowerOff();
            }
            return ret;
        }

        private UInt32 OnPowerOn()
        {
            byte[] yDataIn = { 0x51 };
            byte[] yDataOut = { 0, 0 };
            ushort uOutLength = 2;
            ushort uWrite = 1;
            if (m_Interface.SendCommandtoAdapter(yDataIn, ref yDataOut, ref uOutLength, uWrite))
            {
                if (uOutLength == 2 && yDataOut[0] == 0x51 && yDataOut[1] == 0x1)
                {
                    Thread.Sleep(200);
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else
                    return ElementDefine.IDS_ERR_DEM_POWERON_FAILED;
            }
            return ElementDefine.IDS_ERR_DEM_POWERON_FAILED;
        }

        private UInt32 OnCheckProgramVoltage()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort wdata = 0;
            ret = ReadWord((byte)ElementDefine.VDD_OFFSET, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            double pv = wdata * 2.5;
            if (pv < 7100 || pv > 7600)     //Issue 1217
                ret = ElementDefine.IDS_ERR_DEM_POWERCHECK_FAILED;
            return ret;
        }

        private UInt32 OnPowerOff()
        {
            byte[] yDataIn = { 0x52 };
            byte[] yDataOut = { 0, 0 };
            ushort uOutLength = 2;
            ushort uWrite = 1;
            if (m_Interface.SendCommandtoAdapter(yDataIn, ref yDataOut, ref uOutLength, uWrite))
            {
                if (uOutLength == 2 && yDataOut[0] == 0x52 && yDataOut[1] == 0x2)
                {
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else
                    return ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED;
            }
            return ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED;
        }

        private uint ATEEmptyCheck()
        {
            ushort pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte index = (byte)ElementDefine.EP_ATE_OFFSET; index <= (byte)ElementDefine.EP_ATE_TOP; index++)
            {
                ret = OnReadWord(index, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if (pval == 0xFF)
                {
                    ret = ElementDefine.IDS_ERR_DEM_ATE_EMPTY_CHECK_FAILED;
                    break;
                }
            }
            return ret;
        }

        public uint CheckBinData(List<byte> blist)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            int length = (ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1);
            length *= 3;    //一个字节地址，两个字节数值
            if (blist.Count != length)
            {
                ret = LibErrorCode.IDS_ERR_DEM_BIN_LENGTH_ERROR;
            }
            else
            {
                for (int i = ElementDefine.EF_USR_OFFSET, j = 0; i <= ElementDefine.EF_USR_TOP; i++, j++)
                {
                    if (blist[j * 3] != i)
                    {
                        ret = LibErrorCode.IDS_ERR_DEM_BIN_ADDRESS_ERROR;
                        break;
                    }
                }
            }
            return ret;
        }

        private UInt32 FrozenBitCheck() //注意，这里没有把image里的Frozen bit置为1，记得在后面的流程中做这件事
        {
            ushort pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadWord((byte)ElementDefine.EF_USR_TOP, ref pval);
            if ((pval & 0x8000) == 0x8000)
                ret = LibErrorCode.IDS_ERR_DEM_FROZEN;
            return ret;
        }

        private UInt32 DirtyChipCheck()
        {
            ushort pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte index = (byte)ElementDefine.EF_USR_OFFSET; index <= (byte)ElementDefine.EF_USR_TOP; index++)
            {
                ret = OnReadWord(index, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if (pval == 0xFF)
                {
                    ret = LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                    break;
                }
            }
            return ret;
        }

        private void InitEfuseData()
        {
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                parent.m_EFRegImg[i].err = 0;
                parent.m_EFRegImg[i].val = 0xFF;
            }
        }

        private void PrepareHexData()
        {
            parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val |= 0x8000;    //Set Frozen bit in image

            byte[] usrbuf = new byte[ElementDefine.USR_CRC_BUF_LEN];
            GetUSRCRCRef(ref usrbuf);
            parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val &= 0xfff0;
            parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val |= CalEFUSECRC(usrbuf, ElementDefine.USR_CRC_BUF_LEN);
        }

        private UInt32 Download(List<byte> efusebindata, bool isWithPowerControl)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (isWithPowerControl)
            {
                ret = PowerOn();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = CheckProgramVoltage();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            Dictionary<string, ushort> EFRegImg = LoadEFRegImgFromEFUSEBin(efusebindata);
            WriteToEFUSEBuffer(EFRegImg);
            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                ret = OnWriteWord(badd, EFUSEUSRbuf[badd - (byte)ElementDefine.EF_USR_OFFSET]);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            if (isWithPowerControl)
            {
                ret = PowerOff();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = WorkMode(ElementDefine.COBRA_KALL14D_WKM.EFUSE_WORKMODE_NORMAL);
            return ret;
        }

        private Dictionary<string, ushort> LoadEFRegImgFromEFUSEBin(List<byte> efusebindata)
        {
            Dictionary<string, ushort> output = new Dictionary<string, ushort>();
            for (int i = 0; i < (ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1); i++)
            {
                output.Add(efusebindata[i * 3].ToString("X2"), SharedFormula.MAKEWORD(efusebindata[i * 3 + 2], efusebindata[i * 3 + 1]));
            }
            return output;
        }

        private void WriteToEFUSEBuffer(Dictionary<string, ushort> EFRegImg)
        {
            foreach (var key in EFRegImg.Keys)
            {
                byte badd = Convert.ToByte(key, 16);
                EFUSEUSRbuf[badd - (byte)ElementDefine.EF_USR_OFFSET] = EFRegImg[key];
            }
        }

        private UInt32 ReadBackCheck()
        {
            ushort pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                ret = ReadWord(badd, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if (pval != EFUSEUSRbuf[badd - ElementDefine.EF_USR_OFFSET])
                {
                    ret = LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                    break;
                }
            }
            return ret;
        }

        private UInt32 GetEfuseHexData(ref TASKMessage msg)
        {
            string tmp = "";
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_EFRegImg[i].err;
                tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_EFRegImg[i].val.ToString("X4") + "\r\n";
            }
            msg.sm.efusehexdata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 GetEfuseBinData(ref TASKMessage msg)
        {
            List<byte> tmp = new List<byte>();
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_EFRegImg[i].err;
                tmp.Add((byte)i);
                byte hi = 0, low = 0;
                hi = (byte)((parent.m_EFRegImg[i].val) >> 8);
                low = (byte)(parent.m_EFRegImg[i].val);
                tmp.Add(hi);
                tmp.Add(low);
            }
            msg.sm.efusebindata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private void GetUSRCRCRef(ref byte[] buf)
        {
            for (byte i = 0; i < 32; i++)
            {
                byte shiftdigit = (byte)((i % 4) * 4);
                shiftdigit = (byte)(12 - shiftdigit);
                int reg = (i / 4) + 7;
                buf[i] = (byte)((parent.m_EFRegImgEX[reg].val & (0x0f << shiftdigit)) >> shiftdigit);
            }
            buf[32] = (byte)((parent.m_EFRegImgEX[0x0f].val & (0x0f << 12)) >> 12);
            buf[33] = (byte)((parent.m_EFRegImgEX[0x0f].val & (0x0f << 8)) >> 8);
            buf[34] = (byte)((parent.m_EFRegImgEX[0x0f].val & (0x0f << 4)) >> 4);
        }

        private byte CalEFUSECRC(byte[] buf, UInt16 len)
        {
            return crc4_calc(buf, len);
        }

        private byte crc4_calc(byte[] pdata, int len)
        {

            byte crc = 0;
            byte crcdata;
            byte poly = 0x03;             // poly
            int n, j;                                      // the length of the data

            for (n = 0; n < len; n++)
            {
                crcdata = pdata[n];
                for (j = 0x8; j > 0; j >>= 1)
                {
                    if ((crc & 0x8) != 0)
                    {
                        crc <<= 1;
                        crc ^= poly;
                    }
                    else
                        crc <<= 1;
                    if ((crcdata & j) != 0)
                        crc ^= poly;
                }
                crc = (byte)(crc & 0xf);
            }
            return crc;
        }
        #endregion
    }
}