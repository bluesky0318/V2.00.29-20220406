using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.NewTon.G2
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
            //set { m_PullupR = value; }
        }

        public struct isensStruct
        {
            public bool isens_slop_trim_bit6;
            public UInt16 raw_vadc_chop0_data;
            public UInt16 raw_vadc_chop1_data;
            public UInt16 v600mv_vadc_data;
            public UInt16 isens_slop_trim;
            public UInt16 isens_offset;
            public UInt16 isens_vadc_data;
            public UInt16 end_isens;
        };
        public isensStruct m_isens_struct;

        internal ParamContainer MTPParamlist = null;
        internal ParamContainer I2CParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_EpTrimRegImg = new COBRA_HWMode_Reg[ElementDefine.MTP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_I2CRegImg = new COBRA_HWMode_Reg[ElementDefine.I2C_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_eFlashCtrlImage = new COBRA_HWMode_Reg[ElementDefine.eFlashCtrl_Size];
        internal COBRA_HWMode_Reg[] m_I2CRegistersImage = new COBRA_HWMode_Reg[ElementDefine.I2CRegisters_Size];
        internal COBRA_HWMode_Reg[] m_TimerRegistersImage = new COBRA_HWMode_Reg[ElementDefine.TimerRegisters_Size];
        internal COBRA_HWMode_Reg[] m_WDTRegistersImage = new COBRA_HWMode_Reg[ElementDefine.WDTRegisters_Size];
        internal COBRA_HWMode_Reg[] m_UARTRegistersImage = new COBRA_HWMode_Reg[ElementDefine.UARTRegisters_Size];
        internal Dictionary<UInt32, Tuple<Parameter, Parameter>> m_guid_slope_offset = new Dictionary<UInt32, Tuple<Parameter, Parameter>>();
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();
        public List<byte> m_MTPSpecial_RegList = new List<byte>() {0x0F,0x13 };
        public Dictionary<byte, TSMBbuffer> m_MTPSpecial_RegDic = new Dictionary<byte, TSMBbuffer>();

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        public DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_UNLOCK_ERASE,"Failed to unlock erase!"},
            {ElementDefine.IDS_ERR_DEM_MAINBLOCK_ERASE,"Failed to Main block erase !"},
            {ElementDefine.IDS_ERR_DEM_PAGE_ERASE,"Failed to page erase !"},
            {ElementDefine.IDS_ERR_DEM_INFO_ERASE,"Failed to information area erase !"},
            {ElementDefine.IDS_ERR_DEM_SYS_ERASE,"Failed to system area erase !"},
            {ElementDefine.IDS_ERR_DEM_CRC16_DONE,"Failed to do CRC16!"},
            {ElementDefine.IDS_ERR_DEM_CRC16_COMPARE,"Failed to compare the CRC"},
            {ElementDefine.IDS_ERR_DEM_BLK_ACCESS,"Failed to do block operation"},
            {ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE,"Don't support to operate one parameter."},
            {ElementDefine.IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT,"For the command, the data number should be the multiple of 5."},
            {ElementDefine.IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT,"For the command, the data number should be the multiple of 4."},
        };
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            m_HwMode_RegList.Add(ElementDefine.EEPROMTRIMElement, m_EpTrimRegImg);
            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);
            m_HwMode_RegList.Add(ElementDefine.I2CElement, m_I2CRegImg);

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
            return m_dem_bm.BlockErase(ref bgworker);
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
            return m_dem_bm.ReadWord(reg, ref pval);
        }

        public UInt32 ReadDevice(ref TASKMessage bgworker)
        {
            return m_dem_bm.ReadDevice(ref bgworker);
        }

        public UInt32 WriteDevice(ref TASKMessage bgworker)
        {
            return m_dem_bm.WriteDevice(ref bgworker);
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            MTPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EEPROMTRIMElement);
            if (MTPParamlist == null) return;

            I2CParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.I2CElement);
            if (I2CParamlist == null) return;
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
            TSMBbuffer tsm = null;
            foreach (byte badd in m_MTPSpecial_RegList)
            {
                tsm = new TSMBbuffer();
                m_MTPSpecial_RegDic.Add(badd, tsm);
            }
            for (byte i = 0; i < ElementDefine.MTP_MEMORY_SIZE; i++)
            {
                m_EpTrimRegImg[i] = new COBRA_HWMode_Reg();
                m_EpTrimRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EpTrimRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (int i = 0; i < ElementDefine.I2C_MEMORY_SIZE; i++)
            {
                m_I2CRegImg[i] = new COBRA_HWMode_Reg();
                m_I2CRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_I2CRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (byte i = 0; i < ElementDefine.eFlashCtrl_Size; i++)
            {
                m_eFlashCtrlImage[i] = new COBRA_HWMode_Reg();
                m_eFlashCtrlImage[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_eFlashCtrlImage[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (byte i = 0; i < ElementDefine.I2CRegisters_Size; i++)
            {
                m_I2CRegistersImage[i] = new COBRA_HWMode_Reg();
                m_I2CRegistersImage[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_I2CRegistersImage[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (byte i = 0; i < ElementDefine.TimerRegisters_Size; i++)
            {
                m_TimerRegistersImage[i] = new COBRA_HWMode_Reg();
                m_TimerRegistersImage[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_TimerRegistersImage[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (byte i = 0; i < ElementDefine.WDTRegisters_Size; i++)
            {
                m_WDTRegistersImage[i] = new COBRA_HWMode_Reg();
                m_WDTRegistersImage[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_WDTRegistersImage[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (byte i = 0; i < ElementDefine.UARTRegisters_Size; i++)
            {
                m_UARTRegistersImage[i] = new COBRA_HWMode_Reg();
                m_UARTRegistersImage[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_UARTRegistersImage[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            m_guid_slope_offset.Add(0x00034500, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103E600), I2CParamlist.GetParameterByGuid(0x0103E900)));
            m_guid_slope_offset.Add(0x00034600, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103E700), I2CParamlist.GetParameterByGuid(0x0103EA00)));
            m_guid_slope_offset.Add(0x00034700, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103E800), I2CParamlist.GetParameterByGuid(0x0103EB00)));
            m_guid_slope_offset.Add(0x00034900, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103E808), I2CParamlist.GetParameterByGuid(0x0103EB08)));
            m_guid_slope_offset.Add(0x00034A00, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103E708), I2CParamlist.GetParameterByGuid(0x0103EA08)));
            m_guid_slope_offset.Add(0x00034B00, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103E608), I2CParamlist.GetParameterByGuid(0x0103E908)));
            m_guid_slope_offset.Add(0x00036A00, Tuple.Create(I2CParamlist.GetParameterByGuid(0x0103ED08), I2CParamlist.GetParameterByGuid(0x0103ED00)));
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
        public UInt32 GetINT25Ref(ref double ddata)
        {
            Int16 sval = 0;
            UInt32 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ret = m_dem_bm.UnLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = m_dem_bm.Block4BytesRead(0x6013, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = m_dem_bm.ReLockI2C();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            sval = (Int16)(wval >> 16);
            ddata = (sval * 0.0354); //左移4位
            return ret;
        }
    }
}

