using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ101
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const int RETRY_COUNTER            = 15;
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;
        internal const byte YFLASH_WORKMODE_REG = 0x1F;
        internal const byte TRIM_REG = 0x08;
        internal const byte TRIM0_REG = 0x04;
        internal const byte TRIM1_REG = 0x03;

        internal enum COBRA_BLUEWHALE_WKM : ushort
        {
            YFLASH_WORKMODE_NORMAL = 0,
            YFLASH_WORKMODE_ANALOG_TESTMODE = 0x01,
            YFLASH_WORKMODE_DIGITAL_TESTMODE = 0x02,
            YFLASH_WORKMODE_ADC_FFTMODE = 0x03,
        }

        internal enum COMMAND : ushort
        {
            DGB = 0,
            BURN = 1,
            FREEZE = 2
        }
        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
                PARAM_VOLTAGE = 1,
                PARAM_CURRENT,
                PARAM_INT_TEMP,
                PARAM_EXT_TEMP,
                PARAM_CV = 5,
                PARAM_WAKEUP_V,
                PARAM_RC_HYS,
                PARAM_VSM,
                PARAM_CC,
                PARAM_WAKEUP_C = 10,
                PARAM_EOC,
                PARAM_VICL,
                PARAM_WKT,
                //PARAM_RCG,
                PARAM_EXT_TEMP_TABLE = 40,
                PARAM_INT_TEMP_REFER = 41
        }

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

        #region Operation参数GUID
        internal const UInt32 OpElement = 0x00030000;

        #endregion
    }
}
