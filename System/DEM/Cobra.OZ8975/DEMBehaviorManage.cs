using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ8975
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

        private bool m_bRead = false;
        private object m_lock = new object();
        private CCommunicateManager m_Interface = new CCommunicateManager();
        UInt16[] EFUSEUSRbuf = new UInt16[ElementDefine.EF_USR_BANK1_TOP - ElementDefine.EF_USR_BANK1_OFFSET + 1 + 3]; //bank1的长度，加上0x15,0x16,0x17,0x15从EFUSEUSRbuf[4]里面去

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
        public UInt32 ReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref pval);
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

        protected UInt32 PowerOn()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnPowerOn();
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

        protected UInt32 SetWorkMode(ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnSetWorkMode(wkm);
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

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = data;

            return crc8_calc(ref pdata, 4);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data;

            return crc8_calc(ref pdata, 3);
        }

        protected UInt32 OnReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
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

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    if (receivebuf[1] != calc_crc_read(sendbuf[0], sendbuf[1], receivebuf[0]))
                    {
                        pval = ElementDefine.PARAM_HEX_ERROR;
                        ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    else
                    {
                        pval = receivebuf[0];
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    }
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[4];
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

            sendbuf[3] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2]);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnSetWorkMode(ElementDefine.WORK_MODE wkm)
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = OnReadByte(ElementDefine.WORKMODE_REG, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((bdata & 0x3) == (byte)wkm) return ret;

            ret = OnWriteByte(ElementDefine.WORKMODE_REG, (byte)(bdata | ElementDefine.ALLOW_WR_FLAG));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = OnReadByte(ElementDefine.WORKMODE_REG, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bdata &= 0xFC;
            bdata |= (byte)wkm;
            bdata |= (byte)0x20;
            if (wkm == ElementDefine.WORK_MODE.NORMAL)
                bdata = 0x00;

            ret = OnWriteByte(ElementDefine.WORKMODE_REG, (byte)bdata);
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

        #endregion
        #endregion

        #region 基础服务功能设计
        private bool isContainEfuseRegisters(List<byte> OpReglist)
        {
            foreach (byte badd in OpReglist)
            {
                if (badd <= 0x1f && badd >= 0x10)
                    return true;
            }
            return false;
        }

        private bool isContainEfuseMapRegisters(List<byte> OpReglist)
        {
            foreach (byte badd in OpReglist)
            {
                if (badd <= 0x2f && badd >= 0x20)
                    return true;
            }
            return false;
        }

        private bool isOPBank1Empty()
        {
            byte tmp = 0;
            ReadByte(0x2b, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isOPBank2Empty()
        {
            byte tmp = 0;
            ReadByte(0x2f, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isOPConfigEmpty()
        {
            byte tmp = 0;
            ReadByte(0x27, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isEFBank2Empty()
        {
            byte tmp = 0;
            ReadByte(0x1f, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isEFBank1Empty()
        {
            byte tmp = 0;
            ReadByte(0x1b, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isEFConfigEmpty()
        {
            byte tmp = 0;
            ReadByte(0x17, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            byte offset = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();
            List<byte> OffsetRegList = new List<byte>(); //会去偏移的一些寄存器
            List<byte> OtherRegList = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)  //略过虚拟参数
                    continue;
                if (p == null) break;
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    reg = dic.Value;
                    baddress = (byte)reg.address;
                    OpReglist.Add(baddress);
                    if (((baddress >= 0x18) & (baddress <= 0x1B)) | ((baddress >= 0x28) & (baddress <= 0x2B)))
                        OffsetRegList.Add(baddress);
                    else
                        OtherRegList.Add(baddress);
                }
            }

            OpReglist = OpReglist.Distinct().ToList();
            OffsetRegList = OffsetRegList.Distinct().ToList();
            OtherRegList = OtherRegList.Distinct().ToList();
            if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "RegisterConfig".ToLower()) == 0)
            {
                //if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                if (msg.task_parameterlist.parameterlist.Count == 1)
                    return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

                if (isOPBank2Empty() == true)
                {
                    offset = 0;
                    msg.gm.message = "Reading bank1.";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                }
                else
                {
                    offset = 4;
                    msg.gm.message = "Reading bank2.";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                }

                foreach (byte badd in OffsetRegList)
                {
                    ret = ReadByte((byte)(badd + offset), ref bdata);
                    parent.m_MapRegImg[badd].err = ret;
                    parent.m_MapRegImg[badd].val = (UInt16)bdata;
                }
                foreach (byte badd in OtherRegList)
                {
                    ret = ReadByte(badd, ref bdata);
                    parent.m_MapRegImg[badd].err = ret;
                    parent.m_MapRegImg[badd].val = (UInt16)bdata;
                }
                return ret;
            }
            else if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "EfuseConfig".ToLower()) == 0)
            {
                //if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                if (msg.task_parameterlist.parameterlist.Count == 1)
                    return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

                if (string.IsNullOrEmpty(msg.funName))
                {
                    if (!m_bRead)
                    {
                        for (byte i = 0x15; i <= 0x1A; i++)
                        {
                            parent.m_OpRegImg[i].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            parent.m_OpRegImg[i].val = 0;
                        }
                        m_bRead = true;
                    }
                    else
                        m_bRead = false;
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else if (msg.funName.CompareTo("Read") != 0)
                {
                    if (!m_bRead)
                    {
                        for (byte i = 0x15; i <= 0x1A; i++)
                        {
                            parent.m_OpRegImg[i].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                            parent.m_OpRegImg[i].val = 0;
                        }
                        m_bRead = true;
                    }
                    else
                        m_bRead = false;
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }

                msg.funName = "Reset";
                ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please provide programming voltage!!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                if (isEFBank2Empty() == true)
                {
                    offset = 0;
                    msg.gm.message = "Reading bank1.";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                }
                else if (isEFBank2Empty() == false)
                {
                    offset = 4;
                    msg.gm.message = "Reading bank2.";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                }
            }
            else if (msg.gm.sflname == "Production" || msg.gm.sflname == "Mass Production")
                SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);

            foreach (byte badd in OffsetRegList)
            {
                ret = ReadByte((byte)(badd + offset), ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = (UInt16)bdata;
            }
            foreach (byte badd in OtherRegList)
            {
                ret = ReadByte(badd, ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = (UInt16)bdata;
            }
            if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "EfuseConfig".ToLower()) == 0)
            {
                msg.gm.message = "Please remove programming voltage!!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
                ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            byte offset = 0;
            byte bdata = 0;
            List<byte> OpReglist = new List<byte>();
            List<byte> OffsetRegList = new List<byte>(); //会去偏移的一些寄存器
            List<byte> OtherRegList = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)  //略过虚拟参数
                    continue;
                if (p == null) break;
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    reg = dic.Value;
                    baddress = (byte)reg.address;
                    OpReglist.Add(baddress);
                    if (((baddress >= 0x18) & (baddress <= 0x1B)) | ((baddress >= 0x28) & (baddress <= 0x2B)))
                        OffsetRegList.Add(baddress);
                    else
                        OtherRegList.Add(baddress);
                }
            }

            OpReglist = OpReglist.Distinct().ToList();
            OffsetRegList = OffsetRegList.Distinct().ToList();
            OtherRegList = OtherRegList.Distinct().ToList();
            if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "EfuseConfig".ToLower()) == 0)
            {
                ret = EfuseBlockWrite(ref msg);
                return ret;
            }
            else if (string.Compare(msg.gm.sflname.Trim().Replace(" ", "").ToLower(), "RegisterConfig".ToLower()) == 0)
            {
                //if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                if (msg.task_parameterlist.parameterlist.Count == 1)
                    return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

                ret = ReadByte(ElementDefine.WORKMODE_REG, ref bdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((bdata & 0x03) != (byte)ElementDefine.WORK_MODE.WRITE_MAP_CTRL)
                    return ElementDefine.IDS_ERR_DEM_ERROR_MODE;

                if (isOPConfigEmpty() == false)
                {
                    msg.gm.message = "Configuration parameters are frozen. Skip to program them.";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                    for (byte i = 0x25; i <= 0x27; i++)
                    {
                        if (OtherRegList.Contains(i))
                            OtherRegList.Remove(i);
                    }
                }
                if (isOPBank1Empty() == true)
                {
                    offset = 0;
                    msg.gm.message = "Writing bank1.";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                }
                else if (isOPBank2Empty() == true)
                {
                    offset = 4;
                    ret = ElementDefine.IDS_ERR_DEM_FROZEN_OP;
                    return ret;
                }
                else
                {
                    return ElementDefine.IDS_ERR_DEM_FROZEN;
                }

                foreach (byte badd in OffsetRegList)
                {
                    ret = WriteByte((byte)(badd + offset), (byte)parent.m_MapRegImg[badd].val);
                    parent.m_MapRegImg[badd].err = ret;
                }
                foreach (byte badd in OtherRegList)
                {
                    ret = WriteByte(badd, (byte)parent.m_MapRegImg[badd].val);
                    parent.m_MapRegImg[badd].err = ret;
                }
                return ret;
            }
            foreach (byte badd in OffsetRegList)
            {
                ret = WriteByte((byte)(badd + offset), (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }
            foreach (byte badd in OtherRegList)
            {
                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
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

            List<Parameter> ParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                ParamList.Add(p);
            }
            ParamList.Reverse();

            if (ParamList.Count != 0)
            {
                for (int i = 0; i < ParamList.Count; i++)
                {
                    param = (Parameter)ParamList[i];
                    if (param == null) continue;
                    m_parent.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            if (msg.gm.sflname == "EfuseConfig" || msg.gm.sflname.Equals("EFUSE Config"))
            {
                if (msg.funName == null)
                {
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else if (msg.funName.Equals("Verify") || msg.funName.Equals("Read"))
                {
                    ;   //do nothing
                }
                else    //Issue1369
                {
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
            }
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> ParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                ParamList.Add(p);
            }

            if (ParamList.Count != 0)
            {
                for (int i = 0; i < ParamList.Count; i++)
                {
                    param = (Parameter)ParamList[i];
                    if (param == null) continue;
                    m_parent.Physical2Hex(ref param);
                }
            }
            return ret;
        }

        private UInt32 ConvertPhysicalToHexClean(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> ParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                ParamList.Add(p);
            }

            if (ParamList.Count != 0)
            {
                for (int i = 0; i < ParamList.Count; i++)
                {
                    param = (Parameter)ParamList[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.FROZEN_BIT_CHECK_PC:
                    ret = PowerOn();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = FrozenBitCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = PowerOff();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.FROZEN_BIT_CHECK:
                    ret = FrozenBitCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.DIRTY_CHIP_CHECK_PC:
                    ret = PowerOn();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = DirtyChipCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = PowerOff();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.DIRTY_CHIP_CHECK:
                    ret = DirtyChipCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.DOWNLOAD_PC:
                    ret = DownloadWithPowerControl(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.DOWNLOAD:
                    ret = DownloadWithoutPowerControl(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.READ_BACK_CHECK_PC:
                    ret = PowerOn();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = ReadBackCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = PowerOff();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.READ_BACK_CHECK:
                    ret = ReadBackCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    break;
                case ElementDefine.COMMAND.SAVE_EFUSE_HEX:
                    {
                        InitEfuseData();
                        ret = ConvertPhysicalToHexClean(ref msg);
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
            }
            return ret;
        }

        private bool bank1FRZ = false, bank2FRZ = false, cfgFRZ = false;
        private UInt32 FrozenBitCheck() //注意，这里没有把image里的Frozen bit置为1，记得在后面的流程中做这件事
        {
            SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte pval1 = 0, pval2 = 0;
            byte cfg = 0;
            ret = ReadByte((byte)ElementDefine.EF_CFG, ref cfg);
            ret = ReadByte((byte)ElementDefine.EF_USR_BANK1_TOP, ref pval1);
            ret = ReadByte((byte)ElementDefine.EF_USR_BANK2_TOP, ref pval2);

            if ((cfg & 0x80) == 0x80)
            {
                cfgFRZ = true;
            }
            else
                cfgFRZ = false;
            if ((pval1 & 0x80) == 0x80)
            {
                bank1FRZ = true;
            }
            else
                bank1FRZ = false;
            if ((pval2 & 0x80) == 0x80)
            {
                bank2FRZ = true;
            }
            else
                bank2FRZ = false;

            if (bank1FRZ && bank2FRZ)
            {
                return LibErrorCode.IDS_ERR_DEM_FROZEN;
            }

            return ret;
        }

        private UInt32 DirtyChipCheck()
        {
            SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            byte pval = 0;
            if (cfgFRZ == false)
            {
                for (int i = 0; i < ElementDefine.EF_CFG_SIZE; i++)
                {
                    ret = ReadByte((byte)(ElementDefine.EF_CFG - i), ref pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    if (pval != 0)
                        return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                }
            }
            if (bank1FRZ == false)
            {
                for (byte index = (byte)ElementDefine.EF_USR_BANK1_OFFSET; index <= (byte)ElementDefine.EF_USR_BANK1_TOP; index++)
                {
                    ret = ReadByte(index, ref pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        return ret;
                    }
                    else if (pval != 0)
                    {
                        return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                    }
                }
                return ret;
            }
            if (bank2FRZ == false)
            {
                for (byte index = (byte)ElementDefine.EF_USR_BANK2_OFFSET; index <= (byte)ElementDefine.EF_USR_BANK2_TOP; index++)
                {
                    ret = ReadByte(index, ref pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        return ret;
                    }
                    else if (pval != 0)
                    {
                        return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                    }
                }
                return ret;
            }
            return ret;
        }

        private void InitEfuseData()
        {
            byte addr = 0;
            for (int i = 0; i < ElementDefine.EF_CFG_SIZE; i++)
            {
                addr = (byte)(ElementDefine.EF_CFG - i);
                parent.m_OpRegImg[addr].err = 0;
                parent.m_OpRegImg[addr].val = 0;
            }

            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                parent.m_OpRegImg[i].err = 0;
                parent.m_OpRegImg[i].val = 0;
            }
        }

        private void PrepareHexData()
        {
            if (cfgFRZ == false)
                parent.m_OpRegImg[ElementDefine.EF_CFG].val |= 0x80;    //Set Frozen bit in image
            if (bank1FRZ == false)
                parent.m_OpRegImg[ElementDefine.EF_USR_BANK1_TOP].val |= 0x80;    //Set Frozen bit in image
        }

        private byte WritingBank1Or2 = 0;   //bank1
        private UInt32 DownloadWithPowerControl(ref TASKMessage msg)
        {
            byte addr = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            PrepareHexData();
            ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = PowerOn();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            byte offset = 0;
            if (cfgFRZ == true)
            {
                msg.gm.message = "Register 0x17 is frozen. Skip to program it.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
            }
            else
            {
                for (int i = 0; i < ElementDefine.EF_CFG_SIZE; i++)
                {
                    addr = (byte)(ElementDefine.EF_CFG - i);
                    ret = parent.m_OpRegImg[addr].err;
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    EFUSEUSRbuf[6 - i] = parent.m_OpRegImg[addr].val;
                    ret = WriteByte(addr, (byte)parent.m_OpRegImg[addr].val);
                    parent.m_OpRegImg[addr].err = ret;
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            if (bank1FRZ == false)
            {
                msg.gm.message = "Writing bank1.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                WritingBank1Or2 = 0;
            }
            else if (bank2FRZ == false)
            {
                offset = 4;
                msg.gm.message = "Bank1 is frozen, writing bank2.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                WritingBank1Or2 = 1;
            }

            for (byte badd = (byte)ElementDefine.EF_USR_BANK1_OFFSET; badd <= (byte)ElementDefine.EF_USR_BANK1_TOP; badd++)
            {
                ret = parent.m_OpRegImg[badd].err;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                EFUSEUSRbuf[badd - ElementDefine.EF_USR_BANK1_OFFSET] = parent.m_OpRegImg[badd].val;
                ret = WriteByte((byte)(badd + offset), (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[(byte)(badd)].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                byte tmp = 0;
                ret = ReadByte((byte)(badd + offset), ref tmp);     //Issue 1746 workaround
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = PowerOff();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
            return ret;
        }

        private UInt32 DownloadWithoutPowerControl(ref TASKMessage msg)
        {
            byte addr = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            PrepareHexData();
            ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            byte offset = 0;
            if (cfgFRZ == true)
            {
                msg.gm.message = "Register 0x17 is frozen. Skip to program it.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
            }
            else
            {
                for (int i = 0; i < ElementDefine.EF_CFG_SIZE; i++)
                {
                    addr = (byte)(ElementDefine.EF_CFG - i);
                    ret = parent.m_OpRegImg[addr].err;
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    EFUSEUSRbuf[6 - i] = parent.m_OpRegImg[addr].val;
                    ret = WriteByte(addr, (byte)parent.m_OpRegImg[addr].val);
                    parent.m_OpRegImg[addr].err = ret;
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }

            if (bank1FRZ == false)
            {
                msg.gm.message = "Writing bank1.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                WritingBank1Or2 = 0;
            }
            else if (bank2FRZ == false)
            {
                offset = 4;
                //System.Windows.Forms.MessageBox.Show("Bank1 is frozen, writing bank2.");
                msg.gm.message = "Bank1 is frozen, writing bank2.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                WritingBank1Or2 = 1;
            }

            for (byte badd = (byte)ElementDefine.EF_USR_BANK1_OFFSET; badd <= (byte)ElementDefine.EF_USR_BANK1_TOP; badd++)
            {
                ret = parent.m_OpRegImg[badd].err;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                EFUSEUSRbuf[badd - ElementDefine.EF_USR_BANK1_OFFSET] = parent.m_OpRegImg[badd].val;
                ret = WriteByte((byte)(badd + offset), (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[(byte)(badd)].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
            return ret;
        }

        private UInt32 ReadBackCheck()
        {
            byte addr = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            byte pval = 0;
            for (byte badd = (byte)ElementDefine.EF_USR_BANK1_OFFSET; badd <= (byte)ElementDefine.EF_USR_BANK1_TOP; badd++)
            {
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    Thread.Sleep(100);
                    ret = ReadByte((byte)(badd + 4 * WritingBank1Or2), ref pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue ;
                    if (pval != EFUSEUSRbuf[badd - ElementDefine.EF_USR_BANK1_OFFSET])
                    {
                        ret = LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                        continue;
                    }
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                if(ret !=LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    FolderMap.WriteFile(string.Format("address:0x{0:x1}, data:0x{1:x2}, return:0x{2:x4}", (badd + 4 * WritingBank1Or2), pval, ret));
                    return ret;
                }
            }
            if (cfgFRZ == false)
            {
                for (int i = 0; i < ElementDefine.EF_CFG_SIZE; i++)
                {
                    addr = (byte)(ElementDefine.EF_CFG - i);
                    for (int k = 0; k < ElementDefine.RETRY_COUNTER; k++)
                    {
                        Thread.Sleep(100);
                        ret = ReadByte(addr, ref pval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                        if (pval != EFUSEUSRbuf[6 - i])
                        {
                            ret = LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                            continue;
                        }
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        FolderMap.WriteFile(string.Format("address:0x{0:x1}, data:0x{1:x2}, return:0x{2:x4}", addr, pval, ret));
                        return ret;
                    }
                }
            }
            return ret;
        }

        private UInt32 GetEfuseHexData(ref TASKMessage msg)
        {
            string tmp = "";
            if (parent.m_OpRegImg[ElementDefine.EF_CFG].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_OpRegImg[ElementDefine.EF_CFG].err;
            tmp += "0x15" + ", " + "0x" + parent.m_OpRegImg[0x15].val.ToString("X2") + "\r\n";
            tmp += "0x16" + ", " + "0x" + parent.m_OpRegImg[0x16].val.ToString("X2") + "\r\n";
            tmp += "0x" + ElementDefine.EF_CFG.ToString("X2") + ", " + "0x" + parent.m_OpRegImg[ElementDefine.EF_CFG].val.ToString("X2") + "\r\n";
            for (ushort i = ElementDefine.EF_USR_BANK1_OFFSET; i <= ElementDefine.EF_USR_BANK1_TOP; i++)
            {
                if (parent.m_OpRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_OpRegImg[i].err;
                tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_OpRegImg[i].val.ToString("X2") + "\r\n";
            }
            msg.sm.efusehexdata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 GetEfuseBinData(ref TASKMessage msg)
        {
            List<byte> tmp = new List<byte>();
            if (parent.m_OpRegImg[ElementDefine.EF_CFG].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_OpRegImg[ElementDefine.EF_CFG].err;
            tmp.Add(0x15);
            tmp.Add((byte)(parent.m_OpRegImg[0x15].val));
            tmp.Add(0x16);
            tmp.Add((byte)(parent.m_OpRegImg[0x16].val));
            tmp.Add((byte)ElementDefine.EF_CFG);
            tmp.Add((byte)(parent.m_OpRegImg[ElementDefine.EF_CFG].val));
            for (ushort i = ElementDefine.EF_USR_BANK1_OFFSET; i <= ElementDefine.EF_USR_BANK1_TOP; i++)
            {
                if (parent.m_OpRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_OpRegImg[i].err;
                tmp.Add((byte)i);
                tmp.Add((byte)(parent.m_OpRegImg[i].val));
            }
            msg.sm.efusebindata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 EpBlockRead()
        {
            byte pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
          
            ret = ReadByte(0x39, ref pval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            pval |= 0x08;
            ret = WriteByte(0x39, pval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = ReadByte(0x39, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((pval & 0x08) == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;

                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            return LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            string shwversion = String.Empty;
            byte hval = 0, lval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadByte(0x00, ref hval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadByte(0x01, ref lval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)SharedFormula.MAKEWORD(lval, hval);
            deviceinfor.shwversion = "A0";

            if ((isEFBank1Empty() == true) && (isEFBank2Empty() == false))
                return ElementDefine.IDS_ERR_DEM_BLOCK;

            if ((isOPBank1Empty() == true) && (isOPBank2Empty() == false))
                return ElementDefine.IDS_ERR_DEM_BLOCK;
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

        #region Others
        private UInt32 EfuseBlockWrite(ref TASKMessage msg)
        {
            Reg reg = null;
            byte bdata = 0;
            byte baddress = 0;
            byte offset = 0;
            List<byte> OpReglist = new List<byte>();
            List<byte> OffsetRegList = new List<byte>(); //会去偏移的一些寄存器
            List<byte> OtherRegList = new List<byte>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)  //略过虚拟参数
                    continue;
                if (p == null) break;
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    reg = dic.Value;
                    baddress = (byte)reg.address;
                    OpReglist.Add(baddress);
                    if (((baddress >= 0x18) & (baddress <= 0x1B)) | ((baddress >= 0x28) & (baddress <= 0x2B)))
                        OffsetRegList.Add(baddress);
                    else
                        OtherRegList.Add(baddress);
                }
            }

            OpReglist = OpReglist.Distinct().ToList();
            OffsetRegList = OffsetRegList.Distinct().ToList();
            OtherRegList = OtherRegList.Distinct().ToList();

            //if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
            if (msg.task_parameterlist.parameterlist.Count == 1)
                return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

            ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.gm.message = "Please provide programming voltage!!!";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
            if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
            #region Write
            if (isEFConfigEmpty() == false)
            {
                msg.gm.message = "Configuration parameters are frozen. Skip to program them.";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                for (byte i = 0x15; i <= 0x17; i++)
                {
                    if (OtherRegList.Contains(i))
                        OtherRegList.Remove(i);
                }
            }
            else
            {
                parent.m_OpRegImg[0x17].val |= 0x80;
            }

            if (isEFBank1Empty() == true)
            {
                offset = 0;
                msg.gm.message = "Writing bank1.";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                parent.m_OpRegImg[0x1b].val |= 0x80;
            }
            else if (isEFBank2Empty() == true)
            {
                offset = 4;
                msg.gm.message = "Bank1 is frozen, writing bank2.";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                parent.m_OpRegImg[0x1b].val |= 0x80;
            }
            else
            {
                return ElementDefine.IDS_ERR_DEM_FROZEN;
            }
            foreach (byte badd in OffsetRegList)
            {
                ret = WriteByte((byte)(badd + offset), (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }
            foreach (byte badd in OtherRegList)
            {
                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }
            #endregion
            #region Read
            foreach (byte badd in OffsetRegList)
            {
                ret = ReadByte((byte)(badd + offset), ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = (UInt16)bdata;
            }
            foreach (byte badd in OtherRegList)
            {
                ret = ReadByte(badd, ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = (UInt16)bdata;
            }
            msg.gm.message = "Please remove programming voltage!!!";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
            if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
            ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
            #endregion
            return ret;
        }
        #endregion
    }
}