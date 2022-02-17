using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.SequoiaSBS
{
    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();

        public Dictionary<UInt32, TSMBbuffer> m_HwMode_Dic = new Dictionary<UInt32, TSMBbuffer>();
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);
            InitialImgReg();
            m_dem_bm.Init(this);
            m_dem_dm.Init(this);
        }

        public bool EnumerateInterface()
        {
            return m_dem_bm.EnumerateInterface();
        }

        public bool CreateInterface()
        {
            return m_dem_bm.CreateInterface();
        }

        public bool DestroyInterface()
        {
            return m_dem_bm.DestroyInterface();
        }

        public void UpdataDEMParameterList(Parameter p)
        {
            m_dem_dm.UpdateEpParamItemList(p);
        }

        public void Physical2Hex(ref Parameter param)
        {
            m_dem_dm.Physical2Hex(ref param);
        }

        public void Hex2Physical(ref Parameter param)
        {
            m_dem_dm.Hex2Physical(ref param);
        }

        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            return m_dem_bm.GetDeviceInfor(ref deviceinfor);
        }

        public UInt32 Erase(ref TASKMessage bgworker)
        {
            return m_dem_bm.EraseEEPROM(ref bgworker);
        }

        public UInt32 BlockMap(ref TASKMessage bgworker)
        {
            return m_dem_bm.EpBlockRead();
        }

        public UInt32 Command(ref TASKMessage bgworker)
        {
            return m_dem_bm.Command(ref bgworker);
        }

        public UInt32 Read(ref TASKMessage bgworker)
        {
            return m_dem_bm.Read(ref bgworker);
        }

        public UInt32 Write(ref TASKMessage bgworker)
        {
            return m_dem_bm.Write(ref bgworker);
        }

        public UInt32 BitOperation(ref TASKMessage bgworker)
        {
            return m_dem_bm.BitOperation(ref bgworker);
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage bgworker)
        {
            return m_dem_bm.ConvertHexToPhysical(ref bgworker);
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage bgworker)
        {
            return m_dem_bm.ConvertPhysicalToHex(ref bgworker);
        }

        public UInt32 GetSystemInfor(ref TASKMessage bgworker)
        {
            return m_dem_bm.GetSystemInfor(ref bgworker);
        }

        public UInt32 GetRegisteInfor(ref TASKMessage bgworker)
        {
            return m_dem_bm.GetRegisteInfor(ref bgworker);
        }
        #endregion

        private void InitialImgReg()
        {
            UInt32 bcmd = 0;
            Reg regLow = null;
            ParamContainer paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.SBSElement);
            if (paramContainer == null) return;

            foreach (Parameter p in paramContainer.parameterlist)
            {
                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                p.tsmbBuffer.length = 4;
                if (m_HwMode_Dic.ContainsKey(bcmd)) continue;
                m_HwMode_Dic.Add(bcmd, p.tsmbBuffer);
            }

            paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.CONFIGElement);
            if (paramContainer == null) return;

            foreach (Parameter p in paramContainer.parameterlist)
            {
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    if (dic.Key.Equals("Low"))
                    {
                        regLow = dic.Value;
                        bcmd = regLow.address;
                        p.tsmbBuffer.length = regLow.bitsnumber;
                        if (m_HwMode_Dic.ContainsKey(bcmd)) continue;
                        m_HwMode_Dic.Add(bcmd, p.tsmbBuffer);
                    }
                }
            }
        }

        public UInt32 ReadRsenseMain()
        {
            return m_dem_bm.ReadRsenseMain();
        }
    }
}

