using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.SeaguIIPD
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const UInt32 PARAM_WHEX_ERROR = 0xFFFFFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 RETRY_COUNTER = 5;
        internal const UInt16 BLOCK_OPERATION_BYTES = 32;
        internal static byte[] m_ROM_EP_Buf = null;//new byte[80*1024];

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_HW_CRC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_EEPROM_BUSY = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_CRC_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_SW_RESET = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        #endregion

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP = 2,
            PARAM_EXT_TEMP = 3,
            PARAM_CURRENT = 4,

            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        public enum COBRA_COMMAND_MODE : ushort
        {
            DEBUG_READ = 0x25,
            DEBUG_WRITE = 0x26,

            Robot_Read = 0x50,
            Robot_Write = 0x51,
            Robot_Formula = 0x52,
            Robot_Count = 0x53,
        }
        public enum CHIP_OPERA_MODE : ushort
        {
            SOFT_RESET = 0x01,
            CPU_HOLD = 0x02,
            CPU_RUN = 0x03
        }
        public enum EEPROM_OPERA_MODE : ushort
        {
            STAND_BY = 0x20,
            PAGE_ERASE = 0x0A,
            PROGRAM = 0x0E,
        }

        #region AHB2APB Address
        internal const UInt32 AHB2APB_BASIC_ADDR = 0x40000000;
        internal const UInt32 AHB2APB_SOFT_RESET = 0x0004;
        internal const UInt32 AHB2APB_CPU_HOLD  = 0x0808;
        internal const UInt32 AHB2APB_CPU_SLEEP = 0x0810;
        #endregion

        #region Operation Address
        internal const UInt32 BBCTRL_BASIC_ADDR = 0x50000000;
        internal const UInt32 PDCTRLExpertElement = 0x01030000;
        internal const UInt32 ARMExpertElement = 0x02030000;
        internal const UInt32 APBExpertElement = 0x03030000;
        internal const UInt32 OTPExpertElement = 0x04030000;
        internal const UInt32 OTPTrimCRC = 0x000D3C00;
        #endregion

        #region BBCTRLSystemElement
        internal const UInt32 BBCTRLSystemElement = 0x00030000;
        internal const UInt32 BBCTRL_REF_CV       = 0x00030009;
        internal const UInt32 BBCTRL_REF_CC       = 0x0003000E;
        #endregion

        #region EEPROM Address
        internal const UInt32 EP_PAGE_SIZE = 128;
        internal const UInt32 OTP_PAGE_SIZE = 64;
        internal const UInt32 EP_FIFO_SIZE = 128;
        internal const UInt32 EP_CHIP_ERASE_CMD = 0x54;
        internal const UInt32 EP_MEMORY_SIZE = 0x10000;
        internal const UInt32 OTP_MEMORY_SIZE = 0x44;
        internal const UInt32 EEPROM_START_ADDR = 0x4000;
        internal const UInt32 EEPROM_CTRL_OPERA_ADDR = 0x4604;
        internal const UInt32 EEPROM_CTRL_OPERA_MODE = 0x4600;
        internal const UInt32 EEPROM_CTRL_PROG_FIFO_ADDR = 0x4608;
        internal const UInt32 EEPROM_CTRL_TEST_CTRL_ADDR = 0x4688;
        internal static byte[] m_FIFO_Buf = new byte[EP_FIFO_SIZE];
        #endregion

        #region CRC Address
        internal const UInt32 CRC_START_ADDR = 0x4800;
        internal const UInt32 CRC_END_ADDR = 0x4804;
        internal const UInt32 CRC_CTRL_ADDR = 0x4808;
        internal const UInt32 CRC_RESULT_ADDR = 0x480C;
        #endregion

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion

        #region Trim参数GUID
        internal const UInt32 EEPROMTRIMElement = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 EEPROM_TRIM_PAGE_ADDR = 0x13F80;
        internal const UInt32 OTPTRIMElement = 0x000D0000; //EFUSE参数起始地址
        internal const UInt32 OTP_TRIM_PAGE_ADDR = 0x0000;
        #endregion

        #region EEPROM Parameter参数GUID
        internal const UInt32 Port1SystemElement = 0x00070000;
        internal const UInt32 EEPROM_P1SYS_PAGE_ADDR = 0x13D00;
        internal const UInt32 Port2SystemElement = 0x000A0000;
        internal const UInt32 EEPROM_P2SYS_PAGE_ADDR = 0x13D80;
        internal const UInt32 Port1BuckBoostElement = 0x00080000;
        internal const UInt32 EEPROM_P1BuckBoost_PAGE_ADDR = 0x13C00;
        internal const UInt32 Port2BuckBoostElement = 0x000B0000;
        internal const UInt32 EEPROM_P2BuckBoost_PAGE_ADDR = 0x13C80; 
        internal const UInt32 Port1PDElement = 0x00090000;
        internal const UInt32 Port1PD2Element = 0x000F0000;
        internal const UInt32 EEPROM_P1PD_PAGE_ADDR = 0x13B00;
        internal const UInt32 EEPROM_P1PD2_PAGE_ADDR = 0x13A00;
        internal const UInt32 Port2PDElement = 0x000C0000;
        internal const UInt32 Port2PD2Element = 0x000E0000;
        internal const UInt32 EEPROM_P2PD_PAGE_ADDR = 0x13B80;
        internal const UInt32 EEPROM_P2PD2_PAGE_ADDR = 0x13A80;
        #endregion

        #region UserConfig参数GUID
        internal const UInt32 UserConfigElement = 0x00200000;
        internal const UInt32 UserConfig_Rcs1 = 0x00200000;
        internal const UInt32 UserConfig_Rcs2 = 0x00200001;

        internal const UInt32 UserConfig_OTG = 0x00030007;
        internal const UInt32 UserConfig_CSA_GAIN_SEL = 0x01030151;
        internal const UInt32 UserConfig_REF_CV = 0x00030009;
        internal const UInt32 UserConfig_REF_CC = 0x0003000E;
        internal const UInt32 UserConfig_CHANNEL6 = 0x0103010C;
        internal const UInt32 UserConfig_CHANNEL7 = 0x0103010D;
        #endregion

        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x07030000;
        internal const UInt32 VirtualSWCRC = 0x07030001;
        #endregion
    }
}
