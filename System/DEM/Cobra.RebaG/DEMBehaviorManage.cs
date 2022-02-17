using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.RebaG
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


        bool firsttime = true;
        #region monitor
        private bool m_PecEnable;
        public bool pecenable
        {
            get { return m_PecEnable; }
            set { m_PecEnable = value; }
        }
        #endregion
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
            Options opPEC = parent.m_busoption.GetOptionsByGuid(ElementDefine.BoDevicePEC);
            if (opPEC == null) return false;

            pecenable = (opPEC.data > 0) ? true : false;
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
        public UInt32 ReadByte(List<Parameter> OpParamList, ElementDefine.SECTION ec)
        {
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();

            foreach (Parameter p in OpParamList)
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
                                OpReglist.Add(baddress);
                            }
                            break;
                        }
                }
            }

            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            foreach (byte badd in OpReglist)
            {
                lock (m_lock)
                {
                    if(ec == ElementDefine.SECTION.CHARGER)
                        ret = OnChgReadByte(badd, ref bdata);
                    else if (ec == ElementDefine.SECTION.MONITOR)
                        ret = OnMonReadByte(badd, ref bdata);

                }
                if (ec == ElementDefine.SECTION.CHARGER)
                {
                    parent.m_ChgOpRegImg[badd].err = ret;
                    parent.m_ChgOpRegImg[badd].val = (UInt16)bdata;
                }
                else if (ec == ElementDefine.SECTION.MONITOR)
                {
                    parent.m_MonOpRegImg[badd].err = ret;
                    parent.m_MonOpRegImg[badd].val = (UInt16)bdata;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            return ret;
        }

        public UInt32 WriteByte(List<Parameter> OpParamList, ElementDefine.SECTION ec)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OpReglist = new List<byte>();

            foreach (Parameter p in OpParamList)
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
                lock (m_lock)
                {
                    if (ec == ElementDefine.SECTION.CHARGER)
                        ret = OnChgWriteByte(badd, (byte)parent.m_ChgOpRegImg[badd].val);
                    else if (ec == ElementDefine.SECTION.MONITOR)
                        ret = OnMonWriteByte(badd, (byte)parent.m_MonOpRegImg[badd].val);

                }
                if (ec == ElementDefine.SECTION.CHARGER)
                {
                    parent.m_ChgOpRegImg[badd].err = ret;
                }
                else if (ec == ElementDefine.SECTION.MONITOR)
                {
                    parent.m_MonOpRegImg[badd].err = ret;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }

            return ret;
        }

        public UInt32 ChgReadOneByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnChgReadByte(reg, ref pval);
            }
            return ret;
        }

        public UInt32 ChgWriteOneByte(byte reg, byte val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnChgWriteByte(reg, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnChgReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2C2Address_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen))
                {
                    pval = receivebuf[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                //m_Interface.ResetInterface();
                Thread.Sleep(10);
                //m_Interface.ResetInterface();
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        protected UInt32 OnMonReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[1];
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
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen))
                {
                    pval = receivebuf[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                //m_Interface.ResetInterface();
                Thread.Sleep(10);
                //m_Interface.ResetInterface();
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnMonWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[1];
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
            sendbuf[2] = val;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                //m_Interface.ResetInterface();
                Thread.Sleep(10);
                //m_Interface.ResetInterface();
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnChgWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2C2Address_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = val;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                //m_Interface.ResetInterface();
                Thread.Sleep(10);
                //m_Interface.ResetInterface();
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWorkMode(ElementDefine.COBRA_BLUEWHALE_WKM wkm)
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnMonReadByte(ElementDefine.YFLASH_WORKMODE_REG, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bdata = (byte)(bdata & 0xFC);
            bdata |= 0x48;
            switch (wkm)
            {
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_NORMAL:
                    {
                        ret = OnMonWriteByte(ElementDefine.YFLASH_WORKMODE_REG, (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_NORMAL);
                        break;
                    }
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ANALOG_TESTMODE:
                    {
                        bdata |= (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ANALOG_TESTMODE;
                        ret = OnMonWriteByte(ElementDefine.YFLASH_WORKMODE_REG, bdata);
                        break;
                    }
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_DIGITAL_TESTMODE:
                    {
                        bdata |= (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_DIGITAL_TESTMODE;
                        ret = OnMonWriteByte(ElementDefine.YFLASH_WORKMODE_REG, bdata);
                        break;
                    }
                case ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ADC_FFTMODE:
                    {
                        bdata |= (byte)ElementDefine.COBRA_BLUEWHALE_WKM.YFLASH_WORKMODE_ADC_FFTMODE;
                        ret = OnMonWriteByte(ElementDefine.YFLASH_WORKMODE_REG, bdata);
                        break;
                    }
                default:
                    break;
            }
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

        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MonitorOpReglist = new List<Parameter>();
            List<Parameter> ChargerOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 2:
                        {
                            if (p == null) break;
                            MonitorOpReglist.Add(p);
                            break;
                        }
                    case 3:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
                }
            }

            //Read
            if (MonitorOpReglist.Count != 0)
                ret = ReadByte(MonitorOpReglist, ElementDefine.SECTION.MONITOR);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (ChargerOpReglist.Count != 0)
                ret1 = ReadByte(ChargerOpReglist, ElementDefine.SECTION.CHARGER);
            return ret1;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MonitorOpReglist = new List<Parameter>();
            List<Parameter> ChargerOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 2:
                        {
                            if (p == null) break;
                            MonitorOpReglist.Add(p);
                            break;
                        }
                    case 3:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
                }
            }
            //Write
            if (MonitorOpReglist.Count != 0)
                ret = WriteByte(MonitorOpReglist, ElementDefine.SECTION.MONITOR);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (ChargerOpReglist.Count != 0)
                ret1 = WriteByte(ChargerOpReglist, ElementDefine.SECTION.CHARGER);
            return ret1;
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

            List<Parameter> MonitorOpReglist = new List<Parameter>();
            List<Parameter> ChargerOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 2:
                        {
                            if (p == null) break;
                            MonitorOpReglist.Add(p);
                            break;
                        }
                    case 3:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
                }
            }

            if (MonitorOpReglist.Count != 0)
            {
                for (int i = 0; i < MonitorOpReglist.Count; i++)
                {
                    param = MonitorOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    parent.Hex2Physical(ref param);
                }
            }

            if (ChargerOpReglist.Count != 0)
            {
                for (int i = 0; i < ChargerOpReglist.Count; i++)
                {
                    param = ChargerOpReglist[i];
                    if (param == null) continue;

                    parent.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MonitorOpReglist = new List<Parameter>();
            List<Parameter> ChargerOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 2:
                        {
                            if (p == null) break;
                            MonitorOpReglist.Add(p);
                            break;
                        }
                    case 3:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
                }
            }
            if (MonitorOpReglist.Count != 0)
            {
                for (int i = 0; i < MonitorOpReglist.Count; i++)
                {
                    param = MonitorOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    parent.Physical2Hex(ref param);
                }
            }

            if (ChargerOpReglist.Count != 0)
            {
                for (int i = 0; i < ChargerOpReglist.Count; i++)
                {
                    param = ChargerOpReglist[i];
                    if (param == null) continue;

                    parent.Physical2Hex(ref param);
                }
            }
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.DGB: //DBG
                    ChgWriteOneByte(0xe0, 0x64);
                    ChgWriteOneByte(0xe0, 0x62);
                    ChgWriteOneByte(0xe0, 0x67);
                    break;
                case ElementDefine.COMMAND.BURN: //BURN
                    ChgWriteOneByte(0xf9, 0x01);
                    ChgWriteOneByte(0xf9, 0x01);
                    break;
                case ElementDefine.COMMAND.FREEZE: //FREEZE
                    ChgWriteOneByte(0xf9, 0x02);
                    ChgWriteOneByte(0xf9, 0x02);
                    break;
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 WorkMode(ElementDefine.COBRA_BLUEWHALE_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte b = 0;
            ret = ChgReadOneByte(0x00, ref b);
            if(ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                deviceinfor.status = 0;
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            /*byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            msg.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;
            
            // Write k=1.2 to TSET
            if (firsttime)
            {
                ReadOneByte(0x05, ref bdata);
                bdata &= 0xfc;
                bdata |= 0x02;
                WriteOneByte(0x05, bdata);
                firsttime = false;
            }
            // Write k=1.2 to TSET
            ret = ReadOneByte(0x40, ref bdata);
            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL)||((bdata & 0x20) == 0))
            {
                msg.sm.parts[0] = true;
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            else msg.sm.parts[0] = false;

            ret = ReadOneByte(0x10,ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                msg.sm.parts[0] = true;
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            else msg.sm.parts[0] = false;

            if (bdata < 0x06)
            {
                ret = WriteOneByte(0x10, 0x0A);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    msg.sm.parts[0] = true;
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else msg.sm.parts[0] = false;
            }
            */


            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region 外部温度转换
        public double ResistToTemp(double resist)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }
            return SharedFormula.ResistToTemp(resist, m_TempVals, m_ResistVals);
        }

        public double TempToResist(double temp)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }

            return SharedFormula.TempToResist(temp, m_TempVals, m_ResistVals);
        }
        #endregion
    }
}