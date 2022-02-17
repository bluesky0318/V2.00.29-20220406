using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;

namespace Cobra.FWSeaguIIPD
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

        private DEMBehaviorManage m_dem_bm = null;
        private DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_CMD_SUCCESS,"Command is executed successfully!"},
            {ElementDefine.IDS_ERR_DEM_PREFIX_ERROR,"No Prefix error."},
            {ElementDefine.IDS_ERR_DEM_CRC_ERROR,"CRC32 check error."},
            {ElementDefine.IDS_ERR_DEM_END_ERROR,"No end of package message error."},
            {ElementDefine.IDS_ERR_DEM_NUMBER_ERROR,"Number of data bigger than 250."},
            {ElementDefine.IDS_ERR_DEM_INVALID_COMMAND,"Invalid command."},
            {ElementDefine.IDS_ERR_DEM_ADDR_BOUNDARY_ERR,"Address should be with 4 bytes boundary."},
            {ElementDefine.IDS_ERR_DEM_ADDR_NOT_SUPPOUT_ERR,"Not support input address."},
            {ElementDefine.IDS_ERR_DEM_DATA_OUT_OF_RANGE,"For read/write data command, the data is out of range."},
            {ElementDefine.IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT,"For write command, the data number is not 32 or 128."},
            {ElementDefine.IDS_ERR_DEM_WRITE_DISABLE,"For write command, didn’t setup write enable operation."},
            {ElementDefine.IDS_ERR_DEM_READ_DISABLE,"For read command, didn’t setup read enable operation."},
            {ElementDefine.IDS_ERR_DEM_HEX_FILE_SIZE,"The bin file size is illegal." },
            {ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR,"The prefix error on the GoodCRC commmand" },
            {ElementDefine.IDS_ERR_DEM_CMD_MISMATCH,"The command mismatch between send and receive buffer" },
            {ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE,"The package received valid data size error" },
            {ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_TOTAL_SIZE,"The package received total size error" },
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
            switch (busoptions.BusType)
            {
                case BUS_TYPE.BUS_TYPE_I2C:
                    m_dem_bm = new Behavior.I2CBehavior();
                    break;
                case BUS_TYPE.BUS_TYPE_RS232:
                    m_dem_bm = new Behavior.UartBehavior();
                    break;
            }
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
            return m_dem_bm.Erase(ref bgworker);
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

        public UInt32 ReadDevice(ref TASKMessage bgworker)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 WriteDevice(ref TASKMessage bgworker)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;
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
            return OPParamlist.GetParameterByGuid(guid);
        }

        private void InitialImgReg()
        {
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
    }
}

