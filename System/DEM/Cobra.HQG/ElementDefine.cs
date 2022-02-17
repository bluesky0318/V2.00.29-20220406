using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.HQG
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const int RETRY_COUNTER            = 15;
        internal const UInt16 OP_MEMORY_SIZE        = 0xff;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;


        internal enum COMMAND : ushort
        {
            DGB = 0,
            BURN = 1,
            FREEZE = 2
        }
        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
                PARAM_DEFAULT = 0,
                PARAM_VOLTAGE = 1,
                PARAM_CURRENT,
                PARAM_INT_TEMP,
                PARAM_EXT_TEMP,
                PARAM_CV = 5,
                PARAM_WAKEUP_V,
                PARAM_RC_HYS,
                PARAM_VSM,
                PARAM_CC,
                PARAM_WAKEUP_C = 10,
                PARAM_EOC,
                PARAM_VICL,
                PARAM_WKT,
                PARAM_CAR,
                PARAM_EXT_TEMP_TABLE = 40,
                PARAM_INT_TEMP_REFER = 41
        }

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;

        #endregion
    }
}
