using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ88030
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 YFLASH_MEMORY_SIZE    = 0xFF; //用于EFUSE config, Register config等SFL
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF; //用于Exper SFL
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const UInt16 MAP_REG_START_ADDR    = 0x20;
        internal const UInt16 OP_REG_START_ADDR     = 0x30;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;
        internal const byte WORKMODE_REG = 0x39;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
                PARAM_VOLTAGE = 1,
                PARAM_INT_TEMP,
                PARAM_EXT_TEMP,
                PARAM_CURRENT = 4,
                PARAM_EXT_TEMP_TABLE = 40,
                PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_WARRIORS_WKM :  ushort
        {
            EFLASHMODE_NORMAL = 0,
                EFLASHMODE_MAPREG_PROGRAM = 0x8001,
                EFLASHMODE_EFUSE_PROGRAM = 0x8002,
        }

        public struct Forzen_struct
        {
            public bool m_efuse_cfgfrz;
            public bool m_efuse_bank1;
            public bool m_efuse_bank2;
            public bool m_map_cfgfrz;
            public bool m_map_bank1;
            public bool m_map_bank2;
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_EFUSE_CFGFRZ = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_EFUSE_BANK1 = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_EFUSE_BANK2 = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_MAP_CFGFRZ = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_MAP_BANK1 = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_MAP_BANK2 = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        internal const UInt32 IDS_ERR_DEM_ERROR_MODE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        #endregion

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpETRx = TemperatureElement + 0x00;
        internal const UInt32 TpETPullupR = TemperatureElement + 0x01;
        internal const UInt32 TpN30D = TemperatureElement + 0x02;
        internal const UInt32 TpN25D = TemperatureElement + 0x03;
        internal const UInt32 TpN20D = TemperatureElement + 0x04;
        internal const UInt32 TpN15D = TemperatureElement + 0x05;
        internal const UInt32 TpN10D = TemperatureElement + 0x06;
        internal const UInt32 TpN5D = TemperatureElement + 0x07;
        internal const UInt32 TpN0D = TemperatureElement + 0x08;
        internal const UInt32 TpP5D = TemperatureElement + 0x09;
        internal const UInt32 TpP10D = TemperatureElement + 0x0A;
        internal const UInt32 TpP15D = TemperatureElement + 0x0B;
        internal const UInt32 TpP20D = TemperatureElement + 0x0C;
        internal const UInt32 TpP25D = TemperatureElement + 0x0D;
        internal const UInt32 TpP30D = TemperatureElement + 0x0E;
        internal const UInt32 TpP35D = TemperatureElement + 0x0F;
        internal const UInt32 TpP40D = TemperatureElement + 0x10;
        internal const UInt32 TpP45D = TemperatureElement + 0x11;
        internal const UInt32 TpP50D = TemperatureElement + 0x12;
        internal const UInt32 TpP55D = TemperatureElement + 0x13;
        internal const UInt32 TpP60D = TemperatureElement + 0x14;
        internal const UInt32 TpP65D = TemperatureElement + 0x15;
        internal const UInt32 TpP70D = TemperatureElement + 0x16;
        internal const UInt32 TpP75D = TemperatureElement + 0x17;
        internal const UInt32 TpP80D = TemperatureElement + 0x18;
        internal const UInt32 TpP85D = TemperatureElement + 0x19;
        internal const UInt32 TpP90D = TemperatureElement + 0x1A;
        #endregion        
        
        #region YFLASH参数GUID
        internal const UInt32 YFLASHElement = 0x00020000; //YFLASH参数起始地址
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        #endregion
    }
}
