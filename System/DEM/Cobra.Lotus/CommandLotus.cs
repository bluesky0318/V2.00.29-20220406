using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cobra.Common;

namespace Cobra.Lotus
{
	class CommandLotus
	{
		#region Private member definition

		private DEMDeviceManage m_parent;
		public DEMDeviceManage parent
		{
			get { return m_parent; }
			set { m_parent = value; }
		}

		internal LotusRegClass[] m_LotusRegImage = new LotusRegClass[DefineLotus.RegisterSize];

		#endregion

		#region Initialization

		public void Init(object pParent)
		{
			parent = (DEMDeviceManage)pParent;
			InitializeImage();

			DefineLotus.lstTestModeValue.Clear();
			DefineLotus.lstTestModeValue.Add(0x54);		//T
			DefineLotus.lstTestModeValue.Add(0x53);		//S
			DefineLotus.lstTestModeValue.Add(0x54);		//T
		}

		private void InitializeImage()
		{
			for (UInt16 i = 0; i < DefineLotus.RegisterSize; i++)
			{
				m_LotusRegImage[i] = new LotusRegClass();
				m_LotusRegImage[i].yVal = DefineLotus.PARAM_HEX_ERROR;
				m_LotusRegImage[i].dwErr = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
			}
		}

		#endregion

		#region Register Operation

		public UInt32 ReadByte(List<Parameter> lstPI2C)
		{
			Reg regTmp = null;
			byte yAddress = 0;
			byte yData = 0;
			UInt32 dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;

			foreach (Parameter p in lstPI2C)
			{
				if (p == null)
				{
					dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;
					break;
				}
				else if ((p.guid & DefineLotus.ElementMask) == DefineLotus.OperationElement)
				{
					foreach (KeyValuePair<string, Reg> dic in p.reglist)
					{
						regTmp = dic.Value;
						yAddress = (byte)regTmp.address;
						dwErr = OnReadByte(yAddress, ref yData);
						m_LotusRegImage[yAddress].dwErr = dwErr;
						m_LotusRegImage[yAddress].yVal = yData;
					}
				}
				else
				{
                    dwErr = DefineLotus.IDS_ERR_DEMSUNCH_VR_ADDRESS;		//TBD: Operation mask error in xml file
				}

				if (dwErr != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					break;		//foreach (Parameter p in lstPSVID)
				}
			}	//foreach (Parameter p in lstPI2C)

			return dwErr;
		}

		public UInt32 WriteByte(List<Parameter> lstPI2C)
		{
			Reg regTmp = null;
			byte yAddress = 0;
			UInt32 dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;

			foreach (Parameter p in lstPI2C)
			{
				if (p == null)
				{
					dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;
					break;
				}
				else if ((p.guid & DefineLotus.ElementMask) == DefineLotus.OperationElement)
				{
					foreach (KeyValuePair<string, Reg> dic in p.reglist)
					{
						regTmp = dic.Value;
						yAddress = (byte)regTmp.address;
						dwErr = OnWriteByte(yAddress, m_LotusRegImage[yAddress].yVal);
						m_LotusRegImage[yAddress].dwErr = dwErr;
					}	//foreach (KeyValuePair<string, Reg> dic in p.reglist)
				}
				if (dwErr != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					break;		//foreach (Parameter p in lstPI2C)
				}
			}	//foreach (Parameter p in lstPI2C)

			return dwErr;
		}

		protected UInt32 OnReadByte(byte yInReg, ref byte yOutVal)
		{
			UInt16 wDataOutLen = 0;
			byte[] ySendBuf = new byte[2];
			byte[] yReceiveBuf = new byte[2];
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			int i = 0;
            Options opTmp = parent.m_busopLotus.GetOptionsByGuid(BusOptions.I2CAddress_GUID);

            if(opTmp == null)
            {
                return LibErrorCode.IDS_ERR_I2C_INVALID_PARAMETER;
            }
            ySendBuf[0] = (byte)(opTmp.SelectLocation.Code);
			ySendBuf[1] = yInReg;

			for (i = 0; i < DefineLotus.RetryCount; i++)
			{
				if (parent.m_Interface.ReadDevice(ySendBuf, ref yReceiveBuf, ref wDataOutLen, 1))
				{
					yOutVal = yReceiveBuf[0];
					break;
				}
				else
				{
					Thread.Sleep(10);
				}
			}

			if (i >= DefineLotus.RetryCount)
			{
				parent.m_Interface.GetLastErrorCode(ref dwRet);
			}
			else
			{
				dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			}

			return dwRet;
		}

		protected UInt32 OnWriteByte(byte yInReg, byte yInval)
		{
			UInt16 wDataOutLen = 2;
			byte[] ySendBuf = new byte[4];
			byte[] yReceiveBuf = new byte[1];
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			int i = 0;
            Options opTmp = parent.m_busopLotus.GetOptionsByGuid(BusOptions.I2CAddress_GUID);

            if (opTmp == null)
            {
                return LibErrorCode.IDS_ERR_I2C_INVALID_PARAMETER;
            }
            ySendBuf[0] = (byte)(opTmp.SelectLocation.Code);
			ySendBuf[1] = yInReg;
			ySendBuf[2] = yInval;

			//(A160225)Francis, special for Register 0x0B, cause Kevin shi said that write value should do shift right 1 bit
			if (yInReg == 0x0B)
			{
				ySendBuf[2] >>= 1;
			}
			//(E160225)

			for (i = 0; i < DefineLotus.RetryCount; i++)
			{
				if (parent.m_Interface.WriteDevice(ySendBuf, ref yReceiveBuf, ref wDataOutLen))
				{
					break;
				}
				else
				{
					Thread.Sleep(10);
				}
			}

			if (i >= DefineLotus.RetryCount)
			{
				parent.m_Interface.GetLastErrorCode(ref dwRet);
			}
			else
			{
				dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			}

			return dwRet;
		}

		#endregion

		#region Physical to Hex calculation

		public void PhysicalToHex(ref Parameter p)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			byte yData = 0;
			//double dbTmp = 0.0F;

			if (p == null) return;
			if (p.subtype == (UInt16)DefineLotus.COBRA_PARAM_SUBTYPE.PARAM_TELEMETRY)
			{
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_SUBTYPE;			//TBD: error for TELEMETRY is read only register
			}
			else if (p.subtype == (UInt16)DefineLotus.COBRA_PARAM_SUBTYPE.PARAM_VIDSELECTION)
			{
				yData = CalPhyByRefToHex((float)p.phydata, p.regref, p.phyref);
				dwRet = WriteRegImage(p, yData);
				if (dwRet != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					WriteRegImageError(p, dwRet);
				}
			}
			else if (p.subtype == (UInt16)DefineLotus.COBRA_PARAM_SUBTYPE.PARAM_DEFAULT)
			{
				yData = CalPhyByRefToHex((float)p.phydata, p.regref, p.phyref);
				dwRet = WriteRegImage(p, yData);
				if (dwRet != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					WriteRegImageError(p, dwRet);
				}
			}
			else
			{
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_SUBTYPE;			//TBD: errror subtype
				//0~3, voltage/current/temperature is not used
			}
		}

		private byte CalPhyByRefToHex(float fVal, double RegularRef, double PhysicalRef)
		{
			byte yVal = 0;
			double dbVal, dbInteger, dbFraction;

			dbVal = (double)((double)(fVal * RegularRef) / (double)PhysicalRef);
			dbInteger = Math.Truncate(dbVal);
			dbFraction = (double)(dbVal - dbInteger);
			if (dbFraction >= 0.5)
				dbInteger += 1;
			if (dbFraction <= -0.5)
				dbInteger -= 1;
			yVal = (byte)dbInteger;

			return yVal;
		}

		#endregion

		#region Hex to Physical calculation

		public void HexToPhysical(ref Parameter p)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			byte yData = 0, yRef = 0;
			double dbTmp = 1.0F;
			Parameter pmTmp = null;

			if (p == null) return;
			if (p.subtype == (UInt16)DefineLotus.COBRA_PARAM_SUBTYPE.PARAM_TELEMETRY)
			{
				//TBD: telemetry calculation depends on Regx05 selection, wait Jun forwarding calculation formula
				dwRet = ReadRegImage(p, ref yData);
				if (dwRet != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					p.phydata = DefineLotus.PARAM_PHYSICAL_ERROR;
				}
				else
				{
					p.phydata = CalHexByRefToPhy(yData, p.regref, p.phyref);
				}
			}
			//calculation formula is same as PARAM_DEFAULT
			else if (p.subtype == (UInt16)DefineLotus.COBRA_PARAM_SUBTYPE.PARAM_VIDSELECTION)
			{
				//TBD: 256 selection
				dwRet = ReadRegImage(p, ref yData);
				if (dwRet != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					p.phydata = DefineLotus.PARAM_PHYSICAL_ERROR;
				}
				else
				{
					p.phydata = CalHexByRefToPhy(yData, p.regref, p.phyref);
				}
			}
			else if (p.subtype == (UInt16)DefineLotus.COBRA_PARAM_SUBTYPE.PARAM_DEFAULT)
			{
				dwRet = ReadRegImage(p, ref yData);
				if (dwRet != LibErrorCode.IDS_ERR_SUCCESSFUL)
				{
					p.phydata = DefineLotus.PARAM_PHYSICAL_ERROR;
				}
				else
				{
					p.phydata = CalHexByRefToPhy(yData, p.regref, p.phyref);
				}
			}
			else
			{
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_SUBTYPE;			//TBD: errror subtype
				//0~3, voltage/current/temperature is not used
			}
		}

		private double CalHexByRefToPhy(byte yVal, double RegularRef, double PhysicalRef)
		{
			double dVal = 0;

			dVal = (double)((double)(yVal * PhysicalRef) / (double)RegularRef);

			return dVal;
		}

		#endregion

		#region RegImage Read/Write

		private UInt32 WriteRegImage(Parameter p, byte yVal)
		{
			byte yData = 0, mask;
			Reg regLow = null, regHi = null;
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;

			p.hexdata = yVal;
			foreach (KeyValuePair<string, Reg> dic in p.reglist)
			{
				if (dic.Key.Equals("Low"))
				{
					regLow = dic.Value;
				}
				else if (dic.Key.Equals("High"))
				{
					regHi = dic.Value;
				}
			}

			dwRet = OnReadRegImage(p, (byte)regLow.address, ref yData);
			if (regHi == null)
			{
				mask = (byte)((1 << regLow.bitsnumber) - 1);
				mask <<= regLow.startbit;
				yData &= (byte)(~mask);
				yData |= (byte)(yVal << regLow.startbit);
				dwRet = OnWriteRegImage(p, (byte)regLow.address, yData);
			}
			else
			{
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_LOCATIONLIST;			//TBD
			}

			return dwRet;
		}

		private UInt32 ReadRegImage(Parameter p, ref byte ypVal)
		{
			byte yData = 0, mask;
			Reg regLow = null, regHi = null;
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;

			foreach (KeyValuePair<string, Reg> dic in p.reglist)
			{
				if (dic.Key.Equals("Low"))
				{
					regLow = dic.Value;
				}
				else if (dic.Key.Equals("High"))
				{
					regHi = dic.Value;
				}
			}

			if (regHi == null)
			{
				dwRet = OnReadRegImage(p, (byte)regLow.address, ref yData);
				mask = (byte)((1 << regLow.bitsnumber) - 1);
				mask <<= regLow.startbit;
				yData &= mask;
				yData >>= regLow.startbit;
			}
			else
			{
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_LOCATIONLIST;		//TBD
			}
			ypVal = yData;
			p.hexdata = ypVal;

			return dwRet;
		}

		private UInt32 OnReadRegImage(Parameter pIn, byte yInReg, ref byte yOutVal)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;

			if ((pIn.guid & DefineLotus.ElementMask) == DefineLotus.OperationElement)
			{
				yOutVal = m_LotusRegImage[yInReg].yVal;
				dwRet = m_LotusRegImage[yInReg].dwErr;
			}
			else
			{
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_ELEMENT_GUID;		//TBD
			}

			return dwRet;
		}

		private UInt32 OnWriteRegImage(Parameter pIn, byte yInReg, byte yInVal)
		{
			if ((pIn.guid & DefineLotus.ElementMask) == DefineLotus.OperationElement)
			{
				m_LotusRegImage[yInReg].yVal = yInVal;
				m_LotusRegImage[yInReg].dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;
				return LibErrorCode.IDS_ERR_SUCCESSFUL;
			}
			else
			{
                return DefineLotus.IDS_ERR_DEMSUNCH_ELEMENT_GUID;				//TBD:
			}
		}

		private void WriteRegImageError(Parameter pin, UInt32 dwErr)
		{
			return;
		}

		#endregion

		#region Special Command

		public UInt32 Command(List<Parameter> lstPI2C)
		{
			UInt32 dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;
			Reg regTmp = null;
			byte yAddress = 0, yReadBack = 0;
			List<byte> lstTemp = null;
			byte yPhysical = 0;

			foreach (Parameter p in lstPI2C)	//shoud be only 1 Parameter
			{
				if (p == null)
				{
					//TBD: should not go here
					dwErr = LibErrorCode.IDS_ERR_SECTION_LOTUSREGISTER;		//it maybe empty in lstPI2C
					break;
				}
				else if ((p.guid & DefineLotus.ElementMask) == DefineLotus.OperationElement)
				{
					foreach (KeyValuePair<string, Reg> dic in p.reglist)
					{
						regTmp = dic.Value;
						yAddress = (byte)regTmp.address;
					}	//foreach (KeyValuePair<string, Reg> dic in p.reglist)
					if (yAddress == DefineLotus.I2CTestModeReg)			//==0x10
					{
						yPhysical = (byte)p.phydata;
						if (p.phydata != 0)
						{
							lstTemp = DefineLotus.lstTestModeValue;
						}
						else
						{
							lstTemp = new List<byte>();
							lstTemp.Add(0);
						}
					}
					else
					{
						//TBD: should not be OK
						lstTemp = null;
						dwErr = LibErrorCode.IDS_ERR_SECTION_LOTUSREGISTER;		//it maybe empty in lstPI2C
						break;
					}
				}	//else if ((p.guid & LotusDefine.ElementMask) == LotusDefine.OperationElement)
			}	//foreach (Parameter p in lstPI2C)

			if (lstTemp != null)		//it is command to 0x10
			{
				//if (m_LotusRegImage[yAddress].yVal == 0x00)	//if being 00 as POR
				{
					foreach (byte ytval in lstTemp)
					{
						dwErr = OnWriteByte(yAddress, ytval);
						m_LotusRegImage[yAddress].dwErr = dwErr;
						if (dwErr != LibErrorCode.IDS_ERR_SUCCESSFUL)
						{
							break;
						}
					}
					//if all are OK, 0x10 register value should be 1;
					if (dwErr == LibErrorCode.IDS_ERR_SUCCESSFUL)
					{
						//(M160218)Francis, temporarilly remove read back of Reg0x10 for confirming TestMode status
						//(M160225)Francis, according to experiment value read from chip is 0x03, instead of 0x01 if chip is in TestMode
						dwErr = OnReadByte(yAddress, ref yReadBack);
						//yReadBack = yPhysical;
						if (lstTemp[0] != 0x00)
						{
							if (yReadBack == 0x00)
							{
								dwErr = LibErrorCode.IDS_ERR_SECTION_LOTUSTESTMODE;
							}
						}
						else
						{
							if (yReadBack != 0)
							{
								dwErr = LibErrorCode.IDS_ERR_SECTION_LOTUSNORMALMODE;
							}
						}
						//(E160225)
						//(E1602108)
						//(M160218)Francis, temporarilly remove read back of Reg0x10 for confirming TestMode status
						m_LotusRegImage[yAddress].yVal = yReadBack;
						m_LotusRegImage[yAddress].dwErr = dwErr;
					}
					else
					{
						m_LotusRegImage[yAddress].yVal = 0x00;		//otherwise, set as default
						m_LotusRegImage[yAddress].dwErr = dwErr;
					}
				}
				//else
				//{
					//dwErr = OnWriteByte(yAddress, lstTemp[0]);
					//m_LotusRegImage[yAddress].dwErr = dwErr;
					//if (dwErr == LibErrorCode.IDS_ERR_SUCCESSFUL)
					//{
						//m_LotusRegImage[yAddress].yVal = lstTemp[0];
					//}
				//}
			}	//if (lstTemp != null)

			return dwErr;
		}

		public UInt32 GetDeviceInfor(ref DeviceInfor outDevice)
		{
			int ival = 0;
			string shwversion = String.Empty;
			byte yVersion = 0;
			UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

			if (DefineLotus.bFranTestLotus)
			{
				ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
			}
			else
			{
				ret = OnReadByte(DefineLotus.I2CRegRevision, ref yVersion);
				if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

				ival = (int)((yVersion & 0xF0) >> 4);
				outDevice.status = 0;
				outDevice.type = 0;
				outDevice.hwversion = ival;
				shwversion = String.Format("{0:X2}", ival);
				outDevice.shwversion = shwversion;
				outDevice.hwsubversion = (int)(yVersion & 0x0F);

				foreach (UInt16 type in outDevice.pretype)
				{
					ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
					if (SharedFormula.HiByte(type) != outDevice.type)
						ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
					if (((SharedFormula.LoByte(type) & 0xF0) >> 4) != outDevice.hwversion)
						ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;
					if ((SharedFormula.LoByte(type) & 0x0F) != outDevice.hwsubversion)
						ret = LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

					if (ret == LibErrorCode.IDS_ERR_SUCCESSFUL) break;
				}
			}	//if (LotusDefine.bFranTestLotus)

			return ret;

			//return LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD
		}

		#endregion
	}
}
