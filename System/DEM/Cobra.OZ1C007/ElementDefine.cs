using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ1C007
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;

        internal const UInt32 ChargerElement = 0x01030000;
        #region Local ErrorCode
        internal const UInt32 IDS_ERR_MTP_WRITE_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        #endregion
    }
}
