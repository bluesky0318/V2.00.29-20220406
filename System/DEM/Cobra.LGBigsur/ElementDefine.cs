using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.LGBigsur
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const int RETRY_COUNTER = 5;
        internal const UInt16 BLOCK_OPERATION_BYTES = 32;

        internal const UInt16 MTP_MEMORY_SIZE = 0x20;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 VIRTUAL_MEMORY_SIZE = 0x23;

        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt32 CommandMask = 0x0000FF00;

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_UNLOCK_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_MAINBLOCK_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_PAGE_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_INFO_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_SYS_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_CRC16_DONE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_CRC16_COMPARE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        internal const UInt32 IDS_ERR_DEM_BLK_ACCESS = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        #endregion

        #region PASSWORD
        internal const UInt16 Unlock_Erase_PSW = 0xABCD;
        internal const UInt16 UnLock_I2C_PSW = 0x6318;
        internal const UInt16 ReLock_I2C_PSW = 0x0618;
        internal const UInt16 I2C_AHB_MODE_Enable_PSW = 0x6301;
        #endregion

        internal enum COBRA_DATA_LEN : ushort
        {
            DATA_LEN_FOUR_BYTES = 4,
            DATA_LEN_THIRTY_TWO_BYTES = 32,
        }

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_TEMP,
            PARAM_DWORD = 4,
            PARAM_SIGNED = 5,
            PARAM_DATE = 8,
            PARAM_STRING = 9,
            PARAM_STATUS1 = 20,
            PARAM_STATUS2 = 21,
            PARAM_BATTMODE = 22,
            PARAM_SPECIFICTION_INFO = 23,
            PARAM_BATTSTATUS = 24,

            PARAM_DHG_CHG_Threhold = 30,
            PARAM_DOC2M = 31,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_NEWTON_MEMORY_TYPE : ushort
        {
            MEMORY_TYPE_MaineFlash,
            MEMORY_TYPE_InfoeFlash,
            MEMORY_TYPE_SyseFlash,
            MEMORY_TYPE_eFlashController = 0x03,
            MEMORY_TYPE_SRAM,
            MEMORY_TYPE_SRAMController,
            MEMORY_TYPE_SystemController,
            MEMORY_TYPE_I2CController = 0x07,
            MEMORY_TYPE_TIMER1Registers,
            MEMORY_TYPE_TIMER2Registers,
            MEMORY_TYPE_WDTRegisters = 0x0A,
            MEMORY_TYPE_UARTRegisters,
            MEMORY_TYPE_ADCController,
            MEMORY_TYPE_PadMuxRegisters,
        }

        internal enum COBRA_COMMAND_MODE : ushort
        {
            MAIN_BLOCK_PROGRAM = 0x10,
            DEBUG_READ = 0x25,
            DEBUG_WRITE = 0x26,
        }

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpN40D = TemperatureElement + 0x00;
        internal const UInt32 TpN35D = TemperatureElement + 0x01;
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
        internal const UInt32 TpP95D = TemperatureElement + 0x1B;
        internal const UInt32 TpP100D = TemperatureElement + 0x1C;
        internal const UInt32 TpP105D = TemperatureElement + 0x1D;
        internal const UInt32 TpP110D = TemperatureElement + 0x1E;
        internal const UInt32 TpP115D = TemperatureElement + 0x1F;
        internal const UInt32 TpP120D = TemperatureElement + 0x20;
        internal const UInt32 TpMainRsense = TemperatureElement + 0x21;
        internal const UInt32 TpSlaveRsense = TemperatureElement + 0x22;
        #endregion

        #region MTP参数GUID
        internal const UInt32 MTPElement = 0x00020000; //MTP参数起始地址       

        #endregion

        #region SBS参数GUID
        internal const UInt32 SBSElement = 0x00020000;
        internal const UInt32 BatteryMode = 0x00020320;
        internal const UInt32 Temp = 0x00020800;
        internal const UInt32 Volt = 0x00020900;
        internal const UInt32 Curr = 0x00020A00;
        internal const UInt32 AvgCurr = 0x00020B00;
        internal const UInt32 RSOC = 0x00020D00;
        internal const UInt32 ASOC = 0x00020E00;
        internal const UInt32 RC = 0x00020F00;
        internal const UInt32 FCC = 0x00021000;
        internal const UInt32 RTTE = 0x00021100;
        internal const UInt32 ATTE = 0x00021200;
        internal const UInt32 ATTF = 0x00021300;
        internal const UInt32 BatteryStatus = 0x00021620;
        internal const UInt32 DesignCap = 0x00021800;
        internal const UInt32 DesignVolt = 0x00021900;
        internal const UInt32 SpecInfo = 0x00021A20;
        internal const UInt32 MfgDate = 0x00021B00;
        internal const UInt32 SerialNo = 0x00021C00;
        internal const UInt32 MfgName = 0x00022000;
        internal const UInt32 DevName = 0x00022100;
        internal const UInt32 DevChem = 0x00022200;
        internal const UInt32 MfgData = 0x00022300;
        internal const UInt32 CellVoltMV01 = 0x00023C00;
        internal const UInt32 CellVoltMV02 = 0x00023D00;
        internal const UInt32 PackVoltMV = 0x00025C00;
        internal const UInt32 PackCurrMA1 = 0x00025D00;
        internal const UInt32 ITDK = 0x00026000;
        internal const UInt32 ETDK1 = 0x00026100;
        internal const UInt32 FDA = 0x00026F20;
        internal const UInt32 UC = 0x0002F000;
        #endregion

        #region Log参数GUID
        internal const UInt32 LogElement = 0x00060000;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        #endregion

        #region Virtual参数GUID
        internal const UInt32 VirtualElement = 0x00070000; //Virtual参数起始地址    
        #endregion

        #region Memory Address
        internal const UInt16 MaineFlash_StartAddress = 0x0000;
        public const UInt32 MaineFlash_Size = 65536; //16K

        internal const UInt16 LogFlash_StartAddress = 0x3D00; //0xF400
        internal const UInt16 LogFlash_EraseCounter1 = 0x3D01; //0xF404
        internal const UInt16 LogFlash_EraseCounter2 = 0x3E01; //0xF804
        internal const UInt16 LogFlash_EraseCounter3 = 0x3F01; //0xFC04 
        internal const UInt32 LogFlash_Size = 1024; //1K

        internal const UInt16 InfoFlash_StartAddress = 0x5000;
        internal const UInt32 InfoeFlash_Size = 1024; //1K

        internal const UInt16 SyseFlash_StartAddress = 0x6000;
        internal const UInt32 SyseFlash_Size = 512;

        internal const UInt16 eFlashController_StartAddress = 0x7000;
        internal const UInt32 eFlashController_Size = 512;

        internal const UInt16 SRAM_StartAddress = 0x8000;
        internal const UInt32 SRAM_Size = 4096; //4K

        internal const UInt16 SRAMController_StartAddress = 0xA000;
        internal const UInt32 SRAMController_Size = 512;

        internal const UInt16 SystemController_StartAddress = 0xC000;
        internal const UInt32 SystemController_Size = 1024; //1K

        internal const UInt16 I2CController_StartAddress = 0xC100;
        internal const UInt32 I2CController_Size = 1024; //1K

        internal const UInt16 TIMER1Registers_StartAddress = 0xC200;
        internal const UInt32 TIMER1Registers_Size = 1024; //1K

        internal const UInt16 TIMER2Registers_StartAddress = 0xC300;
        internal const UInt32 TIMER2Registers_Size = 1024; //1K

        internal const UInt16 WDTRegisters_StartAddress = 0xC400;
        internal const UInt32 WDTRegisters_Size = 1024; //1K

        internal const UInt16 UARTRegisters_StartAddress = 0xC500;
        internal const UInt32 UARTRegisters_Size = 1024; //1K

        internal const UInt16 ADCController_StartAddress = 0xC600;
        internal const UInt32 ADCController_Size = 1024; //1K

        internal const UInt16 PadMuxRegisters_StartAddress = 0xC700;
        internal const UInt32 PadMuxRegisters_Size = 1024; //1K
        #endregion

        #region I2C Address
        internal const byte I2C_Adress_STATUS = 0xCB;
        internal const UInt16 I2C_Adress_UNLOCK_ERASE = 0x7008; //0x0020
        internal const UInt16 I2C_Adress_MAIN_ERASE = 0x7009; //0x0024
        internal const UInt16 I2C_Adress_SYS_ERASE = 0x700C; //0x0030
        internal const UInt16 I2C_Adress_O2BLPROT = 0x7002; //0x0008
        internal const UInt16 I2C_Adress_PAGE_ERASE = 0x700A; //0x0028
        internal const UInt16 I2C_Adress_INFO_ERASE = 0x700B; //0x002c
        internal const UInt16 I2C_Adress_Start_Address = 0x7014; //0x0050
        internal const UInt16 I2C_Adress_End_Address = 0x7015; //0x0054
        internal const UInt16 I2C_Adress_CRC16_Result = 0x7016; //0x0058
        internal const UInt16 I2C_Adress_DO_CRC16 = 0x7017; //0x005C
        #endregion
    }
}
