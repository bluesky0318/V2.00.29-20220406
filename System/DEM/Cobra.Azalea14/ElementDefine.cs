using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Azalea14
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        #region Chip Constant
        //internal const UInt16 EF_MEMORY_SIZE = 0x10;
        //internal const UInt16 EF_MEMORY_OFFSET = 0x10;
        internal const UInt16 EF_ATE_OFFSET = 0x10;
        internal const UInt16 EF_ATE_TOP = 0x17;
        internal const UInt16 ATE_CRC_OFFSET = 0x17;

        internal const UInt16 OP_MEMORY_SIZE = 0xFF;

        internal const UInt16 OP_USR_OFFSET = 0x05;
        internal const UInt16 OP_USR_TOP = 0x11;
        internal const Byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;

        internal const int RETRY_COUNTER = 15;
        internal const UInt16 CADC_RETRY_COUNT = 30;
        internal const byte WORKMODE_OFFSET = 0x18;
        internal const byte MAPPINGDISABLE_OFFSET = 0x19;
        internal const UInt32 SectionMask = 0xFFFF0000;

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion
        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 TRIGGER_CADC = 0x00033900; //
        internal const UInt32 MOVING_CADC = 0x00031700; //
        internal const UInt32 COULOMB_COUNTER = 0x00033a00; //
        internal const UInt32 THM0 = 0x00037600; //
        internal const UInt32 THM1 = 0x00037700; //
        internal const UInt32 THM_CRRT_SEL = 0x00031100; //
        internal const UInt32 THM0_OT_TH = 0x0003A008; //
        internal const UInt32 THM0_OTR_TH = 0x0003A000; //
        internal const UInt32 THM1_OT_TH = 0x0003A108; //
        internal const UInt32 THM1_OTR_TH = 0x0003A100; //
        internal const UInt32 CellNum = 0x0003080C; //
        #endregion
        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;
        #endregion
        #endregion
        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            INT_TEMP = 1,
            EXT_TEMP = 2,
            VOLTAGE = 3,
            SAR_CURRENT = 4,
            CADC = 5,
            COULOMB_COUNTER = 6,
            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        //internal const UInt32 IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        //internal const UInt32 IDS_ERR_DEM_ACTIVE_MODE_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        #endregion

        internal enum EFUSE_MODE : ushort
        {
            NORMAL = 0,
            INTERNAL = 0x01,
            PROGRAM = 0x02,
        }

        internal enum COMMAND : ushort
        {
            TRIM_SLOP = 5,
            //WATCH_DOG = 6,
            REGISTER_CONFIG_WRITE_WITH_PASSWORD = 7,
            REGISTER_CONFIG_READ = 9,
            REGISTER_CONFIG_SAVE_HEX = 8,
            SCS = 0x31,
            EXPERT_AZ10D_WAKEUP = 0xFF,
            SCAN_OPTIONS = 0xFFFF
        }
        public enum CADC_MODE : byte
        {
            DISABLE = 0,
            MOVING = 1,
            TRIGGER = 2,
        }
    }
}
