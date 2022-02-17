using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Sequoia
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

        internal const UInt16 MTP_MEMORY_SIZE = 0x20;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 I2C_MEMORY_SIZE = 0xFF;

        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt32 I2CMask = 0x0000FF00;
        internal static int m_trim_count = 0;

        internal const UInt16 DFEController_StartAddress = 0xC900;
        internal const UInt32 DFEController_Size = 1024; //1K

        internal const UInt16 eFlashController_StartAddress = 0x7000;

        internal const UInt16 SystemArea_StartAddress = 0x6000;
        internal const UInt32 SystemArea_Size = 128; //1K

        internal const byte RegI2C_Unlock_CfgWrt = 0x94;
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
        #endregion

        #region PASSWORD
        internal const UInt16 Unlock_Erase_PSW = 0xABCD;
        internal const UInt16 UnLock_I2C_PSW = 0x7918;
        internal const UInt16 ReLock_I2C_PSW = 0x0918;
        internal const UInt16 I2C_AHB_MODE_Enable_PSW = 0x7901;
        internal const UInt16 I2C_AHB_MODE_Default_PSW = 0x7900;
        internal const UInt16 I2C_MEMD_PWD = 0x9A50;
        #endregion

        #region SCAN操作常量定义
        internal const byte TRIGGER_SCAN_REG = 0x6E;
        internal const byte CADCCTRL_REG = 0x80;
        internal const UInt16 TRIGGER_SCAN_ONE_MODE = 0x0100;
        internal const UInt16 TRIGGER_SCAN_FOUR_MODE = 0x2100;
        #endregion

        public enum COBRA_FLASH_OP : ushort
        {
            HI_WORD,
            LO_WORD
        }

        public enum COBRA_COMMAND_MODE : ushort
        {
            AUTO_SCAN_ONE_MODE = 0x13,
            AUTO_SCAN_FOUR_MODE = 0x14,
            TRIGGER_SCAN_FOUR_MODE = 0x20,
            TRIGGER_SCAN_ONE_MODE = 0x21,
            SCS_TRIGGER_SCAN_EIGHT_MODE = 0x31,

            TRIM_COUNT_SLOPE = 0x40,
            TRIM_COUNT_OFFSET = 0x41,
            TRIM_COUNT_RESET = 0x42,
            TRIM_SLOPE_FOUR_MODE = 0x43,
            TRIM_OFFSET_FOUR_MODE = 0x44, 
            SUB_TASK_NORMAL_MODE = 0x80,
            SUB_TASK_SHUTDOWN_MODE = 0x90,
            INVALID_COMMAND = 0xFFFF,
        }

        public enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP,
            PARAM_EXT_TEMP,
            PARAM_CUR = 4,
            PARAM_MTP_SPECIAL_DWORD = 10,
            PARAM_MTP_SPECIAL_HI_WORD = 11,
            PARAM_MTP_SPECIAL_LO_BYTE = 12,
            PARAM_SIGN_ORG = 20,
            PARAM_ORIGNAL = 21,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        #region TRIGGER_SCAN_CHANNEL
        internal enum COBRA_TRIGGER_SCAN_CHANNEL : ushort //Burst不存在于SW
        {
            TRIGGER_SCAN_CHANNEL_NOTHING,//0x00: do nothing;
            TRIGGER_SCAN_CHANNEL_PA0_ADC,// for internal temperature channel;
            TRIGGER_SCAN_CHANNEL_PA1_ADC,
            TRIGGER_SCAN_CHANNEL_PA2_ADC, 
            TRIGGER_SCAN_CHANNEL_PA3_ADC,
            TRIGGER_SCAN_CHANNEL_PA4_ADC,
            TRIGGER_SCAN_CHANNEL_PA5_ADC,
            TRIGGER_SCAN_CHANNEL_PA6_ADC,
            TRIGGER_SCAN_CHANNEL_PA7_ADC,
            TRIGGER_SCAN_CHANNEL_PA8_ADC,
            TRIGGER_SCAN_CHANNEL_PA9_ADC,
            TRIGGER_SCAN_CHANNEL_PA10_ADC,
            TRIGGER_SCAN_CHANNEL_PA11_ADC,
            TRIGGER_SCAN_CHANNEL_PA12_ADC,
            TRIGGER_SCAN_CHANNEL_PA13_ADC,
            TRIGGER_SCAN_CHANNEL_ISENS_ADC =  0x0F,
            TRIGGER_SCAN_CHANNEL_INTMP_ADC,  
            TRIGGER_SCAN_CHANNEL_THM0_ADC_PA0,
            TRIGGER_SCAN_CHANNEL_THM1_ADC_PA1,
            TRIGGER_SCAN_CHANNEL_VD15_ADC,
            TRIGGER_SCAN_CHANNEL_VCC_ADC,
            TRIGGER_SCAN_CHANNEL_THM2_ADC_PA4,
            TRIGGER_SCAN_CHANNEL_THM3_ADC_PA5,
            TRIGGER_SCAN_CHANNEL_V600MV_ADC, 
            TRIGGER_SCAN_CHANNEL_VADC_OFFSET, 
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
        internal const UInt32 MTPElement = 0x00020000; //MTP参数起始地址       
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 PA0 = 0x00035400;
        internal const UInt32 I2C_PA0_Slope_Trim = 0x0103E708;
        internal const UInt32 I2C_PA0_Offset_Trim = 0x0103E700;

        internal const UInt32 THM1 = 0x00035500;
        internal const UInt32 I2C_ExtTemp_Slope_Trim = 0x0103E808;
        internal const UInt32 I2C_ExtTemp_Offset_Trim = 0x0103E800;

        internal const UInt32 ISENS = 0x00036800;
        internal const UInt32 I2C_ISENS_Slope_Trim = 0x0103E908;
        internal const UInt32 I2C_ISENS_Offset_Trim = 0x0103E900;

        internal const UInt32 CADC = 0x00038200;
        internal const UInt32 I2C_CADC_Slope_Trim = 0x0103EB08;
        internal const UInt32 I2C_CADC_Offset_Trim = 0x0103EB00;
        #endregion

        #region I2C参数GUID
        internal const UInt32 I2CElement = 0x01030000;
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
    }
}
