using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL16D
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 EPROM_MEMORY_SIZE = 0xFF;
        internal const UInt16 EF_MEMORY_SIZE = 0x10;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 TestCtrl_FLAG = 0x000F;
        internal const UInt16 ALLOW_WR_CLEAR_FLAG = 0x7FFF;
        internal static int m_trim_count = 0;
        internal const UInt16 TotalCellNum = 16;
        internal const UInt16 DEFAULT_OSR = 0x0200;

        internal const int RETRY_COUNT = 5;
        internal const byte CADCRTL_REG = 0x38;
        internal const byte INTR2_REG = 0x65;
        internal const byte FinalCadcMovingData_Reg = 0x39;
        internal const byte FinalCadcTriggerData_Reg = 0x3F;

        internal const UInt16 EP_ATE_OFFSET = 0x80;
        internal const UInt16 EP_ATE_TOP = 0x90;
        internal const UInt16 ATE_CRC_OFFSET = 0x90;
        internal const UInt16 EF_USR_OFFSET = 0x91;
        internal const UInt16 EF_USR_TOP = 0x9E;
        internal const UInt16 USR_CRC_OFFSET = 0x9E;
        internal const UInt16 VDD_OFFSET = 0x26;
        internal const UInt16 ATE_CRC_BUF_LEN = 27;     // 4 * 7 - 1
        internal const UInt16 USR_CRC_BUF_LEN = 35;     // 4 * 9 - 1

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_READCADC_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_TIGGER_SCAN_TIMEOUT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_OPERATE_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_ERROR_MODE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_POWERON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_POWEROFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_POWERCHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        internal const UInt32 IDS_ERR_DEM_ATE_EMPTY_CHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0008;
        #endregion

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_VOLTAGE = 1,
            PARAM_INT_TEMP = 2,
            PARAM_EXT_TEMP = 3,
            PARAM_CURRENT = 4,
            PARAM_CADC = 5,

            PARAM_EXT_TH = 7,
            PARAM_EXT_HYS = 8,
            PARAM_DFET_INCHG_CTRL = 9,

            PARAM_SIGN_ORG = 20,
            PARAM_EXT_TEMP_TABLE = 40,
            PARAM_INT_TEMP_REFER = 41
        }

        internal enum COBRA_KALL14D_WKM : ushort
        {
            EFUSE_WORKMODE_NORMAL = 0,
            EFUSE_WORKMODE_WRITE_MAP_CTRL = 0x01,
            EFUSE_WORKMODE_PROGRAM = 0x02,
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

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpRsense = TemperatureElement + 0x00;
        #endregion

        #region EPROM参数GUID
        internal const UInt32 EPROMElement = 0x00020000; //EFUSE参数起始地址
        internal const UInt32 EPROM_ovp_th = 0x00029100;
        internal const UInt32 EPROM_ovp_rls_hys = 0x00029108;
        internal const UInt32 EPROM_ovp_dly = 0x0002910D;
        internal const UInt32 EPROM_uvp_th = 0x00029200;
        internal const UInt32 EPROM_uvp_rls_hys = 0x00029207;
        internal const UInt32 EPROM_uvp_dly = 0x0002920C;
        internal const UInt32 EPROM_cocp_th = 0x00029300;
        internal const UInt32 EPROM_doc1p_th = 0x00029308;
        internal const UInt32 EPROM_doc2p_th = 0x00029400;
        internal const UInt32 EPROM_scp_th = 0x00029404;
        internal const UInt32 EPROM_doc2p_dly = 0x00029408;
        internal const UInt32 EPROM_dsg_th = 0x00029500;
        internal const UInt32 EPROM_chg_th = 0x00029504;
        internal const UInt32 EPROM_doc1p_dly = 0x00029508;
        internal const UInt32 EPROM_cocp_dly = 0x0002950D;
        internal const UInt32 EPROM_dot_th = 0x00029600;
        internal const UInt32 EPROM_dotr_hys = 0x00029608;
        internal const UInt32 EPROM_cot_th = 0x00029700;
        internal const UInt32 EPROM_cotr_hys = 0x00029708;
        internal const UInt32 EPROM_dut_th = 0x00029800;
        internal const UInt32 EPROM_dutr_hys = 0x00029809;
        internal const UInt32 EPROM_cut_th = 0x00029900;
        internal const UInt32 EPROM_cutr_hys = 0x00029909;
        internal const UInt32 EPROM_cb_start_th = 0x00029A00;
        internal const UInt32 EPROM_ub_cell_th = 0x00029B00;
        internal const UInt32 EPROM_cell_open_th = 0x00029B06;
        internal const UInt32 EPROM_multi_function_th = 0x00029B08;
        internal const UInt32 EPROM_eoc_th = 0x00029C00;
        internal const UInt32 EPROM_CADC_SYS_OFFSET = 0x00029C08;
        internal const UInt32 EPROM_0v_chg_disable_th = 0x00029A0C;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellNumber = 0x0003BE04;
        internal const UInt32 CADC = 0x00033F00;
        internal const UInt32 CADC_Slope_Trim = 0x0003A200;
        internal const UInt32 CADC_Offset_Trim = 0x0003A208;

        internal const UInt32 CellVoltage01 = 0x00031100;
        internal const UInt32 Cell1_Slope_Trim = 0x0003A300;
        internal const UInt32 Cell1_Offset_Trim = 0x0003A900;

        internal const UInt32 CellVoltage02 = 0x00031200;
        internal const UInt32 Cell2_Slope_Trim = 0x0003A305;
        internal const UInt32 Cell2_Offset_Trim = 0x0003A905;

        internal const UInt32 CellVoltage03 = 0x00031300;
        internal const UInt32 Cell3_Slope_Trim = 0x0003A30A;
        internal const UInt32 Cell3_Offset_Trim = 0x0003A90A;

        internal const UInt32 CellVoltage04 = 0x00031400;
        internal const UInt32 Cell4_Slope_Trim = 0x0003A400;
        internal const UInt32 Cell4_Offset_Trim = 0x0003AA00;

        internal const UInt32 CellVoltage05 = 0x00031500;
        internal const UInt32 Cell5_Slope_Trim = 0x0003A405;
        internal const UInt32 Cell5_Offset_Trim = 0x0003AA05;

        internal const UInt32 CellVoltage06 = 0x00031600;
        internal const UInt32 Cell6_Slope_Trim = 0x0003A40A;
        internal const UInt32 Cell6_Offset_Trim = 0x0003AA0A;

        internal const UInt32 CellVoltage07 = 0x00031700;
        internal const UInt32 Cell7_Slope_Trim = 0x0003A500;
        internal const UInt32 Cell7_Offset_Trim = 0x0003AB00;

        internal const UInt32 CellVoltage08 = 0x00031800;
        internal const UInt32 Cell8_Slope_Trim = 0x0003A505;
        internal const UInt32 Cell8_Offset_Trim = 0x0003AB05;

        internal const UInt32 CellVoltage09 = 0x00031900;
        internal const UInt32 Cell9_Slope_Trim = 0x0003A50A;
        internal const UInt32 Cell9_Offset_Trim = 0x0003AB0A;

        internal const UInt32 CellVoltage10 = 0x00031A00;
        internal const UInt32 Cell10_Slope_Trim = 0x0003A600;
        internal const UInt32 Cell1O_Offset_Trim = 0x0003AC00;

        internal const UInt32 CellVoltage11 = 0x00031B00;
        internal const UInt32 Cell11_Slope_Trim = 0x0003A605;
        internal const UInt32 Cell11_Offset_Trim = 0x0003AC05;

        internal const UInt32 CellVoltage12 = 0x00031C00;
        internal const UInt32 Cell12_Slope_Trim = 0x0003A60A;
        internal const UInt32 Cell12_Offset_Trim = 0x0003AC0A;

        internal const UInt32 CellVoltage13 = 0x00031D00;
        internal const UInt32 Cell13_Slope_Trim = 0x0003A700;
        internal const UInt32 Cell13_Offset_Trim = 0x0003AD00;

        internal const UInt32 CellVoltage14 = 0x00031E00;
        internal const UInt32 Cell14_Slope_Trim = 0x0003A705;
        internal const UInt32 Cell14_Offset_Trim = 0x0003AD05;

        internal const UInt32 CellVoltage15 = 0x00031f00;
        internal const UInt32 Cell15_Slope_Trim = 0x0003A70A;
        internal const UInt32 Cell15_Offset_Trim = 0x0003AD0A;

        internal const UInt32 CellVoltage16 = 0x00032000;
        internal const UInt32 Cell16_Slope_Trim = 0x0003A800;
        internal const UInt32 Cell16_Offset_Trim = 0x0003AE00;

        internal const UInt32 Isens = 0x00032100;
        internal const UInt32 Isens_Slope_Trim = 0x0003AF00;
        internal const UInt32 Isens_Offset_Trim = 0x0003AF08;

        internal const UInt32 VBATT = 0x00032400;
        internal const UInt32 VBATT_Slope_Trim = 0x0003FFFF;
        internal const UInt32 VBATT_Offset_Trim = 0x0003B008;

        internal const UInt32 TS = 0x00032200;
        internal const UInt32 TS_Slope_Trim = 0x0003A805;
        internal const UInt32 TS_Offset_Trim = 0x0003AE05;

        internal const UInt32 Op_ovp_th = 0x0003B100;
        internal const UInt32 Op_ovp_rls_hys = 0x0003B108;
        internal const UInt32 Op_ovp_dly = 0x0003B10D;
        internal const UInt32 Op_uvp_th = 0x0003B200;
        internal const UInt32 Op_uvp_rls_hys = 0x0003B207;
        internal const UInt32 Op_uvp_dly = 0x0003B20C;
        internal const UInt32 Op_cocp_th = 0x0003B300;
        internal const UInt32 Op_doc1p_th = 0x0003B308;
        internal const UInt32 Op_doc2p_th = 0x0003B400;
        internal const UInt32 Op_scp_th = 0x0003B404;
        internal const UInt32 Op_doc2p_dly = 0x0003B408;
        internal const UInt32 Op_dsg_th = 0x0003B500;
        internal const UInt32 Op_chg_th = 0x0003B504;
        internal const UInt32 Op_doc1p_dly = 0x0003B508;
        internal const UInt32 Op_cocp_dly = 0x0003B50D;
        internal const UInt32 Op_dot_th = 0x0003B600;
        internal const UInt32 Op_dotr_hys = 0x0003B608;
        internal const UInt32 Op_cot_th = 0x0003B700;
        internal const UInt32 Op_cotr_hys = 0x0003B708;
        internal const UInt32 Op_dut_th = 0x0003B800;
        internal const UInt32 Op_dutr_hys = 0x0003B809;
        internal const UInt32 Op_cut_th = 0x0003B900;
        internal const UInt32 Op_cutr_hys = 0x0003B909;
        internal const UInt32 Op_cb_start_th = 0x0003BA00;
        internal const UInt32 Op_ub_cell_th = 0x0003BB00;
        internal const UInt32 Op_cell_open_th = 0x0003BB06;
        internal const UInt32 Op_multi_function_th = 0x0003BB08;
        internal const UInt32 Op_eoc_th = 0x0003BC00;
        internal const UInt32 Op_CADC_SYS_OFFSET = 0x0003BC08;
        internal const UInt32 Op_0v_chg_disable_th = 0x0003BA0C;

        internal const UInt32 Op_Map_ovp_th = 0x00039100;
        internal const UInt32 Op_Map_ovp_rls_hys = 0x00039108;
        internal const UInt32 Op_Map_ovp_dly = 0x0003910D;
        internal const UInt32 Op_Map_uvp_th = 0x00039200;
        internal const UInt32 Op_Map_uvp_rls_hys = 0x00039207;
        internal const UInt32 Op_Map_uvp_dly = 0x0003920C;
        internal const UInt32 Op_Map_cocp_th = 0x00039300;
        internal const UInt32 Op_Map_doc1p_th = 0x00039308;
        internal const UInt32 Op_Map_doc2p_th = 0x00039400;
        internal const UInt32 Op_Map_scp_th = 0x00039404;
        internal const UInt32 Op_Map_doc2p_dly = 0x00039408;
        internal const UInt32 Op_Map_dsg_th = 0x00039500;
        internal const UInt32 Op_Map_chg_th = 0x00039504;
        internal const UInt32 Op_Map_doc1p_dly = 0x00039508;
        internal const UInt32 Op_Map_cocp_dly = 0x0003950D;
        internal const UInt32 Op_Map_dot_th = 0x00039600;
        internal const UInt32 Op_Map_dotr_hys = 0x00039608;
        internal const UInt32 Op_Map_cot_th = 0x00039700;
        internal const UInt32 Op_Map_cotr_hys = 0x00039708;
        internal const UInt32 Op_Map_dut_th = 0x00039800;
        internal const UInt32 Op_Map_dutr_hys = 0x00039809;
        internal const UInt32 Op_Map_cut_th = 0x00039900;
        internal const UInt32 Op_Map_cutr_hys = 0x00039909;
        internal const UInt32 Op_Map_cb_start_th = 0x00039A00;
        internal const UInt32 Op_Map_ub_cell_th = 0x00039B00;
        internal const UInt32 Op_Map_cell_open_th = 0x00039B06;
        internal const UInt32 Op_Map_multi_function_th = 0x00039B08;
        internal const UInt32 Op_Map_eoc_th = 0x00039C00;
        internal const UInt32 Op_Map_CADC_SYS_OFFSET = 0x00039C08;
        internal const UInt32 Op_Map_0v_chg_disable_th = 0x00039A0C;
        #endregion

        #region Virtual参数GUID
        internal const UInt32 VirtualElement = 0x00070000;
        internal const UInt32 EOV_E = 0x00070001;
        internal const UInt32 EUV_E = 0x00070002;
        internal const UInt32 EDOC1_E = 0x00070003;
        internal const UInt32 ECOC_E = 0x00070004;
        internal const UInt32 EDOT_E = 0x00070005;
        internal const UInt32 EDUT_E = 0x00070006;
        internal const UInt32 ECOT_E = 0x00070007;
        internal const UInt32 ECUT_E = 0x00070008;
        internal const UInt32 EUB_E = 0x00070009;
        internal const UInt32 ECTO_E = 0x0007000A;
        internal const UInt32 E0V_Charge_Prohibit_E = 0x0007000B;
        internal const UInt32 EEOC_E = 0x0007000C;

        internal const UInt32 OOV_E = 0x00070101;
        internal const UInt32 OUV_E = 0x00070102;
        internal const UInt32 ODOC1_E = 0x00070103;
        internal const UInt32 OCOC_E = 0x00070104;
        internal const UInt32 ODOT_E = 0x00070105;
        internal const UInt32 ODUT_E = 0x00070106;
        internal const UInt32 OCOT_E = 0x00070107;
        internal const UInt32 OCUT_E = 0x00070108;
        internal const UInt32 OUB_E = 0x00070109;
        internal const UInt32 OCTO_E = 0x0007010A;
        internal const UInt32 O0V_Charge_Prohibit_E = 0x0007010B;
        internal const UInt32 OEOC_E = 0x0007010C;
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
