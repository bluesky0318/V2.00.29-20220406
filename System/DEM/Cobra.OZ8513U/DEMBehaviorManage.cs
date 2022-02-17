//#define  debug
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ8513U
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

        private byte[] bval4 = new byte[4];
        private byte[] sendbuf2 = new byte[2];
        private byte[] sendbuf3 = new byte[3];
        private byte[] receivebuf1 = new byte[1];
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
        protected UInt32 ReadBlock(byte datatype, byte command, ref UInt32 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WaitCompleted((byte)(datatype | 0x01), command);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.DATA_REG_LEN; i++)
                ret |= ReadByte((byte)(ElementDefine.DATA_START_REG + i), ref bval4[i]);

            pval = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bval4[0], bval4[1]), SharedFormula.MAKEWORD(bval4[2], bval4[3]));
            return ret;
        }

        protected UInt32 WriteBlock(byte datatype, byte command, UInt32 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = WriteByte((byte)ElementDefine.DATA_TYPE_REG, datatype);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.DATA_REG_LEN; i++)
            {
                ret = WriteByte((byte)(ElementDefine.DATA_START_REG + i), (byte)((pval >> (i * 8)) & 0xFF));
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = WriteByte((byte)ElementDefine.COMMAND_STATE_REG, (byte)(ElementDefine.COMMAND_STATE_MASK | command));
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //ret = WaitCompleted(datatype, command);
            return ret;
        }

        protected UInt32 WaitCompleted(byte datatype, byte command)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWaitCompleted(datatype, command);
            }
            return ret;
        }

        protected UInt32 ReadByte(byte reg, ref byte val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref val);
            }
            return ret;
        }

        protected UInt32 WriteByte(byte reg, byte val)
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf2[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf2[1] = reg;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf2, ref receivebuf1, ref DataOutLen))
                {
                    pval = receivebuf1[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            if (ret == LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT)
                FolderMap.WriteFile(string.Format("Read-- I2C:{0:x2},addr:{1:x2}", sendbuf2[0], sendbuf2[1]));
            
            return ret;
        }

        protected UInt32 OnWaitCompleted(byte datatype, byte command)
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnWriteByte((byte)ElementDefine.DATA_TYPE_REG, datatype);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteByte((byte)ElementDefine.COMMAND_STATE_REG, (byte)(ElementDefine.COMMAND_STATE_MASK | command));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.DATA_REG_LEN; i++)
            {
                ret = OnReadByte((byte)ElementDefine.COMMAND_STATE_REG, ref bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if (bval == command)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                else
                    ret = LibErrorCode.IDS_ERR_I2C_CMD_DISMATCH;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf3[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf3[1] = reg;
            sendbuf3[2] = val;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf3, ref receivebuf1, ref DataOutLen))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            if (ret == LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT)
                FolderMap.WriteFile(string.Format("Write-- I2C:{0:x2},addr:{1:x2},data:{2:x2}", sendbuf3[0], sendbuf3[1], sendbuf3[2]));
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
            byte address = 0x00;
            byte dataType = 0x00;
            OZ8513U_REG org = null;
            Reg reg = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            try
            {
                foreach (Parameter p in demparameterlist.parameterlist)
                {
                    if (p == null) break;
                    dataType = (byte)((p.guid & ElementDefine.CommandMask) >> 16);
                    foreach (KeyValuePair<string, Reg> dic in p.reglist)
                    {
                        reg = dic.Value;
                        if (!ElementDefine.m_rd_dataType.Contains(dataType)) continue;
                        org = parent.FindRegOnImgReg(dataType, reg);
                        if (org == null) continue;
#if debug
                    ret |= org.err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    org.val = (UInt32)new Random().Next(1000);
#else
                        address = (byte)org.reg.address;
                        ret |= org.err = ReadBlock(dataType, address, ref org.val);
#endif
                    }
                }
            }
            catch (System.Exception ex)
            {
                FolderMap.WriteFile(ex.Message);
                FolderMap.WriteFile(string.Format("Read-- type:{0:x2},addr:{1:x2},err:{2:x4},data:{3:x4}", dataType, address, org.err, org.val));
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            byte address = 0x00;
            byte dataType = 0x00;
            OZ8513U_REG org = null;
            Reg reg = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            try
            {
                foreach (Parameter p in demparameterlist.parameterlist)
                {
                    if (p == null) break;
                    dataType = (byte)((p.guid & ElementDefine.CommandMask) >> 16);
                    foreach (KeyValuePair<string, Reg> dic in p.reglist)
                    {
                        reg = dic.Value;
                        if (!ElementDefine.m_wr_dataType.Contains(dataType)) continue;
                        org = parent.FindRegOnImgReg(dataType, reg);
                        if (org == null) continue;

                        address = (byte)org.reg.address;
                        ret |= org.err = WriteBlock(dataType, address, org.val);
                    }
                }
            }
            catch (System.Exception ex)
            {
                FolderMap.WriteFile(ex.Message);
                FolderMap.WriteFile(string.Format("Write-- type:{0:x2},addr:{1:x2},err:{2:x4},data:{3:x4}", dataType, address, org.err, org.val));
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
            try
            {
                if (OpParamList.Count != 0)
                {
                    for (int i = 0; i < OpParamList.Count; i++)
                    {
                        param = (Parameter)OpParamList[i];
                        if (param == null) continue;

                        m_parent.Hex2Physical(ref param);
                    }
                }
            }
            catch (System.Exception ex)
            {
                FolderMap.WriteFile("HexPhysical" + ex.Message);
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
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.TestRead:
                    {
                        ret = ReadByte(0x10, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        msg.sm.misc[0] = bval;

                        ret = ReadByte(0x11, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        msg.sm.misc[1] = bval;

                        ret = ReadByte(0x12, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        msg.sm.misc[2] = bval;

                        ret = ReadByte(0x13, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        msg.sm.misc[3] = bval;

                        ret = ReadByte(0x14, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        msg.sm.misc[4] = bval;

                        ret = ReadByte(0x15, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                        msg.sm.misc[5] = bval;
                    }
                    break;
                case ElementDefine.COMMAND.TestWrite:
                    {
                        ret = WriteByte(0x10, (byte)msg.sm.misc[0]);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;

                        ret = WriteByte(0x11, (byte)msg.sm.misc[1]);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;

                        ret = WriteByte(0x12, (byte)msg.sm.misc[2]);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;

                        ret = WriteByte(0x13, (byte)msg.sm.misc[3]);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;

                        ret = WriteByte(0x14, (byte)msg.sm.misc[4]);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;

                        ret = WriteByte(0x15, (byte)msg.sm.misc[5]);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                    }
                    break;
            }
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion
    }
}