using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using Cobra.Communication;
using Cobra.Common;

namespace Cobra.FWSequoia
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
        private Dictionary<UInt32, double> dic = new Dictionary<UInt32, double>();

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
        internal UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }
        #endregion

        #region 操作寄存器子级操作
        #region CRC16-CCITT(初始值FFFF，多项式1021，异或值FFFF，异或输出，表逆序，算法逆序)
        internal ushort[] CRC16Table = 
            {
	            0x0000, 0x1189, 0x2312, 0x329B, 0x4624, 0x57AD, 0x6536, 0x74BF,
	            0x8C48, 0x9DC1, 0xAF5A, 0xBED3, 0xCA6C, 0xDBE5, 0xE97E, 0xF8F7,
	            0x1081, 0x0108, 0x3393, 0x221A, 0x56A5, 0x472C, 0x75B7, 0x643E,
	            0x9CC9, 0x8D40, 0xBFDB, 0xAE52, 0xDAED, 0xCB64, 0xF9FF, 0xE876,
	            0x2102, 0x308B, 0x0210, 0x1399, 0x6726, 0x76AF, 0x4434, 0x55BD,
	            0xAD4A, 0xBCC3, 0x8E58, 0x9FD1, 0xEB6E, 0xFAE7, 0xC87C, 0xD9F5,
	            0x3183, 0x200A, 0x1291, 0x0318, 0x77A7, 0x662E, 0x54B5, 0x453C,
	            0xBDCB, 0xAC42, 0x9ED9, 0x8F50, 0xFBEF, 0xEA66, 0xD8FD, 0xC974,
	            0x4204, 0x538D, 0x6116, 0x709F, 0x0420, 0x15A9, 0x2732, 0x36BB,
	            0xCE4C, 0xDFC5, 0xED5E, 0xFCD7, 0x8868, 0x99E1, 0xAB7A, 0xBAF3,
	            0x5285, 0x430C, 0x7197, 0x601E, 0x14A1, 0x0528, 0x37B3, 0x263A,
	            0xDECD, 0xCF44, 0xFDDF, 0xEC56, 0x98E9, 0x8960, 0xBBFB, 0xAA72,
	            0x6306, 0x728F, 0x4014, 0x519D, 0x2522, 0x34AB, 0x0630, 0x17B9,
	            0xEF4E, 0xFEC7, 0xCC5C, 0xDDD5, 0xA96A, 0xB8E3, 0x8A78, 0x9BF1,
	            0x7387, 0x620E, 0x5095, 0x411C, 0x35A3, 0x242A, 0x16B1, 0x0738,
	            0xFFCF, 0xEE46, 0xDCDD, 0xCD54, 0xB9EB, 0xA862, 0x9AF9, 0x8B70,
	            0x8408, 0x9581, 0xA71A, 0xB693, 0xC22C, 0xD3A5, 0xE13E, 0xF0B7,
	            0x0840, 0x19C9, 0x2B52, 0x3ADB, 0x4E64, 0x5FED, 0x6D76, 0x7CFF,
	            0x9489, 0x8500, 0xB79B, 0xA612, 0xD2AD, 0xC324, 0xF1BF, 0xE036,
	            0x18C1, 0x0948, 0x3BD3, 0x2A5A, 0x5EE5, 0x4F6C, 0x7DF7, 0x6C7E,
	            0xA50A, 0xB483, 0x8618, 0x9791, 0xE32E, 0xF2A7, 0xC03C, 0xD1B5,
	            0x2942, 0x38CB, 0x0A50, 0x1BD9, 0x6F66, 0x7EEF, 0x4C74, 0x5DFD,
	            0xB58B, 0xA402, 0x9699, 0x8710, 0xF3AF, 0xE226, 0xD0BD, 0xC134,
	            0x39C3, 0x284A, 0x1AD1, 0x0B58, 0x7FE7, 0x6E6E, 0x5CF5, 0x4D7C,
	            0xC60C, 0xD785, 0xE51E, 0xF497, 0x8028, 0x91A1, 0xA33A, 0xB2B3,
	            0x4A44, 0x5BCD, 0x6956, 0x78DF, 0x0C60, 0x1DE9, 0x2F72, 0x3EFB,
	            0xD68D, 0xC704, 0xF59F, 0xE416, 0x90A9, 0x8120, 0xB3BB, 0xA232,
	            0x5AC5, 0x4B4C, 0x79D7, 0x685E, 0x1CE1, 0x0D68, 0x3FF3, 0x2E7A,
	            0xE70E, 0xF687, 0xC41C, 0xD595, 0xA12A, 0xB0A3, 0x8238, 0x93B1,
	            0x6B46, 0x7ACF, 0x4854, 0x59DD, 0x2D62, 0x3CEB, 0x0E70, 0x1FF9,
	            0xF78F, 0xE606, 0xD49D, 0xC514, 0xB1AB, 0xA022, 0x92B9, 0x8330,
	            0x7BC7, 0x6A4E, 0x58D5, 0x495C, 0x3DE3, 0x2C6A, 0x1EF1, 0x0F78,
               };

        internal int CRC16_CCITT(Byte[] data, int data_Len)
        {
            int crc = 0xffff;
            int i = 0;
            while ((data_Len--) > 0)
            {
                //data[i] = Convert.ToUInt16(data[i]);
                crc = (crc >> 8) ^ CRC16Table[(crc ^ data[i]) & 0xff];
                i++;
            }
            crc ^= 0xFFFF;
            return crc;
        }
        #endregion

        protected byte crc8_calc(ref byte[] pdata, UInt16 n)
        {
            byte crc = 0;
            byte crcdata;
            UInt16 i, j;

            for (i = 0; i < n; i++)
            {
                crcdata = pdata[i];
                for (j = 0x80; j != 0; j >>= 1)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc <<= 1;
                        crc ^= 0x07;
                    }
                    else
                        crc <<= 1;

                    if ((crcdata & j) != 0)
                        crc ^= 0x07;
                }
            }
            return crc;
        }

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, byte[] data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = data[0];
            pdata[4] = data[1];

            return crc8_calc(ref pdata, 5);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, byte data0, byte data1)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data0;
            pdata[3] = data1;

            return crc8_calc(ref pdata, 4);
        }

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    if (receivebuf[2] != calc_crc_read(sendbuf[0], sendbuf[1], receivebuf))
                    {
                        return LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    pval = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    break;
                }
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }
            return ret;
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = SharedFormula.HiByte(val);
            sendbuf[3] = SharedFormula.LoByte(val);

            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2], sendbuf[3]);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                    break;
                ret = LibErrorCode.IDS_ERR_I2C_BB_TIMEOUT;
                Thread.Sleep(10);
            }

            return ret;
        }
        #endregion
        #endregion

        #region 基础服务功能设计
        public UInt32 BlockErase(ref TASKMessage msg)
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
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)  //Bigsur
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

        public UInt32 ReadDevice(ref TASKMessage msg)
        {
            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
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

            UInt16 wDataOutLength = 0;
            UInt16 wDataInWrite = (UInt16)bcmd.Length;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (!m_Interface.ReadDevice(yDataIn, ref yDataOut, ref wDataOutLength, wDataInLength, wDataInWrite))
                ret = LibErrorCode.IDS_ERR_COM_COM_TIMEOUT;
            else
            {
                msg.flashData = new byte[wDataOutLength];
                Array.Copy(yDataOut, 0, msg.flashData, 0, wDataOutLength);
            }
            return ret;
        }

        public UInt32 WriteDevice(ref TASKMessage msg)
        {
            UInt16 wDataOutLength = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            var json = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
            byte baddr = byte.Parse(json["address"], System.Globalization.NumberStyles.HexNumber);
            string[] scmd = json["command"].Trim().Split(' ');
            string[] sdata = json["data"].Trim().Split(' ');

            UInt16 wDataInLength = (UInt16)(scmd.Length + 1 + sdata.Length);
            if (string.Compare(json["crc"].ToLower(), "none") != 0)
                wDataInLength++;

            byte[] yDataOut = new byte[1];
            byte[] yDataIn = new byte[wDataInLength];
            yDataIn[0] = (byte)baddr;//(byte)ElementDefine.COBRA_CMD.CMD_WRTIE;
            for (int n = 0; n < scmd.Length; n++)
                yDataIn[n + 1] = byte.Parse(scmd[n], System.Globalization.NumberStyles.HexNumber);

            for (int n = 0; n < sdata.Length; n++)
                yDataIn[n + 1 + scmd.Length] = byte.Parse(sdata[n], System.Globalization.NumberStyles.HexNumber);
            switch (json["crc"].ToLower())
            {
                case "crc8":
                    yDataIn[wDataInLength - 1] = crc8_calc(ref yDataIn, (UInt16)(wDataInLength - 1));
                    break;
                case "crc4":
                    break;
            }

            if (!m_Interface.WriteDevice(yDataIn, ref yDataOut, ref wDataOutLength, (UInt16)(wDataInLength - 2)))
                ret = LibErrorCode.IDS_ERR_COM_COM_TIMEOUT;
            return ret;
        }
        #endregion
    }
}