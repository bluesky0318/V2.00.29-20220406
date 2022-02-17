    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Azalea10
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 YFLASH_MEMORY_SIZE    = 0x1F;
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -999999;
        internal const UInt32 ElementMask           = 0xFFFF0000;

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
                PARAM_VOLTAGE = 1,
                PARAM_INT_TEMP,
                PARAM_EXT_TEMP,
                PARAM_CURRENT = 4,
                PARAM_DOCTH,
                PARAM_EXT_TEMP_TABLE = 40,
                PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_AZALEA10_WKM :  ushort
        {
            YFLASH_WORKMODE_NORMAL = 0,
                YFLASH_WORKMODE_DATA_READ_OUT = 0x02,
                YFLASH_WORKMODE_BANDGAP = 0x03,
                YFLASH_WORKMODE_OSCBGTRM = 0x04,
                YFLASH_WORKMODE_DOCTRM = 0x05,
		        YFLASH_WORKMODE_THMTRM = 0x06,
		        YFLASH_WORKMODE_ADC_TEST = 0x07,
		        YFLASH_WORKMODE_INT_TEMP_OFFSETTRM = 0x08,
                YFLASH_WORKMODE_CHANNEL_SLOPTRM = 0x09,
                YFLASH_WORKMODE_DATA_PREPARATION = 0x0A,
                YFLASH_WORKMODE_ERASE = 0x0B, 
		        YFLASH_WORKMODE_PROGRAMMING = 0x0C,
		        YFLASH_WORKMODE_VPP_FUSEBLOW = 0x0D, 
		        YFLASH_WORKMODE_CELL_BALANCE_TEST = 0x0E,
                YFLASH_WORKMODE_OBSERVEINTERNAL_KEYSIGNAL = 0x0F,
                YFLASH_WORKMODE_MAP = 0x10
        }

        internal enum COBRA_AZALEA10_TESTCTR : ushort
        {
            YFLASH_TESTCTR_NORMAL = 1,
                YFLASH_TESTCTR_WORKMODE = 2
        }

        internal enum COBRA_AZALEA10_ATELCK  : ushort
        {
            YFLASH_ATELCK_MATCHED = 0,
                YFLASH_ATELCK_UNMATCHED,
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
        
        #region YFLASH参数GUID
        internal const UInt32 YFLASHElement = 0x00020000; //YFLASH参数起始地址

        internal const UInt32 YFOSCFrequency                        = YFLASHElement + 0x0000;
        internal const UInt32 YFLDO33voffset                        = YFLASHElement + 0x0400;
        internal const UInt32 YFInternalTemperatureRefer            = YFLASHElement + 0x0200;
        internal const UInt32 YFCOCOffset                           = YFLASHElement + 0x0304;
        internal const UInt32 YFDOCOffset                           = YFLASHElement + 0x0404;
        internal const UInt32 YFSCOffset                            = YFLASHElement + 0x0300;
        internal const UInt32 YFCell12ndOffset                      = YFLASHElement + 0x0500;
        internal const UInt32 YFCell22ndOffset                      = YFLASHElement + 0x0600;
        internal const UInt32 YFCell32ndOffset                      = YFLASHElement + 0x0700;
        internal const UInt32 YFCell42ndOffset                      = YFLASHElement + 0x0800;
        internal const UInt32 YFCell52ndOffset                      = YFLASHElement + 0x0900;
        internal const UInt32 YFCell62ndOffset                      = YFLASHElement + 0x0a00;
        internal const UInt32 YFCell72ndOffset                      = YFLASHElement + 0x0b00;
        internal const UInt32 YFCell82ndOffset                      = YFLASHElement + 0x0c00;
        internal const UInt32 YFOSCTempTrim                         = YFLASHElement + 0x1204;
        internal const UInt32 YFOSCFreqTrim                         = YFLASHElement + 0x1200;
        internal const UInt32 YFBGPTrim                             = YFLASHElement + 0x1300;
        internal const UInt32 YFBGPTempTrim                         = YFLASHElement + 0x1400;
        internal const UInt32 YFCurrentOffsetShift                  = YFLASHElement + 0x1307;
        internal const UInt32 YFWakeupIntegratorDisable             = YFLASHElement + 0x1404;
        internal const UInt32 YFCOCSlopeEnable                      = YFLASHElement + 0x1501;
        internal const UInt32 YFATEFreeze                           = YFLASHElement + 0x1507;
        internal const UInt32 YFGPIO12ndOffset                      = YFLASHElement + 0x5e00;
        internal const UInt32 YFGPIO22ndOffset                      = YFLASHElement + 0x5f00;
        internal const UInt32 YFGPIO32ndOffset                      = YFLASHElement + 0x6000;
        internal const UInt32 YFCurrent2ndOffset                    = YFLASHElement + 0x5d00;
        internal const UInt32 YFCellNumber                          = YFLASHElement + 0x1600;
        internal const UInt32 YFI2CAddress                          = YFLASHElement + 0x1602;
        internal const UInt32 YFRsense                              = YFLASHElement + 0x6300;
        internal const UInt32 YFCOCSlope                            = YFLASHElement + 0x6100;
        internal const UInt32 YFScanRate                            = YFLASHElement + 0x1900;
        internal const UInt32 YFOC0Threshold                        = YFLASHElement + 0x2d00;
        internal const UInt32 YFOC0DelayTime                        = YFLASHElement + 0x2700;
        internal const UInt32 YFOC0ReleaseTime                      = YFLASHElement + 0x2703;
        internal const UInt32 YFCOCThreshold                        = YFLASHElement + 0x1e00;
        internal const UInt32 YFDOCThreshold                        = YFLASHElement + 0x1f00;
        internal const UInt32 YFSCThreshold                         = YFLASHElement + 0x2200;
        internal const UInt32 YFCOCReleaseTime                      = YFLASHElement + 0x2100;
        internal const UInt32 YFDOCReleaseTime                      = YFLASHElement + 0x2103;
        internal const UInt32 YFSCReleaseTime                       = YFLASHElement + 0x2400;
        internal const UInt32 YFOCDelay                             = YFLASHElement + 0x2000;
        internal const UInt32 YFSCDelay                             = YFLASHElement + 0x2300;
        internal const UInt32 YFOverVoltageTH                       = YFLASHElement + 0x3804;
        internal const UInt32 YFOVRelease                           = YFLASHElement + 0x3a04;
        internal const UInt32 YFUVThreshold                         = YFLASHElement + 0x3c04;
        internal const UInt32 YFUVReleaseThreshold                  = YFLASHElement + 0x3e04;
        internal const UInt32 YFOVUVDelay                           = YFLASHElement + 0x2500;
        internal const UInt32 YFRVDelay                             = YFLASHElement + 0x1a06;
        internal const UInt32 YFExtTCh1Enable                       = YFLASHElement + 0x1a00;
        internal const UInt32 YFExtTCh2Enable                       = YFLASHElement + 0x1a02;
        internal const UInt32 YFExtTemp3Enable                      = YFLASHElement + 0x1a04;
        internal const UInt32 YFIntOverTempTH                       = YFLASHElement + 0x4604;
        internal const UInt32 YFIntOverTempRelease                  = YFLASHElement + 0x4804;
        internal const UInt32 YFIntUnderTempTH                      = YFLASHElement + 0x4a04;
        internal const UInt32 YFIntUnderTempRelease                 = YFLASHElement + 0x4c04;
        internal const UInt32 YFExtChgOTTHreshold                   = YFLASHElement + 0x4e04;
        internal const UInt32 YFExtChgOTRelease                     = YFLASHElement + 0x5004;
        internal const UInt32 YFExtDsgOTTHreshold                   = YFLASHElement + 0x2804;
        internal const UInt32 YFExtDsgOTRelease                     = YFLASHElement + 0x2a04;
        internal const UInt32 YFExtUnderTempTH                      = YFLASHElement + 0x5204;
        internal const UInt32 YFExtUnderTempRelease                 = YFLASHElement + 0x5404;
        internal const UInt32 YFOTUTDelay                           = YFLASHElement + 0x2504;
        internal const UInt32 YFPFRecord                            = YFLASHElement + 0x7c00;
        internal const UInt32 YFPFVHEnable                          = YFLASHElement + 0x2607;
        internal const UInt32 YFPFVLEnable                          = YFLASHElement + 0x2606;
        internal const UInt32 YFPFDelay                             = YFLASHElement + 0x2600;
        internal const UInt32 YFPFVHThreshold                       = YFLASHElement + 0x4100;
        internal const UInt32 YFPFVLThreshold                       = YFLASHElement + 0x4204;
        internal const UInt32 YFPFMOSEnable                         = YFLASHElement + 0x2604;
        internal const UInt32 YFPFUnbalanceEnable                   = YFLASHElement + 0x2605;
        internal const UInt32 YFPFUnbalanceTH                       = YFLASHElement + 0x4404;
        internal const UInt32 YFAutoScanEnable                      = YFLASHElement + 0x1906;
        internal const UInt32 YFModeSelect                          = YFLASHElement + 0x1700;
        internal const UInt32 YFPrechargeEnable                     = YFLASHElement + 0x1802;
        internal const UInt32 YFPECEnable                           = YFLASHElement + 0x1606;
        internal const UInt32 YFEFETCMode                           = YFLASHElement + 0x1804;
        internal const UInt32 YFParaAccessCtrl                      = YFLASHElement + 0x5b06;
        internal const UInt32 YFCHGCurrentTHreshold                 = YFLASHElement + 0x1b04;
        internal const UInt32 YFDSGCurrentTHreshold                 = YFLASHElement + 0x1b00;
        internal const UInt32 YFSleepEnable                         = YFLASHElement + 0x1800;
        internal const UInt32 YFSleepWakeupTime                     = YFLASHElement + 0x1c00;
        internal const UInt32 YFChgWakeupCurrentTH                  = YFLASHElement + 0x1c06;
        internal const UInt32 YFDsgWakeupCurrentTH                  = YFLASHElement + 0x1c04;
        internal const UInt32 YFBleedEnable                         = YFLASHElement + 0x1801;
        internal const UInt32 YFExtBleedSel                         = YFLASHElement + 0x1d07;
        internal const UInt32 YFBleedCellNumber                     = YFLASHElement + 0x1d00;
        internal const UInt32 YFBleedAllEnable                      = YFLASHElement + 0x1d02;
        internal const UInt32 YFIdleBleedEnable                     = YFLASHElement + 0x1d03;
        internal const UInt32 YFBleedStartVoltage                   = YFLASHElement + 0x5604;
        internal const UInt32 YFBleedDeltaVol                       = YFLASHElement + 0x1d04;
        internal const UInt32 YFPassAccessCtrl                      = YFLASHElement + 0x7b07;
        internal const UInt32 YFPassword                            = YFLASHElement + 0x7700;
        internal const UInt32 YFProjectInfoAccess                   = YFLASHElement + 0x7507;
        internal const UInt32 YFFactoryName                         = YFLASHElement + 0x6400;
        internal const UInt32 YFProjectName                         = YFLASHElement + 0x6e00;
        internal const UInt32 YFVersionNumber                       = YFLASHElement + 0x7300;

        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;

        #endregion
    }
}
