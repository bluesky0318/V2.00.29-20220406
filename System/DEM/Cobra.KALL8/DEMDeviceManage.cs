using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;
//using Cobra.EM;

namespace Cobra.KALL08
{
    public class DEMDeviceManage : IDEMLib
    {
        #region Properties
        internal double etrxM
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpETRxM);
                if (param == null) return 0.0;
                else return param.phydata;
            }
        }

        internal double etrxS
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpETRxS);
                if (param == null) return 0.0;
                else return param.phydata;
            }
        }

        internal ParamContainer EFParamlist = null;
        internal ParamContainer OPParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_EFRegImg = new COBRA_HWMode_Reg[ElementDefine.EF_MEMORY_SIZE + ElementDefine.EF_MEMORY_OFFSET];
        internal COBRA_HWMode_Reg[] m_EFRegImgEX = new COBRA_HWMode_Reg[ElementDefine.EF_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();


        #region Parameters
        public Parameter pECOT = new Parameter();
        public Parameter pEDOT = new Parameter();
        public Parameter pECUT = new Parameter();
        public Parameter pEDUT = new Parameter();
        public Parameter pECTO = new Parameter();
        public Parameter pEEOC = new Parameter();

        public Parameter pOCOT = new Parameter();
        public Parameter pODOT = new Parameter();
        public Parameter pOCUT = new Parameter();
        public Parameter pODUT = new Parameter();
        public Parameter pOCTO = new Parameter();
        public Parameter pOEOC = new Parameter();

        public Parameter CellNum = new Parameter();
        public Parameter[] Cell = new Parameter[8];

        public Parameter pOCTO_E = new Parameter();
        public Parameter pOCUT_E = new Parameter();
        public Parameter pOEOC_E = new Parameter();
        public Parameter pODUT_E = new Parameter();
        public Parameter pECTO_E = new Parameter();
        public Parameter pECUT_E = new Parameter();
        public Parameter pEEOC_E = new Parameter();
        public Parameter pEDUT_E = new Parameter();
        #endregion
        #region Enable Control bit
        public bool isOCTOEnabled = true;
        public bool isOCUTEnabled = true;
        public bool isOEOCEnabled = true;
        public bool isODUTEnabled = true;
        public bool isECTOEnabled = true;
        public bool isECUTEnabled = true;
        public bool isEEOCEnabled = true;
        public bool isEDUTEnabled = true;
        #endregion
        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_POWERON_FAILED,"Turn on programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED,"Turn off programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWERCHECK_FAILED,"Programming voltage check failed!"}
        };
        #endregion

        #endregion

        #region other functions
        private void InitParameters()
        {
            ParamContainer pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.EFUSEElement);
            pECOT = pc.GetParameterByGuid(ElementDefine.ECOT);
            pEDOT = pc.GetParameterByGuid(ElementDefine.EDOT);
            pECUT = pc.GetParameterByGuid(ElementDefine.ECUT);
            pEDUT = pc.GetParameterByGuid(ElementDefine.EDUT);
            pECTO = pc.GetParameterByGuid(ElementDefine.ECTO);
            pEEOC = pc.GetParameterByGuid(ElementDefine.EEOC);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement);
            pOCOT = pc.GetParameterByGuid(ElementDefine.OCOT);
            pODOT = pc.GetParameterByGuid(ElementDefine.ODOT);
            pOCUT = pc.GetParameterByGuid(ElementDefine.OCUT);
            pODUT = pc.GetParameterByGuid(ElementDefine.ODUT);
            pOCTO = pc.GetParameterByGuid(ElementDefine.OCTO);
            pOEOC = pc.GetParameterByGuid(ElementDefine.OEOC);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.VirtualElement);
            pECTO_E = pc.GetParameterByGuid(ElementDefine.ECTO_E);
            pECUT_E = pc.GetParameterByGuid(ElementDefine.ECUT_E);
            pEEOC_E = pc.GetParameterByGuid(ElementDefine.EEOC_E);
            pEDUT_E = pc.GetParameterByGuid(ElementDefine.EDUT_E);
            pOCTO_E = pc.GetParameterByGuid(ElementDefine.OCTO_E);
            pOCUT_E = pc.GetParameterByGuid(ElementDefine.OCUT_E);
            pOEOC_E = pc.GetParameterByGuid(ElementDefine.OEOC_E);
            pODUT_E = pc.GetParameterByGuid(ElementDefine.ODUT_E);
            CellNum = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement).GetParameterByGuid(ElementDefine.CellNum);
            for (int i = 0; i < 8; i++)
            {
                Cell[i] = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement).GetParameterByGuid(ElementDefine.CellBase + (UInt32)(i * 0x100));
            }

        }

        public void Physical2Hex(ref Parameter param)
        {
            m_dem_dm.Physical2Hex(ref param);
        }

        public void Hex2Physical(ref Parameter param)
        {
            m_dem_dm.Hex2Physical(ref param);
        }

        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            EFParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EFUSEElement);
            if (EFParamlist == null) return;

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (EFParamlist == null) return;

            //pullupR = tempParamlist.GetParameterByGuid(ElementDefine.TpETPullupR).phydata;
            //itv0 = tempParamlist.GetParameterByGuid(ElementDefine.TpITSlope).phydata;
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

        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.EF_MEMORY_SIZE; i++)
            {
                m_EFRegImgEX[i] = new COBRA_HWMode_Reg();
                m_EFRegImgEX[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EFRegImgEX[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;

                m_EFRegImg[i + ElementDefine.EF_MEMORY_OFFSET] = m_EFRegImgEX[i];
            }

            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
        #endregion
        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.EFUSEElement, m_EFRegImg);
            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);

            InitialImgReg();
            InitParameters();

            m_dem_bm.Init(this);
            m_dem_dm.Init(this);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.OCE); 
            LibErrorCode.UpdateDynamicalLibError(ref m_dynamicErrorLib_dic);

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

        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            return m_dem_bm.GetDeviceInfor(ref deviceinfor);
        }

        public UInt32 Erase(ref TASKMessage bgworker)
        {
            //return m_dem_bm.EraseEEPROM(ref bgworker);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
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
            UInt32 ret = 0;
            ret = m_dem_bm.Read(ref bgworker);
            return ret;
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
    }
}

