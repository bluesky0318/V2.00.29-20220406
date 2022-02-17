using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL16H
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const byte   OR_RD_CMD = 0x30;
        internal const byte   OR_WR_CMD = 0xC5;
        internal const UInt16 YFLASH_MEMORY_SIZE    = 0x10;
        internal const UInt16 YFLASH_MEMORY_OFFSET = 0x80;
		/////////////////////////////////////////////////////////////
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -9999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 SPI_RETRY_COUNT = 10;
        internal const UInt16 CADC_RETRY_COUNT = 30;
        internal const UInt16 CMD_SECTION_SIZE = 3;
        // EFUSE control registers' addresses
        internal const byte WORKMODE_OFFSET = 0x70;

        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_ACTIVE_MODE_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_CFET_ON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_CFET_OFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_DFET_ON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_DFET_OFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        #endregion

        internal enum WORK_MODE : ushort
        {
            NORMAL = 0,
            INTERNAL = 0x01,
            PROGRAM = 0x02,
            //EFUSE_WORKMODE_MAPPING = 0x03
        }

        internal enum COMMAND : ushort
        {
            SLOP_TRIM = 5,
            STANDBY_MODE = 6,
            ACTIVE_MODE = 7,
            SHUTDOWN_MODE = 8,
            CFET_ON = 9,
            DFET_ON = 10,
            CFET_OFF = 11,
            DFET_OFF = 12,
            TRIGGER_8_CURRENT_4 = 13,
            TRIGGER_8_CURRENT_8 = 14,
            TRIGGER_8_CURRENT_1 = 15,
            ATE_CRC_CHECK = 17,
            STANDBY_THEN_ACTIVE_100MS = 18,
            ACTIVE_THEN_STANDBY_100MS = 19,
            STANDBY_THEN_ACTIVE_50MS = 20,
            ACTIVE_THEN_STANDBY_50MS = 21,
            STANDBY_THEN_ACTIVE_30MS = 22,
            ACTIVE_THEN_STANDBY_30MS = 23,
            STANDBY_THEN_ACTIVE_20MS = 24,
            ACTIVE_THEN_STANDBY_20MS = 25,
            OPTIONS = 0xFFFF
        }

        internal enum SAR_MODE : byte
        {
            TRIGGER_1 = 0,
            TRIGGER_8 = 1,
            AUTO_1 = 2,
            AUTO_8 = 3,
            TRIGGER_8_TIME_CURRENT_SCAN = 4,
            DISABLE = 5
        }

        public enum CADC_MODE : byte
        {
            DISABLE = 0,
            MOVING = 1,
            TRIGGER = 2,
        }

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpETRx = TemperatureElement + 0x00;
        #endregion
        internal const UInt32 SectionMask = 0xffff0000;
        
        #region YFLASH参数GUID
        internal const UInt32 YFLASHElement = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 YFLASH_COV_TH = 0x0002C000;
        internal const UInt32 YFLASH_BAT_TYPE = 0x0002C007;
        internal const UInt32 YFLASH_COVR_HYS = 0x0002C008;
        internal const UInt32 YFLASH_COV_DLY = 0x0002C00B;
        internal const UInt32 YFLASH_COVUV_SCAN = 0x0002C106;
        internal const UInt32 YFLASH_CUV_DLY = 0x0002C10B;
        internal const UInt32 YFLASH_OV1_TH = 0x0002C200;
        internal const UInt32 YFLASH_OV1_DLY = 0x0002C208;
        internal const UInt32 YFLASH_OV1_RLS_HYS = 0x0002C300;
        internal const UInt32 YFLASH_OV2PF_DLY = 0x0002C408;
        internal const UInt32 YFLASH_UV_TH = 0x0002C500;
        internal const UInt32 YFLASH_UV_DLY = 0x0002C508;
        internal const UInt32 YFLASH_UV_RLS_HYS = 0x0002C600;
        internal const UInt32 YFLASH_BATUV_SHUTDOWN_DLY = 0x0002C708;
        internal const UInt32 YFLASH_DSG_TH = 0x0002C800;
        internal const UInt32 YFLASH_CHG_TH = 0x0002C808;
        internal const UInt32 YFLASH_DOC1P_TH = 0x0002C900;
        internal const UInt32 YFLASH_DOC1P_DLY = 0x0002C908;
        internal const UInt32 YFLASH_DFETPF_DLY = 0x0002CA08;
        internal const UInt32 YFLASH_CFETPF_DLY = 0x0002CB08;
        internal const UInt32 YFLASH_COC1P_TH = 0x0002CC00;
        internal const UInt32 YFLASH_COC1P_DLY = 0x0002CC08;
        internal const UInt32 YFLASH_COC2P_DLY = 0x0002CD00;
        internal const UInt32 YFLASH_DOC2P_DLY = 0x0002CE00;
        internal const UInt32 YFLASH_DOC2P_TH = 0x0002CE0C;
        internal const UInt32 YFLASH_SCP_DLY = 0x0002CF00;
        internal const UInt32 YFLASH_CELLOT_PF_DLY = 0x0002D80A;
        internal const UInt32 YFLASH_FETOT_PF_DLY = 0x0002D90A;
        internal const UInt32 YFLASH_CB_START_TH = 0x0002DB00;
        internal const UInt32 YFLASH_UB_TH = 0x0002DC00;
        internal const UInt32 YFLASH_UB_DLY = 0x0002DC08;
        internal const UInt32 YFLASH_UB_CHK_TH = 0x0002DD00;

        internal const UInt32 YFLASH_MAP_COV_TH = 0x0002E000;
        internal const UInt32 YFLASH_MAP_BAT_TYPE = 0x0002E007;
        internal const UInt32 YFLASH_MAP_COVR_HYS = 0x0002E008;
        internal const UInt32 YFLASH_MAP_COV_DLY = 0x0002E00B;
        internal const UInt32 YFLASH_MAP_COVUV_SCAN = 0x0002E106;
        internal const UInt32 YFLASH_MAP_CUV_DLY = 0x0002E10B;
        internal const UInt32 YFLASH_MAP_OV1_TH = 0x0002E200;
        internal const UInt32 YFLASH_MAP_OV1_DLY = 0x0002E208;
        internal const UInt32 YFLASH_MAP_OV1_RLS_HYS = 0x0002E300;
        internal const UInt32 YFLASH_MAP_OV2PF_DLY = 0x0002E408;
        internal const UInt32 YFLASH_MAP_UV_TH = 0x0002E500;
        internal const UInt32 YFLASH_MAP_UV_DLY = 0x0002E508;
        internal const UInt32 YFLASH_MAP_UV_RLS_HYS = 0x0002E600;
        internal const UInt32 YFLASH_MAP_BATUV_SHUTDOWN_DLY = 0x0002E708;
        internal const UInt32 YFLASH_MAP_DSG_TH = 0x0002E800;
        internal const UInt32 YFLASH_MAP_CHG_TH = 0x0002E808;
        internal const UInt32 YFLASH_MAP_DOC1P_TH = 0x0002E900;
        internal const UInt32 YFLASH_MAP_DOC1P_DLY = 0x0002E908;
        internal const UInt32 YFLASH_MAP_DFETPF_DLY = 0x0002EA08;
        internal const UInt32 YFLASH_MAP_CFETPF_DLY = 0x0002EB08;
        internal const UInt32 YFLASH_MAP_COC1P_TH = 0x0002EC00;
        internal const UInt32 YFLASH_MAP_COC1P_DLY = 0x0002EC08;
        internal const UInt32 YFLASH_MAP_COC2P_DLY = 0x0002ED00;
        internal const UInt32 YFLASH_MAP_DOC2P_DLY = 0x0002EE00;
        internal const UInt32 YFLASH_MAP_DOC2P_TH = 0x0002EE0C;
        internal const UInt32 YFLASH_MAP_SCP_DLY = 0x0002EF00;
        internal const UInt32 YFLASH_MAP_CELLOT_PF_DLY = 0x0002F80A;
        internal const UInt32 YFLASH_MAP_FETOT_PF_DLY = 0x0002F90A;
        internal const UInt32 YFLASH_MAP_CB_START_TH = 0x0002FB00;
        internal const UInt32 YFLASH_MAP_UB_TH = 0x0002FC00;
        internal const UInt32 YFLASH_MAP_UB_DLY = 0x0002FC08;
        internal const UInt32 YFLASH_MAP_UB_CHK_TH = 0x0002FD00;

        internal const byte EF_RD_CMD = 0x30;
        internal const byte EF_WR_CMD = 0xc5;

        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 OP_MAP_OV1_TH = 0x0003E200;
        internal const UInt32 OP_MAP_OV1_DLY = 0x0003E208;
        internal const UInt32 OP_MAP_OV1_RLS_HYS = 0x0003E300;
        internal const UInt32 OP_MAP_OV2PF_DLY = 0x0003E408;
        internal const UInt32 OP_MAP_UV_TH = 0x0003E500;
        internal const UInt32 OP_MAP_UV_DLY = 0x0003E508;
        internal const UInt32 OP_MAP_UV_RLS_HYS = 0x0003E600;
        internal const UInt32 OP_MAP_BATUV_SHUTDOWN_DLY = 0x0003E708;
        internal const UInt32 OP_MAP_DSG_TH = 0x0003E800;
        internal const UInt32 OP_MAP_CHG_TH = 0x0003E808;
        internal const UInt32 OP_MAP_DOC1P_TH = 0x0003E900;
        internal const UInt32 OP_MAP_DOC1P_DLY = 0x0003E908;
        internal const UInt32 OP_MAP_DFETPF_DLY = 0x0003EA08;
        internal const UInt32 OP_MAP_CFETPF_DLY = 0x0003EB08;
        internal const UInt32 OP_MAP_COC1P_TH = 0x0003EC00;
        internal const UInt32 OP_MAP_COC1P_DLY = 0x0003EC08;
        internal const UInt32 OP_MAP_COC2P_DLY = 0x0003ED00;
        internal const UInt32 OP_MAP_DOC2P_DLY = 0x0003EE00;
        internal const UInt32 OP_MAP_DOC2P_TH = 0x0003EE0C;
        internal const UInt32 OP_MAP_SCP_DLY = 0x0003EF00;
        internal const UInt32 OP_MAP_CELLOT_PF_DLY = 0x0003F80A;
        internal const UInt32 OP_MAP_FETOT_PF_DLY = 0x0003F90A;
        internal const UInt32 OP_MAP_CB_START_TH = 0x0003FB00;
        internal const UInt32 OP_MAP_UB_TH = 0x0003FC00;
        internal const UInt32 OP_MAP_UB_DLY = 0x0003FC08;
        internal const UInt32 OP_MAP_UB_CHK_TH = 0x0003FD00;

        #endregion

        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;

        internal const UInt32 OVP_E = 0x000c0001; //
        internal const UInt32 DOC1P_E = 0x000c0002; //
        internal const UInt32 COCP_E = 0x000c0003; //
        #endregion
    }
}
