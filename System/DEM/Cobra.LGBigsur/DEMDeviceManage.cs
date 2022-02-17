using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.LGBigsur
{
    public class DEMDeviceManage : IDEMLib2
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
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();
        public Dictionary<UInt32, TSMBbuffer> m_HwMode_Dic = new Dictionary<UInt32, TSMBbuffer>();
        public byte[] logAreaArray = new byte[1024];

        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region 定义Memory
        public byte[] InfoeFlash_Buffer = new byte[ElementDefine.InfoeFlash_Size];
        public byte[] SysFlash_Buffer = new byte[ElementDefine.SyseFlash_Size];
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
            return m_dem_bm.ReadWord(reg,ref pval);
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
            UInt32 bcmd = 0;
            ParamContainer paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.SBSElement);
            if (paramContainer == null) return;

            foreach (Parameter p in paramContainer.parameterlist)
            {
                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                p.tsmbBuffer.length = 4;
                if (m_HwMode_Dic.ContainsKey(bcmd)) continue;
                m_HwMode_Dic.Add(bcmd, p.tsmbBuffer);
            }

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

            Array.Clear(InfoeFlash_Buffer, 0, InfoeFlash_Buffer.Length);
            Array.Clear(SysFlash_Buffer, 0, SysFlash_Buffer.Length);
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
    }
}

