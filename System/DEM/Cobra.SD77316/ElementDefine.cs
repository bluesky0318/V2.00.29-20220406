using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.SD77316
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
        internal const byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const int RETRY_COUNTER = 5;

        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,

            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41
        }


        internal enum COMMAND : ushort
        {
            Enter_Test_MODE = 6,
            Exit_Test_MODE = 7,
            Freeze = 8,
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_POWERON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        #endregion

        #region Expert参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        #endregion
    }
}
