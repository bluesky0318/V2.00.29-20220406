using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cobra.SWD
{
    public static class JTAG
    {
        #region 确认函数
        internal static void JLINKARM_Sleep(int ms)
        {
            Thread.Sleep(ms);
        }

        #region JLINK开启
        /// <summary>
        /// 打开JLINK设备
        /// </summary>
        /// <remarks>已被废弃，但还是可以用</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_SelectUSB(int serial_no);

        /// <summary>
        /// 打开JLINK设备
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_Open();

        /// <summary>
        /// JLINK是否已经可以操作了
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool JLINKARM_IsOpen();

        /// <summary>
        /// 关闭JLINK设备
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_Close();

        /// <summary>
        /// 获取设备数
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_EMU_GetNumDevices();

        /// <summary>
        /// 使用serial_no获取设备
        /// 成功：0， 失败： -1
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_EMU_SelectByUSBSN(long serial_no);

        /// <summary>
        /// Selects the specified target interface
        /// 成功：0， 失败： -1
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_TIF_Select(int type);
        #endregion

        #region JLINK信息
        /// <summary>
        /// 获取JLINK的DLL版本号
        /// </summary>
        /// <returns></returns>
        /// <remarks>使用10进制数表示</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_GetDLLVersion();

        /// <summary>
        /// 获取JLINK的固件版本号
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_GetHardwareVersion();

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JLINKARM_GetFeatureString([Out(), MarshalAs(UnmanagedType.LPArray)] byte[] oBuffer);

        /// <summary>
        /// The string of the OEM.  If this is an original SEGGER product, then None is returned instead.
        /// </summary>
        /// <param name="oBuffer"></param>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JLINKARM_GetOEMString([Out(), MarshalAs(UnmanagedType.LPArray)] byte[] oBuffer);

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern StringBuilder JLINKARM_GetCompileDateTime();

        /// <summary>
        /// Return the dataTime like 20090928 not the serial number
        /// </summary>
        /// <returns></returns>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_GetSN();

        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_ExecCommand(string cmd, [Out(), MarshalAs(UnmanagedType.LPArray)] char[] buf, int maxBufSize);

        /// <summary>
        /// 设置JLINK接口速度
        /// </summary>
        /// <param name="speed"></param>
        /// <remarks>0为自动调整</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void JLINKARM_SetSpeed(int speed);

        /// <summary>
        /// 设置JTAG为最高速度
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_SetMaxSpeed();

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_GetSpeed();

        [DllImport("JLinkARM.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void JLINKARM_GetSpeedInfo(ref ElementDefine.JLinkSpeedInfo info);

        [DllImport("JLinkARM.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int JLINKARM_DEVICE_GetInfo(int index, ref ElementDefine.JLinkConnectInfo info);


        /// <summary>
        /// 打开JLINK设备
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_EMU_SelectByIndex(int serial_no);

        /// <summary>
        /// Returns the connected emulators number.
        /// Args: host (int): host type to search (default: ``JLinkHost.USB``)
        /// 成功：0， 失败： -1
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_EMU_GetList(int host, int param0, int param1); 

        /// <summary>
        /// Returns a list of all the connected emulators.
        /// Args: host (int): host type to search (default: ``JLinkHost.USB``)
        /// 成功：0， 失败： -1
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_EMU_GetList(int host, IntPtr info, int param1);

        /// <summary>
        ///  Returns the name of the target ARM core.
        /// </summary>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_Core2CoreName(int cmd, [Out(), MarshalAs(UnmanagedType.LPArray)] char[] buf, int maxBufSize);

        /// <summary>
        ///  The identifier of the CPU core.
        ///  This is distinct from the value returned from ``core_id()`` which is the ARM specific identifier.
        /// </summary>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_CORE_GetFound();
        #endregion

        #region ARM信息
        /// <summary>
        /// 获取当前MCU的ID号
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_GetId();

        /// <summary>
        /// 成功：0， 失败： -1
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_Connect();

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool JLINKARM_IsConnected();

        /// <summary>
        /// 系统复位
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_Reset();

        /// <summary>
        /// 当前MCU是否处于停止状态
        /// </summary>
        /// <returns>True if the CPU core is halted, otherwise False</returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool JLINKARM_IsHalted();

        /// <summary>
        /// 中断程序执行
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool JLINKARM_Halt();
                
        /// <summary>
        /// Amount of milliseconds to delay after reset (default: 0)
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_SetResetDelay(int ms);

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_Go();
        #endregion

        #region 读、写操作
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ReadMemU8(UInt32 addr, UInt32 leng, ref byte buf, ref byte status);

        /// <summary>
        /// 写入8位的数据
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="dat"></param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_WriteU8(UInt32 addr, byte dat);

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ReadMemU16(UInt32 addr, UInt32 leng, ref UInt16 buf, ref byte status);

        /// <summary>
        /// 写入16位的数据
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="dat"></param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_WriteU16(UInt32 addr, UInt16 dat);

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void JLINKARM_ReadMemU32(UInt32 addr, UInt32 leng, ref UInt32 buf, ref byte status);

        /// <summary>
        /// 写入32位的数据
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="dat"></param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void JLINKARM_WriteU32(UInt32 addr, UInt32 dat);

        /// <summary>
        /// 读取一段数据
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="size"></param>
        /// <param name="buf"></param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ReadMem(UInt32 addr, UInt32 size, [Out(), MarshalAs(UnmanagedType.LPArray)] byte[] buf);

        /// <summary>
        /// 写入一段数据
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="size"></param>
        /// <param name="buf"></param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_WriteMem(UInt32 addr, UInt32 size, byte[] buf);
        #endregion

        #region 芯片操作
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINK_EraseChip();        
        #endregion

        #region 文件操作
        /// <summary>
        /// Write the file data into flashes the target device.
        /// </summary>
        /// <param name="path (str)">absolute path to the source file to flash</param>
        /// <param name="addr (int)">start address on flash which to write the data</param>
        /// <return>Integer value greater than or equal to zero.  Has no significance.</return>
        /// <remarks></remarks>       
        [DllImport("JLinkARM.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINK_DownloadFile(string path, int addr); 
        #endregion

        #region 其他
        /// <summary>
        /// 测试结果返回为RDI,FlashDL,FlashBP,JFlash,GDB
        /// </summary>
        /// <returns></returns>
        internal static string JLINKARM_StringFeature()
        {
            byte[] aa = new byte[1000];
            JLINKARM_GetFeatureString(aa);
            System.Text.ASCIIEncoding kk = new System.Text.ASCIIEncoding();
            string ss = kk.GetString(aa);
            return ss;
        }

        /// <summary>
        /// 测试结果返回为0
        /// </summary>
        /// <returns></returns>
        internal static string JLINKARM_StringOEM()
        {
            byte[] aa = new byte[1000];
            JLINKARM_GetOEMString(aa);
            System.Text.ASCIIEncoding kk = new System.Text.ASCIIEncoding();
            string ss = kk.GetString(aa);
            return ss;
        }
        #endregion
        #endregion

        #region 未确认

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_GoAllowSim();

        /// <summary>
        /// 单步执行
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool JLINKARM_Step();

        /// <summary>
        /// 清除错误信息
        /// </summary>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ClrError();

        /// <summary>
        /// 取消程序断点
        /// </summary>
        /// <param name="index">断点序号</param>
        /// <remarks>配合JLINKARM_SetBP()使用</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ClrBP(UInt32 index);

        /// <summary>
        /// 设置程序断点
        /// </summary>
        /// <param name="index">断点序号</param>
        /// <param name="addr">目标地址</param>
        /// <remarks>建议使用JLINKARM_SetBPEx()替代</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_SetBP(UInt32 index, UInt32 addr);

        /// <summary>
        /// 设置程序断点
        /// </summary>
        /// <param name="addr">目标地址</param>
        /// <param name="mode">断点类型</param>
        /// <returns>Handle,提供给JLINKARM_ClrBPEx()使用</returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_SetBPEx(UInt32 addr, BP_MODE mode);

        /// <summary>
        /// 取消程序断点
        /// </summary>
        /// <param name="handle"></param>
        /// <remarks>配合JLINKARM_SetBPEx()使用</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ClrBPEx(int handle);

        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int JLINKARM_SetWP(UInt32 addr, UInt32 addrmark, UInt32 dat, UInt32 datmark, byte ctrl, byte ctrlmark);

        /// <summary>
        /// 取消数据断点
        /// </summary>
        /// <param name="handle"></param>
        /// <remarks>配合JLINKARM_SetWP()使用</remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ClrWP(int handle);

        /// <summary>
        /// 设置寄存器
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dat"></param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_WriteReg(ARM_REG index, UInt32 dat);

        /// <summary>
        /// 读取寄存器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_ReadReg(ARM_REG index);

        /// <summary>
        /// 从调试通道获取一串数据
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size">需要获取的数据长度</param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_ReadDCCFast([Out(), MarshalAs(UnmanagedType.LPArray)] UInt32[] buf, UInt32 size);

        /// <summary>
        /// 从调试通道获取一串数据
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size">希望获取的数据长度</param>
        /// <param name="timeout"></param>
        /// <returns>实际获取的数据长度</returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_ReadDCC([Out(), MarshalAs(UnmanagedType.LPArray)] UInt32[] buf, UInt32 size, int timeout);

        /// <summary>
        /// 向调试通道写入一串数据
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size">需要写入的数据长度</param>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void JLINKARM_WriteDCCFast(UInt32[] buf, UInt32 size);

        /// <summary>
        /// 向调试通道写入一串数据
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size">希望写入的数据长度</param>
        /// <param name="timeout"></param>
        /// <returns>实际写入的数据长度</returns>
        /// <remarks></remarks>
        [DllImport("JLinkARM.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 JLINKARM_WriteDCC(UInt32[] buf, UInt32 size, int timeout);

        /// <summary>
        /// 设置数据断点
        /// </summary>
        /// <param name="addr">目标地址</param>
        /// <param name="addrmark">地址屏蔽位</param>
        /// <param name="dat">目标数据</param>
        /// <param name="datmark">数据屏蔽位</param>
        /// <param name="mode">触发模式</param>
        /// <returns>Handle,提供给JLINKARM_ClrWP()函数使用</returns>
        /// <remarks>当前数值除了屏蔽位以外的数据位,与目标数据除了屏蔽位以外的数据位,一致即可产生触发</remarks>
        internal static int JLINKARM_SetWP(UInt32 addr, UInt32 addrmark, UInt32 dat, UInt32 datmark, WP_MODE mode)
        {
            switch (mode)
            {
                case WP_MODE.READ_WRITE:
                    return JLINKARM_SetWP(addr, addrmark, dat, datmark, 0x8, 0xf7);
                case WP_MODE.READ:
                    return JLINKARM_SetWP(addr, addrmark, dat, datmark, 0x8, 0xf6);
                case WP_MODE.WRITE:
                    return JLINKARM_SetWP(addr, addrmark, dat, datmark, 0x9, 0xf6);
                default:
                    return JLINKARM_SetWP(addr, addrmark, dat, datmark, 0x8, 0xf7);
            }
        }

        /// <summary>
        /// ARM内部寄存器
        /// </summary>
        /// <remarks></remarks>
        public enum ARM_REG : uint
        {
            R0,
            R1,
            R2,
            R3,
            R4,
            R5,
            R6,
            R7,
            CPSR,
            R15,
            R8_USR,
            R9_USR,
            R10_USR,
            R11_USR,
            R12_USR,
            R13_USR,
            R14_USR,
            SPSR_FIQ,
            R8_FIQ,
            R9_FIQ,
            R10_FIQ,
            R11_FIQ,
            R12_FIQ,
            R13_FIQ,
            R14_FIQ,
            SPSR_SVC,
            R13_SVC,
            R14_SVC,
            SPSR_ABT,
            R13_ABT,
            R14_ABT,
            SPSR_IRQ,
            R13_IRQ,
            R14_IRQ,
            SPSR_UND,
            R13_UND,
            R14_UND,
            SPSR_SYS,
            R13_SYS,
            R14_SYS,
            PC = 9
        }

        /// <summary>
        /// 程序断点模式
        /// </summary>
        /// <remarks></remarks>
        public enum BP_MODE : uint
        {
            ARM = 1,
            THUMB = 2,
            HARD_ARM = 0xffffff01u,
            HARD_THUMB = 0xffffff02u,
            SOFT_ARM = 0xf1u,
            SOFT_THUMB = 0xf2u
        }

        /// <summary>
        /// 数据断点模式
        /// </summary>
        /// <remarks></remarks>
        public enum WP_MODE : uint
        {
            READ_WRITE,
            READ,
            WRITE
        }        
        #endregion
    }
}
