using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Blueway
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const int RETRY_COUNTER = 5;
        internal const UInt16 BLOCK_OPERATION_BYTES = 32;
        internal const UInt16 Log_Page_Size = 144;
        internal const UInt16 PARA_MEMORY_SIZE = 0x200;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt32 CommandMask = 0x0000FF00;
        internal const UInt32 CommandMask1 = 0x0000FFFF;

        #region MTP操作常量定义
        // MTP operation code
        internal const UInt16 ALLOW_WRT = 0x8000;
        internal const UInt16 ALLOW_WRT_MASK = 0x7FFF;
        internal const UInt16 MEM_MODE_MASK = 0xFFFC;

        // MTP control registers' addresses
        internal const byte MEM_DATA_HI_REG = 0xC0;
        internal const byte MEM_DATA_LO_REG = 0xC1;
        internal const byte MEM_ADDR_REG = 0xC2;
        internal const byte MEM_MODE_REG = 0xC3;

        // MTP Control Flags
        internal const UInt16 YFLASH_ATELOCK_MATCHED_FLAG = 0x0001;
        internal const UInt16 MEM_OP_REQ_FLAG = 0x03;
        #endregion

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_HANDSHAKE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_INVALID_BUFFER = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_CRC16_COMPARE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_RESET_MANUALLY = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_AUTHKEY_LEN_ILLEGAL = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_AUTHKEY_DATA_ILLEGAL = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        internal const UInt32 IDS_ERR_DEM_FAILED_AUTHKEY_COMPARE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        internal const UInt32 IDS_ERR_DEM_RECONNECT_CHARGER = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0009;
        #endregion
        public class BW_HMAC
        {
            public byte[] Authen_Key;
            public byte[] Random_Key;
            public BW_HMAC()
            {
                Authen_Key = new byte[16];
                Random_Key = new byte[20];
            }
        };

        internal enum MF_BLOCK_ACCESS : ushort
        {
            FV = 01,
            FGV = 02,
            RESET = 03,
            FUM = 04,
            DLREAD = 05,
            DLRESET = 06,
            SE = 07,
            DLS = 08
        }

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_PROJ_CURRENT = 3,
            PARAM_SIGNED0 = 4,
            PARAM_SIGNED = 5,
            PRJ_PARAM_OT_UT = 6,
            PARAM_DATE0 = 7,
            PARAM_RSENSE = 8,
            PARAM_STRING = 9,
            PARAM_STRING16 = 10,
            PARAM_PROJ_DOC2TH = 11,
            SBS_PARAM_OT_UT = 16,

            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }
        internal enum COBRA_MEMD : ushort
        {
            MPT_MEMD_DEFAULT = 0,
            MPT_MEMD_DIRECT_W,
            MPT_MEMD_INDIRECT_WR,
            APB_MEMD_DIRECT_W,
        }
        internal enum COBRA_MEMORY_OP_REQ : ushort
        {
            MEMORY_OP_REQ_DEFAULT = 0,
            MEMORY_OP_REQ_MTP_READ,
            MEMORY_OP_REQ_MTP_WRITE,
            MEMORY_OP_REQ_APB_WRITE,
        }

        public enum FILE_TYPE
        {
            FILE_HEX = 0x01,
            FILE_PARAM = 0x02,
            FILE_THERMAL_TABLE = 0x03,
            FILE_OCV_TABLE = 0x04,
            FILE_SELF_DISCH_TABLE = 0x05,
            FILE_RC_TABLE = 0x06,
            FILE_FD_TABLE = 0x07,
            FILE_FGLITE_TABLE = 0x08,
        }

        #region SBS参数GUID
        internal const UInt32 SBSElement = 0x00030000;
        internal const UInt32 BatteryMode = 0x00030320;
        internal const UInt32 BatteryStatus = 0x00031620;
        internal const UInt32 DesignCap = 0x00031800;
        internal const UInt32 DesignVolt = 0x00031900;
        internal const UInt32 SpecInfo = 0x00031A20;
        internal const UInt32 MfgDate = 0x00031B00;
        internal const UInt32 SerialNo = 0x00031C00;
        internal const UInt32 MfgName = 0x00032000;
        internal const UInt32 DevName = 0x00032100;
        internal const UInt32 DevChem = 0x00032200;
        internal const UInt32 MfgData = 0x00032300;
        internal const UInt32 FWVersion = 0x0003F901;
        internal const UInt32 FGVersion = 0x0003F902;
        #endregion

        #region Log参数GUID
        internal const UInt32 LogElement = 0x00020000;
        #endregion

        #region Project参数GUID
        internal const UInt32 Project_StartAddress = 0x00011000;
        internal const UInt32 ParameterArea_StartAddress = 0x6E00;
        internal const UInt32 Parameter_CheckSumAddress = 0x6E00;
        internal const UInt32 OFFSET_PARAM_VALUE_START = 0x6E04;        //TBD
        internal const UInt32 OFFSET_PARAM_VALUE_END = 0x6FC8;
        internal const UInt32 OFFSET_PARAM_CHECKSUM = 0x6E00;       //
        internal const UInt32 PrjParamElement = 0x000E0000; //Virtual参数起始地址    
        internal const UInt32 PARM_BCFG_RSENSEMAIN = 0x000E6E14;
        internal const UInt32 PARM_BCFG_CHGTH = 0x000E6E18;
        internal const UInt32 PARM_BCFG_DSGTH = 0x000E6E1C;
        internal const UInt32 PARM_PROT_DOC2MTH = 0x000E6E98;

        internal const UInt32 PARAM_MF_StartAddress = 0x6F3C;
        internal const UInt32 AuthenticationKey = 0x000E6FEC;
        internal const UInt32 PARM_BCFG_CRC = 0x000E6E00;
        #endregion

        #region Project Table GUID
        internal const UInt32 LUTTableArea_StartAddress = 0x6B00;
        internal const UInt32 LUTTable_SizeAddress = 0x6B04;
        #endregion
    }
}
