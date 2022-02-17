using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;


namespace Cobra.OZ8513U
{
    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        internal ParamContainer tempParamlist = null;
        internal ParamContainer OpParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal Dictionary<byte, List<OZ8513U_REG>> m_OpRegImg = new Dictionary<byte, List<OZ8513U_REG>>();
        private DEMBehaviorManage m_dem_bm = new DEMBehaviorManage();
        private DEMDataManage m_dem_dm = new DEMDataManage();
        #endregion

        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);
            InitialImgReg();
            m_dem_bm.Init(this);
            m_dem_dm.Init(this);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.DEM);
        }

        public bool EnumerateInterface()
        {
            return m_dem_bm.EnumerateInterface();
        }

        public bool CreateInterface()
        {
            return m_dem_bm.CreateInterface();
        }

        public bool DestroyInterface()
        {
            return m_dem_bm.DestroyInterface();
        }

        public void UpdataDEMParameterList(Parameter p)
        {
            m_dem_dm.UpdateEpParamItemList(p);
        }

        public void Physical2Hex(ref Parameter param)
        {
            m_dem_dm.Physical2Hex(ref param);
        }

        public void Hex2Physical(ref Parameter param)
        {
            m_dem_dm.Hex2Physical(ref param);
        }

        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            return m_dem_bm.GetDeviceInfor(ref deviceinfor);
        }

        public UInt32 Erase(ref TASKMessage bgworker)
        {
            return m_dem_bm.EraseEEPROM(ref bgworker);
        }

        public UInt32 BlockMap(ref TASKMessage bgworker)
        {
            return m_dem_bm.EpBlockRead();
        }

        public UInt32 Command(ref TASKMessage bgworker)
        {
            return m_dem_bm.Command(ref bgworker);
        }

        public UInt32 Read(ref TASKMessage bgworker)
        {
            return m_dem_bm.Read(ref bgworker);
        }

        public UInt32 Write(ref TASKMessage bgworker)
        {
            return m_dem_bm.Write(ref bgworker);
        }

        public UInt32 BitOperation(ref TASKMessage bgworker)
        {
            return m_dem_bm.BitOperation(ref bgworker);
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage bgworker)
        {
            return m_dem_bm.ConvertHexToPhysical(ref bgworker);
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage bgworker)
        {
            return m_dem_bm.ConvertPhysicalToHex(ref bgworker);
        }

        public UInt32 GetSystemInfor(ref TASKMessage bgworker)
        {
            return m_dem_bm.GetSystemInfor(ref bgworker);
        }

        public UInt32 GetRegisteInfor(ref TASKMessage bgworker)
        {
            return m_dem_bm.GetRegisteInfor(ref bgworker);
        }
        #endregion

        /// <summary>
        /// 解析Section参数列表
        /// </summary>
        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
        }

        public void ModifyTemperatureConfig(Parameter p, bool bConvert)
        {
            //bConvert为真 physical ->hex;假 hex->physical;
            Parameter tmp = tempParamlist.GetParameterByGuid(p.guid);
            if (tmp == null) return;
            if (bConvert)
                tmp.phydata = p.phydata;
            else
                p.phydata = tmp.phydata;
        }

        private void InitialImgReg()
        {
            byte dataType = 0x00;
            List<OZ8513U_REG> Reglist;

            Reg reg = null;
            OZ8513U_REG oz8513_reg = null;

            OpParamlist = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement);
            if (OpParamlist == null) return;

            foreach (Parameter p in OpParamlist.parameterlist)
            {
                if (p == null) break;
                dataType = (byte)((p.guid & ElementDefine.CommandMask) >> 16);
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    reg = dic.Value;
                    if (!m_OpRegImg.ContainsKey(dataType))
                    {
                        Reglist = new List<OZ8513U_REG>();
                        oz8513_reg = new OZ8513U_REG(ref reg);
                        Reglist.Add(oz8513_reg);
                        m_OpRegImg.Add(dataType, Reglist);
                    }
                    else
                    {
                        oz8513_reg = new OZ8513U_REG(ref reg);
                        m_OpRegImg[dataType].Add(oz8513_reg);
                    }
                }
            }
        }

        public OZ8513U_REG FindRegOnImgReg(byte dataType,Reg oreg)
        {
            if (!m_OpRegImg.ContainsKey(dataType)) return null;

            List<OZ8513U_REG> list = m_OpRegImg[dataType];
            if ((list == null)|(list.Count == 0)) return null;

            foreach (OZ8513U_REG ur in list)
            {
                if (ur == null) continue;
                if ((ur.reg.address == oreg.address) && (ur.reg.startbit == oreg.startbit) && (ur.reg.bitsnumber == oreg.bitsnumber)) return ur;
            }
            return null;
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
    }
}

