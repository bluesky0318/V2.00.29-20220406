using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.CalibratePanel
{
    public class Model
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

        private string m_Name;
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
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


        private double m_dbPhyMin;
        public double dbPhyMin
        {
            get { return m_dbPhyMin; }
            set
            {
                {
                    m_dbPhyMin = value;
                    OnPropertyChanged("dbPhyMin");
                }
            }
        }

        private double m_dbPhyMax;
        public double dbPhyMax
        {
            get { return m_dbPhyMax; }
            set
            {

                {
                    m_dbPhyMax = value;
                    OnPropertyChanged("dbPhyMax");
                }
            }
        }

        private UInt32 m_Guid;
        public UInt32 guid
        {
            get { return m_Guid; }
            set { m_Guid = value; }
        }

        private UInt16 m_Type;
        public UInt16 type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        private UInt32 m_ErrorCode;
        public UInt32 errorcode
        {
            get { return m_ErrorCode; }
            set { m_ErrorCode = value; }
        }
    }

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
}
