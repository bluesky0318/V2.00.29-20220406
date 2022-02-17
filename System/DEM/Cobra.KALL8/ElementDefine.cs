using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL08
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        #region Chip Constant
        internal const byte CELL_NUMBER = 8;
        internal const UInt16 EF_MEMORY_SIZE = 0x10;
        internal const UInt16 EF_MEMORY_OFFSET = 0x60;
        internal const UInt16 EF_ATE_OFFSET = 0x60;
        internal const UInt16 EF_ATE_TOP = 0x65;
        internal const UInt16 ATE_CRC_OFFSET = 0x65;

        internal const UInt16 EF_USR_OFFSET = 0x66;
        internal const UInt16 EF_USR_TOP = 0x6f;
        internal const UInt16 USR_CRC_OFFSET = 0x6f;

        internal const UInt16 OP_USR_OFFSET = 0x76;
        internal const UInt16 OP_USR_TOP = 0x7f;
        internal const UInt16 ATE_CRC_BUF_LEN = 23;     // 4 * 6 - 1
        internal const UInt16 USR_CRC_BUF_LEN = 39;     // 4 * 10 - 1

        internal const UInt16 CELL_OFFSET = 0x11;
        internal const UInt16 CELL_TOP = 0x18;
        internal const UInt16 SCURRENT_OFFSET = 0x19;
        internal const UInt16 MCURRENT_OFFSET = 0x1A;
        internal const UInt16 V800MV_OFFSET = 0x1C;
        internal const UInt16 VDD_OFFSET = 0x1E;

        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -9999;
        internal const UInt32 ElementMask = 0xFFFF0000;

        internal const int RETRY_COUNTER = 15;
        internal const byte WORKMODE_OFFSET = 0x50;
        internal const byte MAPPINGDISABLE_OFFSET = 0x51;
        internal const byte CB_OFFSET = 0x7D;

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpETRxM = TemperatureElement + 0x00;
        internal const UInt32 TpETRxS = TemperatureElement + 0x01;
        #endregion
        internal const UInt32 SectionMask = 0xffff0000;
        #region EFUSE参数GUID
        internal const UInt32 EFUSEElement = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 ECTO = 0x0002680e; //
        internal const UInt32 ECUT = 0x00026c00; //
        internal const UInt32 ECUT_H = 0x00026c06;
        internal const UInt32 EDUT = 0x00026c0a; //
        internal const UInt32 ECOT = 0x00026d00; //
        internal const UInt32 ECOT_H = 0x00026d08;
        internal const UInt32 EDOT = 0x00026e00; //
        internal const UInt32 EDOT_H = 0x00026e08;
        internal const UInt32 EDUT_H = 0x00026f04;
        internal const UInt32 EEOC = 0x00026f08; //

        #endregion
        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellNum = 0x00037708; //
        internal const UInt32 CellBase = 0x00031000; //

        internal const UInt32 OCTO = 0x0003780e; //
        internal const UInt32 OCUT = 0x00037c00; //
        internal const UInt32 OCUT_H = 0x00037c06;
        internal const UInt32 ODUT = 0x00037c0a; //
        internal const UInt32 OCOT = 0x00037d00; //
        internal const UInt32 OCOT_H = 0x00037d08;
        internal const UInt32 ODOT = 0x00037e00; //
        internal const UInt32 ODOT_H = 0x00037e08;
        internal const UInt32 ODUT_H = 0x00037f04;
        internal const UInt32 OEOC = 0x00037f08; //

        internal const UInt32 MCurrent = 0x00031a00; //
        internal const UInt32 SCurrent = 0x00031900; //

        #endregion
        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;

        internal const UInt32 ECTO_E = 0x000c0001; //
        internal const UInt32 ECUT_E = 0x000c0002; //
        internal const UInt32 EDUT_E = 0x000c0003; //
        internal const UInt32 EEOC_E = 0x000c0004; //

        internal const UInt32 OCTO_E = 0x000c0005; //
        internal const UInt32 OCUT_E = 0x000c0006; //
        internal const UInt32 ODUT_E = 0x000c0007; //
        internal const UInt32 OEOC_E = 0x000c0008; //
        #endregion
        #endregion
        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            VOLTAGE = 1,
            INT_TEMP,
            EXT_TEMP,
            MCURRENT = 4,
            CELLNUM = 5,
            SCURRENT = 6,
            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41,
            //OVP = 100,  //cb_start_th
            //UVP,
            //COC,
            //COT_H = 103,
            //IDLE,
            //DUT_H = 105,
            //DOT_H = 106,
            //EOC,
            //DOC2,
            //CUT = 109,
            //DUT = 110,
            //COT = 111,
            //DOT,
            //CUT_H = 113,
            //DOC1,
            CTO = 115,
            //OVP_H = 116,
            XXT_TH = 117,
            XXT_H = 118,
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_POWERON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_POWEROFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_POWERCHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        #endregion

        internal enum WORK_MODE : ushort
        {
            NORMAL = 0,
            INTERNAL = 0x01,
            PROGRAM = 0x02,
        }

        internal enum CELL_BALANCE : byte
        {
            DISABLE = 0,
            IDLE_CHARGE = 1,
            IDLE,
            CHARGE
        }

        internal enum COMMAND : ushort
        {
            TESTCTRL_SLOP_TRIM = 2,
            FROZEN_BIT_CHECK = 9,
            DIRTY_CHIP_CHECK = 10,
            DOWNLOAD_WITH_POWER_CONTROL = 11,
            DOWNLOAD_WITHOUT_POWER_CONTROL = 12,
            READ_BACK_CHECK = 13,
            ATE_CRC_CHECK = 14,
            //GET_EFUSE_HEX_DATA = 15,          //Production make Hex file, no need anymore
            //SAVE_MAPPING_HEX = 16,            //Register make Hex file, no need anymore
            SAVE_EFUSE_HEX = 17,
            GET_MAX_VALUE = 18,
            GET_MIN_VALUE = 19,
            VERIFICATION = 20,                   //Production页面的Read Back Check按钮，比 READ_BACK_CHECK 命令多一些动作
            BIN_FILE_CHECK = 21                   //检查bin文件的合法性
        }
    }
}
