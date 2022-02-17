using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.FWSeaguIIPD
{
    public struct ROM_INFOR
    {
        public byte CP_STATUS;
        public byte FWU;
        public byte ROM_VER;
    }
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const int RETRY_COUNT = 5;
        internal static byte[] interBuffer = new byte[1024 * 64];
        internal const UInt32 EEPROM_START_ADDRESS  = 0x00004000;
        internal const UInt32 EFLASH_START_ADDRESS  = 0x00014000;
        internal const UInt32 SRAM_START_ADDRESS = 0x20000000;
        internal const UInt32 APB_START_ADDRESS = 0x40000000;
        public static List<byte> m_FWU_List = new List<byte> { 0xA5, 0x35, 0x53, 0x4B, 0xB4, 0xAC, 0xCA, 0x27, 0X72 };
        public static ROM_INFOR m_rom_infor;

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_CMD_SUCCESS = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0000;
        internal const UInt32 IDS_ERR_DEM_INVALID_COMMAND = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x000B;
        internal const UInt32 IDS_ERR_DEM_PREFIX_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0015;
        internal const UInt32 IDS_ERR_DEM_CRC_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0016;
        internal const UInt32 IDS_ERR_DEM_END_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0017;
        internal const UInt32 IDS_ERR_DEM_NUMBER_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0018;
        internal const UInt32 IDS_ERR_DEM_ADDR_BOUNDARY_ERR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x001F;
        internal const UInt32 IDS_ERR_DEM_ADDR_NOT_SUPPOUT_ERR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0020;
        internal const UInt32 IDS_ERR_DEM_DATA_OUT_OF_RANGE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0029;
        internal const UInt32 IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x002A;
        internal const UInt32 IDS_ERR_DEM_WRITE_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0033;
        internal const UInt32 IDS_ERR_DEM_READ_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0034;
        internal const UInt32 IDS_ERR_DEM_HEX_FILE_SIZE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0035;
        internal const UInt32 IDS_ERR_DEM_GOODCRC_PREFIX_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0050;
        internal const UInt32 IDS_ERR_DEM_CMD_MISMATCH = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0051;
        internal const UInt32 IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0052;
        internal const UInt32 IDS_ERR_DEM_RECEIVE_PACKAGE_TOTAL_SIZE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0053;
        #endregion

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP = 2,
            PARAM_EXT_TEMP = 3,

            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }
        public enum MEMORY :ushort
        {
            EEPROM,
            System,
            ExtFlash,
        }
        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
            SUB_TASK_COMPARE = 0x11,
            SUB_TASK_ERASE = 0x12,
        }


        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;

        #endregion
    }
}
