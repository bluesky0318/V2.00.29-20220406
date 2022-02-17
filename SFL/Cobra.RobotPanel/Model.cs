using System;
using System.Globalization;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;
using System.Collections.Generic;

namespace Cobra.RobotPanel
{
    public class Infor
    {
        private string m_Record;
        public string Record
        {
            get { return m_Record; }
            set { m_Record = value; }
        }

        public string m_Timer;
        public string Timer
        {
            //get { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); ; }
            get { return m_Timer; }
            set { m_Timer = value; }
        }

        public Infor(string record)
        {
            m_Record = record;
            m_Timer = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Model : INotifyPropertyChanged
    {
        public void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter param = null;
            param = sender as Parameter;
            if (param == null) return;
            switch (e.PropertyName)
            {
                case "u32hexdata":
                    if (bHexDec)
                        sudata = string.Format("0x{0:x8}", param.u32hexdata);
                    else
                        sudata = string.Format("{0}", param.u32hexdata);
                    break;
                case "errorcode":
                    if (param.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        bResult = 1;
                    else
                        bResult = 0;
                    break;
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(e.PropertyName));
        }

        public void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UInt32 wval = 0, mask = 0;
            string tmp = string.Empty;
            switch (e.PropertyName)
            {
                case "sudata":
                    if (bHexDec)
                    {
                        if (sudata == null) break;
                        if (sudata.Contains(ElementDefine.prefix))
                            tmp = sudata.Substring(ElementDefine.prefix.Length);
                        else
                            tmp = sudata;
                        if (!UInt32.TryParse(tmp, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out wval)) return;
                    }
                    else
                    {
                        tmp = sudata;
                        if (!UInt32.TryParse(tmp, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out wval)) return;
                    }
                    udata = wval;
                    pParent.PropertyChanged -= Parent_PropertyChanged;
                    pParent.u32hexdata = udata;
                    pParent.PropertyChanged += Parent_PropertyChanged;
                    break;
                case "address":
                    m_Parent.reglist["Low"].u32Address = m_Address;
                    break;
                default:
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public Model()
        {
            m_Parent = new Parameter();
            Reg reg = new Reg();
            reg.u32Address = m_Address;
            reg.startbit = 0;
            m_Parent.reglist.Add("Low", reg);
            m_Parent.PropertyChanged += Parent_PropertyChanged;
            PropertyChanged += Model_PropertyChanged;

            m_ID = 0;
            m_bSelect = false;
            m_Type = 0;
            m_Address = 0;
            m_bDetail = false;
            m_bHexDec = true;
            m_suData = string.Format("0x{0:x8}", 0);
            m_udata = 0;
            m_bResult = -1;
            m_Comments = string.Empty;

            bitModel_List.Clear();
            formulaModel_List.Clear();
        }

        public Model(int count)
        {
            m_Parent = new Parameter();
            Reg reg = new Reg();
            reg.u32Address = m_Address;
            reg.startbit = 0;
            m_Parent.reglist.Add("Low", reg);
            m_Parent.PropertyChanged += Parent_PropertyChanged;
            PropertyChanged += Model_PropertyChanged;

            m_ID = count;
            m_bSelect = false;
            m_Type = 0;
            m_Address = 0;
            m_bDetail = false;
            m_bHexDec = true;
            m_suData = string.Format("0x{0:x8}", 0);
            m_udata = 0;
            m_bResult = -1;
            m_Comments = string.Empty;

            bitModel_List.Clear();
            formulaModel_List.Clear();
        }

        private Parameter m_Parent;
        public Parameter pParent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        private int m_ID;
        [Newtonsoft.Json.JsonProperty]
        public int id
        {
            get { return m_ID; }
            set
            {
                m_ID = value;
                OnPropertyChanged("id");
            }
        }

        private bool m_bSelect;
        [Newtonsoft.Json.JsonProperty]
        public bool bSelect
        {
            get { return m_bSelect; }
            set
            {
                m_bSelect = value;
                OnPropertyChanged("bSelect");
            }
        }

        private bool m_bDetail;
        [Newtonsoft.Json.JsonProperty]
        public bool bDetail
        {
            get { return m_bDetail; }
            set
            {
                m_bDetail = value;
                OnPropertyChanged("bDetail");
            }
        }

        private int m_Type;
        [Newtonsoft.Json.JsonProperty]
        public int type
        {
            get { return m_Type; }
            set
            {
                m_Type = value;
                OnPropertyChanged("type");
            }
        }

        private UInt32 m_Address;
        [Newtonsoft.Json.JsonProperty]
        public UInt32 address
        {
            get { return m_Address; }
            set
            {
                m_Address = value;
                OnPropertyChanged("address");
            }
        }

        private bool m_bHexDec;
        [Newtonsoft.Json.JsonProperty]
        public bool bHexDec
        {
            get { return m_bHexDec; }
            set
            {
                m_bHexDec = value;
                OnPropertyChanged("bHexDec");
            }
        }

        private string m_suData;
        [Newtonsoft.Json.JsonProperty]
        public string sudata
        {
            get { return m_suData; }
            set
            {
                m_suData = value;
                OnPropertyChanged("sudata");
            }
        }

        private UInt32 m_udata;
        public UInt32 udata
        {
            get { return m_udata; }
            set
            {
                m_udata = value;
                OnPropertyChanged("udata");
            }
        }

        private int m_bResult;
        [Newtonsoft.Json.JsonProperty]
        public int bResult
        {
            get { return m_bResult; }
            set
            {
                m_bResult = value;
                OnPropertyChanged("bResult");
            }
        }

        private string m_Summary;
        [Newtonsoft.Json.JsonProperty]
        public string summary
        {
            get { return m_Summary; }
            set
            {
                m_Summary = value;
                OnPropertyChanged("summary");
            }
        }

        #region RowDetail       
        private ObservableCollection<formulaModel> m_formulaModel_List = new ObservableCollection<formulaModel>();
        [Newtonsoft.Json.JsonProperty]
        public ObservableCollection<formulaModel> formulaModel_List
        {
            get { return m_formulaModel_List; }
            set { m_formulaModel_List = value; }
        }

        private ObservableCollection<bitModel> m_bitModel_List = new ObservableCollection<bitModel>();
        [Newtonsoft.Json.JsonProperty]
        public ObservableCollection<bitModel> bitModel_List
        {
            get { return m_bitModel_List; }
            set { m_bitModel_List = value; }
        }

        private bool m_bComment;
        [Newtonsoft.Json.JsonProperty]
        public bool bComment
        {
            get { return m_bComment; }
            set
            {
                m_bComment = value;
                OnPropertyChanged("bComment");
            }
        }

        private string m_Comments = "Comments";
        [Newtonsoft.Json.JsonProperty]
        public string comments
        {
            get { return m_Comments; }
            set
            {
                m_Comments = value;
                OnPropertyChanged("comments");
            }
        }
        #endregion
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class bitModel : INotifyPropertyChanged
    {
        public void MoInBit_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UInt32 wval = 0, mask = 0;
            Model mo = sender as Model;
            if (mo == null) return;
            switch (e.PropertyName)
            {
                case "udata":
                    if ((bitHn - bitLn + 1) == 32)
                    {
                        utarget = mo.udata;
                        break;
                    }
                    mask = (UInt32)(1 << (bitHn - bitLn + 1)) - 1;
                    mask <<= bitLn;
                    wval = (mo.udata & mask);
                    utarget = wval >> bitLn;
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            UInt32 wval = 0, mask = 0;
            string tmp = string.Empty;
            switch (propName)
            {
                case "suTarget":
                    if (bTargetHexDec)
                    {
                        if (suTarget == null) break;
                        if (suTarget.Contains(ElementDefine.prefix))
                            tmp = suTarget.Substring(ElementDefine.prefix.Length);
                        else
                            tmp = suTarget;
                        if (!UInt32.TryParse(tmp, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out wval)) return;
                    }
                    else
                    {
                        tmp = suTarget;
                        if (!UInt32.TryParse(tmp, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out wval)) return;
                    }
                    utarget = wval;
                    break;
                case "utarget":
                    if (bTargetHexDec)
                        suTarget = string.Format("0x{0:x8}", utarget);
                    else
                        suTarget = string.Format("{0}", utarget);
                    break;
                case "bitLn":
                    if ((bitHn - bitLn + 1) == 32)
                    {
                        utarget = model_Parent.udata;
                        break;
                    }
                    mask = (UInt32)(1 << (bitHn - bitLn + 1)) - 1;
                    mask <<= bitLn;
                    wval = (model_Parent.udata & mask);
                    utarget = wval >> bitLn;
                    break;
                case "bitHn":
                    if ((bitHn - bitLn + 1) == 32)
                    {
                        utarget = model_Parent.udata;
                        break;
                    }
                    mask = (UInt32)(1 << (bitHn - bitLn + 1)) - 1;
                    mask <<= bitLn;
                    wval = (model_Parent.udata & mask);
                    utarget = wval >> bitLn;
                    break;
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Model m_Model_Parent;
        public Model model_Parent
        {
            get { return m_Model_Parent; }
            set { m_Model_Parent = value; }
        }

        public bitModel()
        {
            model_Parent = new Model();
            model_Parent.PropertyChanged += MoInBit_PropertyChanged;
            m_BitHn = 1;
            m_BitLn = 0;
            m_Title = string.Empty;
            m_bTargetHexDec = true;
        }

        public bitModel(Model mo)
        {
            UInt32 wval = 0, mask = 0;

            model_Parent = mo;
            mo.PropertyChanged += MoInBit_PropertyChanged;
            m_BitHn = 1;
            m_BitLn = 0;
            m_Title = string.Empty;
            m_bTargetHexDec = true;
            mask = (UInt32)(1 << (bitHn - bitLn + 1)) - 1;
            mask <<= bitLn;
            wval = (mo.udata & mask);
            utarget = wval >> bitLn;
        }

        private bool m_bTargetHexDec;
        [Newtonsoft.Json.JsonProperty]
        public bool bTargetHexDec
        {
            get { return m_bTargetHexDec; }
            set
            {
                m_bTargetHexDec = value;
                OnPropertyChanged("bTargetHexDec");
            }
        }

        private byte m_BitHn;
        [Newtonsoft.Json.JsonProperty]
        public byte bitHn
        {
            get { return m_BitHn; }
            set
            {
                if (value < m_BitLn) return;
                if (value > 31) return;
                m_BitHn = value;
                OnPropertyChanged("bitHn");
            }
        }

        private byte m_BitLn;
        [Newtonsoft.Json.JsonProperty]
        public byte bitLn
        {
            get { return m_BitLn; }
            set
            {
                if (value > bitHn) return;
                if (value < 0) return;
                m_BitLn = value;
                OnPropertyChanged("bitLn");
            }
        }

        private string m_suTarget;
        [Newtonsoft.Json.JsonProperty]
        public string suTarget
        {
            get { return m_suTarget; }
            set
            {
                m_suTarget = value;
                OnPropertyChanged("suTarget");
            }
        }

        private UInt32 m_uTarget;
        public UInt32 utarget
        {
            get { return m_uTarget; }
            set
            {
                if (value == m_uTarget) return;
                m_uTarget = value;
                OnPropertyChanged("utarget");
            }
        }

        private string m_Title;
        [Newtonsoft.Json.JsonProperty]
        public string title
        {
            get { return m_Title; }
            set
            {
                m_Title = value;
                OnPropertyChanged("title");
            }
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class formulaModel : INotifyPropertyChanged
    {
        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter param = null;
            param = sender as Parameter;
            if (param == null) return;
            switch (e.PropertyName)
            {
                case "itemlist":
                    if (param.itemlist.Count != 0)
                        sOut = param.itemlist[0];
                    break;
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(e.PropertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            switch (propName)
            {
                case "sIn":
                    pParent.sphydata = sIn;
                    break;
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Parameter m_Parent;
        public Parameter pParent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        private Model m_Model_Parent;
        public Model model_Parent
        {
            get { return m_Model_Parent; }
            set { m_Model_Parent = value; }
        }

        private formulaCollection m_curFormulaModel;
        [Newtonsoft.Json.JsonProperty]
        public formulaCollection curFormulaModel
        {
            get { return m_curFormulaModel; }
            set
            {
                m_curFormulaModel = value;
                OnPropertyChanged("curFormulaModel");
            }
        }

        public formulaModel()
        {
            m_Index = 1;
            model_Parent = new Model();
            pParent = new Parameter();
            pParent.PropertyChanged += Parent_PropertyChanged;
        }

        public formulaModel(Model mo, Dictionary<string, Tuple<int, string, string>> formula_dic)
        {
            m_Index = 1;
            model_Parent = mo;
            pParent = new Parameter();
            pParent.PropertyChanged += Parent_PropertyChanged;
            foreach (KeyValuePair<string, Tuple<int, string, string>> entry in formula_dic)
            {
                formulaCollection fms = new formulaCollection();
                fms.formula = entry.Key;
                fms.inTips = entry.Value.Item2;
                fms.outTips = entry.Value.Item3;
                formulaList.Add(fms);
            }
        }

        private ObservableCollection<formulaCollection> m_formulaList = new ObservableCollection<formulaCollection>();
        [Newtonsoft.Json.JsonProperty]
        public ObservableCollection<formulaCollection> formulaList
        {
            get { return m_formulaList; }
            set { m_formulaList = value; }
        }

        private int m_Index;
        [Newtonsoft.Json.JsonProperty]
        public int index
        {
            get { return m_Index; }
            set
            {
                m_Index = value;
                OnPropertyChanged("index");
            }
        }

        private string m_sIn;
        [Newtonsoft.Json.JsonProperty]
        public string sIn
        {
            get { return m_sIn; }
            set
            {
                m_sIn = value;
                OnPropertyChanged("sIn");
            }
        }

        private string m_sOut;
        [Newtonsoft.Json.JsonProperty]
        public string sOut
        {
            get { return m_sOut; }
            set
            {
                m_sOut = value;
                OnPropertyChanged("sOut");
            }
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class formulaCollection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private string m_formula;
        [Newtonsoft.Json.JsonProperty]
        public string formula
        {
            get { return m_formula; }
            set
            {
                m_formula = value;
                OnPropertyChanged("formula");
            }
        }

        private string m_inTips;
        [Newtonsoft.Json.JsonProperty]
        public string inTips
        {
            get { return m_inTips; }
            set
            {
                m_inTips = value;
                OnPropertyChanged("inTips");
            }
        }

        private string m_outTips;
        [Newtonsoft.Json.JsonProperty]
        public string outTips
        {
            get { return m_outTips; }
            set
            {
                m_outTips = value;
                OnPropertyChanged("outTips");
            }
        }
    }
}
