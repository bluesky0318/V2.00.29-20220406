    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.OZ8513
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 TRIM_MEMORY_SIZE = 0x100;
        internal const UInt16 PARA_MEMORY_SIZE = 0x400;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_CURRENT = 4,
            PARAM_DOCTH,
			PARAM_SHORT = 6,		//(A160218)Francis, for 3 offset value is signed 16-bit value calculation
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41,
        }

        #region Parameter GUID
        internal const UInt32 PARAElement = 0x00020000; //Parameter参数起始地址
        internal const UInt32 PARAElement1 = 0x00070000; //Parameter参数起始地址
        internal const UInt32 PARAElement2 = 0x00080000; //Parameter参数起始地址
        internal const UInt32 PARAElement3 = 0x00090000; //Parameter参数起始地址
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;

        #endregion
        #region Trimming参数GUID
        internal const UInt32 TrimElement = 0x000A0000;

        #endregion
    }
}
