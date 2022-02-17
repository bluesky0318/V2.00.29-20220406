    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;
namespace Cobra.FWBigsur
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        internal const UInt16 RETRY_COUNTER = 5;
        internal const UInt16 TRIM_MEMORY_SIZE = 0x80;
        internal const UInt16 PARA_MEMORY_SIZE = 0x100;
        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;

        internal const UInt16 RAM_PAGE_SIZE = 0x200;//512
        internal const UInt16 RAM_MAX_SIZE = 0x400;//1024
        internal const UInt16 FLASH_MAX_CAP = 0x4000;//16K
        internal const UInt16 RAM_DOWNLOAD_BYTES = 45;
        internal const UInt16 RAM_UPLOAD_BYTES = 512;
        internal const UInt32 RAM_START_ADD = 0x20000300;
        internal static byte[] interBuffer = new byte[1024*64];

        internal enum UARTISP : ushort
        {
            UARTISP_SYN_ASCII = 0,
            UARTISP_FREQ_12000,
            UARTISP_READ_CID,
            UARTISP_SYN_CHAR,
            UARTISP_U_23130,
            UARTISP_A_0,
            UARTISP_A_1,
            UARTISP_R_X_Y,
            UARTISP_W_X_Y,
            UARTISP_E_15_15,
            UARTISP_E_0_15,
            UARTISP_J,
            UARTISP_K,
            UARTISP_N,
            UARTISP_P_15_15,
            UARTISP_P_0_15,
            UARTISP_C_X_Y_Z,
        }

        internal static readonly Dictionary<UARTISP, string> Uart_ISP = new Dictionary<UARTISP, string>()
        {
            {UARTISP.UARTISP_SYN_ASCII,"Synchronized\r\n"}, 
            {UARTISP.UARTISP_FREQ_12000,"12000\r\n"},
            {UARTISP.UARTISP_READ_CID,"J\r\n"},
            {UARTISP.UARTISP_SYN_CHAR,"?\r\n"},        //3F 0D 0A
            {UARTISP.UARTISP_U_23130,"U 23130\r\n"},  //55 20 32 33 31 33 30 0D 0A
            {UARTISP.UARTISP_A_0,"A 0\r\n"},          //41 20 30 0D 0A
            {UARTISP.UARTISP_A_1,"A 1\r\n"},          //41 20 31 0D 0A
            {UARTISP.UARTISP_R_X_Y,"R {0} {1}\r\n"},  //52 20 30 20 35 31 32 0D 0A  
            {UARTISP.UARTISP_W_X_Y,"W {0} {1}\r\n"},   //57 20
            {UARTISP.UARTISP_E_15_15,"E 15 15\r\n"},    //45 20 31 35 20 31 35 0D 0A
            {UARTISP.UARTISP_E_0_15,"E 0 15\r\n"},    //45 20 30 20 31 35 0D 0A
            {UARTISP.UARTISP_J,"J\r\n"},                //4A 0D 0A UART ISP Read Part Identification command
            {UARTISP.UARTISP_K,"K\r\n"},                //4B 0D 0A UART ISP Read Boot Code version number command
            {UARTISP.UARTISP_N,"N\r\n"},                //4E 0D 0A UART ISP ReadUID command
            {UARTISP.UARTISP_P_15_15,"P 15 15\r\n"},   //50 20 31 35 20 31 35 0D 0A
            {UARTISP.UARTISP_P_0_15,"P 0 15\r\n"},   //50 20 30 20 31 35 0D 0A
            {UARTISP.UARTISP_C_X_Y_Z,"C {0} {1} {2}\r\n"},  //43 20 31 30 32 34 20 32 36 38 34 33 36 32 32 34 20 31 30 32 34 0D 0A
        };

        internal enum COBRA_PARAM_SUBTYPE : ushort
        {
        }

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
        }
        
        #region YFLASH参数GUID
        internal const UInt32 YFLASHElement = 0x00020000; //YFLASH参数起始地址
        internal const UInt32 PARAElement = 0x00020000; //YFLASH参数起始地址
        internal const UInt32 ProInfoElement = 0x000f0000; //Project Information
        internal const UInt32 ProParaElement = 0x000e0000; //Project parameter list

        internal const UInt32 ParameterAreaStart = 0x00003E80;
        internal const UInt32 LogAreaStart = 0x00003F80;
        internal const UInt32 TableAreaStart = 0x00003C40;
        internal const UInt32 ProgramAreaStart = 0x00000000;
        #endregion
    }
}
