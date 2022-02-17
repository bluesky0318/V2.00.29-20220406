using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.FWNewTon.G2
{
    public class DEMDeviceManage : IDEMLib2
    {
        #region 定义参数subtype枚举类型
        private double m_Proj_Rsense;
        internal double Proj_Rsense
        {
            get
            {
                if (m_Proj_Rsense == 0) return 2500.0;
                else return m_Proj_Rsense;
            }
            set { m_Proj_Rsense = value; }
        }

        internal Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
        internal Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;
        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();

        public Dictionary<UInt32, TSMBbuffer> m_SBS_CMD_Dic = new Dictionary<UInt32, TSMBbuffer>(); //SBS SFL
        public Dictionary<UInt32, UInt32> m_SBS2Offset_Mermory_Map = new Dictionary<UInt32, UInt32>();

        internal COBRA_HWMode_Reg[] m_VirtualRegImg = new COBRA_HWMode_Reg[ElementDefine.VIRTUAL_MEMORY_SIZE];
        internal byte[] m_ProjParamImg = new byte[ElementDefine.PARA_MEMORY_SIZE];
        public byte[] logAreaArray = new byte[1024];
        public byte[] logEntireAreaArray = new byte[3*1024];
        #endregion

        #region 定义Memory
        public byte[] InfoeFlash_Buffer = new byte[ElementDefine.InfoeFlash_Size];
        public byte[] SysFlash_Buffer = new byte[ElementDefine.SyseFlash_Size];
        public byte[] ParameterPage_Buffer = new byte[ElementDefine.ParameterPage_Size];
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
            {ElementDefine.IDS_ERR_DEM_SYS_BACKUP,"Failed to backup trimming data on system area."},
            {ElementDefine.IDS_ERR_DEM_EEPROM_RMP,"Failed to remap EEPROM status."},
            {ElementDefine.IDS_ERR_DEM_RMP_FLAG,"Failed to clear remap EEPROM flag."},

            {ElementDefine.IDS_ERR_DEM_Received_Except,"Make sure sub-command value is 0x70, 0x71, 0x72, and 0x73."},
            {ElementDefine.IDS_ERR_DEM_NEnough_Pdefined,"Make sure number of data after sub-command is passed as definition."},
            {ElementDefine.IDS_ERR_DEM_Erase_UserCode,"User code cannot be erased by bootloader, if not, please contact Developer."},
            {ElementDefine.IDS_ERR_DEM_Reset_BW_UserCode,"User code is defined in ECB file, and all data should be send to bootloader."},
            {ElementDefine.IDS_ERR_DEM_OffsetAddr,"Offset address is defined in ECB file, and it should be send to bootloader correctly."},
            {ElementDefine.IDS_ERR_DEM_Write_UserCode,"Read back of user code value is not same with writing value, needs to re-try to write."},
            {ElementDefine.IDS_ERR_DEM_PEC,"Package checksum of single I2C transaction is wrong, needs to re-send same command."},
            {ElementDefine.IDS_ERR_DEM_Invalid_OffsetAddr,"Make sure Offset is compliant with 32-bits format."},
            {ElementDefine.IDS_ERR_DEM_Handshake,"Handshake data is not sent correctly, or doing write command before handshake command."},
            {ElementDefine.IDS_ERR_DEM_FLASH_CRC,"CRC checksum value of new user code is not matched, needs to re-write and make sure data is correct."},
            {ElementDefine.IDS_ERR_DEM_DOWNLOAD_SUCCESS,"Successful to download project image into flash."},
        };
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

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
        }

        private void InitialImgReg()
        {
            UInt32 bcmd = 0;
            TSMBbuffer tsmb = null;
             ParamContainer paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.SBSElement);
            if (paramContainer == null) return;

            foreach (Parameter p in paramContainer.parameterlist)
            {
                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                if (bcmd == ElementDefine.SBS_UNSEAL_CHIP)
                    p.tsmbBuffer.length = 32;
                else
                    p.tsmbBuffer.length = 4;
                if (m_SBS_CMD_Dic.ContainsKey(bcmd)) continue;
                m_SBS_CMD_Dic.Add(bcmd, p.tsmbBuffer);
            }

            //Insert the Write SBS Command into the Dic
            if (!m_SBS_CMD_Dic.ContainsKey(ElementDefine.SBS_EXTENDEDCOMMAND))
            {
                tsmb = new TSMBbuffer();
                tsmb.length = 2;
                m_SBS_CMD_Dic.Add(ElementDefine.SBS_EXTENDEDCOMMAND, tsmb);
            }

            for (byte i = 0; i < ElementDefine.VIRTUAL_MEMORY_SIZE; i++)
            {
                m_VirtualRegImg[i] = new COBRA_HWMode_Reg();
                m_VirtualRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_VirtualRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            #region
            m_SBS2Offset_Mermory_Map.Clear();
            m_SBS2Offset_Mermory_Map.Add(0x00023C00, 0x000001D8);
            m_SBS2Offset_Mermory_Map.Add(0x00023D00, 0x000001DC);
            m_SBS2Offset_Mermory_Map.Add(0x00026100, 0x000001E0);
            m_SBS2Offset_Mermory_Map.Add(0x00070A00, 0x000001E4);
            m_SBS2Offset_Mermory_Map.Add(0x00070A08, 0x000001EC);
            #endregion

            Array.Clear(m_ProjParamImg, 0, m_ProjParamImg.Length);
            Array.Clear(InfoeFlash_Buffer, 0, InfoeFlash_Buffer.Length);
            Array.Clear(SysFlash_Buffer, 0, SysFlash_Buffer.Length);
            Array.Clear(ParameterPage_Buffer, 0, ParameterPage_Buffer.Length);
        }
    }
}

