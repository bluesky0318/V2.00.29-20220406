using System;
using System.Threading;
using System.Xml;
using Cobra.Common;
using Cobra.Communication;

namespace Cobra.FWSeaguIIPD.Behavior
{
    class I2CBehavior : DEMBehaviorManage
    {
        protected override UInt32 OnBlockRead(byte cmd, ref byte[] buffer)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)buffer.Length; //cmd,len,data,crc
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[DataInLen + 2]; //Length and PEC
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;
            for (int i = 0; i < ElementDefine.RETRY_COUNT; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen + 2)))
                {
                    if (receivebuf[DataInLen + 1] != calc_crc_block_read(0x40, sendbuf[1], receivebuf))// calc_crc_block_read(sendbuf[0], sendbuf[1], receivebuf))
                    {
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    Array.Copy(receivebuf, 1, buffer, 0, DataInLen);
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            return ret;
        }
        protected override UInt32 OnBlockWrite(byte cmd, ref byte[] buffer)
        {
            UInt16 DataOutLen = 0;
            UInt16 DataInLen = (UInt16)(buffer.Length + 3); //len,data, pec, address, command
            byte[] receivebuf = new byte[2];
            byte[] sendbuf = new byte[4 + buffer.Length];
            byte[] tmpbuf = new byte[4 + buffer.Length];
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = cmd;
            sendbuf[2] = (byte)buffer.Length;
            Array.Copy(buffer, 0, sendbuf, 3, buffer.Length);
            Array.Copy(sendbuf, tmpbuf, sendbuf.Length);
            tmpbuf[0] = 0x40;
            sendbuf[3 + buffer.Length] = crc8_calc(ref tmpbuf, (UInt16)(DataInLen));//crc8_calc(ref sendbuf, (UInt16)(DataInLen));
            for (int i = 0; i < ElementDefine.RETRY_COUNT; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, (UInt16)(DataInLen - 1)))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            return ret;
        }
    }
}
