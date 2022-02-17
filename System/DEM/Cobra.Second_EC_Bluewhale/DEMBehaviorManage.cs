using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.Second_EC_Bluewhale
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

        private OZ1C115 m_oz1c115_device = new OZ1C115();
        private MCU m_mcu_device = new MCU();

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();

            m_oz1c115_device.Init(this);
            m_mcu_device.Init(this);
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

        #region 基础服务功能设计
        public UInt32 Read(ref TASKMessage msg)
        {
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OZ1C115Reglist = new List<Parameter>();
            List<Parameter> MCUReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                        {
                            if (p == null) break;
                            OZ1C115Reglist.Add(p);
                            break;
                        }
                    case ElementDefine.COBRA_SUBTYPE.MCU_SUBTYPE:
                        {
                            if (p == null) break;
                            MCUReglist.Add(p);
                            break;
                        }
                }
            }

            //Read
            ret = m_oz1c115_device.Read(OZ1C115Reglist);  
            ret1 = m_mcu_device.Read(MCUReglist);

            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) || (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL))
            {
                ret |= ret1;
                return ret;
            }
            else if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (OZ1C115Reglist.Count == 0))
                return ret;
            else if ((ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) && (MCUReglist.Count == 0))
                return ret1;
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OZ1C115Reglist = new List<Parameter>();
            List<Parameter> MCUReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                        {
                            if (p == null) break;
                            OZ1C115Reglist.Add(p);
                            break;
                        }
                    case ElementDefine.COBRA_SUBTYPE.MCU_SUBTYPE:
                        {
                            if (p == null) break;
                            MCUReglist.Add(p);
                            break;
                        }
                }
            }
            //Write
            ret = m_oz1c115_device.Write(OZ1C115Reglist);
            ret1 = m_mcu_device.Write(MCUReglist);

            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) || (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL))
            {
                ret |= ret1;
                return ret;
            }
            else if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (OZ1C115Reglist.Count == 0))
                return ret;
            else if ((ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) && (MCUReglist.Count == 0))
                return ret1;
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

            List<Parameter> OZ1C115Reglist = new List<Parameter>();
            List<Parameter> MCUReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                        {
                            if (p == null) break;
                            OZ1C115Reglist.Add(p);
                            break;
                        }
                    case ElementDefine.COBRA_SUBTYPE.MCU_SUBTYPE:
                        {
                            if (p == null) break;
                            MCUReglist.Add(p);
                            break;
                        }
                }
            }

            if (OZ1C115Reglist.Count != 0)
            {
                for (int i = 0; i < OZ1C115Reglist.Count; i++)
                {
                    param = OZ1C115Reglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_oz1c115_device.Hex2Physical(ref param);
                }
            }

            if (MCUReglist.Count != 0)
            {
                for (int i = 0; i < MCUReglist.Count; i++)
                {
                    param = MCUReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;
                    m_mcu_device.Hex2Physical(ref param);
                }
            }
            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OZ1C115Reglist = new List<Parameter>();
            List<Parameter> MCUReglist = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                        {
                            if (p == null) break;
                            OZ1C115Reglist.Add(p);
                            break;
                        }
                    case ElementDefine.COBRA_SUBTYPE.MCU_SUBTYPE:
                        {
                            if (p == null) break;
                            MCUReglist.Add(p);
                            break;
                        }
                }
            }

            if (OZ1C115Reglist.Count != 0)
            {
                for (int i = 0; i < OZ1C115Reglist.Count; i++)
                {
                    param = OZ1C115Reglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_oz1c115_device.Physical2Hex(ref param);
                }
            }

            if (MCUReglist.Count != 0)
            {
                for (int i = 0; i < MCUReglist.Count; i++)
                {
                    param = MCUReglist[i];
                    if (param == null) continue;
                    if ((param.guid & ElementDefine.ElementMask) == ElementDefine.TemperatureElement) continue;

                    m_mcu_device.Physical2Hex(ref param);
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
            byte bval = 0; 
            int ival = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = m_oz1c115_device.m_monitor_device.ReadByte(0, ref bval); 
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = (int)(bval>>3);

            ival = (int)(bval&0x07);
            deviceinfor.shwversion = ival.ToString();
            deviceinfor.hwsubversion = ival;
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

        #region 外部温度转换
        public double ResistToTemp(double resist)
        {
            Int32 idx;
            for (idx = 0; idx < parent.m_ResistVals.Count; idx++)
            {
                if (parent.m_ResistVals[idx] <= resist)
                    break;
            }

            if (idx == 0)
                return parent.m_TempVals[0];
            else if (idx >= parent.m_ResistVals.Count)
                idx--;
            else if ((parent.m_ResistVals[idx] < resist) && (parent.m_ResistVals[idx - 1] > resist))
            {
                float slope = (float)((float)parent.m_TempVals[idx] - (float)parent.m_TempVals[idx - 1]) / (float)((float)parent.m_ResistVals[idx] - (float)parent.m_ResistVals[idx - 1]);

                return parent.m_TempVals[idx] - ((float)slope * (float)(parent.m_ResistVals[idx] - resist));
            }

            return parent.m_TempVals[idx];
        }

        public double TempToResist(double temp)
        {
            Int32 idx;
            for (idx = 0; idx < parent.m_TempVals.Count; idx++)
            {
                if (parent.m_TempVals[idx] >= temp)
                    break;
            }

            if (idx == 0)
                return parent.m_ResistVals[0];
            else if (idx >= parent.m_TempVals.Count)
                idx--;
            else if ((parent.m_TempVals[idx] > temp) && (parent.m_TempVals[idx - 1] < temp))
            {
                double slope = (double)((double)parent.m_ResistVals[idx] - (double)parent.m_ResistVals[idx - 1]) / (double)((double)parent.m_TempVals[idx] - (double)parent.m_TempVals[idx - 1]);
                return (double)((double)parent.m_ResistVals[idx] - (double)((double)slope * (double)((double)parent.m_TempVals[idx] - (double)temp)));
            }

            return parent.m_ResistVals[idx];
        }
        #endregion
    }
}