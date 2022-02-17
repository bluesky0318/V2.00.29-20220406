using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ8513U
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
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFF000000;
        internal const int RETRY_COUNTER = 5;
        internal const UInt32 CommandMask = 0x00FF0000;

        internal const UInt16 DATA_TYPE_REG      = 0x10;
        internal const UInt16 DATA_START_REG     = 0x11;
        internal const UInt16 COMMAND_STATE_REG  = 0x15;
        internal const UInt16 DATA_REG_LEN = 0x04;
        internal const UInt16 COMMAND_STATE_MASK = 0x80;
        internal static byte[] m_rd_dataType = new byte[] { 0x00, 0x02, 0x04, 0x10, 0x12, 0x14, 0x16, 0x18, 0x1A, 0x1C };
        internal static byte[] m_wr_dataType = new byte[] { 0x14, 0x16, 0x18, 0x1A, 0x1C, 0x30, 0x32 };

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_PD_VER,
            PARAM_STRING =9
        }

        internal enum COMMAND : ushort
        {
            TestRead = 0,
            TestWrite = 1,
        }

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x03000000;

        #endregion
    }

    public class OZ8513U_REG
    {
        public UInt32 val;
        public UInt32 err;
        public Reg reg;

        public OZ8513U_REG(ref Reg r)
        {
            reg = r;
            val = 0x00000000;
            err = LibErrorCode.IDS_ERR_I2C_INVALID_COMMAND;
        }
    }
}
