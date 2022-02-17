using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using O2Micro.Cobra.Common;
using O2Micro.Cobra.AutoMationTest;

namespace O2Micro.Cobra.HummingBird
{
    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        internal ParamContainer EFParamlist = null;
        internal ParamContainer OPParamlist = null;
        internal ParamContainer INParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_InfoRegImg = new COBRA_HWMode_Reg[ElementDefine.INFO_BLOCK_SIZE];
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);
            AutoMationTest.AutoMationTest.init(m_HwMode_RegList);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);

            InitialImgReg();
            m_dem_bm.Init(this);
            m_dem_dm.Init(this);
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

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            EFParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EFUSEElement);
            if (EFParamlist == null) return;

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (OPParamlist == null) return;
        }

        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            for (UInt16 i = 0; i < ElementDefine.INFO_BLOCK_SIZE; i++)
            {
                m_InfoRegImg[i] = new COBRA_HWMode_Reg();
                m_InfoRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_InfoRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
    }
}

