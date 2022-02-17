using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.OZ88030
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

        #region YFLASH操作常量定义
        private const int RETRY_COUNTER = 5;
        private const byte YFLASH_WORKMODE_REG = 0x39;
        #endregion

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
        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
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

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, UInt16 data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = SharedFormula.HiByte(data);
            pdata[4] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 5);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, UInt16 data)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = SharedFormula.HiByte(data);
            pdata[3] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 4);
        }

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[3];
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
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    bCrc = receivebuf[2];
                    wdata = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    if (bCrc != calc_crc_read(sendbuf[0], sendbuf[1], wdata))
                    {
                        pval = ElementDefine.PARAM_HEX_ERROR;
                        ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    else
                    {
                        pval = wdata;
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    }
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
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
            sendbuf[2] = SharedFormula.HiByte(val);
            sendbuf[3] = SharedFormula.LoByte(val);
            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
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

        #region YFLASH寄存器操作
        #region YFLASH寄存器父级操作
        internal UInt32 YFLASHReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_EFUSE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnReadWord(reg, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_NORMAL);
            }
            return ret;
        }

        internal UInt32 YFLASHWriteWord(byte reg, UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = CheckFrozenBit(reg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            lock (m_lock)
            {
                ret = OnWorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_EFUSE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWriteWord(reg, pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                ret = OnWorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_NORMAL);
            }
            return ret;
        }
        #endregion

        #region YFLASH寄存器子级操作
        protected UInt32 OnWorkMode(ElementDefine.COBRA_WARRIORS_WKM wkm)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnReadWord(YFLASH_WORKMODE_REG, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata |= 0x8000;
            ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            wdata &= 0xFFFC;
            switch (wkm)
            {
                case ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_NORMAL:
                    {
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, wdata);
                        break;
                    }
                case ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_MAPREG_PROGRAM:
                case ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_EFUSE_PROGRAM:
                    {
                        ret = OnWriteWord(YFLASH_WORKMODE_REG, (UInt16)(wdata | (byte)wkm));
                        break;
                    }
            }
            return ret;
        }
        #endregion
        #endregion

        #region YFLASH功能操作
        #region YFLASH功能父级操作
        protected UInt32 WorkMode(ElementDefine.COBRA_WARRIORS_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }

        protected UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 EraseEEPROM(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
                p.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = BlockErase(ref msg);
            return ret;
        }

        public UInt32 EpBlockRead()
        {
            return BlockRead();
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> EfuseMapReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            if (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                if (baddress >= ElementDefine.MAP_REG_START_ADDR)
                                    EfuseMapReglist.Add(baddress);
                                else
                                    YFLASHReglist.Add(baddress);
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
            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            EfuseMapReglist = EfuseMapReglist.Distinct().ToList();
            if ((EfuseMapReglist.Count == 1)||(YFLASHReglist.Count == 1)) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;
            //Read 
            if (YFLASHReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_EFUSE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in YFLASHReglist)
                {
                    //ret = YFLASHReadWord(badd, ref wdata);
                    ret = ReadWord(badd, ref wdata);
                    parent.m_YFRegImg[badd].err = ret;
                    parent.m_YFRegImg[badd].val = wdata;
                }
                //ret = WorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_NORMAL);
                //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (EfuseMapReglist.Count != 0)
            {
                foreach (byte badd in EfuseMapReglist)
                {
                    ret = ReadWord(badd, ref wdata);
                    parent.m_YFRegImg[badd].err = ret;
                    parent.m_YFRegImg[badd].val = wdata;
                }
            }
            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();
            List<byte> OpMapReglist = new List<byte>();
            List<byte> EfuseMapReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;
                                if (baddress >= ElementDefine.MAP_REG_START_ADDR)
                                    EfuseMapReglist.Add(baddress);
                                else
                                    YFLASHReglist.Add(baddress);
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
                                if (baddress < ElementDefine.OP_REG_START_ADDR)
                                    OpMapReglist.Add(baddress);
                                else
                                    OpReglist.Add(baddress);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            OpMapReglist = OpMapReglist.Distinct().ToList();
            EfuseMapReglist = EfuseMapReglist.Distinct().ToList();
            if ((EfuseMapReglist.Count == 1) || (YFLASHReglist.Count == 1)) //避免后期增减参数，所以目前为0x1C个，暂设为0x10
                return ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE;

            //Write 
            if (YFLASHReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_EFUSE_PROGRAM);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in YFLASHReglist)
                {
                    ret = WriteWord(badd, parent.m_YFRegImg[badd].val);
                    parent.m_YFRegImg[badd].err = ret;
                }
                ret = WorkMode(ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            if (EfuseMapReglist.Count != 0)
            {
                ret = ReadWord(ElementDefine.WORKMODE_REG, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wval & 0x8003) != (UInt16)ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_MAPREG_PROGRAM)
                    return ElementDefine.IDS_ERR_DEM_ERROR_MODE;

                foreach (byte badd in EfuseMapReglist)
                {
                    ret = WriteWord(badd, parent.m_YFRegImg[badd].val);
                    parent.m_YFRegImg[badd].err = ret;
                }
            }
            if (OpMapReglist.Count != 0)
            {
                ret = ReadWord(ElementDefine.WORKMODE_REG, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                if ((wval & 0x8003) != (UInt16)ElementDefine.COBRA_WARRIORS_WKM.EFLASHMODE_MAPREG_PROGRAM)
                    return ElementDefine.IDS_ERR_DEM_ERROR_MODE;

                foreach (byte badd in OpMapReglist)
                {
                    ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                    parent.m_OpRegImg[badd].err = ret;
                }
            }
            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
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

            List<Parameter> YFLASHParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            YFLASHParamList.Add(p);
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

            if (YFLASHParamList.Count != 0)
            {
                for (int i = 0; i < YFLASHParamList.Count; i++)
                {
                    param = (Parameter)YFLASHParamList[i];
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

            List<Parameter> YFLASHParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.YFLASHElement:
                        {
                            if (p == null) break;
                            YFLASHParamList.Add(p);
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

            if (YFLASHParamList.Count != 0)
            {
                for (int i = 0; i < YFLASHParamList.Count; i++)
                {
                    param = (Parameter)YFLASHParamList[i];
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
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            int ival = 0;
            UInt16 wval = 0;
            string shwversion = String.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)SharedFormula.HiByte(wval);
            ival = (int)((SharedFormula.LoByte(wval) & 0x70) >> 4);
            deviceinfor.hwversion = ival;
            switch (ival)
            {
                case 0:
                    shwversion = "A";
                    break;
                case 1:
                    shwversion = "B";
                    break;
            }
            ival = (int)(SharedFormula.LoByte(wval) & 0x07);
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)(SharedFormula.LoByte(wval) & 0x07);

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (SharedFormula.HiByte(type) != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if (((SharedFormula.LoByte(type) & 0x70) >> 4) != deviceinfor.hwversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
                if ((SharedFormula.LoByte(type) & 0x07) != deviceinfor.hwsubversion)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            #region Check Frozen Bit
            /*ret = MapRegReadWord(0x23, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_forzen_struct.m_map_cfgfrz = ((wval & 0x8000) == 0x8000) ? true : false;

            ret = MapRegReadWord(0x24, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_forzen_struct.m_map_bank1 = ((wval & 0x8000) == 0x8000) ? true : false;

            ret = MapRegReadWord(0x2A, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_forzen_struct.m_map_bank2 = ((wval & 0x8000) == 0x8000) ? true : false;
            
            ret = YFLASHReadWord(0x13, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_forzen_struct.m_efuse_cfgfrz = ((wval & 0x8000) == 0x8000) ? true : false;

            ret = YFLASHReadWord(0x14, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_forzen_struct.m_efuse_bank1 = ((wval & 0x8000) == 0x8000) ? true : false;

            ret = YFLASHReadWord(0x1A, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.m_forzen_struct.m_efuse_bank2 = ((wval & 0x8000) == 0x8000) ? true : false;*/
            #endregion
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

        #region 其他服务
        public UInt32 CheckFrozenBit(byte reg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (reg == 0x13)
            {
                if (parent.m_forzen_struct.m_efuse_cfgfrz) return ElementDefine.IDS_ERR_DEM_EFUSE_CFGFRZ;
            }
            else if ((reg > 0x13) & (reg < 0x1A))
            {
                if (parent.m_forzen_struct.m_efuse_bank1) return ElementDefine.IDS_ERR_DEM_EFUSE_BANK1;
            }
            else if ((reg > 0x19) & (reg < 0x20))
            {
                if (parent.m_forzen_struct.m_efuse_bank2) return ElementDefine.IDS_ERR_DEM_EFUSE_BANK2;
            }
            else if (reg == 0x23)
            {
                if (parent.m_forzen_struct.m_map_cfgfrz) return ElementDefine.IDS_ERR_DEM_MAP_CFGFRZ;
            }
            else if ((reg > 0x13) & (reg < 0x1A))
            {
                if (parent.m_forzen_struct.m_map_bank1) return ElementDefine.IDS_ERR_DEM_MAP_BANK1;
            }
            else if ((reg > 0x19) & (reg < 0x20))
            {
                if (parent.m_forzen_struct.m_map_bank2) return ElementDefine.IDS_ERR_DEM_MAP_BANK2;
            }
            return ret;
        }
        #endregion
    }
}