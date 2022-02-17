using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;


namespace Cobra.Trim2Panel
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

        private UInt16 m_Format;
        public UInt16 format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        private bool m_bChecked = true;
        public bool bChecked
        {
            get { return m_bChecked; }
            set
            {
                m_bChecked = value;
                OnPropertyChanged("bChecked");
            }
        }

        private string m_Description;
        public string description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        private UInt32 m_ErrorCode;
        public UInt32 errorcode
        {
            get { return m_ErrorCode; }
            set { m_ErrorCode = value; }
        }

        private UInt16 m_SubType;
        public UInt16 subType
        {
            get { return m_SubType; }
            set { m_SubType = value; }
        }

        private Parameter m_Offset_Relation = new Parameter();
        public Parameter offset_relation
        {
            get { return m_Offset_Relation; }
            set
            {
                m_Offset_Relation = value;
                OnPropertyChanged("offset_relation");
            }
        }

        private Parameter m_Slope_Relation = new Parameter();
        public Parameter slope_relation
        {
            get { return m_Slope_Relation; }
            set
            {
                m_Slope_Relation = value;
                OnPropertyChanged("slope_relation");
            }
        }
    }

    public class OutPutModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Model m_Parent;
        public Model parent
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

        //参数在SFL参数列表中位置
        private Int32 m_Order;
        public Int32 order
        {
            get { return m_Order; }
            set { m_Order = value; }
        }

        private string m_SOffset;
        public string sOffset
        {
            get { return m_SOffset; }
            set
            {
                m_SOffset = value;
                OnPropertyChanged("sOffset");
            }
        }

        private string m_SSlope;
        public string sSlope
        {
            get { return m_SSlope; }
            set
            {
                m_SSlope = value;
                OnPropertyChanged("sSlope");
            }
        }

        private string m_SCode;
        public string sCode
        {
            get { return m_SCode; }
            set
            {
                m_SCode = value;
                OnPropertyChanged("sCode");
            }
        }
    }

    public class InPutModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private Model m_Parent;
        public Model parent
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

        //参数在SFL参数列表中位置
        private Int32 m_Order;
        public Int32 order
        {
            get { return m_Order; }
            set { m_Order = value; }
        }

        public bool bChecked
        {
            get { return parent.bChecked; }
            set
            {
                parent.bChecked = value;
                OnPropertyChanged("bChecked");
            }
        }

        private ObservableCollection<string> m_Input = new ObservableCollection<string>();
        public ObservableCollection<string> input
        {
            get { return m_Input; }
            set
            {
                m_Input = value;
                OnPropertyChanged("input");
            }
        }
    }
}
