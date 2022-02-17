using Cobra.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Cobra.BlackBoxPanel.EventLog
{
    //Data structure for XML parsing <Element>/<Private> node, for Operation Register definition
    public class XMLData
    {
        public byte yTotal { get; set; }
        public UInt32 u32Index { get; set; }
        public byte yBitStart { get; set; }
        public byte yLength { get; set; }
        public byte yValue { get; set; }
        public bool bRead { get; set; }
        public bool bWrite { get; set; }
        public string strDescrip { get; set; }
        public string strBitTips { get; set; }
        public Parameter pmrXDParent { get; set; }
        public UInt32 u32Guid { get; set; }
        public string strGroup { get; set; }
        public string strRegName { get; set; }      //(A141218)Francis, for enhancement
        public string strUnit { get; set; }             //(A141218)Francis, for enhancement
        public bool bPhyDataFromList { get; set; }      //(ISSUE:2203)Guo, for enhancement
        public XMLData()
        {
            yTotal = 8;
            u32Index = 0xFFFFFFFF;
            yBitStart = 0xFF;
            yLength = 0xFF;
            yValue = 0xFF;
            bRead = false;
            bWrite = false;
            strDescrip = string.Empty;
            strBitTips = string.Empty;
            pmrXDParent = null;
            u32Guid = 0xFFFFFFFF;
            strGroup = string.Empty;
            strUnit = string.Empty;
            strRegName = string.Empty;
            bPhyDataFromList = false;
        }
    }

    public class BitComponent : INotifyPropertyChanged
    {
        static public string BitDescrpDefault = "--";
        static public string RegDescrpDefault = "-- --";

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public Parameter pmrBitParent { get; set; }     //save sudo Parameter, it's fake and hexref/phyref are all 1
        public XMLData expXMLdataParent { get; set; }      //save ExperXMLData node
        public UInt32 u32Guid { get; set; } //save GUID
        public string strBit { get; set; }  //save "bit_0" string, simly dispaly on UI

        //save readable,
        private bool m_bRead;
        public bool bRead
        {
            get { return m_bRead; }
            set { m_bRead = value; OnPropertyChanged("bRead"); }
        }

        //save writable,
        private bool m_bWrite;
        public bool bWrite
        {
            get { return m_bWrite; }
            set { m_bWrite = value; OnPropertyChanged("bWrite"); }
        }

        //save bit value 0 or 1, binding to UI display value and combine into Register Value
        private byte m_yBitValue;
        public byte yBitValue
        {
            get { return m_yBitValue; }
            set { m_yBitValue = value; OnPropertyChanged("yBitValue"); }
        }

        //save bit is operatable or not, binding to UI, bit_description and bit_operation, bBitEnable = (bRead & bWrite);
        private bool m_bBitEnable;
        public bool bBitEnable
        {
            get { return m_bBitEnable; }
            set { m_bBitEnable = value; OnPropertyChanged("bBitEnable"); }
        }

        //save the length of bit, 
        private byte m_yDescripVisLgth;
        public byte yDescripVisiLgth
        {
            get { return m_yDescripVisLgth; }
            set { m_yDescripVisLgth = value; OnPropertyChanged("yDescripVisiLgth"); }
        }

        //save Bit description
        private string m_strBitDescrip;
        public string strBitDescrip
        {
            get { return m_strBitDescrip; }
            set { m_strBitDescrip = value; OnPropertyChanged("strBitDescrip"); }
        }

        //save Bit description
        private string m_strBitTips;
        public string strBitTips
        {
            get { return m_strBitTips; }
            set { m_strBitTips = value; OnPropertyChanged("strBitTips"); }
        }

        //save Group string
        public string strGroupBit { get; set; }

        //(A141218)Francis, for enhancement
        //save Unit string from xml
        public string strUnit { get; set; }

        private double m_dbPhyValue;
        public double dbPhyValue
        {
            get { return m_dbPhyValue; }
            set
            {
                m_dbPhyValue = value;
                //(M180821)Francis, issue_id=865, sync solution that don't convert physical value from DEM, jsut convert DEC to HEX simply
                if (bShowPhysical)
                {
                    if ((pmrBitParent != null) && (pmrBitParent.itemlist.Count != 0) && (pmrBitParent.itemlist.Count > m_dbPhyValue) && (m_dbPhyValue >= 0)
                        && (expXMLdataParent != null) && expXMLdataParent.bPhyDataFromList)
                    {
                        strPhysicalValue = pmrBitParent.itemlist[(int)m_dbPhyValue].Trim();
                    }
                    else
                    {
                        if (strUnit.Length == 0)
                        {
                            strPhysicalValue = string.Format("{0} {1}", m_dbPhyValue, strUnit);
                        }
                        else
                        {
                            strPhysicalValue = string.Format("{0:F1} {1}", m_dbPhyValue, strUnit);
                        }
                    }
                }
                else
                {
                    if (pmrBitParent != null)
                        strPhysicalValue = string.Format("{0:X}", pmrBitParent.hexdata);
                }
            }
        }

        private string m_strPhysicalValue;
        public string strPhysicalValue
        {
            get { return m_strPhysicalValue; }
            set { m_strPhysicalValue = value; OnPropertyChanged("strPhysicalValue"); }
        }
        //(E141218)

        //(M180821)Francis, issue_id=865, sync solution that don't convert physical value from DEM, jsut convert DEC to HEX simply
        public bool bShowPhysical { get; set; }

        //Construction
        public BitComponent()
        {
            ResetBitContent();
        }

        // <summary>
        // Reset bit content as initialization value
        // </summary>
        public void ResetBitContent()
        {
            yDescripVisiLgth = 1;
            strBitDescrip = string.Format(BitDescrpDefault);
            strPhysicalValue = string.Format(BitDescrpDefault);
            pmrBitParent = null;
            u32Guid = 0;
            strUnit = string.Empty;
            strPhysicalValue = string.Empty;
        }
    }

    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Parameter m_pmrExpMdlParent = new Parameter();
        public Parameter pmrExpMdlParent
        {
            get { return m_pmrExpMdlParent; }
            set { m_pmrExpMdlParent = value; }
        }

        private string m_strRegName;
        public string strRegName
        {
            get { return m_strRegName; }
            set { m_strRegName = value; OnPropertyChanged("strRegName"); }
        }       //save string of Register name

        private UInt32 m_u32RegVal;
        public UInt32 u32RegVal
        {
            get { return m_u32RegVal; }
            set
            {
                if (m_u32RegVal != value)
                {
                    bValueChange = true;
                }
                else
                {
                    bValueChange = false;
                }
                m_u32RegVal = value;
                if (yRegLength == 0x10)
                {
                    strRegVal = string.Format("0x{0:X4}", m_u32RegVal);
                }
                else if (yRegLength == 0x20)
                {
                    strRegVal = string.Format("0x{0:X8}", m_u32RegVal);
                }
                else //otherwise, default is 2 digit value
                {
                    foreach (KeyValuePair<string, Reg> inreg in m_pmrExpMdlParent.reglist)
                    {
                        if (inreg.Value.address == u32RegNum)
                        {
                            if (inreg.Key.Equals("Low"))
                            {
                                m_u32RegVal &= 0x00FF;
                                break;
                            }
                            else if (inreg.Key.Equals("High"))
                            {
                                m_u32RegVal &= 0xFF00;
                                m_u32RegVal >>= 8;
                                break;
                            }
                        }
                    }
                    strRegVal = string.Format("0x{0:X2}", m_u32RegVal);
                }
                ArrangeBitPhyValue();
            }
        }

        private string m_strRegVal;
        public string strRegVal
        {
            get { return m_strRegVal; }
            set
            {
                string strtmp;
                int itmp;
                UInt32 utmp;
                m_strRegVal = value;
                OnPropertyChanged("strRegVal");
                itmp = m_strRegVal.IndexOf("0x");
                if (itmp != -1)
                {
                    strtmp = m_strRegVal.Substring(itmp + 2);
                }
                else
                {
                    strtmp = m_strRegVal;
                }
                if (!UInt32.TryParse(strtmp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out utmp))
                {
                    utmp = 0;
                }
                if (m_u32RegVal != utmp)
                {
                    m_u32RegVal = utmp;
                    SeperateRegValueToBit();
                    ArrangeBitPhyValue();
                    bValueChange = true;
                }
            }
        }

        private bool m_bValueChange;
        public bool bValueChange
        {
            get { return m_bValueChange; }
            set { m_bValueChange = value; OnPropertyChanged("bValueChange"); }
        }

        private bool m_bRead;
        public bool bRead
        {
            get { return m_bRead; }
            set { m_bRead = value; OnPropertyChanged("bRead"); }
        }

        private bool m_bEnable = false;
        public bool bEnable
        {
            get { return m_bEnable; }
            set { m_bEnable = value; OnPropertyChanged("bEnable"); }
        }

        private bool m_bWrite;
        public bool bWrite
        {
            get { return m_bWrite; }
            set { m_bWrite = value; OnPropertyChanged("bWrite"); }
        }

        private bool m_bXprRegShow;
        public bool bXprRegShow
        {
            get { return m_bXprRegShow; }
            set { m_bXprRegShow = value; OnPropertyChanged("bXprRegShow"); }
        }

        public UInt32 u32RegNum { get; set; }   //save index value, as Tag parameter in button_click action
        public string strRegNum { get; set; }       //save index string value, simply display on UI
        public byte yRegLength { get; set; }        //8 or 16 bit length
        public string strTestXpr { get; set; }      // save TestMode string in ExperModel
        public string strGroupReg { get; set; } //Save Group string for each register belongs to

        private bool m_bMarkReg;
        public bool bMarkReg
        {
            get { return m_bMarkReg; }
            set { m_bMarkReg = value; OnPropertyChanged("bMarkReg"); }
        }

        #region Public Property declaration of each bit component,ebcBit0~F, and ArrRegComponet[16] array 
        public BitComponent[] ArrRegComponet = new BitComponent[32];
        private BitComponent m_ebcBit0 = new BitComponent();
        public BitComponent ebcBit0 { get { return m_ebcBit0; } set { m_ebcBit0 = value; } }
        private BitComponent m_ebcBit1 = new BitComponent();
        public BitComponent ebcBit1 { get { return m_ebcBit1; } }
        private BitComponent m_ebcBit2 = new BitComponent();
        public BitComponent ebcBit2 { get { return m_ebcBit2; } }
        private BitComponent m_ebcBit3 = new BitComponent();
        public BitComponent ebcBit3 { get { return m_ebcBit3; } }
        private BitComponent m_ebcBit4 = new BitComponent();
        public BitComponent ebcBit4 { get { return m_ebcBit4; } }
        private BitComponent m_ebcBit5 = new BitComponent();
        public BitComponent ebcBit5 { get { return m_ebcBit5; } }
        private BitComponent m_ebcBit6 = new BitComponent();
        public BitComponent ebcBit6 { get { return m_ebcBit6; } }
        private BitComponent m_ebcBit7 = new BitComponent();
        public BitComponent ebcBit7 { get { return m_ebcBit7; } }
        private BitComponent m_ebcBit8 = new BitComponent();
        public BitComponent ebcBit8 { get { return m_ebcBit8; } }
        private BitComponent m_ebcBit9 = new BitComponent();
        public BitComponent ebcBit9 { get { return m_ebcBit9; } }
        private BitComponent m_ebcBitA = new BitComponent();
        public BitComponent ebcBitA { get { return m_ebcBitA; } }
        private BitComponent m_ebcBitB = new BitComponent();
        public BitComponent ebcBitB { get { return m_ebcBitB; } }
        private BitComponent m_ebcBitC = new BitComponent();
        public BitComponent ebcBitC { get { return m_ebcBitC; } }
        private BitComponent m_ebcBitD = new BitComponent();
        public BitComponent ebcBitD { get { return m_ebcBitD; } }
        private BitComponent m_ebcBitE = new BitComponent();
        public BitComponent ebcBitE { get { return m_ebcBitE; } }
        private BitComponent m_ebcBitF = new BitComponent();
        public BitComponent ebcBitF { get { return m_ebcBitF; } }
        private BitComponent m_ebcBit10 = new BitComponent();
        public BitComponent ebcBit10 { get { return m_ebcBit10; } }
        private BitComponent m_ebcBit11 = new BitComponent();
        public BitComponent ebcBit11 { get { return m_ebcBit11; } }
        private BitComponent m_ebcBit12 = new BitComponent();
        public BitComponent ebcBit12 { get { return m_ebcBit12; } }
        private BitComponent m_ebcBit13 = new BitComponent();
        public BitComponent ebcBit13 { get { return m_ebcBit13; } }
        private BitComponent m_ebcBit14 = new BitComponent();
        public BitComponent ebcBit14 { get { return m_ebcBit14; } }
        private BitComponent m_ebcBit15 = new BitComponent();
        public BitComponent ebcBit15 { get { return m_ebcBit15; } }
        private BitComponent m_ebcBit16 = new BitComponent();
        public BitComponent ebcBit16 { get { return m_ebcBit16; } }
        private BitComponent m_ebcBit17 = new BitComponent();
        public BitComponent ebcBit17 { get { return m_ebcBit17; } }
        private BitComponent m_ebcBit18 = new BitComponent();
        public BitComponent ebcBit18 { get { return m_ebcBit18; } }
        private BitComponent m_ebcBit19 = new BitComponent();
        public BitComponent ebcBit19 { get { return m_ebcBit19; } }
        private BitComponent m_ebcBit1A = new BitComponent();
        public BitComponent ebcBit1A { get { return m_ebcBit1A; } }
        private BitComponent m_ebcBit1B = new BitComponent();
        public BitComponent ebcBit1B { get { return m_ebcBit1B; } }
        private BitComponent m_ebcBit1C = new BitComponent();
        public BitComponent ebcBit1C { get { return m_ebcBit1C; } }
        private BitComponent m_ebcBit1D = new BitComponent();
        public BitComponent ebcBit1D { get { return m_ebcBit1D; } }
        private BitComponent m_ebcBit1E = new BitComponent();
        public BitComponent ebcBit1E { get { return m_ebcBit1E; } }
        private BitComponent m_ebcBit1F = new BitComponent();
        public BitComponent ebcBit1F { get { return m_ebcBit1F; } }
        #endregion
        public Model()
        {
            ArrRegComponet[0] = ebcBit0;
            ArrRegComponet[1] = ebcBit1;
            ArrRegComponet[2] = ebcBit2;
            ArrRegComponet[3] = ebcBit3;
            ArrRegComponet[4] = ebcBit4;
            ArrRegComponet[5] = ebcBit5;
            ArrRegComponet[6] = ebcBit6;
            ArrRegComponet[7] = ebcBit7;
            ArrRegComponet[8] = ebcBit8;
            ArrRegComponet[9] = ebcBit9;
            ArrRegComponet[10] = ebcBitA;
            ArrRegComponet[11] = ebcBitB;
            ArrRegComponet[12] = ebcBitC;
            ArrRegComponet[13] = ebcBitD;
            ArrRegComponet[14] = ebcBitE;
            ArrRegComponet[15] = ebcBitF;
            ArrRegComponet[16] = ebcBit10;
            ArrRegComponet[17] = ebcBit11;
            ArrRegComponet[18] = ebcBit12;
            ArrRegComponet[19] = ebcBit13;
            ArrRegComponet[20] = ebcBit14;
            ArrRegComponet[21] = ebcBit15;
            ArrRegComponet[22] = ebcBit16;
            ArrRegComponet[23] = ebcBit17;
            ArrRegComponet[24] = ebcBit18;
            ArrRegComponet[25] = ebcBit19;
            ArrRegComponet[26] = ebcBit1A;
            ArrRegComponet[27] = ebcBit1B;
            ArrRegComponet[28] = ebcBit1C;
            ArrRegComponet[29] = ebcBit1D;
            ArrRegComponet[30] = ebcBit1E;
            ArrRegComponet[31] = ebcBit1F;
            yRegLength = 0x08;
            for (int i = 0; i < 32; i++)
            {
                ArrRegComponet[i].strBit = string.Format("{0:D}", i);
            }
            bXprRegShow = true;
            strGroupReg = "";
            bValueChange = false;
            bMarkReg = false;
            strRegName = string.Empty;
        }

        public void SumRegisterValue(bool bRdIn = true, bool bWtIn = true)
        {
            UInt32 u32tmp;
            bool brtmp, bwtmp;

            u32tmp = 0; brtmp = false; bwtmp = false;
            for (int i = 0; i < yRegLength; i++)
            {
                u32tmp += Convert.ToUInt32((UInt32)(ArrRegComponet[i].yBitValue << i));
                brtmp |= ArrRegComponet[i].bRead;
                bwtmp |= ArrRegComponet[i].bWrite;
            }

            foreach (KeyValuePair<string, Reg> inreg in m_pmrExpMdlParent.reglist)
            {
                if (inreg.Value.address == u32RegNum)
                {
                    if (inreg.Key.Equals("Low"))
                    {
                        //m_u16RegVal &= 0x00FF;
                    }
                    else
                    {
                        //m_u16RegVal &= 0xFF00;
                        u32tmp <<= 8;
                    }
                }
            }

            u32RegVal = u32tmp;
            if (bRdIn) bRead = brtmp;
            if (bWtIn) bWrite = bwtmp;
        }

        public void SeperateRegValueToBit()
        {
            UInt32 u32tmp = u32RegVal;

            for (int i = 0; i < yRegLength; i++)
            {
                ArrRegComponet[i].yBitValue = Convert.ToByte((u32tmp >> i) & 0x01);
            }
        }

        public Parameter GetParentParameter()
        {
            Parameter pmrtmp = null;
            //Reg regtmp = null;

            foreach (BitComponent expbittmp in ArrRegComponet)
            {
                if ((expbittmp.u32Guid != 0) && (expbittmp.pmrBitParent != null))
                {
                    pmrtmp = expbittmp.pmrBitParent;
                    break;
                }
            }

            pmrExpMdlParent.guid = pmrtmp.guid;
            pmrExpMdlParent.phyref = 1;
            pmrExpMdlParent.regref = 1;
            pmrExpMdlParent.subtype = 0;        //COBRA_PARAM_SUBTYPE
            pmrExpMdlParent.subsection = pmrtmp.subsection; //(A140409)Francis
            pmrExpMdlParent.sfllist = pmrtmp.sfllist;
            pmrExpMdlParent.reglist.Clear();
            //foreach (KeyValuePair<string, Reg> dicreg in pmrExpMdlParent.reglist)
            foreach (KeyValuePair<string, Reg> tmpreg in pmrtmp.reglist)
            {
                if (tmpreg.Key.ToLower().Equals("low"))
                {
                    Reg newReg_low = new Reg();
                    newReg_low.address = tmpreg.Value.address;
                    newReg_low.u32Address = tmpreg.Value.u32Address;
                    newReg_low.bitsnumber = tmpreg.Value.bitsnumber;
                    newReg_low.startbit = tmpreg.Value.startbit;
                    pmrExpMdlParent.reglist.Add("Low", newReg_low);
                }
                else if (tmpreg.Key.ToLower().Equals("high"))
                {
                    Reg newReg_hi = new Reg();
                    newReg_hi.address = tmpreg.Value.address;
                    newReg_hi.u32Address = tmpreg.Value.u32Address;
                    newReg_hi.bitsnumber = tmpreg.Value.bitsnumber;
                    newReg_hi.startbit = tmpreg.Value.startbit;
                    pmrExpMdlParent.reglist.Add("High", newReg_hi);
                }
            }
            foreach (KeyValuePair<string, Reg> dicreg in pmrExpMdlParent.reglist)
            {
                dicreg.Value.startbit = 0;
                dicreg.Value.bitsnumber = yRegLength;
            }

            //return pmrtmp;
            return pmrExpMdlParent;
        }

        public void ArrangeBitPhyValue(AsyncObservableCollection<Model> expregListIn = null)
        {
            UInt32 utmp, uMask;
            UInt16 uaddr = 0x00;
            int i = 0;

            foreach (BitComponent expbittmp in ArrRegComponet)
            {
                uMask = 0xFFFFFFFF;
                if ((expbittmp.u32Guid != 0) && (expbittmp.pmrBitParent != null))
                {
                    expbittmp.dbPhyValue = expbittmp.pmrBitParent.phydata;
                    if (expregListIn != null)   //need to find another ExperModel(another Low or High byte of 1 physical value) in List,
                    {
                        if (expbittmp.pmrBitParent.reglist.Count > 1)
                        {
                            foreach (KeyValuePair<string, Reg> kvptmp in expbittmp.pmrBitParent.reglist)
                            {
                                if (kvptmp.Value.address != u32RegNum)
                                {
                                    uaddr = kvptmp.Value.address;
                                    break;
                                }
                            }
                            foreach (Model epx in expregListIn)
                            {
                                if (epx.u32RegNum == uaddr)
                                {
                                    foreach (BitComponent ebctmp in epx.ArrRegComponet)
                                    {
                                        if ((ebctmp.u32Guid != 0) && (ebctmp.pmrBitParent != null))
                                            ebctmp.dbPhyValue = expbittmp.pmrBitParent.phydata;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                i += 1;
            }
        }
    }
}
