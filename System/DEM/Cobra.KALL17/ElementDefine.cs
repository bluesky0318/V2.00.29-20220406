using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL17
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 EF_MEMORY_SIZE    = 0x10;
        internal const UInt16 EF_MEMORY_OFFSET = 0x80;
		/////////////////////////////////////////////////////////////
        internal const UInt16 EF_ATE_OFFSET = 0x60;
        internal const UInt16 EF_ATE_TOP = 0x66;
        internal const UInt16 ATE_CRC_OFFSET = 0x66;

        internal const UInt16 EF_USR_OFFSET = 0x67;
        internal const UInt16 EF_USR_TOP = 0x6f;
        internal const UInt16 USR_CRC_OFFSET = 0x6f;

        internal const UInt16 ATE_CRC_BUF_LEN = 27;     // 4 * 7 - 1
        internal const UInt16 USR_CRC_BUF_LEN = 35;     // 4 * 9 - 1

        internal const UInt16 CELL_OFFSET = 0x11;
        internal const UInt16 Vpack_wkup = 0x1a;
        internal const UInt16 CELL_TOP = 0x1E;
        internal const UInt16 CURRENT_OFFSET = 0x31;
        internal const UInt16 V800MV_OFFSET = 0x20;
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
            VOLTAGE = 1,
            INT_TEMP,
            EXT_TEMP,
            CURRENT = 4,
            CADC = 5,
            COULOMB_COUNTER = 6,
            WKUP,
            PRE_SET = 12,
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
        
        #region EFUSE参数GUID
        internal const UInt32 EFUSEElement = 0x00020000; //EFUSE参数起始地址

        internal const byte EF_RD_CMD = 0x30;
        internal const byte EF_WR_CMD = 0xc5;

        #endregion
        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellNum = 0x0003510d; //
        internal const UInt32 CellBase = 0x00030100; //
        internal const UInt32 CellCurrent = 0x00031200; //
        internal const UInt32 BASIC_CADC = 0x00033100; //
        internal const UInt32 TRIGGER_CADC = 0x00033800; //
        internal const UInt32 MOVING_CADC = 0x00033900; //

        internal const UInt32 OVP_H = 0x00035008;
        internal const UInt32 DOC1P = 0x00035100;
        internal const UInt32 COCP = 0x00035200;

        internal const byte OR_RD_CMD = 0x30;
        internal const byte OR_WR_CMD = 0xC5;
        #endregion

        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;

        internal const UInt32 OVP_E = 0x000c0001; //
        internal const UInt32 DOC1P_E = 0x000c0002; //
        internal const UInt32 COCP_E = 0x000c0003; //
        #endregion
    }
}
