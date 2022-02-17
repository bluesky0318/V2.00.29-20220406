using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.KALL16D
{
    public class DEMDeviceManage : IDEMLib3
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

        internal ParamContainer EpParamlist = null; 
        internal ParamContainer OpParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions  m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal UInt16 trigger_sw_osr = 0;
        internal UInt16 auto_sw_osr = 0;
        internal ElementDefine.CADC_MODE cadc_mode = ElementDefine.CADC_MODE.DISABLE;
        internal ElementDefine.SCAN_MODE scan_mode = ElementDefine.SCAN_MODE.TRIGGER;
        internal COBRA_HWMode_Reg[] m_EFRegImg = new COBRA_HWMode_Reg[ElementDefine.EPROM_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_EFRegImgEX = new COBRA_HWMode_Reg[ElementDefine.EF_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_EEPROMVerifyImg = new COBRA_HWMode_Reg[ElementDefine.EPROM_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal Dictionary<UInt32, Tuple<Parameter, Parameter>> m_guid_slope_offset = new Dictionary<UInt32, Tuple<Parameter, Parameter>>();
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        public DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT,"Read CADC timeout!"},
            {ElementDefine.IDS_ERR_DEM_TIGGER_SCAN_TIMEOUT,"Trigger Scan timeout!"},
            { ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE,"Don't support to operate one parameter."},
            {ElementDefine.IDS_ERR_DEM_ERROR_MODE,"Please unlock configuration bit before operation."},
            {ElementDefine.IDS_ERR_DEM_POWERON_FAILED,"Turn on programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED,"Turn off programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWERCHECK_FAILED,"Programming voltage check failed!"},
            {ElementDefine.IDS_ERR_DEM_ATE_EMPTY_CHECK_FAILED,"ATE empty check failed!"}
        };
        #endregion

        #region Parameters
        public Parameter pECOT = new Parameter();
        public Parameter pEDOT = new Parameter();
        public Parameter pECUT = new Parameter();
        public Parameter pEDUT = new Parameter();

        public Parameter pOCOT = new Parameter();
        public Parameter pODOT = new Parameter();
        public Parameter pOCUT = new Parameter();
        public Parameter pODUT = new Parameter();

        public Parameter pEOV_E = new Parameter();
        public Parameter pEUV_E = new Parameter();
        public Parameter pEDOC1_E = new Parameter();
        public Parameter pECOC_E = new Parameter();
        public Parameter pEDOT_E = new Parameter();
        public Parameter pEDUT_E = new Parameter();
        public Parameter pECOT_E = new Parameter();
        public Parameter pECUT_E = new Parameter();
        public Parameter pEUB_E = new Parameter();
        public Parameter pECTO_E = new Parameter();
        public Parameter pE0V_Charge_Prohibit_E = new Parameter();
        public Parameter pEEOC_E = new Parameter();
        public Parameter pOOV_E = new Parameter();
        public Parameter pOUV_E = new Parameter();
        public Parameter pODOC1_E = new Parameter();
        public Parameter pOCOC_E = new Parameter();
        public Parameter pODOT_E = new Parameter();
        public Parameter pODUT_E = new Parameter();
        public Parameter pOCOT_E = new Parameter();
        public Parameter pOCUT_E = new Parameter();
        public Parameter pOUB_E = new Parameter();
        public Parameter pOCTO_E = new Parameter();
        public Parameter pO0V_Charge_Prohibit_E = new Parameter();
        public Parameter pOEOC_E = new Parameter();

        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.EPROMElement, m_EFRegImg);
            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);

            InitialImgReg();
            InitParameters();

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

        public UInt32 ReadDevice(ref TASKMessage msg)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 WriteDevice(ref TASKMessage msg)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 Verification(ref TASKMessage bgworker)
        {
            return m_dem_bm.Verification(ref bgworker);
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            EpParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EPROMElement);
            if (EpParamlist == null) return;

            OpParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (OpParamlist == null) return;
        }


        private void InitParameters()
        {
            string str = string.Empty;
            Parameter param = null, param1 = null, param2 = null;
            for (byte i = 0; i < ElementDefine.EF_MEMORY_SIZE; i++)
            {
                m_EFRegImgEX[i] = new COBRA_HWMode_Reg();
                m_EFRegImgEX[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EFRegImgEX[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                m_EFRegImg[i + ElementDefine.EP_ATE_OFFSET] = m_EFRegImgEX[i];
            }

            for (byte i = 0; i < ElementDefine.EPROM_MEMORY_SIZE; i++)
            {
                m_EEPROMVerifyImg[i] = new COBRA_HWMode_Reg();
                m_EEPROMVerifyImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EEPROMVerifyImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            #region EPROM Parameters
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ovp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ovp_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_ovp_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 256; i++)
                {
                    str = string.Format("{0:D}mV", 10 * (i - 1) + 2570);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ovp_rls_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ovp_rls_hys);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_ovp_rls_hys);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:D}mV", 10 * (2*i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ovp_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ovp_dly);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_ovp_dly);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:D} scan cycles", (2 * i + 2));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_uvp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_uvp_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_uvp_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 128; i++)
                {
                    str = string.Format("{0:D}mV", 20 * (i - 1) + 1000);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_uvp_rls_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_uvp_rls_hys);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_uvp_rls_hys);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:D}mV", (2 * i + 1)*20);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_uvp_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_uvp_dly);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_uvp_dly);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D} scan cycles", (2* i + 2));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cocp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cocp_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cocp_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 256; i++)
                {
                    str = string.Format("{0:D}mv", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc1p_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc1p_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_doc1p_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 256; i++)
                {
                    str = string.Format("{0:D}mv", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc2p_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc2p_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_doc2p_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D}mv", i*20);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_scp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_scp_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_scp_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D}mV", i * 30 + 50);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc2p_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc2p_dly);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_doc2p_dly);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D}*doc2p_dly_unit", i+1);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dsg_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dsg_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_dsg_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:F3}mv", (i+1)*0.125);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_chg_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_chg_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_chg_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:F3}mv", (i + 1) * 0.125);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc1p_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc1p_dly);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_doc1p_dly);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:D} scan cycles", (2*i+2));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cocp_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cocp_dly);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cocp_dly);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:D} scan cycles", (2 * i + 2));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dot_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dot_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_dot_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F1}mV", i*2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dotr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dotr_hys);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_dotr_hys);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", (i + 1) * 5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cot_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cot_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cot_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F1}mV", i * 5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cotr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cotr_hys);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cotr_hys);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", (i + 1) * 5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dut_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dut_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_dut_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 512; i++)
                {
                    str = string.Format("{0:F1}mV", i * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dutr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dutr_hys);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_dutr_hys);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", (i+1) * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cut_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cut_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cut_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:F1}mV", i * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cutr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cutr_hys);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cutr_hys);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", (i + 1) * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cb_start_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cb_start_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cb_start_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:F1}mV", i * 20 + 3000);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ub_cell_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ub_cell_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_ub_cell_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                for (int i = 1; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", i * 40);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cell_open_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cell_open_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_cell_open_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                for (int i = 1; i < 4; i++)
                {
                    str = string.Format("{0:D}mV", i * 200 + 600);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_multi_function_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_multi_function_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_multi_function_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D}mV", i * 80 + 2000);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_eoc_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_eoc_th);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_eoc_th);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                for (int i = 1; i < 128; i++)
                {
                    str = string.Format("{0:D}mV", i * 10 + 3240);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_CADC_SYS_OFFSET);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_CADC_SYS_OFFSET);
            param2 = OpParamlist.GetParameterByGuid(ElementDefine.Op_Map_CADC_SYS_OFFSET);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F4}uV", i * 7.8125 - 1000);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            #endregion

            ParamContainer pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.EPROMElement);
            pECOT = pc.GetParameterByGuid(ElementDefine.EPROM_cot_th);
            pEDOT = pc.GetParameterByGuid(ElementDefine.EPROM_dot_th);
            pECUT = pc.GetParameterByGuid(ElementDefine.EPROM_cut_th);
            pEDUT = pc.GetParameterByGuid(ElementDefine.EPROM_dut_th);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement);
            pOCOT = pc.GetParameterByGuid(ElementDefine.Op_cot_th);
            pODOT = pc.GetParameterByGuid(ElementDefine.Op_dot_th);
            pOCUT = pc.GetParameterByGuid(ElementDefine.Op_cut_th);
            pODUT = pc.GetParameterByGuid(ElementDefine.Op_dut_th);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.VirtualElement);
            pEOV_E = pc.GetParameterByGuid(ElementDefine.EOV_E);
            pEUV_E = pc.GetParameterByGuid(ElementDefine.EUV_E);
            pEDOC1_E = pc.GetParameterByGuid(ElementDefine.EDOC1_E);
            pECOC_E = pc.GetParameterByGuid(ElementDefine.ECOC_E);
            pEDOT_E = pc.GetParameterByGuid(ElementDefine.EDOT_E);
            pEDUT_E = pc.GetParameterByGuid(ElementDefine.EDUT_E);
            pECOT_E = pc.GetParameterByGuid(ElementDefine.ECOT_E);
            pECUT_E = pc.GetParameterByGuid(ElementDefine.ECUT_E);
            pEUB_E = pc.GetParameterByGuid(ElementDefine.EUB_E);
            pECTO_E = pc.GetParameterByGuid(ElementDefine.ECTO_E);
            pE0V_Charge_Prohibit_E = pc.GetParameterByGuid(ElementDefine.E0V_Charge_Prohibit_E);
            pEEOC_E = pc.GetParameterByGuid(ElementDefine.EEOC_E);
            pOOV_E = pc.GetParameterByGuid(ElementDefine.OOV_E);
            pOUV_E = pc.GetParameterByGuid(ElementDefine.OUV_E);
            pODOC1_E = pc.GetParameterByGuid(ElementDefine.ODOC1_E);
            pOCOC_E = pc.GetParameterByGuid(ElementDefine.OCOC_E);
            pODOT_E = pc.GetParameterByGuid(ElementDefine.ODOT_E);
            pODUT_E = pc.GetParameterByGuid(ElementDefine.ODUT_E);
            pOCOT_E = pc.GetParameterByGuid(ElementDefine.OCOT_E);
            pOCUT_E = pc.GetParameterByGuid(ElementDefine.OCUT_E);
            pOUB_E = pc.GetParameterByGuid(ElementDefine.OUB_E);
            pOCTO_E = pc.GetParameterByGuid(ElementDefine.OCTO_E);
            pO0V_Charge_Prohibit_E = pc.GetParameterByGuid(ElementDefine.O0V_Charge_Prohibit_E);
            pOEOC_E = pc.GetParameterByGuid(ElementDefine.OEOC_E);
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
            return OpParamlist.GetParameterByGuid(guid);
        }

        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.EPROM_MEMORY_SIZE; i++)
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

            m_guid_slope_offset.Add(ElementDefine.CADC, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.CADC_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.CADC_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage01, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell1_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell1_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage02, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell2_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell2_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage03, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell3_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell3_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage04, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell4_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell4_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage05, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell5_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell5_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage06, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell6_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell6_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage07, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell7_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell7_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage08, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell8_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell8_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage09, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell9_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell9_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage10, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell10_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell1O_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage11, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell11_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell11_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage12, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell12_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell12_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage13, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell13_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell13_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage14, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell14_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell14_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage15, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell15_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell15_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.CellVoltage16, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Cell16_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Cell16_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.Isens, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.Isens_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.Isens_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.VBATT, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.VBATT_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.VBATT_Offset_Trim)));
            m_guid_slope_offset.Add(ElementDefine.TS, Tuple.Create(OpParamlist.GetParameterByGuid(ElementDefine.TS_Slope_Trim), OpParamlist.GetParameterByGuid(ElementDefine.TS_Offset_Trim)));

        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }

        public UInt32 SetCADCMode(ElementDefine.CADC_MODE mode)
        {
            return m_dem_bm.SetCADCMode(mode);
        }
    }
}

