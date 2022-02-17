using Cobra.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Cobra.BlackBoxPanel.StatisticLog
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

        private Model m_UpParam;
        public Model upParam
        {
            get { return m_UpParam; }
            set { m_UpParam = value; }
        }

        private Model m_minParam;
        public Model minParam
        {
            get { return m_minParam; }
            set { m_minParam = value; }
        }

        private Model m_maxParam;
        public Model maxParam
        {
            get { return m_maxParam; }
            set { m_maxParam = value; }
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

        private string m_Name;
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
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

        private string m_minData;
        public string minData
        {
            get
            {
                if (minParam != null)
                    m_minData = minParam.sphydata;
                return m_minData;
            }
            set
            {
                m_minData = value;
                OnPropertyChanged("minData");
            }
        }

        private string m_maxData;
        public string maxData
        {
            get
            {
                if (maxParam != null)
                    m_maxData = maxParam.sphydata;
                return m_maxData;
            }
            set
            {
                m_maxData = value;
                OnPropertyChanged("maxData");
            }
        }

        private UInt32 m_Upguid;
        public UInt32 upguid
        {
            get { return m_Upguid; }
            set { m_Upguid = value; }
        }

        //参数在SFL参数列表中位置
        private Int32 m_Order;
        public Int32 order
        {
            get { return m_Order; }
            set { m_Order = value; }
        }

        private UInt16 m_Catalog;
        public UInt16 catalog
        {
            get { return m_Catalog; }
            set { m_Catalog = value; }
        }

        private UInt16 m_Location;
        public UInt16 location
        {
            get { return m_Location; }
            set { m_Location = value; }
        }

        private UInt32 m_Guid;
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

        private UInt16 m_Format;
        public UInt16 format
        {
            get { return m_Format; }
            set { m_Format = value; }
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

        private UInt32 m_ErrorCode;
        public UInt32 errorcode
        {
            get { return m_ErrorCode; }
            set { m_ErrorCode = value; }
        }
    }
}
