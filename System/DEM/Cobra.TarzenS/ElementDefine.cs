using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.TarzanS
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 EF_MEMORY_OFFSET = 0x10;
        internal const UInt16 EF_MEMORY_SIZE = 0x10;
        internal const byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            EFUSE_PARAM_DEFAULT = 0,
            EFUSE_PARAM_INT_TEMP = 1,
            EFUSE_PARAM_EXT_TEMP,
            EFUSE_PARAM_EXT_TEMP_TABLE = 40,
            EFUSE_PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_DFM_WKM : ushort
        {
            EFUSE_WORKMODE_NORMAL = 0,
            EFUSE_WORKMODE_INTERNAL,
            EFUSE_WORKMODE_PROGRAM,
            EFUSE_WORKMODE_MAPPING,
        }

        internal enum COBRA_DFM_ATELCK : ushort
        {
            EFUSE_ATELCK_MATCHED = 0,
            EFUSE_ATELCK_UNMATCHED,
            EFUSE_ATELCK_MATCHED_10
        }

        #region Efuse参数GUID
        internal const UInt32 EFUSEElement = 0x00020000;    //0x10~0x1f
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;    //0x30~0xff

        #endregion
    }
}
