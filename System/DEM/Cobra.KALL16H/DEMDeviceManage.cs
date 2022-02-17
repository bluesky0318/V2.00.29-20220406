using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.KALL16H
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

        public Parameter pYFLASH_COV_TH = null;
        public Parameter pYFLASH_MAP_COV_TH = null;
        public Parameter pYFLASH_COVUV_SCAN = null;
        public Parameter pYFLASH_MAP_COVUV_SCAN = null;
        public Parameter pYFLASH_BAT_TYPE = null;
        public Parameter pYFLASH_MAP_BAT_TYPE = null;
        public Parameter pYFLASH_COVR_HYS = null;
        public Parameter pYFLASH_MAP_COVR_HYS = null;
        public Parameter pYFLASH_COV_DLY = null;
        public Parameter pYFLASH_MAP_COV_DLY = null;
        public Parameter pYFLASH_CUV_DLY = null;
        public Parameter pYFLASH_MAP_CUV_DLY = null;

        internal ParamContainer YFLASHParamlist = null;
        internal ParamContainer OPParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_YFLASHRegImg = new COBRA_HWMode_Reg[ElementDefine.YFLASH_MEMORY_SIZE + ElementDefine.YFLASH_MEMORY_OFFSET];
        internal COBRA_HWMode_Reg[] m_EFRegImgEX = new COBRA_HWMode_Reg[ElementDefine.YFLASH_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg m_Vwkup = new COBRA_HWMode_Reg();
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
        };
        #endregion
        #endregion

		#region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.YFLASHElement, m_YFLASHRegImg);
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

        #region basic functions
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            YFLASHParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.YFLASHElement);
            if (YFLASHParamlist == null) return;

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (YFLASHParamlist == null) return;
        }

        private void InitParameters()
        {
            string str = string.Empty;
            Parameter param = null, param1 = null,param2 = null;
            #region YFLASH Parameters
            pYFLASH_COV_TH = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COV_TH);
            pYFLASH_MAP_COV_TH = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COV_TH);
            pYFLASH_BAT_TYPE = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_BAT_TYPE);
            pYFLASH_MAP_BAT_TYPE = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_BAT_TYPE);
            pYFLASH_COVR_HYS = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COVR_HYS);
            pYFLASH_MAP_COVR_HYS = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COVR_HYS);
            pYFLASH_COVUV_SCAN = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COVUV_SCAN);
            pYFLASH_MAP_COVUV_SCAN = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COVUV_SCAN);
            pYFLASH_COV_DLY = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COV_DLY);
            pYFLASH_MAP_COV_DLY = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COV_DLY);
            pYFLASH_CUV_DLY = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_CUV_DLY);
            pYFLASH_MAP_CUV_DLY = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_CUV_DLY); 
            if ((pYFLASH_COV_DLY != null) && (pYFLASH_MAP_COV_DLY != null)&& (pYFLASH_CUV_DLY != null) && (pYFLASH_MAP_CUV_DLY != null))
            {
                pYFLASH_COV_DLY.itemlist.Clear();
                pYFLASH_MAP_COV_DLY.itemlist.Clear();
                pYFLASH_CUV_DLY.itemlist.Clear();
                pYFLASH_MAP_CUV_DLY.itemlist.Clear();
                for (int i = 0; i < 32; i++)
                {
                        str = string.Format("{0:D}ms", 125 * (i + 1));
                    pYFLASH_COV_DLY.itemlist.Add(str);
                    pYFLASH_MAP_COV_DLY.itemlist.Add(str);
                    pYFLASH_CUV_DLY.itemlist.Add(str);
                    pYFLASH_MAP_CUV_DLY.itemlist.Add(str);
                }
            }

            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_OV1_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_OV1_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_OV1_TH);
            if ((param != null)&&(param1!=null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:D}mv", 2560 + i * 10);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_OV1_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_OV1_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_OV1_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}ms", 500*(i+1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_OV1_RLS_HYS);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_OV1_RLS_HYS);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_OV1_RLS_HYS);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 64; i++)
                {
                    str = string.Format("{0:D}mV", 10 * i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_OV2PF_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_OV2PF_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_OV2PF_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}s", 1 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_UV_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_UV_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_UV_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 128; i++)
                {
                    str = string.Format("{0:D}mV", 1000 + 20 * i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_UV_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_UV_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_UV_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}ms", 500 * i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_UV_RLS_HYS);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_UV_RLS_HYS);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_UV_RLS_HYS);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 64; i++)
                {
                    str = string.Format("{0:D}mV", 20 * i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_BATUV_SHUTDOWN_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_BATUV_SHUTDOWN_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_BATUV_SHUTDOWN_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}s", 1 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_DSG_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_DSG_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_DSG_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}mV",250 + 125 * i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_CHG_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_CHG_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_CHG_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:F1}mV", 125 + 62.5 * i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_DOC1P_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_DOC1P_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_DOC1P_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 255; i++)
                {
                    str = string.Format("{0:D}mV", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_DOC1P_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_DOC1P_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_DOC1P_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 1; i < 64; i++)
                {
                    str = string.Format("{0:D}ms", 500 * (i+1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_DFETPF_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_DFETPF_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_DFETPF_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 1; i < 64; i++)
                {
                    str = string.Format("{0:D}ms", 500 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_CFETPF_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_CFETPF_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_CFETPF_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 1; i < 64; i++)
                {
                    str = string.Format("{0:D}ms", 250 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COC1P_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COC1P_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_COC1P_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                param.itemlist.Add("Disable");
                param1.itemlist.Add("Disable");
                param2.itemlist.Add("Disable");
                for (int i = 1; i < 255; i++)
                {
                    str = string.Format("{0:D}mV", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COC1P_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COC1P_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_COC1P_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}ms", 250 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_COC2P_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_COC2P_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_COC2P_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 255; i++)
                {
                    str = string.Format("{0:D}ms", 2 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_DOC2P_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_DOC2P_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_DOC2P_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 1024; i++)
                {
                    str = string.Format("{0:D}ms", 2 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_DOC2P_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_DOC2P_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_DOC2P_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0:D}mV", 10 * (i + 1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_SCP_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_SCP_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_SCP_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}us",16*i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_CELLOT_PF_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_CELLOT_PF_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_CELLOT_PF_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}s", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_FETOT_PF_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_FETOT_PF_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_FETOT_PF_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}s", i);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_CB_START_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_CB_START_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_CB_START_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}mV",3000 + i*20);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_UB_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_UB_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_UB_TH);
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
                    str = string.Format("{0:D}mV", i * 10);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_UB_DLY);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_UB_DLY);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_UB_DLY);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 64; i++)
                {
                    str = string.Format("{0:D}s", (i+1));
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            param = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_UB_CHK_TH);
            param1 = YFLASHParamlist.GetParameterByGuid(ElementDefine.YFLASH_MAP_UB_CHK_TH);
            param2 = OPParamlist.GetParameterByGuid(ElementDefine.OP_MAP_UB_CHK_TH);
            if ((param != null) && (param1 != null) && (param2 != null))
            {
                param.itemlist.Clear();
                param1.itemlist.Clear();
                param2.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:D}mV", 2560 + i*625);
                    param.itemlist.Add(str);
                    param1.itemlist.Add(str);
                    param2.itemlist.Add(str);
                }
            }
            #endregion
        }

        public void Physical2Hex(ref Parameter param)
        {
            m_dem_dm.Physical2Hex(ref param);
        }

        public void Hex2Physical(ref Parameter param)
        {
            m_dem_dm.Hex2Physical(ref param);
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
            for (byte i = 0; i < ElementDefine.YFLASH_MEMORY_SIZE; i++)
            {
                m_EFRegImgEX[i] = new COBRA_HWMode_Reg();
                m_EFRegImgEX[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EFRegImgEX[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;

                m_YFLASHRegImg[i + ElementDefine.YFLASH_MEMORY_OFFSET] = m_EFRegImgEX[i];
            }

            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            m_Vwkup.val = ElementDefine.PARAM_HEX_ERROR;
            m_Vwkup.err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
        }

        public UInt32 ReadFromRegImg(Parameter p, ref UInt16 wVal)
        {
            return m_dem_dm.ReadFromRegImg(p, ref wVal);
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
        #endregion
    }
}

