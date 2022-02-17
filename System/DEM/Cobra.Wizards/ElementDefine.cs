using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.SD77060
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
        //internal const UInt16 EF_ATE_OFFSET = 0x10;
        //internal const UInt16 EF_ATE_TOP = 0x17;
        //internal const UInt16 ATE_CRC_OFFSET = 0x17;

        internal const UInt16 OP_MEMORY_SIZE = 0xFF;

        //internal const UInt16 OP_USR_OFFSET = 0x05;
        //internal const UInt16 OP_USR_TOP = 0x11;
        internal const Byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;

        internal const int RETRY_COUNTER = 15;
        //internal const byte WORKMODE_OFFSET = 0x18;
        //internal const byte MAPPINGDISABLE_OFFSET = 0x19;
        internal const UInt32 SectionMask = 0xFFFF0000;

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion
        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        //internal const UInt32 TRIGGER_CADC = 0x00033900; //
        #endregion
        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;
        #endregion
        #endregion
        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            ADC = 1,
            TMP = 2
        }

        #region Local ErrorCode
        //internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        #endregion

        //internal enum EFUSE_MODE : ushort
        //{
        //    NORMAL = 0,
        //    INTERNAL = 0x01,
        //    PROGRAM = 0x02,
        //}

        internal enum COMMAND : ushort
        {
            EXPERT_ENTER_SERIAL_MODE = 0x01,
            EXPERT_ENTER_PARALLEL_MODE = 0x02,
            EXPERT_ENTER_SHIP_MODE = 0x03,
            EXPERT_ENTER_SLEEP_MODE = 0x04,
            EXPERT_EXIT_SLEEP_MODE = 0x05,
            EXPERT_ENTER_TEST_MODE = 0x06,
            EXPERT_EXIT_TEST_MODE = 0x07,
            EXPERT_ENTER_FET_CTRL = 0x08,
            EXPERT_EXIT_FET_CTRL = 0x09,
            EXPERT_ENTER_TRIM_MODE = 0x0a,
            TRIM_SLOPE_TRIMMING = 0x0b,
            TRIM_OFFSET_TRIMMING = 0x0c,
            OPTIONS = 0xFFFF
        }
    }
}
