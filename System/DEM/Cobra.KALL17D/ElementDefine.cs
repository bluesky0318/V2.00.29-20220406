using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.KALL17D
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        internal const UInt16 EF_MEMORY_SIZE    = 0x10;
        internal const UInt16 EF_MEMORY_OFFSET = 0x80;
		/////////////////////////////////////////////////////////////
        internal const UInt16 EF_ATE_OFFSET = 0x60;
        internal const UInt16 EF_ATE_TOP = 0x66;
        internal const UInt16 ATE_CRC_OFFSET = 0x66;

        internal const UInt16 EF_USR_OFFSET = 0x67;
        internal const UInt16 EF_USR_TOP = 0x6f;
        internal const UInt16 USR_CRC_OFFSET = 0x6f;

        internal const UInt16 ATE_CRC_BUF_LEN = 27;     // 4 * 7 - 1
        internal const UInt16 USR_CRC_BUF_LEN = 35;     // 4 * 9 - 1

        internal const UInt16 CELL_OFFSET = 0x11;
        internal const UInt16 Vpack_wkup = 0x1a;
        internal const UInt16 CELL_TOP = 0x1E;
        internal const UInt16 CURRENT_OFFSET = 0x31;
        internal const UInt16 V800MV_OFFSET = 0x20;
		/////////////////////////////////////////////////////////////
        internal const UInt16 OP_MEMORY_SIZE        = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR       = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR  = -9999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 SPI_RETRY_COUNT = 10;
        internal const UInt16 CADC_RETRY_COUNT = 30;
        internal const UInt16 CMD_SECTION_SIZE = 3;
        // EFUSE control registers' addresses
        internal const byte WORKMODE_OFFSET = 0x70;
        internal static int m_trim_count = 0;

        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            VOLTAGE = 1,
            INT_TEMP,
            EXT_TEMP,
            CURRENT = 4,
            CADC = 5,
            COULOMB_COUNTER = 6,
            WKUP,
            PRE_SET = 12,
            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41
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

        internal enum WORK_MODE : ushort
        {
            NORMAL = 0,
            INTERNAL = 0x01,
            PROGRAM = 0x02,
            //EFUSE_WORKMODE_MAPPING = 0x03
        }

        internal enum COMMAND : ushort
        {
            SLOP_TRIM = 5,
            STANDBY_MODE = 6,
            ACTIVE_MODE = 7,
            SHUTDOWN_MODE = 8,
            CFET_ON = 9,
            DFET_ON = 10,
            CFET_OFF = 11,
            DFET_OFF = 12,
            TRIGGER_8_CURRENT_4 = 13,
            TRIGGER_8_CURRENT_8 = 14,
            TRIGGER_8_CURRENT_1 = 15,
            ATE_CRC_CHECK = 17,
            STANDBY_THEN_ACTIVE_100MS = 18,
            ACTIVE_THEN_STANDBY_100MS = 19,
            STANDBY_THEN_ACTIVE_50MS = 20,
            ACTIVE_THEN_STANDBY_50MS = 21,
            STANDBY_THEN_ACTIVE_30MS = 22,
            ACTIVE_THEN_STANDBY_30MS = 23,
            STANDBY_THEN_ACTIVE_20MS = 24,
            ACTIVE_THEN_STANDBY_20MS = 25,
            TRIM_COUNT_SLOPE = 0x40,
            TRIM_COUNT_OFFSET = 0x41,
            TRIM_COUNT_RESET = 0x42,
            TRIM_SLOPE_EIGHT_MODE = 0x43,
            TRIM_OFFSET_EIGHT_MODE = 0x44,
            TRIM_RESET =0x45,
            OPTIONS = 0xFFFF
        }


        internal enum SAR_MODE : byte
        {
            TRIGGER_1 = 0,
            TRIGGER_8 = 1,
            AUTO_1 = 2,
            AUTO_8 = 3,
            TRIGGER_8_TIME_CURRENT_SCAN = 4,
            DISABLE = 5
        }

        public enum CADC_MODE : byte
        {
            DISABLE = 0,
            MOVING = 1,
            TRIGGER = 2,
        }

        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpETRx = TemperatureElement + 0x00;
        #endregion
        internal const UInt32 SectionMask = 0xffff0000;
        
        #region EFUSE参数GUID
        internal const UInt32 EFUSEElement = 0x00020000; //EFUSE参数起始地址

        internal const byte EF_RD_CMD = 0x30;
        internal const byte EF_WR_CMD = 0xc5;

        #endregion
        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;
        internal const UInt32 CellNum = 0x0003510d; //
        internal const UInt32 CellBase = 0x00030100; //
        internal const UInt32 CellCurrent = 0x00031200; //
        internal const UInt32 BASIC_CADC = 0x00033100; //
        internal const UInt32 TRIGGER_CADC = 0x00033800; //
        internal const UInt32 MOVING_CADC = 0x00033900; //

        internal const UInt32 OVP_H = 0x00035008;
        internal const UInt32 DOC1P = 0x00035100;
        internal const UInt32 COCP = 0x00035200;

        internal const byte OR_RD_CMD = 0x30;
        internal const byte OR_WR_CMD = 0xC5;

        internal const UInt32 OP_CELL1V = 0x00030100;
        internal const UInt32 OP_CELL1V_SLOP = 0x0003A30C;
        internal const UInt32 OP_CELL1V_OFFSET = 0x0003A80A;

        internal const UInt32 OP_CELL2V = 0x00030200;
        internal const UInt32 OP_CELL2V_SLOP = 0x0003A308;
        internal const UInt32 OP_CELL2V_OFFSET = 0x0003A805;

        internal const UInt32 OP_CELL3V = 0x00030300;
        internal const UInt32 OP_CELL3V_SLOP = 0x0003A304;
        internal const UInt32 OP_CELL3V_OFFSET = 0x0003A800;

        internal const UInt32 OP_CELL4V = 0x00030400;
        internal const UInt32 OP_CELL4V_SLOP = 0x0003A300;
        internal const UInt32 OP_CELL4V_OFFSET = 0x0003A90A;

        internal const UInt32 OP_CELL5V = 0x00030500;
        internal const UInt32 OP_CELL5V_SLOP = 0x0003A40C;
        internal const UInt32 OP_CELL5V_OFFSET = 0x0003A905;

        internal const UInt32 OP_CELL6V = 0x00030600;
        internal const UInt32 OP_CELL6V_SLOP = 0x0003A408;
        internal const UInt32 OP_CELL6V_OFFSET = 0x0003A900;

        internal const UInt32 OP_CELL7V = 0x00030700;
        internal const UInt32 OP_CELL7V_SLOP = 0x0003A404;
        internal const UInt32 OP_CELL7V_OFFSET = 0x0003AA0A;

        internal const UInt32 OP_CELL8V = 0x00030800;
        internal const UInt32 OP_CELL8V_SLOP = 0x0003A400;
        internal const UInt32 OP_CELL8V_OFFSET = 0x0003AA05;

        internal const UInt32 OP_CELL9V = 0x00030900;
        internal const UInt32 OP_CELL9V_SLOP = 0x0003A50C;
        internal const UInt32 OP_CELL9V_OFFSET = 0x0003AA00;

        internal const UInt32 OP_CELL10V = 0x00030A00;
        internal const UInt32 OP_CELL10V_SLOP = 0x0003A508;
        internal const UInt32 OP_CELL10V_OFFSET = 0x0003AB0A;

        internal const UInt32 OP_CELL11V = 0x00030B00;
        internal const UInt32 OP_CELL11V_SLOP = 0x0003A504;
        internal const UInt32 OP_CELL11V_OFFSET = 0x0003AB05;

        internal const UInt32 OP_CELL12V = 0x00030C00;
        internal const UInt32 OP_CELL12V_SLOP = 0x0003A500;
        internal const UInt32 OP_CELL12V_OFFSET = 0x0003AB00;

        internal const UInt32 OP_CELL13V = 0x00030D00;
        internal const UInt32 OP_CELL13V_SLOP = 0x0003A60C;
        internal const UInt32 OP_CELL13V_OFFSET = 0x0003AC05;

        internal const UInt32 OP_CELL14V = 0x00030E00;
        internal const UInt32 OP_CELL14V_SLOP = 0x0003A608;
        internal const UInt32 OP_CELL14V_OFFSET = 0x0003AC00;

        internal const UInt32 OP_CELL15V = 0x00030F00;
        internal const UInt32 OP_CELL15V_SLOP = 0x0003A604;
        internal const UInt32 OP_CELL15V_OFFSET = 0x0003AD05;

        internal const UInt32 OP_CELL16V = 0x00031000;
        internal const UInt32 OP_CELL16V_SLOP = 0x0003A600;
        internal const UInt32 OP_CELL16V_OFFSET = 0x0003AD00;

        internal const UInt32 OP_CELL17V = 0x00031100;
        internal const UInt32 OP_CELL17V_SLOP = 0x0003A70C;
        internal const UInt32 OP_CELL17V_OFFSET = 0x0003AE00;

        internal const UInt32 OP_PACK_CUR = 0x00031200;
        internal const UInt32 OP_ISENSE_SLOP = 0x0003A700;
        internal const UInt32 OP_ISENSE_OFFSET = 0x0003AF08;

        internal const UInt32 OP_VAUX = 0x00031900;
        internal const UInt32 OP_VAUX_SLOP = 0x0003A20C;
        internal const UInt32 OP_VAUX_OFFSET = 0x0003AD0A;

        internal const UInt32 OP_VBATT = 0x00031b00;
        internal const UInt32 OP_VBATT_SLOP = 0x0003A705;

        internal const UInt32 OP_CADC = 0x00033800;
        internal const UInt32 OP_CADC_SLOP = 0x0003A100;
        internal const UInt32 OP_CADC_OFFSET = 0x0003A108;
        #endregion

        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;

        internal const UInt32 OVP_E = 0x000c0001; //
        internal const UInt32 DOC1P_E = 0x000c0002; //
        internal const UInt32 COCP_E = 0x000c0003; //
        #endregion
    }


    internal class DataPoint
    {
        private double[] input = new double[5];
        private double[] output = new double[5];
        private Dictionary<int, List<(double, double)>> dic = new Dictionary<int, List<(double, double)>>();
        public Parameter parent = null;

        public DataPoint(Parameter param)
        {
            dic.Clear();
            parent = param;
        }

        public void Reset()
        {
            ElementDefine.m_trim_count = 0;
            Array.Clear(input, 0, input.Length);
            Array.Clear(output, 0, output.Length);
            dic.Clear();
        }

        public void SetInput(double din)
        {
            input[ElementDefine.m_trim_count] = din;
        }

        public void SetOutput(int code,double dou)
        {
            output[ElementDefine.m_trim_count] = dou;
            if (!dic.ContainsKey(code))
            {
                List<(double, double)> list = new List<(double, double)>();
                list.Add((input[ElementDefine.m_trim_count], dou));
                dic.Add(code, list);
            }
            else
                dic[code].Add((input[ElementDefine.m_trim_count], dou));
        }
        public UInt32 GetSlope(ref int slope)
        {
            double dslope;
            Dictionary<int, double> dic_slop = new Dictionary<int, double>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            FolderMap.WriteFile("--------------------Count Slope-----------------------------------\n");
            double sumxy = 0, sumx = 0, sumy = 0, sumx2 = 0;
            foreach (int code in dic.Keys)
            {
                sumxy = 0;sumx = 0;sumy = 0;sumx2 = 0;
                List<(double, double)> list = dic[code];
                double n = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    sumxy += list[i].Item2 * list[i].Item1;
                    sumx += list[i].Item2;
                    sumy += list[i].Item1;
                    sumx2 += list[i].Item2 * list[i].Item2;
                    FolderMap.WriteFile(string.Format("Guid:{0:X4},Code:{1},Input:{2},Output:{3}", parent.guid, code, list[i].Item1.ToString(), list[i].Item2.ToString()));
                }
                dslope = ((sumx2 - sumx * sumx / n)/(sumxy - sumx * sumy / n));
                dic_slop.Add(code, dslope);
                FolderMap.WriteFile(string.Format("Code:{0},Slope:{1}", code, dslope));
            }
            slope = dic_slop.OrderBy(d => Math.Abs(Math.Abs(d.Value) - 1)).Select(d => d.Key).FirstOrDefault();
            FolderMap.WriteFile(string.Format("The best: Guid:{0:X4},Code:{1}", parent.guid, slope));
            return ret;
        }

        public UInt32 GetOffset(ref double offset)
        {
            StringBuilder strIn = new StringBuilder();
            StringBuilder strOu = new StringBuilder();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            /*
            for (int i = 0; i < input.Length; i++)
                ddata += (output[i] - input[i]);
            offset = ddata / input.Length;*/
            offset = (output[0] - input[0]);

            FolderMap.WriteFile("--------------------Count Offset-----------------------------------\n");
            /*for (int i = 0; i < input.Length; i++)
            {
                strIn.Append(string.Format("{0:N4}", input[i]));
                strIn.Append("--");
                strOu.Append(string.Format("{0:N4}", output[i]));
                strOu.Append("--");
            }*/
            FolderMap.WriteFile(string.Format("Input:{0} \n Output:{1} \n offset:{2}", input[0].ToString(), output[0].ToString(), offset));
            return ret;
        }
    }
}
