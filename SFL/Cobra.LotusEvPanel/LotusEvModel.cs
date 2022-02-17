using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Cobra.Common;

namespace Cobra.LotusEvPanel
{
	//use to save data in RegList in XML file, for DCLDO group used only
	public class DCRegDesc
	{
		public DCRegDesc(UInt16 uAddress, UInt16 uBitLoc, UInt16 uRegLeng)
		{
			m_RegAddr = uAddress;
			m_RegBitLoc = uBitLoc;
			m_RegLength = uRegLeng;
		}

		private UInt16 m_RegAddr;
		public UInt16 RegAddr
		{
			get { return RegAddr; }
			set { RegAddr = value; }
		}

		private UInt16 m_RegBitLoc;
		public UInt16 RegBitLoc
		{
			get { return m_RegBitLoc; }
			set { m_RegBitLoc = value; }
		}

		private UInt16 m_RegLength;
		public UInt16 RegLength
		{
			get { return m_RegLength; }
			set { m_RegLength = value; }
		}
	}

	public class XMLDataUnit
	{
		public string strGroup { get; set; }
		public string strDescription { get; set; }
		public Int16 iOrder { get; set; }
		public byte yEnableAddr { get; set; }
		public byte yEnableBitLoc { get; set; }
		public byte yEnableLength { get; set; }
		public byte yMarginAddr { get; set; }
		public byte yMarginBitLoc { get; set; }
		public byte yMarginLength { get; set; }
		public byte ySettingAddr { get; set; }
		public byte ySettingBitLoc { get; set; }
		public byte ySettingLength { get; set; }
		public Parameter pmrXDParent { get; set; }
		public UInt32 u32Guid { get; set; }
	}

	//used to binding to DC/LDO group
	public class DCLDOModel : INotifyPropertyChanged
	{
		private static string DCLDO1Mark = "DCLDO1";
		private static string DCLDO8Mark = "DCLDO8";

		public static byte CatagoryDCInvalid = 0xFF;
		public static byte CatagoryDC1 = 0x01;
		public static byte CatagoryDC8 = 0x08;

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public XMLDataUnit xmlParent { get; set; }		//save DCLDOXMLData node
		public UInt32 u32Guid { get; set; }	//save GUID

		private byte m_yDCCatagory;
		public byte yDCCatagory
		{
			get { return m_yDCCatagory; }
			set { m_yDCCatagory = value; OnPropertyChanged("yDCCatagory"); }
		}

		private string m_strChannelName;	//save <Description>
		public string strChannelName
		{
			get { return m_strChannelName; }
			set { m_strChannelName = value; OnPropertyChanged("strChannelName"); }
		}	//binding to UI

		public DCRegDesc dcRegEnable;
		public DCRegDesc dcRegMargin;
		public DCRegDesc dcRegSetting;

		private string m_strChlEnable;
		public string strChlEnable
		{
			get { return m_strChlEnable; }
			set { m_strChlEnable = value; OnPropertyChanged("strChlEnable"); }
		}
		private bool m_bChannelEnable;
		public bool bChannelEnable
		{
			get { return m_bChannelEnable; }
			set
			{ 
				m_bChannelEnable = value;
				if (m_bChannelEnable)
				{
					strChlEnable = "Disable";
				}
				else
				{
					strChlEnable = "Enable";
				}
				OnPropertyChanged("bChannelEnable");
			}
		}

		private string m_strChlMarEn;
		public string strChlMarEn
		{
			get { return m_strChlMarEn; }
			set { m_strChlMarEn = value; OnPropertyChanged("strChlMarEn"); }
		}
		private bool m_bMarginEnable;
		public bool bMarginEnable
		{
			get { return m_bMarginEnable; }
			set
			{
				m_bMarginEnable = value;
				if (m_bMarginEnable)
				{
					strChlMarEn = "Disable Margin";
				}
				else
				{
					strChlMarEn = "Enable  Margin";
				}
				OnPropertyChanged("bMarginEnable");
			}
		}
		private Visibility m_vsMarginButton;
		public Visibility vsMarginButton
		{
			get { return m_vsMarginButton; }
			set { m_vsMarginButton = value; OnPropertyChanged("vsMarginButton"); }
		}

		private string m_MarginPhysical;
		public string MarginPhysical
		{
			get { return m_MarginPhysical; }
			set { m_MarginPhysical = value; OnPropertyChanged("MarginPhysical"); }
		}
		private byte m_yMarginHex;
		public byte yMarginHex
		{
			get { return m_yMarginHex; }
			set 
			{
				m_yMarginHex = value;
				if (m_yMarginHex == 0)
				{
					m_MarginPhysical = "0.000V".ToString();
				}
				else
				{
					double dbtmp = (double)(m_yMarginHex - 1);
					dbtmp *= 0.01;
					dbtmp += 0.5;
					m_MarginPhysical = string.Format("{0:F3}V", dbtmp);
				}
				OnPropertyChanged("MarginPhysical");
			}
		}
		private Visibility m_vsMarginPhysical;
		public Visibility vsMarginPhysical
		{
			get { return m_vsMarginPhysical; }
			set { m_vsMarginPhysical = value; OnPropertyChanged("vsMarginPhysical"); }
		}

		private string m_strChlMarVal;
		public string strChlMarVal
		{
			get { return m_strChlMarVal; }
			set { m_strChlMarVal = value; OnPropertyChanged("strChlMarVal"); }
		}
		private bool m_bMarginValue;
		public bool bMarginValue
		{
			get { return m_bMarginValue; }
			set
			{
				m_bMarginValue = value;
				if (m_bMarginValue)
				{
					strChlMarVal = "-5% Margin";
				}
				else
				{
					strChlMarVal = "+5%  Margin";
				}
				OnPropertyChanged("bMarginValue");
			}
		}
		private Visibility m_vsMarginFixVal;
		public Visibility vsMarginFixVal
		{
			get { return m_vsMarginFixVal; }
			set { m_vsMarginFixVal = value; OnPropertyChanged("vsMarginFixVal"); }
		}
		private Visibility m_vsMarginMassVal;
		public Visibility vsMarginMassVal
		{
			get { return m_vsMarginMassVal; }
			set { m_vsMarginMassVal = value; OnPropertyChanged("vsMarginMassVal"); }
		}

		public Int16 iOrder { get; set; }
		public string strDescription { get; set; }
		public Parameter pmrFromXML { get; set; }

		public DCLDOModel()
		{
		}

		public DCLDOModel(XMLDataUnit unitIn)
		{
			if (unitIn.strGroup.IndexOf(DCLDOModel.DCLDO1Mark) != -1)
			{
				m_yDCCatagory = DCLDOModel.CatagoryDC1;
				vsMarginButton = Visibility.Visible;
				vsMarginPhysical = Visibility.Collapsed;
				vsMarginFixVal = Visibility.Visible;
				vsMarginMassVal = Visibility.Collapsed;
			}
			else if (unitIn.strGroup.IndexOf(DCLDOModel.DCLDO8Mark) != -1)
			{
				m_yDCCatagory = DCLDOModel.CatagoryDC8;
				vsMarginButton = Visibility.Collapsed;
				vsMarginPhysical = Visibility.Visible;
				vsMarginFixVal = Visibility.Collapsed;
				vsMarginMassVal = Visibility.Visible;
			}
			else
			{
				m_yDCCatagory = DCLDOModel.CatagoryDCInvalid;
			}

			m_strChannelName = unitIn.strDescription;
			iOrder = unitIn.iOrder;
			strDescription = unitIn.strDescription;
			pmrFromXML = unitIn.pmrXDParent;
			dcRegEnable = new DCRegDesc(unitIn.yEnableAddr, unitIn.yEnableBitLoc, unitIn.yEnableLength);
			dcRegMargin = new DCRegDesc(unitIn.yMarginAddr, unitIn.yMarginBitLoc, unitIn.yMarginLength);
			dcRegSetting = new DCRegDesc(unitIn.ySettingAddr, unitIn.ySettingBitLoc, unitIn.ySettingLength);
			strChlEnable = "Enable";
			strChlMarEn = "Enable  Margin";
			strChlMarVal = "+5% Margin";
//			dcRegEnable.RegAddr = unitIn.yEnableAddr;
//			dcRegEnable.RegBitLoc = unitIn.yEnableBitLoc;
//			dcRegEnable.RegLength = unitIn.yEnableLength;
//			dcRegMargin.RegAddr = unitIn.yMarginAddr;
//			dcRegMargin.RegBitLoc = unitIn.yMarginBitLoc;
//			dcRegMargin.RegLength = unitIn.yMarginLength;
//			dcRegSetting.RegAddr = unitIn.ySettingAddr;
//			dcRegSetting.RegBitLoc = unitIn.ySettingBitLoc;
//			dcRegSetting.RegLength = unitIn.ySettingLength;
		}
	}

	//(A150714)Francis, Random one function used, binding to UI
	public class DCLDO3Model : INotifyPropertyChanged
	{
		//private static string DCLDO1Mark = "DCLDO1";
		//private static string DCLDO8Mark = "DCLDO8";

		public static byte CatagoryDCInvalid = 0xFF;
		public static byte CatagoryDC1 = 0x01;
		public static byte CatagoryDC8 = 0x08;

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public XMLDataUnit xmlParent { get; set; }		//save DCLDOXMLData node
		public UInt32 u32Guid { get; set; }	//save GUID

		private byte m_yDCCatagory;
		public byte yDCCatagory
		{
			get { return m_yDCCatagory; }
			set { m_yDCCatagory = value; OnPropertyChanged("yDCCatagory"); }
		}

		private string m_strChannelName;	//save <Description>
		public string strChannelName
		{
			get { return m_strChannelName; }
			set { m_strChannelName = value; OnPropertyChanged("strChannelName"); }
		}	//binding to UI

		public DCRegDesc dcRegEnable;
		public DCRegDesc dcRegMargin;
		public DCRegDesc dcRegSetting;

		private string m_strChlEnable;
		public string strChlEnable
		{
			get { return m_strChlEnable; }
			set { m_strChlEnable = value; OnPropertyChanged("strChlEnable"); }
		}
		private bool m_bChannelEnable;
		public bool bChannelEnable
		{
			get { return m_bChannelEnable; }
			set
			{
				m_bChannelEnable = value;
				if (m_bChannelEnable)
				{
					strChlEnable = "Channel is Enable";
				}
				else
				{
					strChlEnable = "Channel is Disable";
				}
				OnPropertyChanged("bChannelEnable");
			}
		}

		private string m_strChlMarEn;
		public string strChlMarEn
		{
			get { return m_strChlMarEn; }
			set { m_strChlMarEn = value; OnPropertyChanged("strChlMarEn"); }
		}
		private bool m_bMarginEnable;
		public bool bMarginEnable
		{
			get { return m_bMarginEnable; }
			set
			{
				m_bMarginEnable = value;
				if (m_bMarginEnable)
				{
					strChlMarEn = "Margin is Enable";
				}
				else
				{
					strChlMarEn = "Margin is Disable";
				}
				OnPropertyChanged("bMarginEnable");
			}
		}
		private Visibility m_vsMarginButton;
		public Visibility vsMarginButton
		{
			get { return m_vsMarginButton; }
			set { m_vsMarginButton = value; OnPropertyChanged("vsMarginButton"); }
		}

		private string m_MarginPhysical;
		public string MarginPhysical
		{
			get { return m_MarginPhysical; }
			set { m_MarginPhysical = value; OnPropertyChanged("MarginPhysical"); }
		}
		private byte m_yMarginHex;
		public byte yMarginHex
		{
			get { return m_yMarginHex; }
			set
			{
				m_yMarginHex = value;
				if (m_yMarginHex == 0)
				{
					m_MarginPhysical = "0.000V".ToString();
				}
				else
				{
					double dbtmp = (double)(m_yMarginHex - 1);
					dbtmp *= 0.01;
					dbtmp += 0.5;
					m_MarginPhysical = string.Format("{0:F3}V", dbtmp);
				}
				OnPropertyChanged("MarginPhysical");
			}
		}
		private Visibility m_vsMarginPhysical;
		public Visibility vsMarginPhysical
		{
			get { return m_vsMarginPhysical; }
			set { m_vsMarginPhysical = value; OnPropertyChanged("vsMarginPhysical"); }
		}

		private string m_strChlMarVal;
		public string strChlMarVal
		{
			get { return m_strChlMarVal; }
			set { m_strChlMarVal = value; OnPropertyChanged("strChlMarVal"); }
		}
		private bool m_bMarginValue;
		public bool bMarginValue
		{
			get { return m_bMarginValue; }
			set
			{
				m_bMarginValue = value;
				if (m_bMarginValue)
				{
					strChlMarVal = "Margin is +5%";
				}
				else
				{
					strChlMarVal = "Margin is -5%";
				}
				OnPropertyChanged("bMarginValue");
			}
		}
		private Visibility m_vsMarginFixVal;
		public Visibility vsMarginFixVal
		{
			get { return m_vsMarginFixVal; }
			set { m_vsMarginFixVal = value; OnPropertyChanged("vsMarginFixVal"); }
		}
		private Visibility m_vsMarginMassVal;
		public Visibility vsMarginMassVal
		{
			get { return m_vsMarginMassVal; }
			set { m_vsMarginMassVal = value; OnPropertyChanged("vsMarginMassVal"); }
		}

		private string m_strADTelemetryVal;
		public string strADTelemetryVal
		{
			get { return m_strADTelemetryVal; }
			set { m_strADTelemetryVal = value; OnPropertyChanged("strADTelemetryVal"); }
		}
		private byte m_yADTelemetryVal;
		public byte yADTelemetryVal
		{
			get { return m_yADTelemetryVal; }
			set
			{
				m_yADTelemetryVal = value;
				strADTelemetryVal = string.Format("0x{0:X2}", value);
			}
		}
		private Visibility m_vsTelemetryVal;
		public Visibility vsTelemetryVal
		{
			get { return m_vsTelemetryVal; }
			set { m_vsTelemetryVal = value; OnPropertyChanged("vsTelemetryVal"); }
		}

		public Int16 iOrder { get; set; }
		public string strDescription { get; set; }
		public Parameter pmrFromXML { get; set; }

		public DCLDO3Model()
		{
		}

		/*
		public DCLDO3Model(XMLDataUnit unitIn)
		{
			if (unitIn.strGroup.IndexOf(DCLDO3Model.DCLDO1Mark) != -1)
			{
				m_yDCCatagory = DCLDO3Model.CatagoryDC1;
				vsMarginButton = Visibility.Visible;
				vsMarginPhysical = Visibility.Collapsed;
				vsMarginFixVal = Visibility.Visible;
				vsMarginMassVal = Visibility.Collapsed;
			}
			else if (unitIn.strGroup.IndexOf(DCLDO3Model.DCLDO8Mark) != -1)
			{
				m_yDCCatagory = DCLDO3Model.CatagoryDC8;
				vsMarginButton = Visibility.Collapsed;
				vsMarginPhysical = Visibility.Visible;
				vsMarginFixVal = Visibility.Collapsed;
				vsMarginMassVal = Visibility.Visible;
			}
			else
			{
				m_yDCCatagory = DCLDOModel.CatagoryDCInvalid;
			}

			m_strChannelName = unitIn.strDescription;
			iOrder = unitIn.iOrder;
			strDescription = unitIn.strDescription;
			pmrFromXML = unitIn.pmrXDParent;
			dcRegEnable = new DCRegDesc(unitIn.yEnableAddr, unitIn.yEnableBitLoc, unitIn.yEnableLength);
			dcRegMargin = new DCRegDesc(unitIn.yMarginAddr, unitIn.yMarginBitLoc, unitIn.yMarginLength);
			dcRegSetting = new DCRegDesc(unitIn.ySettingAddr, unitIn.ySettingBitLoc, unitIn.ySettingLength);
			strChlEnable = "Enable";
			strChlMarEn = "Enable  Margin";
			strChlMarVal = "+5% Margin";
//			dcRegEnable.RegAddr = unitIn.yEnableAddr;
//			dcRegEnable.RegBitLoc = unitIn.yEnableBitLoc;
//			dcRegEnable.RegLength = unitIn.yEnableLength;
//			dcRegMargin.RegAddr = unitIn.yMarginAddr;
//			dcRegMargin.RegBitLoc = unitIn.yMarginBitLoc;
//			dcRegMargin.RegLength = unitIn.yMarginLength;
//			dcRegSetting.RegAddr = unitIn.ySettingAddr;
//			dcRegSetting.RegBitLoc = unitIn.ySettingBitLoc;
//			dcRegSetting.RegLength = unitIn.ySettingLength;
		}
		*/
	}

    public class RandomLogModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

		private string m_strcurrentTime;
		public string strcurrentTime
		{
			get { return m_strcurrentTime; }
			set { m_strcurrentTime = value; OnPropertyChanged("strcurrentTime"); }
		}

        private string m_strCommand;
        public string strCommand
        {
            get { return m_strCommand; }
            set { m_strCommand = value; OnPropertyChanged("strCommand"); }
        }

        private string m_strAddress;
        public string strAddress
        {
            get { return m_strAddress; }
            set { m_strAddress = value; OnPropertyChanged("strAddress"); }
        }
        private byte m_yAddress;
        public byte yAddress
        {
            get { return m_yAddress; }
            set 
            {
                m_yAddress = value;
                strAddress = string.Format("Address={0:X2},\t", m_yAddress);
                OnPropertyChanged("yAddress");
            }
        }

        private string m_strHexValue;
        public string strHexValue
        {
            get { return m_strHexValue; }
            set { m_strHexValue = value; OnPropertyChanged("strHexValue"); }
        }
        private byte m_yHexValue;
        public byte yHexValue
        {
            get { return m_yHexValue; }
            set
            {
                m_yHexValue = value;
                strHexValue = string.Format("HexValue={0:X2},\t", m_yHexValue);
                OnPropertyChanged("yHexValue");
            }
        }

        private string m_strPhyValue;
        public string strPhyValue
        {
            get { return m_strPhyValue; }
            set { m_strPhyValue = value; OnPropertyChanged("strPhyValue"); }
        }
        private int m_iPhyValue;
        public int iPhyValue
        {
            get { return m_iPhyValue; }
            set
            {
                m_iPhyValue = value;
                strPhyValue = string.Format("PhysicalValue={0}, \t", m_iPhyValue);
                OnPropertyChanged("iPhyValue");
            }
        }

        private string m_strResult;
        public string strResult
        {
            get { return m_strResult; }
            set { m_strResult = value; OnPropertyChanged("strResult"); }
        }

        public RandomLogModel()
        {
            InitializeMembers("", "", "", "", "");
        }
        
        public RandomLogModel(string strInCmd)
        {
            InitializeMembers(strInCmd, "", "", "", "");
        }

        public RandomLogModel(string strInCmd, string strInAddr, string strInHex, string strInPhy)
        {
            InitializeMembers(strInCmd, strInAddr, strInHex, strInPhy, "");
        }

		public RandomLogModel(int iChannel, bool bMargEn, bool bMarg5, byte yMargHex)
		{
			string strTimeInfo = DateTime.Now.ToString("HH:mm:ss");
			strTimeInfo += ":" + DateTime.Now.Millisecond.ToString() + "\t";

			strcurrentTime = strTimeInfo;
			strCommand = string.Format("EnableChannel {0},\t", iChannel);
			if (bMargEn)
				strAddress = string.Format("SetMargin Enable,\t");
			else
				strAddress = string.Format("SetMargin Disable,\t");
			if (bMarg5)
				strHexValue = string.Format("Set Margin +5%,\t");
			else
				strHexValue = string.Format("Set Margin -5%,\t");
			strPhyValue = string.Format("Set MarginHex={0:X2}", yMargHex);
            strResult = "";
		}

		public RandomLogModel(int iChannel, bool bMargEn, bool bMarg5, string strMargVolt)
		{
			string strTimeInfo = DateTime.Now.ToString("HH:mm:ss");
			strTimeInfo += ":" + DateTime.Now.Millisecond.ToString() + "\t";

			strcurrentTime = strTimeInfo;
			strCommand = string.Format("EnableChannel {0},\t", iChannel);
			if (iChannel <= 4)
			{
				if (bMargEn)
					strAddress = string.Format("SetMargin Enable,\t");
				else
					strAddress = string.Format("SetMargin Disable,\t");
				if (bMarg5)
					strHexValue = string.Format("Set Margin +5%,\t");
				else
					strHexValue = string.Format("Set Margin -5%,\t");
				//strPhyValue = string.Format("Set MarginHex={0}", strMargVolt);
				strPhyValue = "";
			}
			else
			{
				//if (bMargEn)
					//strAddress = string.Format("SetMargin Enable,\t");
				//else
					//strAddress = string.Format("SetMargin Disable,\t");
				//if (bMarg5)
					//strHexValue = string.Format("Set Margin +5%,\t");
				//else
					//strHexValue = string.Format("Set Margin -5%,\t");
				strAddress = string.Format(",\t\t\t");
				strHexValue = string.Format(",\t\t\t");
				strPhyValue = string.Format("Set MarginHex={0}", strMargVolt);
			}
            strResult = "";
		}

		public RandomLogModel(string strRW, byte yAddr, byte yhex, int iphy, int iresult = 0)
		{
            string strtmp = string.Format("");

            if(iresult == -1)  //comparison result failed
            {
                strtmp = string.Format("False");
            }
            else if (iresult == 1)   //comparison result success
            {
                strtmp = string.Format("True");
            }
            else if (iresult == 0)   //no comparison
            {
                strtmp = string.Format("");
            }
            m_yAddress = yAddr;
            m_yHexValue = yhex;
            m_iPhyValue = iphy;
            InitializeMembers(string.Format("{0},\t", strRW), 
				string.Format("Address={0:X2},\t", yAddr),	
				string.Format("HexValue={0:X2},\t", yhex), 
				string.Format("PhysicalValue={0}, \t", iphy),
                strtmp);
		}

        private void InitializeMembers(string strInCmd, string strInAddr, string strInHex, string strInPhy, string strInResult)
        {
			string strTimeInfo = DateTime.Now.ToString("HH:mm:ss");
			strTimeInfo += ":" + DateTime.Now.Millisecond.ToString() + "\t";

			strcurrentTime = strTimeInfo;
            strCommand = strInCmd;
            strAddress = strInAddr;
            strHexValue = strInHex;
            strPhyValue = strInPhy;
            strResult = strInResult;
        }
    }
	//(E150714)

	//(A150714)Francis, Random one function used, save data from xml, use it to convert to DCLDO3Model
	public class XMLDataDCLDO
	{
		//public string strGroup { get; set; }
		//public string strDescription { get; set; }
		public Parameter pmrXDParent { get; set; }
		public UInt32 u32Guid { get; set; }
		public string[] strEnableXDescript = new string[8];
		public byte yEnableAddr { get; set; }
		public byte yEnableStart { get; set; }
		public byte yEnableNumber { get; set; }
	}

	//(A150915)Francis, CombinationTest function used, use to bind to UI that all register name and register value
	public class CombinationRegister : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

        //public Parameter pmrRegister { get; set; }

		private string m_strRegName;
		public string strRegName
		{
			get { return m_strRegName; }
			set { m_strRegName = value; OnPropertyChanged("strRegName"); }
		}

        private byte m_yRegAddr;
        public byte yRegAddr
        {
            get { return m_yRegAddr; }
            set { m_yRegAddr = value; strRegName = string.Format("Reg0x{0:X2}", m_yRegAddr); }

        }

		private string m_strRegValue;
		public string strRegValue
		{
			get { return m_strRegValue; }
			set { m_strRegValue = value; OnPropertyChanged("strRegValue"); }
		}
		private byte m_yRegValue;
		public byte yRegValue
		{
			get { return m_yRegValue; }
			set { m_yRegValue = value; strRegValue = string.Format("0x{0:X2}", m_yRegValue); }
		}

        public byte yReference { get; set; }
	}

    //(A150916)Francis, CombinationTest function used, use to bind to DC UI
    public class DCCombineModel : INotifyPropertyChanged
    {
        //private static string DCLDO1Mark = "DCLDO1";
        //private static string DCLDO8Mark = "DCLDO8";

        public static byte CatagoryDCInvalid = 0xFF;
        public static byte CatagoryDC1 = 0x01;
        public static byte CatagoryDC8 = 0x08;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public XMLDataUnit xmlParent { get; set; }		//save DCLDOXMLData node
        public UInt32 u32Guid { get; set; }	//save GUID

        private byte m_yDCCatagory;
        public byte yDCCatagory
        {
            get { return m_yDCCatagory; }
            set { m_yDCCatagory = value; OnPropertyChanged("yDCCatagory"); }
        }

        private string m_strChannelName;	//save <Description>
        public string strChannelName
        {
            get { return m_strChannelName; }
            set { m_strChannelName = value; OnPropertyChanged("strChannelName"); }
        }	//binding to UI

        //public DCRegDesc dcRegEnable;
        //public DCRegDesc dcRegMargin;
        //public DCRegDesc dcRegSetting;

        private string m_strChlEnable;
        public string strChlEnable
        {
            get { return m_strChlEnable; }
            set { m_strChlEnable = value; OnPropertyChanged("strChlEnable"); }
        }
        private bool m_bChannelEnable;
        public bool bChannelEnable
        {
            get { return m_bChannelEnable; }
            set
            {
                m_bChannelEnable = value;
                if (m_bChannelEnable)
                {
                    strChlEnable = "is Enable";
                }
                else
                {
                    strChlEnable = "is Disable";
                }
                OnPropertyChanged("bChannelEnable");
            }
        }

        private string m_strChlPowerGood;
        public string strChlPowerGood
        {
            get { return m_strChlPowerGood; }
            set { m_strChlPowerGood = value; OnPropertyChanged("strChlPowerGood"); }
        }
        private bool m_bChlPowerGood;
        public bool bChlPowerGood
        {
            get { return m_bChannelEnable; }
            set
            {
                m_bChlPowerGood = value;
                if(m_bChlPowerGood)
                {
                    strChlPowerGood = "PG=1";
                }
                else
                {
                    strChlPowerGood = "PG=0";
                }
                OnPropertyChanged("bChlPowerGood"); 
            }
        }

        private string m_strChlFault;
        public string strChlFault
        {
            get { return m_strChlFault; }
            set { m_strChlFault = value; OnPropertyChanged("strChlFault"); }
        }
        private bool m_bChlFault;
        public bool bChlFault
        {
            get { return m_bChannelEnable; }
            set
            {
                m_bChlFault = value;
                if (m_bChlFault)
                {
                    strChlFault = "Fault=1";
                }
                else
                {
                    strChlFault = "Fault=0";
                }
                OnPropertyChanged("bChlFault");
            }
        }

        private Visibility m_vsISChannel;
        public Visibility vsISChannel
        {
            get { return m_vsISChannel; }
            set { m_vsISChannel = value; OnPropertyChanged("vsISChannel"); }
        }

        private string m_strChlMarEn;
        public string strChlMarEn
        {
            get { return m_strChlMarEn; }
            set { m_strChlMarEn = value; OnPropertyChanged("strChlMarEn"); }
        }
        private bool m_bMarginEnable;
        public bool bMarginEnable
        {
            get { return m_bMarginEnable; }
            set
            {
                m_bMarginEnable = value;
                if (m_bMarginEnable)
                {
                    strChlMarEn = "Margin is Enable";
                }
                else
                {
                    strChlMarEn = "Margin is Disable";
                }
                OnPropertyChanged("bMarginEnable");
            }
        }
        private Visibility m_vsMarginButton;
        public Visibility vsMarginButton
        {
            get { return m_vsMarginButton; }
            set { m_vsMarginButton = value; OnPropertyChanged("vsMarginButton"); }
        }

        private string m_MarginPhysical;
        public string MarginPhysical
        {
            get { return m_MarginPhysical; }
            set { m_MarginPhysical = value; OnPropertyChanged("MarginPhysical"); }
        }
        private byte m_yMarginHex;
        public byte yMarginHex
        {
            get { return m_yMarginHex; }
            set
            {
                m_yMarginHex = value;
                if (m_yMarginHex == 0)
                {
                    m_MarginPhysical = "0.000V".ToString();
                }
                else
                {
                    double dbtmp = (double)(m_yMarginHex - 1);
                    dbtmp *= 0.01;
                    dbtmp += 0.5;
                    m_MarginPhysical = string.Format("{0:F3}V", dbtmp);
                }
                OnPropertyChanged("MarginPhysical");
            }
        }
        private Visibility m_vsMarginPhysical;
        public Visibility vsMarginPhysical
        {
            get { return m_vsMarginPhysical; }
            set { m_vsMarginPhysical = value; OnPropertyChanged("vsMarginPhysical"); }
        }

        private string m_strChlMarVal;
        public string strChlMarVal
        {
            get { return m_strChlMarVal; }
            set { m_strChlMarVal = value; OnPropertyChanged("strChlMarVal"); }
        }
        private bool m_bMarginValue;
        public bool bMarginValue
        {
            get { return m_bMarginValue; }
            set
            {
                m_bMarginValue = value;
                if (m_bMarginValue)
                {
                    strChlMarVal = "is +5%";
                }
                else
                {
                    strChlMarVal = "is -5%";
                }
                OnPropertyChanged("bMarginValue");
            }
        }
        private Visibility m_vsMarginFixVal;
        public Visibility vsMarginFixVal
        {
            get { return m_vsMarginFixVal; }
            set { m_vsMarginFixVal = value; OnPropertyChanged("vsMarginFixVal"); }
        }
        private Visibility m_vsMarginMassVal;
        public Visibility vsMarginMassVal
        {
            get { return m_vsMarginMassVal; }
            set { m_vsMarginMassVal = value; OnPropertyChanged("vsMarginMassVal"); }
        }

        private string m_strChlDDRVal;
        public string strChlDDRVal
        {
            get { return m_strChlDDRVal; }
            set { m_strChlDDRVal = value; OnPropertyChanged("strChlDDRVal"); }
        }
        private byte m_yChlDDRVal;
        public byte yChlDDRVal
        {
            get { return m_yChlDDRVal; }
            set
            {
                m_yChlDDRVal = value;
                if (m_strChannelName.IndexOf("3") != -1)
                {
                    if(m_yChlDDRVal == 0)
                    {
                        strChlDDRVal = "1.0V";
                    }
                    else
                    {
                        strChlDDRVal = "1.05V";
                    }
                }
                else if(m_strChannelName.IndexOf("4") != -1)
                {
                    if(m_yChlDDRVal == 0)
                    {
                        strChlDDRVal = "1.2V";
                    }
                    else if(m_yChlDDRVal == 1)
                    {
                        strChlDDRVal = "1.25V";
                    }
                    else if (m_yChlDDRVal == 2)
                    {
                        strChlDDRVal = "1.35V";
                    }
                    else if (m_yChlDDRVal == 3)
                    {
                        strChlDDRVal = "1.5V";
                    }
                    else if (m_yChlDDRVal == 4)
                    {
                        strChlDDRVal = "1.1V";
                    }
                }
                else if(m_strChannelName.IndexOf("OTP") != -1)
                {
                    if (m_yChlDDRVal == 0)
                    {
                        strChlDDRVal = "85°C";
                    }
                    else if(m_yChlDDRVal == 1)
                    {
                        strChlDDRVal = "100°C";
                    }
                    else if (m_yChlDDRVal == 2)
                    {
                        strChlDDRVal = "120°C";
                    }
                    else if (m_yChlDDRVal == 3)
                    {
                        strChlDDRVal = "150°C";
                    }
                }
                OnPropertyChanged("yChlDDRVal");
            }
        }
        private Visibility m_vsChlDDRVal;
        public Visibility vsChlDDRVal
        {
            get { return m_vsChlDDRVal; }
            set { m_vsChlDDRVal = value; OnPropertyChanged("vsChlDDRVal"); }
        }

        private string m_strADTelemetryVal;
        public string strADTelemetryVal
        {
            get { return m_strADTelemetryVal; }
            set { m_strADTelemetryVal = value; OnPropertyChanged("strADTelemetryVal"); }
        }
        private byte m_yADTelemetryVal;
        public byte yADTelemetryVal
        {
            get { return m_yADTelemetryVal; }
            set
            {
                m_yADTelemetryVal = value;
                strADTelemetryVal = string.Format("0x{0:X2}", value);
            }
        }
        private Visibility m_vsTelemetryVal;
        public Visibility vsTelemetryVal
        {
            get { return m_vsTelemetryVal; }
            set { m_vsTelemetryVal = value; OnPropertyChanged("vsTelemetryVal"); }
        }

        public Int16 iOrder { get; set; }
        public string strDescription { get; set; }
        public Parameter pmrFromXML { get; set; }

        public byte yADTelVoltLowBound { get; set; }
        public byte yADTelVoltHighBound { get; set; }
        public byte yADTelCurrLowBound { get; set; }
        public byte yADTelCurrHighBound { get; set; }

        public DCCombineModel()
        {
        }

        /*
        public DCLDO3Model(XMLDataUnit unitIn)
        {
            if (unitIn.strGroup.IndexOf(DCLDO3Model.DCLDO1Mark) != -1)
            {
                m_yDCCatagory = DCLDO3Model.CatagoryDC1;
                vsMarginButton = Visibility.Visible;
                vsMarginPhysical = Visibility.Collapsed;
                vsMarginFixVal = Visibility.Visible;
                vsMarginMassVal = Visibility.Collapsed;
            }
            else if (unitIn.strGroup.IndexOf(DCLDO3Model.DCLDO8Mark) != -1)
            {
                m_yDCCatagory = DCLDO3Model.CatagoryDC8;
                vsMarginButton = Visibility.Collapsed;
                vsMarginPhysical = Visibility.Visible;
                vsMarginFixVal = Visibility.Collapsed;
                vsMarginMassVal = Visibility.Visible;
            }
            else
            {
                m_yDCCatagory = DCLDOModel.CatagoryDCInvalid;
            }

            m_strChannelName = unitIn.strDescription;
            iOrder = unitIn.iOrder;
            strDescription = unitIn.strDescription;
            pmrFromXML = unitIn.pmrXDParent;
            dcRegEnable = new DCRegDesc(unitIn.yEnableAddr, unitIn.yEnableBitLoc, unitIn.yEnableLength);
            dcRegMargin = new DCRegDesc(unitIn.yMarginAddr, unitIn.yMarginBitLoc, unitIn.yMarginLength);
            dcRegSetting = new DCRegDesc(unitIn.ySettingAddr, unitIn.ySettingBitLoc, unitIn.ySettingLength);
            strChlEnable = "Enable";
            strChlMarEn = "Enable  Margin";
            strChlMarVal = "+5% Margin";
//			dcRegEnable.RegAddr = unitIn.yEnableAddr;
//			dcRegEnable.RegBitLoc = unitIn.yEnableBitLoc;
//			dcRegEnable.RegLength = unitIn.yEnableLength;
//			dcRegMargin.RegAddr = unitIn.yMarginAddr;
//			dcRegMargin.RegBitLoc = unitIn.yMarginBitLoc;
//			dcRegMargin.RegLength = unitIn.yMarginLength;
//			dcRegSetting.RegAddr = unitIn.ySettingAddr;
//			dcRegSetting.RegBitLoc = unitIn.ySettingBitLoc;
//			dcRegSetting.RegLength = unitIn.ySettingLength;
        }
        */
    }

    //(A150922)Francis, CombinationTest function used, use to bind to Message UI
    public class CombineMessageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private string m_strMessage;
        public string strMessage
        {
            get { return m_strMessage; }
            set { m_strMessage = value; OnPropertyChanged("strMessage"); }
        }

        private bool m_bRedAlert;
        public bool bRedAlert
        {
            get { return m_bRedAlert; }
            set { m_bRedAlert = value; OnPropertyChanged("bRedAlert"); }
        }

        public CombineMessageModel()
        { }

        public CombineMessageModel(string strIn)
        {
            strMessage = strIn;
        }
    }

	//no used
    public class DCLDC2Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

		private string m_strChannelName;
		public string strChannelName
		{
			get { return m_strChannelName; }
			set { m_strChannelName = value; }
		}

		private string m_strChlEnable;
		public string strChlEnalbe
		{
			get { return m_strChlEnable; }
			set { m_strChlEnable = value; }
		}
		private bool m_bChannelEnable;
		public bool bChannelEnable
		{
			get {return m_bChannelEnable;}
			set { m_bChannelEnable = value;}
		}

    }

	public class XMLDataStatus
	{
		public string[] strArrBitXDescript = new string[8];
		public string strAlertDescript { get; set; }
		public byte yBitAddress { get; set; }
		public byte yBitStart { get; set; }
		public byte yBitNumber { get; set; }
		public Parameter pmrXDParent { get; set; }
		public UInt32 u32Guid { get; set; }
	}

	public class VIDVoltage : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public string[] strVIDArr = new string[16];

		//private string m_strVID00;
		public string strVID00
		{
			get { return strVIDArr[0]; }
			set { strVIDArr[0] = value; OnPropertyChanged("strVID00"); }
		}

		public string strVID01
		{
			get { return strVIDArr[1]; }
			set { strVIDArr[1] = value; OnPropertyChanged("strVID01"); }
		}

		public string strVID02
		{
			get { return strVIDArr[2]; }
			set { strVIDArr[2] = value; OnPropertyChanged("strVID02"); }
		}

		public string strVID03
		{
			get { return strVIDArr[3]; }
			set { strVIDArr[3] = value; OnPropertyChanged("strVID03"); }
		}

		public string strVID04
		{
			get { return strVIDArr[4]; }
			set { strVIDArr[4] = value; OnPropertyChanged("strVID04"); }
		}

		public string strVID05
		{
			get { return strVIDArr[5]; }
			set { strVIDArr[5] = value; OnPropertyChanged("strVID05"); }
		}

		public string strVID06
		{
			get { return strVIDArr[6]; }
			set { strVIDArr[6] = value; OnPropertyChanged("strVID06"); }
		}

		public string strVID07
		{
			get { return strVIDArr[7]; }
			set { strVIDArr[7] = value; OnPropertyChanged("strVID07"); }
		}

		public string strVID08
		{
			get { return strVIDArr[8]; }
			set { strVIDArr[8] = value; OnPropertyChanged("strVID08"); }
		}

		public string strVID09
		{
			get { return strVIDArr[9]; }
			set { strVIDArr[9] = value; OnPropertyChanged("strVID09"); }
		}

		public string strVID0A
		{
			get { return strVIDArr[10]; }
			set { strVIDArr[10] = value; OnPropertyChanged("strVID0A"); }
		}

		public string strVID0B
		{
			get { return strVIDArr[11]; }
			set { strVIDArr[11] = value; OnPropertyChanged("strVID0B"); }
		}

		public string strVID0C
		{
			get { return strVIDArr[12]; }
			set { strVIDArr[12] = value; OnPropertyChanged("strVID0C"); }
		}

		public string strVID0D
		{
			get { return strVIDArr[13]; }
			set { strVIDArr[13] = value; OnPropertyChanged("strVID0D"); }
		}

		public string strVID0E
		{
			get { return strVIDArr[14]; }
			set { strVIDArr[14] = value; OnPropertyChanged("strVID0E"); }
		}

		public string strVID0F
		{
			get { return strVIDArr[15]; }
			set { strVIDArr[15] = value; OnPropertyChanged("strVID0F"); }
		}

		public Int16 iRowNum { get; set; }
	}

	public class StatusModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		private string m_strReg07Description;
		public string strReg07Description
		{
			get { return m_strReg07Description; }
			set
			{
				m_strReg07Description = value;
				OnPropertyChanged("strReg07Description");
			}
		}

		private bool m_bReg07Value;
		public bool bReg07Value
		{
			get { return m_bReg07Value; }
			set
			{
				m_bReg07Value = value;
				OnPropertyChanged("bReg07Value");
			}
		}

		private string m_strReg08Description;
		public string strReg08Description
		{
			get { return m_strReg08Description; }
			set
			{
				m_strReg08Description = value;
				OnPropertyChanged("strReg08Description");
			}
		}

		private bool m_bReg08Value;
		public bool bReg08Value
		{
			get { return m_bReg08Value; }
			set
			{
				m_bReg08Value = value;
				OnPropertyChanged("bReg08Value");
			}
		}

		private Visibility m_bReg08Visible;
		public Visibility bReg08Visible
		{
			get { return m_bReg08Visible; }
			set
			{
				m_bReg08Visible = value;
				OnPropertyChanged("bReg08Visible");
			}
		}

		public int iOrder { get; set; }

		//public StatusModel(XMLDataStatus xmlin)
		//{
		//}
	}

    public class CommandsI2C : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private bool m_bCmdRW;   //0:Write, 1:Read
        public bool bCmdRW
        {
            get { return m_bCmdRW; }
            set
			{ 
				m_bCmdRW = value; 
				//OnPropertyChanged("bCmdRW"); 
				if (m_bCmdRW)
					m_strCmdRW = string.Format("Read");
				else
					m_strCmdRW = string.Format("Write");
				OnPropertyChanged("strCmdRW");
			}
        }

		private string m_strCmdRW;
		public string strCmdRW
		{
			get { return m_strCmdRW; }
			set
			{
				m_strCmdRW = value;
				if (m_strCmdRW.IndexOf("Write") != -1)
				{
					m_bCmdRW = false;
				}
				else
				{
					m_bCmdRW = true;
				}
			}
		}

        private byte m_yRegIndex;
        public byte yRegIndex
        {
            get { return m_yRegIndex; }
            set
			{ 
				m_yRegIndex = value; 
				//OnPropertyChanged("yRegIndex"); 
				m_strRegIndex = string.Format("{0:X2}", m_yRegIndex);
				//OnPropertyChanged("strRegIndex");
			}
        }

		private string m_strRegIndex;
		public string strRegIndex
		{
			get { return m_strRegIndex; }
			set
			{
				//string strVal = value;
				//byte	yVal = 0;
				m_strRegIndex = value;
				m_yRegIndex = Convert.ToByte(value, 16);
				OnPropertyChanged("strRegIndex");
			}
		}

        private byte m_yRegValue;
        public byte yRegValue
        {
            get { return m_yRegValue; }
            set
			{
				m_yRegValue = value;
				//OnPropertyChanged("yRegValue"); 
				strRegValue = string.Format("{0:X2}", m_yRegValue);
				//OnPropertyChanged("strRegValue"); 
			}
        }

		private string m_strRegValue;
		public string strRegValue
		{
			get { return m_strRegValue; }
			set
			{
				m_strRegValue = value;
				//m_yRegValue = Convert.ToByte(m_strRegValue.ToLower());
				OnPropertyChanged("strRegValue");
			}
		}

        private UInt16 m_uBlanktime;
        public UInt16 uBlanktime
        {
            get { return m_uBlanktime; }
            set { m_uBlanktime = value; OnPropertyChanged("uBlanktime"); }
        }

        private bool m_bRepeat;
        public bool bRepeat
        {
            get { return m_bRepeat; }
            set { m_bRepeat = value; OnPropertyChanged("bRepeat"); }
        }

		private UInt16 m_iOrderI2C;
		public UInt16 iOrderI2C
		{
			get { return m_iOrderI2C; }
			set { m_iOrderI2C = value; OnPropertyChanged("iOrderI2C"); }
		}

		public byte[] ySendBuf = new byte[4];
		public byte[] yReceiveBuf = new byte[4];


        //fake constructor
        public CommandsI2C()
        {
            bCmdRW = true;
            yRegIndex = 0x0B;
            yRegValue = 0x00;
            m_uBlanktime = 1000;
            m_bRepeat = false;
			m_iOrderI2C = 1;
			//m_ListCmd.Add("Write");
			//m_ListCmd.Add("Read");
		}

		public CommandsI2C(bool bRWin, byte yIndexin, byte yValuein, UInt16 iblankin, bool bRepin, UInt16 iorderin)
		{
			bCmdRW = bRWin;
			yRegIndex = yIndexin;
			yRegValue = yValuein;
			m_uBlanktime = iblankin;
			m_bRepeat = bRepin;
			m_iOrderI2C = iorderin;
			//m_ListCmd.Add("Write");
			//m_ListCmd.Add("Read");
		}

		public void MakeBytePackage(byte yAddr)
		{
            ySendBuf[0] = yAddr;
            ySendBuf[1] = m_yRegIndex;
            ySendBuf[2] = m_yRegValue;
            yReceiveBuf[0] = 0x00;
            yReceiveBuf[1] = 0x00;
            yReceiveBuf[2] = 0x00;
		}
	}
}
