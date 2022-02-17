using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using Cobra.Common;
using Cobra.Communication;
using Cobra.EM;
using Cobra.Lotus;


namespace Cobra.LotusEvPanel
{
	public class LotusEvViewMode
	{
		static public UInt32 SectionElementFlag = 0xFFFF0000;
		static public UInt32 OperationElement = 0x00030000;
		static public string GroupDCLDO = string.Format("DCLDO");
        static public string strSetting = Path.Combine(FolderMap.m_root_folder, "Settings\\LotusSetting.xml");
        static public string strRandomLogFolder = Path.Combine(FolderMap.m_currentproj_folder, "Log");
        static public string strOpenedConfig = null;
        static public UInt16 uInvalidValue = 0xFF00;
        static public UInt16 uValidMask = 0x00FF;

		public Device devParent { get; set; }
		public LotusEvControl ctrParent { get; set; }
		public string strSFLName { get; set; }

		private ParamContainer m_pmcntDMParameterList = new ParamContainer();
		public ParamContainer pmcntDMParameterList
		{
			get { return m_pmcntDMParameterList; }
			set { m_pmcntDMParameterList = value; }
		}

		private AsyncObservableCollection<DCLDOModel> m_DCLDORegList = new AsyncObservableCollection<DCLDOModel>();
		public AsyncObservableCollection<DCLDOModel> DCLDORegList
		{
			get { return m_DCLDORegList; }
			set { m_DCLDORegList = value; }
		}

        //no used
        private AsyncObservableCollection<DCLDC2Model> m_DCLDO2List = new AsyncObservableCollection<DCLDC2Model>();
        public AsyncObservableCollection<DCLDC2Model> DCLDO2List
        {
            get { return m_DCLDO2List; }
            set { m_DCLDO2List = value; }
        }

        private AsyncObservableCollection<VIDVoltage> m_VIDList = new AsyncObservableCollection<VIDVoltage>();
        public AsyncObservableCollection<VIDVoltage> VIDList
        {
            get { return m_VIDList; }
            set { m_VIDList = value; }
        }

        private AsyncObservableCollection<StatusModel> m_StatusList = new AsyncObservableCollection<StatusModel>();
        public AsyncObservableCollection<StatusModel> StatusList
        {
            get { return m_StatusList; }
            set { m_StatusList = value; }
        }

        private AsyncObservableCollection<CommandsI2C> m_I2CList = new AsyncObservableCollection<CommandsI2C>();
        public AsyncObservableCollection<CommandsI2C> I2CList
        {
            get { return m_I2CList; }
            set { m_I2CList = value; }
        }

        //(A150714)Francis, Random data binding used
		private AsyncObservableCollection<DCLDO3Model> m_DCLDORegRandomList = new AsyncObservableCollection<DCLDO3Model>();
		public AsyncObservableCollection<DCLDO3Model> DCLDORegRandomList
		{
			get { return m_DCLDORegRandomList; }
			set { m_DCLDORegRandomList = value; }
		}

        //(A150714)Francis, Log data for Random one function
        private AsyncObservableCollection<RandomLogModel> m_LogList = new AsyncObservableCollection<RandomLogModel>();
        public AsyncObservableCollection<RandomLogModel> LogList
        {
            get { return m_LogList; }
            set { m_LogList = value; }
        }

        //(A150915)Francis, Combination data binding used, binding to all Register
        private AsyncObservableCollection<CombinationRegister> m_CmbinaRegList = new AsyncObservableCollection<CombinationRegister>();
        public AsyncObservableCollection<CombinationRegister> CmbinaRegList
        {
            get { return m_CmbinaRegList; }
            set { m_CmbinaRegList = value; }
        }

        //(A150916)Francis
        private AsyncObservableCollection<DCCombineModel> m_DCCombinationList = new AsyncObservableCollection<DCCombineModel>();
        public AsyncObservableCollection<DCCombineModel> DCCombinationList
        {
            get { return m_DCCombinationList; }
            set { m_DCCombinationList = value; }
        }

        //(A150922)Francis
        private AsyncObservableCollection<CombineMessageModel> m_CombineMessageList = new AsyncObservableCollection<CombineMessageModel>();
        public AsyncObservableCollection<CombineMessageModel> CombineMessageList
        {
            get { return m_CombineMessageList; }
            set { m_CombineMessageList = value; }
        }

        //(A150716)Francis, Log data for Random one function
        public static FileStream m_fsLogFile;
        public static StreamWriter m_swriteLogFile;

        public ParamContainer pmcntStatus = new ParamContainer();
        public ParamContainer pmcntDCLDO = new ParamContainer();

        public ParamContainer pmcntRandom = new ParamContainer();			//(A150714)Francis, Random communication used
        public ParamContainer pmcntRndTelSelect = new ParamContainer();			//(A150714)Francis, Telemetry selection used in Random tab
        public ParamContainer pmcntRndTelConvert = new ParamContainer();			//(A150714)Francis, Telemetry read used in Random tab

        public ParamContainer pmcntCmbRegister = new ParamContainer();      //(A150916)Francis, all register list used in All register in Combination tab
        public ParamContainer pmcntCmbDCAllReg0A = new ParamContainer();         //(A150916)Francis, All DC Parameter, used in Combination DC all
        public ParamContainer pmcntCmbDCAllReg0708 = new ParamContainer();
        public ParamContainer pmcntCmbDCAllReg05 = new ParamContainer();
        public ParamContainer pmcntCmbDCAllReg06 = new ParamContainer();
        public ParamContainer pmcntCmbDCAllReg00 = new ParamContainer();
        public ParamContainer pmcntCmbDCAllReg0B = new ParamContainer();
        public ParamContainer pmcntCmbDCAllReg01 = new ParamContainer();
        public ParamContainer pmcntCmbDCAllReg02 = new ParamContainer();

		public bool bEnableRandomTab = true;

        //public ParamContainer pmcntI2CList = new ParamContainer();

		// <summary>
		// Constructor, parse xml definition and create/save in DataStructure
		// </summary>
		// <param name="pParent"></param>
		// <param name="parent"></param>

		public LotusEvViewMode(object pParent, object parent)
		{
			bool bPrepare = true;
			#region Initialization of Device / ExperControl / SFLname

			devParent = (Device)pParent;
			if (devParent == null) return;

			ctrParent = (LotusEvControl)parent;
			if (ctrParent == null) return;

			strSFLName = ctrParent.sflname;
			if (String.IsNullOrEmpty(strSFLName)) return;

			#endregion

			pmcntDMParameterList = devParent.GetParamLists(strSFLName);
			pmcntStatus.parameterlist.Clear();
			pmcntDCLDO.parameterlist.Clear();
			pmcntRandom.parameterlist.Clear();

			foreach (Parameter param in pmcntDMParameterList.parameterlist)
			{
				if (param == null) continue;
				if ((param.guid & SectionElementFlag) == OperationElement)
                    bPrepare &= ParseLotusEvXML(param);
				else
				{	//Therefore, if there are some Element in XML with LotusEv private section, but are not in sElementDefine
					//here wiil be ran
					bPrepare = false;
				}

			}
            bPrepare &= OpenI2CCmdCfg();
			//CalculateModelRegister();
			//BackExpertUI();
			//DCLDORegList.Sort(x => x.iOrder);
			//CollectDCLDORegister();

			if (!bPrepare)
			{
				MessageBox.Show(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EXPSFL_XML));
			}
			else
			{
				double dx;
				VIDList.Clear();
				for (int k = 0; k < 16; k++)
				{
					VIDVoltage vidtmp = new VIDVoltage();
					vidtmp.iRowNum = (Int16)k;
					for (int l = 0; l < 16; l++)
					{
						if((k==0) &&(l==0))
						{
							dx = 0F;
						}
						else
						{
							dx = ((k * 16 + (l - 1)) * 0.01) + 0.5;
						}
						vidtmp.strVIDArr[l] = string.Format("{0:F3}V", dx);
					}
					VIDList.Add(vidtmp);
					//if (k == 0)
						//vidtmp.strVID00 = "0.000".ToString();
				}
			}
		}

		private bool ParseLotusEvXML(Parameter paramIn)
		{
			byte ydata = 0;
			//Int16 idata = 0;
			XMLDataUnit mdlNew = new XMLDataUnit();
			XMLDataStatus mdlSt = new XMLDataStatus();
			XMLDataDCLDO mdlDc = new XMLDataDCLDO();
			bool bSuccess = true;
			int iDCLDO = 0;
			int iDCNew = 0;
			int iStatus = 0;
			//int iI2CCommand = 0;

            //(A150915)Francis, initialize u32Guid value as 0, use it to check data is valid if u32Guid != 0
            mdlNew.u32Guid = 0;     //actually this is no used
            mdlSt.u32Guid = 0;
            mdlDc.u32Guid = 0;
            //(E150915)

			foreach (DictionaryEntry de in paramIn.sfllist[strSFLName].nodetable)
			{
				switch (de.Key.ToString())
				{
					#region DC/LDO Group, no used
					/*
					case "Group":
						{
							mdlNew.strGroup = de.Value.ToString();
							break;
						}
					case "Description":
						{
							mdlNew.strDescription = de.Value.ToString();
							break;
						}
					case "Order":
						{
							if (!Int16.TryParse(de.Value.ToString(), out idata))
							{
								mdlNew.iOrder = 9;
							}
							else
							{
								mdlNew.iOrder = idata;
							}
							break;
						}
					case "EnableAddr":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.yEnableAddr = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.yEnableAddr = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "EnableBitLoc":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.yEnableBitLoc = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.yEnableBitLoc = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "EnableLength":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.yEnableLength = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.yEnableLength = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "MarginAddr":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.yMarginAddr = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.yMarginAddr = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "MarginBitLoc":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.yMarginBitLoc = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.yMarginBitLoc = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "MarginLength":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.yMarginLength = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.yMarginLength = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "SettingAddr":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.ySettingAddr = 0x08;
								bSuccess = false;
							}
							else
							{
								mdlNew.ySettingAddr = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "SettingBitLoc":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.ySettingBitLoc = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.ySettingBitLoc = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
					case "SettingLength":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlNew.ySettingLength = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlNew.ySettingLength = Convert.ToByte(de.Value.ToString(), 16);
								iDCLDO += 1;
							}
							break;
						}
						*/
					#endregion
					#region Status Group
					case "BitAddress":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlSt.yBitAddress = 0x07;
								bSuccess &= false;
							}
							else
							{
								mdlSt.yBitAddress = Convert.ToByte(de.Value.ToString(), 16);
								iStatus += 1;
							}
							break;
						}
					case "BitStart":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlSt.yBitStart = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlSt.yBitStart = Convert.ToByte(de.Value.ToString(), 16);
								iStatus += 1;
							}
							break;
						}
					case "BitNumber":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlSt.yBitNumber = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlSt.yBitNumber = Convert.ToByte(de.Value.ToString(), 16);
								iStatus += 1;
							}
							break;
						}
					case "Bit0Description":
						{
							mdlSt.strArrBitXDescript[0] = de.Value.ToString();
							break;
						}
					case "Bit1Description":
						{
							mdlSt.strArrBitXDescript[1] = de.Value.ToString();
							break;
						}
					case "Bit2Description":
						{
							mdlSt.strArrBitXDescript[2] = de.Value.ToString();
							break;
						}
					case "Bit3Description":
						{
							mdlSt.strArrBitXDescript[3] = de.Value.ToString();
							break;
						}
					case "Bit4Description":
						{
							mdlSt.strArrBitXDescript[4] = de.Value.ToString();
							break;
						}
					case "Bit5Description":
						{
							mdlSt.strArrBitXDescript[5] = de.Value.ToString();
							break;
						}
					case "Bit6Description":
						{
							mdlSt.strArrBitXDescript[6] = de.Value.ToString();
							break;
						}
					case "Bit7Description":
						{
							mdlSt.strArrBitXDescript[7] = de.Value.ToString();
							break;
						}
					case "AlertDescription":
						{
							mdlSt.strAlertDescript = de.Value.ToString();
							break;
						}
					#endregion
					#region New DC/LDO Group
					case "EnableAddr":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlDc.yEnableAddr = 0x0A;
								bSuccess &= false;
							}
							else
							{
								mdlDc.yEnableAddr = Convert.ToByte(de.Value.ToString(), 16);
								iDCNew += 1;
							}
							break;
						}
					case "EnableBitLoc":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlDc.yEnableStart = 0x00;
								bSuccess &= false;
							}
							else
							{
								mdlDc.yEnableStart = Convert.ToByte(de.Value.ToString(), 16);
								iDCNew += 1;
							}
							break;
						}
					case "EnableLength":
						{
							if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
							{
								mdlDc.yEnableNumber = 0x08;
								bSuccess &= false;
							}
							else
							{
								mdlDc.yEnableNumber = Convert.ToByte(de.Value.ToString(), 16);
								iDCNew += 1;
							}
							break;
						}
					case "En0Description":
						{
							mdlDc.strEnableXDescript[0] = de.Value.ToString();
							break;
						}
					case "En1Description":
						{
							mdlDc.strEnableXDescript[1] = de.Value.ToString();
							break;
						}
					case "En2Description":
						{
							mdlDc.strEnableXDescript[2] = de.Value.ToString();
							break;
						}
					case "En3Description":
						{
							mdlDc.strEnableXDescript[3] = de.Value.ToString();
							break;
						}
					case "En4Description":
						{
							mdlDc.strEnableXDescript[4] = de.Value.ToString();
							break;
						}
					case "En5Description":
						{
							mdlDc.strEnableXDescript[5] = de.Value.ToString();
							break;
						}
					case "En6Description":
						{
							mdlDc.strEnableXDescript[6] = de.Value.ToString();
							break;
						}
					case "En7Description":
						{
							mdlDc.strEnableXDescript[7] = de.Value.ToString();
							break;
						}
					case "EnableRandom":
						{
							if(!bool.TryParse(de.Value.ToString(), out bEnableRandomTab))
							{
								bEnableRandomTab = true;
							}
							break;
						}
					#endregion
				}	//switch
			}
			// iDCLDO  is not used, will be zero
			//if DC/LDO group
			if ((iDCLDO == 9) && (iStatus == 0) && (iDCNew == 0))
			{
				//bSuccess &= ConvertDCLDOModel(ref mdlNew);
				mdlNew.pmrXDParent = paramIn;
				mdlNew.u32Guid = paramIn.guid;
				DCLDOModel mdlDCNew = new DCLDOModel(mdlNew);
				DCLDORegList.Add(mdlDCNew);
				//(A150714)Francis, no used here
				//DCLDO3Model mdlDC3 = new DCLDO3Model(mdlNew);
				//DCLDORegRandomList.Add(mdlDC3);
				//(E150714)
			}
			else if ((iDCLDO == 0) && (iStatus == 3) && (iDCNew == 0))
			{
				mdlSt.pmrXDParent = paramIn;
				mdlSt.u32Guid = paramIn.guid;
				bSuccess &= ConstructStatusBindingContent(mdlSt);
                bSuccess &= ConstructCombinationRegisterAll(mdlSt);     //(A150915)Francis
			}
			else if ((iDCLDO == 0) && (iStatus == 0) && (iDCNew == 3))
			{
				mdlDc.pmrXDParent = paramIn;
				mdlDc.u32Guid = paramIn.guid;
				bSuccess &= ConstructDCLDOBindingContent(mdlDc);
				bSuccess &= ConstructDCLDORandomBindingContent(mdlDc);
                bSuccess &= ConstructCombinationRegisterAll(mdlDc);     //(A150915)Francis
			}
			//(A150915)Francis, for Reg0x0B, it will be part of Status and be part of Combination tab
			else if ((iDCLDO == 0) && (iStatus == 3) && (iDCNew == 3))
			{
				//add in Status
				mdlSt.pmrXDParent = paramIn;
				mdlSt.u32Guid = paramIn.guid;
				bSuccess &= ConstructStatusBindingContent(mdlSt);
                bSuccess &= ConstructCombinationRegisterAll(mdlSt);     //(A150915)Francis
                bSuccess &= ConstructReg0x0BInCombination(mdlDc);
				//add in
				//mdlDc.pmrXDParent = paramIn;
				//mdlDc.u32Guid = paramIn.guid;
				//bSuccess &= ConstructDCLDOBindingContent(mdlDc);
				//bSuccess &= ConstructDCLDORandomBindingContent(mdlDc);
			}
			else
			{
				bSuccess = false;
			}
			//ConvertXMLDataToModel(ref xmlData);
            AllocateParameterToAllContainer();      //foreach all Parameter in RegisterAll and assign to ParamContainer; to fast up running

			return bSuccess;
		}

		/*
		public bool ReadRegFromDevice(ref TASKMessage tskmsgLotus, DCLDOModel dcin = null)
		{
			//Parameter pmrtmp = null;
			ParamContainer pmCtntmp = new ParamContainer();
			AsyncObservableCollection<DCLDOModel> dclisttemp = new AsyncObservableCollection<DCLDOModel>();
			ParamContainer pmPhytmp = new ParamContainer();

			if (devParent.bBusy)
			{
				tskmsgLotus.errorcode = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
				return false;
			}
			else
			{
				devParent.bBusy = true;
				if (!GetDeviceInfor(ref tskmsgLotus))		//errorcode will be assigned in GetDeviceInfor
				{
					devParent.bBusy = false;
					return false;
				}
			}


			if (dcin != null)
			{
				//if(dcin.GetParentParameter())
				//if (pmrtmp == null)
				{	//collect Parameter error
					//return u32Return;
					devParent.bBusy = false;
					tskmsgLotus.errorcode = LibErrorCode.IDS_ERR_EXPSFL_OPREG_NOT_FOUND;
					return false;
				}
				//pmCtntmp.parameterlist.Add(pmrtmp);
				dclisttemp.Add(dcin);
				pmCtntmp.parameterlist.Add(dcin.pmrFromXML);
			}
			else
			{
				//TBD: suppose to be read all for DCLDO channel
			}
			tskmsgLotus.gm.controls = strSFLName;
			tskmsgLotus.task_parameterlist = pmCtntmp;
			tskmsgLotus.task = TM.TM_READ;
			devParent.AccessDevice(ref tskmsgLotus);
			while (tskmsgLotus.bgworker.IsBusy)
				System.Windows.Forms.Application.DoEvents();
			System.Windows.Forms.Application.DoEvents();
			//u32Return = tskmsgExper.errorcode;
			//if (u32Return == LibErrorCode.IDS_ERR_SUCCESSFUL)
			if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
			{
				tskmsgLotus.task = TM.TM_CONVERT_HEXTOPHYSICAL;
				devParent.AccessDevice(ref tskmsgLotus);
				while (tskmsgLotus.bgworker.IsBusy)
					System.Windows.Forms.Application.DoEvents();
				foreach (DCLDOModel dceach in dclisttemp)
				{
					//dceach = dceach.pmrExpMdlParent.hexdata;
					//dceach.SeperateRegValueToBit();
				}
			}
			else
			{
				devParent.bBusy = false;
				return false;
			}

			devParent.bBusy = false;
			return true;
		}
		*/

		#region Read/Write methods

		#region used in Command tab

		private bool ReadFromDevice(ref TASKMessage tskmsgLotus, ParamContainer pmcntIn)
		{
			bool bRet = true;

			if (ctrParent.bFranTestMode)
			{
				Random rnd = new Random();
				foreach (Parameter pmt in pmcntIn.parameterlist)
				{
					pmt.hexdata = (UInt16)rnd.Next(0, 256);
					pmt.phydata = Convert.ToDouble(pmt.hexdata);
					string strdbg = string.Format("Read addr={0:X2}, hexvalue={1:X2}, physical={2}", pmt.reglist["Low"].address, pmt.hexdata, pmt.phydata);
					Debug.WriteLine(strdbg);
					RandomLogModel rlm = new RandomLogModel("Read", (byte)pmt.reglist["Low"].address, (byte)pmt.hexdata, (int)pmt.phydata);
					LogList.Add(rlm);
					//SaveAllLogListAndClose(false);
					//LogList.Clear();
					//SaveLogFile(rlm);
				}
				return true;
			}

			if (devParent.bBusy)
			{
				tskmsgLotus.errorcode = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
				return false;
			}
			else
			{
				devParent.bBusy = true;
				if (!GetDeviceInfor(ref tskmsgLotus))		//errorcode will be assigned in GetDeviceInfor
				{
					devParent.bBusy = false;
					return false;
				}
			}

			tskmsgLotus.gm.controls = strSFLName;
			tskmsgLotus.task_parameterlist = pmcntIn;
			tskmsgLotus.task = TM.TM_READ;
			devParent.AccessDevice(ref tskmsgLotus);
			while (tskmsgLotus.bgworker.IsBusy)
				System.Windows.Forms.Application.DoEvents();
			System.Windows.Forms.Application.DoEvents();
			if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
			{
				tskmsgLotus.task = TM.TM_CONVERT_HEXTOPHYSICAL;
				devParent.AccessDevice(ref tskmsgLotus);
				while (tskmsgLotus.bgworker.IsBusy)
					System.Windows.Forms.Application.DoEvents();
				if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
					bRet = true;
				else
					bRet = false;
			}
			else
			{
				bRet = false;
			}
			devParent.bBusy = false;
			foreach (Parameter pmt in pmcntIn.parameterlist)
			{
				RandomLogModel rlm = new RandomLogModel("Read", (byte)pmt.reglist["Low"].address, (byte)pmt.hexdata, (int)pmt.phydata);
				LogList.Add(rlm);
				//SaveAllLogListAndClose(false);
				//LogList.Clear();
				//SaveLogFile(rlm);
			}

			return bRet;
		}

		private bool WriteToDevice(ref TASKMessage tskmsgLotus, ParamContainer pmcntIn)
		{
			bool bRet = true;

			if (ctrParent.bFranTestMode)
			{
				foreach (Parameter pmt in pmcntIn.parameterlist)
				{
					pmt.hexdata = Convert.ToByte(pmt.phydata);
					string strdbg = string.Format("Write addr= {0:X2}, hexvalue = {1:X2}, physical = {2}", pmt.reglist["Low"].address, pmt.hexdata, pmt.phydata);
					Debug.WriteLine(strdbg);
					RandomLogModel rlm = new RandomLogModel("Write", (byte)pmt.reglist["Low"].address, (byte)pmt.hexdata, (int)pmt.phydata);
					LogList.Add(rlm);
					//SaveAllLogListAndClose(false);
					//LogList.Clear();
					//SaveLogFile(rlm);
				}
				return true;
			}

			if (devParent.bBusy)
			{
				tskmsgLotus.errorcode = LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY;
				return false;
			}
			else
			{
				devParent.bBusy = true;
				if (!GetDeviceInfor(ref tskmsgLotus))		//errorcode will be assigned in GetDeviceInfor
				{
					devParent.bBusy = false;
					return false;
				}
			}

			tskmsgLotus.gm.controls = strSFLName;
			tskmsgLotus.task_parameterlist = pmcntIn;
			tskmsgLotus.task = TM.TM_CONVERT_PHYSICALTOHEX;
			devParent.AccessDevice(ref tskmsgLotus);
			while (tskmsgLotus.bgworker.IsBusy)
				System.Windows.Forms.Application.DoEvents();
			System.Windows.Forms.Application.DoEvents();
			if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
			{
				tskmsgLotus.task = TM.TM_WRITE;
				devParent.AccessDevice(ref tskmsgLotus);
				while (tskmsgLotus.bgworker.IsBusy)
					System.Windows.Forms.Application.DoEvents();
				if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
					bRet = true;
				else
					bRet = false;
			}
			else
			{
				bRet = false;
			}
			foreach (Parameter pmt in pmcntIn.parameterlist)
			{
				RandomLogModel rlm = new RandomLogModel("Write", (byte)pmt.reglist["Low"].address, (byte)pmt.hexdata, (int)pmt.phydata);
				LogList.Add(rlm);
				//SaveAllLogListAndClose(false);
				//LogList.Clear();
				//SaveLogFile(rlm);
			}
			devParent.bBusy = false;

			return bRet;
		}

		public bool ReadRegFromDevice(ref TASKMessage tskmsgLotus)
		{
			return ReadFromDevice(ref tskmsgLotus, pmcntDCLDO);
		}

		public bool WriteEnableToDevice(ref TASKMessage tskmsgLotus, DCLDOModel dcldoin)
		{
			ParamContainer pmtmp = new ParamContainer();
			Parameter prmtmp = null;
			byte yback;
			bool bRet = true;

			foreach(Parameter pmter in pmcntDCLDO.parameterlist)
			{
				if (pmter.reglist["Low"].address == 0x0A)
				{
					prmtmp = pmter;
					break;
				}
			}
			if (prmtmp == null)
			{
				return false;
			}
			else
			{
				byte yorigin = Convert.ToByte(prmtmp.phydata);
				yback = yorigin;
				byte ymask = (byte)(0x01 << dcldoin.iOrder);
				ymask = (byte)~ymask;
				yorigin &= ymask;
				yorigin |= (byte)(Convert.ToByte(dcldoin.bChannelEnable) << dcldoin.iOrder);
				prmtmp.phydata = Convert.ToDouble(yorigin);
				pmtmp.parameterlist.Add(prmtmp);
			}

			bRet &= WriteToDevice(ref tskmsgLotus, pmtmp);
			if (!bRet)
			{
				dcldoin.bChannelEnable = !dcldoin.bChannelEnable;
				prmtmp.phydata = Convert.ToDouble(yback);
			}
			return bRet;
		}

		public bool WriteMarginToDevice(ref TASKMessage tskmsgLotus, DCLDOModel dcldoin)
		{
			ParamContainer pmtmp = new ParamContainer();
			Parameter prmtmp = null;
			byte yorigin, yback;
			bool bRet = true;

			foreach (Parameter pmter in pmcntDCLDO.parameterlist)
			{
				if (pmter.reglist["Low"].address == 0x00)
				{
					prmtmp = pmter;
					break;
				}
			}
			if (prmtmp == null)
			{
				return false;
			}
			else
			{
				yorigin = Convert.ToByte(prmtmp.phydata);
				yback = yorigin;
				byte ysetval;
				ysetval = (byte)(Convert.ToByte(dcldoin.bMarginEnable) << 1);
				ysetval += (Convert.ToByte(dcldoin.bMarginValue));
				if (dcldoin.iOrder == 7)
				{
					yorigin &= 0x3F;
					ysetval <<= 6;
				}
				else if (dcldoin.iOrder == 6)
				{
					yorigin &= 0xCF;
					ysetval <<= 4;
				}
				else if (dcldoin.iOrder == 5)
				{
					yorigin &= 0xF3;
					ysetval <<= 2;
				}
				else if (dcldoin.iOrder == 4)
				{
					yorigin &= 0xFC;
					//ysetval
				}
				else
				{
					yorigin &= 0xff;
					return false;
				}

				yorigin += ysetval;
				prmtmp.phydata = Convert.ToDouble(yorigin);
				pmtmp.parameterlist.Add(prmtmp);
			}

			bRet &= WriteToDevice(ref tskmsgLotus, pmtmp);
			if (!bRet)
			{
				yorigin = (byte)(yback ^ yorigin);
				if((yorigin == 0x80) || (yorigin == 0x20) || (yorigin == 0x08) || (yorigin == 0x02))
					dcldoin.bMarginEnable = !dcldoin.bMarginEnable;
				else
					dcldoin.bMarginValue = !dcldoin.bMarginValue;
				prmtmp.phydata = Convert.ToDouble(yback);
			}
			return bRet;
		}

		public bool WriteHexMarginToDevice(ref TASKMessage tskmsgLotus, DCLDOModel dcldoin)
		{
			ParamContainer pmtmp = new ParamContainer();
			Parameter prmtmp = null;

			foreach (Parameter pmter in pmcntDCLDO.parameterlist)
			{
				if (pmter.reglist["Low"].address == (4-dcldoin.iOrder))
				{
					prmtmp = pmter;
					break;
				}
			}
			if (prmtmp == null)
			{
				return false;
			}
			else
			{
				prmtmp.phydata = Convert.ToDouble(dcldoin.yMarginHex);
				pmtmp.parameterlist.Add(prmtmp);
			}
			return WriteToDevice(ref tskmsgLotus, pmtmp);
		}

		#endregion

		#region used in Random one tab

		//(A150715)Francis, Random access Read/Write function
		public bool ReadRandomRegFromDevice(ref TASKMessage tskmsgLotus)
		{
			return ReadFromDevice(ref tskmsgLotus, pmcntRandom);
		}

		public bool WriteRandomEnableToDevice(ref TASKMessage tskmsgLotus, DCLDO3Model dcldoin)
		{
			ParamContainer pmtmp = new ParamContainer();
			Parameter prmtmp = null;
			byte yback;
			bool bRet = true;

			foreach(Parameter pmter in pmcntRandom.parameterlist)
			{
				if (pmter.reglist["Low"].address == 0x0A)
				{
					prmtmp = pmter;
					break;
				}
			}
			if (prmtmp == null)
			{
				return false;
			}
			else
			{
				byte yorigin = Convert.ToByte(prmtmp.phydata);
				yback = yorigin;
				byte ymask = (byte)(0x01 << dcldoin.iOrder);
				ymask = (byte)~ymask;
				yorigin &= ymask;
				yorigin |= (byte)(Convert.ToByte(dcldoin.bChannelEnable) << dcldoin.iOrder);
				//(A150716)Francis, as Yanlin request set CH2 same time if set LDO1EN, set Ch3 same time if set LDO2EN
				if (dcldoin.bChannelEnable)
				{
					if (dcldoin.iOrder == 0)
					{
						yorigin |= 0x20;
					}
					else if (dcldoin.iOrder == 1)
					{
						yorigin |= 0x40;
					}
				}
				else
				{
					if (dcldoin.iOrder == 0)
					{
						yorigin &= 0xDF;
					}
					else if (dcldoin.iOrder == 1)
					{
						yorigin &= 0xBF;
					}
				}
				//(E150716)
				prmtmp.phydata = Convert.ToDouble(yorigin);
				pmtmp.parameterlist.Add(prmtmp);
			}

			bRet &= WriteToDevice(ref tskmsgLotus, pmtmp);
			if (!bRet)
			{
				dcldoin.bChannelEnable = !dcldoin.bChannelEnable;
				prmtmp.phydata = Convert.ToDouble(yback);
			}
			return bRet;
		}

		public bool WriteRandomMarginToDevice(ref TASKMessage tskmsgLotus, DCLDO3Model dcldoin)
		{
			ParamContainer pmtmp = new ParamContainer();
			Parameter prmtmp = null;
			byte yorigin, yback;
			bool bRet = true;

			foreach (Parameter pmter in pmcntRandom.parameterlist)
			{
				if (pmter.reglist["Low"].address == 0x00)
				{
					prmtmp = pmter;
					break;
				}
			}
			if (prmtmp == null)
			{
				return false;
			}
			else
			{
				yorigin = Convert.ToByte(prmtmp.phydata);
				yback = yorigin;
				byte ysetval;
				ysetval = (byte)(Convert.ToByte(dcldoin.bMarginEnable) << 1);
				ysetval += (Convert.ToByte(dcldoin.bMarginValue));
				if (dcldoin.iOrder == 7)
				{
					yorigin &= 0x3F;
					ysetval <<= 6;
				}
				else if (dcldoin.iOrder == 6)
				{
					yorigin &= 0xCF;
					ysetval <<= 4;
				}
				else if (dcldoin.iOrder == 5)
				{
					yorigin &= 0xF3;
					ysetval <<= 2;
				}
				else if (dcldoin.iOrder == 4)
				{
					yorigin &= 0xFC;
					//ysetval
				}
				else
				{
					yorigin &= 0xff;
					return false;
				}

				yorigin += ysetval;
				prmtmp.phydata = Convert.ToDouble(yorigin);
				pmtmp.parameterlist.Add(prmtmp);
			}

			bRet &= WriteToDevice(ref tskmsgLotus, pmtmp);
			if (!bRet)
			{
				yorigin = (byte)(yback ^ yorigin);
				if ((yorigin == 0x80) || (yorigin == 0x20) || (yorigin == 0x08) || (yorigin == 0x02))
					dcldoin.bMarginEnable = !dcldoin.bMarginEnable;
				else
					dcldoin.bMarginValue = !dcldoin.bMarginValue;
				prmtmp.phydata = Convert.ToDouble(yback);
			}
			return bRet;
		}

		public bool WriteRandomHexMarginToDevice(ref TASKMessage tskmsgLotus, DCLDO3Model dcldoin)
		{
			ParamContainer pmtmp = new ParamContainer();
			Parameter prmtmp = null;

			foreach (Parameter pmter in pmcntRandom.parameterlist)
			{
				if (pmter.reglist["Low"].address == (4 - dcldoin.iOrder))
				{
					prmtmp = pmter;
					break;
				}
			}
			if (prmtmp == null)
			{
				return false;
			}
			else
			{
				prmtmp.phydata = Convert.ToDouble(dcldoin.yMarginHex);
				pmtmp.parameterlist.Add(prmtmp);
			}
			return WriteToDevice(ref tskmsgLotus, pmtmp);
		}

		public bool WriteTelemetrySelectionToDevice(ref TASKMessage tskmsgLotus, int iChannel, bool bClear = false)
		{
			bool bReturn = true;

			if((iChannel == -1) || (iChannel >= DCLDORegRandomList.Count))
			{
				return bReturn;
			}

			if(DCLDORegRandomList[iChannel].strChannelName.IndexOf("LDO") != -1)
			{
				//if LDO channel is random, no need to set Telemetry
				bReturn = true;
			}
			else
			{
				byte ySetVal;
				if(bClear)
					ySetVal = 0;
				else
					ySetVal = (byte)(8 - DCLDORegRandomList[iChannel].iOrder);
				if(pmcntRndTelSelect.parameterlist.Count == 1)
				{
					pmcntRndTelSelect.parameterlist[0].phydata = (double)ySetVal;
					bReturn &= WriteToDevice(ref tskmsgLotus, pmcntRndTelSelect);
				}
				else
				{
					bReturn = false;
				}
			}

			return bReturn;
		}

		public bool ReadTelemetryConvertionFromDevice(ref TASKMessage tskmsgLotus, int iChannel)
		{
			bool bReturn = true;
			byte yRVal = 0x00;

			if (DCLDORegRandomList[iChannel].strChannelName.IndexOf("LDO") != -1)
			{
				//if LDO channel is random, no need to read Telemetry
				bReturn = true;
			}
			else
			{
				bReturn &= ReadFromDevice(ref tskmsgLotus, pmcntRndTelConvert);
				if (bReturn)
				{
					if (pmcntRndTelConvert.parameterlist.Count == 1)
					{
						yRVal = (byte)pmcntRndTelConvert.parameterlist[0].hexdata;
					}
					else
					{
						yRVal = 0x00;
						bReturn = false;
					}
					DCLDORegRandomList[iChannel].yADTelemetryVal = yRVal;
				}
			}

			return bReturn;
		}
		//(E150715)

		#endregion

		#endregion

		#region DCLDO methods

		private bool ConstructDCLDOBindingContent(XMLDataDCLDO xmlin)
		{
			bool bRet = true;

			if (xmlin.yEnableAddr == 0x0A)
			{
				//xmlin.pmrXDParent.reglist["Low"].bitsnumber = 0x08;
				//pmcntDCLDO.parameterlist.Add(xmlin.pmrXDParent);
				pmcntDCLDO.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));
				//for (int i = 0; i < xmlin.strEnableXDescript.Length; i++)
				//{
					//DCLDOModel stdld = new DCLDOModel();
					//stdld.strChannelName = xmlin.strEnableXDescript[i];
					//stdld.strChlEnable = "Enable".ToString();
					//DCLDORegList.Add(stdld);
				//}
				int i = 7;
				foreach (DCLDOModel stdld in DCLDORegList)
				{
					stdld.strChannelName = xmlin.strEnableXDescript[i];
					stdld.strChlEnable = "Enable".ToString();
					stdld.iOrder = (Int16)i;
					stdld.MarginPhysical = "0.000V".ToString();
					i -= 1;
				}
			}
			else if (xmlin.yEnableAddr == 0x00)
			{
				//xmlin.pmrXDParent.reglist["Low"].bitsnumber = 0x08;
				//pmcntDCLDO.parameterlist.Add(xmlin.pmrXDParent);
				pmcntDCLDO.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));
				for (int i = 0; i < xmlin.strEnableXDescript.Length; i++)
				{
					DCLDOModel stdld = new DCLDOModel();
					DCLDORegList.Add(stdld);
				}
				DCLDORegList[0].strChlMarEn = xmlin.strEnableXDescript[7];
				DCLDORegList[0].strChlMarVal = xmlin.strEnableXDescript[6];
				DCLDORegList[1].strChlMarEn = xmlin.strEnableXDescript[5];
				DCLDORegList[1].strChlMarVal = xmlin.strEnableXDescript[4];
				DCLDORegList[2].strChlMarEn = xmlin.strEnableXDescript[3];
				DCLDORegList[2].strChlMarVal = xmlin.strEnableXDescript[2];
				DCLDORegList[3].strChlMarEn = xmlin.strEnableXDescript[1];
				DCLDORegList[3].strChlMarVal = xmlin.strEnableXDescript[0];
				for(int j = 0; j < 4; j++)
				{
					DCLDORegList[j].vsMarginButton = Visibility.Visible;
					DCLDORegList[j].vsMarginPhysical = Visibility.Collapsed;
					DCLDORegList[j].vsMarginFixVal = Visibility.Visible;
					DCLDORegList[j].vsMarginMassVal = Visibility.Collapsed;
					DCLDORegList[j].yDCCatagory = DCLDOModel.CatagoryDC1;
				}
			}
			else if ((xmlin.yEnableAddr == 0x01) || (xmlin.yEnableAddr == 0x02) ||
						(xmlin.yEnableAddr == 0x03) || (xmlin.yEnableAddr == 0x04))
			{
				//xmlin.pmrXDParent.reglist["Low"].bitsnumber = 0x08;
				//pmcntDCLDO.parameterlist.Add(xmlin.pmrXDParent);
				pmcntDCLDO.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));
				DCLDORegList[xmlin.yEnableAddr + 3].vsMarginButton = Visibility.Collapsed;
				DCLDORegList[xmlin.yEnableAddr + 3].vsMarginPhysical = Visibility.Visible;
				DCLDORegList[xmlin.yEnableAddr + 3].vsMarginFixVal = Visibility.Collapsed;
				DCLDORegList[xmlin.yEnableAddr + 3].vsMarginMassVal = Visibility.Visible;
				DCLDORegList[xmlin.yEnableAddr + 3].yDCCatagory = DCLDOModel.CatagoryDC8;
			}
			else
			{
				//(A150715)Francis, bypass register 0x05 and 0x06, it is not used in Command tab
                //(M150916)Francis, add 0x09, 0xFE, 0xFF bypass register, this is used only in Combination tab
				if((xmlin.yEnableAddr == 0x05) || (xmlin.yEnableAddr == 0x06) || (xmlin.yEnableAddr == 0x09) ||
                    (xmlin.yEnableAddr == 0xFE) || (xmlin.yEnableAddr == 0xFF))
				{
					bRet = true;
				}
				else
				{
					bRet = false;
				}
			}

			return bRet;
		}

		//(E150715)

		//no used
		private void CollectDCLDORegister()
		{
			Parameter pmtarget;

			pmcntDCLDO.parameterlist.Clear();
			foreach (DCLDOModel mdltmp in DCLDORegList)
			{
				if (pmcntDCLDO.GetParameterByGuid(mdltmp.pmrFromXML.guid) == null)
				{
					pmtarget = new Parameter();
					pmtarget.guid = mdltmp.pmrFromXML.guid;
					pmtarget.phyref = mdltmp.pmrFromXML.phyref;
					pmtarget.regref = mdltmp.pmrFromXML.regref;
					pmtarget.subtype = mdltmp.pmrFromXML.subtype;
					pmtarget.subsection = mdltmp.pmrFromXML.subsection;
					pmtarget.reglist.Clear();
					foreach (KeyValuePair<string, Reg> tmpreg in mdltmp.pmrFromXML.reglist)
					{
						Reg newReg = new Reg();
						newReg.address = tmpreg.Value.address;
						newReg.bitsnumber = tmpreg.Value.bitsnumber;
						newReg.startbit = tmpreg.Value.startbit;
						//regtmp = dicreg.Value;
						//regtmp.startbit = 0;
						//regtmp.bitsnumber = yRegLength;	//force 8-bits or 16-bits length
						//dicreg.Value.address = pmrtmp.reglist
						//dicreg.Value.startbit = 0;
						//dicreg.Value.bitsnumber = yRegLength;
						//pmrExpMdlParent.reglist.Add(tmpreg.Key, newReg);
						pmtarget.reglist.Add("Low", newReg);
					}
				}
			}
		}

		public bool ConvertChannelToBit()
		{
			bool bRet = true;

			foreach (Parameter pread in pmcntDCLDO.parameterlist)
			{
				if (pread.reglist["Low"].address == 0x0A)
				{
					int i = 7;
					byte ytmp = (byte)(pread.hexdata);
					foreach (DCLDOModel dldcld in DCLDORegList)
					{
						if (i < 0) i = 0;
						dldcld.bChannelEnable = Convert.ToBoolean((ytmp >> i) & 0x01);
						i -= 1;
					}
				}
				else if (pread.reglist["Low"].address == 0x00)
				{
					int j = 7;
					byte ytmp = (byte)(pread.hexdata);
					for (int k = 0; k < 4; k++)
					{
						if (j < 0) j = 0;
						DCLDORegList[k].bMarginEnable = Convert.ToBoolean((ytmp >> j) & 0x01);
						j -= 1;
						DCLDORegList[k].bMarginValue = Convert.ToBoolean((ytmp >> j) & 0x01);
						j -= 1;
					}
				}
				else if ((pread.reglist["Low"].address == 0x01) || (pread.reglist["Low"].address == 0x02) ||
							(pread.reglist["Low"].address == 0x03) || (pread.reglist["Low"].address == 0x04))
				{
					DCLDORegList[pread.reglist["Low"].address + 0x03].yMarginHex = (byte)pread.hexdata;
				}
				else
				{
					bRet &= false;
				}
			}

			return bRet;
		}

		#endregion

		#region Monitor methods

		private bool ConstructStatusBindingContent(XMLDataStatus xmlin)
		{
			bool bRet = true;

			if (xmlin.yBitAddress == 0x07)
			{
				//xmlin.pmrXDParent.reglist["Low"].bitsnumber = 08;
				//pmcntStatus.parameterlist.Add(xmlin.pmrXDParent);
				pmcntStatus.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));
				for (int i = 0; i < xmlin.strArrBitXDescript.Length; i++)
				{
					StatusModel stml = new StatusModel();
					stml.strReg07Description = xmlin.strArrBitXDescript[i];
					StatusList.Add(stml);
				}
			}
			else if (xmlin.yBitAddress == 0x08)
			{
				//xmlin.pmrXDParent.reglist["Low"].bitsnumber = 08;
				//pmcntStatus.parameterlist.Add(xmlin.pmrXDParent);
				pmcntStatus.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));
				if (StatusList.Count != xmlin.strArrBitXDescript.Length)
				{
					bRet &= false;	//should not happen
				}
				else
				{
					for (int j = 0; j < xmlin.strArrBitXDescript.Length; j++)
					{
						StatusModel stms = StatusList[j];
						stms.strReg08Description = xmlin.strArrBitXDescript[j];
						stms.bReg08Visible = Visibility.Visible;
					}
				}
			}
			else if (xmlin.yBitAddress == 0x0B)
			{
				pmcntStatus.parameterlist.Add(xmlin.pmrXDParent);
				StatusModel stmal = new StatusModel();
				stmal.strReg07Description = xmlin.strAlertDescript;
				stmal.bReg08Visible = Visibility.Collapsed;
				StatusList.Add(stmal);
			}
			else
			{
				bRet = false;
			}
			

			return bRet;
		}

		public bool ReadStatusFromDevice(ref TASKMessage tskmsgLotus)
		{
			return ReadFromDevice(ref tskmsgLotus, pmcntStatus);
		}

		public bool ConvertStatusToBit()
		{
			bool bRet = true;

			foreach (Parameter pread in pmcntStatus.parameterlist)
			{
				if (pread.reglist["Low"].address == 0x07)
				{
					for (int i=0; i<8; i++)
					{
						StatusList[i].bReg07Value = Convert.ToBoolean((pread.hexdata >> i) & 0x01);
					}
				}
				else if (pread.reglist["Low"].address == 0x08)
				{
					for (int i = 0; i < 8; i++)
					{
						StatusList[i].bReg08Value = Convert.ToBoolean((pread.hexdata >> i) & 0x01);
					}
				}
				else if (pread.reglist["Low"].address == 0x0B)
				{
					StatusList[StatusList.Count - 1].bReg07Value = Convert.ToBoolean(pread.hexdata & 0x01);
				}
				else
				{
					bRet &= false;
					break;
				}
			}

			return bRet;
		}

		#endregion

		#region CommandI2C group related

		public bool OpenI2CCmdCfg(string strOpenFilePath = null)
        {
			XmlElement xmlEmtRoot;
			XmlDocument xmlConfig = new XmlDocument();
			XmlDocument xmlCommands = new XmlDocument();
			string strTemp = null;
			CommandsI2C i2ctmp = null;
			byte yIndex = 0x0B, yValue = 0x00;
			bool bRW = false, bRepeat = false;
			UInt16 iBlank = 1000, iOrder = 1;
			bool bRet = true;

			//if (ctrParent.bFranTestMode)
			//{
				//i2ctmp = new CommandsI2C();

				//m_I2CList.Add(i2ctmp);
			//}
			//else
			{
				if (!File.Exists(LotusEvViewMode.strSetting))
				{
                    #region create a new Lotus setting  XML
                    try
					{
						//strTemp = Path.Combine(FolderMap.m_projects_folder, ctrParent.sflname);
						strTemp = Path.Combine(FolderMap.m_currentproj_folder, "defaultI2C.xml");
						xmlConfig.AppendChild(xmlConfig.CreateXmlDeclaration("1.0", "UTF-8", "yes"));
						xmlEmtRoot = xmlConfig.CreateElement("Root");
						xmlConfig.AppendChild(xmlEmtRoot);
						XmlElement xeLotus = xmlConfig.CreateElement("I2CConfig");
						xmlEmtRoot.AppendChild(xeLotus);
						XmlElement xeLotusFile = xmlConfig.CreateElement("File");
                        xeLotusFile.InnerText = strTemp;
						xeLotus.AppendChild(xeLotusFile);
                        XmlElement xeRandomLog = xmlConfig.CreateElement("RandomLog");
                        xmlEmtRoot.AppendChild(xeRandomLog);
                        XmlElement xeRndlogFolder = xmlConfig.CreateElement("Folder");
                        xeRndlogFolder.InnerText = LotusEvViewMode.strRandomLogFolder;
                        xeRandomLog.AppendChild(xeRndlogFolder);
						xmlConfig.Save(LotusEvViewMode.strSetting);
					}
					catch (Exception ef)
					{
						//should not happen
						//bConfigOK = false;
						//CreateNewErrorLog(TableConfigFile, UInt32.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, LibErrorCode.IDS_ERR_TMK_TBL_CONFIG_NO_EXIT);
						return false;
					}
                    #endregion
                }
				else		//if (!File.Exists(LotusEvViewMode.strSetting))
				{
					#region read I2C command Config
					xmlConfig.Load(LotusEvViewMode.strSetting);
                    xmlEmtRoot = xmlConfig.DocumentElement;
                    XmlNode xnI2CFile = xmlEmtRoot.SelectSingleNode("//I2CConfig/File");
                    if ((strOpenFilePath == null) || (xnI2CFile == null))
                    {
                        strTemp = xnI2CFile.InnerText;
                    }
                    else
                    {
                        strTemp = strOpenFilePath;
                        xnI2CFile.InnerText = strTemp;
                        xmlConfig.Save(LotusEvViewMode.strSetting);
                    }
                    XmlNode xnRandomLogFolder = xmlEmtRoot.SelectSingleNode("//RandomLog/Folder");
                    if(xnRandomLogFolder == null)
                    {
                        XmlElement xeRandomLog = xmlConfig.CreateElement("RandomLog");
                        xmlEmtRoot.AppendChild(xeRandomLog);
                        XmlElement xeRndlogFolder = xmlConfig.CreateElement("Folder");
                        xeRndlogFolder.InnerText = LotusEvViewMode.strRandomLogFolder;
                        xeRandomLog.AppendChild(xeRndlogFolder);
                        xmlConfig.Save(LotusEvViewMode.strSetting);
                    }
                    else
                    {
                        LotusEvViewMode.strRandomLogFolder = xnRandomLogFolder.InnerText;
                    }
					#endregion
				}	//if (!File.Exists(LotusEvViewMode.strSetting))
			}	//if (ctrParent.bFranTestMode)

			if ((strTemp == null) || (!File.Exists(strTemp)))
			{
                #region reset I2C config string in Lotus setting  XML
                strTemp = Path.Combine(FolderMap.m_currentproj_folder, "defaultI2C.xml");
                xmlConfig.Load(LotusEvViewMode.strSetting);
                xmlEmtRoot = xmlConfig.DocumentElement;
                XmlNode xnI2CFile = xmlEmtRoot.SelectSingleNode("//I2CConfig/File");
                xnI2CFile.InnerText = strTemp;
                xmlConfig.Save(LotusEvViewMode.strSetting);
                #endregion
                //bRet &= SaveI2CCmdCfg(strTemp, true);
				i2ctmp = new CommandsI2C();
				m_I2CList.Add(i2ctmp);
			}
			else
			{
				#region read I2C command Config
				XElement cmdsNode = XElement.Load(strTemp);
				m_I2CList.Clear();
				IEnumerable<XElement> cmdslist = from target in cmdsNode.Elements("CommandSet") select target;
				foreach (XElement xemt in cmdslist)
				{
					iOrder = 1;
					if(!String.IsNullOrEmpty(xemt.Attribute("Order").Value))
						UInt16.TryParse(xemt.Attribute("Order").Value, out iOrder);
					bRW = false;
					if (!String.IsNullOrEmpty(xemt.Element("RW").Value))
						bool.TryParse(xemt.Element("RW").Value, out bRW);
					yIndex = 0x0B;
					if (!String.IsNullOrEmpty(xemt.Element("Index").Value))
						yIndex = Convert.ToByte(xemt.Element("Index").Value.ToString(), 16);
					yValue = 0x00;
					if (!String.IsNullOrEmpty(xemt.Element("Value").Value))
						yValue = Convert.ToByte(xemt.Element("Value").Value.ToString(), 16);
					iBlank = 1000;
					if (!String.IsNullOrEmpty(xemt.Element("Blank").Value))
						UInt16.TryParse(xemt.Element("Blank").Value, out iBlank);
					bRepeat = false;
					if (!String.IsNullOrEmpty(xemt.Element("Repeat").Value))
						bool.TryParse(xemt.Element("Repeat").Value, out bRepeat);

					i2ctmp = new CommandsI2C(bRW, yIndex, yValue, iBlank, bRepeat, iOrder);
					m_I2CList.Add(i2ctmp);
				}

				#endregion
			}

            if (bRet)
                LotusEvViewMode.strOpenedConfig = strTemp;
            else
                LotusEvViewMode.strOpenedConfig = null;

            #region check Random Log folder exist
            if (strOpenFilePath == null)    //first time call this function at initialization process
            {
                try
                {
                    if (!Directory.Exists(LotusEvViewMode.strRandomLogFolder))
                    {
                        //Directory.CreateDirectory(LotusEvViewMode.strRandomLogFolder);
						LotusEvViewMode.strRandomLogFolder = Path.Combine(FolderMap.m_currentproj_folder, "Log");
						xmlConfig.Load(LotusEvViewMode.strSetting);
						xmlEmtRoot = xmlConfig.DocumentElement;
						XmlNode xnRandomLogFolder = xmlEmtRoot.SelectSingleNode("//RandomLog/Folder");
						xnRandomLogFolder.InnerText = LotusEvViewMode.strRandomLogFolder;
                        xmlConfig.Save(LotusEvViewMode.strSetting);
						Directory.CreateDirectory(LotusEvViewMode.strRandomLogFolder);
                    }
                }
                catch (Exception efd)
                {
                    return false;
                }
            }
            #endregion

            return bRet;
        }

		public bool SaveI2CCmdCfg(string strTargetCfgfile, bool bDefault = false)
        {
			XmlElement xmlEmtRoot;
			XmlDocument xmlCommands = new XmlDocument();
            XmlElement xecmdI2C, xeI2CRW, xeI2CIndex, xeI2CValue, xeI2CBlank, xeI2CRepeat;
            XmlAttribute xasetorder;
			//CommandsI2C i2ctmp = null;
			//byte yIndex = 0x0B, yValue = 0x00;
			//bool bRW = false, bRepeat = false;
			//UInt16 iBlank = 1000, iOrder = 1;
			//bool bRet = true;

            if (bDefault)
            {
                #region creat a default I2C commands XML
                xmlCommands.AppendChild(xmlCommands.CreateXmlDeclaration("1.0", "UTF-8", "yes"));
                xmlEmtRoot = xmlCommands.CreateElement("Root");
                xmlCommands.AppendChild(xmlEmtRoot);
                xecmdI2C = xmlCommands.CreateElement("CommandSet");
                xasetorder = xmlCommands.CreateAttribute("Order");
                xasetorder.Value = "01";
                xecmdI2C.Attributes.Append(xasetorder);
                xmlEmtRoot.AppendChild(xecmdI2C);
                xeI2CRW = xmlCommands.CreateElement("RW");
                xeI2CRW.InnerText = false.ToString();
                xecmdI2C.AppendChild(xeI2CRW);
                xeI2CIndex = xmlCommands.CreateElement("Index");
                xeI2CIndex.InnerText = "0B";
                xecmdI2C.AppendChild(xeI2CIndex);
                xeI2CValue = xmlCommands.CreateElement("Value");
                xeI2CValue.InnerText = "00";
                xecmdI2C.AppendChild(xeI2CValue);
                xeI2CBlank = xmlCommands.CreateElement("Blank");
                xeI2CBlank.InnerText = "1000";
                xecmdI2C.AppendChild(xeI2CBlank);
                xeI2CRepeat = xmlCommands.CreateElement("Repeat");
                xeI2CRepeat.InnerText = false.ToString();
                xecmdI2C.AppendChild(xeI2CRepeat);
                xmlCommands.Save(strTargetCfgfile);
                LotusEvViewMode.strOpenedConfig = strTargetCfgfile;
                #endregion
            }
            else
            {
                #region save to Setting file
                xmlCommands.AppendChild(xmlCommands.CreateXmlDeclaration("1.0", "UTF-8", "yes"));
                xmlEmtRoot = xmlCommands.CreateElement("Root");
                xmlCommands.AppendChild(xmlEmtRoot);
                for (int i = 0; i < m_I2CList.Count; i++)
                {
                    xecmdI2C = xmlCommands.CreateElement("CommandSet");
                    xasetorder = xmlCommands.CreateAttribute("Order");
                    xasetorder.Value = i.ToString(); ;
                    xecmdI2C.Attributes.Append(xasetorder);
                    xmlEmtRoot.AppendChild(xecmdI2C);
                    xeI2CRW = xmlCommands.CreateElement("RW");
                    xeI2CRW.InnerText = m_I2CList[i].bCmdRW.ToString();
                    xecmdI2C.AppendChild(xeI2CRW);
                    xeI2CIndex = xmlCommands.CreateElement("Index");
                    xeI2CIndex.InnerText = string.Format("{0:X2}", m_I2CList[i].yRegIndex);
                    xecmdI2C.AppendChild(xeI2CIndex);
                    xeI2CValue = xmlCommands.CreateElement("Value");
                    xeI2CValue.InnerText = string.Format("{0:X2}", m_I2CList[i].yRegValue);
                    xecmdI2C.AppendChild(xeI2CValue);
                    xeI2CBlank = xmlCommands.CreateElement("Blank");
                    xeI2CBlank.InnerText = string.Format("{0}", m_I2CList[i].uBlanktime);
                    xecmdI2C.AppendChild(xeI2CBlank);
                    xeI2CRepeat = xmlCommands.CreateElement("Repeat");
                    xeI2CRepeat.InnerText = m_I2CList[i].bRepeat.ToString();
                    xecmdI2C.AppendChild(xeI2CRepeat);
                }
                xmlCommands.Save(strTargetCfgfile);
                LotusEvViewMode.strOpenedConfig = strTargetCfgfile;
                #endregion
            }

			return true;
        }

		public void ConstructI2CCollection()
		{
            Cobra.Lotus.DEMDeviceManage Lotusdem = (Cobra.Lotus.DEMDeviceManage)devParent.device_dm.dem_lib;
            Options opTmp = Lotusdem.m_busopLotus.GetOptionsByGuid(BusOptions.I2CAddress_GUID);
            byte yI2CAddr = 0x30;

            if(opTmp != null)
            {
                yI2CAddr = (byte)(opTmp.SelectLocation.Code);
            }
			foreach (CommandsI2C i2ctmp in I2CList)
			{
                //i2ctmp.MakeBytePackage((byte)((SVIDBusOptions)Lotusdem.m_busopLotus).SelDeviceAddress.value);
                i2ctmp.MakeBytePackage(yI2CAddr);
			}
			
		}

        public bool DeliverI2CSetNDelay()
        {
            Cobra.Lotus.DEMDeviceManage Lotusdem = (Cobra.Lotus.DEMDeviceManage)devParent.device_dm.dem_lib;
            CCommunicateManager tmpcom = Lotusdem.m_Interface;
            bool bRet = true;
            UInt16 urecei = 0;
            //string  strdbg;
            Random rnd = new Random();

            foreach(CommandsI2C i2ctmp in I2CList)
            {
                if (i2ctmp.bCmdRW)   //read
                {
                    urecei = 0;
                    //if (!ctrParent.bFranTestMode)
                    {
                        bRet &= tmpcom.ReadDevice(i2ctmp.ySendBuf, ref i2ctmp.yReceiveBuf, ref urecei, 1);
                    }
                    //else
                    //{
                        //i2ctmp.yReceiveBuf[0] = (byte)rnd.Next(0, 256);
                        //strdbg = string.Format("Readd = {0:X2}, {1:X2}, {2:X2}, {3:X2}", i2ctmp.ySendBuf[0], i2ctmp.ySendBuf[1], i2ctmp.ySendBuf[2], i2ctmp.ySendBuf[3]);
                        //Debug.WriteLine(strdbg);
                    //}
                    //if(bRet)
                    //{
                        //i2ctmp.yRegValue = i2ctmp.yReceiveBuf[0];
                    //}
                }
                else
                {
                    urecei = 2;
                    //if (!ctrParent.bFranTestMode)
                    //{
                        bRet &= tmpcom.WriteDevice(i2ctmp.ySendBuf, ref i2ctmp.yReceiveBuf, ref urecei);
                    //}
                    //else
                    //{
                        //i2ctmp.yRegValue = (byte)rnd.Next(0, 256);
                        //strdbg = string.Format("writed = {0:X2}, {1:X2}, {2:X2}, {3:X2}", i2ctmp.ySendBuf[0], i2ctmp.ySendBuf[1], i2ctmp.ySendBuf[2], i2ctmp.ySendBuf[3]);
                        //Debug.WriteLine(strdbg);
                    //}
                }
				//if (!bRet)
					//break;
				//else
				{
					Thread.Sleep(i2ctmp.uBlanktime-1);
				}
            }

            return bRet;
        }

        #endregion

		#region Random one Access

		//(A150715)Francis, create Random binding contect
		private bool ConstructDCLDORandomBindingContent(XMLDataDCLDO xmlin)
		{
			bool bRet = true;

			if (xmlin.yEnableAddr == 0x0A)
			{
				pmcntRandom.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));		//(A150715)Francis, Random communication
				int i = 7;
				foreach (DCLDO3Model stdld in DCLDORegRandomList)
				{
					stdld.strChannelName = xmlin.strEnableXDescript[i];
					stdld.strChlEnable = "Channel is Disable".ToString();
					stdld.iOrder = (Int16)i;
					stdld.MarginPhysical = "0.000V".ToString();
                    i -= 1;
				}
                //(A150916)Francis, add DCCombineModel for Comination tab
                i = 7;
                foreach (DCCombineModel dccmbmdl in DCCombinationList)
                {
                    dccmbmdl.strChannelName = xmlin.strEnableXDescript[i].Trim();
                    dccmbmdl.strChlEnable = "is Disable".ToString();
                    dccmbmdl.iOrder = (Int16)i;
                    dccmbmdl.MarginPhysical = "0.000V".ToString();
                    dccmbmdl.bChlPowerGood = false;
                    dccmbmdl.bChlFault = false;
                    dccmbmdl.vsISChannel = Visibility.Visible;
                    #region     //(A150923)Francis, according to Jun's request, have a upper/lower bound for range check
                    //if (ctrParent.bFranTestMode)
                    //{
                    //Random rndBound = new Random();
                    //dccmbmdl.yADTelVoltLowBound = (byte)rndBound.Next(0, 256);
                    //dccmbmdl.yADTelVoltHighBound = (byte)rndBound.Next(0, 256);
                    //dccmbmdl.yADTelCurrLowBound = (byte)rndBound.Next(0, 256);
                    //dccmbmdl.yADTelCurrHighBound = (byte)rndBound.Next(0, 256);
                    //}
                    //else
                    switch (i)
                    {
                        case 7: //Channel 1
                            {
                                dccmbmdl.yADTelVoltLowBound = 0x91;
                                dccmbmdl.yADTelVoltHighBound = 0x97;
                                dccmbmdl.yADTelCurrLowBound = 0x91;
                                dccmbmdl.yADTelCurrHighBound = 0x97;
                                break;
                            }
                        case 6: //Channel 2
                            {
                                dccmbmdl.yADTelVoltLowBound = 0xF1;
                                dccmbmdl.yADTelVoltHighBound = 0xF7;
                                dccmbmdl.yADTelCurrLowBound = 0xF1;
                                dccmbmdl.yADTelCurrHighBound = 0xF7;
                                break;
                            }
                        case 5: //Channel 3
                            {
                                dccmbmdl.yADTelVoltLowBound = 0x47;
                                dccmbmdl.yADTelVoltHighBound = 0x4D;
                                dccmbmdl.yADTelCurrLowBound = 0x47;
                                dccmbmdl.yADTelCurrHighBound = 0x4D;
                                break;
                            }
                        case 4: //Channel 4
                            {
                                dccmbmdl.yADTelVoltLowBound = 0x55;
                                dccmbmdl.yADTelVoltHighBound = 0x5B;
                                dccmbmdl.yADTelCurrLowBound = 0x55;
                                dccmbmdl.yADTelCurrHighBound = 0x5B;
                                break;
                            }
                        case 3: //Channel 5
                            {
                                dccmbmdl.yADTelVoltLowBound = 0x82;
                                dccmbmdl.yADTelVoltHighBound = 0x88;
                                dccmbmdl.yADTelCurrLowBound = 082;
                                dccmbmdl.yADTelCurrHighBound = 0x88;
                                break;
                            }
                        case 2: //Channel 6
                            {
                                dccmbmdl.yADTelVoltLowBound = 0x3B;
                                dccmbmdl.yADTelVoltHighBound = 0x43;
                                dccmbmdl.yADTelCurrLowBound = 0x3B;
                                dccmbmdl.yADTelCurrHighBound = 0x43;
                                break;
                            }
                        default:
                            {
                                dccmbmdl.yADTelVoltLowBound = 0;
                                dccmbmdl.yADTelVoltHighBound = 0xFF;
                                dccmbmdl.yADTelCurrLowBound = 0;
                                dccmbmdl.yADTelCurrHighBound = 0xFF;
                                break;
                            }
                    }
                    #endregion
                    if ((i == 5) || (i == 4))
                    {
                        dccmbmdl.yChlDDRVal = 0;
                    }
                    i -= 1;
                }
                //(E150916)
            }
			else if (xmlin.yEnableAddr == 0x00)
			{
				pmcntRandom.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));		//(A150715)Francis, Random communication
				for (int i = 0; i < xmlin.strEnableXDescript.Length; i++)
				{
					DCLDO3Model stdld = new DCLDO3Model();
					DCLDORegRandomList.Add(stdld);
                    //(A150916)Francis, add DCCombineModel for Comination tab
                    DCCombineModel cmbmld = new DCCombineModel();
                    cmbmld.vsChlDDRVal = Visibility.Collapsed;
                    DCCombinationList.Add(cmbmld);
                    //(E150916)
					//(M150717)Francis, as Yanlin request, remove LDO1/2 from UI
					if (i >= 5)
						break;
				}
				for (int j = 0; j < 4; j++)
				{
					//DCLDORegRandomList[0].strChlMarEn = "Margin is Disable";//xmlin.strEnableXDescript[7];
					//DCLDORegRandomList[0].strChlMarVal = "Margin is -5%";// xmlin.strEnableXDescript[6];
					//DCLDORegRandomList[1].strChlMarEn = "Margin is Disable";// xmlin.strEnableXDescript[5];
					//DCLDORegRandomList[1].strChlMarVal = "Margin is -5%";//xmlin.strEnableXDescript[4];
					//DCLDORegRandomList[2].strChlMarEn = "Margin is Disable";// xmlin.strEnableXDescript[3];
					//DCLDORegRandomList[2].strChlMarVal = "Margin is -5%";//xmlin.strEnableXDescript[2];
					//DCLDORegRandomList[3].strChlMarEn = "Margin is Disable";// xmlin.strEnableXDescript[1];
					//DCLDORegRandomList[3].strChlMarVal = "Margin is -5%";//xmlin.strEnableXDescript[0];
					//DCLDORegRandomList[j].strChlMarEn = "Margin is Disable";// xmlin.strEnableXDescript[1];
					//DCLDORegRandomList[j].strChlMarVal = "Margin is -5%";//xmlin.strEnableXDescript[0];
                    DCLDORegRandomList[j].bMarginEnable = false;
                    DCLDORegRandomList[j].bMarginValue = false;
					DCLDORegRandomList[j].vsMarginButton = Visibility.Visible;
					DCLDORegRandomList[j].vsMarginPhysical = Visibility.Collapsed;
					DCLDORegRandomList[j].vsMarginFixVal = Visibility.Visible;
					DCLDORegRandomList[j].vsMarginMassVal = Visibility.Collapsed;
					DCLDORegRandomList[j].yDCCatagory = DCLDOModel.CatagoryDC1;
					DCLDORegRandomList[j].vsTelemetryVal = Visibility.Visible;
					DCLDORegRandomList[j].yADTelemetryVal = 0x00;
                    //(A150916)Francis, DCCombineModel for Comination tab
                    //DCCombinationList[j].strChlMarEn = "Margin is Disable";// xmlin.strEnableXDescript[1];
                    //DCCombinationList[j].strChlMarVal = "is -5%";//xmlin.strEnableXDescript[0];
                    DCCombinationList[j].bMarginEnable = false;
                    DCCombinationList[j].bMarginValue = false;
                    DCCombinationList[j].vsMarginButton = Visibility.Visible;
                    DCCombinationList[j].vsMarginPhysical = Visibility.Collapsed;
                    DCCombinationList[j].vsMarginFixVal = Visibility.Visible;
                    DCCombinationList[j].vsMarginMassVal = Visibility.Collapsed;
                    DCCombinationList[j].yDCCatagory = DCLDOModel.CatagoryDC1;
                    DCCombinationList[j].vsTelemetryVal = Visibility.Visible;
                    DCCombinationList[j].yADTelemetryVal = 0x00;
                    if (j >= 2)
                    {
                        DCCombinationList[j].vsChlDDRVal = Visibility.Visible;
                    }
                    //(E150916)
                }
			}
			else if ((xmlin.yEnableAddr == 0x01) || (xmlin.yEnableAddr == 0x02) ||
						(xmlin.yEnableAddr == 0x03) || (xmlin.yEnableAddr == 0x04))
			{
				//(M150717)Francis, as Yanlin request, remove LDO1/2 from UI
				if ((xmlin.yEnableAddr == 0x01) || (xmlin.yEnableAddr == 0x02))
				{   //ch5 and ch6
					pmcntRandom.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));		//(A150715)Francis, Random communication
					DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginButton = Visibility.Collapsed;
					DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginPhysical = Visibility.Visible;
					DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginFixVal = Visibility.Collapsed;
					DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginMassVal = Visibility.Visible;
					DCLDORegRandomList[xmlin.yEnableAddr + 3].yDCCatagory = DCLDOModel.CatagoryDC8;
					DCLDORegRandomList[xmlin.yEnableAddr + 3].vsTelemetryVal = Visibility.Visible;
					DCLDORegRandomList[xmlin.yEnableAddr + 3].yADTelemetryVal = 0x00;
                    //(A150916)Francis, DCCombineModel for Comination tab
                    DCCombinationList[xmlin.yEnableAddr + 3].vsMarginButton = Visibility.Collapsed;
                    DCCombinationList[xmlin.yEnableAddr + 3].vsMarginPhysical = Visibility.Visible;
                    DCCombinationList[xmlin.yEnableAddr + 3].vsMarginFixVal = Visibility.Collapsed;
                    DCCombinationList[xmlin.yEnableAddr + 3].vsMarginMassVal = Visibility.Visible;
                    DCCombinationList[xmlin.yEnableAddr + 3].yDCCatagory = DCLDOModel.CatagoryDC8;
                    DCCombinationList[xmlin.yEnableAddr + 3].vsTelemetryVal = Visibility.Visible;
                    DCCombinationList[xmlin.yEnableAddr + 3].yADTelemetryVal = 0x00;
                    //(E150916)
                }
				else
				{
					//pmcntRandom.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));		//(A150715)Francis, Random communication
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginButton = Visibility.Collapsed;
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginPhysical = Visibility.Visible;
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginFixVal = Visibility.Collapsed;
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].vsMarginMassVal = Visibility.Visible;
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].yDCCatagory = DCLDOModel.CatagoryDC8;
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].vsTelemetryVal = Visibility.Collapsed;
					//DCLDORegRandomList[xmlin.yEnableAddr + 3].yADTelemetryVal = 0x00;
				}
			}
			else if (xmlin.yEnableAddr == 0x05)
			{
				pmcntRndTelSelect.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));		//Add into Telemetry Selection
			}
			else if (xmlin.yEnableAddr == 0x06)
			{
				pmcntRndTelConvert.parameterlist.Add(CreateNCloneParameter(xmlin.pmrXDParent));		//Add into AD Telemetry 
			}
			else
			{
                //(M150916)Francis, add 0x09, 0xFE, 0xFF bypass register, this is used only in Combination tab
                if ((xmlin.yEnableAddr == 0x09) || (xmlin.yEnableAddr == 0xFE) || (xmlin.yEnableAddr == 0xFF))
                {
                    bRet = true;
                }
                else
                {
                    bRet = false;
                }
			}

			return bRet;
		}

		public bool ConvertRandomChannelToBit()
		{
			bool bRet = true;

			foreach (Parameter pread in pmcntRandom.parameterlist)
			{
				if (pread.reglist["Low"].address == 0x0A)
				{
					int i = 7;
					byte ytmp = (byte)(pread.hexdata);
					foreach (DCLDO3Model dldcld in DCLDORegRandomList)
					{
						if (i < 0) i = 0;
						dldcld.bChannelEnable = Convert.ToBoolean((ytmp >> i) & 0x01);
						i -= 1;
					}
				}
				else if (pread.reglist["Low"].address == 0x00)
				{
					int j = 7;
					byte ytmp = (byte)(pread.hexdata);
					for (int k = 0; k < 4; k++)
					{
						if (j < 0) j = 0;
						DCLDORegRandomList[k].bMarginEnable = Convert.ToBoolean((ytmp >> j) & 0x01);
						j -= 1;
						DCLDORegRandomList[k].bMarginValue = Convert.ToBoolean((ytmp >> j) & 0x01);
						j -= 1;
					}
				}
				else if ((pread.reglist["Low"].address == 0x01) || (pread.reglist["Low"].address == 0x02) ||
							(pread.reglist["Low"].address == 0x03) || (pread.reglist["Low"].address == 0x04))
				{
					DCLDORegRandomList[pread.reglist["Low"].address + 0x03].yMarginHex = (byte)pread.hexdata;
				}
				else
				{
					bRet &= false;
				}
			}

			return bRet;
		}

        public bool CreateLogFile(string strLogTarget = "Random", string postfix = ".log")
        {
            bool bReturn = true;
			string strFilePath = strLogTarget + DateTime.Now.GetDateTimeFormats('s')[0].ToString().Replace(@":", @"-") + postfix;
            string strFullPath;

            strFullPath = Path.Combine(LotusEvViewMode.strRandomLogFolder, strFilePath);
            try
            {
                m_fsLogFile = new FileStream(strFullPath, FileMode.OpenOrCreate);
                m_swriteLogFile = new StreamWriter(m_fsLogFile);
            }
            catch (Exception elog)
            {
                bReturn = false;
            }
           if(bReturn)
           {
               //strFilePath = string.Format("\t\t\t\t, Command string,\tMarginEn,\tMargin5%, \tMarginHex");
               //m_swriteLogFile.WriteLine(strFilePath);
               //m_swriteLogFile.Flush();
           }

            return bReturn;
        }

        public bool SaveLogFile(RandomLogModel rndLog, bool bTitle = false)
        {
            bool bReturn = true;
            //string strTimeInfo = DateTime.Now.ToString("HH:mm:ss");
            string strLogContent;

            //strTimeInfo += ":" + DateTime.Now.Millisecond.ToString() + "\t";
			//strLogContent = strTimeInfo+rndLog.strCommand+rndLog.strAddress+rndLog.strHexValue+rndLog.strPhyValue;
            //(M150922)Francis,
			//strLogContent = rndLog.strcurrentTime+rndLog.strCommand+rndLog.strAddress+rndLog.strHexValue+rndLog.strPhyValue;
            if (!bTitle)
            {
                strLogContent = rndLog.strcurrentTime + rndLog.strCommand + rndLog.strAddress + rndLog.strHexValue + rndLog.strPhyValue + rndLog.strResult;
            }
            else 
            { 
                strLogContent = rndLog.strcurrentTime + string.Format(",{0}0x{1:X2}\t,0x{2:X2}\t,{3}\t,{4}",
                                                                        rndLog.strCommand, rndLog.yAddress, rndLog.yHexValue, rndLog.iPhyValue,rndLog.strResult);
            }
			if (m_fsLogFile.CanWrite)
			{
				m_swriteLogFile.WriteLine(strLogContent);
				m_swriteLogFile.Flush();
			}

            return bReturn;
        }

		public bool SaveLogFile(string strRW, byte yAddr, byte yhex, int iphy)
		{
			bool bReturn = true;
			string strTimeInfo = DateTime.Now.ToString("HH:mm:ss");
			string strLogContent;

			strTimeInfo += ":" + DateTime.Now.Millisecond.ToString("FFF") + "\t";
			strLogContent = string.Format("{0},\t\t\tAddress={1:X2},\t\t\tHexValue={2:X3},\t\tPhysicalValue={3}", strRW, yAddr, yhex, iphy);
			m_swriteLogFile.WriteLine(strLogContent);
			m_swriteLogFile.Flush();

			return bReturn;
		}

		public bool SaveAllLogListAndClose(bool bClose = true, bool bTitle = false)
		{
			bool bReturn = true;

            if(bTitle)
            {
                m_swriteLogFile.WriteLine("Time\t\t,Command,Address,HexValue,Physical,Reference,Result");
                m_swriteLogFile.Flush();
            }
			foreach (RandomLogModel rrlm in LogList)
			{
				bReturn &= SaveLogFile(rrlm, bTitle);
			}
			if (bClose)
			{
				LogList.Clear();
				m_fsLogFile.Close();
			}

			return bReturn;
		}

		#endregion

        #region Combination methods

        private bool ConstructCombinationRegisterAll(XMLDataStatus xmldsIn)
        {
            bool bRet = true;
            //Parameter pmrNew = new Parameter();
            CombinationRegister cmbregNew = new CombinationRegister();

            /*
            pmrNew.guid = xmldsIn.pmrXDParent.guid;
            pmrNew.phyref = xmldsIn.pmrXDParent.phyref;
            pmrNew.regref = xmldsIn.pmrXDParent.regref;
            pmrNew.subtype = xmldsIn.pmrXDParent.subtype;
            pmrNew.subsection = xmldsIn.pmrXDParent.subsection;	//(A140409)Francis
            pmrNew.reglist.Clear();
            foreach (KeyValuePair<string, Reg> tmpreg in xmldsIn.pmrXDParent.reglist)
            {
                Reg newReg = new Reg();
                newReg.address = tmpreg.Value.address;
                newReg.bitsnumber = 8;// tmpreg.Value.bitsnumber;
                newReg.startbit = 0;// tmpreg.Value.startbit;
                pmrNew.reglist.Add("Low", newReg);
                cmbregNew.strRegName = string.Format("Reg 0x{0:X2}", newReg.address);
            }
            cmbregNew.pmrRegister = pmrNew;
            cmbregNew.strRegName = string.Format("Reg0x{0:X2}", xmldsIn.yBitAddress);
            */
            cmbregNew.yRegAddr = xmldsIn.yBitAddress;
            cmbregNew.yRegValue = 0;
            #region     //(A150923)Francis, according to Jun's request, add a reference value for each register
            switch(cmbregNew.yRegAddr)
            { 
                case 0x00:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0x01:
                {
                    cmbregNew.yReference = 0x83;
                    break;
                }
                case 0x02:
                {
                    cmbregNew.yReference = 0x24;
                    break;
                }
                case 0x03:
                {
                    cmbregNew.yReference = 0xC9;
                    break;
                }
                case 0x04:
                {
                    cmbregNew.yReference = 0x2E;
                    break;
                }
                case 0x05:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0x06:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0x07:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0x08:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0x09:
                {
                    cmbregNew.yReference = 0xFF;
                    break;
                }
                case 0x0A:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0x0B:
                {
                    cmbregNew.yReference = 0x41;
                    break;
                }
                case 0xFE:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
                case 0xFF:
                {
                    cmbregNew.yReference = 0xA1;
                    break;
                }
                default:
                {
                    cmbregNew.yReference = 0x00;
                    break;
                }
            }
            #endregion
            CmbinaRegList.Add(cmbregNew);
            pmcntCmbRegister.parameterlist.Add(CreateNCloneParameter(xmldsIn.pmrXDParent));
            return bRet;
        }

        private bool ConstructCombinationRegisterAll(XMLDataDCLDO xmldcIN)
        {
            bool bRet = true;
            //Parameter pmrNew = new Parameter();
            CombinationRegister cmbregNew = new CombinationRegister();
            /*
            pmrNew.guid = xmldcIN.pmrXDParent.guid;
            pmrNew.phyref = xmldcIN.pmrXDParent.phyref;
            pmrNew.regref = xmldcIN.pmrXDParent.regref;
            pmrNew.subtype = xmldcIN.pmrXDParent.subtype;
            pmrNew.subsection = xmldcIN.pmrXDParent.subsection;	//(A140409)Francis
            pmrNew.reglist.Clear();
            foreach (KeyValuePair<string, Reg> tmpreg in xmldcIN.pmrXDParent.reglist)
            {
                Reg newReg = new Reg();
                newReg.address = tmpreg.Value.address;
                newReg.bitsnumber = 8;// tmpreg.Value.bitsnumber;
                newReg.startbit = 0;// tmpreg.Value.startbit;
                pmrNew.reglist.Add("Low", newReg);
                cmbregNew.strRegName = string.Format("Reg 0x{0:X2}", newReg.address);
            }
            cmbregNew.pmrRegister = pmrNew;
            cmbregNew.strRegName = string.Format("Reg0x{0:X2}", xmldcIN.yEnableAddr);
            */
            cmbregNew.yRegAddr = xmldcIN.yEnableAddr;
            cmbregNew.yRegValue = 0;
            #region     //(A150923)Francis, according to Jun's request, add a reference value for each register
            switch (cmbregNew.yRegAddr)
            {
                case 0x00:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0x01:
                    {
                        cmbregNew.yReference = 0x83;
                        break;
                    }
                case 0x02:
                    {
                        cmbregNew.yReference = 0x24;
                        break;
                    }
                case 0x03:
                    {
                        cmbregNew.yReference = 0xC9;
                        break;
                    }
                case 0x04:
                    {
                        cmbregNew.yReference = 0x2E;
                        break;
                    }
                case 0x05:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0x06:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0x07:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0x08:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0x09:
                    {
                        cmbregNew.yReference = 0xFF;
                        break;
                    }
                case 0x0A:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0x0B:
                    {
                        cmbregNew.yReference = 0x41;
                        break;
                    }
                case 0xFE:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
                case 0xFF:
                    {
                        cmbregNew.yReference = 0xA1;
                        break;
                    }
                default:
                    {
                        cmbregNew.yReference = 0x00;
                        break;
                    }
            }
            #endregion
            CmbinaRegList.Add(cmbregNew);
            pmcntCmbRegister.parameterlist.Add(CreateNCloneParameter(xmldcIN.pmrXDParent));

            return bRet;
        }

        private bool ConstructReg0x0BInCombination(XMLDataDCLDO xmldcIn)
        {
            bool bRet = true;
            DCCombineModel cmbmld = new DCCombineModel();

            cmbmld.strChannelName = "OTP";
            cmbmld.vsMarginButton = Visibility.Collapsed;
            cmbmld.vsMarginFixVal = Visibility.Collapsed;
            cmbmld.vsMarginMassVal = Visibility.Collapsed;
            cmbmld.vsMarginPhysical = Visibility.Collapsed;
            cmbmld.vsTelemetryVal = Visibility.Visible;
            cmbmld.vsChlDDRVal = Visibility.Visible;
            cmbmld.vsISChannel = Visibility.Collapsed;
            cmbmld.yChlDDRVal = 0;
            cmbmld.yADTelemetryVal = 0;
            //if (ctrParent.bFranTestMode)
            //{
                //Random rndBound = new Random();
                //cmbmld.yADTelVoltLowBound = (byte)rndBound.Next(0, 256);
                //cmbmld.yADTelVoltHighBound = (byte)rndBound.Next(0, 256);
                //cmbmld.yADTelCurrLowBound = (byte)rndBound.Next(0, 256);
                //cmbmld.yADTelCurrHighBound = (byte)rndBound.Next(0, 256);
            //}
            //else
            {
                cmbmld.yADTelVoltLowBound = 0;
                cmbmld.yADTelVoltHighBound = 0xFF;
                cmbmld.yADTelCurrLowBound = 0;
                cmbmld.yADTelCurrHighBound = 0xFF;
            }
            DCCombinationList.Add(cmbmld);

            cmbmld = new DCCombineModel();
            cmbmld.strChannelName = "ALRT";
            cmbmld.vsMarginButton = Visibility.Collapsed;
            cmbmld.vsMarginFixVal = Visibility.Collapsed;
            cmbmld.vsMarginMassVal = Visibility.Collapsed;
            cmbmld.vsMarginPhysical = Visibility.Collapsed;
            cmbmld.vsTelemetryVal = Visibility.Collapsed;
            cmbmld.vsChlDDRVal = Visibility.Collapsed;
            cmbmld.vsISChannel = Visibility.Collapsed;
            //if (ctrParent.bFranTestMode)
            //{
                //Random rndBound2 = new Random();
                //cmbmld.yADTelVoltLowBound = (byte)rndBound2.Next(0, 256);
                //cmbmld.yADTelVoltHighBound = (byte)rndBound2.Next(0, 256);
                //cmbmld.yADTelCurrLowBound = (byte)rndBound2.Next(0, 256);
                //cmbmld.yADTelCurrHighBound = (byte)rndBound2.Next(0, 256);
            //}
            //else
            {
                cmbmld.yADTelVoltLowBound = 0x01;
                cmbmld.yADTelVoltHighBound = 0xFF;
                cmbmld.yADTelCurrLowBound = 0x01;
                cmbmld.yADTelCurrHighBound = 0xFF;
            }
            DCCombinationList.Add(cmbmld);

            return bRet;
        }

        private void FillValueInAllRegisterTab(Parameter pmrIn, int iIndexInLog)
        {
            byte yAddress = 0x00;

            foreach(KeyValuePair<string, Reg> regadd in pmrIn.reglist)
            {
                if(regadd.Key.Equals("Low"))
                {
                    yAddress = (byte)regadd.Value.address;
                    break;
                }
            }

            foreach(CombinationRegister cbRegone in CmbinaRegList)
            {
                if(cbRegone.yRegAddr == yAddress)
                {
                    cbRegone.yRegValue = (byte)pmrIn.hexdata;
                    if(cbRegone.yRegValue != cbRegone.yReference)
                    {
                        m_LogList[iIndexInLog].strResult = string.Format("0x{0:X2}\t\t,False", cbRegone.yReference);
                    }
                    else
                    {
                        m_LogList[iIndexInLog].strResult = string.Format("0x{0:X2}\t\t,T", cbRegone.yReference);
                    }
                    break;
                }
            }
        }

        private void AllocateParameterToAllContainer()
        {
            byte yAddress = 0x00;

            pmcntCmbDCAllReg0A.parameterlist.Clear();
            pmcntCmbDCAllReg0708.parameterlist.Clear();
            pmcntCmbDCAllReg05.parameterlist.Clear();
            pmcntCmbDCAllReg06.parameterlist.Clear();
            pmcntCmbDCAllReg00.parameterlist.Clear();
            pmcntCmbDCAllReg0B.parameterlist.Clear();
            pmcntCmbDCAllReg01.parameterlist.Clear();
            pmcntCmbDCAllReg02.parameterlist.Clear();
            foreach (Parameter pmrIn in pmcntCmbRegister.parameterlist)
            {
                foreach (KeyValuePair<string, Reg> regadd in pmrIn.reglist)
                {
                    if (regadd.Key.Equals("Low"))
                    {
                        yAddress = (byte)regadd.Value.address;
                        break;
                    }
                }
                if(yAddress == 0x00)
                {
                    pmcntCmbDCAllReg00.parameterlist.Add(pmrIn);
                }
                else if(yAddress == 0x01)
                {
                    pmcntCmbDCAllReg01.parameterlist.Add(pmrIn);
                }
                else if(yAddress == 0x02)
                {
                    pmcntCmbDCAllReg02.parameterlist.Add(pmrIn);
                }
                else if(yAddress == 0x05)
                {
                    pmcntCmbDCAllReg05.parameterlist.Add(pmrIn);
                }
                else if(yAddress == 0x06)
                {
                    pmcntCmbDCAllReg06.parameterlist.Add(pmrIn);
                }
                else if((yAddress == 0x07) || (yAddress == 0x08))
                {
                    pmcntCmbDCAllReg0708.parameterlist.Add(pmrIn);
                }
                else if(yAddress == 0x0A)
                {
                    pmcntCmbDCAllReg0A.parameterlist.Add(pmrIn);
                }
                else if(yAddress == 0x0B)
                {
                    pmcntCmbDCAllReg0B.parameterlist.Add(pmrIn);
                }
            }
        }

        private void MapADTelemetryValueToUI(byte yTelVal)
        {
            byte yRead = (byte)pmcntCmbDCAllReg06.parameterlist[0].hexdata;

            CmbinaRegList[6].yRegValue = yRead;

            if((yTelVal <= 6) && (yTelVal > 0))
            {
                DCCombinationList[yTelVal - 1].yADTelemetryVal = yRead;
                if ((DCCombinationList[yTelVal - 1].yADTelemetryVal >= DCCombinationList[yTelVal - 1].yADTelVoltLowBound) && 
                    (DCCombinationList[yTelVal - 1].yADTelemetryVal <= DCCombinationList[yTelVal - 1].yADTelVoltHighBound))
                {
                    m_LogList[m_LogList.Count - 1].strResult = string.Format("0x{0:X2}-0x{1:X2}\t,T", DCCombinationList[yTelVal - 1].yADTelVoltLowBound, DCCombinationList[yTelVal - 1].yADTelVoltHighBound);
                }
                else
                {
                    m_LogList[m_LogList.Count - 1].strResult = string.Format("0x{0:X2}-0x{1:X2}\t,False", DCCombinationList[yTelVal - 1].yADTelVoltLowBound, DCCombinationList[yTelVal - 1].yADTelVoltHighBound);
                    ctrParent.iErrorCount += 1;
                }
            }
            else if(yTelVal == 0x0F)
            {
                DCCombinationList[6].yADTelemetryVal = yRead;
                //if ((DCCombinationList[6].yADTelemetryVal >= DCCombinationList[6].yADTelVoltLowBound) && 
                    //(DCCombinationList[6].yADTelemetryVal <= DCCombinationList[6].yADTelVoltHighBound))
                //{
                    //m_LogList[m_LogList.Count - 1].strResult = string.Format("T");
                //}
                //else
                //{
                    //m_LogList[m_LogList.Count - 1].strResult = string.Format("False");
                    //ctrParent.iErrorCount += 1;
                //}
            }
        }

        //private byte CalculateValueAccordingChannel(ref byte yTargetChls, ref UInt16 yMarginVal, ref UInt16 yDDRReg0B, 
                                                    //ref UInt16 yVIDReg01, ref UInt16 yVIDReg02, bool bOTP = false)
        private bool CalculateValueAccordingChannel(ref TASKMessage tskmsgLotus, ref byte yTargetChls, bool bOTP = false)
        {
            //byte yRet = 0x00;
            bool bRet = true;
            byte yValue = 0x00;
            byte iMask = 1;
            byte yMarginVal = 0x00;
            byte yDDRReg0B = 0x00;
            byte yVIDRegVal = 0x00;
            bool bMargin = false;
            bool bDDRReg0B = false;
            bool bVIDReg01 = false;
            bool bVIDReg02 = false;
            int iRandValue1, iRandValue2, iRandValue3 = 0;
            Random rndValue = new Random();

            if (bOTP)   //thermal request
            {
                //yRet = 0x0F;
                #region have a random value for Control Register [b6:b5]
                iRandValue1 = rndValue.Next(0, 4);
                yDDRReg0B = (byte)(iRandValue1 << 5);
                yValue = 0x0F;
                bDDRReg0B = true;
                DCCombinationList[6].yChlDDRVal = (byte)iRandValue1;
                #endregion

                #region Start to command(read/write) to Device
                //step 11, Write OTP threshold through “Control Register [b6:b5]”
                if (bDDRReg0B)
                {
                    pmcntCmbDCAllReg0B.parameterlist[0].phydata = (float)yDDRReg0B;
                    bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0B);
                }
                //else
                //{
                    //bRet = false;
                //}
                //step 12, Write “Telemetry Selection Register” for the selected channel
                pmcntCmbDCAllReg05.parameterlist[0].phydata = (float)yValue;// yTargetChls;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
                CmbinaRegList[05].yRegValue = yValue;
                //step 13, Read “A/D Telemetry Register”
                bRet &= ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg06);
                MapADTelemetryValueToUI(yValue);
                #endregion
            }
            else
            {
                #region according to which channel is enable(yTagetChls), set up value should/would to write to Device
                for (int i = 7; i >= 2; i--)     //bit 7 to bit 2 of Reg0x0A
                {
                    iMask = (byte)(1 << i);
                    if((yTargetChls & iMask) != 0)  //found enable bit == 1
                    {
                        //calculate Telemetry Selection Register
                        //yRet = (byte)(i - 1);
                        yValue = (byte)(8 - i); //calculate TEL[3:0] value
                        //yTargetChls &= Convert.ToByte(~iMask);  //clear that bit
                        yTargetChls &= Convert.ToByte(0xFF - iMask);
                        //calculate Margin/VID Register/Control Register[b4:b1]
                        if((i == 7) || (i == 6) || (i == 5) || (i == 4))        //channel 1~4
                        {
                            iRandValue1 = rndValue.Next(0, 2);      //Margin Enable Random value
                            iRandValue2 = rndValue.Next(0, 2);      //Margin +/-5% random value
                            bMargin = true;
                            if(i == 7)          //channel 1
                            {
                                yMarginVal = (byte)((iRandValue1 << 7) + (iRandValue2 << 6));
                            }
                            else if(i == 6)     //channel 2
                            {
                                yMarginVal = (byte)((iRandValue1 << 5) + (iRandValue2 << 4));
                            }
                            else if (i == 5)    //channel 3
                            {
                                yMarginVal = (byte)((iRandValue1 << 3) + (iRandValue2 << 2));
                                iRandValue3 = rndValue.Next(0, 2);      //V1A random value
                                yDDRReg0B = (byte)(iRandValue3 << 1);
                                DCCombinationList[(7 - i)].yChlDDRVal = (byte)iRandValue3;
                                bDDRReg0B = true;
                            }
                            else if(i == 4)     //channel 4
                            {
                                yMarginVal = (byte)((iRandValue1 << 1) + (iRandValue2));
                                iRandValue3 = rndValue.Next(0, 5);      //DDR random value
                                yDDRReg0B = (byte)(iRandValue3 << 2);
                                DCCombinationList[(7 - i)].yChlDDRVal = (byte)iRandValue3;
                                bDDRReg0B = true;
                            }
                            else
                            {
                                bRet = false;   //just for case
                            }
                            if(bRet)
                            {
                                CmbinaRegList[0].yRegValue = yMarginVal;    //map to register all group UI
                                if (iRandValue1 != 0)
                                {
                                    DCCombinationList[(7 - i)].bMarginEnable = true;    //map to DC channels UI
                                }
                                if(iRandValue2 != 0)
                                {
                                    DCCombinationList[(7 - i)].bMarginValue = true;
                                }
                                if(bDDRReg0B)
                                {
                                    CmbinaRegList[0].yRegValue = (byte)iRandValue3;
                                }
                            }
                        }
                        else if((i == 3) || (i == 2))
                        {
                            iRandValue1 = rndValue.Next(0, 256);
                            yVIDRegVal = (byte)iRandValue1;
                            if(i == 3)
                            {
                                bVIDReg01 = true;
                            }
                            else if(i== 2)
                            {
                                bVIDReg02 = true;
                            }
                            else
                            {
                                bRet = false;   //just for case
                            }
                            if (bRet)
                            {
                                DCCombinationList[(7 - i)].yMarginHex = yVIDRegVal;
                                if (bVIDReg01)
                                {
                                    CmbinaRegList[1].yRegValue = yVIDRegVal;
                                }
                                if (bVIDReg02)
                                {
                                    CmbinaRegList[2].yRegValue = yVIDRegVal;
                                }
                            }
                        }
                        break;  //break for loop
                    }   //if((yTargetChls & iMask) != 0)
                }   //for (int i = 7; i >= 2; i--)
                #endregion

                #region Start to command(read/write) to Device
                //step 5, Write “Telemetry Selection Register” for the selected channel
                pmcntCmbDCAllReg05.parameterlist[0].phydata = (float)yValue;// yTargetChls;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
                CmbinaRegList[05].yRegValue = yValue;
                //step 6, Read “A/D Telemetry Register”
                bRet &= ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg06);
                MapADTelemetryValueToUI(yValue);

                //if(bOTP)    //thermal request
                //{
                //step 7, Write “Margining or VID Register or Control Register [b4:b1]” random value to change output voltage for the selected channel.
                if (bMargin)
                {
                    pmcntCmbDCAllReg00.parameterlist[0].phydata = (float)yMarginVal;
                    bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg00);
                }
                //else
                //{
                    //bRet = false;
                //}

                if (bVIDReg01)
                {
                    pmcntCmbDCAllReg01.parameterlist[0].phydata = (float)yVIDRegVal;
                    bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg01);
                }
                //else
                //{
                    //bRet = false;
                //}

                if (bVIDReg02)
                {
                    pmcntCmbDCAllReg02.parameterlist[0].phydata = (float)yVIDRegVal;
                    bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg02);
                }
                //else
                //{
                    //bRet = false;
                //}

                if (bDDRReg0B)
                {
                    pmcntCmbDCAllReg0B.parameterlist[0].phydata = (float)yDDRReg0B;
                    bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0B);
                }
                //else
                //{
                    //bRet = false;
                //}
                //}

                Thread.Sleep(1);
                //step 8, Read “General Status Register” to check PG
                //step 9, Read “Fault Register” to check Fault flag.
                bRet &= ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg0708);
                ReflectValueOnUIReg0708(yTargetChls);

                #endregion
            }


            //return yRet;
            return bRet;
        }

        private bool ClearAllRegister(ref TASKMessage tskmsgLotus)
        {
            bool bRet = true;

            if (pmcntCmbDCAllReg05.parameterlist[0].phydata != 0)
            {
                pmcntCmbDCAllReg05.parameterlist[0].phydata = 0;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
            }
            if (pmcntCmbDCAllReg00.parameterlist[0].phydata != 0)
            {
                pmcntCmbDCAllReg00.parameterlist[0].phydata = 0;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg00);
            }
            if(pmcntCmbDCAllReg01.parameterlist[0].phydata != 0)
            {
                pmcntCmbDCAllReg01.parameterlist[0].phydata = 0;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg01);
            }
            if(pmcntCmbDCAllReg02.parameterlist[0].phydata != 0)
            {
                pmcntCmbDCAllReg02.parameterlist[0].phydata = 0;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg02);
            }
            if(pmcntCmbDCAllReg0B.parameterlist[0].phydata != 0)
            {
                pmcntCmbDCAllReg0B.parameterlist[0].phydata = 0;
                bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0B);
            }

            return bRet;
        }

        private void ReflectValueOnUIReg0708(byte yTargetEnCh)
        {
            foreach(Parameter pmrReg in pmcntCmbDCAllReg0708.parameterlist)
            {
                if(((pmrReg.guid & 0x0000FFFF) >> 8) == 0x07)
                {
                    CmbinaRegList[0x07].yRegValue = (byte)pmrReg.hexdata;
                    if(CmbinaRegList[0x07].yRegValue != CmbinaRegList[0x0A].yRegValue)//yTargetEnCh)
                    {
                        m_LogList[m_LogList.Count - 2].strResult = string.Format("0x{0:X2}\t\t,False", CmbinaRegList[0x0A].yRegValue);
                    }
                    else
                    {
                        m_LogList[m_LogList.Count - 2].strResult = string.Format("0x{0:X2}\t\t,T", CmbinaRegList[0x0A].yRegValue);
                    }
                }
                else if(((pmrReg.guid & 0x0000FFFF) >> 8) == 0x08)
                {
                    CmbinaRegList[0x08].yRegValue = (byte)pmrReg.hexdata;
                    if(CmbinaRegList[0x08].yRegValue != 0)
                    {
                        m_LogList[m_LogList.Count - 1].strResult = string.Format("0x00\t\t,False");
						ctrParent.iErrorCount += 1;
                    }
                    else
                    {
                        m_LogList[m_LogList.Count - 1].strResult = string.Format("0x00\t\t,T");
                    }
                }
            }
            for(int i = 0; i<6; i++)
            {
                if ((CmbinaRegList[0x07].yRegValue & (byte)(1 << (7 - i))) != 0)
                    DCCombinationList[i].bChlPowerGood = true;
                else
                    DCCombinationList[i].bChlPowerGood = false;
                if ((CmbinaRegList[0x08].yRegValue & (byte)(1 << (7 - i))) != 0)
                    DCCombinationList[i].bChlFault = true;
                else
                    DCCombinationList[i].bChlFault = false;
            }
        }

        public bool ReadAllNMapToCmbRegister(ref TASKMessage tskmsgLotus)
        {
            bool bRet;
            int i = 0;

            bRet = ReadFromDevice(ref tskmsgLotus, pmcntCmbRegister);
            if(bRet)
            {
                foreach(Parameter pmrCmb in pmcntCmbRegister.parameterlist)
                {
                    FillValueInAllRegisterTab(pmrCmb, i);
                    i += 1;
                }
            }

            return bRet;
        }

        public bool EnableDCNWriteOut(ref TASKMessage tskmsgLotus, bool[] enChls)
        {
            bool bRet = true;
            int i = 0;
            byte yTargetChls = 0x00;
            //UInt16 uMarginVal = uInvalidValue;
            //UInt16 uDDRReg0B = uInvalidValue;
            //UInt16 uVIDReg01 = uInvalidValue;
            //UInt16 uVIDReg02 = uInvalidValue;
            ParamContainer pmWrite = new ParamContainer();

            //calculate the value which will be wrote Reg0x0A
            yTargetChls = 0x00;
            for (i = 0; i < enChls.Length; i++)
            {
                if (enChls[i] == true)
                {
                    yTargetChls |= (byte)(0x01 << (7 - i));
                    DCCombinationList[i].bChannelEnable = true;     //act on UI, Channel Enable
                    //if(i>=4)
                        //System.Windows.Forms.Application.DoEvents();
                }
            }

            #region check Reg00~Reg0B ParamContainer has assigned parameter, just for case
            if(pmcntCmbDCAllReg00.parameterlist.Count != 1)
            {
                return false;
            }
            if(pmcntCmbDCAllReg01.parameterlist.Count != 1)
            {
                return false;
            }
            if (pmcntCmbDCAllReg02.parameterlist.Count != 1)
            {
                return false;
            }
            if (pmcntCmbDCAllReg05.parameterlist.Count != 1)
            {
                return false;
            }
            if(pmcntCmbDCAllReg06.parameterlist.Count != 1)
            {
                return false;
            }
            if(pmcntCmbDCAllReg0708.parameterlist.Count != 2)
            {
                return false;
            }
            if(pmcntCmbDCAllReg0A.parameterlist.Count != 1)
            {
                return false;
            }
            if(pmcntCmbDCAllReg0B.parameterlist.Count != 1)
            {
                return false;
            }
            #endregion

            //step 2, Write “General Enable Register” to enable one or more or all DC/DC channels  
            pmcntCmbDCAllReg0A.parameterlist[0].phydata = (float)yTargetChls;
            CmbinaRegList[0x0A].yRegValue = yTargetChls;            //act on UI, register all group
            bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0A);
            Thread.Sleep(1);

            //step 3, Read “General Status Register” to check PG
            //step 4, Read “Fault Register” to check Fault flag.
            bRet &= ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg0708);
            ReflectValueOnUIReg0708(yTargetChls);

            //while (yTargetChls != 0)
            //{
                //step 5, Write “Telemetry Selection Register” for the selected channel
                //pmcntCmbDCAllReg05.parameterlist[0].phydata = (float)CalculateValueAccordingChannel(ref yTargetChls, uMarginVal, uDDRReg0B, uVIDReg01, uVIDReg02);
                //WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
            //}
            //step 10: repeat step 5 to step 9
            for (i = 0; i < enChls.Length; i++) 
            {
                if(enChls[i] == true)
                    bRet &= CalculateValueAccordingChannel(ref tskmsgLotus, ref yTargetChls);
            }
            /* (M150920)Move step 11-15 to another method, it will be called after 1 or more "Channel Enable"
            //step 11-13
            bRet &= CalculateValueAccordingChannel(ref tskmsgLotus, ref yTargetChls, true);

            //step 14, Read ALRT bit through “Control Register b0”
            bRet &= ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg0B);

            //step 15, Write 00h to “Margining register”, “DC-DC CH5 VID register”, “DC-DC CH6 VID register” and “Control register”
            bRet &= ClearAllRegister(ref tskmsgLotus);
            */

            //foreach(DCCombineModel dccmbmdlIn in DCCombinationList)
            //{
                //if(enChls[(int)dccmbmdlIn.iOrder] == true)
                //{

                //}
            //}

            return bRet;
        }

        public bool EnableOTPNClearReg(ref TASKMessage tskmsgLotus, bool[] enChls)
        {
            bool bRet = true;
            byte yTargetChls = 0x00;    //no used, just passed in CalculateValueAccordingChannel()

            //step 11-13
            bRet &= CalculateValueAccordingChannel(ref tskmsgLotus, ref yTargetChls, true);

            //step 14, Read ALRT bit through “Control Register b0”
            bRet &= ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg0B);
            CmbinaRegList[0x0B].yRegValue = (byte)pmcntCmbDCAllReg0B.parameterlist[0].hexdata;
            if ((CmbinaRegList[0x0B].yRegValue & 0x01) != 0)
            {
                DCCombinationList[0x07].strChannelName = "ALERT  = 1";
                m_LogList[m_LogList.Count - 1].strResult = string.Format("data AND 1\t,T");
            }
            else
            {
                DCCombinationList[0x07].strChannelName = "ALERT  = 0";
                m_LogList[m_LogList.Count - 2].strResult = string.Format("data AND 1\t,False");
				ctrParent.iErrorCount += 1;
            }
            Thread.Sleep(1);

            //step 15, Write 00h to “Margining register”, “DC-DC CH5 VID register”, “DC-DC CH6 VID register” and “Control register”
            bRet &= ClearAllRegister(ref tskmsgLotus);

            return bRet;
        }

        public bool DisableEnRegister(ref TASKMessage tskmsgLotus)
        {
            bool bRet = true;
            pmcntCmbDCAllReg0A.parameterlist[0].phydata = 0;
            bRet &= WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0A);
            foreach(CombinationRegister cmbRegIn in CmbinaRegList)
            {
                cmbRegIn.yRegValue = 0;
            }
            foreach(DCCombineModel dccmbmodIn in DCCombinationList)
            {
                dccmbmodIn.bChannelEnable = false;
                dccmbmodIn.bMarginEnable = false;
                dccmbmodIn.bMarginValue = false;
                dccmbmodIn.bChlFault = false;
                dccmbmodIn.bChlPowerGood = false;
                dccmbmodIn.yADTelemetryVal = 0;
                dccmbmodIn.yChlDDRVal = 0;
                dccmbmodIn.yMarginHex = 0;
            }

            return bRet;
        }

        #endregion

        #region Reliability methods

        public bool ReliableReadDC(ref TASKMessage tskmsgLotus, int iSelected, UInt16 iTimes)
        {
            bool bRet = true;
            //byte yEnValue = 0x00, yTelVolt = 0x00, yTelCurr = 0x00, yTelTherm = 0x0F;

            //yEnValue = (byte)(1 << (7- iSelected));
            //yTelVolt = (byte)(iSelected + 1);
            //yTelCurr = (byte)(iSelected + 0x09);

            DCCombinationList[iSelected].bChannelEnable = true;
            pmcntCmbDCAllReg0A.parameterlist[0].phydata = (float)(1 << (7 - iSelected));
            WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0A);
            //Thread.Sleep(1);
            for (int i = 0; i < iTimes; i++)
            {
                pmcntCmbDCAllReg05.parameterlist[0].phydata = (float)(iSelected + 1);
                WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
                Thread.Sleep(1);
                ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg06);
                DCCombinationList[iSelected].yADTelemetryVal = (byte)(pmcntCmbDCAllReg06.parameterlist[0].hexdata);
                if ((DCCombinationList[iSelected].yADTelemetryVal >= DCCombinationList[iSelected].yADTelVoltLowBound) &&
                    (DCCombinationList[iSelected].yADTelemetryVal <= DCCombinationList[iSelected].yADTelVoltHighBound))
                {
                    m_LogList[m_LogList.Count - 1].strResult = string.Format("0x{0:X2}-0x{1:X2}\t,T", DCCombinationList[iSelected].yADTelVoltLowBound, DCCombinationList[iSelected].yADTelVoltHighBound);
                }
                else
                {
                    m_LogList[m_LogList.Count - 1].strResult = string.Format("0x{0:X2}-0x{1:X2}\t,False", DCCombinationList[iSelected].yADTelVoltLowBound, DCCombinationList[iSelected].yADTelVoltHighBound);
                    ctrParent.iErrorCount += 1;
                }

                pmcntCmbDCAllReg05.parameterlist[0].phydata = (float)(iSelected + 0x09);
                WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
                Thread.Sleep(1);
                ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg06);
                DCCombinationList[iSelected].yADTelemetryVal = (byte)(pmcntCmbDCAllReg06.parameterlist[0].hexdata);
                //(M150923)Francis, don't know range, so remove it temporalilly
                //if ((DCCombinationList[iSelected].yADTelemetryVal >= DCCombinationList[iSelected].yADTelCurrLowBound) &&
                    //(DCCombinationList[iSelected].yADTelemetryVal <= DCCombinationList[iSelected].yADTelCurrHighBound))
                //{
                    //m_LogList[m_LogList.Count - 1].strResult = string.Format("T");
                //}
                //else
                //{
                    //m_LogList[m_LogList.Count - 1].strResult = string.Format("False");
                    //ctrParent.iErrorCount += 1;
                //}

                pmcntCmbDCAllReg05.parameterlist[0].phydata = (float)0x0F;
                WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
                Thread.Sleep(1);
                ReadFromDevice(ref tskmsgLotus, pmcntCmbDCAllReg06);
                DCCombinationList[6].yADTelemetryVal = (byte)(pmcntCmbDCAllReg06.parameterlist[0].hexdata);
                //(D150923)Francis, delete range checking when theraml A/D telemetry value return 
                //if ((DCCombinationList[iSelected].yADTelemetryVal >= DCCombinationList[iSelected].yADTelVoltLowBound) &&
                    //(DCCombinationList[iSelected].yADTelemetryVal <= DCCombinationList[iSelected].yADTelVoltHighBound))
                //{
                    //m_LogList[m_LogList.Count - 1].strResult = string.Format("T");
                //}
                //else
                //{
                    //m_LogList[m_LogList.Count - 1].strResult = string.Format("False");
                    //ctrParent.iErrorCount += 1;
                //}
            }
            Thread.Sleep(1);
            pmcntCmbDCAllReg0A.parameterlist[0].phydata = 0;
            WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg0A);
            DCCombinationList[iSelected].bChannelEnable = false;
            pmcntCmbDCAllReg05.parameterlist[0].phydata = 0F;
            WriteToDevice(ref tskmsgLotus, pmcntCmbDCAllReg05);
            DCCombinationList[iSelected].yADTelemetryVal = 0;
            DCCombinationList[6].yADTelemetryVal = 0;

            return bRet;
        }

        #endregion

        #region other private methods

        private bool GetDeviceInfor(ref TASKMessage tskmsgLotus)
        {
			tskmsgLotus.task = TM.TM_SPEICAL_GETREGISTEINFOR;
			devParent.AccessDevice(ref tskmsgLotus);
			while (tskmsgLotus.bgworker.IsBusy)
				System.Windows.Forms.Application.DoEvents();
			System.Windows.Forms.Application.DoEvents();
			if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
			{
				//return true;
			}
			else
			{
				return false;
			}

			tskmsgLotus.task = TM.TM_SPEICAL_GETDEVICEINFOR;
            devParent.AccessDevice(ref tskmsgLotus);
            while (tskmsgLotus.bgworker.IsBusy)
                System.Windows.Forms.Application.DoEvents();
            System.Windows.Forms.Application.DoEvents();
            if (tskmsgLotus.errorcode == LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

		private Parameter CreateNCloneParameter(Parameter pmtarget)
		{
			Parameter pmnew = new Parameter();

			pmnew.guid = pmtarget.guid;
			pmnew.phyref = pmtarget.phyref;
			pmnew.regref = pmtarget.regref;
			pmnew.subsection = pmtarget.subsection;
			pmnew.subtype = pmtarget.subtype;
			pmnew.reglist.Clear();
			Reg newReg = new Reg();
			newReg.address = pmtarget.reglist["Low"].address;
			newReg.bitsnumber = 8;
			newReg.startbit = 0;
			pmnew.reglist.Add("Low", newReg);

			return pmnew;
		}

		#endregion
    }
}
