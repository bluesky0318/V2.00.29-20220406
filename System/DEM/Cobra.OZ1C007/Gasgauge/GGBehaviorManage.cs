using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.IO;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ1C007.Gasgauge
{
    internal class GGBehaviorManage
    {
        //父对象保存
        private GGDeviceManage m_parent;
        public GGDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private byte[] m_temporary_memory = new byte[32 * 1024];

        private object m_lock = new object();
        public void Init(object pParent)
        {
            parent = (GGDeviceManage)pParent;
        }

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        #region SW I2C Parameter
        protected UInt32 BlockRead(byte cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(cmd, ref pval);
            }
            return ret;
        }

        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }
        #endregion

        protected UInt32 ReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteByte(byte reg, byte val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }

        protected UInt32 InfoBlockRead()
        {
            UInt16 DataOutLen = 4;
            byte[] receivebuf = new byte[ElementDefine.INFO_BLOCK_USED + 2];
            byte[] sendbuf = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                try
                {
                    sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                }
                catch (System.Exception ex)
                {
                    return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                }
                sendbuf[1] = 0x75;
                sendbuf[2] = 0x80;
                sendbuf[3] = 0x00;
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(ElementDefine.INFO_BLOCK_USED + 2), 3))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
                for (int i = 0; i < ElementDefine.INFO_BLOCK_USED; i++)
                {
                    parent.m_InfoRegImg[i].val = receivebuf[i + 2];
                    parent.m_InfoRegImg[i].err = ret;
                }
                ret = OnBlockWriteDone();
            }
            return ret;
        }

        protected UInt32 InfoBlockWrite()
        {
            byte checkSum = 0;
            UInt16 DataOutLen = 0;
            byte[] receivebuf = new byte[1];
            byte[] sendbuf = new byte[5 + ElementDefine.INFO_BLOCK_USED];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            ret = WriteByte(0x77, 0x73);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            lock (m_lock)
            {
                try
                {
                    sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                }
                catch (System.Exception ex)
                {
                    return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                }
                sendbuf[1] = 0x70;
                sendbuf[2] = 0x02;
                sendbuf[3] = 0x80;
                sendbuf[4] = 0x00;
                //Array.Copy(parent.m_InfoRegImg, 0, sendbuf, 5, ElementDefine.INFO_BLOCK_USED);
                for (int i = 0; i < (ElementDefine.INFO_BLOCK_USED - 1); i++)
                {
                    sendbuf[5 + i] = (byte)parent.m_InfoRegImg[i].val;
                    checkSum += (byte)parent.m_InfoRegImg[i].val;
                }
                sendbuf[5 + ElementDefine.INFO_BLOCK_USED - 1] = checkSum;

                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.INFO_BLOCK_USED + 3))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = WriteByte(0x77, 0x00);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        protected UInt32 BlockWriteDone()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnBlockWriteDone();
            }
            return ret;
        }

        protected UInt32 MainBlockRead(int len, ref byte[] buffer)
        {
            UInt16 DataOutLen = 4;
            byte[] receivebuf = new byte[ElementDefine.BLOCK_OPERATION_BYTES + 2];
            byte[] sendbuf = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < len; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0x73;
                    sendbuf[2] = SharedFormula.HiByte(fAddr);
                    sendbuf[3] = SharedFormula.LoByte(fAddr); ;
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.BLOCK_OPERATION_BYTES + 2, 3))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        Thread.Sleep(10);
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Array.Copy(receivebuf, 2, buffer, fAddr, ElementDefine.BLOCK_OPERATION_BYTES);
                }
            }
            return ret;
        }

        protected UInt32 MainBlockRead(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 4;
            byte[] receivebuf = new byte[ElementDefine.BLOCK_OPERATION_BYTES + 2];
            byte[] sendbuf = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0x73;
                    sendbuf[2] = SharedFormula.HiByte(fAddr);
                    sendbuf[3] = SharedFormula.LoByte(fAddr); ;
                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.BLOCK_OPERATION_BYTES + 2, 3))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        Thread.Sleep(10);
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    Array.Copy(receivebuf, 2, msg.flashData, fAddr, ElementDefine.BLOCK_OPERATION_BYTES);
                }
            }
            return ret;
        }

        protected UInt32 MainBlockWrite(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            byte[] receivebuf = new byte[1];
            byte[] sendbuf = new byte[5 + ElementDefine.BLOCK_OPERATION_BYTES];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.BLOCK_OPERATION_BYTES) //必须从上传下来的是1024的整数倍，多余的填写0xFF
                {
                    try
                    {
                        sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                    }
                    catch (System.Exception ex)
                    {
                        return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                    }
                    sendbuf[1] = 0x70;
                    sendbuf[2] = 0x01;
                    sendbuf[3] = SharedFormula.HiByte(fAddr);
                    sendbuf[4] = SharedFormula.LoByte(fAddr);
                    Array.Copy(msg.flashData, fAddr, sendbuf, 5, ElementDefine.BLOCK_OPERATION_BYTES);

                    for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                    {
                        if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.BLOCK_OPERATION_BYTES + 3))
                        {
                            ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            break;
                        }
                        ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                        Thread.Sleep(10);
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            return ret;
        }

        protected UInt32 MTPCheckSumTest(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
                ret = OnMTPCheckSumTest(ref msg);
            }
            return ret;
        }

        protected UInt32 WaitCheckSumFinish()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWaitCheckSumFinish();
            }
            return ret;
        }

        protected UInt32 MTPBlockWrite()
        {
            byte checkSum = 0;
            UInt16 DataOutLen = 0;
            byte[] mtpbuf = new byte[ElementDefine.INFO_BLOCK_USED];
            byte[] receivebuf = new byte[1];
            byte[] sendbuf = new byte[5 + ElementDefine.INFO_BLOCK_USED];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            lock (m_lock)
            {
                try
                {
                    sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                }
                catch (System.Exception ex)
                {
                    return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                }
                sendbuf[1] = 0x70;
                sendbuf[2] = 0x01;
                sendbuf[3] = 0x7F;
                sendbuf[4] = 0xF8;
                //Array.Copy(parent.m_InfoRegImg, 0, sendbuf, 5, ElementDefine.INFO_BLOCK_USED);
                for (int i = 0; i < (ElementDefine.INFO_BLOCK_USED - 1); i++)
                {
                    sendbuf[5 + i] = (byte)parent.m_InfoRegImg[i].val;
                    checkSum += (byte)parent.m_InfoRegImg[i].val;
                }
                sendbuf[5 + ElementDefine.INFO_BLOCK_USED - 1] = checkSum;

                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.INFO_BLOCK_USED + 3))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = MTPBlockRead(ElementDefine.INFO_BLOCK_USED, ref mtpbuf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            for (int i = 0; i < (ElementDefine.INFO_BLOCK_USED - 1); i++)
            {
                if (mtpbuf[i] != sendbuf[i + 5]) return OZ1C007.ElementDefine.IDS_ERR_MTP_WRITE_FAILED;
            }
            return ret;
        }

        protected UInt32 MTPBlockRead(int len, ref byte[] buffer)
        {
            UInt16 DataOutLen = 4;
            byte[] receivebuf = new byte[ElementDefine.INFO_BLOCK_USED + 2];
            byte[] sendbuf = new byte[4];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                try
                {
                    sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
                }
                catch (System.Exception ex)
                {
                    return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
                }
                sendbuf[1] = 0x73;
                sendbuf[2] = 0x7F;
                sendbuf[3] = 0xF8;
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, ElementDefine.INFO_BLOCK_USED + 2, 3))
                    {
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    Thread.Sleep(10);
                    ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                Array.Copy(receivebuf, 2, buffer, 0, ElementDefine.INFO_BLOCK_USED);
            }
            return ret;
        }

        #endregion

        #region 操作寄存器子级操作
        #region SW I2C Parameter
        protected UInt32 OnBlockRead(byte cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2C2Address_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.ReadDevice(sendbuf, ref t, ref DataOutLen, pval.length))
                {
                    pval.bdata = t;
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2C2Address_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    pval = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
            }

            return ret;
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[4];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2C2Address_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = SharedFormula.HiByte(val);
            sendbuf[3] = SharedFormula.LoByte(val);

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
            }
            return ret;
        }
        #endregion

        protected UInt32 OnReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    pval = receivebuf[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
            }

            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[4];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = val;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnBlockWriteDone()
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0x70;
            sendbuf[2] = 0x5A;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnMTPCheckSumTest(ref TASKMessage msg)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[8];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = 0x70;//8’h80:IIC Master do the mtp checksum test 		I2C_CMD Register
            sendbuf[2] = 0x80;
            sendbuf[3] = 0x00;
            sendbuf[4] = 0x00;
            sendbuf[5] = SharedFormula.HiByte((UInt16)(msg.flashData.Length - 1));
            sendbuf[6] = SharedFormula.LoByte((UInt16)(msg.flashData.Length - 1));
            sendbuf[7] = SWCheckSum(ref msg);

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 6))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWaitCheckSumFinish()
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnReadByte(ElementDefine.REG_I2C_MTP_STATUS, ref bdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((bdata & ElementDefine.I2C_MTP_STATUS_MASK) == 0x00) break;
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnReadByte(ElementDefine.REG_I2C_MTP_STATUS, ref bdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((bdata & ElementDefine.MTP_CHECKSUM_FINISH_MASK) == ElementDefine.MTP_CHECKSUM_FINISH_MASK)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
            }
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
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
            #region Info Mapping
            ret = InfoMapping();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            #endregion
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> MTPINFOReglist = new List<byte>();
            List<byte> SWI2CReglist = new List<byte>();
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
                                if (baddress < ElementDefine.SW_I2C_PARAM_OFFSET)
                                    MTPINFOReglist.Add(baddress);
                                else
                                    SWI2CReglist.Add(baddress);
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
                }
            }
            OpReglist = OpReglist.Distinct().ToList();
            MTPINFOReglist = MTPINFOReglist.Distinct().ToList();
            SWI2CReglist = SWI2CReglist.Distinct().ToList();

            if (MTPINFOReglist.Count != 0)
            {
                ret = InfoBlockRead();
            }

            if (SWI2CReglist.Count != 0)
            {
                foreach (byte badd in SWI2CReglist)
                {
                    ret = ReadWord((byte)(badd - ElementDefine.SW_I2C_PARAM_OFFSET), ref wdata);
                    parent.m_InfoRegImg[badd].err = ret;
                    parent.m_InfoRegImg[badd].val = wdata;
                }
            }

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

            List<byte> MTPINFOReglist = new List<byte>();
            List<byte> SWI2CReglist = new List<byte>();
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
                                if (baddress < ElementDefine.SW_I2C_PARAM_OFFSET)
                                    MTPINFOReglist.Add(baddress);
                                else
                                    SWI2CReglist.Add(baddress);
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
                }
            }
            OpReglist = OpReglist.Distinct().ToList();
            MTPINFOReglist = MTPINFOReglist.Distinct().ToList();
            SWI2CReglist = SWI2CReglist.Distinct().ToList();

            if (MTPINFOReglist.Count != 0)
            {
                ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = InfoBlockWrite();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = BlockWriteDone();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = MTPBlockWrite();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = BlockWriteDone();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                return ret;
            }

            if (SWI2CReglist.Count != 0)
            {
                foreach (byte badd in SWI2CReglist)
                {
                    ret = WriteWord((byte)(badd - ElementDefine.SW_I2C_PARAM_OFFSET), parent.m_InfoRegImg[badd].val);
                    parent.m_InfoRegImg[badd].err = ret;
                }
            }
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
            UInt16 DataOutLen = 0;
            StringBuilder strb = new StringBuilder();

            byte bAddress = msg.flashData[0];
            UInt16 DataInWrite = (UInt16)msg.flashData[1];
            UInt16 DataInLen = (UInt16)msg.flashData[msg.flashData.Length - 1];

            byte[] receivebuf = new byte[ElementDefine.MAX_COM_SIZE];
            byte[] sendbuf = new byte[ElementDefine.MAX_COM_SIZE];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {
                sendbuf[0] = bAddress;
                switch (bAddress & 0x01)
                {
                    case 0: //read
                        {
                            Array.Copy(msg.flashData, 2, sendbuf, 1, DataInWrite);
                            if (parent.parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen, DataInWrite))
                            {
                                msg.flashData = new byte[DataOutLen];
                                Array.Copy(receivebuf, 0, msg.flashData, 0, DataOutLen);
                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                break;
                            }
                        }
                        break;
                    case 1://write
                        {
                            DataInLen = (UInt16)(msg.flashData.Length - 2);
                            msg.flashData[0] = (byte)(sendbuf[0] & 0xFE);
                            if (parent.parent.m_Interface.WriteDevice(msg.flashData, ref receivebuf, ref DataOutLen, DataInLen))
                            {
                                strb.Clear();
                                for (int n = 0; n < msg.flashData.Length; n++)
                                    strb.Append(string.Format("{0:x2}-", msg.flashData[n]));
                                FolderMap.WriteFile(strb.ToString());
                                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                                break;
                            }
                        }
                        break;
                }
            }
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
                }
            }

            if (EFUSEParamList.Count != 0)
            {
                for (int i = 0; i < EFUSEParamList.Count; i++)
                {
                    param = (Parameter)EFUSEParamList[i];
                    if (param == null) continue;

                    m_parent.Hex2Physical(ref param);
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
                }
            }

            if (EFUSEParamList.Count != 0)
            {
                for (int i = 0; i < EFUSEParamList.Count; i++)
                {
                    param = (Parameter)EFUSEParamList[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
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
            byte checksum = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region MainBlock
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        checksum = SWCheckSum(ref msg);
                        msg.controlmsg.message = string.Format("The bin file checksum is: 0x{0:x2}", checksum);
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;

                        ret = Download(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
                        ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = MainBlockRead(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = BlockWriteDone();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_COMPARE:
                    {
                        int len = 0;
                        len = msg.flashData.Length;
                        Array.Clear(ElementDefine.interBuffer, 0, ElementDefine.interBuffer.Length);
                        Array.Copy(msg.flashData, ElementDefine.interBuffer, len);

                        ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        msg.controlmsg.message = "Begin upload data..";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        ret = MainBlockRead(len, ref ElementDefine.interBuffer);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        msg.controlmsg.message = "Save upload data..";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        var dumpBuf = ElementDefine.interBuffer;
                        SaveFile(Path.Combine(FolderMap.m_logs_folder, string.Format("{0}{1}{2}", "Dump", DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-"), ".log")), len, ref dumpBuf);

                        msg.controlmsg.message = "Begin verify data..";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        for (int n = 0; n < len; n++)
                        {
                            if (msg.flashData[n] == ElementDefine.interBuffer[n]) continue;

                            msg.controlmsg.message = string.Format("SW verify data failed on 0x{0:x16}, Write is 0x{1:x2},Read is 0x{2:x2}!!",
                                (UInt16)n, msg.flashData[n], ElementDefine.interBuffer[n]);
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                            ret = LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                            Thread.Sleep(100);
                        }
                        if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            msg.controlmsg.message = "SW verify data successfully!!";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        }
                        else
                        {
                            msg.controlmsg.message = "SW verify data failed!!";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        }
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE:
                    {
                        ret = SwitchMode((ElementDefine.COBRA_SUB_TASK)msg.sub_task);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE:
                    {
                        ret = SwitchMode((ElementDefine.COBRA_SUB_TASK)msg.sub_task);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_INFO_MAP:
                    {
                        ret = InfoMapping();
                        break;
                    }
                #endregion
                default:
                    break;
            }
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            string shwversion = String.Empty;
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadByte(0x00, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)bval;
            shwversion += "A0";
            deviceinfor.shwversion = shwversion;
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            byte bcmd = 0;
            UInt16 wval = 0;
            Parameter param = null;
            TSMBbuffer tsmBuffer = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            param = GetParameterByGuid(ElementDefine.FWEdition, demparameterlist.parameterlist);
            if (param != null)
            {
                param.tsmbBuffer.length = 4;
                tsmBuffer = param.tsmbBuffer;
                bcmd = (byte)(((param.guid & ElementDefine.CommandMask) >> 8) - ElementDefine.SW_I2C_PARAM_OFFSET);
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }

            param = GetParameterByGuid(ElementDefine.AleInfoCRC, demparameterlist.parameterlist);
            if (param != null)
            {
                param.tsmbBuffer.length = 8;
                tsmBuffer = param.tsmbBuffer;
                bcmd = (byte)(((param.guid & ElementDefine.CommandMask) >> 8) - ElementDefine.SW_I2C_PARAM_OFFSET);
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }

            param = GetParameterByGuid(ElementDefine.BatteryTableVersion, demparameterlist.parameterlist);
            if (param != null)
            {
                param.tsmbBuffer.length = 32;
                tsmBuffer = param.tsmbBuffer;
                bcmd = (byte)(((param.guid & ElementDefine.CommandMask) >> 8) - ElementDefine.SW_I2C_PARAM_OFFSET);
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }

            param = GetParameterByGuid(ElementDefine.BoardOffsetHW, demparameterlist.parameterlist);
            if (param != null)
            {
                bcmd = (byte)(((param.guid & ElementDefine.CommandMask) >> 8) - ElementDefine.SW_I2C_PARAM_OFFSET);
                param.errorcode = ret = ReadWord(bcmd, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.m_InfoRegImg[bcmd + ElementDefine.SW_I2C_PARAM_OFFSET].val = wval;
                parent.m_InfoRegImg[bcmd + ElementDefine.SW_I2C_PARAM_OFFSET].err = ret;
                parent.Hex2Physical(ref param);
            }

            param = GetParameterByGuid(ElementDefine.ChipID, demparameterlist.parameterlist);
            if (param != null)
            {
                bcmd = (byte)(((param.guid & ElementDefine.CommandMask) >> 8) - ElementDefine.SW_I2C_PARAM_OFFSET);
                param.errorcode = ret = ReadWord(bcmd, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.m_InfoRegImg[bcmd + ElementDefine.SW_I2C_PARAM_OFFSET].val = (UInt16)(wval >> 8);
                parent.m_InfoRegImg[bcmd + ElementDefine.SW_I2C_PARAM_OFFSET].err = ret;
                parent.Hex2Physical(ref param);
            }
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion

        #region 功能函数
        public UInt32 SwitchMode(ElementDefine.COBRA_SUB_TASK subTask)
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (subTask)
            {
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE:
                    ret = ReadByte(0x80, ref bdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    bdata &= 0xFE;
                    ret = WriteByte(0x80, bdata);
                    break;
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE:
                    ret = ReadByte(0x80, ref bdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    bdata |= 0x01;
                    ret = WriteByte(0x80, bdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = WriteByte(0x58, 0x00);
                    break;
            }
            return ret;
        }

        private UInt32 SetTestMode(byte data)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WriteByte(0x82, 0x040);
            return ret;
        }

        internal void SaveFile(string fullpath, int len, ref byte[] bdata)
        {
            FileInfo file = new FileInfo(@fullpath);

            using (FileStream fs = file.OpenWrite())
                fs.Write(bdata, 0, len);
        }

        internal byte SWCheckSum(ref TASKMessage task)
        {
            byte checksum = 0;
            for (int i = 0; i < task.flashData.Length; i++)
                checksum += task.flashData[i];
            return checksum;
        }

        private UInt32 InfoMapping()
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = InfoBlockRead();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < 6; i++)
            {
                ret = WriteByte((byte)(0x90 + i), (byte)parent.m_InfoRegImg[i].val);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = ReadByte(0x96, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bdata = (byte)(bdata & 0x0F);
            bdata |= (byte)((byte)(parent.m_InfoRegImg[0x06].val) & 0xF0);
            ret = WriteByte(0x96, bdata);
            return ret;
        }
        #endregion

        #region 其他
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
        #endregion

        #region Download
        public UInt32 Download(ref TASKMessage msg)
        {
            byte[] mtpbuf = new byte[ElementDefine.INFO_BLOCK_USED];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            UInt32 ret1 = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if((msg.flashData.Length > 30*1024)|(msg.flashData.Length <= 32*1024))
            {
                Array.Clear(m_temporary_memory, 0, m_temporary_memory.Length);
                Array.Copy(msg.flashData, m_temporary_memory, msg.flashData.Length);
                ret = InfoBlockRead();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                for (int i = 0; i < ElementDefine.INFO_BLOCK_USED; i++)
                {
                    if (parent.m_InfoRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    mtpbuf[i] = (byte)parent.m_InfoRegImg[i].val;
                }
                Array.Copy(mtpbuf, 0, m_temporary_memory, m_temporary_memory.Length - ElementDefine.INFO_BLOCK_USED, ElementDefine.INFO_BLOCK_USED);
                msg.flashData = m_temporary_memory;
            }

            ret = MainBlockWrite(ref msg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret1 = DumpFileAndVerify(ref msg);
            if (ret1 == LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                msg.controlmsg.message = "Successful to dump and verify..";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            }

            ret = BlockWriteDone();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret1;
        }

        public UInt32 DumpFileAndVerify(ref TASKMessage msg)
        {
            int len = msg.flashData.Length;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Array.Clear(ElementDefine.interBuffer, 0, ElementDefine.interBuffer.Length);
            Array.Copy(msg.flashData, ElementDefine.interBuffer, len);

            ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_DEBUG_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Begin upload data..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            ret = MainBlockRead(len, ref ElementDefine.interBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = SwitchMode(ElementDefine.COBRA_SUB_TASK.SUB_TASK_NORMAL_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Save upload data..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            var dumpBuf = ElementDefine.interBuffer;
            SaveFile(Path.Combine(FolderMap.m_logs_folder, string.Format("{0}{1}{2}", "Dump", DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-"), ".log")), len, ref dumpBuf);

            msg.controlmsg.message = "Begin verify data..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            Thread.Sleep(5);
            for (int n = 0; n < len; n++)
            {
                if (msg.flashData[n] == ElementDefine.interBuffer[n]) continue;

                msg.controlmsg.message = string.Format("SW verify data failed on 0x{0:x16}, Write is 0x{1:x2},Read is 0x{2:x2}!!",
                    (UInt16)n, msg.flashData[n], ElementDefine.interBuffer[n]);
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                ret = LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                Thread.Sleep(5);
                break;
            }
            return ret;
        }
        #endregion
    }
}