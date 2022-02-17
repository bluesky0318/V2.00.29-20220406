using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;


namespace Cobra.OZ8975
{
    public class DEMDeviceManage : IDEMLib
    {
        #region Properties

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

        internal ParamContainer EFParamlist = null;
        internal ParamContainer OPParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_MapRegImg = new COBRA_HWMode_Reg[ElementDefine.MAP_MEMORY_SIZE];
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();

        public Parameter pE_BAT_TYPE = null;
        public Parameter pE_DOT_TH = null;
        public Parameter pE_DOT_E = null;
        public Parameter pE_OVP_TH = null;
        public Parameter pO_BAT_TYPE = null;
        public Parameter pO_DOT_TH = null;
        public Parameter pO_DOT_E = null;
        public Parameter pO_OVP_TH = null;

        public Parameter pE_Uuvp = null;
        public Parameter pO_Uuvp = null;
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_POWERON_FAILED,"Turn on programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED,"Turn off programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWERCHECK_FAILED,"Programming voltage check failed!"},
            {ElementDefine.IDS_ERR_DEM_FROZEN,"Bank1 and bank2 are frozen, stop writing."},
            {ElementDefine.IDS_ERR_DEM_FROZEN_OP,"Bank1 is frozen, so writing OP registers is prohibited. Please check IC document for details."},
            {ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE,"Don't support to operate one parameter."},
            {ElementDefine.IDS_ERR_DEM_BLOCK,"Bank2 frozen bit is set while bank1 frozen bit is not, the write operation is canceled."},
            {ElementDefine.IDS_ERR_DEM_ERROR_MODE,"Please make OZ8975 into register write mode before write mapping registers."},
        };
        #endregion

        private void InitParameters()
        {
            ParamContainer pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.EFUSEMapElement); 
            pO_DOT_TH = pc.GetParameterByGuid(ElementDefine.O_DOT_TH);
            pO_BAT_TYPE = pc.GetParameterByGuid(ElementDefine.O_BAT_TYPE);
            pO_OVP_TH = pc.GetParameterByGuid(ElementDefine.O_OVP_TH);
            pO_Uuvp = pc.GetParameterByGuid(ElementDefine.O_Uuvp);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement);
            pE_DOT_TH = pc.GetParameterByGuid(ElementDefine.E_DOT_TH);
            pE_BAT_TYPE = pc.GetParameterByGuid(ElementDefine.E_BAT_TYPE);
            pE_OVP_TH = pc.GetParameterByGuid(ElementDefine.E_OVP_TH);
            pE_Uuvp = pc.GetParameterByGuid(ElementDefine.E_Uuvp);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.VirtualElement);
            pE_DOT_E = pc.GetParameterByGuid(ElementDefine.E_DOT_E);
            pO_DOT_E = pc.GetParameterByGuid(ElementDefine.O_DOT_E);
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

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (EFParamlist == null) return;
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
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }


            for (byte i = 0; i < ElementDefine.MAP_MEMORY_SIZE; i++)
            {
                m_MapRegImg[i] = new COBRA_HWMode_Reg();
                m_MapRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_MapRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            //m_HwMode_RegList.Add(ElementDefine.EFUSEElement, m_EFRegImg);
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
            if (bgworker.gm.sflname == "OPConfig")
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
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

        #region 子操作
        public UInt32 ResetOvpOffset(ref Parameter p)
        {
            byte bval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (p.guid)
            {
                case ElementDefine.OVP_TH1:
                    ret = m_dem_bm.ReadByte(0x2A, ref bval);
                    break;
                case ElementDefine.OVP_TH2:
                    ret = m_dem_bm.ReadByte(0x2E, ref bval);
                    break;
            }
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if ((bval & 0x80) == 0x80)
                p.offset = 3400;
            else
                p.offset = 3900;            
            return ret;
        }
        #endregion
    }
}

