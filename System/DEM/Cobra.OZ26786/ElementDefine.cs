using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ26786
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const int    RETRY_COUNTER         = 5;
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
                PARAM_CHARGE_CURRENT_0x14 = 10,
                PARAM_CHARGE_VOL_0x15 = 11,
                PARAM_LEVEL1_ADAPTER_OCP_0x16 = 12,
                PARAM_LEVEL2_ADAPTER_OCP_0x17 = 13,
                PARAM_Boost_Current_LMT_3B01 = 14,
                PARAM_PSYS_Gain_3B08 = 15,
                PARAM_BATMIN_TH_3B0B = 16,
                PARAM_BATLOW_TH_3B0E = 17,
                PARAM_Deglitch_ForOCP1_3C00 = 18,
                PARAM_Deglitch_ForOCP2_3C03 = 19,
                PARAM_Normal_Deglitch_3C06 = 20,
                PARAM_Duration_Time_3C08 = 21,
                PARAM_RAD_3D00 = 22,
                PARAM_PreCHG_3D04 = 23,
                PARAM_RCH_3D08 = 24,
                PARAM_Fpwm_3D0B = 25,
                PARAM_IAD_Gain_3D0E = 26,
                PARAM_VAD_3D0F = 27,
                PARAM_Adapter_Current_3F07 = 28,
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

        #region EFUSE参数GUID
        internal const UInt32 OpMapElement = 0x00020000; //EFUSE参数起始地址
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        #endregion
    }
}
