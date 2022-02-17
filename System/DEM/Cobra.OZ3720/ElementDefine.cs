using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Azalea20
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 EFUSE_MEMORY_SIZE = 0x20;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 TestCtrl_FLAG = 0x000F;
        internal const UInt16 EFUSE_MODE_CLEAR_FLAG = 0xFFF0;
        internal const UInt16 ALLOW_WR_CLEAR_FLAG = 0x7FFF;
        internal const UInt16 nTrim_Times = 1;

        internal const byte CADCRTL_REG = 0x38;
        internal const byte FinalCadcMovingData_Reg = 0x17;
        internal const byte FinalCadcTriggerData_Reg = 0x39;

        internal static int m_trim_count = 0;
        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP = 2,
            PARAM_EXT_TEMP = 3,
            PARAM_CURRENT = 4,
            PARAM_DOCTH,
            PARAM_UV_SHUTDOWN_TH = 10,
            PARAM_DEFAULT_OFFSET = 11,
            PARAM_SIGN_ORG = 20,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_AZALEA20_WKM : ushort
        {
            EFUSE_WORKMODE_NORMAL = 0,
            EFUSE_WORKMODE_WRITE_MAP_CTRL = 0x01,
            EFUSE_WORKMODE_PROGRAM = 0x02,
        }

        internal enum COBRA_COMMAND_MODE : ushort
        {
            TRIGGER_SCAN_CTO_MODE = 0x13,
            TRIGGER_SCAN_ALL_MODE = 0x14,
            TRIGGER_SCAN_EIGHT_MODE = 0x20,
            TRIGGER_SCAN_ONE_MODE = 0x21,
            TRIM_TRIGGER_SCAN_EIGHT_MODE = 0x30,
            SCS_TRIGGER_SCAN_EIGHT_MODE = 0x31,
            TRIGGER_SCAN_UI = 0x32,
            TRIM_COUNT_SLOPE = 0x40,
            TRIM_COUNT_OFFSET = 0x41,
            TRIM_COUNT_RESET = 0x42,
            TRIM_SLOPE_EIGHT_MODE = 0x43,
            TRIM_OFFSET_EIGHT_MODE = 0x44,
            EXCEPTION_MODE_WATCH_DOG = 0x96,
            INVALID_COMMAND = 0xFFFF,
        }

        public enum CADC_MODE : byte
        {
            DISABLE = 0,
            MOVING = 1,
            TRIGGER = 2,
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_WAIT_TRIGGER_FLAG_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_ACTIVE_MODE_ERROR = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_CFET_ON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_CFET_OFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_DFET_ON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_DFET_OFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        #endregion

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion

        #region EFUSE参数GUID
        internal const UInt32 EFUSEElement = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 EFUSE_Osc512k_Ftrim = 0x00020000;
        internal const UInt32 EFUSE_DOC_Trim = 0x00020005;
        internal const UInt32 EFUSE_VR25v_VTrim = 0x00020100;
        internal const UInt32 EFUSE_VR25v_TTrim = 0x00020200;
        internal const UInt32 EFUSE_Thm3k_Trim = 0x00020300;
        internal const UInt32 EFUSE_Thm_Offset = 0x00020306;
        internal const UInt32 EFUSE_Thm60k_Trim = 0x00020400;
        internal const UInt32 EFUSE_Int_Tmp_Trim = 0x00020504;
        internal const UInt32 EFUSE_Cell01_Slope_Trim = 0x00020500;
        internal const UInt32 EFUSE_Cell02_Slope_Trim = 0x00020604;
        internal const UInt32 EFUSE_Cell03_Slope_Trim = 0x00020600;
        internal const UInt32 EFUSE_Cell04_Slope_Trim = 0x00020704;
        internal const UInt32 EFUSE_Cell05_Slope_Trim = 0x00020700;
        internal const UInt32 EFUSE_Packc_Slope_Trim = 0x00020800;
        internal const UInt32 EFUSE_Packv_Slope_Trim = 0x00020804;
        internal const UInt32 EFUSE_Packc_Offset = 0x00020900;
        internal const UInt32 EFUSE_Packv_Offset = 0x00020a06;
        internal const UInt32 EFUSE_Cell01_Offset = 0x00020a00;
        internal const UInt32 EFUSE_Cell02_Offset = 0x00020b00;
        internal const UInt32 EFUSE_Cell03_Offset = 0x00020c00;
        internal const UInt32 EFUSE_Cell04_Offset = 0x00020d00;
        internal const UInt32 EFUSE_Cell05_Offset = 0x00020e00;
        internal const UInt32 EFUSE_ATE_CRC_Sum = 0x00020f00;
        internal const UInt32 EFUSE_ATE_frozen = 0x00020f07;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt16 TotalCellNum = 20;
        internal const UInt32 OP_STR = 0x00030210;
        internal const UInt32 OP_CellNumber = 0x0003BE04;
        internal const UInt32 OP_CELL1V_CTO = 0x00032100;
        internal const UInt32 OP_CELL2V_CTO = 0x00032200;
        internal const UInt32 OP_CELL3V_CTO = 0x00032300;
        internal const UInt32 OP_CELL4V_CTO = 0x00032400;
        internal const UInt32 OP_CELL5V_CTO = 0x00032500;
        internal const UInt32 OP_CELL6V_CTO = 0x00032600;
        internal const UInt32 OP_CELL7V_CTO = 0x00032700;
        internal const UInt32 OP_CELL8V_CTO = 0x00032800;
        internal const UInt32 OP_CELL9V_CTO = 0x00032900;
        internal const UInt32 OP_CELL10V_CTO = 0x00032A00;
        internal const UInt32 OP_CELL11V_CTO = 0x00032B00;
        internal const UInt32 OP_CELL12V_CTO = 0x00032C00;
        internal const UInt32 OP_CELL13V_CTO = 0x00032D00;
        internal const UInt32 OP_CELL14V_CTO = 0x00032E00;
        internal const UInt32 OP_CELL15V_CTO = 0x00032F00;
        internal const UInt32 OP_CELL16V_CTO = 0x00033000;
        internal const UInt32 OP_CELL17V_CTO = 0x00033100;
        internal const UInt32 OP_CELL18V_CTO = 0x00033200;
        internal const UInt32 OP_CELL19V_CTO = 0x00033300;
        internal const UInt32 OP_CELL20V_CTO = 0x00033400;

        internal const UInt32 OP_INTMP = 0x00034000;
        internal const UInt32 OP_CELL1V = 0x00034100;
        internal const UInt32 OP_CELL2V = 0x00034200;
        internal const UInt32 OP_CELL3V = 0x00034300;
        internal const UInt32 OP_CELL4V = 0x00034400;
        internal const UInt32 OP_CELL5V = 0x00034500;
        internal const UInt32 OP_CELL6V = 0x00034600;
        internal const UInt32 OP_CELL7V = 0x00034700;
        internal const UInt32 OP_CELL8V = 0x00034800;
        internal const UInt32 OP_CELL9V = 0x00034900;
        internal const UInt32 OP_CELL10V = 0x00034A00;
        internal const UInt32 OP_CELL11V = 0x00034B00;
        internal const UInt32 OP_CELL12V = 0x00034C00;
        internal const UInt32 OP_CELL13V = 0x00034D00;
        internal const UInt32 OP_CELL14V = 0x00034E00;
        internal const UInt32 OP_CELL15V = 0x00034F00;
        internal const UInt32 OP_CELL16V = 0x00035000;
        internal const UInt32 OP_CELL17V = 0x00035100;
        internal const UInt32 OP_CELL18V = 0x00035200;
        internal const UInt32 OP_CELL19V = 0x00035300;
        internal const UInt32 OP_CELL20V = 0x00035400;
        internal const UInt32 OP_ISENS = 0x00035500;
        internal const UInt32 OP_THM0 = 0x00035600;
        internal const UInt32 OP_THM1 = 0x00035700;
        internal const UInt32 OP_THM2 = 0x00035800;
        internal const UInt32 OP_GP0V = 0x00035900;
        internal const UInt32 OP_GP1V = 0x00035A00;
        internal const UInt32 OP_GP2V = 0x00035B00;
        internal const UInt32 OP_VBATT = 0x00035C00;
        internal const UInt32 OP_VCC = 0x00035D00;
        internal const UInt32 OP_V50V = 0x00035E00;
        internal const UInt32 OP_MCU = 0x00035F00;

        internal const UInt32 OP_INTMP_8 = 0x00036000;
        internal const UInt32 OP_CELL1V_8 = 0x00036100;
        internal const UInt32 OP_CELL1V_SLOP   = 0x0003A505;
        internal const UInt32 OP_CELL1V_OFFSET = 0x0003A50A;
        internal const UInt32 OP_CELL2V_8 = 0x00036200;
        internal const UInt32 OP_CELL2V_SLOP   = 0x0003A500;
        internal const UInt32 OP_CELL2V_OFFSET = 0x0003A60A;
        internal const UInt32 OP_CELL3V_8 = 0x00036300;
        internal const UInt32 OP_CELL3V_SLOP = 0x0003A605;
        internal const UInt32 OP_CELL3V_OFFSET = 0x0003A70A;
        internal const UInt32 OP_CELL4V_8 = 0x00036400;
        internal const UInt32 OP_CELL4V_SLOP = 0x0003A600;
        internal const UInt32 OP_CELL4V_OFFSET = 0x0003A80A;
        internal const UInt32 OP_CELL5V_8 = 0x00036500;
        internal const UInt32 OP_CELL5V_SLOP = 0x0003A705;
        internal const UInt32 OP_CELL5V_OFFSET = 0x0003A90A;
        internal const UInt32 OP_CELL6V_8 = 0x00036600;
        internal const UInt32 OP_CELL6V_SLOP = 0x0003A700;
        internal const UInt32 OP_CELL6V_OFFSET = 0x0003AA0A;
        internal const UInt32 OP_CELL7V_8 = 0x00036700;
        internal const UInt32 OP_CELL7V_SLOP = 0x0003A805;
        internal const UInt32 OP_CELL7V_OFFSET = 0x0003AB0A;
        internal const UInt32 OP_CELL8V_8 = 0x00036800;
        internal const UInt32 OP_CELL8V_SLOP = 0x0003A800;
        internal const UInt32 OP_CELL8V_OFFSET = 0x0003AC0A;
        internal const UInt32 OP_CELL9V_8 = 0x00036900;
        internal const UInt32 OP_CELL9V_SLOP = 0x0003A905;
        internal const UInt32 OP_CELL9V_OFFSET = 0x0003AD0A;
        internal const UInt32 OP_CELL10V_8 = 0x00036A00;
        internal const UInt32 OP_CELL10V_SLOP = 0x0003A900;
        internal const UInt32 OP_CELL10V_OFFSET = 0x0003AE0A;
        internal const UInt32 OP_CELL11V_8 = 0x00036B00;
        internal const UInt32 OP_CELL11V_SLOP = 0x0003AA05;
        internal const UInt32 OP_CELL11V_OFFSET = 0x0003AF0A;
        internal const UInt32 OP_CELL12V_8 = 0x00036C00;
        internal const UInt32 OP_CELL12V_SLOP = 0x0003AA00;
        internal const UInt32 OP_CELL12V_OFFSET = 0x0003B00A;
        internal const UInt32 OP_CELL13V_8 = 0x00036D00;
        internal const UInt32 OP_CELL13V_SLOP = 0x0003AB05;
        internal const UInt32 OP_CELL13V_OFFSET = 0x0003B10A;
        internal const UInt32 OP_CELL14V_8 = 0x00036E00;
        internal const UInt32 OP_CELL14V_SLOP = 0x0003AB00;
        internal const UInt32 OP_CELL14V_OFFSET = 0x0003B208;
        internal const UInt32 OP_CELL15V_8 = 0x00036F00;
        internal const UInt32 OP_CELL15V_SLOP = 0x0003AC05;
        internal const UInt32 OP_CELL15V_OFFSET = 0x0003B200;
        internal const UInt32 OP_CELL16V_8 = 0x00037000;
        internal const UInt32 OP_CELL16V_SLOP = 0x0003AC00;
        internal const UInt32 OP_CELL16V_OFFSET = 0x0003B300;
        internal const UInt32 OP_CELL17V_8 = 0x00037100;
        internal const UInt32 OP_CELL17V_SLOP = 0x0003AD05;
        internal const UInt32 OP_CELL17V_OFFSET = 0x0003B300;
        internal const UInt32 OP_CELL18V_8 = 0x00037200;
        internal const UInt32 OP_CELL18V_SLOP = 0x0003AD00;
        internal const UInt32 OP_CELL18V_OFFSET = 0x0003B408;
        internal const UInt32 OP_CELL19V_8 = 0x00037300;
        internal const UInt32 OP_CELL19V_SLOP = 0x0003AE05;
        internal const UInt32 OP_CELL19V_OFFSET = 0x0003B400;
        internal const UInt32 OP_CELL20V_8 = 0x00037400;
        internal const UInt32 OP_CELL20V_SLOP = 0x0003AE00;
        internal const UInt32 OP_CELL20V_OFFSET = 0x0003B508;
        internal const UInt32 OP_ISENS_8 = 0x00037500;
        internal const UInt32 OP_ISENS_SLOP = 0x0003BA08;
        internal const UInt32 OP_ISENS_OFFSET = 0x0003A305;
        internal const UInt32 OP_THM0V_8 = 0x00037600;
        internal const UInt32 OP_THM0V_SLOP = 0x0003AF05;
        internal const UInt32 OP_THM0V_OFFSET = 0x0003B500;
        internal const UInt32 OP_THM1V_8 = 0x00037700;
        internal const UInt32 OP_THM1V_SLOP = 0x0003AF00;
        internal const UInt32 OP_THM1V_OFFSET = 0x0003B608;
        internal const UInt32 OP_THM2V_8 = 0x00037800;
        internal const UInt32 OP_THM2V_SLOP = 0x0003B005;
        internal const UInt32 OP_THM2V_OFFSET = 0x0003B600;
        internal const UInt32 OP_THM3V_8 = 0x0003E100;
        internal const UInt32 OP_THM3V_SLOP = 0x0003B000;
        internal const UInt32 OP_THM3V_OFFSET = 0x0003B708;
        internal const UInt32 OP_GP0V_8 = 0x00037900;
        internal const UInt32 OP_GP1V_8 = 0x00037A00;
        internal const UInt32 OP_GP2V_8 = 0x00037B00;
        internal const UInt32 OP_VBATT_8 = 0x00037C00;
        internal const UInt32 OP_VBATT_SLOP = 0x0003B105;
        internal const UInt32 OP_VBATT_OFFSET = 0x0003B908;
        internal const UInt32 OP_CADC = 0x00033F00;
        internal const UInt32 OP_CADC_SLOP = 0x0003A400;
        internal const UInt32 OP_CADC_OFFSET = 0x0003A408;
        internal const UInt32 OP_VCC_8 = 0x00037D00;
        internal const UInt32 OP_V50V_8 = 0x00037E00;
        internal const UInt32 OP_MCU_8 = 0x00037F00;
        internal const UInt32 OP_Doc1p_TH = 0x00030800;
        internal const UInt32 OP_Cocp_TH = 0x00030900;
        internal const UInt32 OP_Cell_cto_on_time = 0x00030C04;
        internal const UInt32 OP_Cell_cto_off_time = 0x00030C08;
        internal const UInt32 inter_settling_time = 0x00030D00;
        internal const UInt32 dda_settling_time = 0x00030D04;
        internal const UInt32 thm_settling_time = 0x00030D08;
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
            slope =  ((sumxy - sumx * sumy / n) / (sumx2 - sumx * sumx / n)); 
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
