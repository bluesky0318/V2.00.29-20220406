using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Az5D
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 EFUSE_MEMORY_SIZE = 0x10;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 TestCtrl_FLAG = 0x000F;
        internal const UInt16 EFUSE_MODE_CLEAR_FLAG = 0xFFF0;
        internal const UInt16 ALLOW_WR_CLEAR_FLAG = 0x7FFF;
        internal const UInt16 nTrim_Times = 1;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP =2,
            PARAM_EXT_TEMP_20UA = 3,
            PARAM_CURRENT = 4,
            PARAM_EXT_TEMP_120UA = 5,
            PARAM_DOCTH,
            PARAM_SIGN_ORG = 20,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_AZALEA5V_WKM : ushort
        {
            EFUSE_WORKMODE_NORMAL = 0,
            EFUSE_WORKMODE_WRITE_MAP_CTRL = 0x01,
            EFUSE_WORKMODE_PROGRAM = 0x02,
        }

        internal enum COBRA_AZALEA5V_TESTCTRL : ushort
        {
            EFUSE_TESTCTRL_NORMAL = 0,
            EFUSE_TESTCTRL_VR26V_TRIM = 3,
            EFUSE_TESTCTRL_OSC512K_TRIM = 4,
            EFUSE_TESTCTRL_DOC_TRIM = 5,
            EFUSE_TESTCTRL_THM_RESISTOR_TRIM = 6,
            EFUSE_TESTCTRL_LEVEL_SHIFTR_TEST = 7,
            EFUSE_TESTCTRL_INTVTS_OFFSET_TRIM = 8,
            EFUSE_TESTCTRL_SLOPE_TRIM = 9,
            EFUSE_TESTCTRL_CELL_BALANCE_TRIM = 14,
            EFUSE_TESTCTRL_KEY_SIGNAL = 15
        }

        internal enum COBRA_COMMAND_MODE : ushort
        {
            TRIGGER_SCAN_CTO_MODE = 0x13,
            TRIGGER_SCAN_ALL_MODE = 0x14,
            TRIGGER_SCAN_EIGHT_MODE = 0x20,
            TRIGGER_SCAN_ONE_MODE = 0x21,
            TRIM_TRIGGER_SCAN_EIGHT_MODE = 0x30
        }

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion

        #region EFUSE参数GUID
        internal const UInt32 EFUSEElement              = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 EFUSE_Osc512k_Ftrim       = 0x00020000;
        internal const UInt32 EFUSE_DOC_Trim            = 0x00020005;
        internal const UInt32 EFUSE_VR25v_VTrim         = 0x00020100;
        internal const UInt32 EFUSE_VR25v_TTrim         = 0x00020200;
        internal const UInt32 EFUSE_Thm3k_Trim          = 0x00020300;
        internal const UInt32 EFUSE_Thm_Offset          = 0x00020306;
        internal const UInt32 EFUSE_Thm60k_Trim         = 0x00020400;
        internal const UInt32 EFUSE_Int_Tmp_Trim        = 0x00020504;
        internal const UInt32 EFUSE_Cell01_Slope_Trim   = 0x00020500;
        internal const UInt32 EFUSE_Cell02_Slope_Trim   = 0x00020604;
        internal const UInt32 EFUSE_Cell03_Slope_Trim   = 0x00020600;
        internal const UInt32 EFUSE_Cell04_Slope_Trim   = 0x00020704;
        internal const UInt32 EFUSE_Cell05_Slope_Trim   = 0x00020700;
        internal const UInt32 EFUSE_Packc_Slope_Trim    = 0x00020800;
        internal const UInt32 EFUSE_Packv_Slope_Trim    = 0x00020804;
        internal const UInt32 EFUSE_Packc_Offset        = 0x00020900;
        internal const UInt32 EFUSE_Packv_Offset        = 0x00020a06;
        internal const UInt32 EFUSE_Cell01_Offset       = 0x00020a00;
        internal const UInt32 EFUSE_Cell02_Offset       = 0x00020b00;
        internal const UInt32 EFUSE_Cell03_Offset       = 0x00020c00;
        internal const UInt32 EFUSE_Cell04_Offset       = 0x00020d00;
        internal const UInt32 EFUSE_Cell05_Offset       = 0x00020e00;
        internal const UInt32 EFUSE_ATE_CRC_Sum         = 0x00020f00;
        internal const UInt32 EFUSE_ATE_frozen          = 0x00020f07;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement          = 0x00030000;
        internal const UInt32 OP_STR                    = 0x00030210;
        internal const UInt32 OP_CELL1V_CTO             = 0x00032100;
        internal const UInt32 OP_CELL2V_CTO             = 0x00032200;
        internal const UInt32 OP_CELL3V_CTO             = 0x00032300;
        internal const UInt32 OP_CELL4V_CTO             = 0x00032400;
        internal const UInt32 OP_CELL5V_CTO             = 0x00032500;

        internal const UInt32 OP_INTMP                  = 0x00034000;
        internal const UInt32 OP_CELL1V                 = 0x00034100;
        internal const UInt32 OP_CELL2V                 = 0x00034200;
        internal const UInt32 OP_CELL3V                 = 0x00034300;
        internal const UInt32 OP_CELL4V                 = 0x00034400;
        internal const UInt32 OP_CELL5V                 = 0x00034500;
        internal const UInt32 OP_THM20UA                = 0x00035500;
        internal const UInt32 OP_THM120UA               = 0x00035600;
        internal const UInt32 OP_Packc                  = 0x00035900;
        internal const UInt32 OP_Packv                  = 0x00035B00;
        internal const UInt32 OP_VCC                    = 0x00035C00;
        internal const UInt32 OP_V50V                   = 0x00035D00;
        internal const UInt32 OP_MCU                    = 0x00035E00;

        internal const UInt32 OP_INTMP_8                = 0x00036000;
        internal const UInt32 OP_CELL1V_8               = 0x00036100;
        internal const UInt32 OP_CELL2V_8               = 0x00036200;
        internal const UInt32 OP_CELL3V_8               = 0x00036300;
        internal const UInt32 OP_CELL4V_8               = 0x00036400;
        internal const UInt32 OP_CELL5V_8               = 0x00036500;
        internal const UInt32 OP_THM20UA_8              = 0x00037500;
        internal const UInt32 OP_THM120UA_8             = 0x00037600;
        internal const UInt32 OP_Packc_8                = 0x00037900;
        internal const UInt32 OP_Packv_8                = 0x00037B00;
        internal const UInt32 OP_VCC_8                  = 0x00037C00;
        internal const UInt32 OP_V50V_8                 = 0x00037D00;
        internal const UInt32 OP_MCU_8                  = 0x00037E00;

        #endregion
    }
}
