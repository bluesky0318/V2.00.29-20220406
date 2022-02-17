using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Azalea5H
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 EPROM_MEMORY_SIZE = 0x1F;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 TestCtrl_FLAG = 0x000F;
        internal const UInt16 ALLOW_WR_CLEAR_FLAG = 0x7FFF;
        internal static int m_trim_count = 0;
        internal const UInt16 TotalCellNum = 5;
        internal const UInt16 DEFAULT_OSR = 0x0200;

        internal const int RETRY_COUNT = 5;
        internal const byte CADCRTL_REG = 0x38;
        internal const byte INTR2_REG = 0x65;
        internal const byte FinalCadcMovingData_Reg = 0x39;
        internal const byte FinalCadcTriggerData_Reg = 0x3F;

        #region SCAN操作常量定义
        internal const byte TRIGGER_SCAN_REG = 0x35;
        internal const UInt16 TRIGGER_SCAN_ONE_MODE = 0x2000;
        internal const UInt16 TRIGGER_SCAN_EIGHT_MODE = 0x6000;
        internal const UInt16 TRIGGER_SCAN_REQ_SINGLE = 0x0100;
        #endregion

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_TIGGER_SCAN_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_ERROR_MODE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_POWERON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_POWEROFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_POWERCHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        internal const UInt32 IDS_ERR_DEM_ATE_EMPTY_CHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        internal const UInt32 IDS_ERR_DEM_CELLNUMBER_OVERRANGE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0009;
        #endregion

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP = 2,
            PARAM_EXT_TEMP = 3,
            PARAM_CURRENT = 4,
            PARAM_CADC = 5,

            PARAM_SIGN_ORG = 20,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_AZALEA5H_WKM : ushort
        {
            EEPROM_WORKMODE_NORMAL = 0,
            EEPROM_WORKMODE_WRITE_MAP_CTRL = 0x01,
            EEPROM_WORKMODE_PROGRAM = 0x02,
        }

        public enum COBRA_COMMAND_MODE : ushort
        {
            FROZEN_BIT_CHECK = 0x09,
            DIRTY_CHIP_CHECK = 0x0A,
            DOWNLOAD_WITH_POWER_CONTROL = 0x0B,
            DOWNLOAD_WITHOUT_POWER_CONTROL = 0x0C,
            READ_BACK_CHECK = 0x0D,
            ATE_CRC_CHECK = 0x0E,
            //GET_EFUSE_HEX_DATA = 15,          //Production make Hex file, no need anymore
            //SAVE_MAPPING_HEX = 16,            //Register make Hex file, no need anymore
            SAVE_EFUSE_HEX = 0x11,
            GET_MAX_VALUE = 0x12,
            GET_MIN_VALUE = 0x13,
            VERIFICATION = 0x14,                   //Production页面的Read Back Check按钮，比 READ_BACK_CHECK 命令多一些动作
            BIN_FILE_CHECK = 0x15,                   //检查bin文件的合法性
            ATE_EMPTY_CHECK = 0x16,                   //检查ATE区域是否为空

            TRIGGER_SCAN_EIGHT_MODE = 0x20,
            TRIGGER_SCAN_ONE_MODE = 0x21,
            SCS_TRIGGER_SCAN_EIGHT_MODE = 0x31,
            TRIGGER_SCAN_UI = 0x32,

            TRIM_COUNT_SLOPE = 0x40,
            TRIM_COUNT_OFFSET = 0x41,
            TRIM_COUNT_RESET = 0x42,
            TRIM_SLOPE_EIGHT_MODE = 0x43,
            TRIM_OFFSET_EIGHT_MODE = 0x44,
            INVALID_COMMAND = 0xFFFF,
        }

        public enum CADC_MODE : byte
        {
            DISABLE = 0,
            MOVING = 1,
            TRIGGER = 2,
        }

        public enum SCAN_MODE : byte
        {
            TRIGGER = 0,
            AUTO = 1,
        }

        #region TRIGGER_SCAN_CHANNEL
        internal enum COBRA_TRIGGER_SCAN_CHANNEL : ushort //Burst不存在于SW
        {
            TRIGGER_SCAN_CHANNEL_INTEL_TEMP,//0x00: do nothing;
            TRIGGER_SCAN_CHANNEL_CELL1,
            TRIGGER_SCAN_CHANNEL_CELL2,
            TRIGGER_SCAN_CHANNEL_CELL3,
            TRIGGER_SCAN_CHANNEL_CELL4,
            TRIGGER_SCAN_CHANNEL_CELL5,
            TRIGGER_SCAN_CHANNEL_ISENS = 0x06,
            TRIGGER_SCAN_CHANNEL_TS, 
            TRIGGER_SCAN_CHANNEL_INTMP,
            TRIGGER_SCAN_CHANNEL_VBATT,
            TRIGGER_SCAN_CHANNEL_VCC,// for VCC channel;
            TRIGGER_SCAN_CHANNEL_V50V,//0x15: for V50V channel;
            TRIGGER_SCAN_CHANNEL_VMCU,
            TRIGGER_SCAN_CHANNEL_VM,
            TRIGGER_SCAN_CHANNEL_THM,
            TRIGGER_SCAN_CHANNEL_GPIO1,
        }
        #endregion

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion

        #region EFUSE参数GUID
        internal const UInt32 EPROMElement = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 EEPROM_CADC_Slope_Trim = 0x00020300;
        internal const UInt32 EEPROM_Cell01_Slope_Trim = 0x00020400;
        internal const UInt32 EEPROM_Cell02_Slope_Trim = 0x00020408;
        internal const UInt32 EEPROM_Cell03_Slope_Trim = 0x00020500;
        internal const UInt32 EEPROM_Cell04_Slope_Trim = 0x00020508;
        internal const UInt32 EEPROM_Cell05_Slope_Trim = 0x00020600;
        internal const UInt32 EEPROM_TS_Slope_Trim = 0x00020608;
        internal const UInt32 EEPROM_AUX_Slope_Trim = 0x00020700;
        internal const UInt32 EEPROM_VBATT_VCC_Slope_Trim = 0x00020708;
        internal const UInt32 EEPROM_ISENS_Slope_Trim = 0x00020E00;

        internal const UInt32 EEPROM_CADC_Offset = 0x00020308;
        internal const UInt32 EEPROM_Cell01_Offset = 0x00020800;
        internal const UInt32 EEPROM_Cell02_Offset = 0x00020808;
        internal const UInt32 EEPROM_Cell03_Offset = 0x00020900;
        internal const UInt32 EEPROM_Cell04_Offset = 0x00020908;
        internal const UInt32 EEPROM_Cell05_Offset = 0x00020A00;
        internal const UInt32 EEPROM_TS_Offset = 0x00020A08;
        internal const UInt32 EEPROM_AUX_Offset = 0x00020B00;
        internal const UInt32 EEPROM_VBATT_VCC_Offset = 0x00020B08;
        internal const UInt32 EEPROM_ISENS_Offset = 0x00020E08;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellNumber = 0x0003B009;
        internal const UInt32 CADC = 0x00033F00;
        internal const UInt32 CADC_Slope_Trim = 0x0003A300;
        internal const UInt32 CADC_Offset_Trim = 0x0003A308;

        internal const UInt32 CellVoltage01 = 0x00031100;
        internal const UInt32 Cell1_Slope_Trim = 0x0003A400;
        internal const UInt32 Cell1_Offset_Trim = 0x0003A800;

        internal const UInt32 CellVoltage02 = 0x00031200;
        internal const UInt32 Cell2_Slope_Trim = 0x0003A408;
        internal const UInt32 Cell2_Offset_Trim = 0x0003A808;

        internal const UInt32 CellVoltage03 = 0x00031300;
        internal const UInt32 Cell3_Slope_Trim = 0x0003A500;
        internal const UInt32 Cell3_Offset_Trim = 0x0003A900;

        internal const UInt32 CellVoltage04 = 0x00031400;
        internal const UInt32 Cell4_Slope_Trim = 0x0003A508;
        internal const UInt32 Cell4_Offset_Trim = 0x0003A908;

        internal const UInt32 CellVoltage05 = 0x00031500;
        internal const UInt32 Cell5_Slope_Trim = 0x0003A600;
        internal const UInt32 Cell5_Offset_Trim = 0x0003AA00;

        internal const UInt32 Isens = 0x00032100;
        internal const UInt32 Isens_Slope_Trim = 0x0003AE00;
        internal const UInt32 Isens_Offset_Trim = 0x0003AE08;

        internal const UInt32 VBATT = 0x00032400;
        internal const UInt32 VBATT_Slope_Trim = 0x0003A708;
        internal const UInt32 VBATT_Offset_Trim = 0x0003AB08;

        internal const UInt32 TS = 0x00032200;
        internal const UInt32 TS_Slope_Trim = 0x0003A608;
        internal const UInt32 TS_Offset_Trim = 0x0003AA08;

        internal const UInt32 AUX = 0x00032900;
        internal const UInt32 AUX_Slope_Trim = 0x0003A700;
        internal const UInt32 AUX_Offset_Trim = 0x0003AB00;

        internal const UInt32 Op_ov1p_th = 0x00035000;
        internal const UInt32 Op_uvp_th = 0x00035100;
        internal const UInt32 Op_scp_th = 0x00035107;
        internal const UInt32 Op_cocp_th = 0x00035200;
        internal const UInt32 Op_doc1p_th = 0x00035208;
        internal const UInt32 Op_doc2p_th = 0x00035300;
        internal const UInt32 Op_dot_th = 0x00035500;
        internal const UInt32 Op_cot_th = 0x00035600;
        internal const UInt32 Op_uv_shutdown_th = 0x00035800;
        internal const UInt32 Op_uv_shutdown_dly = 0x0003580C;
        #endregion
    }

    internal class DataPoint
    {
        private double[] input = new double[5];
        private double[] output = new double[5];
        public Parameter parent = null;

        public DataPoint(Parameter param)
        {
            parent = param;
        }

        public void SetInput(double din)
        {
            input[ElementDefine.m_trim_count] = din;
        }

        public void SetOutput(double dou)
        {
            output[ElementDefine.m_trim_count] = dou;
        }

        public UInt32 GetSlope(ref double slope)
        {
            StringBuilder strIn = new StringBuilder();
            StringBuilder strOu = new StringBuilder();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (input == null)
                throw new ArgumentNullException("xArray");
            if (output == null)
                throw new ArgumentNullException("yArray");
            if (input.Length != output.Length)
                throw new ArgumentException("Array Length Mismatch");
            if (input.Length < 2)
                throw new ArgumentException("Arrays too short.");

            double n = output.Length;
            double sumxy = 0, sumx = 0, sumy = 0, sumx2 = 0;
            for (int i = 0; i < output.Length; i++)
            {
                sumxy += output[i] * input[i];
                sumx += output[i];
                sumy += input[i];
                sumx2 += output[i] * output[i];
            }
            slope = ((sumxy - sumx * sumy / n) / (sumx2 - sumx * sumx / n));
            FolderMap.WriteFile("--------------------Count Slope-----------------------------------\n");
            //slope = ((sumx2 - sumx * sumx / n) / (sumxy - sumx * sumy / n));
            for (int i = 0; i < input.Length; i++)
            {
                strIn.Append(string.Format("{0:N4}", input[i]));
                strIn.Append("--");
                strOu.Append(string.Format("{0:N4}", output[i]));
                strOu.Append("--");
            }
            FolderMap.WriteFile(string.Format("Input:{0} \n Output:{1} \n slope:{2}", strIn.ToString(), strOu.ToString(), slope));
            return ret;
        }

        public UInt32 GetOffset(ref double offset)
        {
            double ddata = 0;
            StringBuilder strIn = new StringBuilder();
            StringBuilder strOu = new StringBuilder();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (output == null)
                throw new ArgumentNullException("xArray");
            if (input == null)
                throw new ArgumentNullException("yArray");
            if (output.Length != input.Length)
                throw new ArgumentException("Array Length Mismatch");
            if (output.Length < 2)
                throw new ArgumentException("Arrays too short.");

            /*for (int i = 0; i < input.Length; i++)
                ddata += (output[i] - input[i]);
            offset = ddata / input.Length;*/
            offset = output[0] - input[0];

            FolderMap.WriteFile("--------------------Count Offset-----------------------------------\n");
            for (int i = 0; i < input.Length; i++)
            {
                strIn.Append(string.Format("{0:N4}", input[i]));
                strIn.Append("--");
                strOu.Append(string.Format("{0:N4}", output[i]));
                strOu.Append("--");
            }
            FolderMap.WriteFile(string.Format("Input:{0} \n Output:{1} \n offset:{2}", strIn.ToString(), strOu.ToString(), offset));
            return ret;
        }
    }
}
