using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ8513
{
    internal class DEMDataManage
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private List<UInt32> m_OcDelayList  = new List<UInt32>();
        private List<UInt32> m_ScDelayList  = new List<UInt32>();
        private List<UInt16> m_OcRegList    = new List<UInt16>();
        private List<UInt16> m_ScRegList    = new List<UInt16>();

        #endregion

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            if (parent.YFParamlist == null) return;
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter p)
        {
            return;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            UInt32 wdata = 0;
            float fval = 0;
            double resistor = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        wdata = Physical2Regular((float)p.phydata, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        float Thm_PullupRes = 0;
                        ret = GetThmPullupResistorFromImg(ref Thm_PullupRes);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.hexdata = ElementDefine.PARAM_HEX_ERROR;
                            break;
                        }

                        resistor = TempToResist(p.phydata);
                        fval = (float)((5000 * resistor + Thm_PullupRes * 1000 * 5000) / ((m_parent.pullupR + Thm_PullupRes) * 1000 + resistor));
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        fval = (float)((p.phydata + 50) * 16.0);
                        wdata = Physical2Regular(fval, p.regref, p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);

                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                    {
                        m_parent.ModifyTemperatureConfig(p, true);
                        break;
                    }
                default:
                    {
                        wdata = (UInt32)((double)(p.phydata * p.regref) / (double)p.phyref);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
            }
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            short  sdata = 0;
            UInt32 wdata = 0;
            Double ddata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_VOLTAGE:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP:
                    {
                        float Thm_PullupRes = 0;
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;

                        ret = GetThmPullupResistorFromImg(ref Thm_PullupRes);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        //p.phydata = (double)((p.phydata * (m_parent.pullupR + Thm_PullupRes) * 1000 - Thm_PullupRes * 1000 * 2500) / (2500 - p.phydata));
                        p.phydata = (double)((p.phydata * (Thm_PullupRes) * 1000) / (2500 - p.phydata));
                        p.phydata = ResistToTemp(p.phydata);//*/
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = (double)(p.phydata / 16 - 50);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_CURRENT:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = wdata;
                        p.phydata = Regular2Physical(wdata, p.regref, p.phyref);
                        p.phydata = (double)((p.phydata - 1000.0) * 1000 * 1000 / (9.8 * m_parent.rsense))*(-1.0);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_DOCTH:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        switch (wdata)
                        {
                            case 0:
                                ddata = 10;
                                break;
                            case 1:
                                ddata = 80;
                                break;
                            case 2:
                                ddata = 100;
                                break;
                            case 3:
                                ddata = 120;
                                break;
                            default:
                                p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                                return;
                        }
                        p.phydata = (double)(ddata * 1000 / m_parent.rsense);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_INT_TEMP_REFER:
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE:
                    {
                        m_parent.ModifyTemperatureConfig(p, false);
                        break;
                    }
				case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_SHORT:
					{
						ret = ReadFromRegImg(p, ref wdata);
						if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
						{
							p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
							break;
						}
						try
						{
							//if (!short.TryParse(wdata.ToString(), System.Globalization.NumberStyles.HexNumber, null, out sdata))
								//sdata = Convert(wdata);
								sdata = (short)wdata;
						}
						catch (Exception e)
						{
							sdata = 0;
						}
						p.phydata = sdata;
						break;
					}
                default:
                    {
                        ret = ReadFromRegImg(p, ref wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }

                      //  ddata = (Int32)wdata;

                      //  p.phydata = (double)((double)wdata * p.phyref / p.regref);

                        p.phydata = (double)((double)(Int32)wdata * p.phyref / p.regref);
                    }
                    break;
            }
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(UInt32 wVal, double RegularRef, double PhysicalRef)
        {
	        double dval, integer, fraction;

	        dval = (double)((double)(wVal*PhysicalRef)/(double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
	        if(fraction >= 0.5)
		        integer += 1;
	        else if(fraction <= -0.5)
		        integer -= 1;

	        return (double)integer;
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private double Regular2Physical(short sVal, double RegularRef, double PhysicalRef)
        {
            double dval, integer, fraction;

            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            else if (fraction <= -0.5)
                integer -= 1;

            return (double)integer;
        }

        /// <summary>
        /// 转换Physical -> Hex
        /// </summary>
        /// <param name="fVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        private UInt16 Physical2Regular(float fVal, double RegularRef, double PhysicalRef)
        {
	        UInt16 wval;
	        double dval, integer, fraction;

	        dval = (double)((double)(fVal*RegularRef)/(double)PhysicalRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
	        if(fraction>=0.5)
		        integer += 1;
	        if(fraction<=-0.5)
		        integer -= 1;
            wval = (UInt16)integer;

            return wval;
        }

        /// <summary>
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadFromRegImg(Parameter p, ref UInt32 pval)
        {
            UInt32 data;
            UInt32 hi = 0, lo = 0;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                {
                    regLow = dic.Value;
                    ret = ReadRegFromImg(regLow.address, p.guid, ref lo);
                    lo <<= (32 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadRegFromImg(regHi.address, p.guid, ref hi);
                    hi <<= (32 - regHi.bitsnumber - regHi.startbit); //align with left
                    hi >>= (32 - regHi.bitsnumber); //align with right
                }
            }

            data = (UInt32)(lo);// | ((UInt32)((UInt32)(hi))) << 32));
            data >>= (32 - regLow.bitsnumber); //align with right
   
            if(regHi != null)
            {
                data |= ((UInt32)((UInt32)(hi))) << regLow.bitsnumber;
            }

	        pval = (UInt32)data;
            p.hexdata = (UInt16)pval;
	        return ret;
        }

        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadSignedFromRegImg(Parameter p, ref short pval)
        {
            UInt32 wdata = 0, tr = 0;
            Int16 sdata=0;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            if (regHi != null)
                tr = (UInt32)(32 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt32)(32 - regLow.bitsnumber);
            /*
            wdata <<= tr;
            sdata = (Int16)wdata;
            sdata = (Int16)(sdata / (1 << tr));
            */
            pval = sdata;
            return ret;
        }


        /// <summary>
        /// 写数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <returns></returns>
        public UInt32 WriteToRegImg(Parameter p, UInt32 wVal)
        {
            UInt32  lomask = 0, himask = 0;
            UInt32 plo, phi, ptmp;
            UInt64 ultmp = 0;
            //byte hi = 0, lo = 0, tmp = 0;
            Reg regLow = null, regHi = null;
            UInt32 data = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            p.hexdata = (UInt16)wVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            ret = ReadRegFromImg(regLow.address, p.guid, ref data);
            if (regHi == null)
            {
                
                if (regLow.bitsnumber < 32)
                    lomask = (UInt32)((1 << regLow.bitsnumber) - 1);// use 1<<32 will have limitation for 32bits 
                else
                    lomask = 0xFFFFFFFF;

                /*
                 *This is solution for extention from 32bits to 64 bits
                ultmp = 1;
                ultmp <<= regLow.bitsnumber;
                ultmp -= 1;
                lomask = (UInt32)ultmp;
                //lomask = (UInt32)((UInt64)((1 << regLow.bitsnumber) - 1));
                 * */
                lomask <<= regLow.startbit;
                data &= (UInt32)(~lomask);
                data |= (UInt32)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data);
            }
            else
            {

                lomask = (UInt32)((1 << regLow.bitsnumber) - 1);
                plo = (UInt32)(wVal & lomask);
                himask = (UInt32)((1 << regHi.bitsnumber) - 1);
                himask <<= regLow.bitsnumber;
                phi = (UInt32)((wVal & himask) >> regLow.bitsnumber);

                //mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                lomask <<= regLow.startbit;
                ptmp = (UInt32)(data & ~lomask);
                ptmp |= (UInt32)(plo << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, ptmp);

                ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                himask = (UInt32)((1 << regHi.bitsnumber) - 1);
                himask <<= regHi.startbit;
                ptmp = (UInt32)(data & ~himask);
                ptmp |= (UInt32)(phi << regHi.startbit);
                WriteRegToImg(regHi.address, p.guid, ptmp);

            }

            return ret;
        }

        /// <summary>
        /// 写有符号数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <param name="pChip"></param>
        /// <returns></returns>
        private UInt32 WriteSignedToRegImg(Parameter p, Int16 sVal)
        {
            UInt16 wdata, tr = 0;
            Int16 sdata;
            Reg regLow = null, regHi = null;

            sdata = sVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }
            if (regHi != null)
                tr = (UInt16)(16 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt16)(16 - regLow.bitsnumber);

            sdata *= (Int16)(1 << tr);
            wdata = (UInt16)sdata;
            wdata >>= tr;

            return WriteToRegImg(p, wdata);
        }

        private void WriteToRegImgError(Parameter p, UInt32 err)
        { 
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt32 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.PARAElement:
                    {
                        pval = parent.m_ParaRegImg[reg].val;
                        ret = parent.m_ParaRegImg[reg].err;
                        break;
                    }

                case ElementDefine.TrimElement:
                    {
                        pval = parent.m_TrimRegImg[reg].val;
                        ret = parent.m_TrimRegImg[reg].err;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        pval = (ushort)parent.m_OpRegImg[reg].val;
                        ret = parent.m_OpRegImg[reg].err;
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }

        private void WriteRegToImg(UInt16 reg, UInt32 guid, UInt32 value)
        {
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.PARAElement:
                    {
                        parent.m_ParaRegImg[reg].val = value;
                        parent.m_ParaRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }

                case ElementDefine.TrimElement:
                    {
                        parent.m_TrimRegImg[reg].val = value;
                        parent.m_TrimRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                case ElementDefine.OperationElement:
                    {
                        parent.m_OpRegImg[reg].val = value;
                        parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
                        break;
                    }
                default:
                    break;
            }
        }

        private UInt32 GetThmPullupResistorFromImg(ref float pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte val = 0;

            val = (byte)(SharedFormula.LoByte((ushort)parent.m_OpRegImg[0x03].val) & 0x03);
            switch (val)
            {
                case 0:
                    pval = 3;
                    break;
                case 1:
                    pval = 60; 
                    break;
                default:
                    pval = 0;
                    break;
            }

            ret = parent.m_OpRegImg[0x03].err;
            return ret;
        }
        #endregion   

        #region 外部温度转换

        public double ResistToTemp(double resist)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }
            return SharedFormula.ResistToTemp(resist, m_TempVals, m_ResistVals);
        }

        public double TempToResist(double temp)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype == ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }

            return SharedFormula.TempToResist(temp, m_TempVals, m_ResistVals);
        }
        #endregion   
    }
}
