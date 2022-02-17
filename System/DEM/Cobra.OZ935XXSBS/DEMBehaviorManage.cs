//#define SIMULATION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.BigsurSBS
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

        private BatteryMode batteryMode;
        private Random rd = new Random();
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
        protected UInt32 BlockRead(byte cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
#if SIMULATION
                rd.NextBytes(pval.bdata);
#else
                ret = OnBlockRead(cmd, ref pval);
#endif
            }
            return ret;
        }

        protected UInt32 BlockRead(UInt16 cmd, ref TSMBbuffer pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockRead(cmd, ref pval);
            }
            return ret;
        }

        protected UInt32 BlockWrite(byte cmd, TSMBbuffer val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, val);
            }
            return ret;
        }

        protected UInt32 BlockWrite(UInt16 cmd, TSMBbuffer val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, val);
            }
            return ret;
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
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnBlockRead(byte cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref t, ref DataOutLen, pval.length))
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

        protected UInt32 OnBlockWrite(byte cmd, TSMBbuffer val)
        {
            bool bPEC = true;
            bool bsuc = false;
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)val.length;
            byte[] sendbuf = null;//new byte[DataInLen + 3]; //I2C, CMD,PEC
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            bPEC = parent.m_busoption.GetOptionsByGuid(BusOptions.I2CPECMODE_GUID).SelectLocation.Code > 0 ? true : false;
            if (bPEC)
                sendbuf = new byte[DataInLen + 3]; //I2C, CMD,PEC
            else
                sendbuf = new byte[DataInLen + 2]; //I2C, CMD

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;

            for (int i = 0; i < val.length; i++)
                sendbuf[2 + i] = val.bdata[i];

            if (bPEC)
                sendbuf[val.length + 2] = crc8_calc(ref sendbuf, (UInt16)(DataInLen + 2));

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (bPEC)
                    bsuc = m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 1)); //valid data and pec
                else
                    bsuc = m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen);

                if (bsuc)
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

        protected UInt32 OnBlockRead(UInt16 cmd, ref TSMBbuffer pval)
        {
            var t = pval.bdata;
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
            sendbuf[1] = 0xF9;
            sendbuf[2] = 0xBB;
            sendbuf[3] = SharedFormula.HiByte(cmd);
            sendbuf[4] = SharedFormula.LoByte(cmd);

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)3)) //valid data and pec
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref t, ref DataOutLen, pval.length))
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

        protected UInt32 OnBlockWrite(UInt16 cmd, TSMBbuffer val)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)val.length;
            byte[] sendbuf = new byte[DataInLen + 5]; 
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
            sendbuf[1] = 0xF9;
            sendbuf[2] = 0xAA;
            sendbuf[3] = SharedFormula.HiByte(cmd);
            sendbuf[4] = SharedFormula.LoByte(cmd);

            for (int i = 0; i < val.length; i++)
                sendbuf[5 + i] = val.bdata[i];

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if(m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 3))) //valid data and pec
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
            byte bcmd = 0;
            UInt16 ucmd = 0;
            TSMBbuffer tsmBuffer = null;
            List<byte> SBSReglist = new List<byte>();
            List<UInt16> ConfigReglist = new List<UInt16>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                            SBSReglist.Add(bcmd);

                            break;
                        }
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;

                            ucmd = (UInt16)((p.guid & ElementDefine.Command2Mask) >> 4);
                            ConfigReglist.Add(ucmd);
                            break;
                        }
                }
            }

            SBSReglist = SBSReglist.Distinct().ToList();
            ConfigReglist = ConfigReglist.Distinct().ToList();

            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[badd];
#if SIMULATION
                rd.NextBytes(tsmBuffer.bdata);
#else
                ret = BlockRead(badd, ref tsmBuffer);
#endif
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }

            foreach (UInt16 uadd in ConfigReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(uadd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[uadd];
                ret = BlockRead(uadd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            byte bcmd = 0;
            UInt16 ucmd = 0;
            TSMBbuffer tsmBuffer = null;
            List<byte> SBSReglist = new List<byte>();
            List<UInt16> ConfigReglist = new List<UInt16>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                                SBSReglist.Add(bcmd);
                            }
                            break;
                        }
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;

                            ucmd = (UInt16)((p.guid & ElementDefine.Command2Mask) >> 4);
                            ConfigReglist.Add(ucmd);
                            break;
                        }
                }
            }

            SBSReglist = SBSReglist.Distinct().ToList();
            ConfigReglist = ConfigReglist.Distinct().ToList();
            foreach (byte badd in SBSReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(badd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[badd];
#if SIMULATION
                rd.NextBytes(tsmBuffer.bdata);
#else
                ret = BlockWrite(badd, tsmBuffer);
#endif
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
            }

            foreach (UInt16 uadd in ConfigReglist)
            {
                if (!parent.m_HwMode_Dic.ContainsKey(uadd)) continue;
                tsmBuffer = parent.m_HwMode_Dic[uadd];
                ret = BlockWrite(uadd, tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
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
            List<Parameter> SBSParamList = new List<Parameter>();
            List<Parameter> ConfigReglist = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            SBSParamList.Add(p);
                            break;
                        }
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;
                            ConfigReglist.Add(p);
                            break;
                        }
                }
            }

            if (SBSParamList.Count != 0)
            {
                for (int i = 0; i < SBSParamList.Count; i++)
                {
                    param = (Parameter)SBSParamList[i];
                    if (param == null) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }
            if (ConfigReglist.Count != 0)
            {
                for (int i = 0; i < ConfigReglist.Count; i++)
                {
                    param = (Parameter)ConfigReglist[i];
                    if (param == null) continue;

                    m_parent.Hex2Physical(ref param);
                }
            }

            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            List<Parameter> SBSParamList = new List<Parameter>();
            List<Parameter> ConfigReglist = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.SBSElement:
                        {
                            if (p == null) break;
                            SBSParamList.Add(p);
                            break;
                        }
                    case ElementDefine.CONFIGElement:
                        {
                            if (p == null) break;
                            ConfigReglist.Add(p);
                            break;
                        }
                }
            }

            if (SBSParamList.Count != 0)
            {
                for (int i = 0; i < SBSParamList.Count; i++)
                {
                    param = (Parameter)SBSParamList[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
                }
            } 
            if (ConfigReglist.Count != 0)
            {
                for (int i = 0; i < ConfigReglist.Count; i++)
                {
                    param = (Parameter)ConfigReglist[i];
                    if (param == null) continue;

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
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)  //Bigsur
        {
            int ival = 0;
            byte bcmd = 0;
            TSMBbuffer tsmBuffer = null;
            string shwversion = String.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            bcmd = (byte)((ElementDefine.SpecInfo & ElementDefine.CommandMask) >> 8);
            if (!parent.m_HwMode_Dic.ContainsKey(bcmd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;

            tsmBuffer = parent.m_HwMode_Dic[bcmd];
            ret = BlockRead(bcmd, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)tsmBuffer.bdata[1];
            ival = (int)((tsmBuffer.bdata[0] & 0x70) >> 4);
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
            ival = (int)(tsmBuffer.bdata[0] & 0x07);
            shwversion += String.Format("{0:d}", ival);
            deviceinfor.shwversion = shwversion;
            deviceinfor.hwsubversion = (int)(tsmBuffer.bdata[0] & 0x07);
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            byte bcmd = 0;
            byte cellnumber = 0;
            byte totalCellNumber = 0;
            Parameter param = null;
            bool bTHM0Enabled = false, bTHM1Enabled = false, bTHM2Enabled = false, bTHM3Enabled = false;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if (p == null) continue;
                p.bShow = true;
                p.tsmbBuffer.length = 4;
            }

            param = GetParameterByGuid(ElementDefine.MfgName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            param = GetParameterByGuid(ElementDefine.DevName, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            param = GetParameterByGuid(ElementDefine.DevChem, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;
            param = GetParameterByGuid(ElementDefine.MfgData, demparameterlist.parameterlist);
            param.tsmbBuffer.length = (ushort)ElementDefine.COBRA_DATA_LEN.DATA_LEN_THIRTY_TWO_BYTES;

            #region Read Static Parameter
            param = GetParameterByGuid(ElementDefine.MfgAccess, demparameterlist.parameterlist);
            bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
            TSMBbuffer tsmBuffer = param.tsmbBuffer;
            param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.Hex2Physical(ref param);

            #region BatteryMode
            param = GetParameterByGuid(ElementDefine.BatteryMode, demparameterlist.parameterlist);
            bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
            tsmBuffer = param.tsmbBuffer;
            param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            parent.Hex2Physical(ref param);
#if SIMULATION
            tsmBuffer.bdata[0] = 0xBA;
            cellnumber = (byte)(tsmBuffer.bdata[0] & 0x0F);
            bTHM0Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 4)&0x01) > 0 ? true : false;
            bTHM1Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 5)&0x01) > 0 ? true : false;
            bTHM2Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 6)&0x01) > 0 ? true : false;
            bTHM3Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 7)&0x01) > 0 ? true : false;
#else
            cellnumber = (byte)(tsmBuffer.bdata[0] & 0x0F);
            bTHM0Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 4) & 0x01) > 0 ? true : false;
            bTHM1Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 5) & 0x01) > 0 ? true : false;
            bTHM2Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 6) & 0x01) > 0 ? true : false;
            bTHM3Enabled = (((tsmBuffer.bdata[0] & 0xF0) >> 7) & 0x01) > 0 ? true : false;
#endif
            param = GetParameterByGuid(ElementDefine.CellVoltMV10,demparameterlist.parameterlist);
            if (param == null) totalCellNumber = 6;
            else totalCellNumber = 10;

            for (int n = 0; n < totalCellNumber; n++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.CellVoltMV01 + (n << 8)), demparameterlist.parameterlist);
                if (param == null) continue;
                if ((n < (cellnumber - 1)) || (n == (totalCellNumber -1))) param.bShow = true;
                else param.bShow = false;
            }
            if (!bTHM0Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK1, demparameterlist.parameterlist);
                param.bShow = false;
            }
            if (!bTHM1Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK2, demparameterlist.parameterlist);
                param.bShow = false;
            }
            if (!bTHM2Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK3, demparameterlist.parameterlist);
                param.bShow = false;
            }
            if (!bTHM3Enabled)
            {
                param = GetParameterByGuid(ElementDefine.ETDK4, demparameterlist.parameterlist);
                param.bShow = false;
            }
            #endregion

            //0x18~0x1c
            for (int i = 0; i < 5; i++)
            {
                if (i == 2)
                    param = GetParameterByGuid(ElementDefine.SpecInfo, demparameterlist.parameterlist);
                else
                    param = GetParameterByGuid((UInt32)(ElementDefine.DesignCap + (i << 8)), demparameterlist.parameterlist);
                bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
                tsmBuffer = param.tsmbBuffer;
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }
            //0x20~0x23
            for (int i = 0; i < 4; i++)
            {
                param = GetParameterByGuid((UInt32)(ElementDefine.MfgName + (i << 8)), demparameterlist.parameterlist);
                bcmd = (byte)((param.guid & ElementDefine.CommandMask) >> 8);
                tsmBuffer = param.tsmbBuffer;
                param.errorcode = ret = BlockRead(bcmd, ref tsmBuffer);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                parent.Hex2Physical(ref param);
            }
            #endregion
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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

        public UInt32 ReadRsenseMain()
        {
            UInt16 uadd = 0x101;
            if (!parent.m_HwMode_Dic.ContainsKey(uadd)) return LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
           
            TSMBbuffer tsmBuffer = parent.m_HwMode_Dic[uadd];
            return BlockRead(uadd, ref tsmBuffer);
        }
        #endregion
    }
}