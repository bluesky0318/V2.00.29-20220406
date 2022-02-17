using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Second_EC_Bluewhale
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const int RETRY_COUNTER                = 1;
        internal const UInt16 OP_MEMORY_SIZE            = 0xFF;
        internal const UInt16 MCU_MEMORY_SIZE           = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR           = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR      = -999999;
        internal const UInt32 ElementMask               = 0xFFFF0000;
        internal const UInt32 CommandMask               = 0x0000FF00;
        internal const UInt32 OZ1C115Element            = 0x00030000;
        internal const UInt32 SBSElement                = 0x00060000;
        internal const UInt32 MCUElement                = 0x00090000;

        internal enum COBRA_SUBTYPE : ushort
        {
            EXT_TEMPERATURE = 0,
            MCU_SUBTYPE = 1,
            OZ1C115_SUBTYPE_MONITOR = 2,
            OZ1C115_SUBTYPE_CHARGE = 3,
        }

        //千万注意40，41不要修订
        internal enum COBRA_MONITOR_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_CURRENT,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_CHARGER_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_CURRENT,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_CV = 5,
            PARAM_WAKEUP_V,
            PARAM_VSM = 8,
            PARAM_CC,
            PARAM_WAKEUP_C,
            PARAM_EOC,
            PARAM_VICL,
            PARAM_WKT,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_MCU_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_CURRENT,
            PARAM_VSM,
            PARAM_WAKEUP_V,
            PARAM_WAKEUP_C,
            PARAM_WKT = 5,
            PARAM_CV,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        public static UInt32 CheckData(byte[] data)
        {
            byte bCheckSum = 0;
            if (data[0] != 0x55) return LibErrorCode.IDS_ERR_SBSSFL_SW_FRAME_HEAD;
            if ((data[1] >= 0xF0) && (data[1] <= 0xFF))
            {
                switch (data[1])
                {
                    case 0xF0:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_FRAME_HEAD;
                    case 0xF1:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_FRAME_CHECKSUM;
                    case 0xF2:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_COMMAND_NODEF;
                    case 0xF3:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_COMMAND_EXECU;
                    case 0xF4:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_COMMAND_EXECU_TO;
                    case 0xF5:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_I2C_BLOCKED;
                    case 0xF6:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_PEC_CHECK;
                    default:
                        break;
                }
            }

            bCheckSum = (byte)(0x00 - data[1] - data[2] - data[3]);
            if (bCheckSum != data[4]) return LibErrorCode.IDS_ERR_SBSSFL_SW_PEC_CHECK;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
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
    }
}
