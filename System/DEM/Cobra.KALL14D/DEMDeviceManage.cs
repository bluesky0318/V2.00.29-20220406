using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.KALL14D
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

        internal ParamContainer EpParamlist = null; 
        internal ParamContainer OpParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions  m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_EFRegImg = new COBRA_HWMode_Reg[ElementDefine.EPROM_MEMORY_SIZE];
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
            Parameter param = null, param1 = null;
            #region EPROM Parameters
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ovp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ovp_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                for (int i = 1; i < 256; i++)
                {
                    str = string.Format("{0:D}mV", 10 * (i - 1) + 2570);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ovp_rls_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ovp_rls_hys);
            if ((param != null)&&(param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:D}mV", 10 * (2*i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ovp_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ovp_dly);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:D} scan cycles", (2 * i + 2));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_uvp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_uvp_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                for (int i = 1; i < 128; i++)
                {
                    str = string.Format("{0:D}mV", 20 * (i - 1) + 1000);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_uvp_rls_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_uvp_rls_hys);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:D}mV", (2 * i + 1)*20);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_uvp_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_uvp_dly);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D} scan cycles", (8* i + 4));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cocp_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cocp_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:D}mv", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc1p_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc1p_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:D}mv", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc2p_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc2p_dly);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D}*doc2p_dly_factor", i+1);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dsg_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dsg_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:F3}mv", (i+1)*0.125);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_chg_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_chg_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:F3}mv", (i + 1) * 0.125);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_doc1p_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_doc1p_dly);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:D} scan cycles", (8*i+4));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cocp_dly);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cocp_dly);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:D} scan cycles", (2 * i + 2));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dot_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dot_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F1}mV", i*2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dotr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dotr_hys);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", i * 5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cot_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cot_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F1}mV", i * 5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cotr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cotr_hys);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", i * 5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dut_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dut_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 512; i++)
                {
                    str = string.Format("{0:F1}mV", i * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_dutr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_dutr_hys);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                    str = string.Format("{0:F1}mV", i * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cut_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cut_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:F1}mV", i * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cutr_hys);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cutr_hys);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:F1}mV", i * 2.5);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cb_start_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cb_start_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:F1}mV", i * 20 + 3000);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_ub_cell_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_ub_cell_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param.itemlist.Add("no function");
                param1.itemlist.Add("no function");
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:F1}mV", (i+1) * 40);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_cell_open_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_cell_open_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param.itemlist.Add("disable cell-tap-open detection");
                param1.itemlist.Add("disable cell-tap-open detection");
                for (int i = 0; i < 8; i++)
                {
                    str = string.Format("{0:D}mV", (i + 1) * 200 + 600);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_multi_function_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_multi_function_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}mV", i * 20 + 1800);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_eoc_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_eoc_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param.itemlist.Add("disable EOC check");
                param1.itemlist.Add("disable EOC check");
                for (int i = 1; i < 128; i++)
                {
                    str = string.Format("{0:D}mV", i * 10 + 3250);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            param = EpParamlist.GetParameterByGuid(ElementDefine.EPROM_0v_chg_disable_th);
            param1 = OpParamlist.GetParameterByGuid(ElementDefine.Op_0v_chg_disable_th);
            if ((param != null) && (param1 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param.itemlist.Add("disable");
                param1.itemlist.Add("disable");
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}mV", i * 20 + 800);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                }
            }
            #endregion
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

