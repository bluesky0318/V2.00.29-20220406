using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.OZ93510
{
    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        private double m_Main_Rsense;
        internal double main_Rsense
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpMainRsense);
                if (param == null) return 2500.0;
                else return (param.phydata * 1000.0);
            }
            //set { m_PullupR = value; }
        }

        private double m_Slave_Rsense;
        internal double slave_Rsense
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpSlaveRsense);
                if (param == null) return 5000.0;
                else return (param.phydata * 1000.0);
            }
            //set { m_PullupR = value; }
        }

        internal ParamContainer MTPParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_MTPRegImg = new COBRA_HWMode_Reg[ElementDefine.MTP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_VirtualRegImg = new COBRA_HWMode_Reg[ElementDefine.VIRTUAL_MEMORY_SIZE];
        internal Dictionary<UInt32, Tuple<Parameter, Parameter>> m_guid_slope_offset = new Dictionary<UInt32, Tuple<Parameter, Parameter>>();
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT,"Read CADC timeout!"},
            {ElementDefine.IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT,"Wait trigger scan flag timeout!"},
            {ElementDefine.IDS_ERR_DEM_ACTIVE_MODE_ERROR,"Not in Active mode, please check."},
            {ElementDefine.IDS_ERR_DEM_CFET_ON_FAILED,"Cannot turn on CFET. Please check 1. if there is any OV or COC event 2. EFETC mode setting 3. EFETC pin status."},
            {ElementDefine.IDS_ERR_DEM_CFET_OFF_FAILED,"Cannot turn off CFET. Please check if it is in discharging state."},
            {ElementDefine.IDS_ERR_DEM_DFET_ON_FAILED,"Cannot turn on DFET. Please check 1. if there is any DOC or SC event 2. EFETC mode setting 3. EFETC pin status."},
            {ElementDefine.IDS_ERR_DEM_DFET_OFF_FAILED,"Cannot turn off DFET. Please check if it is in charging state."},
        };
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.MTPElement, m_MTPRegImg);
            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);
            InitialImgReg();
            m_dem_bm.Init(this);
            m_dem_dm.Init(this);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.OCE);
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

        internal UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            return m_dem_bm.ReadWord(reg,ref pval);
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            MTPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.MTPElement);
            if (MTPParamlist == null) return;

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
            for (byte i = 0; i < ElementDefine.MTP_MEMORY_SIZE; i++)
            {
                m_MTPRegImg[i] = new COBRA_HWMode_Reg();
                m_MTPRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_MTPRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            for (byte i = 0; i < ElementDefine.VIRTUAL_MEMORY_SIZE; i++)
            {
                m_VirtualRegImg[i] = new COBRA_HWMode_Reg();
                m_VirtualRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_VirtualRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            m_guid_slope_offset.Add(0x00035000, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020608), MTPParamlist.GetParameterByGuid(0x00020E08)));
            m_guid_slope_offset.Add(0x00035100, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020600), MTPParamlist.GetParameterByGuid(0x00020E00)));
            m_guid_slope_offset.Add(0x00035200, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020708), MTPParamlist.GetParameterByGuid(0x00020F08)));
            m_guid_slope_offset.Add(0x00035300, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020700), MTPParamlist.GetParameterByGuid(0x00020F00)));
            m_guid_slope_offset.Add(0x00035400, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020808), MTPParamlist.GetParameterByGuid(0x00021008)));
            m_guid_slope_offset.Add(0x00035500, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020800), MTPParamlist.GetParameterByGuid(0x00021000)));
            m_guid_slope_offset.Add(0x00035600, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020908), MTPParamlist.GetParameterByGuid(0x00021108)));
            m_guid_slope_offset.Add(0x00035700, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020900), MTPParamlist.GetParameterByGuid(0x00021100)));
            m_guid_slope_offset.Add(0x00035800, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020A08), MTPParamlist.GetParameterByGuid(0x00021208)));
            m_guid_slope_offset.Add(0x00035900, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020A00), MTPParamlist.GetParameterByGuid(0x00021200)));
            m_guid_slope_offset.Add(0x00035F00, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020B08), MTPParamlist.GetParameterByGuid(0x00021308)));
            m_guid_slope_offset.Add(0x00036700, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020D00), MTPParamlist.GetParameterByGuid(0x00021506)));
            m_guid_slope_offset.Add(0x00071400, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020B00), MTPParamlist.GetParameterByGuid(0x00021300)));
            m_guid_slope_offset.Add(0x00071800, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020C08), MTPParamlist.GetParameterByGuid(0x00021408)));
            m_guid_slope_offset.Add(0x00071D00, Tuple.Create(MTPParamlist.GetParameterByGuid(0x00020C08), MTPParamlist.GetParameterByGuid(0x00021408)));
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }

        public void SetSisensBit(UInt16 wval)
        {
            m_OpRegImg[0x01].val = wval;
            m_OpRegImg[0x01].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 GetINT25Ref(ref double ddata)
        {
            Int16 sval = 0;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = m_dem_bm.MTPReadWord(0x18, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            sval = (Int16)(wval << 2);
            ddata = (sval * 0.3125) / 4; //左移4位
            return ret;
        }
    }
}

