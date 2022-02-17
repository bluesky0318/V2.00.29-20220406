using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.Lotus
{
	internal class DefineLotus
	{
		internal const bool bFranTestLotus = false;
		internal const int RetryCount = 5;
		internal const UInt16 RegisterSize = 0x100;
		internal const byte PARAM_HEX_ERROR = 0xFF;
		internal const Double PARAM_PHYSICAL_ERROR = -999999;
		internal const UInt32 ElementMask = 0xFFFF0000;
		internal const UInt32 OperationElement = 0x00030000;
		internal const UInt16 SubSectionLotus = 0;

		internal const byte I2CRegRevision = 0xFF;
		internal const byte I2CTestModeReg = 0x10;
		internal const byte I2CTestCmdReg = 0x11;

		internal static List<byte> lstTestModeValue = new List<byte>();

		internal enum COBRA_PARAM_SUBTYPE : ushort
		{
			PARAM_DEFAULT = 0,									//display hex value directly; ps. all SVID are below to this subtype and parts of I2C register
			PARAM_VOLTAGE = 1,									//voltage
			PARAM_CURRENT,										//current
			PARAM_TEMPERATURE,								//temperature
			PARAM_TELEMETRY,									//Regx06 Telemetry reading value depends on Regx05 setting
			PARAM_VIDSELECTION,								//VID 256 kinds of selection
		}

        public const UInt32 IDS_ERR_DEMSUNCH_PARAMETER_XML = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x01;
        public const UInt32 IDS_ERR_DEMSUNCH_PARAMCONTAINT_EMPTY = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x02;
        public const UInt32 IDS_ERR_DEMSUNCH_SUBTYPE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x03;
        public const UInt32 IDS_ERR_DEMSUNCH_LOCATIONLIST = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x04;
        public const UInt32 IDS_ERR_DEMSUNCH_ELEMENT_GUID = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x05;
        public const UInt32 IDS_ERR_DEMSUNCH_VR_ADDRESS = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x06;
	}
}
