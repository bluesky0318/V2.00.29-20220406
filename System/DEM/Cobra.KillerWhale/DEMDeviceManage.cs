using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Cobra.Common;

namespace Cobra.KillerWhale
{
    #region 硬件模式寄存器结构定义
    public class COBRA_HWMode_Reg
    {
        public UInt16 val;
        public UInt32 err;
    }
    #endregion

    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        private double m_PullupR;
        internal double pullupR
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpETPullupR);
                if (param == null) return 0.0;
                else return param.phydata;
                //return m_PullupR; 
            }
            //set { m_PullupR = value; }
        }

        private double m_EtRx;
        internal double etrx
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpETRx);
                if (param == null) return 0.0;
                else return param.phydata;
                //return m_PullupR; 
            }
            //set { m_PullupR = value; }
        }

        internal ParamContainer tempParamlist = null;
        internal BusOptions  m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);
            m_dem_bm.Init(this);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.DEM);
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

        public void UpdataDEMParameterList(Parameter p)
        {
            return;
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            int index = 0;

            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;
        }

        public void ModifyTemperatureConfig(Parameter p, bool bConvert)
        {
            //bConvert为真 physical ->hex;假 hex->physical;
            Parameter tmp = tempParamlist.GetParameterByGuid(p.guid);
            if (tmp == null) return;
            if (bConvert)
                tmp.phydata = p.phydata;
            else
                p.phydata = tmp.phydata;
        }
    }
}

