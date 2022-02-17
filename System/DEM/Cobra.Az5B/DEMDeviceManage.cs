using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.Az5B
{
    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        private double m_Rsense;
        internal double rsense
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpRsense);
                if (param == null) return 2500.0;
                else return (param.phydata * 1000.0);
            }
        }

        internal ParamContainer EFParamlist = null; 
        internal ParamContainer OPParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions  m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_EFRegImg = new COBRA_HWMode_Reg[ElementDefine.EFUSE_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal ElementDefine.CADC_MODE cadc_mode = ElementDefine.CADC_MODE.DISABLE;
        internal Dictionary<UInt32, Tuple<Parameter, Parameter>> m_guid_slope_offset = new Dictionary<UInt32, Tuple<Parameter, Parameter>>();
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT,"Read CADC timeout!"},
            {ElementDefine.IDS_ERR_DEM_TIGGER_SCAN_TIMEOUT,"Trigger Scan timeout!"},
            { ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE,"Don't support to operate one parameter."},
            {ElementDefine.IDS_ERR_DEM_ERROR_MODE,"Please unlock configuration bit before operation."},
        };
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
            m_dem_bm.Init(this);
            m_dem_dm.Init(this);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.DEM);
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
            if (String.Equals(bgworker.gm.sflname, "Trim2")) return LibErrorCode.IDS_ERR_SUCCESSFUL;
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
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            EFParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EFUSEElement);
            if (EFParamlist == null) return;

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (OPParamlist == null) return;
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

        public Parameter GetOpParameterByGuid(UInt32 guid)
        {
            return OPParamlist.GetParameterByGuid(guid);
        }

        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.EFUSE_MEMORY_SIZE; i++)
            {
                m_EFRegImg[i] = new COBRA_HWMode_Reg();
                m_EFRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EFRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            m_guid_slope_offset.Add(0x00033900, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039400), OPParamlist.GetParameterByGuid(0x00039408)));
            m_guid_slope_offset.Add(0x00036100, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039500), OPParamlist.GetParameterByGuid(0x00039A00)));
            m_guid_slope_offset.Add(0x00036200, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039508), OPParamlist.GetParameterByGuid(0x00039A08)));
            m_guid_slope_offset.Add(0x00036300, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039600), OPParamlist.GetParameterByGuid(0x00039B00)));
            m_guid_slope_offset.Add(0x00036400, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039608), OPParamlist.GetParameterByGuid(0x00039B08)));
            m_guid_slope_offset.Add(0x00036500, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039700), OPParamlist.GetParameterByGuid(0x00039C00)));
            m_guid_slope_offset.Add(0x00037500, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039708), OPParamlist.GetParameterByGuid(0x00039F08)));
            m_guid_slope_offset.Add(0x00037600, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039800), OPParamlist.GetParameterByGuid(0x00039D00)));
            m_guid_slope_offset.Add(0x00037700, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039808), OPParamlist.GetParameterByGuid(0x00039D08)));
            m_guid_slope_offset.Add(0x00037C00, Tuple.Create(OPParamlist.GetParameterByGuid(0x00039900), OPParamlist.GetParameterByGuid(0x00039908)));
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }

        public UInt32 GetThmCrrtSel(ref int thm_crrt)
        {
            return m_dem_bm.GetThmCrrtSel(ref thm_crrt);
        }

        public UInt32 SetCADCMode(ElementDefine.CADC_MODE mode)
        {
            return m_dem_bm.SetCADCMode(mode);
        }
    }
}

