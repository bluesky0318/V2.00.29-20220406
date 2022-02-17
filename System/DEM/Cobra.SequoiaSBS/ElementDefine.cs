using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.SequoiaSBS
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
        internal const UInt16 ONE_PAGE_SIZE = 0x400;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt32 CommandMask = 0x0000FF00;
        internal const UInt32 Command2Mask = 0x0000FFF0;
        internal const byte SBSBatteryMode = 0x03;
        internal const UInt16 TotalCellNum = 10;
        internal const UInt16 TotalExtNum = 4;
        internal const UInt16 CommonDataLen = 2;
        internal const UInt16 MaxBytesNum = 32;
        internal const UInt16 BLOCK_OPERATION_BYTES = 32;

        internal const UInt16 DFEController_StartAddress = 0xC900;
        internal const UInt32 DFEController_Size = 1024; //1K

        internal const byte RegI2C_Unlock_CfgWrt = 0x94;
        internal const byte RegI2C_Unlock_PwrmdWrt = 0xAA;
        internal const byte RegI2C_Unlock_I2cCfgWrt = 0x11;
        internal const byte RegI2C_AHB_MODE = 0x12;
        internal const byte RegI2C_MEMD = 0x20;

        internal static byte[] interBuffer = new byte[1024 * 64];

        #region PASSWORD
        internal const UInt16 Unlock_Erase_PSW = 0xABCD;
        internal const UInt16 UnLock_I2C_PSW = 0x7918;
        internal const UInt16 ReLock_I2C_PSW = 0x0918;
        internal const UInt16 I2C_AHB_MODE_Enable_PSW = 0x7901;
        internal const UInt16 I2C_AHB_MODE_Default_PSW = 0x7900;
        internal const UInt16 I2C_MEMD_PWD = 0x9A50;
        #endregion

        public enum COBRA_FLASH_OP : ushort
        {
            HI_WORD,
            LO_WORD
        }

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
        }

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
            SUB_TASK_COMPARE = 0x11,
            SUB_TASK_ERASE = 0x12,
        }


        #region SBS参数GUID
        internal const UInt32 SBSElement = 0x00020000;
        internal const UInt32 MfgAccess = 0x00020000;
        internal const UInt32 BatteryMode = 0x00020320;
        internal const UInt32 Temp = 0x00020800;
        internal const UInt32 Volt = 0x00020900;
        internal const UInt32 Curr = 0x00020A00;
        internal const UInt32 AvgCurr = 0x00020B00;
        internal const UInt32 MaxErr = 0x00020C00;
        internal const UInt32 RSOC = 0x00020D00;
        internal const UInt32 ASOC = 0x00020E00;
        internal const UInt32 RC = 0x00020F00;
        internal const UInt32 FCC = 0x00021000;
        internal const UInt32 RTTE = 0x00021100;
        internal const UInt32 ATTE = 0x00021200;
        internal const UInt32 ATTF = 0x00021300;
        internal const UInt32 BatteryStatus = 0x00021620;
        internal const UInt32 CycleCount = 0x00021700;
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
        internal const UInt32 CellVoltMV03 = 0x00023E00;
        internal const UInt32 CellVoltMV04 = 0x00023F00;
        internal const UInt32 CellVoltMV05 = 0x00024000;
        internal const UInt32 CellVoltMV06 = 0x00024100;
        internal const UInt32 CellVoltMV07 = 0x00024200;
        internal const UInt32 CellVoltMV08 = 0x00024300;
        internal const UInt32 CellVoltMV09 = 0x00024400;
        internal const UInt32 CellVoltMV10 = 0x00024500;
        internal const UInt32 CellVoltMV11 = 0x00024600;
        internal const UInt32 CellVoltMV12 = 0x00024700;
        internal const UInt32 CellVoltMV13 = 0x00024800;
        internal const UInt32 CellVoltMV14 = 0x00024900;
        internal const UInt32 PackVoltMV = 0x00025C00;
        internal const UInt32 PackCurrMA1 = 0x00025D00;
        internal const UInt32 PackCurrMA2 = 0x00025E00;
        internal const UInt32 ITDK = 0x00026000;
        internal const UInt32 ETDK1 = 0x00026100;
        internal const UInt32 ETDK2 = 0x00026200;
        internal const UInt32 ETDK3 = 0x00026300;
        internal const UInt32 ETDK4  = 0x00026400;
        internal const UInt32 Status1 = 0x00026520;
        internal const UInt32 Status2 = 0x00026620;
        #endregion

        #region SBS参数GUID
        internal const UInt32 CONFIGElement = 0x00030000;
        #endregion
    }
}
