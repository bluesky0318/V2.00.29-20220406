using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.MPT
{
    public class Mcp23017
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMBehaviorManage m_parent;
        public DEMBehaviorManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private byte m_BusAddress = 0x42;
        public byte busaddress
        {
            get { return m_BusAddress; }
            set { m_BusAddress = value; }
        }

        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        #endregion

        #region 硬件模式下相关参数数据初始化
        public void Init(object pParent)
        {
            parent = (DEMBehaviorManage)pParent;
            InitialImgReg();
        }

        //操作寄存器初始化
        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }
        #endregion

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        public UInt32 ReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (parent.m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        public UInt32 ReadByte(byte reg, byte[] pval, UInt16 wDataInLength = 1)
        {
            byte[] bval = new byte[wDataInLength];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            lock (parent.m_lock)
            {
                ret = OnReadByte(reg, bval, wDataInLength);
            }
            return ret;
        }

        public UInt32 WriteByte(byte reg, byte val)
        {
            UInt32 ret = 0;
            lock (parent.m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        protected UInt32 OnReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = busaddress;
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                {
                    pval = receivebuf[0];
                    break;
                }
                Thread.Sleep(10);
            }

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnReadByte(byte reg, byte[] pval, UInt16 wDataInLength = 1)
        {
            UInt16 DataOutLen = wDataInLength;
            byte[] sendbuf = new byte[wDataInLength];
            byte[] receivebuf = new byte[wDataInLength];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = busaddress;
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, wDataInLength))
                {
                    receivebuf.CopyTo(pval, 0);
                    break;
                }
                Thread.Sleep(10);
            }

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            sendbuf[0] = busaddress;
            sendbuf[1] = reg;
            sendbuf[2] = val;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 1))
                    break;
                Thread.Sleep(10);
            }

            parent.m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }
        #endregion
        #endregion

        #region 芯片功能操作
        public UInt32 Init()
        {
            byte bval =0 ;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            // read status
            for (byte i = 0; i < 0x02; i++)
                ret = WriteByte(i, 0);

            ret = WriteByte(0x13, 0x02);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            ret = ReadByte(0x13, ref bval);
            return ret;
        }
        #endregion
    }
}
