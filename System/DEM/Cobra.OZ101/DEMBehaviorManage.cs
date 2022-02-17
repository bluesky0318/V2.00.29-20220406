using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
//using Cobra.EM;

namespace Cobra.OZ101
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
        public UInt32 ReadByte(List<Parameter> OpParamList)
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
                    case ElementDefine.OpElement:
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

            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            foreach (byte badd in OpReglist)
            {
                lock (m_lock)
                {
                    ret = OnReadByte(badd, ref bdata);

                }
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = (UInt16)bdata;
            }
            return ret;
        }

        public UInt32 WriteByte(List<Parameter> OpParamList)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OpReglist = new List<byte>();

            foreach (Parameter p in OpParamList)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.OpElement:
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

            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            foreach (byte badd in OpReglist)
            {

                lock (m_lock)
                {
                    ret |= OnWriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                }
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ReadOneByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        public UInt32 WriteOneByte(byte reg, byte val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnReadByte(byte reg, ref byte pval)
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
                    break;
                }
                Thread.Sleep(10);
            }
            m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
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
                    break;
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
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                            if (p == null) break;
                            OpReglist.Add(p);
            }
            ret1 = ReadByte(OpReglist);
            return ret1;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                            if (p == null) break;
                            OpReglist.Add(p);
            }
            ret1 = WriteByte(OpReglist);
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

            List<Parameter> OpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                            if (p == null) break;
                            OpReglist.Add(p);
            }

            /*if (MonitorOpReglist.Count != 0)
            {
                for (int i = 0; i < MonitorOpReglist.Count; i++)
                {
                    param = MonitorOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_monitor_device.Hex2Physical(ref param);
                }
            }*/

            if (OpReglist.Count != 0)
            {
                for (int i = 0; i < OpReglist.Count; i++)
                {
                    param = OpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                    parent.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                            if (p == null) break;
                            OpReglist.Add(p);
            }
            /*if (MonitorOpReglist.Count != 0)
            {
                for (int i = 0; i < MonitorOpReglist.Count; i++)
                {
                    param = MonitorOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_monitor_device.Physical2Hex(ref param);
                }
            }*/

            if (OpReglist.Count != 0)
            {
                for (int i = 0; i < OpReglist.Count; i++)
                {
                    param = OpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    parent.Physical2Hex(ref param);
                }
            }
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            /*switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.DGB: //DBG
                    m_dem_dm.WriteOneByte(0xe0, 0x64);
                    m_dem_dm.WriteOneByte(0xe0, 0x62);
                    m_dem_dm.WriteOneByte(0xe0, 0x67);
                    break;
                case ElementDefine.COMMAND.BURN: //BURN
                    m_dem_dm.WriteOneByte(0xf9, 0x01);
                    m_dem_dm.WriteOneByte(0xf9, 0x01);
                    break;
                case ElementDefine.COMMAND.FREEZE: //FREEZE
                    m_dem_dm.WriteOneByte(0xf9, 0x02);
                    m_dem_dm.WriteOneByte(0xf9, 0x02);
                    break;
            }*/
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        protected UInt32 WorkMode(ElementDefine.COBRA_BLUEWHALE_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            string shwversion = String.Empty;
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadOneByte(0x00, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

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

            ret = m_dem_dm.ReadOneByte(0x40, ref bdata);
            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL)||((bdata & 0x20) == 0))
            {
                msg.sm.parts[0] = true;
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            else msg.sm.parts[0] = false;

            ret = m_dem_dm.ReadOneByte(0x10,ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                msg.sm.parts[0] = true;
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            else msg.sm.parts[0] = false;

            if (bdata < 0x06)
            {
                ret = m_dem_dm.WriteOneByte(0x10, 0x0A);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    msg.sm.parts[0] = true;
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else msg.sm.parts[0] = false;
            }*/

            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

    }
}