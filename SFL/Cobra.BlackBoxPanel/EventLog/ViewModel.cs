using Cobra.Common;
using Cobra.EM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Cobra.BlackBoxPanel.EventLog
{
    class ViewModel
    {
        private Device m_device_parent;
        public Device device_parent
        {
            get { return m_device_parent; }
            set { m_device_parent = value; }
        }

        private string m_SFLname;
        public string sflname
        {
            get { return m_SFLname; }
            set { m_SFLname = value; }
        }

        private ParamContainer m_pmcntDMParameterList = new ParamContainer();
        public ParamContainer pmcntDMParameterList
        {
            get { return m_pmcntDMParameterList; }
            set { m_pmcntDMParameterList = value; }
        }

        private AsyncObservableCollection<Model> m_RegisterList = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> RegisterList
        {
            get { return m_RegisterList; }
            set { m_RegisterList = value; }
        }
        public ListCollectionView lstclRegisterList { get; set; } 
        public ViewModel(object pParent, string name)
        {
            #region 相关初始化
            device_parent = (Device)pParent;
            if (device_parent == null) return;

            sflname = name;
            if (String.IsNullOrEmpty(sflname)) return;
            #endregion
            pmcntDMParameterList = device_parent.GetParamLists(sflname);
            foreach (Parameter param in pmcntDMParameterList.parameterlist)
            {
                if (param == null) continue;
                ParseOpRegParameterToData(param);
            }
        }

        private bool ParseOpRegParameterToData(Parameter paramIn)
        {
            UInt16 udata = 0;
            byte ydata = 0;
            bool bdata = false;
            byte yValid = 0;            //id=647

            if (!paramIn.sfllist[sflname].nodetable.ContainsKey("Catalog")) return false;
            if (!UInt16.TryParse(paramIn.sfllist[sflname].nodetable["Catalog"].ToString(), out udata)) return false;
            if (udata != (UInt16)LOG_TYPE.EVENT_LOG) return false;

            XMLData xmlData = new XMLData();
            foreach (DictionaryEntry de in paramIn.sfllist[sflname].nodetable)
            {
                switch (de.Key.ToString())
                {
                    case "BitTotal":
                        {
                            if (!Byte.TryParse(de.Value.ToString(), NumberStyles.HexNumber, null as IFormatProvider, out ydata))
                            {
                                xmlData.yTotal = 0x08;
                            }
                            else
                            {
                                xmlData.yTotal = Convert.ToByte(de.Value.ToString(), 16);
                            }
                            break;
                        }
                    case "Index":
                        {
                            xmlData.u32Index = Convert.ToUInt32(de.Value.ToString(), 16);
                            yValid++;
                            break;
                        }
                    case "BitStart":
                        {
                            xmlData.yBitStart = Convert.ToByte(de.Value.ToString(), 16);
                            yValid++;
                            break;
                        }
                    case "Length":
                        {
                            xmlData.yLength = Convert.ToByte(de.Value.ToString(), 16);
                            yValid++;
                            break;
                        }
                    case "Value":
                        {
                            break;
                        }
                    case "Read":
                        {
                            if (!Boolean.TryParse(de.Value.ToString(), out bdata))
                            {
                                xmlData.bRead = false;
                            }
                            else
                            {
                                xmlData.bRead = bdata;
                            }
                            break;
                        }
                    case "Write":
                        {
                            if (!Boolean.TryParse(de.Value.ToString(), out bdata))
                            {
                                xmlData.bWrite = false;
                            }
                            else
                            {
                                xmlData.bWrite = bdata;
                            }
                            break;
                        }
                    case "Description":
                        {
                            xmlData.strDescrip = de.Value.ToString();
                            break;
                        }
                    case "Tips":
                        {
                            xmlData.strBitTips = de.Value.ToString();
                            break;
                        }
                    case "Group":
                        {
                            xmlData.strGroup = de.Value.ToString();
                            break;
                        }
                    case "Unit":
                        {
                            xmlData.strUnit = de.Value.ToString();
                            break;
                        }
                    case "RegisterName":
                        {
                            xmlData.strRegName = de.Value.ToString();
                            break;
                        }
                    case "bPhyDataFromList":
                        {
                            if (!Boolean.TryParse(de.Value.ToString(), out bdata))
                            {
                                xmlData.bPhyDataFromList = false;
                            }
                            else
                            {
                                xmlData.bPhyDataFromList = bdata;
                            }
                            break;
                        }

                }   //switch
            }

            xmlData.pmrXDParent = paramIn;
            xmlData.u32Guid = paramIn.guid;
            ConvertXMLDataToModel(ref xmlData);
            lstclRegisterList = new ListCollectionView(RegisterList);
            lstclRegisterList.GroupDescriptions.Add(new PropertyGroupDescription("strGroupReg"));
            return true;
        }

        private void ConvertXMLDataToModel(ref XMLData xmldataIn)
        {
            Model mdltemp;
            bool bAdd = false;
            byte yBitStartLoc = xmldataIn.yBitStart;
            byte yBitLength = 0;
            byte yTotalLeng = (byte)(xmldataIn.yTotal & 0x38);
            byte yLoHi = (byte)(xmldataIn.yTotal & 0xE0);
            int iLoopParse = 0;
            UInt32 u32TargetIndex = 0;
            byte i, j;
            AsyncObservableCollection<Model> Listtmp = null;

            Listtmp = RegisterList;
            if ((yTotalLeng != 0x08) && (yTotalLeng != 0x10) && (yTotalLeng != 0x20))
            {
                yTotalLeng = 0x08;
            }

            iLoopParse = (int)((xmldataIn.yLength - 1) / yTotalLeng);
            if (iLoopParse >= 1)
            {
                System.Windows.Forms.Application.DoEvents();
            }

            for (j = 0; j <= iLoopParse; j++)
            {
                if (yLoHi == 0)
                {
                    u32TargetIndex = (UInt32)(xmldataIn.u32Index + j);
                }
                else
                {
                    u32TargetIndex = (UInt32)(xmldataIn.u32Index - j);
                }
                mdltemp = SearchExpModelByIndex(u32TargetIndex, xmldataIn, Listtmp);
                if ((u32TargetIndex >= 0x30) && (u32TargetIndex <= 0x3f))
                {
                    u32TargetIndex -= 1;
                    u32TargetIndex += 1;
                }

                if (mdltemp == null)
                {
                    mdltemp = new Model();
                    bAdd = true;
                }

                if (xmldataIn.yLength <= ((j + 1) * yTotalLeng - yBitStartLoc))
                {
                    yBitLength = (byte)(xmldataIn.yLength - yBitLength);
                }
                else
                {   //if not
                    if (j == 0)
                    {
                        yBitLength = (byte)(yTotalLeng - yBitStartLoc);
                    }
                    else
                    {
                        if (xmldataIn.yLength <= ((j + 1) * yTotalLeng - xmldataIn.yBitStart))
                        {
                            yBitLength = (byte)(xmldataIn.yLength - (yTotalLeng - xmldataIn.yBitStart));
                        }
                        else
                        {
                            yBitLength = yTotalLeng;
                        }
                    }
                }

                for (i = 0; i < yBitLength; i++)
                {
                    mdltemp.ArrRegComponet[i + yBitStartLoc].yBitValue = 0;
                    mdltemp.ArrRegComponet[i + yBitStartLoc].bBitEnable = (xmldataIn.bRead | xmldataIn.bWrite);
                    mdltemp.ArrRegComponet[i + yBitStartLoc].bRead = xmldataIn.bRead;
                    mdltemp.ArrRegComponet[i + yBitStartLoc].bWrite = xmldataIn.bWrite;
                    if (mdltemp.ArrRegComponet[i + yBitStartLoc].bBitEnable)
                        mdltemp.ArrRegComponet[i + yBitStartLoc].strBitDescrip = xmldataIn.strDescrip;
                    else
                        mdltemp.ArrRegComponet[i + yBitStartLoc].strBitDescrip = BitComponent.BitDescrpDefault;
                    mdltemp.ArrRegComponet[i + yBitStartLoc].strBitTips = xmldataIn.strBitTips;
                    mdltemp.ArrRegComponet[i + yBitStartLoc].strUnit = xmldataIn.strUnit;
                    if (i == 0)
                    {
                        mdltemp.ArrRegComponet[i + yBitStartLoc].yDescripVisiLgth = yBitLength;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].pmrBitParent = xmldataIn.pmrXDParent;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].u32Guid = xmldataIn.u32Guid;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].strGroupBit = xmldataIn.strGroup;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].dbPhyValue = 0.0;  //(A141219)Francis, assign physical value a default value
                        mdltemp.ArrRegComponet[i + yBitStartLoc].expXMLdataParent = xmldataIn;
                    }
                    else
                    {
                        mdltemp.ArrRegComponet[i + yBitStartLoc].yDescripVisiLgth = 0;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].pmrBitParent = null;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].u32Guid = 0;
                        mdltemp.ArrRegComponet[i + yBitStartLoc].strGroupBit = "";
                    }
                }

                if (bAdd)
                {
                    mdltemp.u32RegNum = u32TargetIndex;
                    mdltemp.strRegNum = string.Format("0x{0:X8}", mdltemp.u32RegNum);
                    mdltemp.yRegLength = yTotalLeng;
                    mdltemp.strGroupReg = xmldataIn.strGroup;
                    Listtmp.Add(mdltemp);
                    bAdd = false;
                    //(A150106)Francis
                    if ((yTotalLeng == xmldataIn.yLength) && (xmldataIn.strRegName.Length == 0))
                    {
                        xmldataIn.strRegName = xmldataIn.strDescrip;
                        mdltemp.strRegName = xmldataIn.strDescrip;
                    }
                    else if (yTotalLeng < xmldataIn.yLength)
                    {
                        xmldataIn.strRegName = xmldataIn.strDescrip;
                        if (j != iLoopParse)
                        {
                            mdltemp.strRegName = xmldataIn.strDescrip + "__L";
                        }
                        else
                        {
                            mdltemp.strRegName = xmldataIn.strDescrip + "__H";
                        }
                    }
                    else
                    {
                        mdltemp.strRegName = xmldataIn.strRegName;
                    }
                    //(E150106)
                }   //if (bAdd)
                else
                {
                    if (mdltemp.strRegName.Length == 0)
                        mdltemp.strRegName = BitComponent.RegDescrpDefault;
                }
                yBitStartLoc = 0;
            }
        }
        public Model SearchExpModelByIndex(UInt32 u32Tag, XMLData xmlDataIn, AsyncObservableCollection<Model> tagList)
        {
            Model expmdltmp = null;
            if (tagList == null) return null;
            foreach (Model expm in tagList)
            {
                if (expm.u32RegNum == u32Tag)
                {
                    if (expm.strGroupReg.Equals(xmlDataIn.strGroup))
                    {
                        expmdltmp = expm;
                        break;
                    }
                }
            }
            return expmdltmp;
        }
    }
}
