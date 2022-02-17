using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace O2Micro.Cobra.HummingBird
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
        internal const UInt32 CommandMask = 0x0000FF00;
        internal const UInt16 RETRY_COUNTER = 5;
        internal const UInt16 BLOCK_OPERATION_BYTES = 4;//32;

        internal const UInt16 MTP_BLOCK_SIZE = 0x8000;
        internal const UInt16 INFO_BLOCK_SIZE = 0x40 + 0xC0; ////0x00 - 0x40 MTP INFO 0x40 - 0x100    SW I2C Parameter
        internal const UInt16 INFO_BLOCK_USED = 8;
        internal const UInt16 MAX_COM_SIZE = 54;

        internal const byte REG_I2C_MTP_STATUS = 0x72;
        internal const byte I2C_MTP_STATUS_MASK = 0x01;
        internal const byte MTP_CHECKSUM_STATUS_MASK = 0x02;
        internal const byte MTP_CHECKSUM_FINISH_MASK = 0x04;
        internal static byte[] interBuffer = new byte[1024 * 64];

        #region SW I2C Paramter
        internal const byte SW_I2C_PARAM_OFFSET = 0x40; //0x00 - 0x40 MTP INFO 0x40 - 0x100    SW I2C Parameter
        #endregion

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            EFUSE_PARAM_DEFAULT = 0,
            EFUSE_PARAM_INT_TEMP = 1,
            EFUSE_PARAM_EXT_TEMP,
            CONST_DATA_FWEDITION= 30,
            CONST_DATA_ATEINFOCRC = 31,
            CONST_DATA_BAT_TABLE_VER = 32,
            EFUSE_PARAM_EXT_TEMP_TABLE = 40,
            EFUSE_PARAM_INT_TEMP_REFER = 41,

        }

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
            SUB_TASK_NORMAL_MODE = 0x80,
            SUB_TASK_DEBUG_MODE = 0x90,
            SUB_TASK_COMPARE = 0x11
        }

        #region Efuse参数GUID
        internal const UInt32 EFUSEElement = 0x00020000;
        internal const UInt32 FWEdition = 0x00029000;
        internal const UInt32 AleInfoCRC = 0x00029100;
        internal const UInt32 BatteryTableVersion = 0x00029200;
        internal const UInt32 BoardOffsetHW = 0x00029300;
        internal const UInt32 ChipID = 0x00029400;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;

        #endregion
    }
}
