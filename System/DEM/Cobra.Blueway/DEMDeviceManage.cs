using System;
using System.Collections.Generic;
using System.Reflection;
using Cobra.Common;

namespace Cobra.Blueway
{
    public class DEMDeviceManage : IDEMLib2
    {
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

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;
        internal ParamContainer m_Project_ParamContainer = null;
        internal DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        internal DEMDataManage m_dem_dm = new DEMDataManage();
        internal ElementDefine.BW_HMAC m_hMAC = new ElementDefine.BW_HMAC();
        public Dictionary<UInt32, TSMBbuffer> m_SBSMode_Dic = new Dictionary<UInt32, TSMBbuffer>();
        public Dictionary<UInt16, TSMBbuffer> m_F9Mode_Dic = new Dictionary<UInt16, TSMBbuffer>();

        public byte[] m_ProjParamImg = new byte[ElementDefine.PARA_MEMORY_SIZE];
        public byte[] logAreaArray = new byte[ElementDefine.Log_Page_Size];
        public byte[] logEntireAreaArray = new byte[3 * ElementDefine.Log_Page_Size];
        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_HANDSHAKE,"Failed to handshake with Bootloader!"},
            {ElementDefine.IDS_ERR_DEM_INVALID_BUFFER,"Invalid data buffer!"},
            {ElementDefine.IDS_ERR_DEM_CRC16_COMPARE,"Failed to compare the CRC!"},
            {ElementDefine.IDS_ERR_DEM_RESET_MANUALLY,"Please reset the chip manually!"},
            {ElementDefine.IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE,"Don't support to operate one parameter."},
            {ElementDefine.IDS_ERR_DEM_AUTHKEY_LEN_ILLEGAL,"The authentication key should be the unique 128bit."},
            {ElementDefine.IDS_ERR_DEM_AUTHKEY_DATA_ILLEGAL,"The authentication key should be the hex char."},
            {ElementDefine.IDS_ERR_DEM_FAILED_AUTHKEY_COMPARE,"Failed to verify the authentication" },
            {ElementDefine.IDS_ERR_DEM_RECONNECT_CHARGER,"Please Re-connect the charger." }
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
            m_Project_ParamContainer = m_SFLs_ParamlistContainer.GetParameterListByName("Project");
            m_Project_ParamContainer.parameterlist.CollectionChanged += ProjectParameterlist_CollectionChanged;
        }

        private void InitialImgReg()
        {
            UInt32 bcmd = 0;
            TSMBbuffer tsmBuffer = null;
            ParamContainer paramContainer = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.SBSElement);
            if (paramContainer == null) return;

            m_SBSMode_Dic.Clear();
            m_F9Mode_Dic.Clear();
            foreach (Parameter p in paramContainer.parameterlist)
            {
                bcmd = (byte)((p.guid & ElementDefine.CommandMask) >> 8);
                if ((bcmd == 0x20) | (bcmd == 0x21) | (bcmd == 0x22) | (bcmd == 0x23))
                    p.tsmbBuffer.length = 32;
                else
                    p.tsmbBuffer.length = 2;
                if (m_SBSMode_Dic.ContainsKey(bcmd)) continue;
                m_SBSMode_Dic.Add(bcmd, p.tsmbBuffer);
            }
            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 4;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.FV, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 4;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.FGV, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 6;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.RESET, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 6;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.FUM, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 32;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.DLREAD, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 10;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.DLRESET, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 4;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.SE, tsmBuffer);

            tsmBuffer = new TSMBbuffer();
            tsmBuffer.length = 4;
            m_F9Mode_Dic.Add((UInt16)ElementDefine.MF_BLOCK_ACCESS.DLS, tsmBuffer);
        }

        public Parameter GetParameterByGuidFromProject(UInt32 guid)
        {
            if (m_Project_ParamContainer != null)
                return m_Project_ParamContainer.GetParameterByGuid(guid);
            else
                return null;
        }

        private void ProjectParameterlist_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Parameter newItem in e.NewItems)
                {
                    switch(newItem.guid)
                    {
                        case ElementDefine.PARM_BCFG_RSENSEMAIN:
                            m_Proj_Rsense = newItem.phydata * 1000.0;
                            break;
                        case ElementDefine.PARM_PROT_DOC2MTH:
                            newItem.itemlist.Clear();
                            newItem.itemlist.Add("Disable");
                            for (int i = 1; i < 25; i++)
                                newItem.itemlist.Add(string.Format("{0:F2}mA", ((i - 1) * 10 + 20) * 1000000.0 / m_Proj_Rsense));
                            break;
                    }
                }
            }
        }

    }
}

