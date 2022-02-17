using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
//using Cobra.EM;

namespace Cobra.OZ9313
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
        protected UInt32 ReadByte(byte slave_addr, byte reg, ref byte pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadByte(slave_addr, reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteByte(byte slave_addr, byte reg, byte val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteByte(slave_addr, reg, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnReadByte(byte slave_addr, byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            sendbuf[0] = slave_addr;
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    pval = receivebuf[0];
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
            }

            return ret;
        }

        protected UInt32 OnWriteByte(byte slave_addr, byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[4];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            sendbuf[0] = slave_addr;
            sendbuf[1] = reg;
            sendbuf[2] = val;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
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
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
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
                }
            }
            OpReglist = OpReglist.Distinct().ToList();
            foreach (byte badd in OpReglist)
            {
                ret = ReadByte(0x30, badd, ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = (UInt16)bdata;
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
                    case ElementDefine.EFUSEElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
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
            foreach (byte badd in OpReglist)
            {
                ret = WriteByte(0x30, badd, (byte)parent.m_OpRegImg[badd].val);
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
            UInt16 addr = 0; ;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region MainBlock
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        ret = OnResetChip();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        Thread.Sleep(3500);

                        ret = OnClearWatchDog();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        //erase program
                        for (int i = 0; i < 54; i++)
                        {
                            addr = (UInt16)(1024 * i);
                            if (i < 53)
                                ret = OnProgErasePage(addr, false);
                            else
                                ret = OnProgErasePage(addr, true);

                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }

                        ret = OnClearWatchDog();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnParamPageErase(addr);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnClearWatchDog();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnClearWatchDog();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        //download program and const data
                        var flashdata = msg.flashData;
                        ret = OnDownloadProgram(ref flashdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnClearWatchDog();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnEnablePort30();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnResetChip();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
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

            ret = ReadByte(0x16,0x00, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)bval;
            shwversion += "A0";
            deviceinfor.shwversion = shwversion;
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

        #region OZ9313子功能模块
        public UInt32 OnEnablePort30()
        {
            int i = 0;
            int delay;
            Random ro = new Random(10);
            long tick = DateTime.Now.Ticks;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            do
            {
                ret = OnWriteByte(0x16, 0xFF, 0xA5);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    i++;
                    Random ran = new Random((int)(tick & 0xffffffffL) | (int)(tick >> 32));
                    delay = ran.Next() % 1000;
                    Thread.Sleep(delay);
                }
                else
                {
                    ret = OnWriteByte(0x30, 0x81, 0x1c);
                    if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                }
            } while (i < 2);
            return ret;
        }

        public UInt32 OnDisablePort30()
        {
            return OnWriteByte(0x16, 0xFF, 0x5A);
        }

        public UInt32 OnResetChip()
        {
            return OnWriteByte(0x30, 0x81, 0xC7);
        }

        public UInt32 OnClearWatchDog()
        {
            return OnResetChip();
        }

        public UInt32 OnParamPageErase(UInt16 address)
        {
            byte val = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //set memory mode
            ret = OnWriteByte(0x30, ElementDefine.SYSCR, (byte)ElementDefine.COBRA_SYS_MODE.SYS_MEMORY_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //send address
            ret = OnWriteByte(0x30, ElementDefine.MARH, SharedFormula.HiByte(address));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnWriteByte(0x30, ElementDefine.MARL, SharedFormula.LoByte(address));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //set download mode
            ret = OnWriteByte(0x30, ElementDefine.MOMCR, (byte)ElementDefine.COBRA_MEM_ACCESS.MEM_DOWNLOAD_MODE | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_PAGE_ERASE_PARAM_FLASH);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //wait operation complete
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnReadByte(0x30, ElementDefine.MOMSR,ref val);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((val & (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_PAGE_ERASE) == (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_PAGE_ERASE)
                    break;

                Thread.Sleep(10);
            }

            //clear flag
            ret = OnWriteByte(0x30, ElementDefine.MOMSR, (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_PAGE_ERASE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            return ret;
        }

        public UInt32 OnProgErasePage(UInt16 address, bool bNormal)
        {

            byte val = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //set memory access none
            ret = OnWriteByte(0x30, ElementDefine.MOMCR, ((byte)ElementDefine.COBRA_MEM_ACCESS.MEM_NO_ACCESS | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_NO_ACCESS));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            //set memory mode
            ret = OnWriteByte(0x30, ElementDefine.SYSCR, (byte)ElementDefine.COBRA_SYS_MODE.SYS_MEMORY_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            //send address
            ret = OnWriteByte(0x30, ElementDefine.MARH, SharedFormula.HiByte(address));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = OnWriteByte(0x30, ElementDefine.MARL, SharedFormula.LoByte(address));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            //set download mode
            ret = OnWriteByte(0x30, ElementDefine.MOMCR, ((byte)ElementDefine.COBRA_MEM_ACCESS.MEM_DOWNLOAD_MODE | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_PAGE_ERASE_PROGRAM_FLASH));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            //wait operation complete
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnWriteByte(0x30, ElementDefine.MOMSR, val);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;

                if ((val & (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_PAGE_ERASE) == (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_PAGE_ERASE)
                    break;

                Thread.Sleep(100);
            }

            //clear flag
            ret = OnWriteByte(0x30, ElementDefine.MOMSR, (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_PAGE_ERASE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (bNormal)
            {
                //set memory access none   
                ret = OnWriteByte(0x30, ElementDefine.MOMCR, ((byte)ElementDefine.COBRA_MEM_ACCESS.MEM_NO_ACCESS | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_NO_ACCESS));
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;

                //set normal mode
                ret = OnWriteByte(0x30, ElementDefine.SYSCR, (byte)ElementDefine.COBRA_SYS_MODE.SYS_NORMAL_MODE);
            }

            return ret;
        }

        public UInt32 OnDownloadProgram(ref byte[] pflash)
        {
            byte flag = 0;
            UInt16 i, j;
            TSMBbuffer buffer;
            UInt16 addr;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;


            //set memory access none
            ret = OnWriteByte(0x30, ElementDefine.MOMCR, (byte)((byte)ElementDefine.COBRA_MEM_ACCESS.MEM_NO_ACCESS | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_NO_ACCESS));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //set memory mode
            ret = OnWriteByte(0x30, ElementDefine.SYSCR, (byte)ElementDefine.COBRA_SYS_MODE.SYS_MEMORY_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //set download program eflash
            ret = OnWriteByte(0x30, ElementDefine.MOMCR, (byte)((byte)ElementDefine.COBRA_MEM_ACCESS.MEM_DOWNLOAD_MODE | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_WRITE_PROGRAM_FLASH));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //download
            for (i = 0; i < ElementDefine.PROG_FLASH_BLOCKS; i++)
            {
                //send address
                addr = (UInt16)(i * 32);
                ret = OnWriteByte(0x30, ElementDefine.MARH, SharedFormula.HiByte(addr));
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                ret = OnWriteByte(0x30, ElementDefine.MARL, SharedFormula.LoByte(addr));
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;

                //send block
                /*
                memcpy(buffer.data, (BYTE*)(pflash+addr), 32);
                buffer.length = 32;
                m_Adapter->SendBlock(0x30,0xFF,buffer.data, buffer.length);
                if(ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/
            }

            //set download complete
            ret = OnWriteByte(0x30, ElementDefine.MOMSR, (byte)ElementDefine.COBRA_FLAG_FLASH.DOWNLOAD_FLASH_FINISHED);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //wait complete
            for (j = 0; j < ElementDefine.RETRY_COUNTER; j++)
            {
                ret = OnReadByte(0x30, ElementDefine.MOMSR, ref flag);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((flag & (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_DOWNLOAD_FLASH) == (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_DOWNLOAD_FLASH)
                    break;

                Thread.Sleep(100);
            }

            //clear flag
            ret = OnWriteByte(0x30, ElementDefine.MOMSR, (byte)ElementDefine.COBRA_FLAG_FLASH.FLAG_DOWNLOAD_FLASH);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //set memory access none
            ret = OnWriteByte(0x30, ElementDefine.MOMCR, (byte)((byte)ElementDefine.COBRA_MEM_ACCESS.MEM_NO_ACCESS | (byte)ElementDefine.COBRA_SUB_MEM_ACCESS.SUB_NO_ACCESS));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            //set normal mode
            ret = OnWriteByte(0x30, ElementDefine.SYSCR, (byte)ElementDefine.COBRA_SYS_MODE.SYS_NORMAL_MODE);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (j >= ElementDefine.RETRY_COUNTER) return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            return ret;
        }
        #endregion
    }
}