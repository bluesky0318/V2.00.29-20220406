using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.FalconLY
{
    internal struct BatteryMode
    {
        internal bool ILFC_Enable;
        internal bool IEXTEND_Enable;
        internal bool CEXTEND_Enable;
        internal bool VEXTEND_Enable;
        internal byte CellNum;
        internal byte ExtTempNum;
    }

    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const int RETRY_COUNTER = 5;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt32 CommandMask = 0x0000FF00;
        internal const byte SBSBatteryMode = 0x03;
        internal const UInt16 TotalCellNum = 36;
        internal const UInt16 TotalExtNum = 4;
        internal const UInt16 CommonDataLen = 2;
        internal const UInt16 MaxBytesNum = 20;    //FW 固定回應長度為 21 個 byte BYTE[0]: 總長度 (不含自身)，固定為0x14 (20個BYTE)。BYTE[1]: 字串長度(不含字串結尾的0)

        internal enum COBRA_DATA_LEN : ushort
        {
            DATA_LEN_ONE_BYTE,
                DATA_LEN_TWO_BYTES = 2,
                DATA_LEN_FOUR_BYTES = 5,
                DATA_LEN_TWENTY_BYTES = 21,  
        }

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
           PARAM_DEFAULT = 0,
                PARAM_SIGN,
                PARAM_BYTE,
                PARAM_WORD,
                PARAM_TEMP,
                PARAM_DATA = 8,
                PARAM_STRING = 9,
        }


        #region SBS参数GUID
        internal const UInt32 SBSElement                            = 0x00020000;
        internal const UInt32 ManufacturerAccess                    = 0x00020000;
        internal const UInt32 AFEChipStatus                         = 0x00020100;
        internal const UInt32 FirmwareVersion                       = 0x00020200;
        internal const UInt32 BatteryMode                           = 0x00020300;
        internal const UInt32 ExternalTemperature1                  = 0x00020400;
        internal const UInt32 ExternalTemperature2                  = 0x00020500;
        internal const UInt32 ExternalTemperature3                  = 0x00020600;
        internal const UInt32 ExternalTemperature4                  = 0x00020700;
        internal const UInt32 InternalTemperature                   = 0x00020800;
        internal const UInt32 PackVoltage                           = 0x00020900;
        internal const UInt32 PackCurrent                           = 0x00020A00;
        internal const UInt32 AverageCurrent                        = 0x00020B00;
        internal const UInt32 ExtendedCommand                       = 0x00020C00;
        internal const UInt32 RelativeStateOfCharge                 = 0x00020D00;
        internal const UInt32 AbsolutStataOfCharge                  = 0x00020E00;
        internal const UInt32 RemainingCapacity                     = 0x00020F00;
        internal const UInt32 FullChargeCapacity                    = 0x00021000;
        internal const UInt32 ChargingCurrent                       = 0x00021400;
        internal const UInt32 ChargingVoltage                       = 0x00021500;
        internal const UInt32 BatteryStatus                         = 0x00021600;
        internal const UInt32 CycleCount                            = 0x00021700;
        internal const UInt32 DesignCapacity                        = 0x00021800;
        internal const UInt32 DesignVoltage                         = 0x00021900;
        internal const UInt32 SpecificationInfo                     = 0x00021A00;
        internal const UInt32 ManufactureDate                       = 0x00021B00;
        internal const UInt32 SerialNumber                          = 0x00021C00;
        internal const UInt32 FastChargingCurrent                   = 0x00021D00;
        internal const UInt32 FastChargingVoltage                   = 0x00021E00;
        internal const UInt32 ManufacturerName                      = 0x00022000;
        internal const UInt32 DeviceName                            = 0x00022100;
        internal const UInt32 DeviceChemistry                       = 0x00022200;
        internal const UInt32 CellVoltage01                         = 0x00022300;
        internal const UInt32 CellVoltage02                         = 0x00022400;
        internal const UInt32 CellVoltage03                         = 0x00022500;
        internal const UInt32 CellVoltage04                         = 0x00022600;
        internal const UInt32 CellVoltage05                         = 0x00022700;
        internal const UInt32 CellVoltage06                         = 0x00022800;
        internal const UInt32 CellVoltage07                         = 0x00022900;
        internal const UInt32 CellVoltage08                         = 0x00022A00;
        internal const UInt32 CellVoltage09                         = 0x00022B00;
        internal const UInt32 CellVoltage10                         = 0x00022C00;
        internal const UInt32 CellVoltage11                         = 0x00022D00;
        internal const UInt32 CellVoltage12                         = 0x00022E00;
        internal const UInt32 CellVoltage13                         = 0x00022F00;
        internal const UInt32 CellVoltage14                         = 0x00023000;
        internal const UInt32 CellVoltage15                         = 0x00023100;
        internal const UInt32 CellVoltage16                         = 0x00023200;
        internal const UInt32 CellVoltage17                         = 0x00023300;
        internal const UInt32 CellVoltage18                         = 0x00023400;
        internal const UInt32 CellVoltage19                         = 0x00023500;
        internal const UInt32 CellVoltage20                         = 0x00023600;
        internal const UInt32 CellVoltage21                         = 0x00023700;
        internal const UInt32 CellVoltage22                         = 0x00023800;
        internal const UInt32 CellVoltage23                         = 0x00023900;
        internal const UInt32 CellVoltage24                         = 0x00023A00;
        internal const UInt32 CellVoltage25                         = 0x00023B00;
        internal const UInt32 CellVoltage26                         = 0x00023C00;
        internal const UInt32 CellVoltage27                         = 0x00023D00;
        internal const UInt32 CellVoltage28                         = 0x00023E00;
        internal const UInt32 CellVoltage29                         = 0x00023F00;
        internal const UInt32 CellVoltage30                         = 0x00024000;
        internal const UInt32 CellVoltage31                         = 0x00024100;
        internal const UInt32 CellVoltage32                         = 0x00024200;
        internal const UInt32 CellVoltage33                         = 0x00024300;
        internal const UInt32 CellVoltage34                         = 0x00024400;
        internal const UInt32 CellVoltage35                         = 0x00024500;
        internal const UInt32 CellVoltage36                         = 0x00024600;
        #endregion
    }
}
