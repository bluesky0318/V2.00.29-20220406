using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.MPT
{
    internal class ElementDefine
    {
        internal const int    RETRY_COUNTER         = 5;
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
                PARAM_VOLTAGE = 1,
                PARAM_CURRENT,
                PARAM_INT_TEMP,
                PARAM_EXT_TEMP_TABLE = 40,
                PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_ADC7745_REG :  ushort
        {
            ADC_STATUS              = 0x00,
               ADC_CAP_DATA_H       = 0x01,
               ADC_CAP_DATA_M       = 0x02,
               ADC_CAP_DATA_L       = 0x03,
               ADC_VT_DATA_H        = 0x04,
               ADC_VT_DATA_M        = 0x05,
               ADC_VT_DATA_L        = 0x06,
               ADC_CAP_SETUP        = 0x07,
               ADC_VT_SETUP         = 0x08,
               ADC_EXC_SETUP        = 0x09,
               ADC_CONFIGURATION    = 0x0A,
               ADC_CAP_DAC_A        = 0x0B,
               ADC_CAP_DAC_B        = 0x0C,
               ADC_CAP_OFFSET_H     = 0x0D,
               ADC_CAP_OFFSET_L     = 0x0E,
               ADC_CAP_GAIN_H       = 0x0F,
               ADC_CAP_GAIN_L       = 0x10,
               ADC_VOLT_GAIN_H      = 0x11,
               ADC_VOLT_GAIN_L      = 0x12
        }

        internal enum COBRA_ADC7745_CONTROL : ushort
        {
            ADC_STATUS_EXCERR       = 0x08,
                ADC_STATUS_RDY      = 0x04,
                ADC_STATUS_RDYVT    = 0x02,
                ADC_STATUS_RDYCAP   = 0x01,
                ADC_CAPEN           = 0x80,
                ADC_CIN2            = 0x40,
                ADC_CAPDIFF         = 0x20,
                ADC_CAPCHOP         = 0x01,
                ADC_VTEN            = 0x80,
                ADC_VTMD1           = 0x40,
                ADC_VTMD0           = 0x20,
                ADC_EXTREF          = 0x10,
                ADC_VTSHORT         = 0x02,
                ADC_VTCHOP          = 0x01,
                ADC_CLKCTRL         = 0x80,
                ADC_EXCON           = 0x40,
                ADC_EXCB            = 0x20,
                ADC_NOT_EXCB        = 0x10,
                ADC_EXCA            = 0x08,
                ADC_NOT_EXCA        = 0x04,
                ADC_EXCLVL1         = 0x02,
                ADC_EXCLVL0         = 0x01,
                ADC_VTFS1           = 0x80,
                ADC_VTFS0           = 0x40,
                ADC_CAPFS2          = 0x20,
                ADC_CAPFS1          = 0x10,
                ADC_CAPFS0          = 0x08,
                ADC_MD2             = 0x04,
                ADC_MD1             = 0x02,
                ADC_MD0             = 0x01,
                ADC_DACEN           = 0x80,
                ADC_DACAENA         = 0x80,
                ADC_DACBENB         = 0x80
        }

        internal enum COBRA_ADC7745_VTSETUP : ushort
        {
             ADC_VTEN		        = 0x80,
                 ADC_VTMD1		    = 0x40,
                 ADC_VTMD0		    = 0x20,
                 ADC_EXTREF		    = 0x10,
                 ADC_VTSHORT		= 0x02,
                 ADC_VTCHOP		    = 0x01
        }

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CurrentIntegral = 0x000304001;

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
    }
}
