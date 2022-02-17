//#define debug
//#if debug
//#define functiontimeout
//#define pec
//#define frozen
//#define dirty
//#define readback
//#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
using System.IO;

namespace Cobra.KALL08
{
    internal class DEMBehaviorManage
    {
        private byte calATECRC;
        private byte calUSRCRC;
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        UInt16[] EFUSEUSRbuf = new UInt16[ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1];      //Used for read back check

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


        protected UInt32 SetWorkMode(ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSetWorkMode(wkm);
            }
            return ret;
        }


        protected UInt32 GetWorkMode(ref ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnGetWorkMode(ref wkm);
            }
            return ret;
        }

        protected UInt32 SetAllowWrite(bool allow_write)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSetAllowWrite(allow_write);
            }
            return ret;
        }


        protected UInt32 GetAllowWrite(ref bool allow_write)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnGetAllowWrite(ref allow_write);
            }
            return ret;
        }

        protected UInt32 GetCellBalance(ref ElementDefine.CELL_BALANCE cb)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnGetCellBalance(ref cb);
            }
            return ret;
        }

        protected UInt32 SetCellBalance(ElementDefine.CELL_BALANCE cb)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSetCellBalance(cb);
            }
            return ret;
        }

        protected UInt32 GetMappingDisable(ref bool mapping_disable)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnGetMappingDisable(ref mapping_disable);
            }
            return ret;
        }

        protected UInt32 SetMappingDisable(bool mapping_disable)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnSetMappingDisable(mapping_disable);
            }
            return ret;
        }
        protected UInt32 TriggerScanRequest(ushort addr)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnTriggerScanRequest(addr);
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
        protected UInt32 CheckProgramVoltage()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnCheckProgramVoltage();
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
#if debug
            pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if functiontimeout
            ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
#else
            
#if pec
            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
#endif

#endif
            return ret;
#else
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
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
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
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
#endif
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
#if debug
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if functiontimeout
            ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
#else
            
#if pec
            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
#endif

#endif
            return ret;
#else
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
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
#endif
        }

        protected UInt32 OnGetWorkMode(ref ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.WORKMODE_OFFSET,ref buf);
            buf &= 0x0003;
            wkm = (ElementDefine.WORK_MODE)buf;
            return ret;
        }

        protected UInt32 OnSetWorkMode(ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = OnWriteWord(ElementDefine.WORKMODE_OFFSET, (ushort)((ushort)wkm | 0x8000));
            ret = OnWriteWord(ElementDefine.WORKMODE_OFFSET, (ushort)((ushort)wkm | 0x8000));
            return ret;
        }

        protected UInt32 OnGetAllowWrite(ref bool allow_write)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.WORKMODE_OFFSET, ref buf);
            allow_write = (buf & 0x8000) == 0x8000;
            return ret;
        }

        protected UInt32 OnSetAllowWrite(bool allow_write)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.WORKMODE_OFFSET, ref buf);
            if (allow_write)
                buf |= 0x8000;
            else
                buf &= 0x7fff;
            ret = OnWriteWord(ElementDefine.WORKMODE_OFFSET, buf);
            return ret;
        }

        protected UInt32 OnGetCellBalance(ref ElementDefine.CELL_BALANCE cb)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.CB_OFFSET, ref buf);
            buf &= 0x6000;
            buf >>= 13;
            cb = (ElementDefine.CELL_BALANCE)buf;
            return ret;
        }
        protected UInt32 OnSetCellBalance(ElementDefine.CELL_BALANCE cb)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.CB_OFFSET,ref buf);
            buf &= 0x9fff;
            buf |= (ushort)((ushort)cb << 13);
            ret = OnWriteWord(ElementDefine.CB_OFFSET, buf);
            return ret;
        }

        protected UInt32 OnGetMappingDisable(ref bool mapping_disable)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.MAPPINGDISABLE_OFFSET, ref buf);
            mapping_disable = (buf & 0x0200) == 0x0200;
            return ret;
        }

        protected UInt32 OnSetMappingDisable(bool mapping_disable)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = OnReadWord(ElementDefine.MAPPINGDISABLE_OFFSET, ref buf);
            if (mapping_disable)
                buf |= 0x0200;
            else
                buf &= 0xfdff;
            ret = OnWriteWord(ElementDefine.MAPPINGDISABLE_OFFSET, buf);
            return ret;
        }

        private UInt32 SafeRead(ushort addr, ref ushort curr)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte pec_error = 0;
            for (; pec_error < 4; pec_error++)
            {
                ret = OnReadWord((byte)addr, ref curr);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    if (ret == LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR)
                    {
                        if (pec_error != 3)
                            Thread.Sleep(15);
                        continue;
                    }
                    else
                    {
                        return ret;
                    }
                }
                return ret;
            }
            return ret;
        }

        protected UInt32 OnTriggerScanRequest(ushort addr)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0, curr1 = 0;
            //byte pec_error = 0;
            ret = OnReadWord(0x45, ref buf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            buf &= 0xfff5;
            buf |= 0x0001;
            ret = OnWriteWord(0x45, buf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;


            ret = OnReadWord(0x40, ref buf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            buf |= 0x8010;
            ret = OnWriteWord(0x40, buf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            Thread.Sleep(2);


            int i = 0;
            for (; i < ElementDefine.RETRY_COUNTER; i++)
            {
                ret = OnReadWord(0x45, ref buf);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;

                if ((buf & 0x0001) == 0x0001)
                {

                    ret = SafeRead(addr, ref curr1);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = OnReadWord(0x45, ref buf);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    buf &= 0xfff5;
                    buf |= 0x0001;
                    ret = OnWriteWord(0x45, buf);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    parent.m_OpRegImg[addr].err = ret;
                    parent.m_OpRegImg[addr].val = curr1;
                    return ret;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            if (i >= ElementDefine.RETRY_COUNTER)
                return LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
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

        private UInt32 OnCheckProgramVoltage()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort wdata = 0;
            ret = ReadWord((byte)ElementDefine.VDD_OFFSET, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            double pv = wdata * 2.5;
            if (pv < 7100 || pv > 7600)
                ret = ElementDefine.IDS_ERR_DEM_POWERCHECK_FAILED;
            return ret;
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

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.SectionMask)
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EFUSEReglist = EFUSEReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();
            //Read 
            if (EFUSEReglist.Count != 0)
            {
                List<byte> EFATEList = new List<byte>();
                List<byte> EFUSRList = new List<byte>();
                foreach (byte addr in EFUSEReglist)
                {
                    if (addr <= ElementDefine.EF_ATE_TOP && addr >= ElementDefine.EF_MEMORY_OFFSET)
                        EFATEList.Add(addr);
                    else if (addr <= ElementDefine.EF_USR_TOP && addr >= ElementDefine.EF_USR_OFFSET)
                        EFUSRList.Add(addr);
                }
                if (EFATEList.Count != 0)
                {
                    ret = CheckATECRC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }

                if (EFUSRList.Count != 0)
                {
                    ret = CheckUSRCRC();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }
                /*foreach (byte badd in EFUSEReglist)
                {
                    ret = EFUSEReadWord(badd, ref wdata);
                    parent.m_EFRegImg[badd].err = ret;
                    parent.m_EFRegImg[badd].val = wdata;
                }

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/
            }
            //bug 15978
            if (msg.gm.sflname == "Scan" || msg.gm.sflname == "Trim")  //如果是Scan/Trim SFL
            {
                bool containCell = false;
                bool allow_write = false;
                bool mapping_disable = false;
                ElementDefine.WORK_MODE wkm = ElementDefine.WORK_MODE.NORMAL;
                ElementDefine.CELL_BALANCE cb = ElementDefine.CELL_BALANCE.DISABLE;
                foreach (byte badd in OpReglist)        //check if there is any Cell parameters
                {
                    if (badd >= ElementDefine.CELL_OFFSET && badd <= ElementDefine.CELL_TOP)
                    {
                        containCell = true;
                        break;
                    }
                }

                if (containCell)  //Disable Cell Ballance
                {
                    ret = GetAllowWrite(ref allow_write);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = GetWorkMode(ref wkm);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = GetCellBalance(ref cb);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = GetMappingDisable(ref mapping_disable);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = SetWorkMode(ElementDefine.WORK_MODE.INTERNAL);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = SetCellBalance(ElementDefine.CELL_BALANCE.DISABLE);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Thread.Sleep(10);
                }

                foreach (byte badd in OpReglist)
                {
                    if (badd == ElementDefine.MCURRENT_OFFSET || badd == ElementDefine.SCURRENT_OFFSET)
                    {
                        ret = TriggerScanRequest(badd);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                    }
                    else
                    {
                        ret = ReadWord(badd, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        parent.m_OpRegImg[badd].err = ret;
                        parent.m_OpRegImg[badd].val = wdata;
                    }
                }
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;

                if (containCell)
                {
                    ret = SetMappingDisable(mapping_disable);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = SetCellBalance(cb);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = SetWorkMode(wkm);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = SetAllowWrite(allow_write);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }
                return ret;
            }

            else
            {
                foreach (byte badd in OpReglist)
                {
                    ret = ReadWord(badd, ref wdata);
                    parent.m_OpRegImg[badd].err = ret;
                    parent.m_OpRegImg[badd].val = wdata;
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }
                return ret;
            }/*
            bool containCell = false;
            foreach (byte badd in OpReglist)        //check if there is any Cell parameters
            {
                if (badd >= ElementDefine.CELL_OFFSET && badd <= ElementDefine.CELL_TOP)
                {
                    containCell = true;
                    break;
                }
            }
            ElementDefine.CELL_BALANCE cb = ElementDefine.CELL_BALANCE.DISABLE;
            if (containCell)  //Disable Cell Ballance
            {
                if ((msg.gm.sflname == "Scan" || msg.gm.sflname == "Trim"))  //如果是Scan/Trim SFL要读电流
                {
                    ret = SetWorkMode(ElementDefine.WORK_MODE.INTERNAL);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = GetCellBalance(ref cb);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = SetCellBalance(ElementDefine.CELL_BALANCE.DISABLE);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Thread.Sleep(10);
                }
            }

            foreach (byte badd in OpReglist)
            {
                if ((msg.gm.sflname == "Scan" || msg.gm.sflname == "Trim"))  //如果是Scan/Trim SFL要读电流
                {
                    if (badd == ElementDefine.CURRENT_OFFSET)
                    {
                        //ret = ReadWord((byte)ElementDefine.V800MV_OFFSET, ref wdata);              //先读v800mv，bug 16010
                        ret = TriggerScanRequest();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                    }
                }

                ret = ReadWord(badd, ref wdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
            }
            if (containCell)
            {
                if ((msg.gm.sflname == "Scan" || msg.gm.sflname == "Trim"))  //如果是Scan/Trim SFL要读电流
                {
                    ret = SetCellBalance(cb);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                }
            }*/
        }

        private bool isATEFRZ()
        {
                return (parent.m_EFRegImgEX[ElementDefine.EF_ATE_TOP - ElementDefine.EF_MEMORY_OFFSET].val & 0x8000) == 0x8000;
        }

        private bool isUSRFRZ()
        {
                return (parent.m_EFRegImgEX[ElementDefine.EF_USR_TOP - ElementDefine.EF_MEMORY_OFFSET].val & 0x8000) == 0x8000;
        }


        private UInt32 CheckATECRC()
        {
            //UInt16 len = 8;
            //byte tmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte[] atebuf = new byte[ElementDefine.ATE_CRC_BUF_LEN];

            ret = ReadATECRCRefReg();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (!isATEFRZ())
                return LibErrorCode.IDS_ERR_SUCCESSFUL;

            GetATECRCRef(ref atebuf);
            calATECRC = CalEFUSECRC(atebuf, ElementDefine.ATE_CRC_BUF_LEN);

            byte readATECRC = 0;
            ret = ReadATECRC(ref readATECRC);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (readATECRC == calATECRC)
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            else
            {
                parent.m_EFRegImgEX[ElementDefine.EF_ATE_TOP - ElementDefine.EF_MEMORY_OFFSET].err = LibErrorCode.IDS_ERR_DEM_ATE_CRC_ERROR;
                return LibErrorCode.IDS_ERR_DEM_ATE_CRC_ERROR;
            }
        }

        private UInt32 ReadATECRCRefReg()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte i = (byte)ElementDefine.EF_ATE_OFFSET; i <= (byte)ElementDefine.EF_ATE_TOP; i++)
            {
                ushort wdata = 0;
                parent.m_EFRegImg[i].err = ReadWord(i, ref wdata);
                parent.m_EFRegImg[i].val = wdata;
                ret |= parent.m_EFRegImg[i].err;
            }
            return ret;
        }
        private void GetATECRCRef(ref byte[] buf)
        {
            for (byte i = 0; i < 20; i++)
            {
                byte shiftdigit = (byte)((i % 4) * 4);
                shiftdigit = (byte)(12 - shiftdigit);
                int reg = i / 4;
                buf[i] = (byte)((parent.m_EFRegImgEX[reg].val & (0x0f << shiftdigit)) >> shiftdigit);
            }
            buf[20] = (byte)((parent.m_EFRegImgEX[5].val & (0x0f << 12)) >> 12);
            buf[21] = (byte)((parent.m_EFRegImgEX[5].val & (0x0f << 8)) >> 8);
            buf[22] = (byte)((parent.m_EFRegImgEX[5].val & (0x0f << 4)) >> 4);
        }
        private UInt32 ReadATECRC(ref byte crc)
        {
            ushort wdata = 0;

            parent.m_EFRegImg[ElementDefine.ATE_CRC_OFFSET].err = ReadWord((byte)ElementDefine.ATE_CRC_OFFSET, ref wdata);
            if (parent.m_EFRegImg[ElementDefine.ATE_CRC_OFFSET].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_EFRegImg[ElementDefine.ATE_CRC_OFFSET].err;
            parent.m_EFRegImg[ElementDefine.ATE_CRC_OFFSET].val = wdata;
            crc = (byte)(wdata & 0x000f);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 CheckUSRCRC()
        {
            //UInt16 len = 8;
            //byte tmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadUSRCRCRefReg();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (!isUSRFRZ())
                return LibErrorCode.IDS_ERR_SUCCESSFUL;

            byte[] usrbuf = new byte[ElementDefine.USR_CRC_BUF_LEN];
            GetUSRCRCRef(ref usrbuf);
            calUSRCRC = CalEFUSECRC(usrbuf, ElementDefine.USR_CRC_BUF_LEN);
            byte readUSRCRC = 0;
            ret = ReadUSRCRC(ref readUSRCRC);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            if (calUSRCRC == readUSRCRC)
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            else
            {
                parent.m_EFRegImgEX[0x0f].err = LibErrorCode.IDS_ERR_DEM_ATE_CRC_ERROR;
                return LibErrorCode.IDS_ERR_DEM_ATE_CRC_ERROR;
            }
        }




        private UInt32 ReadUSRCRCRefReg()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (byte i = (byte)ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                ushort wdata = 0;
                parent.m_EFRegImg[i].err = ReadWord(i, ref wdata);
                parent.m_EFRegImg[i].val = wdata;
                ret |= parent.m_EFRegImg[i].err;
            }
            return ret;
        }
        private void GetUSRCRCRef(ref byte[] buf)
        {
            for (byte i = 0; i < 36; i++)
            {
                byte shiftdigit = (byte)((i % 4) * 4);
                shiftdigit = (byte)(12 - shiftdigit);
                int reg = (i / 4) + 6;
                buf[i] = (byte)((parent.m_EFRegImgEX[reg].val & (0x0f << shiftdigit)) >> shiftdigit);
            }
            buf[36] = (byte)((parent.m_EFRegImgEX[0x0f].val & (0x0f << 12)) >> 12);
            buf[37] = (byte)((parent.m_EFRegImgEX[0x0f].val & (0x0f << 8)) >> 8);
            buf[38] = (byte)((parent.m_EFRegImgEX[0x0f].val & (0x0f << 4)) >> 4);
        }
        private UInt32 ReadUSRCRC(ref byte crc)
        {
            ushort wdata = 0;

            parent.m_EFRegImg[ElementDefine.USR_CRC_OFFSET].err = ReadWord((byte)ElementDefine.USR_CRC_OFFSET, ref wdata);
            if (parent.m_EFRegImg[ElementDefine.USR_CRC_OFFSET].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_EFRegImg[ElementDefine.USR_CRC_OFFSET].err;
            parent.m_EFRegImg[ElementDefine.USR_CRC_OFFSET].val = wdata;
            crc = (byte)(wdata & 0x000f);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private byte CalEFUSECRC(byte[] buf, UInt16 len)
        {
            return crc4_calc(buf, len);
        }

        /*
        private byte crc4_calc(byte[] pdata, int len)
        {

            byte crc = 0;
            byte crcdata;
            //byte poly = 0x07;             // poly
            //uint p = (uint)poly + 0x100;
            int n, j;                                      // the length of the data


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
        */

        private byte crc4_calc(byte[] pdata, int len)
        {

            byte crc = 0;
            byte crcdata;
            byte poly = 0x03;             // poly
            int n, j;                                      // the length of the data

            for (n = 0; n < len; n++)
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
            return crc;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt16 pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> EFUSEReglist = new List<byte>();
            List<byte> EFUSEATEReglist = new List<byte>();
            UInt16[] EFUSEATEbuf = new UInt16[6];
            List<byte> EFUSEUSRReglist = new List<byte>();
            UInt16[] EFUSEUSRbuf = new UInt16[10];
            List<byte> OpReglist = new List<byte>();
            UInt16[] pdata = new UInt16[6];

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.SectionMask)
                {
                    case ElementDefine.EFUSEElement:
                        {
                            if (p == null) break;
                            if ((p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_READ_WRITE_UNABLE) || (p.errorcode == LibErrorCode.IDS_ERR_DEM_PARAM_WRITE_UNABLE)) continue;
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
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            EFUSEReglist = EFUSEReglist.Distinct().ToList();
            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            if (EFUSEReglist.Count != 0)
            {

                msg.gm.message = "Efuse can only be written once! Continue?";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                foreach (byte addr in EFUSEReglist)
                {
                    if (addr <= ElementDefine.EF_ATE_TOP)
                        EFUSEATEReglist.Add(addr);
                    else
                        EFUSEUSRReglist.Add(addr);
                }

                if (EFUSEATEReglist.Count > 0)  //Y版本
                {
                    OnReadWord((byte)ElementDefine.EF_ATE_TOP, ref pval);
                    if ((pval & 0x8000) == 0x8000)
                    {
                        return LibErrorCode.IDS_ERR_DEM_FROZEN;
                    }
                    parent.m_EFRegImg[ElementDefine.EF_ATE_TOP].val |= 0x8000;    //Set Frozen bit in image
                }

                OnReadWord((byte)ElementDefine.EF_USR_TOP, ref pval);
                if ((pval & 0x8000) == 0x8000)
                {
                    return LibErrorCode.IDS_ERR_DEM_FROZEN;
                }
                parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val |= 0x8000;    //Set Frozen bit in image

                msg.gm.message = "Please change to program voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                //while (IsEfuseBusy()) ;

                SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);


                if (EFUSEATEReglist.Count > 0)  //Y版本
                {
                    byte[] atebuf = new byte[ElementDefine.ATE_CRC_BUF_LEN];
                    GetATECRCRef(ref atebuf);
                    parent.m_EFRegImg[ElementDefine.EF_ATE_TOP].val &= 0xfff0;
                    parent.m_EFRegImg[ElementDefine.EF_ATE_TOP].val |= CalEFUSECRC(atebuf, ElementDefine.ATE_CRC_BUF_LEN);

                    foreach (byte badd in EFUSEATEReglist)
                    {
                        ret1 = parent.m_EFRegImg[badd].err;
                        ret |= ret1;
                        if (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;

                        EFUSEATEbuf[badd - ElementDefine.EF_ATE_OFFSET] = parent.m_EFRegImg[badd].val;
                        ret1 = OnWriteWord(badd, parent.m_EFRegImg[badd].val);
                        parent.m_EFRegImg[badd].err = ret1;
                        ret |= ret1;
                    }
                }

                byte[] usrbuf = new byte[ElementDefine.USR_CRC_BUF_LEN];
                GetUSRCRCRef(ref usrbuf);
                parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val &= 0xfff0;
                parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val |= CalEFUSECRC(usrbuf, ElementDefine.USR_CRC_BUF_LEN);

                foreach (byte badd in EFUSEUSRReglist)
                {
                    ret1 = parent.m_EFRegImg[badd].err;
                    ret |= ret1;
                    if (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;

                    EFUSEUSRbuf[badd - ElementDefine.EF_USR_OFFSET] = parent.m_EFRegImg[badd].val;
                    ret1 = OnWriteWord(badd, parent.m_EFRegImg[badd].val);
                    parent.m_EFRegImg[badd].err = ret1;
                    ret |= ret1;
                }

                msg.gm.message = "Please change to normal voltage, then continue!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                foreach (byte badd in EFUSEATEReglist)
                {
                    //EFUSEATEbuf[badd - ElementDefine.EF_MEMORY_OFFSET] = (byte)parent.m_EFRegImg[badd - ElementDefine.EF_MEMORY_OFFSET].val;
                    ret1 = OnReadWord(badd, ref pval);
                    if (pval != EFUSEATEbuf[badd - ElementDefine.EF_ATE_OFFSET])
                        return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                }

                foreach (byte badd in EFUSEUSRReglist)
                {
                    //EFUSEATEbuf[badd - ElementDefine.EF_MEMORY_OFFSET] = (byte)parent.m_EFRegImg[badd - ElementDefine.EF_MEMORY_OFFSET].val;
                    ret1 = OnReadWord(badd, ref pval);
                    if (pval != EFUSEUSRbuf[badd - ElementDefine.EF_USR_OFFSET])
                        return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                }

                SetWorkMode(ElementDefine.WORK_MODE.NORMAL);

            }
            //ushort efuse_mode = 0;
            bool mapping_disable = false;
            if (msg.gm.sflname == "Register Config")
            {
                /*ReadWord(0x50, ref efuse_mode);
                efuse_mode &= 0xfffc;
                efuse_mode |= 0x8001;
                WriteWord(0x50, efuse_mode);*/
                SetWorkMode(ElementDefine.WORK_MODE.INTERNAL);
                GetMappingDisable(ref mapping_disable);
                SetMappingDisable(true);
            }
            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            /*if (msg.gm.sflname == "Register Config")
            {
                ReadWord(0x1e, ref efuse_mode);
                efuse_mode &= 0xfffc;
                WriteWord(0x1e, efuse_mode);
            }*/

            return ret;
        }

        public UInt32 BitOperation(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.SectionMask)
                {
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;

                                parent.m_OpRegImg[baddress].val = 0x00;
                                parent.WriteToRegImg(p, 1);
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
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
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
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.SectionMask)
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
                    if ((param.guid & ElementDefine.SectionMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.SectionMask) == ElementDefine.TemperatureElement) continue;

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

            List<Parameter> virtualparamlist = new List<Parameter>();

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                {
                    virtualparamlist.Add(p);
                    continue;
                }
                switch (p.guid & ElementDefine.SectionMask)
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
                    if ((param.guid & ElementDefine.SectionMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            if (OpParamList.Count != 0)
            {
                for (int i = 0; i < OpParamList.Count; i++)
                {
                    param = (Parameter)OpParamList[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.SectionMask) == ElementDefine.TemperatureElement) continue;

                    m_parent.Physical2Hex(ref param);
                }
            }

            return ret;
        }

        private uint ReadAvrage(ref TASKMessage msg)
        {
            uint errorcode = 0;
            List<double[]> llt = new List<double[]>();
            List<double> avr = new List<double>();
            foreach (Parameter param in msg.task_parameterlist.parameterlist)
            {
                llt.Add(new double[5]);
                avr.Add(0);
            }
            for (int i = 0; i < 5; i++)
            {
                errorcode = Read(ref msg);
                if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return errorcode;
                }
                errorcode = ConvertHexToPhysical(ref msg);
                if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return errorcode;
                }
                foreach (Parameter param in msg.task_parameterlist.parameterlist)   //将电流参数的物理值从电流改为电压
                {
                    if (param.guid == ElementDefine.MCurrent)
                        param.phydata = -param.phydata * parent.etrxM / 1000.0F;
                    else if (param.guid == ElementDefine.SCurrent)
                        param.phydata = -param.phydata * parent.etrxS / 1000.0F;
                }
                for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
                {
                    llt[j][i] = msg.task_parameterlist.parameterlist[j].phydata;
                    avr[j] += llt[j][i];
                }
                Thread.Sleep(100);
            }

            for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
            {
                //llt[j][i] = msg.task_parameterlist.parameterlist[j].phydata;
                avr[j] /= 5;
                int minIndex = 0;
                double err = 999;
                for (int i = 0; i < 5; i++)
                {
                    if (err > Math.Abs(llt[j][i] - avr[j]))
                    {
                        err = Math.Abs(llt[j][i] - avr[j]);
                        minIndex = i;
                    }
                }
                msg.task_parameterlist.parameterlist[j].phydata = llt[j][minIndex];
            }
            return errorcode;
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            Parameter param = null;
            ParamContainer demparameterlist = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                #region Slop Trim
                case ElementDefine.COMMAND.TESTCTRL_SLOP_TRIM:
                    {
                        demparameterlist = msg.task_parameterlist;
                        if (demparameterlist == null) return ret;


                        Parameter CellNum = parent.m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement).GetParameterByGuid(ElementDefine.CellNum);
                        TASKMessage tmp_msg = new TASKMessage();
                        ParamContainer pc = new ParamContainer();
                        pc.parameterlist.Add(CellNum);
                        tmp_msg.task_parameterlist = pc;
                        Read(ref tmp_msg);
                        ConvertHexToPhysical(ref tmp_msg);


                        ushort buf = 0;
                        //Write(AllowWR, 1);
                        ReadWord(0x50, ref buf);
                        WriteWord(0x50, (ushort)(buf | 0x0001));


                        //Write(EfuseMode, 0x01);
                        ReadWord(0x50, ref buf);
                        buf &= 0xfffc;
                        buf |= 0x0001;
                        WriteWord(0x50, buf);
                        //Write(TestCtrl, 5, Mapping, 1);
                        ReadWord(0x51, ref buf);
                        buf &= 0xfff0;
                        buf |= 0x0205;
                        WriteWord(0x51, buf);
                        //Write(Cell02Offset, 0, GroupOffset, 0);
                        ReadWord(0x75, ref buf);
                        buf &= 0xc00f;
                        WriteWord(0x75, buf);


                        for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                        {
                            param = demparameterlist.parameterlist[i];
                            param.sphydata = String.Empty;
                        }

                        for (ushort code = 0; code < 16; code++)
                        {
                            //Write Slope Trim
                            WriteWord(0x72, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                            WriteWord(0x73, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                            //ushort buf = 0;
                            ReadWord(0x74, ref buf);
                            buf &= 0xff0f;
                            WriteWord(0x74, (ushort)((code << 4) | buf));

                            ret = ReadAvrage(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                            {
                                param = demparameterlist.parameterlist[i];
                                param.sphydata += param.phydata.ToString() + ",";
                            }
                        }
                        break;
                    }
                #endregion

                case ElementDefine.COMMAND.FROZEN_BIT_CHECK:
                    ret = FrozenBitCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;

                case ElementDefine.COMMAND.DIRTY_CHIP_CHECK:
                    ret = DirtyChipCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;

                case ElementDefine.COMMAND.DOWNLOAD_WITH_POWER_CONTROL:
                    {
                        ret = Download(msg.sm.efusebindata, true);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
#if debug
                        Thread.Sleep(1000);
#endif
                        break;
                    }

                case ElementDefine.COMMAND.DOWNLOAD_WITHOUT_POWER_CONTROL:
                    {
                        ret = Download(msg.sm.efusebindata, false);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
#if debug
                        Thread.Sleep(1000);
#endif
                        break;
                    }
                case ElementDefine.COMMAND.READ_BACK_CHECK:
                    {
                        ret = ReadBackCheck();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
                case ElementDefine.COMMAND.ATE_CRC_CHECK:
                    {
                        ret = CheckATECRC();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
                case ElementDefine.COMMAND.SAVE_EFUSE_HEX:
                    {
                        InitEfuseData();
                        ret = ConvertPhysicalToHex(ref msg);
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
                case ElementDefine.COMMAND.BIN_FILE_CHECK:
                    {
                        string binFileName = msg.sub_task_json;

                        var blist = SharedAPI.LoadBinFileToList(binFileName);
                        if (blist.Count == 0)
                            ret = LibErrorCode.IDS_ERR_DEM_LOAD_BIN_FILE_ERROR;
                        else
                            ret = CheckBinData(blist);
                        break;
                    }
                case ElementDefine.COMMAND.GET_MAX_VALUE:
                    {
                        param = msg.task_parameterlist.parameterlist[0];
                        //double originalvalue = param.phydata;
                        double maxvalue = 0;
                        for (long i = param.dbHexMin; i <= param.dbHexMax; i++)
                        {
                            parent.WriteToRegImg(param, (ushort)i);
                            parent.Hex2Physical(ref param);
                            if (maxvalue < param.phydata)
                                maxvalue = param.phydata;
                        }
                        param.dbPhyMax = maxvalue;
                        //param.phydata = originalvalue;
                        break;
                    }
                case ElementDefine.COMMAND.GET_MIN_VALUE:
                    {
                        param = msg.task_parameterlist.parameterlist[0];
                        //double originalvalue = param.phydata;
                        double minvalue = 99999;
                        for (long i = param.dbHexMin; i <= param.dbHexMax; i++)
                        {
                            parent.WriteToRegImg(param, (ushort)i);
                            parent.Hex2Physical(ref param);
                            if (minvalue > param.phydata)
                                minvalue = param.phydata;
                        }
                        param.dbPhyMin = minvalue;
                        //param.phydata = originalvalue;
                        break;
                    }
                case ElementDefine.COMMAND.VERIFICATION:    //先从文件中读出数据到EFUSEBuffer,然后读回检查
                    {
                        Dictionary<string, ushort> EFRegImg = LoadEFRegImgFromEFUSEBin(msg.sm.efusebindata);

                        WriteToEFUSEBuffer(EFRegImg);

                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = ReadBackCheck();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
            }
            return ret;
        }


        public uint CheckBinData(List<byte> blist)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            int length = (ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1);
            length *= 3;    //一个字节地址，两个字节数值
            if (blist.Count != length)
            {
                ret = LibErrorCode.IDS_ERR_DEM_BIN_LENGTH_ERROR;
            }
            else
            {
                for (int i = ElementDefine.EF_USR_OFFSET, j = 0; i <= ElementDefine.EF_USR_TOP; i++, j++)
                {
                    if (blist[j * 3] != i)
                    {
                        ret = LibErrorCode.IDS_ERR_DEM_BIN_ADDRESS_ERROR;
                        break;
                    }
                }
            }
            return ret;
        }
        private UInt32 FrozenBitCheck() //注意，这里没有把image里的Frozen bit置为1，记得在后面的流程中做这件事
        {
#if frozen
            return LibErrorCode.IDS_ERR_DEM_FROZEN;
#else
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort pval = 0;
            ret = OnReadWord((byte)ElementDefine.EF_USR_TOP, ref pval);
            if ((pval & 0x8000) == 0x8000)
            {
                return LibErrorCode.IDS_ERR_DEM_FROZEN;
            }

            return ret;
#endif
        }

        private UInt32 DirtyChipCheck()
        {
#if dirty
            return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
#else
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort pval = 0;
            for (byte index = (byte)ElementDefine.EF_USR_OFFSET; index <= (byte)ElementDefine.EF_USR_TOP; index++)
            {
                ret = OnReadWord(index, ref pval);
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
#endif
        }

        private void InitEfuseData()
        {
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                parent.m_EFRegImg[i].err = 0;
                parent.m_EFRegImg[i].val = 0;
            }
        }

        private void PrepareHexData()
        {
            parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val |= 0x8000;    //Set Frozen bit in image

            byte[] usrbuf = new byte[ElementDefine.USR_CRC_BUF_LEN];
            GetUSRCRCRef(ref usrbuf);
            parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val &= 0xfff0;
            parent.m_EFRegImg[ElementDefine.EF_USR_TOP].val |= CalEFUSECRC(usrbuf, ElementDefine.USR_CRC_BUF_LEN);

        }

        private UInt32 Download(List<byte> efusebindata, bool isWithPowerControl)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                return ret;
            }


            if (isWithPowerControl)
            {
                ret = PowerOn();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            ret = CheckProgramVoltage();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            Dictionary<string, ushort> EFRegImg = LoadEFRegImgFromEFUSEBin(efusebindata);

            WriteToEFUSEBuffer(EFRegImg);

            //for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                ret = OnWriteWord(badd, EFUSEUSRbuf[badd - (byte)ElementDefine.EF_USR_OFFSET]);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
            }

            if (isWithPowerControl)
            {
                ret = PowerOff();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                return ret;
            }

            return ret;
        }

        private Dictionary<string, ushort> LoadEFRegImgFromEFUSEBin(List<byte> efusebindata)
        {
            Dictionary<string, ushort> output = new Dictionary<string, ushort>();
            for (int i = 0; i < (ElementDefine.EF_USR_TOP - ElementDefine.EF_USR_OFFSET + 1); i++)
            {
                output.Add(efusebindata[i * 3].ToString("X2"), SharedFormula.MAKEWORD(efusebindata[i * 3 + 2], efusebindata[i * 3 + 1]));
            }
            return output;
        }

        private void WriteToEFUSEBuffer(Dictionary<string, ushort> EFRegImg)
        {
            foreach (var key in EFRegImg.Keys)
            {
                byte badd = Convert.ToByte(key, 16);
                EFUSEUSRbuf[badd - (byte)ElementDefine.EF_USR_OFFSET] = EFRegImg[key];
            }
        }

        private UInt32 ReadBackCheck()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if readback
            return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
#else
            ushort pval = 0;
            for (byte badd = (byte)ElementDefine.EF_USR_OFFSET; badd <= (byte)ElementDefine.EF_USR_TOP; badd++)
            {
                ret = ReadWord(badd, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                if (pval != EFUSEUSRbuf[badd - ElementDefine.EF_USR_OFFSET])
                {
                    return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                }
            }
            return ret;
#endif
        }

        private UInt32 GetEfuseHexData(ref TASKMessage msg)
        {
            string tmp = "";
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_EFRegImg[i].err;
                tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_EFRegImg[i].val.ToString("X4") + "\r\n";
            }
            msg.sm.efusehexdata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 GetEfuseBinData(ref TASKMessage msg)
        {
            List<byte> tmp = new List<byte>();
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                if (parent.m_EFRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_EFRegImg[i].err;
                //tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_EFRegImg[i].val.ToString("X4") + "\r\n";
                tmp.Add((byte)i);
                byte hi = 0, low = 0;
                hi = (byte)((parent.m_EFRegImg[i].val) >> 8);
                low = (byte)(parent.m_EFRegImg[i].val);
                tmp.Add(hi);
                tmp.Add(low);
            }
            msg.sm.efusebindata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 EpBlockRead()
        {
            ushort wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = ReadWord(0x40, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = WriteWord(0x40, (ushort)(wdata | 0x8080));
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            for (int i = 0; i < 5; i++)
            {
                ret = ReadWord(0x40, ref wdata);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                if ((wdata & 0x8000) == 0x8000)
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                Thread.Sleep(2);
            }
            return LibErrorCode.IDS_ERR_DEM_MAPPING_TIMEOUT;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#else
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = wval;

            foreach (UInt16 type in deviceinfor.pretype)
            {
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                if (type != deviceinfor.type)
                    ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
            }

            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                LibErrorCode.UpdateDynamicalErrorDescription(ret, new string[] { deviceinfor.shwversion });

            return ret;
#endif
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            msg.sm.dic.Clear();
            UInt32 cellnum = (UInt32)parent.CellNum.phydata + 5;    //“00” ~ “11” corresponds to 5 ~ 8 cells.

            if (cellnum == ElementDefine.CELL_NUMBER)
            {
                for (byte i = 0; i < ElementDefine.CELL_NUMBER; i++)
                    msg.sm.dic.Add((uint)(i), true);
            }
            else
            {
                for (byte i = 0; i < ElementDefine.CELL_NUMBER; i++)
                {
                    if (i < cellnum - 1)
                        msg.sm.dic.Add((uint)i, true);
                    else if (i == cellnum - 1)
                        msg.sm.dic.Add(ElementDefine.CELL_NUMBER - 1, false);
                    else if (i < (ElementDefine.CELL_NUMBER -1))
                        msg.sm.dic.Add((uint)i, false);
                    else if (i == (ElementDefine.CELL_NUMBER - 1))
                        msg.sm.dic.Add(cellnum - 1, true);
                }
            }

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