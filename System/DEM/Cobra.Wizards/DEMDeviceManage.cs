using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;
using Cobra.Communication;

namespace Cobra.SD77060
{
    public class DEMDeviceManage : IDEMLib
    {
        #region Properties

        internal ParamContainer OPParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManageBase m_dem_bm_base = new DEMBehaviorManageBase();
        private ExpertDEMBehaviorManage m_expert_dem_bm = new ExpertDEMBehaviorManage();
        private TrimDEMBehaviorManage m_trim_dem_bm = new TrimDEMBehaviorManage();
        private ScanDEMBehaviorManage m_scan_dem_bm = new ScanDEMBehaviorManage();

        public CCommunicateManager m_Interface = new CCommunicateManager();

        #region Parameters
        #endregion
        #region Enable Control bit
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            //{ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT,"Read CADC timeout!"},
        };
        #endregion

        #endregion

        #region other functions
        private void InitParameters()
        {
            //ParamContainer pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement);
            //pTHM_CRRT_SEL = pc.GetParameterByGuid(ElementDefine.THM_CRRT_SEL);
            //CellNum = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement).GetParameterByGuid(ElementDefine.CellNum);
        }

        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
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
        }
        #endregion
        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);

            InitialImgReg();
            InitParameters();

            CreateInterface();

            m_dem_bm_base.parent = this;
            m_dem_bm_base.dem_dm = new DEMDataManageBase(m_dem_bm_base);
            m_expert_dem_bm.parent = this;
            m_expert_dem_bm.dem_dm = new DEMDataManageBase(m_dem_bm_base);
            m_trim_dem_bm.parent = this;
            m_trim_dem_bm.dem_dm = new DEMDataManageBase(m_dem_bm_base);
            m_scan_dem_bm.parent = this;
            m_scan_dem_bm.dem_dm = new DEMDataManageBase(m_dem_bm_base);

            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.OCE); 
            LibErrorCode.UpdateDynamicalLibError(ref m_dynamicErrorLib_dic);

        }


        #region 端口操作
        public bool CreateInterface()
        {
            bool bdevice = EnumerateInterface();
            if (!bdevice) return false;

            return m_Interface.OpenDevice(ref m_busoption);
        }

        public bool DestroyInterface()
        {
            return m_Interface.CloseDevice();
        }

        public bool EnumerateInterface()
        {
            return m_Interface.FindDevices(ref m_busoption);
        }
        #endregion

        public void UpdataDEMParameterList(Parameter p)
        {
            //m_register_config_dem_bm.dem_dm.UpdateEpParamItemList(p);
        }

        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            return m_dem_bm_base.GetDeviceInfor(ref deviceinfor);
        }

        public UInt32 Erase(ref TASKMessage bgworker)
        {
            //return m_dem_bm_base.EraseEEPROM(ref bgworker);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 BlockMap(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.EpBlockRead();
        }

        public UInt32 Command(ref TASKMessage bgworker)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            //EXPERT_ENTER_SERIAL_MODE = 0x01,
            //EXPERT_ENTER_PARALLEL_MODE = 0x02,
            //EXPERT_ENTER_SHIP_MODE = 0x03,
            //EXPERT_ENTER_SLEEP_MODE = 0x04,
            //EXPERT_EXIT_SLEEP_MODE = 0x05,
            //EXPERT_ENTER_TEST_MODE = 0x06,
            //EXPERT_EXIT_TEST_MODE = 0x07,
            //EXPERT_ENTER_FET_CTRL = 0x08,
            //EXPERT_EXIT_FET_CTRL = 0x09,
            //EXPERT_ENTER_TRIM_MODE = 0x0a,
            switch ((ElementDefine.COMMAND)bgworker.sub_task)
            {
                case ElementDefine.COMMAND.EXPERT_ENTER_SERIAL_MODE:
                case ElementDefine.COMMAND.EXPERT_ENTER_PARALLEL_MODE:
                case ElementDefine.COMMAND.EXPERT_ENTER_SHIP_MODE:
                case ElementDefine.COMMAND.EXPERT_ENTER_SLEEP_MODE:
                case ElementDefine.COMMAND.EXPERT_EXIT_SLEEP_MODE:
                case ElementDefine.COMMAND.EXPERT_ENTER_TEST_MODE:
                case ElementDefine.COMMAND.EXPERT_EXIT_TEST_MODE:
                case ElementDefine.COMMAND.EXPERT_ENTER_FET_CTRL:
                case ElementDefine.COMMAND.EXPERT_EXIT_FET_CTRL:
                case ElementDefine.COMMAND.EXPERT_ENTER_TRIM_MODE:
                    ret = m_expert_dem_bm.Command(ref bgworker);
                    break;
                case ElementDefine.COMMAND.TRIM_SLOPE_TRIMMING:
                case ElementDefine.COMMAND.TRIM_OFFSET_TRIMMING:
                    ret = m_trim_dem_bm.Command(ref bgworker);
                    break;
                case ElementDefine.COMMAND.OPTIONS:
                    ret = m_scan_dem_bm.Command(ref bgworker);
                    break;
            }
            return ret;
        }

        public UInt32 Read(ref TASKMessage bgworker)
        {
            UInt32 ret = 0;
            //if (bgworker.gm.sflname == "Scan")          //Scan里面有个PreRead，所以这里只能区别处理
            //    ret = m_scan_dem_bm.Read(ref bgworker);
            //else
                ret = m_dem_bm_base.Read(ref bgworker);
            return ret;
        }

        public UInt32 Write(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.Write(ref bgworker);
        }

        public UInt32 BitOperation(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.BitOperation(ref bgworker);
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.ConvertHexToPhysical(ref bgworker);
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.ConvertPhysicalToHex(ref bgworker);
        }

        public UInt32 GetSystemInfor(ref TASKMessage bgworker)
        {
            //return m_scan_dem_bm.GetSystemInfor(ref bgworker);
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.GetRegisteInfor(ref bgworker);
        }
        #endregion
    }
}

