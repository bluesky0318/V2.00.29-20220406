using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.MCUConfigurationPanel
{
    public class SFLModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Parameter m_Parent;
        public Parameter parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        private string m_NickName;
        public string nickname
        {
            get { return m_NickName; }
            set { m_NickName = value; }
        }

        private string m_Address;
        public string address
        {
            get { return m_Address; }
            set { m_Address = value; }
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

        private UInt32 m_Guid;
        public UInt32 guid
        {
            get { return m_Guid; }
            set { m_Guid = value; }
        }

        //参数在SFL参数列表中位置
        private Int32 m_Order;
        public Int32 order
        {
            get { return m_Order; }
            set { m_Order = value; }
        }

        private string m_Catalog;
        public string catalog
        {
            get { return m_Catalog; }
            set { m_Catalog = value; }
        }

        private UInt16 m_EditorType;
        public UInt16 editortype
        {
            get { return m_EditorType; }
            set { m_EditorType = value; }
        }

        private UInt16 m_Format;
        public UInt16 format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        private string m_Description;
        public string description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        private double m_MinValue;
        public double minvalue
        {
            get { return m_MinValue; }
            set { m_MinValue = value; }
        }

        private double m_MaxValue;
        public double maxvalue
        {
            get { return m_MaxValue; }
            set { m_MaxValue = value; }
        }

        private bool m_bEdit;
        public bool bedit
        {
            get { return m_bEdit; }
            set
            {
                m_bEdit = value;
                OnPropertyChanged("bedit");
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

        private bool m_bRange;
        public bool brange
        {
            get { return m_bRange; }
            set
            {
                m_bRange = value;
                OnPropertyChanged("brange");
            }
        }

        private string m_sPhydata;
        public string sphydata
        {
            get { return m_sPhydata; }
            set
            {
                //if (m_sPhydata != value)
                {
                    m_sPhydata = value;
                    OnPropertyChanged("sphydata");
                }
            }
        }

        private UInt16 m_EventMode;
        public UInt16 eventmode
        {
            get { return m_EventMode; }
            set
            {
                //if (m_EventMode != value)
                {
                    m_EventMode = value;
                    OnPropertyChanged("eventmode");
                }
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

        private bool m_bROne;
        public bool brone
        {
            get { return m_bROne; }
            set
            {
                //if (m_bCheck != value)
                {
                    m_bROne = value;
                    OnPropertyChanged("brone");
                }
            }
        }

        private bool m_bWOne;
        public bool bwone
        {
            get { return m_bWOne; }
            set
            {
                //if (m_bCheck != value)
                {
                    m_bWOne = value;
                    OnPropertyChanged("bwone");
                }
            }
        }

        private bool m_bSubMenu;
        public bool bsubmenu
        {
            get { return m_bSubMenu; }
            set
            {
                //if (m_bCheck != value)
                {
                    m_bSubMenu = value;
                    OnPropertyChanged("bsubmenu");
                }
            }
        }

        private UInt32 m_ErrorCode;
        public UInt32 errorcode
        {
            get { return m_ErrorCode; }
            set { m_ErrorCode = value; }
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

        public SFLModel()
        {
            sphydata = string.Empty;
        }
    }
}
