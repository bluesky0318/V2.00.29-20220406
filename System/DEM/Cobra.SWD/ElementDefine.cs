using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Cobra.Common;

namespace Cobra.SWD
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
        internal const UInt16 PARAM_HEX_ERROR = 0xFFFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
        internal const UInt32 ElementMask = 0xFFFF0000;
        internal const UInt16 BLOCK_OPERATION_BYTES = 4; //Only Support DWORD
        internal static byte[] interBuffer = new byte[1024 * 64];

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_ERASE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_DEVICE_CLOSED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_DEVICE_CONNECT = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_DEVICE_ENUM = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_DEVICE_RESET = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        #endregion

        internal enum COBRA_SUB_TASK : ushort
        {
            SUB_TASK_MAIN_BLOCK_PROGRAM = 0x10,
            SUB_TASK_MAIN_BLOCK_DUMP = 0x20,
            SUB_TASK_COMPARE = 0x11,
            SUB_TASK_ERASE = 0x12,
        }

        #region JLINK Const
        //Target interfaces for the J-Link.
        internal enum COBRA_JLINK_INTERFACE : ushort
        {
            JLINK_INTERFACE_JTAG = 0,
            JLINK_INTERFACE_SWD = 1,
            JLINK_INTERFACE_FINE = 3,
            JLINK_INTERFACE_ICSP = 4,
            JLINK_INTERFACE_SPI = 5,
            JLINK_INTERFACE_C2 = 6,
        }

        //Enumeration for the different JLink hosts: currently only IP and USB
        internal enum COBRA_JLINK_HOST : ushort
        {
            USB = (1 << 0),
            IP = (1 << 1),
            USB_OR_IP = USB | IP
        }

        // J-Link V9 and J-Link ULTRA/PRO V4 have 336 bytes of memory for licenses,
        //so we base this number on that.  Other models have 80 bytes.
        internal const int MAX_BUF_SIZE = 336;
        //Maximum number of CPU registers.
        internal const int MAX_NUM_CPU_REGISTERS = 256;
        //Maximum speed (in kHz) that can be passed to `set_speed()`.
        internal const int MAX_JTAG_SPEED = 12000;
        // Minimum speed (in kHz) that can be passed to `set_speed()`.
        internal const int MIN_JTAG_SPEED = 5;
        // This speed cannot be passed to `set_speed()`.
        internal const int INVALID_JTAG_SPEED = 0xFFFE;
        // Auto detection of JTAG speed.
        internal const int AUTO_JTAG_SPEED = 0x0;
        // Adaptive clocking as JTAG speed.
        internal const int ADAPTIVE_JTAG_SPEED = 0xFFFF;
        // Maximum number of methods of debug entry at a single time.
        internal const int MAX_NUM_MOES = 8;

        //SerialNumber: J-Link serial number.
        //Connection: type of connection (e.g. ``enums.JLinkHost.USB``)
        //USBAddr: USB address if connected via USB.
        //aIPAddr: IP address if connected via IP.
        //Time: Time period (ms) after which UDP discover answer was received.
        //Time_us: Time period (uS) after which UDP discover answer was received.
        // HWVersion: Hardware version of J-Link, if connected via IP.
        // abMACAddr: MAC Address, if connected via IP.
        //acProduct: Product name, if connected via IP.
        //acNickname: Nickname, if connected via IP.
        //acFWString: Firmware string, if connected via IP.
        //IsDHCPAssignedIP: Is IP address reception via DHCP.
        //IsDHCPAssignedIPIsValid: True if connected via IP.
        //NumIPConnections: Number of IP connections currently established.
        // NumIPConnectionsIsValid: True if connected via IP.
        //aPadding: Bytes reserved for future use.
        /*
        [StructLayout(LayoutKind.Sequential)]
        public class JLinkConnectInfo
        {
            public UInt32 SerialNumber;
            public Byte Connection;
            public UInt32 USBAddr;
            public Byte[] aIPAddr = new Byte[16];
            public int Time;
            public UInt64 Time_us;
            public UInt32 HWVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public Byte[] abMACAddr = new Byte[6];
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] acProduct = new char[32];
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] acNickname = new char[32];
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
            public char[] acFWString = new char[112];
            public char IsDHCPAssignedIP;
            public char IsDHCPAssignedIPIsValid;
            public char NumIPConnections;
            public char NumIPConnectionsIsValid;
            public Byte[] aPadding = new Byte[34];
        }*/
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct JLinkConnectInfo
        {
            public UInt32 SerialNumber;
            public Byte Connection;
            public UInt32 USBAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] aIPAddr;
            public int Time;
            public UInt64 Time_us;
            public UInt32 HWVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public Byte[] abMACAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] acProduct;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] acNickname;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
            public char[] acFWString;
            public char IsDHCPAssignedIP;
            public char IsDHCPAssignedIPIsValid;
            public char NumIPConnections;
            public char NumIPConnectionsIsValid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
            public Byte[] aPadding;
        }

        /*Represents information about an emulator's supported speeds.
         The emulator can support all target interface speeds calculated by dividing the base frequency by atleast MinDiv.
         Attributes:
            SizeOfStruct: the size of this structure.
            BaseFreq: Base frequency (in HZ) used to calculate supported speeds.
            MinDiv: minimum divider allowed to divide the base frequency.
            SupportAdaptive: ``1`` if emulator supports adaptive clocking, otherwise 0.
         */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct JLinkSpeedInfo
        {
            public UInt32 SizeOfStruct;
            public UInt32 BaseFreq;
            public UInt16  MinDiv;
            public UInt16 SupportAdaptive;
        }
        #endregion
    }
}
