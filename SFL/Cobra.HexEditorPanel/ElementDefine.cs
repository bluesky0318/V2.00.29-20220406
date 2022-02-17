using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.HexEditorPanel
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        #region Project File参数GUID
        internal const UInt32 MTP_MAX_CAP = 0x100000;
        internal const UInt32 ProjectFileElement = 0x000f0000;
        internal const UInt32 HexFileElement = ProjectFileElement + 0x01;

        internal static byte[] m_MTP_Memory = new byte[MTP_MAX_CAP];
        #endregion
    }
}
