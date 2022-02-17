//#define DEBUG_LOG
//#define DATA_PACKAGE_LEN 32

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
using System.Text.RegularExpressions;

namespace Cobra.LPC812
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

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
        }

        #region 端口操作
        public bool CreateInterface()
        {
            bool bdevice = EnumerateInterface();
            if (!bdevice) return false;

            return m_Interface.OpenDevice(ref parent.m_busoption);
        }

        public bool DestroyInterface()
        {
            return m_Interface.CloseDevice();
        }

        public bool EnumerateInterface()
        {
            return m_Interface.FindDevices(ref parent.m_busoption);
        }
        #endregion

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        protected UInt32 ReadWord(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnReadWord(cmd, iLen, ref pval, ref oLen);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnWriteWord(cmd, iLen, ref pval, ref oLen);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        private UInt32 OnReadWord(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(cmd, ref pval, ref oLen, iLen))
                    break;
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWriteWord(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(cmd, ref pval, ref oLen, iLen))
                {
                    ret = CheckData(pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    break;
                }
            }
            Thread.Sleep(10);
            return ret;

        }
        #endregion
        #endregion

        #region YFLASH寄存器操作
        #region YFLASH寄存器父级操作
        protected UInt32 BlockErase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {

            }
            return ret;
        }

        protected UInt32 BlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (m_lock)
            {

            }

            return ret;
        }
        #endregion

        #region YFLASH寄存器子级操作
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 Erase(ref TASKMessage msg)
        {
            UInt16 blen = 1;
            Byte[] cmdlst = new Byte[64];
            Byte[] revlst = new Byte[64];
            string sUartIsp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ISPMode();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_U_23130];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_P_0_15];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_E_0_15];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
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
            msg.controlmsg.message = "Begin to write image.";
            return Write_Flash(ref msg, ElementDefine.RAM_MAX_CAP);
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
            Parameter param = null;
            List<Parameter> parameterList = new List<Parameter>();
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                switch (p.guid & ElementDefine.ElementMask)
                {
                    case ElementDefine.ProParaElement:
                        {
                            if (p == null) break;
                            parameterList.Add(p);
                            break;
                        }
                }
            }

            if (parameterList.Count != 0)
            {
                for (int i = 0; i < parameterList.Count; i++)
                {
                    param = (Parameter)parameterList[i];
                    if (param == null) continue;

                    m_parent.Physical2Hex(ref param);
                }
                for (int i = 0; i < m_parent.m_ProjParamImg.Length; i++)
                {
                    if (m_parent.m_ProjParamImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    msg.flashData[ElementDefine.ParameterAreaStart + i] = (byte)m_parent.m_ProjParamImg[i].val;
                }
            }

            return ret;
        }

        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region Read flash and compare with image
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_COMPARE:
                    {
                        Byte[] data = new Byte[0x4000];
                        for (ret = 0; ret < 0x4000; ret++)
                            data[ret] = 0x00;
                        msg.percent = 95;
                        msg.controlmsg.message = "Prepare read flash";
                        ret = Read_Flash(ref msg, 0x4000, data);

                        for (int i = 0; i < 0x4000; i++)
                        {
                            if (data[i] != msg.flashData[i])
                                ret = LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                        }
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
        #endregion

        #region 系统功能
        public UInt32 ISPMode()
        {
            UInt16 blen = 1;
            string tmp = string.Empty;
            byte[] cmdlst = new byte[64];
            byte[] reclst = new byte[256];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            cmdlst[0] = 0x3F;
            ret = ReadWord(cmdlst, 1, ref reclst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            tmp = System.Text.Encoding.Default.GetString(reclst, 0, 14);
            if (string.Compare(tmp, ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_SYN_ASCII]) == 0)
            {
                blen = 1;
                cmdlst = System.Text.Encoding.Default.GetBytes(tmp);
                ret = ReadWord(cmdlst, 14, ref reclst, ref blen);

                cmdlst = System.Text.Encoding.Default.GetBytes(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_FREQ_12000]);
                ret = ReadWord(cmdlst, 7, ref reclst, ref blen);

                cmdlst = System.Text.Encoding.Default.GetBytes(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_READ_CID]);
                ret = ReadWord(cmdlst, (ushort)cmdlst.Length, ref reclst, ref blen);
            }
            else if (reclst[0] == 0x3F)
            {
                blen = 1;
                cmdlst = System.Text.Encoding.Default.GetBytes(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_SYN_CHAR]);
                ret = ReadWord(cmdlst, 3, ref reclst, ref blen);
            }
            else if (reclst[0] == 0x30)
                ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            else
                ret = LibErrorCode.IDS_ERR_MODE_ISP_FAIL;
            return ret;
        }

        public UInt32 CheckData(byte[] data)
        {
            byte bCheckSum = 0;
            if (data[0] != 0x55) return LibErrorCode.IDS_ERR_SBSSFL_SW_FRAME_HEAD;
            if ((data[1] >= 0xF0) && (data[1] <= 0xFF))
            {
                switch (data[1])
                {
                    case 0xF0:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_FRAME_HEAD;
                    case 0xF1:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_FRAME_CHECKSUM;
                    case 0xF2:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_COMMAND_NODEF;
                    case 0xF3:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_COMMAND_EXECU;
                    case 0xF4:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_COMMAND_EXECU_TO;
                    case 0xF5:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_I2C_BLOCKED;
                    case 0xF6:
                        return LibErrorCode.IDS_ERR_SBSSFL_FW_PEC_CHECK;
                    default:
                        break;
                }
            }

            bCheckSum = (byte)(0x00 - data[1] - data[2] - data[3]);
            if (bCheckSum != data[4]) return LibErrorCode.IDS_ERR_SBSSFL_SW_PEC_CHECK;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 Write_Flash(ref TASKMessage msg, UInt16 length)
        {
            UInt16 blen = 1;
            UInt16 ulen = 0;
            Byte[] cmdlst;
            string sUartIsp = string.Empty;
            UInt16 wPage = ElementDefine.RAM_PAGE_SIZE;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt16 data_length = ElementDefine.RAM_DOWNLOAD_BYTES;

            if (length > ElementDefine.RAM_MAX_CAP) ulen = ElementDefine.RAM_MAX_CAP;
            else ulen = length;

            Byte[] sedlst = new Byte[1024];
            Byte[] revlst = new Byte[1024];

            /*ret = ISPMode();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/ //Bigsur 2017/12/21 可能会恢复

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_U_23130];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

            for (ushort fAddr = 0; fAddr < ulen; fAddr += wPage)
            {
                blen = 1;
                sUartIsp = string.Format(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_W_X_Y], ElementDefine.RAM_START_ADD, ElementDefine.RAM_PAGE_SIZE);
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                ret = ReadWord(cmdlst, (byte)cmdlst.Length, ref revlst, ref blen);

                for (ushort address = 0; address < wPage; address += data_length)
                {
                    if ((address + data_length) > wPage)
                        data_length = (ushort)(wPage - address);
                    else
                        data_length = ElementDefine.RAM_DOWNLOAD_BYTES;

                    for (ushort i = 0; i < data_length; i++)
                        sedlst[i] = msg.flashData[address + fAddr + i];

                    blen = 0;
                    ret = WriteWord(sedlst, data_length, ref revlst, ref blen);
                }

                blen = 1;
                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

                blen = 1;
                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_P_0_15];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

                blen = 1;
                sUartIsp = string.Format(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_C_X_Y_Z], fAddr, ElementDefine.RAM_START_ADD, ElementDefine.RAM_PAGE_SIZE);
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

                blen = 1;
                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

                if (ulen != 0)
                {
                    msg.gm.message = "Write Flash";
                    msg.percent = ((fAddr) * 100) / ulen;
                    msg.bgworker.ReportProgress(msg.percent, msg.gm.message);
                }
            }
            return ret;
        }

        public UInt32 Read_Flash(ref TASKMessage msg, UInt16 length, Byte[] bDatabuffer)
        {
            UInt16 blen = 1;
            Byte[] cmdlst = new Byte[256];
            Byte[] revlst = new Byte[256];
            string sUartIsp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ISPMode();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);

            for (ushort fAddr = 0; fAddr < ElementDefine.RAM_MAX_CAP; fAddr += 16)
            {
                blen = 1;
                sUartIsp = string.Format(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_R_X_Y], fAddr, 16);
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (blen == 16)
                {
                    for (blen = 0; blen < 16; blen++)
                    {
                        bDatabuffer[fAddr + blen] = revlst[blen];
                    }
                }
                else if (blen == 19)
                {
                    for (blen = 0; blen < 16; blen++)
                        bDatabuffer[fAddr + blen] = revlst[blen + 3];
                }
                else if (blen == 22)
                {
                    for (blen = 0; blen < 16; blen++)
                        bDatabuffer[fAddr + blen] = revlst[blen + 6];
                }

                msg.gm.message = "Read Flash";
                msg.percent = ((fAddr) * 100) / ElementDefine.RAM_MAX_CAP;
                msg.bgworker.ReportProgress(msg.percent, msg.gm.message);
            }

            blen = 1;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            return ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
        }
        #endregion
    }
}