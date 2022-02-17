using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Cobra.Common;
using Cobra.Communication;

namespace Cobra.Lotus
{
	public class LotusRegClass
	{
		public byte yVal;
		public UInt32 dwErr;
	}

	public class DEMDeviceManage : IDEMLib
	{
		public BusOptions m_busopLotus = null;
		internal DeviceInfor m_devinforLotus = null;
		internal ParamListContainer m_plstcntSectionParameter = null;
		internal ParamListContainer m_plstcntSFLAll = null;

		private CommandLotus m_CmdLotus = new CommandLotus();									//Lotus register handling
		private List<Parameter> m_lstpmLotus = new List<Parameter>();								//Lotus register temperary buffer

		public object m_lock = new object();
        public CCommunicateManager m_Interface = new CCommunicateManager();
        private Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<UInt32, string>()
        {
            {DefineLotus.IDS_ERR_DEMSUNCH_PARAMETER_XML,"DeviceDescription XML has something lost, please contact Engineer."},
            {DefineLotus.IDS_ERR_DEMSUNCH_PARAMCONTAINT_EMPTY,"Parameter list has lost, please contact Engineer."},
            {DefineLotus.IDS_ERR_DEMSUNCH_SUBTYPE,"Subtype value setting error in XML, it should be in reasonable transfer number, please contact Engineer."},
            {DefineLotus.IDS_ERR_DEMSUNCH_LOCATIONLIST,"LocationList should have Low address only in xml, please contact Engineer."},
            {DefineLotus.IDS_ERR_DEMSUNCH_ELEMENT_GUID,"Element Guid has error in xml, please contact Engineer."},
            {DefineLotus.IDS_ERR_DEMSUNCH_VR_ADDRESS,"Wrong VR address definition in DeviceDescriptor.xml, please contact Engineer."}
        };

		#region Implementation of Interface definition function

		public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
		{
			m_busopLotus = busoptions;
			m_plstcntSectionParameter = deviceParamlistContainer;
			m_plstcntSFLAll = sflParamlistContainer;

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);
			LotusDEMCreateInterface();
			m_CmdLotus.Init(this);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.DEM);
            LibErrorCode.UpdateDynamicalLibError(ref m_dynamicErrorLib_dic);
		}

		public bool EnumerateInterface()
		{
			return LotusDEMEnumerateInterface();
		}

		public bool CreateInterface()
		{
			return LotusDEMCreateInterface();
		}

        public bool DestroyInterface()
        {
            return LotusDEMDestroyInterface();
        }

		public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
		{
			return m_CmdLotus.GetDeviceInfor(ref deviceinfor);
		}

		public UInt32 Erase(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD:, return successful?

			return dwRet;
		}

		public UInt32 BlockMap(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD:, return successful?

			return dwRet;
		}

		public UInt32 Command(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD: should be default an error 
			SetupLotusListBuffer(ref bgworker);
			if (m_lstpmLotus.Count != 0)
			{
				m_CmdLotus.Command(m_lstpmLotus);
			}
			else
			{
				//TBD
                dwRet = DefineLotus.IDS_ERR_DEMSUNCH_VR_ADDRESS;
			}

			return dwRet;
		}

		public UInt32 Read(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD: should be default an error 

			SetupLotusListBuffer(ref bgworker);
			dwRet = m_CmdLotus.ReadByte(m_lstpmLotus);

			return dwRet;
		}

		public UInt32 Write(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD: should be default an error 

			SetupLotusListBuffer(ref bgworker);
			dwRet |= m_CmdLotus.WriteByte(m_lstpmLotus);

			return dwRet;
		}

		public UInt32 BitOperation(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD:, return successful?

			return dwRet;
		}

		public UInt32 ConvertHexToPhysical(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD: should be default an error 
			Parameter psi;

			SetupLotusListBuffer(ref bgworker);
			if (m_lstpmLotus.Count != 0)
			{
				for (int i = 0; i < m_lstpmLotus.Count; i++)
				{
					psi = m_lstpmLotus[i];
					if (psi == null) continue;

					m_CmdLotus.HexToPhysical(ref psi);
				}
			}

			return dwRet;
		}

		public UInt32 ConvertPhysicalToHex(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;		//TBD: should be default an error 
			Parameter psi;

			SetupLotusListBuffer(ref bgworker);
			if (m_lstpmLotus.Count != 0)
			{
				for (int i = 0; i < m_lstpmLotus.Count; i++)
				{
					psi = m_lstpmLotus[i];
					if (psi == null) continue;

					m_CmdLotus.PhysicalToHex(ref psi);
				}
			}

			return dwRet;
		}

		public UInt32 GetSystemInfor(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;			//TBD:, return successful?

			return dwRet;
		}

		public UInt32 GetRegisteInfor(ref TASKMessage bgworker)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;			//TBD:, return successful?

			return dwRet;
		}

		public void UpdataDEMParameterList(Parameter p)
		{
			return;
		}

		#endregion

		/*
		#region publice method body
		public UInt32 FindParameterByGUID(UInt32 dwTargetGUID, ref Parameter pmOut)
		{
			ParamContainer pmcntTmp = null;
			UInt32 dwErr = LibErrorCode.IDS_ERR_DEMSUNCH_PARAMETER_XML;

			if (m_plstcntSectionParameter != null)
			{
				pmcntTmp = m_plstcntSectionParameter.GetParameterListByGuid(LotusDefine.OperationElement);
				if (pmcntTmp != null)
				{
					pmOut = pmcntTmp.GetParameterByGuid(dwTargetGUID);
					if (pmOut != null)
					{
						dwErr = LibErrorCode.IDS_ERR_SUCCESSFUL;
					}
				}
			}

			return dwErr;
		}

		#endregion
		*/

		#region private method body

		#region Communication Layer Operation

		private bool LotusDEMCreateInterface()
		{
			bool bPort = LotusDEMEnumerateInterface();
			if (!bPort) return false;

			return m_Interface.OpenDevice(ref m_busopLotus);
		}

		public bool LotusDEMDestroyInterface()
		{
			return m_Interface.CloseDevice();
		}

		public bool LotusDEMEnumerateInterface()
		{
			return m_Interface.FindDevices(ref m_busopLotus);
		}

		#endregion

		private UInt32 SetupLotusListBuffer(ref TASKMessage tskmsgIn)
		{
			UInt32 dwRet = LibErrorCode.IDS_ERR_SUCCESSFUL;
			ParamContainer pmcntTmp = tskmsgIn.task_parameterlist;

			m_lstpmLotus.Clear();

			if (pmcntTmp == null)
			{
				dwRet = DefineLotus.IDS_ERR_DEMSUNCH_PARAMCONTAINT_EMPTY;
				return dwRet;
			}

			foreach (Parameter p in pmcntTmp.parameterlist)
			{
				if (p == null)
				{
                    dwRet = DefineLotus.IDS_ERR_DEMSUNCH_PARAMCONTAINT_EMPTY;
					break;
				}
				else if ((p.subsection == DefineLotus.SubSectionLotus))
				{
					m_lstpmLotus.Add(p);
				}
				else
				{
                    dwRet = DefineLotus.IDS_ERR_DEMSUNCH_PARAMCONTAINT_EMPTY;	//TBD
					break;
				}
			}

			return dwRet;

		}

		#endregion
	}
}
