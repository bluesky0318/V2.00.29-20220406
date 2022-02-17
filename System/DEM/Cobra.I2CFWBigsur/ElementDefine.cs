using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.I2CFWBigsur
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
        internal const UInt32 MTPStartAddress = 0x00010000;
        internal const UInt32 FLASHBStartAddress = 0x60000000;
        internal const UInt16 DWORD_OPERATION_BYTES = 4; //Only Support DWORD
        internal const UInt16 THIRTYTWO_BLOCK_OPERATION = 32; //Only Support 32bytes
        internal const UInt16 MTPSize = 16 * 1024;
        internal static byte[] interBuffer = new byte[1024 * 128];

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_HANDSHAKE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        #endregion

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
            SUB_TASK_COMPARE = 0x11,
            SUB_TASK_ERASE = 0x12,
        }

        internal enum COBRA_FLASH_OPERATE : ushort
        {
            COBRA_FLASH_A,
                COBRA_FLASH_B
        }
    }
}
