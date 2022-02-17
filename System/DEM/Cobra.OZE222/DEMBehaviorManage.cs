using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZE222
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

        public UInt32 WriteBits(byte reg, byte val, byte startbit, byte length)
        {
            UInt32 ret = 0;
            byte data = 0, lomask = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref data);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                lomask = (byte)((1 << length) - 1);
                lomask <<= startbit;
                data &= (byte)(~lomask);
                data |= (byte)(val << startbit);
                ret = OnWriteByte(reg, data);
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
                m_Interface.ResetInterface();
                Thread.Sleep(10);
                m_Interface.ResetInterface();
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
                m_Interface.ResetInterface();
                Thread.Sleep(10);
                m_Interface.ResetInterface();
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
            ret1 = ReadByte(ChargerOpReglist);
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
            ret1 = WriteByte(ChargerOpReglist);
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

            List<Parameter> ChargerOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 3:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
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
        public UInt32 WritePassword()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WriteOneByte(0xe0, 0x64);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteOneByte(0xe0, 0x62);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteOneByte(0xe0, 0x67);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            return ret; 
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.WritePassWord: //DBG
                    ret = WritePassword();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.BURN: //BURN
                    ret= WriteOneByte(0xf9, 0x01);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteOneByte(0xf9, 0x01);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.FREEZE: //FREEZE
                    ret = WriteOneByte(0xf9, 0x02);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteOneByte(0xf9, 0x02);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.SwitchToECModeCV42CC5:
                    ret = WritePassword();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0xe3, 1, 4, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x00, 0x08, 0, 5);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x02, 0x07, 0, 4);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Thread.Sleep(200);
                    ret = WriteBits(0xe3, 1, 7, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0xe3, 0, 0, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.SwitchToSWModeCV42CC2:
                    ret = WritePassword();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0xe3, 0, 7, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x00, 0x08, 0, 5);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x02, 0x0a, 0, 4);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Thread.Sleep(200);
                    ret = WriteBits(0xe3, 0, 4, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.SwitchToECModeCV44CC5:
                    ret = WritePassword();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0xe3, 1, 4, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x00, 0x18, 0, 5);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x02, 0x07, 0, 4);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Thread.Sleep(200);
                    ret = WriteBits(0xe3, 1, 7, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0xe3, 0, 0, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.SwitchToSWModeCV44CC2:
                    ret = WritePassword();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0xe3, 0, 7, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x00, 0x10, 0, 5);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteBits(0x02, 0x0a, 0, 4);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Thread.Sleep(200);
                    ret = WriteBits(0xe3, 0, 4, 1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
            }
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte b = 0;
            ret = ReadOneByte(0x00, ref b);
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
            byte bdata = 0;
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


            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion
    }
}