using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.MissionPeak
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
        internal const UInt16 OTP_MEMORY_SIZE = 0x10;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;

        internal const byte TEST_MODE_Reg = 0x20;
        internal const byte NORMAL_MODE_PWD = 0x53;
        internal const byte TEST_MODE_PWD = 0x54;
        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP = 2,
            PARAM_EXT_TEMP_20UA = 3,
            PARAM_CURRENT = 4,
            PARAM_EXT_TEMP = 5,

            PARAM_SCD_ILIM = 10,
            PARAM_SCD_PG_ADJ = 11,
            PARAM_VBAT_UV_CRIT_THRSH = 12,
            PARAM_VBAT_UV_HYS = 13,
            PARAM_VBAT_UV_WARN = 14,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_NORMAL_MODE = 0x90,
            SUB_TASK_TEST_MODE = 0x91,
            SUB_TASK_START_TRIM = 0x92,
            SUB_TASK_BURN = 0x93,
            SUB_TASK_FREEZE = 0x94,
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        #endregion

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion

        #region OTP参数GUID
        internal const UInt32 OTPElement = 0x00020000; //EFUSE参数起始地址
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        #endregion
    }
}
