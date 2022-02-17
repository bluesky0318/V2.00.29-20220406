using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Yeti
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


        /*internal enum COMMAND : ushort
        {
            DGB = 0,
            BURN = 1,
            FREEZE = 2
        }*/
        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            /*ICC = 1,
            VREG = 2,
            ITRKCHG = 3,
            IPRECHG = 4,
            EOC = 5,
            VPRECHG = 6,
            VSYSMIN_OS = 7,
            VRECHG = 8,*/
        }

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;

        #endregion
    }
}
