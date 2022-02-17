using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ1C007.Charger
{
    internal class ChargerBehaviorManage
    {
        //父对象保存
        private ChargerDeviceManage m_parent;
        public ChargerDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        public object m_lock = new object();

        public void Init(object pParent)
        {
            parent = (ChargerDeviceManage)pParent;
        }

        #region 操作寄存器操作
        #region 操作寄存器父级操作
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
                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }

            return ret;
        }

        public UInt32 ReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        public UInt32 WriteByte(byte reg, byte val)
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

            sendbuf[0] = 0x20;
            sendbuf[1] = reg;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen))
                {
                    pval = receivebuf[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                parent.parent.m_Interface.ResetInterface();
                Thread.Sleep(10);
                parent.parent.m_Interface.ResetInterface();
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = 0x20;
            sendbuf[1] = reg;
            sendbuf[1] = reg;
            sendbuf[2] = val;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                parent.parent.m_Interface.ResetInterface();
                Thread.Sleep(10);
                parent.parent.m_Interface.ResetInterface();
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            }
            //m_Interface.GetLastErrorCode(ref ret);
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
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
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
                                OpReglist.Add(baddress);
                            }
                            break;
                        }
                }
            }
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (OpReglist.Count != 0)
            {
                foreach (byte badd in OpReglist)
                {
                    ret = ReadByte(badd, ref bdata);
                    parent.m_OpRegImg[badd].err = ret;
                    parent.m_OpRegImg[badd].val = (UInt16)bdata;
                }
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
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
                                OpReglist.Add(baddress);
                            }
                            break;
                        }
                }
            }
            OpReglist = OpReglist.Distinct().ToList();
            if (OpReglist.Count != 0)
            {
                foreach (byte badd in OpReglist)
                {
                    ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
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
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                }
            }
            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            OpParamList.Add(p);
                            break;
                        }
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            byte bdata = 0;
            byte startAddr = 0;
            byte bSize = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.DGB: //DBG
                    WriteByte(0xe0, 0x64);
                    WriteByte(0xe0, 0x62);
                    WriteByte(0xe0, 0x67);
                    break;
                case ElementDefine.COMMAND.BURN: //BURN
                    WriteByte(0xff, 0x01);
                    WriteByte(0xff, 0x01);
                    break;
                case ElementDefine.COMMAND.FREEZE: //FREEZE
                    WriteByte(0xff, 0x02);
                    WriteByte(0xff, 0x02);
                    break;
                #region DEBUG SFL
                case ElementDefine.COMMAND.DBG_READ_REGULAR:
                    {
                        var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                        startAddr = byte.Parse(options["StartAddr"].Trim(), System.Globalization.NumberStyles.HexNumber);
                        bSize = byte.Parse(options["Size"].Trim());
                        for (byte i = 0; i < bSize; i++)
                        {
                            ret = ReadByte((byte)(startAddr + i), ref bdata);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                msg.flashData[i] = 0xFF;
                            else
                                msg.flashData[i] = bdata;
                        }
                        break;

                    }
                #endregion
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            string shwversion = String.Empty;
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadByte(0x14, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;

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
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion
    }
}