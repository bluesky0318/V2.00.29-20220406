using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.Second_EC_Bluewhale
{
    class OZ1C115
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMBehaviorManage m_parent;
        public DEMBehaviorManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public Monitor m_monitor_device = new Monitor();
        public Charger m_charger_device = new Charger();
        #endregion

        public void Init(object pParent)
        {
            parent = (DEMBehaviorManage)pParent;

            m_monitor_device.Init(parent);
            m_charger_device.Init(parent);
        }

        #region 操作寄存器操作
        public UInt32 Read(List<Parameter> OpParamList)
        {
            if (OpParamList.Count == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;

            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MonitorOpReglist = new List<Parameter>();
            List<Parameter> ChargerOpReglist = new List<Parameter>();

            foreach (Parameter p in OpParamList)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                        {
                            if (p == null) break;
                            MonitorOpReglist.Add(p);
                            break;
                        }
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
                }
            }

            //Read
            ret = m_monitor_device.ReadByte(MonitorOpReglist);
            ret1 = m_charger_device.ReadByte(ChargerOpReglist);

            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL))
                return ret;
            else if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (ChargerOpReglist.Count == 0))
                return ret;
            else if ((ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) && (MonitorOpReglist.Count == 0))
                return ret1;
            return ret;
        }

        public UInt32 Write(List<Parameter> OpParamList)
        {
            if (OpParamList.Count == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;

            UInt32 ret1 = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> MonitorOpReglist = new List<Parameter>();
            List<Parameter> ChargerOpReglist = new List<Parameter>();

            foreach (Parameter p in OpParamList)
            {
                switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
                {
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                        {
                            if (p == null) break;
                            MonitorOpReglist.Add(p);
                            break;
                        }
                    case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                        {
                            if (p == null) break;
                            ChargerOpReglist.Add(p);
                            break;
                        }
                }
            }
            //Write
            ret = m_monitor_device.WriteByte(MonitorOpReglist);
            ret1 = m_charger_device.WriteByte(ChargerOpReglist);

            if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL))
                return ret;
            else if ((ret != LibErrorCode.IDS_ERR_SUCCESSFUL) && (ChargerOpReglist.Count == 0))
                return ret;
            else if ((ret1 != LibErrorCode.IDS_ERR_SUCCESSFUL) && (MonitorOpReglist.Count == 0))
                return ret1;
            return ret;
        }
        #endregion

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
            {
                case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                    {
                        if (p == null) break;
                        m_monitor_device.Physical2Hex(ref p);
                        break;
                    }
                case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                    {
                        if (p == null) break;
                        m_charger_device.Physical2Hex(ref p);
                        break;
                    }
            }
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            switch ((ElementDefine.COBRA_SUBTYPE)p.subsection)
            {
                case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_MONITOR:
                    {
                        if (p == null) break;
                        m_monitor_device.Hex2Physical(ref p);
                        break;
                    }
                case ElementDefine.COBRA_SUBTYPE.OZ1C115_SUBTYPE_CHARGE:
                    {
                        if (p == null) break;
                        m_charger_device.Hex2Physical(ref p);
                        break;
                    }
            }
        }
    }
}
