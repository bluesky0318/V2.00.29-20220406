using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ26305
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

        protected UInt32 WorkMode(ElementDefine.COBRA_PIKACHU5_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
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
        #endregion

        #region 操作寄存器子级操作
        public byte crc4_calc(byte[] pdata, int first, int last)
        {

            byte crc = 0;
            byte crcdata;
            UInt16 d = 0;
            byte poly = 0x03;   // poly
                                //WORD p = (WORD)poly + 0x100;
            int n, j;               // the length of the data

            if (first < last)
            {
                for (n = first; n <= last; n++)
                {
                    crcdata = pdata[n];
                    for (j = 0x8; j > 0; j >>= 1)
                    {
                        if ((crc & 0x8) != 0)
                        {
                            crc <<= 1;
                            crc ^= poly;
                        }
                        else
                            crc <<= 1;
                        if ((crcdata & j) != 0)
                            crc ^= poly;
                    }
                    crc = (byte)(crc & 0xf);
                }
            }
            else
            {
                for (n = first; n >= last; n--)
                {
                    crcdata = pdata[n];
                    for (j = 0x8; j > 0; j >>= 1)
                    {
                        if ((crc & 0x8) != 0)
                        {
                            crc <<= 1;
                            crc ^= poly;
                        }
                        else
                            crc <<= 1;
                        if ((crcdata & j) != 0)
                            crc ^= poly;
                    }
                    crc = (byte)(crc & 0xf);
                }
            }
            return crc;
        }
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

        protected UInt32 OnWorkMode(ElementDefine.COBRA_PIKACHU5_WKM wkm)
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadByte(ElementDefine.WORKMODE_REG, ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bdata |= 0x80;
            ret = OnWriteByte(ElementDefine.WORKMODE_REG, bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            bdata &= 0xFC;
            switch (wkm)
            {
                case ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_NORMAL:
                    {
                        ret = OnWriteByte(ElementDefine.WORKMODE_REG, bdata);
                        break;
                    }
                case ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_MAPREG_PROGRAM:
                case ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_EFUSE_PROGRAM:
                    {
                        ret = OnWriteByte(ElementDefine.WORKMODE_REG, (byte)(bdata | (byte)wkm));
                        break;
                    }
            }
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
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            byte bdata = 0x08;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = WriteByte(0x39, bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = ReadByte(0x39, ref bdata);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    if ((bdata & 0x08) != 0x00)
                    {
                        ret = LibErrorCode.IDS_ERR_DEM_MAPPING_TIMEOUT;
                        continue;
                    }
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EEPROMReglist = new List<byte>();
            List<byte> EEPROMMapReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                if (baddress >= ElementDefine.MAP_REG_START_ADDR)
                                    EEPROMMapReglist.Add(baddress);
                                else
                                    EEPROMReglist.Add(baddress);
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

            OpReglist = OpReglist.Distinct().ToList();
            EEPROMReglist = EEPROMReglist.Distinct().ToList();
            EEPROMMapReglist = EEPROMMapReglist.Distinct().ToList();
            if ((EEPROMMapReglist.Count == 1) || (EEPROMReglist.Count == 1)) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;
            //Read 
            if (EEPROMReglist.Count != 0)
            {
                foreach (byte badd in EEPROMReglist)
                {
                    ret = ReadByte(badd, ref bdata);
                    parent.m_EEPROMRegImg[badd].err = ret;
                    parent.m_EEPROMRegImg[badd].val = bdata;
                }
            }
            if (EEPROMMapReglist.Count != 0)
            {
                foreach (byte badd in EEPROMMapReglist)
                {
                    ret = ReadByte(badd, ref bdata);
                    parent.m_EEPROMRegImg[badd].err = ret;
                    parent.m_EEPROMRegImg[badd].val = bdata;
                }
            }
            foreach (byte badd in OpReglist)
            {
                ret = ReadByte(badd, ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = bdata;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EEPROMReglist = new List<byte>();
            List<byte> EEPROMMapReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                if (baddress >= ElementDefine.MAP_REG_START_ADDR)
                                    EEPROMMapReglist.Add(baddress);
                                else
                                    EEPROMReglist.Add(baddress);
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

            OpReglist = OpReglist.Distinct().ToList();
            EEPROMReglist = EEPROMReglist.Distinct().ToList();
            EEPROMMapReglist = EEPROMMapReglist.Distinct().ToList();
            if ((EEPROMMapReglist.Count == 1) || (EEPROMReglist.Count == 1)) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

            //Write 
            if (EEPROMReglist.Count != 0)
            {
                ret = FrozenBitCheck();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_EFUSE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please provide 7.2V power supply to VPP pin, and limit its current to 150mA.";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                foreach (byte badd in EEPROMReglist)
                {
                    ret = WriteByte(badd, (byte)parent.m_EEPROMRegImg[badd].val);
                    parent.m_EEPROMRegImg[badd].err = ret;
                }

                CountCheckSumAndFrozen();
                ret = WriteByte(0x1C, (byte)parent.m_EEPROMRegImg[0x1C].val);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                msg.gm.message = "Please remove 7.2V power supply from VPP pin.";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (EEPROMMapReglist.Count != 0)
            {
                /*
                ret = ReadByte(ElementDefine.WORKMODE_REG, ref bdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((bdata & 0x83) != (UInt16)ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_MAPREG_PROGRAM)
                    return ElementDefine.IDS_ERR_DEM_ERROR_MODE;*/
                ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_MAPREG_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in EEPROMMapReglist)
                {
                    ret = WriteByte(badd, (byte)parent.m_EEPROMRegImg[badd].val);
                    parent.m_EEPROMRegImg[badd].err = ret;
                }

                ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
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

            List<Parameter> EFUSEParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.EEPROMElement:
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
                    case ElementDefine.EEPROMElement:
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
                        ret = ConvertPhysicalToHex(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        CountCheckSumAndFrozen();
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
                case ElementDefine.COMMAND.LOAD_BIN_FILE:
                    {
                        string binFileName = msg.sub_task_json;

                        var blist = SharedAPI.LoadBinFileToList(binFileName);
                        if (blist.Count == 0)
                            ret = LibErrorCode.IDS_ERR_DEM_LOAD_BIN_FILE_ERROR;
                        else
                        {
                            ret = LoadBinData(blist);
                        }
                        ret = ConvertHexToPhysical(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
            }
            return ret;
        }

        public uint LoadBinData(List<byte> blist)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            bool isAddress = true;
            ushort address = 0;
            foreach (var item in blist)
            {
                if (isAddress)
                {
                    address = item;
                    isAddress = false;
                }
                else
                {
                    parent.m_EEPROMRegImg[address].err = ret;
                    parent.m_EEPROMRegImg[address].val = item;
                    isAddress = true;
                }
            }
            return ret;
        }

        private UInt32 FrozenBitCheck()
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadByte((byte)ElementDefine.EF_USR_TOP, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if ((bval & ElementDefine.USR_FROZEN_MASK) == ElementDefine.USR_FROZEN_Data)
                return LibErrorCode.IDS_ERR_DEM_FROZEN;
            return ret;
        }

        private UInt32 DirtyChipCheck()
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                ret = ReadByte((byte)i, ref bval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                if (bval != 0xFF)
                    return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
            }
            return ret;
        }
        private void InitEfuseData()
        {
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                parent.m_EEPROMRegImg[i].err = 0;
                parent.m_EEPROMRegImg[i].val = 0xFF;
                if (i == 0x18) parent.m_EEPROMRegImg[i].val = 0xC0;
            }
        }
        private UInt32 DownloadWithPowerControl(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            LoadEFRegImgFromEFUSEBin(msg.sm.efusebindata);

            ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_EFUSE_PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = PowerOn();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            CountCheckSumAndFrozen();
            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                ret = WriteByte(badd, (byte)parent.m_EEPROMRegImg[badd].val);
                parent.m_EEPROMRegImg[(byte)(badd)].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            ret = PowerOff();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_NORMAL);
            return ret;
        }
        private UInt32 DownloadWithoutPowerControl(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            LoadEFRegImgFromEFUSEBin(msg.sm.efusebindata);
            ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_EFUSE_PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            CountCheckSumAndFrozen();
            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                ret = WriteByte(badd, (byte)parent.m_EEPROMRegImg[badd].val);
                parent.m_EEPROMRegImg[(byte)(badd)].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            ret = WorkMode(ElementDefine.COBRA_PIKACHU5_WKM.EFLASHMODE_NORMAL);
            return ret;
        }
        private UInt32 ReadBackCheck()
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
                {
                    Thread.Sleep(100);
                    ret = ReadByte(badd, ref bval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    if (bval != parent.m_EEPROMRegImg[badd].val)
                    {
                        ret = LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                        continue;
                    }
                    break;
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            return ret;
        }
        private UInt32 GetEfuseHexData(ref TASKMessage msg)
        {
            string tmp = String.Empty;
            tmp = "0x10, 0x7F" + "\r\n";
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                if (parent.m_EEPROMRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL) return parent.m_EEPROMRegImg[i].err;
                tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_EEPROMRegImg[i].val.ToString("X2") + "\r\n";
            }
            msg.sm.efusehexdata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 GetEfuseBinData(ref TASKMessage msg)
        {
            List<byte> tmp = new List<byte>();
            tmp.Add(0x10);
            tmp.Add(0x7F);
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                if (parent.m_EEPROMRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_EEPROMRegImg[i].err;
                tmp.Add((byte)i);
                tmp.Add((byte)(parent.m_EEPROMRegImg[i].val));
            }
            msg.sm.efusebindata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        private void LoadEFRegImgFromEFUSEBin(List<byte> efusebindata)
        {
            byte badd = 0;
            for (int i = 0; i < (ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1); i++)
            {
                badd = efusebindata[i * 2];
                parent.m_EEPROMRegImg[badd].val = efusebindata[i * 2 + 1];
            }
        }
        private void CountCheckSumAndFrozen()
        {
            byte crc_result = 0x0F;
            byte[] crc_array = new byte[13];
            crc_array[0] = (byte)((((byte)parent.m_EEPROMRegImg[0x16].val) >> 4) & 0x0D);
            crc_array[1] = (byte)(((byte)parent.m_EEPROMRegImg[0x16].val) & 0x0F);
            crc_array[2] = (byte)((((byte)parent.m_EEPROMRegImg[0x17].val) >> 4) & 0x0F);
            crc_array[3] = (byte)(((byte)parent.m_EEPROMRegImg[0x17].val) & 0x0F);
            crc_array[4] = (byte)((((byte)parent.m_EEPROMRegImg[0x18].val) >> 4) & 0x0F);
            crc_array[5] = (byte)(((byte)parent.m_EEPROMRegImg[0x18].val) & 0x0F);
            crc_array[6] = (byte)((((byte)parent.m_EEPROMRegImg[0x19].val) >> 4) & 0x0B);
            crc_array[7] = (byte)(((byte)parent.m_EEPROMRegImg[0x19].val) & 0x0F);
            crc_array[8] = (byte)((((byte)parent.m_EEPROMRegImg[0x1A].val) >> 4) & 0x0F);//bit7 is NA and set as 0
            crc_array[9] = (byte)(((byte)parent.m_EEPROMRegImg[0x1A].val) & 0x0F);
            crc_array[10] = (byte)((((byte)parent.m_EEPROMRegImg[0x1B].val) >> 4) & 0x0F);
            crc_array[11] = (byte)(((byte)parent.m_EEPROMRegImg[0x1B].val) & 0x0F);
            crc_array[12] = 0x0A;
            crc_result = crc4_calc(crc_array, 0, crc_array.Length-2);//crc4_calc(crc_array, crc_array.Length);
            parent.m_EEPROMRegImg[0x1C].val = 0x00;
            parent.m_EEPROMRegImg[0x1C].val |= 0xA0;
            parent.m_EEPROMRegImg[0x1C].val |= (byte)(crc_result & 0x0F);
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