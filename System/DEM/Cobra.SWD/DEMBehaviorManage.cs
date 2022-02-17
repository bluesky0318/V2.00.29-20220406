//#define DEBUG_LOG
//#define DATA_PACKAGE_LEN 32

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.SWD
{
    internal class DEMBehaviorManage
    {
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        private object m_lock = new object();
        private CCommunicateManager m_Interface = new CCommunicateManager();

        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_ERASE,"Failed to erase chip!"},
            {ElementDefine.IDS_ERR_DEM_DEVICE_CLOSED,"Device should be not open!"},
            {ElementDefine.IDS_ERR_DEM_DEVICE_CONNECT,"Failed to connect device!"},  
            {ElementDefine.IDS_ERR_DEM_DEVICE_ENUM,"Failed to enumerate devices!"},
            {ElementDefine.IDS_ERR_DEM_DEVICE_RESET,"Failed to reset chip!"}
        };
        #endregion

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
        }

        #region 端口操作
        public bool CreateInterface()
        {
            Connected_emulators(ElementDefine.COBRA_JLINK_HOST.USB);
            JTAG.JLINKARM_Open();
            JTAG.JLINKARM_TIF_Select((int)ElementDefine.COBRA_JLINK_INTERFACE.JLINK_INTERFACE_SWD);
            Connect("Cortex-M0", 4000, true);
            if (JTAG.JLINKARM_IsOpen())
                return true;
            else
                return false;
        }

        public bool DestroyInterface()
        {
            JTAG.JLINKARM_Close();
            if (JTAG.JLINKARM_IsOpen())
                return false;
            else
                return true;
        }

        public bool EnumerateInterface()
        {
            return true;
        }

        /// <summary>
        /// Connects the J-Link to its target.
        /// </summary>
        /// <param name="chip_name">target chip name</param>
        /// <param name="speed">connection speed, one of {5-12000, 'auto', 'adaptive'}</param>
        /// <param name="verbose">boolean indicating if connection should be verbose in logging</param>
        /// <returns>None</returns>
        private UInt32 Connect(string chip_name, int speed = 4000, bool verbose = false)
        {
            int res = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (verbose)
                Exec_command("EnableRemarks = 1");
            //This is weird but is currently the only way to specify what the
            //target is to the J-Link.
            Exec_command("Device = Cortex-M0");//(string.Format("Device = {0:%s}",chip_name));
            // Need to select target interface speed here, so the J-Link knows what
            // speed to use to establish target communication.
            JTAG.JLINKARM_SetSpeed(speed);
            res = JTAG.JLINKARM_Connect();
            if (res < 0)
                ret = ElementDefine.IDS_ERR_DEM_DEVICE_CONNECT;
            return ret;
        }

        /// <summary>
        /// Executes the given command.
        /// This method executes a command by calling the DLL's exec method.Direct API methods should be prioritized over calling this method.
        /// </summary>
        /// <param name="cmd(str)">the command to run</param>
        /// <returns>The return code of running the command.</returns>
        private UInt32 Exec_command(string cmd)
        {
            int res = 0;
            char[] err_buf = new char[ElementDefine.MAX_BUF_SIZE];
            res = JTAG.JLINKARM_ExecCommand(cmd, err_buf, ElementDefine.MAX_BUF_SIZE);
            return 0;
            /*err_buf = (ctypes.c_char * self.MAX_BUF_SIZE)()
                    res = self._dll.
                    err_buf = ctypes.string_at(err_buf).decode()

                    if len(err_buf) > 0:
                        # This is how they check for error in the documentation, so check
                        # this way as well.
                        raise errors.JLinkException(err_buf.strip())

                    return res*/
        }

        /// <summary>
        /// Returns a list of all the connected emulators.
        /// </summary>
        /// <param name="host(int)">host type to search (default:JLinkHost.USB``)</param>
        /// <returns>List of JLinkConnectInfospecifying the connected emulators.</returns>
        private UInt32 Connected_emulators(ElementDefine.COBRA_JLINK_HOST host = ElementDefine.COBRA_JLINK_HOST.USB)
        {
            int res = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            res = JTAG.JLINKARM_EMU_GetList((int)host, 0, 0);
            if (res < 0) return ElementDefine.IDS_ERR_DEM_DEVICE_ENUM;

            int num_devices = res;
            ElementDefine.JLinkConnectInfo[] info = new ElementDefine.JLinkConnectInfo[num_devices];
            int size = Marshal.SizeOf(typeof(ElementDefine.JLinkConnectInfo)) * num_devices;
            IntPtr pBuff = Marshal.AllocHGlobal(size);  //申请非托管内存
            /* Marshal.StructureToPtr(info, pBuff, true);*/
            int num_found = JTAG.JLINKARM_EMU_GetList((int)host, pBuff, num_devices);
            //if (num_found < 0) return num_found;

            //ElementDefine.JLinkConnectInfo[] info1 = new ElementDefine.JLinkConnectInfo[num_devices];
            var info1 = Marshal.PtrToStructure(pBuff, typeof(ElementDefine.JLinkConnectInfo));
            return 0;
        }

        /// <summary>
        /// Returns the name of the target ARM core.such as CORTEX-M0
        /// </summary>
        /// <returns>The target core's name.</returns>
        private string Core_name()
        {
            char[] buf = new char[ElementDefine.MAX_BUF_SIZE];
            JTAG.JLINKARM_Core2CoreName(JTAG.JLINKARM_CORE_GetFound(), buf, ElementDefine.MAX_BUF_SIZE);
            return new string(buf);
        }
        #endregion

        #region 寄存器基础操作
        #region 操作寄存器父级操作
        protected UInt32 ReadByte(UInt32 reg, ref byte val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref val);
            }
            return ret;
        }

        protected UInt32 ReadWord(UInt32 reg, ref UInt16 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref val);
            }
            return ret;
        }

        protected UInt32 Read2Word(UInt32 reg, ref UInt32 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnRead2Word(reg, ref val);
            }
            return ret;
        }

        protected UInt32 BlockRead(UInt32 reg, ref byte[] pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnBlockRead(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteByte(UInt32 reg, byte val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }

        protected UInt32 WriteWord(UInt32 reg, UInt16 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }

        protected UInt32 Write2Word(UInt32 reg, UInt32 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWrite2Word(reg, val);
            }
            return ret;
        }

        protected UInt32 BlockWrite(UInt32 reg, byte[] pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnBlockWrite(reg, pval);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnReadByte(UInt32 reg, ref byte pval)
        {
            byte ret = 0;
            JTAG.JLINKARM_ReadMemU8(reg, 1, ref pval, ref ret);
            return ret;
        }

        protected UInt32 OnReadWord(UInt32 reg, ref UInt16 pval)
        {
            byte ret = 0;
            JTAG.JLINKARM_ReadMemU16(reg, 1, ref pval, ref ret);
            return ret;
        }

        protected UInt32 OnRead2Word(UInt32 reg, ref UInt32 pval)
        {
            byte ret = 0;
            JTAG.JLINKARM_ReadMemU32(reg, 1, ref pval, ref ret);
            return ret;
        }

        protected UInt32 OnBlockRead(UInt32 reg, ref byte[] pval)
        {
            byte ret = 0;
            JTAG.JLINKARM_ReadMem(reg, (UInt32)pval.Length, pval);
            return ret;
        }

        protected UInt32 OnWriteByte(UInt32 reg, byte val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            JTAG.JLINKARM_WriteU8(reg, val);
            return ret;
        }

        protected UInt32 OnWriteWord(UInt32 reg, UInt16 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            JTAG.JLINKARM_WriteU16(reg, val);
            return ret;
        }

        protected UInt32 OnWrite2Word(UInt32 reg, UInt32 val)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            JTAG.JLINKARM_WriteU32(reg, val);
            return ret;
        }

        protected UInt32 OnBlockWrite(UInt32 reg, byte[] pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            JTAG.JLINKARM_WriteMem(reg, (UInt32)pval.Length, pval);
            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        /// <summary>
        /// Erases the flash contents of the device.
        /// This erases the flash memory of the target device.  If this method fails, the device may be left in an inoperable state.
        /// This has to be in a try-catch, as the device may not be in a state where it can halt, but we still want to try and erase.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>Number of bytes erased</returns>
        public UInt32 Erase(ref TASKMessage msg)
        {
            int res = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            try
            {
                if (!JTAG.JLINKARM_IsHalted())
                    JTAG.JLINKARM_Halt();
            }
            catch { }
            res = JTAG.JLINK_EraseChip();
            if (res < 0)
                ret = ElementDefine.IDS_ERR_DEM_ERASE;

            return ret;
        }

        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Read(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Write(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 BitOperation(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region Read flash and compare with image
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        ret = MainBlockWrite(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
                        ret = MainBlockRead(ref msg);
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_COMPARE:
                    {
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_ERASE:
                    {
                        break;
                    }
                #endregion
                default:
                    break;
            }
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            int res = 0;
            string tmp = string.Empty;
            UInt32 hi, mid, low, ddata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (JTAG.JLINKARM_IsOpen())
                deviceinfor.status = 0;
            else
            {
                deviceinfor.status = 1;
                return ElementDefine.IDS_ERR_DEM_DEVICE_CLOSED;
            }

            res = JTAG.JLINKARM_EMU_GetList((int)ElementDefine.COBRA_JLINK_HOST.IP, 0, 0);
            ddata = JTAG.JLINKARM_GetDLLVersion();
            hi = (UInt32)(ddata / 10000);
            mid = (UInt32)((ddata / 100) - hi * 100);
            low = (UInt32)(ddata - hi * 10000 - mid * 100);
            deviceinfor.shwversion = String.Format("{0}.{1}.{2}", hi, mid, low);

            ddata = JTAG.JLINKARM_GetHardwareVersion();
            hi = (UInt32)(ddata / 10000);
            mid = (UInt32)((ddata / 1000) - hi * 10);
            deviceinfor.ateversion = string.Format("{0}.{1}", hi, mid);

            ddata = JTAG.JLINKARM_GetId();
            deviceinfor.fwversion = string.Format("0x{0:x4}", ddata);

            /* ElementDefine.JLinkSpeedInfo info = new ElementDefine.JLinkSpeedInfo();
             JTAG.JLINKARM_GetSpeedInfo(ref info);*/
            return ret;
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        public UInt32 ReadDevice(ref TASKMessage msg)
        {
            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = 0;// byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
            string[] scmd = json["command"].Split(' ');
            byte[] bcmd = new byte[scmd.Length];
            for (int i = 0; i < bcmd.Length; i++)
                bcmd[i] = byte.Parse(scmd[i], System.Globalization.NumberStyles.HexNumber);

            UInt16 wDataInLength = UInt16.Parse(json["length"]);
            if (string.Compare(json["crc"].ToLower(), "none") != 0)
                wDataInLength++;

            byte[] yDataOut = new byte[wDataInLength];
            byte[] yDataIn = new byte[bcmd.Length + 1];
            yDataIn[0] = baddr;
            Array.Copy(bcmd, 0, yDataIn, 1, bcmd.Length);

            UInt16 wDataInWrite = (UInt16)bcmd.Length;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            UInt32 addr = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bcmd[3], bcmd[2]), SharedFormula.MAKEWORD(bcmd[1], bcmd[0]));
            JTAG.JLINKARM_ReadMem(addr, wDataInLength, yDataOut);

            msg.flashData = new byte[wDataInLength];
            Array.Copy(yDataOut, 0, msg.flashData, 0, wDataInLength);

            return ret;
        }

        public UInt32 WriteDevice(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            string[] scmd = json["command"].Split(' ');
            byte[] bcmd = new byte[scmd.Length];
            for (int i = 0; i < bcmd.Length; i++)
                bcmd[i] = byte.Parse(scmd[i], System.Globalization.NumberStyles.HexNumber);

            string[] sdata = json["data"].Trim().Split(' ');
            byte[] bdata = new byte[sdata.Length];
            for (int i = 0; i < bdata.Length; i++)
                bdata[i] = byte.Parse(sdata[i], System.Globalization.NumberStyles.HexNumber);
            UInt16 wDataInLength = (UInt16)sdata.Length;

            UInt32 addr = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(bcmd[3], bcmd[2]), SharedFormula.MAKEWORD(bcmd[1], bcmd[0]));
            JTAG.JLINKARM_WriteMem(addr, wDataInLength, bdata);
            return ret;
        }
        #endregion

        #region 复合功能
        protected UInt32 MainBlockRead(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            lock (m_lock)
            {

            }
            return ret;
        }

        protected UInt32 MainBlockWrite(ref TASKMessage msg)
        {
            int num = 0;
            bool ret = false;
            UInt32 wdata = 0;

            for (int nk = 0; nk < (msg.flashData.Length / 1024); nk++)
            {
                msg.controlmsg.message = "Check RAM!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                ret = CheckRAMStatus();

                msg.controlmsg.message = "Halt cpu!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                JTAG.JLINKARM_Halt();

                msg.controlmsg.message = "Set CMD to RAM 0x20001BFC!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                Write2Word(0x20001BFC, 0xA55A5AA5);

                msg.controlmsg.message = "Set BASE address to RAM 0x20001BF0!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                Write2Word(0x20001BF0, 0x10000);

                msg.controlmsg.message = "Set ADDR to RAM 0x20001BF4!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                Write2Word(0x20001BF4, (UInt32)(0x10000 + nk * 1024));

                msg.controlmsg.message = "Set LEN to RAM 0x20001BF8!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                Write2Word(0x20001BF8, 1024);

                msg.controlmsg.message = string.Format("Write {0}k to data flash!!", nk);
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                num = 0;
                for (UInt32 i = 0x20001C00; i < 0x20002000; i = (UInt32)(0x20001C00 + num * 4))
                {
                    wdata = SharedFormula.MAKEDWORD(SharedFormula.MAKEWORD(msg.flashData[nk * 1024 + num * 4], msg.flashData[nk * 1024 + 1 + num * 4]),
                        SharedFormula.MAKEWORD(msg.flashData[nk * 1024 + 2 + num * 4], msg.flashData[nk * 1024 + 3 + num * 4]));
                    Write2Word(i, wdata);
                    num++;
                }

                msg.controlmsg.message = "Run CPU!!";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                JTAG.JLINKARM_Go();
                Thread.Sleep(200);
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private bool CheckRAMStatus()
        {
            uint wdata = 0;
            bool ret = false;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                Read2Word(0x20001BFC, ref wdata);
                if (wdata == 0)
                {
                    ret = true;
                    break;
                }
                Thread.Sleep(10);
            }
            return ret;
        }

        private UInt32 FileDownload(string path, int addr)
        {
            int res = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            try
            {
                if (!JTAG.JLINKARM_IsHalted())
                    JTAG.JLINKARM_Halt();
            }
            catch { }
            res = JTAG.JLINK_DownloadFile(path, addr);
            if (res < 0)
                ret = ElementDefine.IDS_ERR_DEM_ERASE;

            return ret;
        }

        /// <summary>
        /// This method resets the target, and by default toggles the RESET and TRST pins.
        /// </summary>
        /// <param name="ms">Amount of milliseconds to delay after reset (default: 0)</param>
        /// <param name="halt">if the CPU should halt after reset (default: True)</param>
        /// <returns>Number of bytes read.</returns>
        private UInt32 Reset(int ms = 0, bool halt = true)
        {
            int res = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            JTAG.JLINKARM_SetResetDelay(ms);

            res = JTAG.JLINKARM_Reset();
            if (res < 0)
                return ElementDefine.IDS_ERR_DEM_DEVICE_RESET;
            else if (!halt)
                JTAG.JLINKARM_Go();
            return ret;
        }
        #endregion
    }
}