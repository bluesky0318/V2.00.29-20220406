using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using Cobra.Communication;
using Cobra.Common;
using System.Text.RegularExpressions;

namespace Cobra.Uart
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
        Byte[] sedlst = new Byte[ElementDefine.wSize];
        private object m_lock = new object();
        private Task task;
        private CancellationToken token;
        private CancellationTokenSource tokenSource = null;
        private CCommunicateManager m_Interface = new CCommunicateManager();

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();
           for (int i = 0; i < ElementDefine.wSize; i++)
                sedlst[i] = (byte)(i % 256);
             /*for (int i = 0; i < 16; i++)
                sedlst[i] = (byte)i;*/
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

        #region 基础服务功能设计
        public UInt32 Erase(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
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
            UInt16 blen = 1;
            Byte[] revlst = new Byte[1024];
            string str = string.Empty;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COBRA_SUB_TASK)msg.sub_task)
            {
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_PROGRAM:
                    {
                        if (ElementDefine.dtimes % 2 == 0)
                        {
                            tokenSource = new CancellationTokenSource();
                            token = tokenSource.Token;
                            task = new Task(() =>
                            {
                                while (true)
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                    ret = BlockWrite(sedlst, (UInt16)sedlst.Length, ref revlst, ref blen);
                                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) break;
                                    ElementDefine.stimes++;
                                }

                            }, token);
                            task.Start();
                        }
                        else
                        {
                            tokenSource.Cancel();
                            Thread.Sleep(100);
                            FolderMap.WriteFile(string.Format("Send times:{0} and bytes:{1}", ElementDefine.stimes, ElementDefine.stimes * ElementDefine.wSize));
                            ElementDefine.stimes = 0;
                        }
                        ElementDefine.dtimes++;
                    }
                    break;
                case ElementDefine.COBRA_SUB_TASK.SUB_TASK_MAIN_BLOCK_DUMP:
                    {
                        break;
                    }
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

        protected UInt32 BlockWrite(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            lock (m_lock)
            {
                ret = OnBlockWrite(cmd, iLen, ref pval, ref oLen);
            }
            return ret;
        }
        protected UInt32 OnBlockWrite(byte[] cmd, UInt16 iLen, ref byte[] pval, ref UInt16 oLen)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (!m_Interface.WriteDevice(cmd, ref pval, ref oLen, iLen))
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
            return ret;
        }
    }
}