using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;
namespace Cobra.Uart
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 RETRY_COUNTER = 5;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal static UInt16 dtimes = 0;
        internal static UInt64 stimes = 0;
        internal static UInt16 wSize = 1024;

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
        }
    }
}
