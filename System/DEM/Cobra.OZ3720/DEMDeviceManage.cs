using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using Cobra.Common;

namespace Cobra.Azalea20
{
    public class DEMDeviceManage : IDEMLib
    {
        #region 定义参数subtype枚举类型
        private double m_Rsense;
        internal double rsense
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpRsense);
                if (param == null) return 2500.0;
                else return (param.phydata * 1000.0);
            }
        }

        internal ParamContainer EFParamlist = null;
        internal ParamContainer tempParamlist = null;
        internal ParamContainer OperationParamlist = null;

        internal BusOptions  m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_EFRegImg = new COBRA_HWMode_Reg[ElementDefine.EFUSE_MEMORY_SIZE];
        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE]; 
        internal Dictionary<UInt32, Tuple<Parameter, Parameter>> m_guid_slope_offset = new Dictionary<UInt32, Tuple<Parameter, Parameter>>();
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

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

            m_HwMode_RegList.Add(ElementDefine.EFUSEElement, m_EFRegImg);
            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);

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
            string str = string.Empty;
            Parameter param = null;
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            EFParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.EFUSEElement);
            if (EFParamlist == null) return;

            OperationParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (EFParamlist == null) return;

            param = OperationParamlist.GetParameterByGuid(ElementDefine.OP_Doc1p_TH);
            if(param != null)
            {
                param.itemlist.Clear();
                for(int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F3}mv", 5.0 + i * 0.625);
                    param.itemlist.Add(str);
                }
            }
            param = OperationParamlist.GetParameterByGuid(ElementDefine.OP_Cocp_TH);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 256; i++)
                {
                    str = string.Format("{0:F3}mv", 2.5 + i * 0.3125);
                    param.itemlist.Add(str);
                }
            }
            param = OperationParamlist.GetParameterByGuid(ElementDefine.OP_Cell_cto_on_time);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0}time_unit", (i+1));
                    param.itemlist.Add(str);
                }
            }
            param = OperationParamlist.GetParameterByGuid(ElementDefine.OP_Cell_cto_off_time);
            if (param != null)
            {
                param.itemlist.Clear();
                param.itemlist.Add("Do ADC at once");
                for (int i = 1; i < 256; i++)
                {
                    str = string.Format("{0}time_unit", (i + 1));
                    param.itemlist.Add(str);
                }
            }
            param = OperationParamlist.GetParameterByGuid(ElementDefine.inter_settling_time);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0}time_unit", (2*i + 24));
                    param.itemlist.Add(str);
                }
            }
            param = OperationParamlist.GetParameterByGuid(ElementDefine.dda_settling_time);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0}time_unit", (i + 24));
                    param.itemlist.Add(str);
                }
            }
            param = OperationParamlist.GetParameterByGuid(ElementDefine.thm_settling_time);
            if (param != null)
            {
                param.itemlist.Clear();
                for (int i = 0; i < 16; i++)
                {
                    str = string.Format("{0}time_unit", (i + 64));
                    param.itemlist.Add(str);
                }
            }
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
            for (byte i = 0; i < ElementDefine.EFUSE_MEMORY_SIZE; i++)
            {
                m_EFRegImg[i] = new COBRA_HWMode_Reg();
                m_EFRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_EFRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }

            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
            m_guid_slope_offset.Add(ElementDefine.OP_CELL1V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL1V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL1V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL2V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL2V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL2V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL3V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL3V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL3V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL4V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL4V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL4V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL5V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL5V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL5V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL6V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL6V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL6V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL7V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL7V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL7V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL8V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL8V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL8V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL9V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL9V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL9V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL10V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL10V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL10V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL11V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL11V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL11V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL12V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL12V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL12V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL13V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL13V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL13V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL14V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL14V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL14V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL15V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL15V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL15V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL16V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL16V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL16V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL17V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL17V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL17V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL18V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL18V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL18V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL19V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL19V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL19V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CELL20V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL20V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CELL20V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_ISENS_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_ISENS_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_ISENS_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_VBATT_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_VBATT_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_VBATT_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_CADC, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_CADC_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_CADC_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_THM0V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM0V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM0V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_THM1V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM1V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM1V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_THM2V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM2V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM2V_OFFSET)));
            m_guid_slope_offset.Add(ElementDefine.OP_THM3V_8, Tuple.Create(OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM3V_SLOP), OperationParamlist.GetParameterByGuid(ElementDefine.OP_THM3V_OFFSET)));

        }

        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            return m_dem_dm.WriteToRegImg(p, wVal);
        }

        public UInt32 GetThmCrrtSel(UInt32 guid,ref byte crrtSel)
        {
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte reg = (byte)((guid & 0x0000FF00) >> 8);

            ret = m_dem_bm.ReadWord(0x11,ref wval);
            if(ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            wval &= 0xFFFC;
            wval |= 0x02;
            ret = m_dem_bm.WriteWord(0x11, wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            Thread.Sleep(5);
            ret = m_dem_bm.ReadWord(reg, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            if (wval < 14000)
            {
                crrtSel = 120;
                m_OpRegImg[reg].val = wval;
                m_OpRegImg[reg].err = ret;
            }
            else
            {
                ret = m_dem_bm.ReadWord(0x11, ref wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                wval &= 0xFFFC;
                wval |= 0x01;
                ret = m_dem_bm.WriteWord(0x11, wval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                Thread.Sleep(5);
                ret = m_dem_bm.ReadWord(reg, ref wval);
                crrtSel = 20;
                m_OpRegImg[reg].val = wval;
                m_OpRegImg[reg].err = ret;
            }
            return ret;
        }
    }
}

