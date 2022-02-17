using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ26305
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
        internal const UInt16 EEPROM_MEMORY_SIZE = 0xFF;
        internal const UInt16 EF_TOTAL_PARAMS = 0x10;
        internal const byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const int RETRY_COUNTER = 5;
        internal const UInt16 MAP_REG_START_ADDR = 0x20;
        internal const byte WORKMODE_REG = 0x40;

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpETRx = TemperatureElement + 0x00;
        #endregion

        #region Efuse参数GUID
        internal const UInt32 E_BAT_TYPE = 0x00021907;
        internal const UInt32 E_OVP_TH = 0x00021900;
        internal const UInt32 E_OVR_HYS = 0x00021B04;
        internal const UInt32 E_Uuvp = 0x00021A00;
        internal const UInt32 E_Uuvp_Hys = 0x00021A04;
        #endregion

        #region Efuse Mapping参数GUID
        internal const UInt32 EFUSEMapElement = 0x00020000;    //0x10~0x1f
        internal const UInt32 O_BAT_TYPE = 0x00022907;
        internal const UInt32 O_OVP_TH = 0x00022900;
        internal const UInt32 O_OVR_HYS = 0x00022B04;
        internal const UInt32 O_Uuvp = 0x00022A00;
        internal const UInt32 O_Uuvp_Hys = 0x00022A04;
        #endregion

        #region EEPROM参数GUID
        internal const UInt32 EEPROMElement = 0x00020000;    //0x10~0x1f
        #endregion

        #region Expert参数GUID
        internal const UInt32 OperationElement = 0x00030000;    //0x30~0xff
        internal const UInt32 EXPERT_E_OVP_TH = 0x00031900;
        internal const UInt32 EXPERT_M_OVP_TH = 0x00032900;
        #endregion

        #region Virtual参数GUID
        internal const UInt32 VirtualElement = 0x000c0000;
        #endregion

        #region 芯片常量
        internal const UInt16 EF_USR_OFFSET = 0x16;
        internal const UInt16 EF_USR_TOP = 0x1C;
        internal const UInt16 USR_FROZEN_MASK = 0xF0;
        internal const UInt16 USR_FROZEN_Data = 0xA0;
        #endregion
        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            OVP = 2,
            EXPERT_OVP = 4,
            CONFIG_UVR_HYS =5,
            CONFIG_CELL = 6,

            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_POWERON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_POWEROFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_POWERCHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_FROZEN = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_FROZEN_OP = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_BLOCK = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        internal const UInt32 IDS_ERR_DEM_ERROR_MODE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0009;
        #endregion

        internal enum COBRA_PIKACHU5_WKM : ushort
        {
            EFLASHMODE_NORMAL = 0,
            EFLASHMODE_MAPREG_PROGRAM = 0x8001,
            EFLASHMODE_EFUSE_PROGRAM = 0x8002,
        }

        internal enum COMMAND : ushort
        {
            FROZEN_BIT_CHECK_PC = 9,
            FROZEN_BIT_CHECK = 10,
            DIRTY_CHIP_CHECK_PC = 11,
            DIRTY_CHIP_CHECK = 12,
            DOWNLOAD_PC = 13,
            DOWNLOAD = 14,
            READ_BACK_CHECK_PC = 15,
            READ_BACK_CHECK = 16,
            //GET_EFUSE_HEX_DATA = 17,  //不再使用此命令，与OZ77系列统一
            SAVE_EFUSE_HEX = 18,
            LOAD_BIN_FILE = 22                   //加载bin文件
        }
    }
}
