/*#define SIMULATION*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.FalconLY
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

        private BatteryMode batteryMode;
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
        protected UInt32 BlockRead(byte cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(cmd, ref pval);
            }
            return ret;
        }

        protected UInt32 BlockWrite(byte cmd, TSMBbuffer val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnBlockRead(byte cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                DateTime t1 = DateTime.Now;
                if (m_Interface.ReadDevice(sendbuf, ref t, ref DataOutLen, pval.length))
                {
                    pval.bdata = t;
                    if (pval.length > ElementDefine.CommonDataLen)
                    {
                        if ((pval.length - 1) != pval.bdata[0])
                        {
                            ret = LibErrorCode.IDS_ERR_I2C_CMD_DISMATCH; 
                            continue;
                        }
                        for (int j = 0; j < pval.length; j++)
                        {
                            pval.bdata[j] = pval.bdata[j + 1];
                        }
                    }
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnBlockWrite(byte cmd, TSMBbuffer val)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)(2 + val.length);
            byte[] sendbuf = new byte[DataInLen];
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
            sendbuf[1] = cmd;
            for (int i = 0; i < val.length; i++)
                sendbuf[2 + i] = val.bdata[i];

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            m_Interface.GetLastErrorCode(ref ret);
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
            byte bcmd = 0;
            Random rd = new Random();
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> SBSReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) continue;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
#if SIMULATION
                           rd.NextBytes(p.tsmbBuffer.bdata);
#else
                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            TSMBbuffer tsmBuffer = p.tsmbBuffer;
                            p.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
#endif
                            param = p;
                            parent.Hex2Physical(ref param);
                            break;
                        }
                }
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            byte bcmd = 0;
            TSMBbuffer tSmbBuffer = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> SBSReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;

                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            p.errorcode = ret = BlockWrite(bcmd, tSmbBuffer);
                            break;
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
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 guid = ElementDefine.SBSElement;
            TSMBbuffer tsmbBuf = new TSMBbuffer();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if SIMULATION
            tsmbBuf.bdata[0] = 0xFF;
            tsmbBuf.bdata[1] = 0xFF;
#else

            ret = BlockRead(ElementDefine.SBSBatteryMode, ref tsmbBuf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
#endif

            batteryMode.ILFC_Enable = (tsmbBuf.bdata[1] & 0x10) > 0 ? true:false;
            batteryMode.IEXTEND_Enable = (tsmbBuf.bdata[1] & 0x20) > 0 ? true : false;
            batteryMode.CEXTEND_Enable = (tsmbBuf.bdata[1] & 0x40) > 0 ? true : false;
            batteryMode.VEXTEND_Enable = (tsmbBuf.bdata[1] & 0x80) > 0 ? true : false;
            batteryMode.CellNum = (byte)(tsmbBuf.bdata[0] &0x3F);
            batteryMode.ExtTempNum = (byte)(tsmbBuf.bdata[1] &0x03);

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach(Parameter p in demparameterlist.parameterlist)
            {
                if (p == null) continue;
                p.bShow = true;
            }

            for (int i = batteryMode.CellNum; i < ElementDefine.TotalCellNum; i++)
            {
                guid = (UInt32)((ElementDefine.CellVoltage01 >> 8) + i) << 8;
                HideParameterByGuid(guid, demparameterlist.parameterlist);                                
            }

            for (int i = batteryMode.ExtTempNum; i < ElementDefine.TotalExtNum; i++)
            {
                guid = (UInt32)((ElementDefine.ExternalTemperature1 >> 8) + i) << 8;
                HideParameterByGuid(guid, demparameterlist.parameterlist);
            }

            if (!batteryMode.IEXTEND_Enable)
            {
                param = GetParameterByGuid(ElementDefine.PackCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.AverageCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.ChargingCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.FastChargingCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
            }
            else
            {
                param = GetParameterByGuid(ElementDefine.PackCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.AverageCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.ChargingCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.FastChargingCurrent, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
            }

            if (!batteryMode.CEXTEND_Enable)
            {
                param = GetParameterByGuid(ElementDefine.RemainingCapacity, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.FullChargeCapacity, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.DesignCapacity, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
            }
            else
            {
                param = GetParameterByGuid(ElementDefine.RemainingCapacity, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.FullChargeCapacity, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.DesignCapacity, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
            }

            if (!batteryMode.VEXTEND_Enable)
            {
                param = GetParameterByGuid(ElementDefine.PackVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.ChargingVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.DesignVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
                param = GetParameterByGuid(ElementDefine.FastChargingVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_FOUR_BYTES;
            }
            else
            {
                param = GetParameterByGuid(ElementDefine.PackVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.ChargingVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.DesignVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
                param = GetParameterByGuid(ElementDefine.FastChargingVoltage, demparameterlist.parameterlist);
                param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWO_BYTES;
            }


            param = GetParameterByGuid(ElementDefine.ManufacturerName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWENTY_BYTES;
            param = GetParameterByGuid(ElementDefine.DeviceName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWENTY_BYTES;
            param = GetParameterByGuid(ElementDefine.DeviceChemistry, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_TWENTY_BYTES;
            return ret;
        }
        #endregion

        public void HideParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist)
        {
            foreach (Parameter param in parameterlist.ToArray())
            {
                if (param.guid.Equals(guid))
                {
                    param.bShow = false;
                    break;
                }
            }
            return;
        }

        public Parameter GetParameterByGuid(UInt32 guid, AsyncObservableCollection<Parameter> parameterlist)
        {
            foreach (Parameter param in parameterlist)
            {
                if (param.guid.Equals(guid))
                    return param;
            }
            return null;
        }
    }
}