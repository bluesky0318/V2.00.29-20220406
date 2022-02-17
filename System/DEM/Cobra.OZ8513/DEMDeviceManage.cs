using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Cobra.Common;

namespace Cobra.OZ8513
{
    #region 硬件模式寄存器结构定义
    public class COBRA_HWMode_Reg
    {
        public UInt32 val;
        public UInt32 err;
    }
    #endregion

    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        private double m_PullupR;
        internal double pullupR
        {
            get
            {
            //    Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpETPullupR);
            //    if (param == null) return 0.0;
            //    else return param.phydata;
                return m_PullupR; 
            }
            //set { m_PullupR = value; }
        }

        private double m_Rsense;
        internal double rsense
        {
            get
            {
                //Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpRsense);
                //if (param == null) return 2500.0;
                //else return (param.phydata * 1000.0);
                return m_Rsense;
            }
            //set { m_PullupR = value; }
        }

        internal ParamContainer YFParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions  m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_TrimRegImg = new COBRA_HWMode_Reg[ElementDefine.TRIM_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_ParaRegImg = new COBRA_HWMode_Reg[ElementDefine.PARA_MEMORY_SIZE];

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
        //    tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
        //    if (tempParamlist == null) return;

            YFParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.PARAElement);
            if (YFParamlist == null) return;

            //pullupR = tempParamlist.GetParameterByGuid(ElementDefine.TpETPullupR).phydata;
            //itv0 = tempParamlist.GetParameterByGuid(ElementDefine.TpITSlope).phydata;
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
            for (short i = 0; i < ElementDefine.TRIM_MEMORY_SIZE; i++)
            {
                m_TrimRegImg[i] = new COBRA_HWMode_Reg();
                m_TrimRegImg[i].val = 0;// ElementDefine.PARAM_HEX_ERROR;
                m_TrimRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            for (short i = 0; i < ElementDefine.PARA_MEMORY_SIZE; i++)
            {
                m_ParaRegImg[i] = new COBRA_HWMode_Reg();
                m_ParaRegImg[i].val = 0;// ElementDefine.PARAM_HEX_ERROR;
                m_ParaRegImg[i].err = LibErrorCode.IDS_ERR_SUCCESSFUL; ;
            }
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = 0;// ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }
    }
}

