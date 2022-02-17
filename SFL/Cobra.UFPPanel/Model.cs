using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using Cobra.Common;

namespace Cobra.UFPPanel
{
	public class Model : INotifyPropertyChanged
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

        private string m_Catalog;
        public string catalog
        {
            get { return m_Catalog; }
            set 
            {
                m_Catalog = value;
                OnPropertyChanged("catalog");
            }
        }

        private byte m_TmpStyle;
        public byte tmpstyle
        {
            get { return m_TmpStyle; }
            set
            {
                m_TmpStyle = value;
                OnPropertyChanged("tmpstyle");
            }
        }

        private byte m_DataType;
        public byte dataType
        {
            get { return m_DataType; }
            set 
            { 
                m_DataType = value; 
                OnPropertyChanged("dataType");
            }
        }

        //参数归类Flag
        private UInt32 m_Flag;
        public UInt32 flag
        {
            get { return m_Flag; }
            set
            {
                m_Flag = value;
                OnPropertyChanged("flag");
            }
        }

        private bool m_bRead;
		public bool bRead 
		{
			get { return m_bRead; }
			set { m_bRead = value; OnPropertyChanged("bRead"); }
		}

		private bool m_bWrite;
		public bool bWrite
		{
			get { return m_bWrite; }
			set { m_bWrite = value; OnPropertyChanged("bWrite"); }
		}

        private string m_Description;
        public string description
        {
            get { return m_Description; }
            set
            {
                m_Description = value;
                OnPropertyChanged("description");
            }
        }

        private AsyncObservableCollection<subModel> m_subModel_List = new AsyncObservableCollection<subModel>();
        public AsyncObservableCollection<subModel> subModel_List
        {
            get { return m_subModel_List; }
            set { m_subModel_List = value; }
        }

#region Test Area Parameter
        private byte m_bReg0;
        public byte bReg0
        {
            get { return m_bReg0; }
            set
            {
                m_bReg0 = value;
                OnPropertyChanged("bReg0");
            }
        }

        private byte m_bReg1;
        public byte bReg1
        {
            get { return m_bReg1; }
            set
            {
                m_bReg1 = value;
                OnPropertyChanged("bReg1");
            }
        }

        private byte m_bReg2;
        public byte bReg2
        {
            get { return m_bReg2; }
            set
            {
                m_bReg2 = value;
                OnPropertyChanged("bReg2");
            }
        }

        private byte m_bReg3;
        public byte bReg3
        {
            get { return m_bReg3; }
            set
            {
                m_bReg3 = value;
                OnPropertyChanged("bReg3");
            }
        }

        private byte m_bReg4;
        public byte bReg4
        {
            get { return m_bReg4; }
            set
            {
                m_bReg4 = value;
                OnPropertyChanged("bReg4");
            }
        }

        private byte m_bReg5;
        public byte bReg5
        {
            get { return m_bReg5; }
            set
            {
                m_bReg5 = value;
                OnPropertyChanged("bReg5");
            }
        }
#endregion

        public void AddSmodel(ref subModel sm)
        {
            UInt32 guid = (sm.parent.guid & ViewMode.CommandMask2);
            foreach (subModel smodel in subModel_List)
            {
                if ((smodel.parent.guid & ViewMode.CommandMask2) == guid)
                {
                    smodel.ssubModel_List.Add(sm);
                    return;
                }
            }
            sm.ssubModel_List.Add(sm);
            subModel_List.Add(sm);
        }
	}

    public class subModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            switch (propName)
            {
                case "data":
                    {
                        if (tpModel_List != null)
                            ParseTPModel();
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

        private string m_NickName;
        public string nickname
        {
            get { return m_NickName; }
            set
            {
                m_NickName = value;
                OnPropertyChanged("nickname");
            }
        }

        private string m_SubNickName;
        public string subnickname
        {
            get { return m_SubNickName; }
            set
            {
                m_SubNickName = value;
                OnPropertyChanged("subnickname");
            }
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

        private AsyncObservableCollection<subModel> m_ssubModel_List = new AsyncObservableCollection<subModel>();
        public AsyncObservableCollection<subModel> ssubModel_List
        {
            get { return m_ssubModel_List; }
            set { m_ssubModel_List = value; }
        }

        private bool m_bRead;
        public bool bRead
        {
            get { return m_bRead; }
            set { m_bRead = value; OnPropertyChanged("bRead"); }
        }

        private bool m_bWrite;
        public bool bWrite
        {
            get { return m_bWrite; }
            set { m_bWrite = value; OnPropertyChanged("bWrite"); }
        }

        #region TP Parameter
        public System.Windows.Visibility bToolTip
        {
            get { return (tpModel_List == null)? System.Windows.Visibility.Hidden:System.Windows.Visibility.Visible; }
        }

        private string m_Caption;
        public string caption
        {
            get { return m_Caption; }
            set
            {
                m_Caption = value;
                OnPropertyChanged("caption");
            }
        }

        public AsyncObservableCollection<TPModel> tpModel_List
        {
            get 
            {
                if (tps_List.Count ==0)
                    return null;
                return m_TPS_List[caption];
            }
            set
            {
                tpModel_List = value;
                OnPropertyChanged("tpModel_List");
            }
        }

        private Dictionary<string, AsyncObservableCollection<TPModel>> m_TPS_List = new Dictionary<string, AsyncObservableCollection<TPModel>>();
        public Dictionary<string, AsyncObservableCollection<TPModel>> tps_List
        {
            get { return m_TPS_List; }
            set { m_TPS_List = value; }
        }

        private void ParseTPModel()
        {
            UInt32 udata = (UInt32)data;
            switch((byte)(udata>>30))
            {
                case 0:
                    caption = "Fixed supply";
                    break;
                case 1:
                    caption = "Battery";
                    break;
                case 2:
                    caption = "Variable Supply";
                    break;
                case 3:
                    caption = "Augmented Power Data Object";
                    break;
                
            }
            foreach (TPModel tpModel in tpModel_List)
            {
                tpModel.data = (udata << (32 - tpModel.startbit - tpModel.bitsnumber)) >> (32 - tpModel.bitsnumber); 
            }
        }
        #endregion
    }

    public class TPModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private string m_NickName;
        public string nickname
        {
            get { return m_NickName; }
            set { m_NickName = value; }
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

        private string m_Catalog;
        public string catalog
        {
            get { return m_Catalog; }
            set { m_Catalog = value; }
        }

        private string m_Description;
        public string description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        private UInt16 m_StartBit;
        public UInt16 startbit
        {
            get { return m_StartBit; }
            set { m_StartBit = value; }
        }

        private UInt16 m_BitsNumber;
        public UInt16 bitsnumber
        {
            get { return m_BitsNumber; }
            set { m_BitsNumber = value; }
        }
    }
}
