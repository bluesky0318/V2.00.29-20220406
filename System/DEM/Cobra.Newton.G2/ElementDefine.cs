using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.NewTon.G2
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const int RETRY_COUNTER = 5;
        internal const UInt16 BLOCK_OPERATION_BYTES = 32;

        internal const UInt16 MTP_MEMORY_SIZE = 0x80;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 I2C_MEMORY_SIZE = 0x100;
        internal const UInt16 eFlashCtrl_Size = 0xFF;
        internal const UInt16 I2CRegisters_Size = 0xFF;
        internal const UInt16 TimerRegisters_Size = 0xFF;
        internal const UInt16 WDTRegisters_Size = 0xFF;
        internal const UInt16 UARTRegisters_Size = 0xFF;

        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt32 I2CMask = 0x0000FF00;
        internal const UInt16 TRIM_TIMES = 5;
        internal static int m_trim_count = 0;

        internal const UInt16 DFEController_StartAddress = 0xC600;
        internal const UInt32 DFEController_Size = 1024; //1K

        internal const UInt16 SystemArea_StartAddress = 0x6000;
        internal const UInt32 SystemArea_Size = 128; //1K

        internal const UInt16 EEPROMController_StartAddress = 0x7000;

        internal const byte RegI2C_Unlock_CfgWrt = 0x80;
        internal const byte RegI2C_Unlock_PwrmdWrt = 0xAA;
        internal const byte RegI2C_Unlock_I2cCfgWrt = 0x11;
        internal const byte RegI2C_AHB_MODE = 0x12;
        internal const byte RegI2C_MEMD = 0x20;

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_UNLOCK_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_MAINBLOCK_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_PAGE_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_INFO_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_SYS_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_CRC16_DONE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_CRC16_COMPARE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        internal const UInt32 IDS_ERR_DEM_BLK_ACCESS = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        internal const UInt32 IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0009; 
        internal const UInt32 IDS_ERR_DEM_RD_DATA_SIZE_INCORRECT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x000A;
        internal const UInt32 IDS_ERR_DEM_WR_DATA_SIZE_INCORRECT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x000B;
        #endregion

        #region PASSWORD
        internal const UInt16 Unlock_Erase_PSW = 0xABCD;
        internal const UInt16 UnLock_I2C_PSW = 0x6318;
        internal const UInt16 ReLock_I2C_PSW = 0x0618;
        internal const UInt16 I2C_AHB_MODE_Enable_PSW = 0x6301;
        internal const UInt16 I2C_AHB_MODE_Default_PSW = 0x6300;
        internal const UInt16 I2C_MEMD_PWD = 0x9A50;
        #endregion

        #region SCAN操作常量定义
        internal const UInt16 AUTO_SCAN_REG = 0x58;
        internal const UInt16 AUTO_SCAN_ONE_MODE = 0x7F01;
        internal const UInt16 AUTO_SCAN_FOUR_MODE = 0x7F11;

        internal const byte TRIGGER_SCAN_REG = 0x5C;
        internal const byte CADCCTRL_REG = 0x68;
        internal const byte GPIOCFG = 0x24;
        internal const UInt16 TRIGGER_SCAN_ONE_MODE = 0x0100;
        internal const UInt16 TRIGGER_SCAN_FOUR_MODE = 0x2100;
        #endregion

        public enum COBRA_COMMAND_MODE : ushort
        {
            MAIN_BLOCK_PROGRAM = 0x10,
            AUTO_SCAN_ONE_MODE = 0x13,
            AUTO_SCAN_FOUR_MODE = 0x14,
            TRIGGER_SCAN_FOUR_MODE = 0x20,
            TRIGGER_SCAN_ONE_MODE = 0x21,
            DEBUG_READ = 0x25,
            DEBUG_WRITE = 0x26,
            SCS_TRIGGER_SCAN_EIGHT_MODE = 0x31,
            TRIGGER_SCAN_UI = 0x32,

            TRIM_COUNT_SLOPE = 0x40,
            TRIM_COUNT_OFFSET = 0x41,
            TRIM_COUNT_RESET = 0x42,
            TRIM_SLOPE_FOUR_MODE = 0x43,
            TRIM_OFFSET_FOUR_MODE = 0x44,
            INVALID_COMMAND = 0xFFFF,
        }

        public enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_CUR = 4,
            PARAM_ISENS = 5,
            PARAM_CELL_VOL = 6,
            PARAM_CELL2_VOL = 7,
            PARAM_MTP_SPECIAL_DWORD = 10,
            PARAM_MTP_SPECIAL_HI_WORD = 11,
            PARAM_SIGN_ORG = 20,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }
        public enum CHIP_OPERA_MODE : ushort
        {
            SOFT_RESET = 0x01,
            CPU_HOLD = 0x02,
            CPU_RUN = 0x03
        }

        #region TRIGGER_SCAN_CHANNEL
        internal enum COBRA_TRIGGER_SCAN_CHANNEL : ushort //Burst不存在于SW
        {
            TRIGGER_SCAN_CHANNEL_NOTHING,//0x00: do nothing;
            TRIGGER_SCAN_CHANNEL_INTEL_TEMP,// for internal temperature channel;
            TRIGGER_SCAN_CHANNEL_CELL1 = 2,
            TRIGGER_SCAN_CHANNEL_EXTTEMP, //THM0 channel;
            TRIGGER_SCAN_CHANNEL_ISENS,
            TRIGGER_SCAN_CHANNEL_VD15,
            TRIGGER_SCAN_CHANNEL_VCC,
            TRIGGER_SCAN_CHANNEL_TS2_GPIO,
            TRIGGER_SCAN_CHANNEL_CELL1_CELL2,
            TRIGGER_SCAN_CHANNEL_VADC,
            TRIGGER_SCAN_CHANNEL_V560,
            TRIGGER_SCAN_CHANNEL_ISENS_CELL1,
            TRIGGER_SCAN_CHANNEL_ISENS_CELL1_CELL2,
            TRIGGER_SCAN_CHANNEL_INT_ID,
        }
        #endregion

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
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
        internal const UInt32 TpRsense = TemperatureElement + 0x21;
        internal const UInt32 TpSlaveRsense = TemperatureElement + 0x22;
        #endregion

        #region MTP参数GUID
        internal const UInt32 EEPROMTRIMElement = 0x00020000; //MTP参数起始地址     
        internal const UInt16 EpTrim_Offset_ADDR = 0x00;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellVoltage01 = 0x00034500;
        internal const UInt32 Cell1_Slope_Trim = 0x00020600;
        internal const UInt32 Cell1_Offset_Trim = 0x00020900;
        internal const UInt32 Cell1_2nd_Offset_Trim = 0x00020910;
        internal const UInt32 I2C_Cell1_Slope_Trim = 0x0103E600;
        internal const UInt32 I2C_Cell1_Offset_Trim = 0x0103E900;

        internal const UInt32 CellVoltage02 = 0x00034B00;
        internal const UInt32 Cell2_Slope_Trim = 0x00020608;
        internal const UInt32 Cell2_Offset_Trim = 0x00020908;
        internal const UInt32 Cell2_2nd_Offset_Trim = 0x00020917;
        internal const UInt32 I2C_Cell2_Slope_Trim = 0x0103E608;
        internal const UInt32 I2C_Cell2_Offset_Trim = 0x0103E908;

        internal const UInt32 CADC = 0x00036A00;
        internal const UInt32 CADC_Slope_Trim = 0x00020D08;
        internal const UInt32 CADC_Offset_Trim = 0x00020D00;
        internal const UInt32 I2C_CADC_Slope_Trim = 0x0103ED08;
        internal const UInt32 I2C_CADC_Offset_Trim = 0x0103ED00;

        internal const UInt32 ExtTemp = 0x00034600;
        internal const UInt32 ExtTemp_Slope_Trim = 0x00020700;
        internal const UInt32 ExtTemp_Offset_Trim = 0x00020A00;
        internal const UInt32 I2C_ExtTemp_Slope_Trim = 0x0103E700;
        internal const UInt32 I2C_ExtTemp_Offset_Trim = 0x0103EA00;

        internal const UInt32 GPIO = 0x00034A00;
        internal const UInt32 GPIO_Slope_Trim = 0x00020708;
        internal const UInt32 GPIO_Offset_Trim = 0x00020A08;
        internal const UInt32 I2C_GPIO_Slope_Trim = 0x0103E708;
        internal const UInt32 I2C_GPIO_Offset_Trim = 0x0103EA08;

        internal const UInt32 VCC = 0x00034900;
        internal const UInt32 VCC_Slope_Trim = 0x00020808;
        internal const UInt32 VCC_Offset_Trim = 0x00020B08;
        internal const UInt32 I2C_VCC_Slope_Trim = 0x0103E808;
        internal const UInt32 I2C_VCC_Offset_Trim = 0x0103EB08;

        internal const UInt32 ISENS = 0x00034700;
        internal const UInt32 VirtualISENS = 0x00074700;
        internal const UInt32 ISENS_Slope_Trim = 0x00020800;
        internal const UInt32 ISENS_Offset_Trim = 0x00020B00;
        internal const UInt32 I2C_ISENS_Slope_Trim = 0x0103E800;
        internal const UInt32 I2C_ISENS_Offset_Trim = 0x0103EB00;

        internal const UInt32 INT_ID = 0x00034C00;
        internal const UInt32 INT_ID_Slope_Trim = 0x00020E08;
        internal const UInt32 INT_ID_Offset_Trim = 0x00021208;
        internal const UInt32 I2C_INT_ID_Slope_Trim = 0x0103EE08;
        internal const UInt32 I2C_INT_ID_Offset_Trim = 0x0103F208;

        internal const UInt32 TS2_GPIO = 0x00074A00;
        internal const UInt32 TS2_GPIO_Slope_Trim = 0x00020E00;
        internal const UInt32 TS2_GPIO_Offset_Trim = 0x00021200;
        internal const UInt32 I2C_TS2_GPIO_Slope_Trim = 0x0103EE00;
        internal const UInt32 I2C_TS2_GPIO_Offset_Trim = 0x0103F200;
        #endregion

        #region I2C参数GUID
        internal const UInt32 I2CElement = 0x01030000;
        #endregion

        #region Expert参数GUID
        internal const UInt16 eFlashCtrl_StartAddress = 0x4000;
        internal const UInt32 eFlashCtrlElement = 0x02030000;
        internal const UInt16 I2CRegisters_StartAddress = 0xC100;
        internal const UInt32 I2CRegistersElement = 0x03030000;
        internal const UInt16 TimerRegisters_StartAddress = 0xC200;
        internal const UInt32 TimerRegistersElement = 0x04030000;
        internal const UInt16 WDTRegisters_StartAddress = 0xC400;
        internal const UInt32 WDTRegistersElement = 0x05030000;
        internal const UInt16 UARTRegisters_StartAddress = 0xC500;
        internal const UInt32 UARTRegistersElement = 0x06030000;
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

        #region Virtual参数GUID
        internal const UInt32 VirtualElement = 0x00070000; //Virtual参数起始地址    
        internal const UInt32 TS2GPIOAdcSel = 0x00074A00;
        #endregion
    }

    internal class DataPoint
    {
        private double[] input = new double[ElementDefine.TRIM_TIMES];
        private double[] output = new double[ElementDefine.TRIM_TIMES];
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

            for (int i = 0; i < input.Length; i++)
                ddata += (output[i] - input[i]);
            offset = ddata / input.Length;

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

        public UInt32 GetSlopeAndOffset(ref double slope, ref double offset)
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

            slope = (input[0] - input[1]) / (output[0] - output[1]);
            offset = slope * output[0] - input[0];
            FolderMap.WriteFile("--------------------Count Slope-----------------------------------\n");
            for (int i = 0; i < input.Length; i++)
            {
                strIn.Append(string.Format("{0:N4}", input[i]));
                strIn.Append("--");
                strOu.Append(string.Format("{0:N4}", output[i]));
                strOu.Append("--");
            }
            FolderMap.WriteFile(string.Format("Input:{0} \n Output:{1} \n slope:{2} offset:{3}", strIn.ToString(), strOu.ToString(), slope, offset));
            return ret;
        }
    }
}
