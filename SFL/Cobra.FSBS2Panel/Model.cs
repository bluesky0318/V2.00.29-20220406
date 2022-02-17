using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;
using System.ComponentModel;
using Microsoft.Research.DynamicDataDisplay.Common;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.FSBS2Panel
{
    public class DataPointCollection : RingArray<DataPoint>
    {
        private const int TOTAL_POINTS = 3600;
        public DataPointCollection()
            : base(TOTAL_POINTS) // here i set how much values to show 
        {
        }
    }

    public class DataPoint
    {
        public ulong time_point { get; set; }
        public double ddata { get; set; }
        public DataPoint(double data, ulong date)
        {
            this.time_point = date;
            this.ddata = data;
        }
    }

    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            switch (propName)
            {
                case "data":
                    {
                        if (itemlist.Count != 0)
                        {
                            bitsList.Clear();
                            for (UInt16 i = 0; i < itemlist.Count; i++)
                                bitsList.Add(((((UInt32)data) & (1 << i)) >> i == 0) ? false : true);
                        }
                    }
                    break;
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Parameter m_Parent;
        public Parameter parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        private UInt32 m_Guid;
        public UInt32 guid
        {
            get { return m_Guid; }
            set { m_Guid = value; }
        }

        private UInt16 m_ListIndex;
        public UInt16 listindex
        {
            get { return m_ListIndex; }
            set
            {
                m_ListIndex = value;
                OnPropertyChanged("listindex");
            }
        }

        private ElementDefine.SBS_PARAM_SUBTYPE m_SubType;
        public ElementDefine.SBS_PARAM_SUBTYPE subType
        {
            get { return m_SubType; }
            set { m_SubType = value; }
        }

        private string m_NickName;
        public string nickname
        {
            get { return m_NickName; }
            set { m_NickName = value; }
        }

        private string m_LogName;
        public string logname
        {
            get { return m_LogName; }
            set { m_LogName = value; }
        }

        private double m_Data;
        public double data
        {
            get { return m_Data; }
            set
            {
                m_Data = value;
                OnPropertyChanged("data");
            }
        }

        //参数CMD标签
        private string m_Order;
        public string order
        {
            get { return m_Order; }
            set { m_Order = value; }
        }

        private UInt16 m_Index;
        public UInt16 index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        private string m_sPhydata;
        public string sphydata
        {
            get { return m_sPhydata; }
            set
            {
                m_sPhydata = value;
                OnPropertyChanged("sphydata");

            }
        }

        private string m_Catalog;
        public string catalog
        {
            get { return m_Catalog; }
            set { m_Catalog = value; }
        }

        private bool m_bShow;
        public bool bShow
        {
            get { return m_bShow; }
            set
            {
                m_bShow = value;
                OnPropertyChanged("bShow");
            }
        }

        private bool m_bEnable = false;
        public bool bEnable
        {
            get { return m_bEnable; }
            set
            {
                m_bEnable = value;
                OnPropertyChanged("bEnable");
            }
        }
        private bool m_bWrite = false;
        public bool bWrite
        {
            get { return m_bWrite; }
            set
            {
                m_bWrite = value;
                OnPropertyChanged("bWrite");
            }
        }
        private UInt16 m_Format;
        public UInt16 format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        private WaveControl m_WaveControl = null;
        public WaveControl waveControl
        {
            get { return m_WaveControl; }
            set
            {
                m_WaveControl = value;
                OnPropertyChanged("waveControl");
            }
        }

        private UInt32 m_ErrorCode;
        public UInt32 errorcode
        {
            get { return m_ErrorCode; }
            set { m_ErrorCode = value; }
        }

        private ObservableCollection<string> m_ItemList = new ObservableCollection<string>();
        public ObservableCollection<string> itemlist
        {
            get { return m_ItemList; }
            set
            {
                m_ItemList = value;
                OnPropertyChanged("itemlist");
            }
        }

        private UInt16 m_EditorType;
        public UInt16 editortype
        {
            get { return m_EditorType; }
            set { m_EditorType = value; }
        }

        private AsyncObservableCollection<bool> m_BitsList = new AsyncObservableCollection<bool>();
        public AsyncObservableCollection<bool> bitsList
        {
            get { return m_BitsList; }
            set
            {
                m_BitsList = value;
                OnPropertyChanged("bitsList");
            }
        }

        private ObservableCollection<UInt32> m_Relations = new ObservableCollection<UInt32>();
        public ObservableCollection<UInt32> relations
        {
            get { return m_Relations; }
            set
            {
                m_Relations = value;
                OnPropertyChanged("relations");
            }
        }

        private AsyncObservableCollection<Model> m_Relation_Params = new AsyncObservableCollection<Model>();
        public AsyncObservableCollection<Model> relation_params
        {
            get { return m_Relation_Params; }
            set
            {
                m_Relation_Params = value;
                OnPropertyChanged("relation_params");
            }
        }
    }

    public struct LogParam
    {
        public string name;
        public string group;
    }

    public class ScanLogUIData
    {
        private DataTable m_logbuf = new DataTable();
        public DataTable logbuf
        {
            get { return m_logbuf; }
            set { m_logbuf = value; }
        }
        public void BuildColumn(List<LogParam> paramlist, bool isWithTime)  //从param list创建Column,Caption中包含Group信息
        {
            DataColumn col;
            foreach (LogParam param in paramlist)
            {
                col = new DataColumn();
                col.DataType = System.Type.GetType("System.String");
                col.ColumnName = param.name;
                col.AutoIncrement = false;
                col.Caption = param.group;
                col.ReadOnly = false;
                col.Unique = false;
                logbuf.Columns.Add(col);
            }
            if (isWithTime)
            {
                col = new DataColumn();
                col.DataType = System.Type.GetType("System.DateTime");
                col.ColumnName = "Time";
                col.AutoIncrement = false;
                col.Caption = "Time";
                col.ReadOnly = false;
                col.Unique = false;
                logbuf.Columns.Add(col);
            }
        }
    }

    public class vModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Model mParent;
        public Model pParent
        {
            get { return mParent; }
            set { mParent = value; }
        }

        private bool mUsability;
        public bool pUsability
        {
            get { return mUsability; }
            set
            {
                mUsability = value;
                OnPropertyChanged("pUsability");
            }
        }

        private string mTip;
        public string pTip
        {
            get { return mTip; }
            set
            {
                mTip = value;
                OnPropertyChanged("pTip");
            }
        }

        private int mIndex;
        public int pIndex
        {
            get { return mIndex; }
            set
            {
                mIndex = value;
                OnPropertyChanged("pIndex");
            }
        }

        private double mValue;
        public double pValue
        {
            get
            {
                if (pUsability == true)
                    return double.NaN;
                return Math.Round(mValue, 2);
            }
            set
            {
                mValue = value;
                OnPropertyChanged("pValue");
            }
        }

        private double? mMinValue;
        public double? pMinValue
        {
            get { return mMinValue; }
            set
            {
                mMinValue = value;
                OnPropertyChanged("pMinValue");
            }
        }

        private double? mMaxValue;
        public double? pMaxValue
        {
            get { return mMaxValue; }
            set
            {
                mMaxValue = value;
                OnPropertyChanged("pMaxValue");
            }
        }
    }

    public class tModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Model mParent;
        public Model pParent
        {
            get { return mParent; }
            set { mParent = value; }
        }

        private bool mUsability;
        public bool pUsability
        {
            get { return mUsability; }
            set
            {
                mUsability = value;
                OnPropertyChanged("pUsability");
            }
        }

        private string mTip;
        public string pTip
        {
            get { return mTip; }
            set
            {
                mTip = value;
                OnPropertyChanged("pTip");
            }
        }

        private int mIndex;
        public int pIndex
        {
            get { return mIndex; }
            set
            {
                mIndex = value;
                OnPropertyChanged("pIndex");
            }
        }

        private int mIndexGPIO;
        public int pIndexGPIO
        {
            get { return mIndexGPIO; }
            set
            {
                mIndexGPIO = value;
                OnPropertyChanged("pIndexGPIO");
            }
        }

        private double mValue;
        public double pValue
        {
            get { return Math.Round(mValue, 2); }
            set
            {
                mValue = value;
                OnPropertyChanged("pValue");
            }
        }

        private double? mMinValue;
        public double? pMinValue
        {
            get { return mMinValue; }
            set
            {
                mMinValue = value;
                OnPropertyChanged("pMinValue");
            }
        }

        private double? mMaxValue;
        public double? pMaxValue
        {
            get { return mMaxValue; }
            set
            {
                mMaxValue = value;
                OnPropertyChanged("pMaxValue");
            }
        }

        private string mLabel;
        public string pLabel
        {
            get { return mLabel; }
            set
            {
                mLabel = value;
                OnPropertyChanged("pLabel");
            }
        }
    }

    public class cModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Model mParent;
        public Model pParent
        {
            get { return mParent; }
            set { mParent = value; }
        }

        private double? mCOCTH;
        public double? pCOCTH
        {
            get { return mCOCTH; }
            set
            {
                if (mCOCTH != value)
                {
                    mCOCTH = value;
                    OnPropertyChanged("pCOCTH");
                }
            }
        }

        private double? mDOCTH;
        public double? pDOCTH
        {
            get { return mDOCTH; }
            set
            {
                if (mDOCTH != value)
                {
                    mDOCTH = value;
                    OnPropertyChanged("pDOCTH");
                }
            }
        }

        private double mValue;
        public double pValue
        {
            get { return Math.Round(mValue, 2); }
            set
            {
                if (mValue != value)
                {
                    mValue = value;
                    OnPropertyChanged("pValue");
                }
            }
        }

        private double? mMinValue;
        public double? pMinValue
        {
            get { return mMinValue; }
            set
            {
                mMinValue = value;
                OnPropertyChanged("pMinValue");
            }
        }

        private double? mMaxValue;
        public double? pMaxValue
        {
            get { return mMaxValue; }
            set
            {
                mMaxValue = value;
                OnPropertyChanged("pMaxValue");
            }
        }

        private bool? mCharge;
        public bool? pCharge
        {
            get { return mCharge; }
            set
            {
                if (mCharge != value)
                {
                    mCharge = value;
                    OnPropertyChanged("pCharge");
                }
            }
        }

        private string mTip;
        public string pTip
        {
            get { return mTip; }
            set
            {
                mTip = value;
                OnPropertyChanged("pTip");
            }
        }

        private bool? mDischarge;
        public bool? pDischarge
        {
            get { return mDischarge; }
            set
            {
                if (mDischarge != value)
                {
                    mDischarge = value;
                    OnPropertyChanged("pDischarge");
                }
            }
        }

        private bool mUsability;
        public bool pUsability
        {
            get { return mUsability; }
            set
            {
                mUsability = value;
                OnPropertyChanged("pUsability");
            }
        }

        private int mIndexGPIO;
        public int pIndexGPIO
        {
            get { return mIndexGPIO; }
            set
            {
                mIndexGPIO = value;
                OnPropertyChanged("pIndexGPIO");
            }
        }

        private int mIndex;
        public int pIndex
        {
            get { return mIndex; }
            set
            {
                mIndex = value;
                OnPropertyChanged("pIndex");
            }
        }
    }

    public class DataBaseRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private string m_Timestamp;
        public string Timestamp
        {
            get { return m_Timestamp; }
            set
            {
                m_Timestamp = value;
                OnPropertyChanged("Timestamp");
            }
        }

        private long m_RecordNumber;
        public long RecordNumber
        {
            get { return m_RecordNumber; }
            set
            {
                m_RecordNumber = value;
                OnPropertyChanged("RecordNumber");
            }
        }
    }

    public class setModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private string m_Nickname;
        public string nickname
        {
            get { return m_Nickname; }
            set
            {
                m_Nickname = value;
            }
        }

        private string m_Catalog;
        public string catalog
        {
            get { return m_Catalog; }
            set { m_Catalog = value; }
        }

        private UInt16 m_Editortype;
        public UInt16 editortype
        {
            get { return m_Editortype; }
            set { m_Editortype = value; }
        }

        private double m_PhyData;
        public double phydata
        {
            get { return m_PhyData; }
            set
            {
                //if (m_PhyData != value)
                {
                    m_PhyData = value;
                    OnPropertyChanged("phydata");
                }
            }
        }

        private string m_sPhyData;
        public string sphydata
        {
            get { return m_sPhyData; }
            set
            {
                m_sPhyData = value;
                OnPropertyChanged("sphydata");
            }
        }

        private UInt16 m_ListIndex;
        public UInt16 listindex
        {
            get { return m_ListIndex; }
            set
            {
                //if (m_ListIndex != value)
                {
                    m_ListIndex = value;
                    OnPropertyChanged("listindex");
                }
            }
        }

        private bool m_bCheck;
        public bool bcheck
        {
            get { return m_bCheck; }
            set
            {
                //if (m_bCheck != value)
                {
                    m_bCheck = value;
                    OnPropertyChanged("bcheck");
                }
            }
        }

        private AsyncObservableCollection<string> m_ItemList = new AsyncObservableCollection<string>();
        public AsyncObservableCollection<string> itemlist
        {
            get { return m_ItemList; }
            set
            {
                m_ItemList = value;
                OnPropertyChanged("itemlist");
            }
        }

        public Dictionary<string,string> m_Item_dic = new Dictionary<string,string>();
        public Dictionary<string, string> item_dic
        {
            get { return m_Item_dic; }
            set
            {
                m_Item_dic = value;
                OnPropertyChanged("item_dic");
            }
        }

        /// <summary>
        /// 参数初始化
        /// </summary>
        /// <param name="node"></param>
        public setModel(XmlNode node)
        {
            string tmp  = string.Empty;
            m_Nickname = node.Attributes["Name"].Value;
            foreach (XmlNode snode in node.ChildNodes)
            {
                editortype = Convert.ToUInt16(node.SelectSingleNode("EditorType").InnerText.Trim());
                switch (snode.Name)
                {
                    case "DefValue":
                        //phydata = Convert.ToDouble(snode.InnerText.Trim());
                        tmp = snode.InnerText.Trim();
                        switch (editortype)
                        {
                            case 0: //Text
                                sphydata = tmp;
                                break;
                            case 1: //Comboboxs
                                listindex = Convert.ToUInt16(tmp);
                                phydata = listindex;
                                break;
                            case 2: //Checkbox
                                bcheck = Convert.ToUInt16(tmp) > 0 ? true:false;
                                break;
                        }
                        break;
                    case "Catalog":
                        catalog = snode.InnerText.Trim();
                        break;
                    case "ItemList":
                        foreach (XmlNode ssnode in snode.ChildNodes)
                        {
                            m_Item_dic.Add(ssnode.InnerText.Trim(), ssnode.Attributes["Value"].Value.Trim());
                            itemlist.Add(ssnode.InnerText.Trim());
                        }
                        break;
                }
            }
        }
    }
}
