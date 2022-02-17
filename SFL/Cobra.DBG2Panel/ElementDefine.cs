using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.DBG2Panel
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 ROW_SIZE = 0x10;
    }
}
