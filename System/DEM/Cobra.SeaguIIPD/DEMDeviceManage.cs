using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.SeaguIIPD
{
    public class DEMDeviceManage : IDEMLib2
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

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_EFRegImg = new COBRA_HWMode_Reg[ElementDefine.EP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OTPRegImg = new COBRA_HWMode_Reg[ElementDefine.OTP_MEMORY_SIZE];
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();
        public Dictionary<UInt32, COBRA_HWMode_Reg> m_OpImage_Dic = new Dictionary<UInt32, COBRA_HWMode_Reg>();

        public DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();
        //Key: Formula Name, Value: (Parameter Number, Parameter Serial)
        internal Dictionary<string, Tuple<int, string, string>> m_formula_dic = new Dictionary<string, Tuple<int, string, string>>
        {
            {"OPCS",new Tuple<int, string,string>(4,"Please input 4 parameters in order and separated by commas like Vcs1(mV),Vcs2(mV),V1(mV),V2(mV)","Gain,VOS") },
            {"ADC",new Tuple<int, string,string>(4,"Please input 4 parameters in order and separated by commas like V1(mV),V2(mV),ADC1,ADC2","ADC_Gain,ADC_Offset") },
            {"DACV",new Tuple<int, string,string>(4,"Please input 4 parameters in order and separated by commas like VOUT1(mV),VOUT2(mV),DACV1,DACV2","DACV_Gain,DACV_Offset") },
            {"DACC1",new Tuple<int, string,string>(4,"Please input 4 parameters in order and separated by commas like ViOUT1(mV),ViOUT2(mV),DACC1,DACC2","DACC_Gain,DACC_Offset") },
            {"DACC2",new Tuple<int, string, string>(4,"Please input 4 parameters in order and separated by commas like IOUT1(mA),IOUT2(mA),DACC1,DACC2","DACC_Gain,DACC_Offset") }
};
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_HW_CRC_TIMEOUT,"HW CRC count timeout!"},
            {ElementDefine.IDS_ERR_DEM_CRC_ERROR,"CRC32 check error."},
            {ElementDefine.IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT,"For the command, the data number should be the multiple of 5."},
            {ElementDefine.IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT,"For the command, the data number should be the multiple of 4."},
            {ElementDefine.IDS_ERR_DEM_EEPROM_BUSY,"EEPROM OPERATION BUSY,Please check!"},
            {ElementDefine.IDS_ERR_DEM_SW_RESET,"Failed to reset chip!"},
        };
        #endregion

        #region Parameters
        public Parameter pURcs1 = new Parameter();
        public Parameter pURcs2 = new Parameter();
        public Parameter pUOTG = new Parameter();
        public Parameter pUCSA_GAIN_SEL = new Parameter();
        public Parameter pREFCV = new Parameter();
        public Parameter pREFCC = new Parameter();
        public Parameter pCHANNEL6 = new Parameter();
        public Parameter pCHANNEL7 = new Parameter();
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.EEPROMTRIMElement, m_EFRegImg);
            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);

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
            return m_dem_bm.ReadDevice(ref msg);
        }
        public UInt32 WriteDevice(ref TASKMessage msg)
        {
            return m_dem_bm.WriteDevice(ref msg);
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            EFParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EEPROMTRIMElement);
            if (EFParamlist == null) return;

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.Port1SystemElement);
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

        private void InitialImgReg()
        {
            Reg regLow = null;
            string str = string.Empty;
            Parameter param = null;
            COBRA_HWMode_Reg HWMode_Reg = null;
            for (UInt32 i = 0; i < ElementDefine.EP_MEMORY_SIZE; i++)
            {
                m_EFRegImg[i] = new COBRA_HWMode_Reg();
                m_EFRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EFRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (UInt32 i = 0; i < ElementDefine.OTP_MEMORY_SIZE; i++)
            {
                m_OTPRegImg[i] = new COBRA_HWMode_Reg();
                m_OTPRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OTPRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            #region BBCTRLSystemElement
            ParamContainer paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.BBCTRLSystemElement);
            if (paramContainer == null) return;
            foreach (Parameter p in paramContainer.parameterlist)
            {
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    if (dic.Key.Equals("Low"))
                    {
                        regLow = dic.Value;
                        HWMode_Reg = new COBRA_HWMode_Reg();
                        HWMode_Reg.wval = 0;
                        HWMode_Reg.err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        if (!m_OpImage_Dic.ContainsKey(regLow.u32Address))
                            m_OpImage_Dic.Add(regLow.u32Address, HWMode_Reg);
                    }
                }
            }
            param = paramContainer.GetParameterByGuid(ElementDefine.BBCTRL_REF_CV);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 4096; i++)
                {
                    str = string.Format("{0:F3}V", i * 6.0 / 1000.0);
                    param.itemlist.Add(str);
                }
            }
            param = paramContainer.GetParameterByGuid(ElementDefine.BBCTRL_REF_CC);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 1024; i++)
                {
                    str = string.Format("{0:F3}A", i * 9.6 / 1000.0);
                    param.itemlist.Add(str);
                }
            }
            #endregion

            paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.APBExpertElement);
            if (paramContainer == null) return;
            foreach (Parameter p in paramContainer.parameterlist)
            {
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    if (dic.Key.Equals("Low"))
                    {
                        regLow = dic.Value;
                        HWMode_Reg = new COBRA_HWMode_Reg();
                        HWMode_Reg.wval = 0;
                        HWMode_Reg.err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        if (!m_OpImage_Dic.ContainsKey(regLow.u32Address))
                            m_OpImage_Dic.Add(regLow.u32Address, HWMode_Reg);
                    }
                }
            }
            paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.PDCTRLExpertElement);
            if (paramContainer == null) return;
            foreach (Parameter p in paramContainer.parameterlist)
            {
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    if (dic.Key.Equals("Low"))
                    {
                        regLow = dic.Value;
                        HWMode_Reg = new COBRA_HWMode_Reg();
                        HWMode_Reg.wval = 0;
                        HWMode_Reg.err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        if (!m_OpImage_Dic.ContainsKey(regLow.u32Address))
                            m_OpImage_Dic.Add(regLow.u32Address, HWMode_Reg);
                    }
                }
            }

            paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.ARMExpertElement);
            if (paramContainer == null) return;
            foreach (Parameter p in paramContainer.parameterlist)
            {
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    if (dic.Key.Equals("Low"))
                    {
                        regLow = dic.Value;
                        HWMode_Reg = new COBRA_HWMode_Reg();
                        HWMode_Reg.wval = 0;
                        HWMode_Reg.err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        if (!m_OpImage_Dic.ContainsKey(regLow.u32Address))
                            m_OpImage_Dic.Add(regLow.u32Address, HWMode_Reg);
                    }
                }
            }
        }

        private void InitParameters()
        {
            string str = string.Empty;

            ParamContainer pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.UserConfigElement);
            pURcs1 = pc.GetParameterByGuid(ElementDefine.UserConfig_Rcs1);
            pURcs2 = pc.GetParameterByGuid(ElementDefine.UserConfig_Rcs2);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.BBCTRLSystemElement);
            pUOTG = pc.GetParameterByGuid(ElementDefine.UserConfig_OTG);
            pREFCV = pc.GetParameterByGuid(ElementDefine.UserConfig_REF_CV);
            pREFCC = pc.GetParameterByGuid(ElementDefine.UserConfig_REF_CC);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.PDCTRLExpertElement);
            pUCSA_GAIN_SEL = pc.GetParameterByGuid(ElementDefine.UserConfig_CSA_GAIN_SEL);
            pCHANNEL6 = pc.GetParameterByGuid(ElementDefine.UserConfig_CHANNEL6);
            pCHANNEL7 = pc.GetParameterByGuid(ElementDefine.UserConfig_CHANNEL7);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OTPTRIMElement);
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
    }
}

