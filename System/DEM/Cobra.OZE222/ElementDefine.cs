using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.OZE222
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
        internal const byte TRIM_REG = 0x08;
        internal const byte TRIM0_REG = 0x04;
        internal const byte TRIM1_REG = 0x03;


        internal enum COMMAND : ushort
        {
            WritePassWord = 0,
            BURN = 1,
            FREEZE = 2,
            SwitchToECModeCV42CC5 = 3,
            SwitchToSWModeCV42CC2 = 4,
            SwitchToECModeCV44CC5 = 5,
            SwitchToSWModeCV44CC2 = 6,
        }
        internal enum SUBTYPE : ushort
        {
                DEFAULT = 0,
                VOLTAGE = 1,
                CURRENT,
                INT_TEMP,
                EXT_TEMP,
                CV = 5,
                WAKEUP_V,
                RC_HYS,
                VSM,
                CC,
                WAKEUP_C = 10,
                EOC,
                ILMT,
                WKT,
                CAR,
                EXT_TEMP_TABLE = 40,
                INT_TEMP_REFER = 41
        }

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 OTGOCP = 0x00030501;

        #endregion
        #region Virtual参数GUID
        internal const UInt32 VirtualElement = 0x000c0000;
        internal const UInt32 Pin8 = 0x000c0501;

        #endregion
    }
}
