using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Collections;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.PenguinDC
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

        // YFLASH operation code
        private const byte EFUSE_VENDOR_ACCESS_DIS_REG = 0x41;
        private const byte EFUSE_VENDOR_ACCESS_DIS_MASK = 0xDF;

        private const byte EFUSE_PG_DIS_REG = 0x41;
        private const byte EFUSE_PG_DIS_MASK =0xEF; 

        private const byte EFUSE_RELOAD_REG = 0x4A;
        private const byte EFUSE_RELOAD_MASK = 0x01;

        private const byte I2C_CRC_DIS_REG = 0x51;
        private const byte I2C_CRC_DIS_EN_MASK = 0x80;

        private const byte EFUSE_FREEZE_REG = 0x57;
        private const byte EFUSE_FREEZE_MASK = 0x7F;

        private const byte EFUSE_REG_SHIFT = 0x50;
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
        protected UInt32 ReadByte(byte reg, ref byte pval)
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
        #endregion

        #region 操作寄存器子级操作
        private byte crc4_calc(byte[] pdata, int len)
        {
            byte crc = 0;
            byte crcdata;
            int n, j;                                      

            for (n = len - 1; n >= 0; n--)
            {
                crcdata = pdata[n];
                for (j = 0x8; j > 0; j >>= 1)
                {
                    if ((crc & 0x8) != 0)
                    {
                        crc <<= 1;
                        crc ^= 0x3;
                    }
                    else
                        crc <<= 1;
                    if ((crcdata & j) != 0)
                        crc ^= 0x3;
                }
                crc = (byte)(crc & 0xf);
            }
            return crc;
        }

        protected byte crc8_calc(ref byte[] pdata, byte n)
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
            pdata[3] = SharedFormula.HiByte(data);
            pdata[4] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 5);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[3];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data;

            return crc8_calc(ref pdata, 3);
        }

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
            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    pval = receivebuf[0];
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
            UInt16 DataInLen = 1;
            byte[] sendbuf = new byte[4];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            bool bcrc = (parent.m_busoption.GetOptionsByGuid(BusOptions.I2CPECMODE_GUID).SelectLocation.Code == 1) ? true : false;

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

            if (bcrc)
            {
                DataInLen = 2;
                sendbuf[3] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            }

            for (int i = 0; i < RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }
        #endregion
        #endregion

        #region YFLASH寄存器操作
        #region YFLASH寄存器父级操作
        protected UInt32 WorkMode(ElementDefine.COBRA_PENGUINDC_WKM wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWorkMode(wkm);
            }
            return ret;
        }

        internal UInt32 YFLASHReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnYFLASHReadByte(reg, ref pval);
            }
            return ret;
        }

        internal UInt32 YFLASHWriteByte(byte reg, byte bval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnYFLASHWriteByte(reg, bval);
            }
            return ret;
        }
        #endregion

        #region YFLASH寄存器子级操作
        protected UInt32 OnWorkMode(ElementDefine.COBRA_PENGUINDC_WKM wkm)
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch (wkm)
            {
                case ElementDefine.COBRA_PENGUINDC_WKM.YFLASH_WORKMODE_NORMAL:
                    {
                        ret = OnReadByte(EFUSE_VENDOR_ACCESS_DIS_REG, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        if ((bval & 0x20) == 0x20) return ret;
                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, (byte)(bval | 0x20));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret; ;

                        break;
                    }
                case ElementDefine.COBRA_PENGUINDC_WKM.REG_WORKMODE_41T4F_60_RW: //vendor_access_dis=0;efuse_pg_dis=x;efuse_freeze=x 
                    {
                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, 0xEA);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, 0x15);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    }
                case ElementDefine.COBRA_PENGUINDC_WKM.REG_WORKMODE_50T5F_RW: //vendor_access_dis=0;efuse_pg_dis=1;efuse_freeze=x 
                    {
                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, 0xEA);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, 0x15);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnReadByte(EFUSE_VENDOR_ACCESS_DIS_REG, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWriteByte(EFUSE_PG_DIS_REG, (byte)(bval | (~EFUSE_PG_DIS_MASK)));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    }
                case ElementDefine.COBRA_PENGUINDC_WKM.YFLASH_WORKMODE_50T5F_RW: //vendor_access_dis=0;efuse_pg_dis=0;efuse_freeze=x 
                    {
                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, 0xEA);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWriteByte(EFUSE_VENDOR_ACCESS_DIS_REG, 0x15); //vendor_access_dis=0
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnReadByte(EFUSE_VENDOR_ACCESS_DIS_REG, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWriteByte(EFUSE_PG_DIS_REG, (byte)(bval & EFUSE_PG_DIS_MASK));//efuse_pg_dis=0
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnReadByte(EFUSE_FREEZE_REG, ref bval);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        ret = OnWriteByte(EFUSE_FREEZE_REG, (byte)(bval & EFUSE_FREEZE_MASK));//efuse_freeze=0
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        break;
                    }
            }
            return ret;
        }

        protected UInt32 OnYFLASHReadByte(byte reg, ref byte pval)
        {
            return OnReadByte(reg, ref pval);
        }

        protected UInt32 OnYFLASHWriteByte(byte reg, byte val)
        {
            Thread.Sleep(2);
            return OnWriteByte(reg, val);
        }
        #endregion
        #endregion

        #region YFLASH功能操作
        #region YFLASH功能父级操作
        protected UInt32 BlockErase(ref TASKMessage msg)
        {
            byte bval = 1;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {
            }
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {

            }
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
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

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

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (YFLASHReglist.Count != 0)
            {
                ret = WorkMode(ElementDefine.COBRA_PENGUINDC_WKM.YFLASH_WORKMODE_50T5F_RW);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in YFLASHReglist)
                {
                    ret = YFLASHReadByte((byte)(badd + EFUSE_REG_SHIFT), ref bdata);
                    parent.m_YFRegImg[badd].err = ret;
                    parent.m_YFRegImg[badd].val = bdata;
                }

                ret = WorkMode(ElementDefine.COBRA_PENGUINDC_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> YFLASHReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

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

            YFLASHReglist = YFLASHReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            if (YFLASHReglist.Count != 0)
            {
                byte[] atebuf31 = new byte[ElementDefine.YFLASH_MEMORY_SIZE*2 -1];
                byte[] atebuf16 = new byte[ElementDefine.YFLASH_MEMORY_SIZE]; 
                for (int i = 0; i < ElementDefine.YFLASH_MEMORY_SIZE; i++)
                    atebuf16[i] = (byte)parent.m_YFRegImg[i].val;

                ConvertCRCData(ref atebuf31, atebuf16);
                UInt16 calATECRC = crc4_calc(atebuf31, ElementDefine.YFLASH_MEMORY_SIZE*2 -1);
                parent.m_YFRegImg[ElementDefine.YFLASH_MEMORY_SIZE -1].val |= (byte)(calATECRC<<4);   
                
                ret = WorkMode(ElementDefine.COBRA_PENGUINDC_WKM.YFLASH_WORKMODE_50T5F_RW);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                foreach (byte badd in YFLASHReglist)
                {
                    ret1 = YFLASHWriteByte((byte)(badd + EFUSE_REG_SHIFT), (byte)parent.m_YFRegImg[badd].val);
                    parent.m_YFRegImg[badd].err = ret1;
                    ret |= ret1;
                }

                ret = WorkMode(ElementDefine.COBRA_PENGUINDC_WKM.YFLASH_WORKMODE_NORMAL);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            foreach (byte badd in OpReglist)
            {
                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        private void ConvertCRCData(ref byte[] buf, byte[] img)
        {
            byte k, m;
            byte[] bb = new byte[1];
            BitArray mybit = new BitArray(4);
            for (byte i = 0; i < 31; i++)
            {
                byte shiftdigit = (byte)((i % 2) * 4);
                int reg = i / 2;
                buf[i] = (byte)((img[reg] & (0x0f << shiftdigit)) >> shiftdigit);
            }

            for (int z = 0; z < buf.Length; z++)
            {
                k = buf[z];
                for (int i = 0; i < 4; i++)
                {
                    byte tt = (byte)(k & (1 << i));
                    mybit[3 - i] = (tt > 0) ? true : false;
                }
                mybit.CopyTo(bb, 0);
                buf[z] = bb[0];
            }
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
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpReglist = new List<Parameter>();

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
                                if (baddress == EFUSE_VENDOR_ACCESS_DIS_REG)
                                    OpReglist.Add(p);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            if (OpReglist.Count != 1) return LibErrorCode.IDS_ERR_DEM_PARAM_READ_UNABLE;

            ret = WorkMode((ElementDefine.COBRA_PENGUINDC_WKM)OpReglist[0].phydata);
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            int ival = 0;
            string shwversion = String.Empty;
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadByte(0x00, ref bval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (byte)(bval & 0x03);
            ival = (int)((bval & 0x38) >> 4);
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
            shwversion += String.Format("{0:d}", 0);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = 0;

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (type != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }
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