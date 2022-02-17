using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.OZ9316
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 EP_MEMORY_SIZE = 0x60;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 SM_MEMORY_SIZE = 0x80;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 SPI_RETRY_COUNT = 5;
        internal const UInt16 CMD_SECTION_SIZE = 3;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_CELLNUM,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_CURRENT = 5,
            PARAM_BLD_ACC,
            PARAM_RSENSE,
            PARAM_DOC2_TH = 8,
            PARAM_DOC2_SLOP,
            PARAM_DOC2_OFFSET,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_OZ9316_WKM : ushort
        {
            WORKMODE_NORMAL = 0,
            WORKMODE_EEPROM_MAP = 3,
            WORKMODE_SOFTWARE_RESET_REQUESTED = 6,
            WORKMODE_SLEEP_MODE_REQUESTED = 9,
            WORKMODE_STANDBY_MODE_REQUESTED = 10,
            WORKMODE_POWER_DOWN_MODE_REQUESTED = 12,
            WORKMODE_ATE_MODE = 15,
        }

        internal enum COBRA_OZ9316_ATEM : ushort
        {
            ATEMODE_NORMAL = 0,
            ATEMODE_TRIM_MODE,
            ATEMODE_ANALOG_TEST_MODE1,
            ATEMODE_ANALOG_TEST_MODE2,
            ATEMODE_ANALOG_TEST_MODE3,
            ATEMODE_ANALOG_TEST_MODE4,
            ATEMODE_ANALOG_TEST_MODE5,
            ATEMODE_EEPROM_BLOCK_ERASE_REQUESTED = 7,
            ATEMODE_EEPROM_BLOCK_WRITE_MODE,
            ATEMODE_EEPROM_DOWNLOAD_MODE,
            ATEMODE_EEPROM_MAPPING_REQUESTED,
            ATEMODE_EEPROM_TEST_MODE,
            ATEMODE_SRAM_BIST_REQUEST
        }

        internal enum COBRA_OZ9316_COMMAND : ushort
        {
            CHECK_SCAN_STATUS,
                TRIGGER_SCAN_REQ,
                ADC_RESUME_REQ,
                CALIBRATION = 0x06,
                SCAN_TRIGGER,
                SCAN_AUTO,
                TRIM,    
        }

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpITSlope = TemperatureElement + 0x00;
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
        internal const UInt32 TpP35D = TemperatureElement + 0x1F;
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

        #region EEPROM参数GUID
        internal const byte EP_RD_CMD = 0x60;
        internal const byte EP_WR_CMD = 0x90;
        internal const byte ATE_MAX_DATA = 0x27;
        internal const byte EEP_MAX_DATA = 0x60;
        internal const UInt32 EEPROMElement = 0x00020000; //EEPROM参数起始地址

        internal const UInt32 EpITV0 = EEPROMElement + 0x00001700;
        internal const UInt32 EpDoc2Slop = EEPROMElement + 0x00002500;
        internal const UInt32 EpDoc2Offset = EEPROMElement + 0x00002600;
        internal const UInt32 EpRsense = EEPROMElement + 0x00005A00;
        internal const UInt32 EpCOCTH = EEPROMElement + 0x00003108;
        internal const UInt32 EpDOC1TH = EEPROMElement + 0x00003008;
        internal const UInt32 EpDOC2TH = EEPROMElement + 0x00002B08;
        internal const UInt32 EpBldAcc = EEPROMElement + 0x00002D00;
        internal const UInt32 EpInChg = EEPROMElement + 0x00003100;
        internal const UInt32 EpInDsg = EEPROMElement + 0x00003000;
        #endregion

        #region Operation参数GUID
        internal const byte OR_RD_CMD = 0x30;
        internal const byte OR_WR_CMD = 0x50;
        internal const UInt32 OperationElement = 0x00030000;

        #endregion

        #region SRAM参数GUID
        internal const byte SRAM_RD_CMD = 0xA0;
        internal const byte SRAM_WR_CMD = 0xC0;
        internal const UInt32 SRAMElement = 0x00040000;
        internal const UInt32 SrCell1  = SRAMElement + 0x00000100;
        internal const UInt32 SrCell2  = SRAMElement + 0x00000600;
        internal const UInt32 SrCell3  = SRAMElement + 0x00000700;
        internal const UInt32 SrCell4  = SRAMElement + 0x00000800;
        internal const UInt32 SrCell5  = SRAMElement + 0x00000900;
        internal const UInt32 SrCell6  = SRAMElement + 0x00000A00;
        internal const UInt32 SrCell7  = SRAMElement + 0x00000B00;
        internal const UInt32 SrCell8  = SRAMElement + 0x00000C00;
        internal const UInt32 SrCell9  = SRAMElement + 0x00000D00;
        internal const UInt32 SrCell10 = SRAMElement + 0x00000E00;
        internal const UInt32 SrCell11 = SRAMElement + 0x00000F00;
        internal const UInt32 SrCell12 = SRAMElement + 0x00001000;
        internal const UInt32 SrCell13 = SRAMElement + 0x00001100;
        internal const UInt32 SrCell14 = SRAMElement + 0x00001200;
        internal const UInt32 SrCell15 = SRAMElement + 0x00001300;
        internal const UInt32 SrCell16 = SRAMElement + 0x00001400;
        internal const UInt32 SrVPack  = SRAMElement + 0x00001500;
        internal const UInt32 SrGPIO1  = SRAMElement + 0x00001800;
        #endregion

        #region 设备工作FLAG
        internal const UInt16 WORK_MODE_FLAG        = 0xF0FF;
        internal const UInt16 ATE_MODE_FLAG         = 0xFFF0;
        internal const UInt16 ATE_MODE_FINISH_FLAG  = 0x000F;
        internal const UInt16 EEPROM_BUSY_FLAG      = 0x0080;
        internal const UInt16 EEPROM_ERASE_FLAG     = 0x0040;
        internal const UInt16 CADC_SUPPORT_FLAG     = 0x2000;
        internal const UInt16 VADC_SUPPORT_FLAG     = 0x000F;

        internal const byte WORKMODE_REG            = 0x67;
        internal const byte SYSCFG_REG              = 0x28;
        internal const byte ATEMODE_REG             = 0x75;
        internal const byte EE_BUSY_FINISH_REG      = 0x75;
        internal const byte EE_CELL_NUMBER          = 0x04;
        #endregion

        #region 设备密码服务
        //Password
        internal const byte ATE_PRIMARY_PSW_REG = 0x07;
        internal const byte ATE_SECOND_PSW_REG = 0x61;
        internal const byte USER_PRIMARY_PSW_REG = 0x2F;
        internal const byte USER_SECOND_PSW_REG = 0x62;
        internal const byte INVALID_PSW_ONE = 0x00;
        internal const byte INVALID_PSW_TWO = 0xFF;
        internal const byte ATE_SECOND_PSW_TO_ATEMODE = 0x33;
        internal const byte ATE_SECOND_PSW_TO_BGR1BGR2ADCONLINECALIBRATION_CHANNELTRIMREGISTER = 0x77;
        internal const byte USER_SECOND_PSW_USERMAPPINGREG_TOWRITEDIRECT = 0x66;
        #endregion
    }
}
