using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.Azalea20
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
        private bool bwatchDog = false;
        private System.Timers.Timer watchDogTimer = null;
        private CCommunicateManager m_Interface = new CCommunicateManager();
        private Dictionary<UInt32, double> dic = new Dictionary<UInt32, double>();
        private List<DataPoint> m_dataPoint_List = new List<DataPoint>();

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT,"Read CADC timeout!"},
            {ElementDefine.IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT,"Wait trigger scan flag timeout!"},
            {ElementDefine.IDS_ERR_DEM_ACTIVE_MODE_ERROR,"Not in Active mode, please check."},
            {ElementDefine.IDS_ERR_DEM_CFET_ON_FAILED,"Cannot turn on CFET. Please check 1. if there is any OV or COC event 2. EFETC mode setting 3. EFETC pin status."},
            {ElementDefine.IDS_ERR_DEM_CFET_OFF_FAILED,"Cannot turn off CFET. Please check if it is in discharging state."},
            {ElementDefine.IDS_ERR_DEM_DFET_ON_FAILED,"Cannot turn on DFET. Please check 1. if there is any DOC or SC event 2. EFETC mode setting 3. EFETC pin status."},
            {ElementDefine.IDS_ERR_DEM_DFET_OFF_FAILED,"Cannot turn off DFET. Please check if it is in charging state."},
        };
        #endregion
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();

            watchDogTimer = new System.Timers.Timer();
            watchDogTimer.AutoReset = true;
            watchDogTimer.Enabled = true;
            watchDogTimer.Elapsed += watchDogTimer_Elapsed;
        }

        #region EFUSE操作常量定义
        private const int RETRY_COUNTER = 5;
        private const byte EFUSE_DATA_OFFSET = 0x80;
        private const byte EFUSE_MAP_OFFSET = 0xA0;

        // EFUSE operation code
        private const byte EFUSE_WORKMODE_NORMAL = 0x00;
        private const byte EFUSE_WORKMODE_WRITE_MAP_CTRL = 0x01;
        private const byte EFUSE_WORKMODE_PROGRAMMING = 0x02;

        // EFUSE control registers' addresses
        private const byte EFUSE_WORKMODE_REG = 0x18;
        private const byte EFUSE_TESTCTR_REG = 0x19;
        private const byte EFUSE_MAP_REG = 0x0F;
        private const byte EFUSE_ATE_FROZEN_REG = 0x0F;

        // EFUSE Control Flags
        private const UInt16 EFUSE_ATE_FROZEN_FLAG = 0x8000;
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
        public UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }
        public UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }
        protected UInt32 WorkMode(ElementDefine.COBRA_AZALEA20_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
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
        protected UInt32 OnWorkMode(ElementDefine.COBRA_AZALEA20_WKM wkm)
        {
            byte blow = 0;
            byte bhigh = 0;
            UInt16 wdata = 0;
            UInt16 wdata1 = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (wkm == ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_NORMAL)
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

        #region 基础服务功能设计
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
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        public UInt32 EpBlockRead()
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
                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in EFUSEReglist)
                {
                    ret = ReadWord((byte)(badd + EFUSE_DATA_OFFSET), ref wdata);
                    parent.m_EFRegImg[badd].err = ret;
                    parent.m_EFRegImg[badd].val = wdata;
                }

                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = ReadWord((byte)(EFUSE_ATE_FROZEN_REG + EFUSE_DATA_OFFSET), ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wdata & EFUSE_ATE_FROZEN_FLAG) == EFUSE_ATE_FROZEN_FLAG)
                {
                    msg.gm.message = "ATE zone had been forzen,can not be written!";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                    return ret;
                }

                foreach (byte badd in EFUSEReglist)
                {
                    ret = WriteWord((byte)(badd + EFUSE_DATA_OFFSET), parent.m_EFRegImg[badd].val);
                    parent.m_EFRegImg[badd].err = ret;
                }

                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            if (OpEfuseReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in OpEfuseReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            if (OpMapReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                foreach (byte badd in OpMapReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_NORMAL);
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
                        ret = EnableCADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.TRIGGER_SCAN_UI:
                    {
                        var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                        switch (json["VADCMode"])
                        {
                            case "TrigOne":
                                ShowAllParameter(demparameterlist.parameterlist, false);
                                ShowTriggerOneParameter(demparameterlist.parameterlist, true);
                                break;
                            case "TrigEight":
                                ShowAllParameter(demparameterlist.parameterlist, false);
                                ShowTriggerEightParameter(demparameterlist.parameterlist, true);
                                break;
                        }
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
                        ret = EnableCADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion

                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

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
                        #region 准备寄存器初始化
                        ret = EnableCADC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        #region Trim 8 tims
                        for (int n = 0; n < 8; n++)
                        {
                            ret = Read(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (int i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = (Parameter)demparameterlist.parameterlist[i];
                                if (param == null) continue;
                                if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

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
                case ElementDefine.COBRA_COMMAND_MODE.SCS_TRIGGER_SCAN_EIGHT_MODE:
                    {
                        param = demparameterlist.parameterlist[0];
                        if (param == null) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

                        switch (param.guid)
                        {
                            case ElementDefine.OP_CADC:
                                ret = EnableCADC();
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case ElementDefine.COBRA_COMMAND_MODE.EXCEPTION_MODE_WATCH_DOG:
                    bwatchDog = !bwatchDog;
                    ret = ClearWatchDog();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
            deviceinfor.type = wval;
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
                case 2:
                    shwversion = "C";
                    break;
            }
            ival = (int)(SharedFormula.LoByte(wval) & 0x03);
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)(SharedFormula.LoByte(wval) & 0x03);

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (type != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if (((SharedFormula.LoByte(wval) & 0x30) >> 4) != deviceinfor.hwversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if ((SharedFormula.LoByte(wval) & 0x03) != deviceinfor.hwsubversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt16 wval = 0;
            byte bval = 0;
            Parameter param = null;
            UInt32 guid = ElementDefine.OperationElement;
            UInt32 guid1 = ElementDefine.OperationElement;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            ret = ReadWord(0xBE, ref wval); //Cell number
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bval = (byte)(((wval & 0x00F0) >> 4) + 5);
            if (bval < 10) bval = 10;
            param = demparameterlist.GetParameterByGuid(ElementDefine.OP_CELL1V);
            if (!param.bShow)
                guid1 = (UInt32)(ElementDefine.OP_CELL1V_8 >> 8);
            else
                guid1 = (UInt32)(ElementDefine.OP_CELL1V >> 8);
            for (int i = 0; i < bval - 1; i++)
            {
                guid = (UInt32)(guid1 + i) << 8;
                ShowParameterByGuid(guid, demparameterlist.parameterlist, true);
            }
            for (int i = bval - 1; i < ElementDefine.TotalCellNum - 1; i++)
            {
                guid = (UInt32)(guid1 + i) << 8;
                ShowParameterByGuid(guid, demparameterlist.parameterlist, false);
            }
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
            {
                if (p == null) continue;
                p.bShow = bshow;
            }
        }

        private void ShowTriggerOneParameter(AsyncObservableCollection<Parameter> paramList, bool bshow = true)
        {
            UInt32 guid = ElementDefine.OperationElement;
            for (int i = 0; i < 32; i++)
            {
                guid = (UInt32)((ElementDefine.OP_INTMP >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList, bshow);
            }
            ShowParameterByGuid(ElementDefine.OP_CADC, paramList, bshow);
        }

        private void ShowTriggerEightParameter(AsyncObservableCollection<Parameter> paramList, bool bshow = true)
        {
            UInt32 guid = ElementDefine.OperationElement;
            for (int i = 0; i < 32; i++)
            {
                guid = (UInt32)((ElementDefine.OP_INTMP_8 >> 8) + i) << 8;
                ShowParameterByGuid(guid, paramList, bshow);
            }
            ShowParameterByGuid(ElementDefine.OP_CADC, paramList, bshow);
        }
        #endregion
        public UInt32 ClearTrimAndOffset(ParamContainer demparameterlist)
        {
            UInt16 wval = 0;
            Reg regLow = null;
            Parameter param = null;
            Tuple<Parameter, Parameter> slope_offset = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_WRITE_MAP_CTRL);
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

                if (!slope_offset.Item2.reglist.ContainsKey("Low")) continue;
                regLow = slope_offset.Item2.reglist["Low"];
                ret = ReadWord((byte)regLow.address, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                wval &= (UInt16)(~(((1 << regLow.bitsnumber) - 1) << regLow.startbit));
                ret = WriteWord((byte)regLow.address, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = WorkMode(ElementDefine.COBRA_AZALEA20_WKM.EFUSE_WORKMODE_NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        public UInt32 EnableCADC()
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            //Clear CADC flag
            //ret = ReadWord(0x01, ref wval);
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            //wval |= 0x01;
            ret = WriteWord(0x01, 0x01);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadWord(0x38, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            wval |= 0xA1;
            ret = WriteWord(0x38, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < 200; i++)
            {
                ret = ReadWord(0x01, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if ((wval & 0x0001) == 0x0001)
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(100);
            }
            return ret;
        }

        public void ShowParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist, bool bshow = true)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param == null) continue;
                if (param.guid.Equals(guid))
                {
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

        public UInt32 ClearWatchDog()
        {
            UInt16 wdt_timer = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (!bwatchDog) //Close
                watchDogTimer.Stop();
            else //Open
            {
                ret = ReadWord(0x05, ref wdt_timer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                switch (wdt_timer)
                {
                    case 0:
                        watchDogTimer.Interval = 100;//250;
                        break;
                    case 1:
                        watchDogTimer.Interval = 300;//500;
                        break;
                    case 2:
                        watchDogTimer.Interval = 700;//1000;
                        break;
                    case 3:
                        watchDogTimer.Interval = 1500;//2000;
                        break;
                    case 4:
                        watchDogTimer.Interval = 3000;//4000;
                        break;
                    case 5:
                        watchDogTimer.Interval = 6500;//8000;
                        break;
                    case 6:
                        watchDogTimer.Interval = 14000;//16000;
                        break;
                    case 7:
                        watchDogTimer.Interval = 30000;//32000;
                        break;
                }
                watchDogTimer.Start();
            }
            return ret;
        }

        public void watchDogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UInt16 wdt_timer = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0x05, ref wdt_timer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                bwatchDog = false;
                watchDogTimer.Stop();
            }
        }

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
                    case ElementDefine.OP_CELL1V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL1V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL2V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL2V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL3V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL3V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL4V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL4V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL5V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL5V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL6V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL6V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL7V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL7V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL8V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL8V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL9V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL9V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL10V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL10V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL11V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL11V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL12V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL12V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL13V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL13V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL14V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL14V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL15V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL15V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL16V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL16V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL17V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL17V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL18V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL18V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL19V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL19V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL20V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL20V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_ISENS_8:
                        param = GetParameterByGuid(ElementDefine.OP_ISENS_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_VBATT_8:
                        param = GetParameterByGuid(ElementDefine.OP_VBATT_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM0V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM0V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM1V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM1V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM2V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM2V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM3V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM3V_SLOP, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CADC:
                        param = GetParameterByGuid(ElementDefine.OP_CADC_SLOP, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                if (param.guid == ElementDefine.OP_CADC_SLOP)
                    param.phydata = Math.Round((1 - slope) * 4096, 0);
                else if (param.guid == ElementDefine.OP_VBATT_SLOP)
                    param.phydata = Math.Round((1 - slope) * 1024, 0);
                else
                    param.phydata = Math.Round((1 - slope) * 2048, 0);
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
                    case ElementDefine.OP_CELL1V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL1V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL2V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL2V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL3V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL3V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL4V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL4V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL5V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL5V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL6V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL6V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL7V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL7V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL8V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL8V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL9V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL9V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL10V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL10V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL11V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL11V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL12V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL12V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL13V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL13V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL14V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL14V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL15V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL15V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL16V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL16V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL17V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL17V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL18V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL18V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL19V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL19V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CELL20V_8:
                        param = GetParameterByGuid(ElementDefine.OP_CELL20V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_ISENS_8:
                        param = GetParameterByGuid(ElementDefine.OP_ISENS_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_VBATT_8:
                        param = GetParameterByGuid(ElementDefine.OP_VBATT_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM0V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM0V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM1V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM1V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM2V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM2V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_THM3V_8:
                        param = GetParameterByGuid(ElementDefine.OP_THM3V_OFFSET, demparameterlist.parameterlist);
                        break;
                    case ElementDefine.OP_CADC:
                        param = GetParameterByGuid(ElementDefine.OP_CADC_OFFSET, demparameterlist.parameterlist);
                        break;
                }
                if (param == null) continue;
                param.phydata = offset;
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
    }
}