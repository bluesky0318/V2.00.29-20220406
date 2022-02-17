using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ93506
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 MTP_MEMORY_SIZE = 0x20;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 VIRTUAL_MEMORY_SIZE = 0x23;

        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal static int m_trim_count = 0;

        internal const byte CADCRTL_REG = 0x63;
        internal const byte FinalCadcMovingData_Reg = 0x67;
        internal const byte FinalCadcTriggerData_Reg = 0x65;

        #region SCAN操作常量定义
        internal const byte AUTO_SCAN_REG = 0xA1;
        internal const UInt16 AUTO_SCAN_ONE_MODE = 0x000D;
        internal const UInt16 AUTO_SCAN_EIGHT_MODE = 0x000F;

        internal const byte TRIGGER_SCAN_REG = 0xBB;
        internal const UInt16 TRIGGER_SCAN_ONE_MODE = 0x2000;
        internal const UInt16 TRIGGER_SCAN_EIGHT_MODE = 0x6000;
        internal const UInt16 TRIGGER_SCAN_REQ_SINGLE = 0x0100;
        #endregion

        #region MTP操作常量定义
        internal const UInt16 TotalCellNum = 6;
        internal const UInt16 TotalExtNum = 4;
        public const int RETRY_COUNTER = 5;
        public const int CADC_RETRY_COUNT = 30;
        public const UInt16 WakeUpMPTINFOWriteCode = 0xC369;
        public const UInt16 WakeUpMPTINFOCheckCode = 0x3C96;

        // MTP operation code
        internal const UInt16 ALLOW_WRT      = 0x8000;
        internal const UInt16 ALLOW_WRT_MASK = 0x7FFF;
        internal const UInt16 MEM_MODE_MASK  = 0xFFFC;

        // MTP control registers' addresses
        internal const byte MEM_DATA_HI_REG = 0xC0;
        internal const byte MEM_DATA_LO_REG = 0xC1;
        internal const byte MEM_ADDR_REG    = 0xC2;
        internal const byte MEM_MODE_REG    = 0xC3;

        // MTP Control Flags
        internal const UInt16 YFLASH_ATELOCK_MATCHED_FLAG = 0x0001;
        internal const UInt16 MEM_OP_REQ_FLAG = 0x03; 
        internal const UInt16 PASSWORD = 0x7806;
        #endregion

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_ACTIVE_MODE_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_CFET_ON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_CFET_OFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_DFET_ON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_DFET_OFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        #endregion

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_MAIN_CUR = 4,
            PARAM_SLAVE_CUR,
            PARAM_DOCTH,
            PARAM_FINAL_CUR = 7,
            PARAM_CELLNUM = 10,
            PARAM_SIGN_ORG = 20,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_MEMD : ushort
        {
            MPT_MEMD_DEFAULT = 0,
            MPT_MEMD_DIRECT_W,
            MPT_MEMD_INDIRECT_WR,
            APB_MEMD_DIRECT_W,
        }

        internal enum COBRA_MEMORY_OP_REQ : ushort
        {
            MEMORY_OP_REQ_DEFAULT = 0,
            MEMORY_OP_REQ_MTP_READ,
            MEMORY_OP_REQ_MTP_WRITE,
            MEMORY_OP_REQ_APB_WRITE, 
        }

        internal enum COBRA_BIGSUR_ATELCK : ushort
        {
            YFLASH_ATELCK_MATCHED = 0,
            YFLASH_ATELCK_UNMATCHED,
            YFLASH_ATELCK_MATCHED_10
        }

        internal enum COBRA_COMMAND_MODE : ushort
        {
            TEST_CTRL_NORMAL_MODE = 0,
            TEST_CTRL_VR25_VD33_VTS,
            TEST_CTRL_OSC32K_xtal32k,
            TEST_CTRL_OSC16M,
            TEST_CTRL_THMx_20uA_VADC = 4,
            TEST_CTRL_VREF_VADC,
            TEST_CTRL_CADC,
            TEST_CTRL_VREF_CADC,
            TEST_CTRL_Main_INDSG_DOC2_SC,
            TEST_CTRL_Slave_DOC2_SC,
            TEST_CTRL_Main_DSG_CHG = 0x0A,
            TEST_CTRL_Charger_In_Slave_FET,
            TEST_CTRL_IOT,
            TEST_CTRL_Internal_Critical_Signal = 0x0F,
            AUTO_SCAN_ONE_MODE = 0x13,
            AUTO_SCAN_EIGHT_MODE = 0x14,
            TRIGGER_SCAN_EIGHT_MODE = 0x20,
            TRIGGER_SCAN_ONE_MODE = 0x21,
            SCS_TRIGGER_SCAN_EIGHT_MODE = 0x31,

            TRIM_COUNT_SLOPE = 0x40,
            TRIM_COUNT_OFFSET = 0x41,
            TRIM_COUNT_RESET = 0x42,
            TRIM_SLOPE_EIGHT_MODE = 0x43,
            TRIM_OFFSET_EIGHT_MODE = 0x44,
            WORK_MODE_POWER_DOWN = 0x96,
            INVALID_COMMAND = 0xFFFF,
        }

        internal enum CADC_MODE : byte
        {
            DISABLE = 0,
            MOVING = 1,
            TRIGGER = 2,
        }

        #region TRIGGER_SCAN_CHANNEL
        internal enum COBRA_TRIGGER_SCAN_CHANNEL : ushort //Burst不存在于SW
        {
            TRIGGER_SCAN_CHANNEL_NOTHING,//0x00: do nothing;
            TRIGGER_SCAN_CHANNEL_CELL1,
            TRIGGER_SCAN_CHANNEL_CELL2,
            TRIGGER_SCAN_CHANNEL_CELL3,
            TRIGGER_SCAN_CHANNEL_CELL4,
            TRIGGER_SCAN_CHANNEL_CELL5,
            TRIGGER_SCAN_CHANNEL_CELL6,
            TRIGGER_SCAN_CHANNEL_CELL7,
            TRIGGER_SCAN_CHANNEL_CELL8,
            TRIGGER_SCAN_CHANNEL_CELL9,
            TRIGGER_SCAN_CHANNEL_CELL10 = 0x0A, //0x01 ~ 0x0A for CELL01 ~ CELL10 respectively; 
            TRIGGER_SCAN_CHANNEL_THM0, //THM0 channel;
            TRIGGER_SCAN_CHANNEL_THM1, //THM1 channel;
            TRIGGER_SCAN_CHANNEL_THM2, //THM2 channel;
            TRIGGER_SCAN_CHANNEL_THM3, //THM3 channel;
            TRIGGER_SCAN_CHANNEL_SLAVE_PACKC, //for slave pack current measurement including doing adc_chop = 1 and adc_chop = 0;
            TRIGGER_SCAN_CHANNEL_MAIN_PACKC = 0x10,// for main pack current measurement including doing adc_chop = 1 and adc_chop = 0;
            TRIGGER_SCAN_CHANNEL_INTEL_TEMP,// for internal temperature channel;
            TRIGGER_SCAN_CHANNEL_VCC,// for VCC channel;
            TRIGGER_SCAN_CHANNEL_VPACK,//0x13: for VPACK channel;
            TRIGGER_SCAN_CHANNEL_VBATT,//0x14: for VBATT channel;
            TRIGGER_SCAN_CHANNEL_V50V,//0x15: for V50V channel;
            TRIGGER_SCAN_CHANNEL_V33V,//0x16: for V33V channel;
            TRIGGER_SCAN_CHANNEL_V18V,//0x17: for V18V channel;
            TRIGGER_SCAN_CHANNEL_PA0 = 0x18,//0x18~0x23: for PA0~PA11 channel respectively.
            TRIGGER_SCAN_CHANNEL_PA1,
            TRIGGER_SCAN_CHANNEL_PA2,
            TRIGGER_SCAN_CHANNEL_PA3,
            TRIGGER_SCAN_CHANNEL_PA4,
            TRIGGER_SCAN_CHANNEL_PA5,
            TRIGGER_SCAN_CHANNEL_PA6,
            TRIGGER_SCAN_CHANNEL_PA7,
            TRIGGER_SCAN_CHANNEL_PA8,
            TRIGGER_SCAN_CHANNEL_PA9,
            TRIGGER_SCAN_CHANNEL_PA10,
            TRIGGER_SCAN_CHANNEL_PA11 = 0x23,
            TRIGGER_SCAN_CHANNEL_SAFETY_CHECK,//0x24: safety check trigger scan from 0x10 to 0x01
        }
        #endregion

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

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellVoltage01 = 0x00035000;
        internal const UInt32 Cell1_Slope_Trim =0x00020608;
        internal const UInt32 Cell1_Offset_Trim = 0x00020E08;

        internal const UInt32 CellVoltage02 = 0x00035100;
        internal const UInt32 Cell2_Slope_Trim = 0x00020600;
        internal const UInt32 Cell2_Offset_Trim = 0x00020E00;

        internal const UInt32 CellVoltage03 = 0x00035200;
        internal const UInt32 Cell3_Slope_Trim = 0x00020708;
        internal const UInt32 Cell3_Offset_Trim = 0x00020F08;

        internal const UInt32 CellVoltage04 = 0x00035300;
        internal const UInt32 Cell4_Slope_Trim = 0x00020700;
        internal const UInt32 Cell4_Offset_Trim = 0x00020F00;

        internal const UInt32 CellVoltage05 = 0x00035400;
        internal const UInt32 Cell5_Slope_Trim = 0x00020808;
        internal const UInt32 Cell5_Offset_Trim = 0x00021008;

        internal const UInt32 CellVoltage06 = 0x00035500;
        internal const UInt32 Cell6_Slope_Trim = 0x00020800;
        internal const UInt32 Cell6_Offset_Trim = 0x00021000;

        internal const UInt32 CellVoltage07 = 0x00035600;
        internal const UInt32 Cell7_Slope_Trim = 0x00020908;
        internal const UInt32 Cell7_Offset_Trim = 0x00021108;

        internal const UInt32 CellVoltage08 = 0x00035700;
        internal const UInt32 Cell8_Slope_Trim = 0x00020900;
        internal const UInt32 Cell8_Offset_Trim = 0x00021100;

        internal const UInt32 CellVoltage09 = 0x00035800;
        internal const UInt32 Cell9_Slope_Trim = 0x00020A08;
        internal const UInt32 Cell9_Offset_Trim = 0x00021208;

        internal const UInt32 CellVoltage10 = 0x00035900;
        internal const UInt32 Cell10_Slope_Trim = 0x00020A00;
        internal const UInt32 Cell10_Offset_Trim = 0x00021200;

        internal const UInt32 CellNumber = 0x00038A0D;
        internal const UInt32 THM0Config = 0x0003030A;
        internal const UInt32 THM1Config = 0x0003050A;
        internal const UInt32 THM2Config = 0x00030B0A;
        internal const UInt32 THM3Config = 0x00030D0A;

        internal const UInt32 ExternalTemperature0 = 0x00035A00;
        internal const UInt32 ExternalTemperature1 = 0x00035B00;
        internal const UInt32 ExternalTemperature2 = 0x00035C00;
        internal const UInt32 ExternalTemperature3 = 0x00035D00;

        internal const UInt32 MainPackCur = 0x00035F00;
        internal const UInt32 MainPackCur_Slope_Trim = 0x00020B08;
        internal const UInt32 MainPackCur_Offset_Trim = 0x00021308;

        internal const UInt32 FinalPackCur = 0x00036700;
        internal const UInt32 FinalPackCur_Slope_Trim = 0x00020D00;
        internal const UInt32 FinalPackCur_Offset_Trim = 0x00021506;

        internal const UInt32 VBATT = 0x00071400;
        internal const UInt32 VBATT_Slope_Trim = 0x00020B00;
        internal const UInt32 VBATT_Offset_Trim = 0x00021300;

        internal const UInt32 PA5 = 0x00071D00;
        internal const UInt32 PA5_Slope_Trim = 0x00020C08;
        internal const UInt32 PA5_Offset_Trim = 0x00021408;
        #endregion

        #region Virtual参数GUID
        internal const UInt32 VirtualElement = 0x00070000; //Virtual参数起始地址    
        internal const UInt32 VirVBattAdcSel = 0x00071400;
        internal const UInt32 VirPA5AdcSel = 0x00071D00;
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
            //slope =  ((sumxy - sumx * sumy / n) / (sumx2 - sumx * sumx / n)); 
            FolderMap.WriteFile("--------------------Count Slope-----------------------------------\n");
            slope = ((sumx2 - sumx * sumx / n)/(sumxy - sumx * sumy / n));
             for (int i = 0; i < input.Length; i++)
             {
                 strIn.Append(string.Format("{0:N4}", input[i]));
                 strIn.Append("--");
                 strOu.Append(string.Format("{0:N4}", output[i]));
                 strOu.Append("--");
             }
             FolderMap.WriteFile(string.Format("Input:{0} \n Output:{1} \n slope:{2}", strIn.ToString(), strOu.ToString(),slope));
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

            for (int i = 0; i < input.Length; i++)
                ddata += (output[i] - input[i]);
            offset = ddata/input.Length;

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
