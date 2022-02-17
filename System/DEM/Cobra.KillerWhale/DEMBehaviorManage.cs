using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.KillerWhale
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

        public object m_lock = new object();
        public CCommunicateManager m_Interface = new CCommunicateManager();

        private LanYang m_lanyang_device = new LanYang();
        private SeaElf m_seaelf_device = new SeaElf();

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();

            m_lanyang_device.Init(this);
            m_seaelf_device.Init(this);
        }

        #region 端口操作
        public bool CreateInterface()
        {
            bool bdevice = EnumerateInterface();
            if (!bdevice) return false;

            m_lanyang_device.pecenable = parent.m_busoption.GetOptionsByGuid(BusOptions.I2CPECMODE_GUID).bcheck;
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
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> LanYangOpReglist = new List<Parameter>();
            List<Parameter> SeaElfOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 0:
                        {
                            if (p == null) break;
                            LanYangOpReglist.Add(p);
                            break;
                        }
                    case 1:
                        {
                            if (p == null) break;
                            SeaElfOpReglist.Add(p);
                            break;
                        }
                }
            }

            //Read
            ret = m_lanyang_device.ReadByte(LanYangOpReglist);
            ret1 = m_seaelf_device.ReadByte(SeaElfOpReglist);
            /*
            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL))
                return ret;
            else if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (SeaElfOpReglist.Count == 0))
                return ret;
            else if ((ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) && (LanYangOpReglist.Count == 0))
                return ret1;*/
            return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> LanYangOpReglist = new List<Parameter>();
            List<Parameter> SeaElfOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 0:
                        {
                            if (p == null) break;
                            LanYangOpReglist.Add(p);
                            break;
                        }
                    case 1:
                        {
                            if (p == null) break;
                            SeaElfOpReglist.Add(p);
                            break;
                        }
                }
            }
            //Write
            ret = m_lanyang_device.WriteByte(LanYangOpReglist);
            ret1 = m_seaelf_device.WriteByte(SeaElfOpReglist);
            /*
            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL))
                return ret;
            else if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (SeaElfOpReglist.Count == 0))
                return ret;
            else if ((ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) && (LanYangOpReglist.Count == 0))
                return ret1;*/
            return ret;//LibErrorCode.IDS_ERR_SUCCESSFUL;
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

            List<Parameter> LanYangOpReglist = new List<Parameter>();
            List<Parameter> SeaElfOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 0:
                        {
                            if (p == null) break;
                            LanYangOpReglist.Add(p);
                            break;
                        }
                    case 1:
                        {
                            if (p == null) break;
                            SeaElfOpReglist.Add(p);
                            break;
                        }
                }
            }

            if (LanYangOpReglist.Count != 0)
            {
                for (int i = 0; i < LanYangOpReglist.Count; i++)
                {
                    param = LanYangOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_lanyang_device.Hex2Physical(ref param);
                }
            }

            if (SeaElfOpReglist.Count != 0)
            {
                for (int i = 0; i < SeaElfOpReglist.Count; i++)
                {
                    param = SeaElfOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_seaelf_device.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> LanYangOpReglist = new List<Parameter>();
            List<Parameter> SeaElfOpReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.subsection)
                {
                    case 0:
                        {
                            if (p == null) break;
                            LanYangOpReglist.Add(p);
                            break;
                        }
                    case 1:
                        {
                            if (p == null) break;
                            SeaElfOpReglist.Add(p);
                            break;
                        }
                }
            }
            if (LanYangOpReglist.Count != 0)
            {
                for (int i = 0; i < LanYangOpReglist.Count; i++)
                {
                    param = LanYangOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_lanyang_device.Physical2Hex(ref param);
                }
            }

            if (SeaElfOpReglist.Count != 0)
            {
                for (int i = 0; i < SeaElfOpReglist.Count; i++)
                {
                    param = SeaElfOpReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_seaelf_device.Physical2Hex(ref param);
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
                                if (baddress == 0x1F)
                                    OpReglist.Add(p);
                            }
                            break;
                        }
                    case ElementDefine.TemperatureElement:
                        break;
                }
            }

            if (OpReglist.Count != 1) return LibErrorCode.IDS_ERR_DEM_PARAM_READ_UNABLE;
            ret = WorkMode((ElementDefine.COBRA_KILLERWHALE_WKM)OpReglist[0].phydata);
            return ret;
        }
        #endregion

        protected UInt32 WorkMode(ElementDefine.COBRA_KILLERWHALE_WKM wkm)
        {
            UInt32 ret = m_lanyang_device.WorkMode(wkm);
            return ret;
        }

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
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            msg.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = m_seaelf_device.ReadOneByte(0x40, ref bdata);
            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL)||((bdata & 0x20) == 0))
            {
                msg.sm.parts[0] = true;
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            else msg.sm.parts[0] = false;

            ret = m_seaelf_device.ReadOneByte(0x10,ref bdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                msg.sm.parts[0] = true;
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
            else msg.sm.parts[0] = false;

            if (bdata < 0x06)
            {
                ret = m_seaelf_device.WriteOneByte(0x10, 0x0A);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    msg.sm.parts[0] = true;
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else msg.sm.parts[0] = false;
            }

            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region 外部温度转换
        public double ResistToTemp(double resist)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }
            return SharedFormula.ResistToTemp(resist, m_TempVals, m_ResistVals);
        }

        public double TempToResist(double temp)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }

            return SharedFormula.TempToResist(temp, m_TempVals, m_ResistVals);
        }
        #endregion
    }
}