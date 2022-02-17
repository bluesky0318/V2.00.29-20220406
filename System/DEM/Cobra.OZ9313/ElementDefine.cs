using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.OZ9313
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const byte SYSCR = 0x81;
        internal const byte MARH = 0x88;
        internal const byte MARL = 0x89;
        internal const byte MOMCR = 0x8C;
        internal const byte MOMSR = 0x8D;
        internal const byte PSARH = 0x8E;
        internal const byte PSARL = 0x8F;
        internal const byte PWMSR = 0x97;
        internal const byte SIFPWH = 0x91;
        internal const byte SIFPWL = 0x92;
        internal const byte SPGPWH = 0x93;
        internal const byte SPGPWL = 0x94;
        internal const byte SPMPWH = 0x95;
        internal const byte SPMPWL = 0x96;

        internal const byte BP1H = 0x84;
        internal const byte PSA1H = 0x84;
        internal const byte BP1L = 0x85;
        internal const byte PSA1L = 0x85;
        internal const byte BP2H = 0x86;
        internal const byte PSA2H = 0x86;
        internal const byte BP2L = 0x87;
        internal const byte PSA2L = 0x87;

        internal const UInt16 MAIN_FLASH_BLOCKS = 0x800;
        internal const UInt16 PROG_FLASH_BLOCKS = 0x6C0;
        internal const UInt16 PARAM_FLASH_BLOCKS = 0x140;
        internal const byte INFO_FLASH_BLOCKS = 0x08;
        internal const byte PAGE_BLOCKS = 32;

        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 RETRY_COUNTER = 5;
        internal const UInt16 BLOCK_OPERATION_BYTES = 32;

        internal const UInt16 MTP_BLOCK_SIZE = 0x8000;
        internal const UInt16 INFO_BLOCK_SIZE = 64;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            EFUSE_PARAM_DEFAULT = 0,
            EFUSE_PARAM_INT_TEMP = 1,
            EFUSE_PARAM_EXT_TEMP,
            EFUSE_PARAM_EXT_TEMP_TABLE = 40,
            EFUSE_PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
        }

        internal enum COBRA_SYS_MODE : ushort
        {
            SYS_NORMAL_MODE = 0x07,
            SYS_DEBUG_MODE = 0x0E,
            SYS_MEMORY_MODE = 0x1C,
            SYS_DIGITAL_TEST_MODE = 0x31,
            SYS_ANALOG_TEST_MODE = 0x38
        };

        internal enum COBRA_MEM_ACCESS : ushort
        {
            MEM_NO_ACCESS = 0x00,
            MEM_DATA_ACCESS = 0xC0,
            MEM_DOWNLOAD_MODE = 0x30,
            MEM_BIST_MODE = 0xF0,
            MEM_FLASH_TEST = 0x50
        };

        internal enum COBRA_SUB_MEM_ACCESS : ushort
        {
            SUB_NO_ACCESS = 0x00,
            SUB_READ_MAIN_FLASH = 0x01,
            SUB_WRITE_PROGRAM_FLASH = 0x02,
            SUB_WRITE_PARAM_FLASH = 0x03,
            SUB_PAGE_ERASE_PROGRAM_FLASH = 0x05,
            SUB_PAGE_ERASE_PARAM_FLASH = 0x06,
            SUB_ERASE_MAIN_FLASH = 0x08,
            SUB_READ_INFO_BLOCK = 0x09,
            SUB_WRITE_INFO_BLOCK = 0x0A,
            SUB_ERASE_INFO_BLOCK = 0x0C,
            SUB_MACRO_ERASE = 0x0F
        };

        internal enum COBRA_FLAG_FLASH : ushort
        {
            FLAG_FLASH_TEST = 0x01,
            DOWNLOAD_FLASH_FINISHED = 0x02,
            FLAG_DOWNLOAD_FLASH = 0x04,
            FLAG_PSA = 0x08,
            FLAG_MACRO_ERASE = 0x10,
            FLAG_INFO_ERASE = 0x20,
            FLAG_MAIN_ERASE = 0x40,
            FLAG_PAGE_ERASE = 0x80
        };

        internal enum COBRA_PGPWD_INVALID : ushort
        {
            PGPWD_INVALID = 0x20,
            PGPWD_MATCH = 0x10,
            PMPWD_INVALID = 0x08,
            PMPWD_MATCH = 0x04,
            IFPWD_INVALID = 0x02,
            IFPWD_MATCH = 0x01
        };

        #region Efuse参数GUID
        internal const UInt32 EFUSEElement = 0x00020000;    //0x10~0x1f
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;    //0x30~0xff

        #endregion
    }
}
