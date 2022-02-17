using System;
using System.Threading;
using System.Text;
using Cobra.Common;
using Cobra.Communication;

namespace Cobra.FWSeaguIIPD.Behavior
{
    class UartBehavior : DEMBehaviorManage
    {
        private byte[] GoodCRCBuf = new byte[7];
        protected override UInt32 OnBlockRead(byte cmd, ref byte[] buffer)
        {
            byte[] sendbuf = null;
            byte[] receivebuf = null;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            BuildSendBuf(cmd, ref sendbuf, ref receivebuf);
            UInt16 DataOutLen = (UInt16)receivebuf.Length;
            UInt16 DataInLen = (UInt16)sendbuf.Length; //cmd,len,data,crc
            for (int i = 0; i < ElementDefine.RETRY_COUNT; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen, 1))
                {
                    ret = ParseRecBuf(cmd, receivebuf, ref buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        DataOutLen = (UInt16)receivebuf.Length;
                        continue;
                    }
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            return ret;
        }

        protected override UInt32 OnBlockWrite(byte cmd, ref byte[] buffer)
        {
            byte[] sendbuf = null;
            byte[] receivebuf = null;
            UInt32 ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            BuildSendBuf(cmd, ref sendbuf, ref receivebuf, buffer);
            UInt16 DataOutLen = (UInt16)receivebuf.Length;
            UInt16 DataInLen = (UInt16)sendbuf.Length; //cmd,len,data,crc
            for (int i = 0; i < ElementDefine.RETRY_COUNT; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, DataInLen, 1))
                {
                    ret = ParseRecBuf(cmd, receivebuf, ref buffer);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        DataOutLen = (UInt16)receivebuf.Length;
                        continue;
                    }
                    break;
                }
                Thread.Sleep(10);
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
            }
            return ret;
        }

        private UInt32 GoodCRC(byte cmd)
        {
            byte[] bval = new byte[3] { cmd, 01, 00 };
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (GoodCRCBuf[3] != LibErrorCode.IDS_ERR_SUCCESSFUL) return GoodCRCBuf[3];
            if (GoodCRCBuf[0] != 0xA2) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
            if (GoodCRCBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
            if (GoodCRCBuf[2] != 1) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
            if (GoodCRCBuf[4] != crc8_calc(ref bval, (UInt16)bval.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
            if ((GoodCRCBuf[5] != 0x0d) | (GoodCRCBuf[6] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
            return ret;
        }

        private void BuildSendBuf(byte cmd, ref byte[] sendBuf, ref byte[] receiveBuf, byte[] buffer = null)
        {
            byte[] crcbuf = null;
            int dataLen = (buffer == null) ? 0 : buffer.Length;
            crcbuf = new byte[2 + dataLen];
            sendBuf = new byte[6 + dataLen];
            sendBuf[0] = 0x3A;
            sendBuf[1] = cmd;
            sendBuf[2] = (byte)dataLen;
            if (buffer != null)
                Array.Copy(buffer, 0, sendBuf, 3, buffer.Length);
            Array.Copy(sendBuf, 1, crcbuf, 0, crcbuf.Length);
            sendBuf[3 + dataLen] = crc8_calc(ref crcbuf, (UInt16)crcbuf.Length);
            sendBuf[4 + dataLen] = 0x0D;
            sendBuf[5 + dataLen] = 0x0A;
            switch (cmd)
            {
                case 0xF0:
                    receiveBuf = new byte[GoodCRCBuf.Length + 9];
                    break;
                case 0xF1:
                    receiveBuf = new byte[GoodCRCBuf.Length + 8];
                    break;
                case 0xF2:
                    receiveBuf = new byte[GoodCRCBuf.Length + 11];
                    break;
                case 0xF3:
                case 0xF5:
                case 0xF6:
                case 0xF7:
                case 0xF8:
                case 0xF9:
                case 0xFE:
                case 0xE9:
                case 0xEA:
                case 0xEB:
                    receiveBuf = new byte[GoodCRCBuf.Length + 7];
                    break;
                case 0xFC:
                    receiveBuf = new byte[GoodCRCBuf.Length];
                    break;
                case 0xF4:
                case 0xFA:
                case 0xFB:
                    receiveBuf = new byte[GoodCRCBuf.Length + 6 + 32];//128
                    break;
                case 0xEC:
                    receiveBuf = new byte[GoodCRCBuf.Length + 10];
                    break;
            }
        }

        private UInt32 ParseRecBuf(byte cmd, byte[] receivebuf, ref byte[] buffer)
        {
            byte[] crcbuf = null;
            int dataLen = (buffer == null) ? 0 : buffer.Length;
            if (receivebuf.Length < GoodCRCBuf.Length) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_TOTAL_SIZE;
            byte[] dataBuf = new byte[receivebuf.Length - GoodCRCBuf.Length];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            Array.Copy(receivebuf, 0, GoodCRCBuf, 0, GoodCRCBuf.Length);
            ret = GoodCRC(cmd);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (dataBuf.Length == 0) return LibErrorCode.IDS_ERR_SUCCESSFUL;
            Array.Copy(receivebuf, GoodCRCBuf.Length, dataBuf, 0, dataBuf.Length);
            switch (cmd)
            {
                case 0xF0:
                    crcbuf = new byte[5];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 3) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[6] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[7] != 0x0d) | (dataBuf[8] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    Array.Copy(dataBuf, 3, buffer, 0, buffer.Length);
                    break;
                case 0xF1:
                    crcbuf = new byte[4];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 2) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[3] != LibErrorCode.IDS_ERR_SUCCESSFUL) return (ElementDefine.IDS_ERR_DEM_CMD_SUCCESS + dataBuf[3]);
                    if (dataBuf[5] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[6] != 0x0d) | (dataBuf[7] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    break;
                case 0xF2:
                    crcbuf = new byte[7];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 5) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[3 + dataLen] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[4 + dataLen] != 0x0d) | (dataBuf[5 + dataLen] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    Array.Copy(dataBuf, 3, buffer, 0, buffer.Length);
                    break;
                case 0xF4:
                    crcbuf = new byte[34];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 32) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[32 + 3] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[32 + 4] != 0x0d) | (dataBuf[32 + 5] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    Array.Copy(dataBuf, 3, buffer, 0, buffer.Length);
                    break;
                case 0xF3:
                case 0xF5:
                case 0xF6:
                case 0xF7:
                case 0xF8:
                case 0xF9:
                case 0xFE:
                case 0xE9:
                case 0xEA:
                case 0xEB:
                    crcbuf = new byte[3];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 1) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[3] != LibErrorCode.IDS_ERR_SUCCESSFUL) return (ElementDefine.IDS_ERR_DEM_CMD_SUCCESS + dataBuf[3]);
                    if (dataBuf[4] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[5] != 0x0d) | (dataBuf[6] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    break;
                case 0xFA:
                case 0xFB:
                    crcbuf = new byte[32 + 2];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 32) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[32 + 3] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[32 + 4] != 0x0d) | (dataBuf[32 + 5] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    Array.Copy(dataBuf, 3, buffer, 0, buffer.Length);
                    break;
                case 0xFC:
                    crcbuf = new byte[3];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 1) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[4] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[5] != 0x0d) | (dataBuf[6] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    break;
                case 0xEC:
                    crcbuf = new byte[6];
                    Array.Copy(dataBuf, 1, crcbuf, 0, crcbuf.Length);
                    if (dataBuf[0] != 0xA3) return ElementDefine.IDS_ERR_DEM_GOODCRC_PREFIX_ERROR;
                    if (dataBuf[1] != cmd) return ElementDefine.IDS_ERR_DEM_CMD_MISMATCH;
                    if (dataBuf[2] != 4) return ElementDefine.IDS_ERR_DEM_RECEIVE_PACKAGE_DATA_SIZE;
                    if (dataBuf[7] != crc8_calc(ref crcbuf, (UInt16)crcbuf.Length)) return ElementDefine.IDS_ERR_DEM_CRC_ERROR;
                    if ((dataBuf[8] != 0x0d) | (dataBuf[9] != 0x0a)) return ElementDefine.IDS_ERR_DEM_END_ERROR;
                    Array.Copy(dataBuf, 3, buffer, 0, buffer.Length);
                    break;
            }
            return ret;
        }
    }
}
