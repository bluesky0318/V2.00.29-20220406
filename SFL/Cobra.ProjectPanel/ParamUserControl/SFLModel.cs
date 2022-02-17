using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Cobra.Common;
using System.Xml;

namespace Cobra.ProjectPanel.Param
{
    public class SFLModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Parameter m_Parent = new Parameter();
        public Parameter parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        private SFLViewMode m_ViewMode;
        public SFLViewMode viewMode
        {
            get { return m_ViewMode; }
            set { m_ViewMode = value; }
        }
        private string m_NickName;
        public string nickname
        {
            get { return m_NickName; }
            set { m_NickName = value; }
        }
        private UInt32 m_Guid = 0;
        public UInt32 guid
        {
            get { return m_Guid; }
            set { m_Guid = value; }
        }
        private UInt16 m_EditorType;
        public UInt16 editortype
        {
            get { return m_EditorType; }
            set { m_EditorType = value; }
        }

        private string m_Catalog;
        public string catalog
        {
            get { return m_Catalog; }
            set { m_Catalog = value; }
        }

        private UInt16 m_Format;
        public UInt16 format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        private double m_MinValue;
        public double minvalue
        {
            get { return m_MinValue; }
            set
            {
                if (m_MinValue != value)
                {
                    m_MinValue = value;
                    OnPropertyChanged("minvalue");
                }
            }
        }

        private double m_MaxValue;
        public double maxvalue
        {
            get { return m_MaxValue; }
            set
            {
                if (m_MaxValue != value)
                {
                    m_MaxValue = value;
                    OnPropertyChanged("maxvalue");
                }
            }
        }
        private bool m_bEdit;
        public bool bedit       //Binding to IsEnable
        {
            get { return m_bEdit; }
            set
            {
                m_bEdit = value;
                OnPropertyChanged("bedit");
            }
        }
        private string m_Description;
        public string description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        private Int32 m_Order;
        public Int32 order
        {
            get { return m_Order; }
            set { m_Order = value; }
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

        private AsyncObservableCollection<UInt32> m_Relations = new AsyncObservableCollection<UInt32>();
        public AsyncObservableCollection<UInt32> relations
        {
            get { return m_Relations; }
            set
            {
                m_Relations = value;
                OnPropertyChanged("relations");
            }
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

        private bool m_bRange;
        public bool brange      //Using phydata or sphydata
        {
            get { return m_bRange; }
            set
            {
                m_bRange = value;
                OnPropertyChanged("brange");
            }
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

        private bool m_bCheck;
        public bool bcheck
        {
            get { return m_bCheck; }
            set
            {
                m_bCheck = value;
                OnPropertyChanged("bcheck");
            }
        }

        private bool m_bError;
        public bool berror
        {
            get { return m_bError; }
            set
            {
                m_bError = value;
                OnPropertyChanged("berror");
            }
        }

        private double m_Data;
        public double data
        {
            get { return m_Data; }
            set
            {
                if (m_Data != value)
                {
                    m_Data = value;
                    OnPropertyChanged("data");
                }
            }
        }

        private UInt32 m_ErrorCode;
        public UInt32 errorcode
        {
            get { return m_ErrorCode; }
            set { m_ErrorCode = value; }
        }

        /// <summary>
        /// 参数初始化
        /// </summary>
        /// <param name="node"></param>
        public SFLModel(SFLViewMode vm, XElement node)
        {
            bool bdata = false;
            double ddata = 0;
            string tmp = string.Empty;
            viewMode = vm;

            if (parent.GetXElementValueByAttribute(node, "Guid") != null)
                guid = parent.guid = Convert.ToUInt32(parent.GetXElementValueByAttribute(node, "Guid"), 16);

            if (parent.GetXElementValueByName(node, "Key") != null)
                parent.key = Convert.ToDouble(parent.GetXElementValueByName(node, "Key"));

            if (parent.GetXElementValueByName(node, "SubType") != null)
                parent.subtype = Convert.ToUInt16(parent.GetXElementValueByName(node, "SubType"));

            if (parent.GetXElementValueByName(node, "SubSection") != null)
                parent.subsection = Convert.ToUInt16(parent.GetXElementValueByName(node, "SubSection"));

            if (parent.GetXElementValueByName(node, "Offset") != null)
                parent.offset = Convert.ToDouble(parent.GetXElementValueByName(node, "Offset"));

            if (parent.GetXElementValueByName(node, "PhysicalData") != null)
                parent.phydata = Convert.ToDouble(parent.GetXElementValueByName(node, "PhysicalData"));

            if (parent.GetXElementValueByName(node, "PhyRef") != null)
                parent.phyref = Convert.ToDouble(parent.GetXElementValueByName(node, "PhyRef"));

            if (parent.GetXElementValueByName(node, "RegRef") != null)
                parent.regref = Convert.ToDouble(parent.GetXElementValueByName(node, "RegRef"));

            if (parent.GetXElementValueByName(node, "HexMin") != null)
                parent.dbHexMin = Convert.ToInt32(parent.GetXElementValueByName(node, "HexMin"), 16);

            if (parent.GetXElementValueByName(node, "HexMax") != null)
                parent.dbHexMax = Convert.ToInt32(parent.GetXElementValueByName(node, "HexMax"), 16);

            if (parent.GetXElementValueByName(node, "PhyMin") != null)
                parent.dbPhyMin = Convert.ToDouble(parent.GetXElementValueByName(node, "PhyMin"));

            if (parent.GetXElementValueByName(node, "PhyMax") != null)
                parent.dbPhyMax = Convert.ToDouble(parent.GetXElementValueByName(node, "PhyMax"));

            XElement itemnodes = node.Element("ItemList");
            if (itemnodes != null)
            {
                IEnumerable<XElement> items = from Target in itemnodes.Elements() select Target;
                foreach (XElement item in items)
                    parent.itemlist.Add(item.Value);
            }
            XElement Nodes = node.Element("LocationList");
            if (Nodes != null)
            {
                IEnumerable<XElement> snode = from Target in Nodes.Elements("Location") where Target.HasElements select Target;
                foreach (XElement ssnode in snode)
                {
                    tmp = parent.GetXElementValueByAttribute(ssnode, "Position");
                    Reg register = new Reg();

                    if (parent.GetXElementValueByName(ssnode, "Address") != null)
                        register.address = Convert.ToUInt16(parent.GetXElementValueByName(ssnode, "Address"), 16);

                    if (parent.GetXElementValueByName(ssnode, "StartBit") != null)
                        register.startbit = Convert.ToUInt16(parent.GetXElementValueByName(ssnode, "StartBit"), 10);

                    if (parent.GetXElementValueByName(ssnode, "BitsNumber") != null)
                        register.bitsnumber = Convert.ToUInt16(parent.GetXElementValueByName(ssnode, "BitsNumber"), 10);

                    parent.reglist.Add(tmp, register);
                }
            }
            parent.errorcode = LibErrorCode.IDS_ERR_SUCCESSFUL;

            XElement sflnodes = node.Element("Private");
            if (sflnodes != null)
            {
                IEnumerable<XElement> sflitems = from Target in sflnodes.Elements() select Target;
                foreach (XElement sflitem in sflitems)
                {
                    if (!sflitem.HasElements) continue;
                    if (parent.GetXElementValueByName(sflitem, "NickName") != null)
                        nickname = parent.GetXElementValueByName(sflitem, "NickName");

                    if (parent.GetXElementValueByName(sflitem, "bEdit") != null)
                    {
                        if (!Boolean.TryParse(parent.GetXElementValueByName(sflitem, "bEdit"), out bdata))
                            bedit = true;
                        else
                            bedit = bdata;
                    }
                    else
                        bedit = true;

                    if (parent.GetXElementValueByName(sflitem, "EditType") != null)
                        editortype = Convert.ToUInt16(parent.GetXElementValueByName(sflitem, "EditType"));

                    if (parent.GetXElementValueByName(sflitem, "Format") != null)
                        format = Convert.ToUInt16(parent.GetXElementValueByName(sflitem, "Format"));

                    if (parent.GetXElementValueByName(sflitem, "MinValue") != null)
                        minvalue = Convert.ToDouble(parent.GetXElementValueByName(sflitem, "MinValue"));

                    if (parent.GetXElementValueByName(sflitem, "MaxValue") != null)
                        maxvalue = Convert.ToDouble(parent.GetXElementValueByName(sflitem, "MaxValue"));

                    if (parent.GetXElementValueByName(sflitem, "BRange") != null)
                    {
                        if (!Boolean.TryParse(parent.GetXElementValueByName(sflitem, "BRange"), out bdata))
                            brange = true;
                        else
                            brange = bdata;
                    }
                    else
                        brange = true;

                    if (parent.GetXElementValueByName(sflitem, "Relations") != null)
                    {
                        foreach (XElement el in sflitem.Element("Relations").Descendants())
                        {
                            if (String.IsNullOrEmpty(el.Value)) continue;
                            relations.Add(Convert.ToUInt32(el.Value, 16));
                        }
                    }
                    if (parent.GetXElementValueByName(sflitem, "DefValue") != null)
                    {
                        if (!brange)
                            sphydata = parent.GetXElementValueByName(sflitem, "DefValue");
                        else
                        {
                            if (Double.TryParse(parent.GetXElementValueByName(sflitem, "DefValue"), out ddata))
                                data = ddata;
                            else
                                sphydata = parent.GetXElementValueByName(sflitem, "DefValue");
                        }
                    }

                    if (parent.GetXElementValueByName(sflitem, "Description") != null)
                        description = parent.GetXElementValueByName(sflitem, "Description");

                    if (parent.GetXElementValueByName(sflitem, "Catalog") != null)
                        catalog = parent.GetXElementValueByName(sflitem, "Catalog");
                }
            }

            berror = false;
            itemlist = parent.itemlist;
            parent.PropertyChanged += new PropertyChangedEventHandler(viewMode.Parameter_PropertyChanged);
            this.PropertyChanged += new PropertyChangedEventHandler(viewMode.SFL_Parameter_PropertyChanged);
            viewMode.phyTostr(this);
        }
    }
}
