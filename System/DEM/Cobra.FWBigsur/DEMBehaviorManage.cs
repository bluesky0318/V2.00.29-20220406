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

namespace Cobra.FWBigsur
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
            /*for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(cmd, ref pval, ref oLen, iLen))
                    break;
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;*/
            if (!m_Interface.ReadDevice(cmd, ref pval, ref oLen, iLen))
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            return ret;
        }

        protected UInt32 OnWriteWord(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            /*for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(cmd, ref pval, ref oLen, iLen))
                {
                    ret = CheckData(pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) continue;
                    break;
                }
            }
            Thread.Sleep(10);
            return ret;*/

            if (!m_Interface.WriteDevice(cmd, ref pval, ref oLen, iLen))
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
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

            msg.controlmsg.message = "Begin to erase..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;

            msg.controlmsg.message = "Enter ISP mode..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            ret = ISPMode();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = SwitchEcho(ref msg, true);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Unlock Flash Erase..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_U_23130];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = string.Format("Prepare sector(s) for erase operation from {0} to {1}", 0, 15);
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_P_0_15];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                //Thread.Sleep(1000);
                msg.controlmsg.message = string.Format("Erase sector(s) from {0} to {1}", 0, 15);
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_E_0_15];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = (byte)(sUartIsp.Length + 3);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                //Thread.Sleep(1);
            }
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
            int len = 0;
            bool bErase = false, bVerify = false;
            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            if (!bool.TryParse(json["vap_Cb"].Trim(), out bErase))   bErase = false; 
            if (!bool.TryParse(json["eaf_Cb"].Trim(), out bVerify))  bVerify = false;

            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                #region Read flash and compare with image
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        if(bErase)
                        {
                            ret = Erase(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }
                        msg.controlmsg.message = "Erase successfully...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        ret = MainBlockWrite(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        msg.controlmsg.message = "Download successfully...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        if (bVerify)
                        {
                            len = msg.flashData.Length;
                            Array.Clear(ElementDefine.interBuffer, 0, ElementDefine.interBuffer.Length);
                            Array.Copy(msg.flashData, ElementDefine.interBuffer, len);

                            msg.controlmsg.message = "Begin upload data..";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                            ret = MainBlockRead(ref msg);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;


                            msg.controlmsg.message = "Begin verify data..";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                            for (int n = 0; n < len; n++)
                            {
                                if (msg.flashData[n] == ElementDefine.interBuffer[n]) continue;
                                return LibErrorCode.IDS_ERR_INVALID_MAIN_FLASH_COMPARE_ERROR;
                            }
                        }
                        msg.controlmsg.message = "Verify successfully...";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                        break;
                    }
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
                        ret = MainBlockRead(ref msg);
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
            UInt16 blen = 1;
            Byte[] cmdlst;
            Byte[] revlst = new Byte[256];
            string[] atmp = { "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string sUartIsp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                ret = ISPMode();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                deviceinfor.status = 0;

                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }

                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_J];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = (byte)(sUartIsp.Length + 6);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                string str = System.Text.Encoding.ASCII.GetString(revlst, 0, blen);
                string[] arr = str.Split(Environment.NewLine.ToCharArray());
                Array.Copy(arr, atmp, arr.Length > atmp.Length ? atmp.Length : arr.Length);
                deviceinfor.fwversion = string.Format("{0}.{1}", atmp[2], atmp[4]);

                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_K];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = (byte)(sUartIsp.Length + 10);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                str = System.Text.Encoding.ASCII.GetString(revlst, 0, blen);
                arr = str.Split(Environment.NewLine.ToCharArray());
                //deviceinfor.ateversion = string.Format("{0}.{1}", arr[2], arr[4]);
                Array.Copy(arr, atmp, arr.Length > atmp.Length ? atmp.Length : arr.Length);
                deviceinfor.ateversion = string.Format("{0}.{1}", atmp[2], atmp[4]);

                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_N];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = (byte)(sUartIsp.Length + 15);
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                str = System.Text.Encoding.ASCII.GetString(revlst, 0, blen);
                arr = str.Split(Environment.NewLine.ToCharArray());
                Array.Copy(arr, atmp, arr.Length > atmp.Length ? atmp.Length : arr.Length);
                deviceinfor.shwversion = string.Format("{0}\n{1}\n{2}\n{3}",
                String.Format("{0:x4}", UInt64.Parse(string.IsNullOrEmpty(atmp[2]) ? "0" : atmp[2])).ToUpper(),
                String.Format("{0:x4}", UInt64.Parse(string.IsNullOrEmpty(atmp[4]) ? "0" : atmp[4])).ToUpper(),
                String.Format("{0:x4}", UInt64.Parse(string.IsNullOrEmpty(atmp[6]) ? "0" : atmp[6])).ToUpper(),
                String.Format("{0:x4}", UInt64.Parse(string.IsNullOrEmpty(atmp[8]) ? "0" : atmp[8])).ToUpper());
            }
            catch (System.Exception ex)
            {

            }
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
            UInt16 blen = 14;
            string tmp = string.Empty;
            byte[] cmdlst = new byte[64];
            byte[] reclst = new byte[256];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            cmdlst[0] = 0x3F;
            ret = ReadWord(cmdlst, 1, ref reclst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                cmdlst[0] = 0x3F;
                blen = 1;
                ret = ReadWord(cmdlst, 1, ref reclst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }

            tmp = System.Text.Encoding.Default.GetString(reclst, 0, 14);
            if (string.Compare(tmp, ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_SYN_ASCII]) == 0)
            {
                blen = 18;
                cmdlst = System.Text.Encoding.Default.GetBytes(tmp);
                ret = ReadWord(cmdlst, 14, ref reclst, ref blen);//Synchronized 0d 0a OK 0d 0a = 18bytes
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                blen = 7;
                cmdlst = System.Text.Encoding.Default.GetBytes(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_FREQ_12000]);
                ret = ReadWord(cmdlst, 7, ref reclst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                blen = 9;
                cmdlst = System.Text.Encoding.Default.GetBytes(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_READ_CID]);
                ret = ReadWord(cmdlst, (ushort)cmdlst.Length, ref reclst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }
            else if (reclst[0] == 0x3F)
            {
                blen = 3;
                cmdlst = System.Text.Encoding.Default.GetBytes(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_SYN_CHAR]);
                ret = ReadWord(cmdlst, 3, ref reclst, ref blen);
            }
            else
                ret = LibErrorCode.IDS_ERR_MODE_ISP_FAIL;
            return ret;
        }

        public UInt32 SwitchEcho(ref TASKMessage msg, bool bEcho)
        {
            UInt16 blen = 1;
            Byte[] cmdlst = new Byte[256];
            Byte[] revlst = new Byte[515];
            string sUartIsp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (bEcho)
            {
                msg.controlmsg.message = "Turns echo on..";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
            else
            {
                msg.controlmsg.message = "Turns echo off..";
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = 3;
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    blen = 3; //Delay wait time
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
            }
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

        public UInt32 MainBlockWrite(ref TASKMessage msg)
        {
            UInt16 blen = 1;
            UInt16 ulen = 0;
            Byte[] cmdlst;
            string sUartIsp = string.Empty;
            //UInt16 wPage = ElementDefine.RAM_PAGE_SIZE;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            UInt16 data_length = ElementDefine.RAM_DOWNLOAD_BYTES;

            Byte[] sedlst = new Byte[1024];
            Byte[] revlst = new Byte[1024];

            msg.controlmsg.message = "Enter ISP mode..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            ret = ISPMode();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Turns echo on..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3); //Delay wait time
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            /*if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            }*/
            /*
            ret = SwitchEcho(ref msg, true);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

            msg.controlmsg.message = "Unlock Flash Write..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_U_23130];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = string.Format("Prepare sector(s) for write operation from {0} to {1}", 15, 15);
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_P_15_15];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = string.Format("Erase sector(s) from {0} to {1}", 15, 15);
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_E_15_15];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Unlock Flash Write..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_U_23130];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Turns echo off..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            /*ret = SwitchEcho(ref msg, false);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

            for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.RAM_PAGE_SIZE) //必须从上传下来的是1024的整数倍，多余的填写0xFF
            {
                ulen = (UInt16)((fAddr / ElementDefine.RAM_PAGE_SIZE) % 2);
                #region 写1K Ram
                //w 0x10000300 512 w 0x10000500 512
                msg.controlmsg.message = string.Format("Writes {0} bytes of data to address 0x{1:x4}...", ElementDefine.RAM_PAGE_SIZE
                    , (ElementDefine.RAM_START_ADD + ulen * ElementDefine.RAM_PAGE_SIZE));
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                sUartIsp = string.Format(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_W_X_Y],
                    (ElementDefine.RAM_START_ADD + ulen * ElementDefine.RAM_PAGE_SIZE), ElementDefine.RAM_PAGE_SIZE);
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = 3;
                ret = ReadWord(cmdlst, (byte)cmdlst.Length, ref revlst, ref blen);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                for (ushort address = 0; address < ElementDefine.RAM_PAGE_SIZE; address += data_length)
                {
                    if ((address + data_length) > ElementDefine.RAM_PAGE_SIZE)
                        data_length = (ushort)(ElementDefine.RAM_PAGE_SIZE - address);
                    else
                        data_length = ElementDefine.RAM_DOWNLOAD_BYTES;

                    for (ushort i = 0; i < data_length; i++)
                        sedlst[i] = msg.flashData[address + fAddr + i];

                    blen = 0;
                    ret = WriteWord(sedlst, data_length, ref revlst, ref blen);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                }
                #endregion
                Thread.Sleep(20);
                #region 复制1k数据从Ram到Flash
                if (ulen != 0)
                {
                    msg.controlmsg.message = "Turns echo on..";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                    sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
                    cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                    blen = 3;
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    /*if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = SwitchEcho(ref msg, true);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/

                    msg.controlmsg.message = string.Format("Prepare sector(s) for write operation from {0} to {1}", 0, 15);
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                    sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_P_0_15];
                    cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                    blen = (byte)(sUartIsp.Length + 3);
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    msg.controlmsg.message = string.Format("copies {0} bytes from the RAM address 0x{1:x4} to the flash address {2}",
                        ((UInt16)(fAddr / ElementDefine.RAM_MAX_SIZE)) * ElementDefine.RAM_MAX_SIZE,
                        ElementDefine.RAM_START_ADD, ElementDefine.RAM_MAX_SIZE);
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                    //C i*1024 0x10000300 1024
                    sUartIsp = string.Format(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_C_X_Y_Z], (((UInt16)(fAddr / ElementDefine.RAM_MAX_SIZE)) * ElementDefine.RAM_MAX_SIZE + 0x10000), ElementDefine.RAM_START_ADD, ElementDefine.RAM_MAX_SIZE);
                    cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                    blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    msg.controlmsg.message = "Turns echo off..";
                    msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                    sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
                    cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                    blen = (byte)(sUartIsp.Length + 3); //Delay wait time
                    ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                    /*if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = SwitchEcho(ref msg, false);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/
                }
                #endregion
            }
            Thread.Sleep(20);
            return ret;
        }

        public UInt32 MainBlockRead(ref TASKMessage msg)
        {
            UInt16 blen = 1;
            Byte[] cmdlst = new Byte[256];
            Byte[] revlst = new Byte[515];
            string sUartIsp = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            msg.controlmsg.message = "Enter ISP mode..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            ret = ISPMode();
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            msg.controlmsg.message = "Turns echo off..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_0];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            blen = (byte)(sUartIsp.Length + 3);
            ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            /*if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = SwitchEcho(ref msg, false);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;*/
            Thread.Sleep(100);
            for (ushort fAddr = 0; fAddr < msg.flashData.Length; fAddr += ElementDefine.RAM_UPLOAD_BYTES)
            {
                msg.controlmsg.message = string.Format("Reads {0} bytes of data from address {1}", ElementDefine.RAM_UPLOAD_BYTES, fAddr);
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
                sUartIsp = string.Format(ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_R_X_Y], fAddr, ElementDefine.RAM_UPLOAD_BYTES);
                cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
                blen = ElementDefine.RAM_UPLOAD_BYTES + 3;
                ret = ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                //blen = 0;
                //ret = WriteWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
                //Thread.Sleep(1);
                //blen = ElementDefine.RAM_UPLOAD_BYTES + 3;
                //ret = ReadWord(cmdlst, (byte)0, ref revlst, ref blen);

                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                Array.Copy(revlst, 3, msg.flashData, fAddr, ElementDefine.RAM_UPLOAD_BYTES);
            }

            msg.controlmsg.message = "Turns echo on..";
            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_DEFAULT;
            blen = 3;
            sUartIsp = ElementDefine.Uart_ISP[ElementDefine.UARTISP.UARTISP_A_1];
            cmdlst = System.Text.Encoding.Default.GetBytes(sUartIsp);
            return ReadWord(cmdlst, (byte)sUartIsp.Length, ref revlst, ref blen);
            //ret = SwitchEcho(ref msg, true);
            //return ret;
        }
        #endregion
    }
}