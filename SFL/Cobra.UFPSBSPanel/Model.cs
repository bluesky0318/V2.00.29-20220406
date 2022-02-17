using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.ComponentModel;
using Microsoft.Research.DynamicDataDisplay.Common;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.UFPSBSPanel
{
    public class DatePointCollection : RingArray<DatePoint>
    {
        public DatePointCollection() : base(216000)//ElementDefine.ShowMax) // here i set how much values to show 
        {
        }
    }

    public class DatePoint
    {
        public double time_point { get; set; }
        public double ddata { get; set; }
        public DatePoint(double data, double date)
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

        private ElementDefine.SBS_PARAM_SHOWMODE m_showMode;
        public ElementDefine.SBS_PARAM_SHOWMODE showMode
        {
            get { return m_showMode; }
            set { m_showMode = value; }
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
    }

    public class LogFile : INotifyPropertyChanged
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

    public class LogData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private DataTable m_DataTable = new DataTable();
        public DataTable dataTable
        {
            get { return m_DataTable; }
            set { m_DataTable = value; }
        }

        public void BuildColumn(Model md)
        {
            DataColumn col = new DataColumn();
            col.DataType = System.Type.GetType("System.String");
            col.ColumnName = md.logname;
            col.AutoIncrement = false;
            col.ReadOnly = false;
            col.Unique = false;
            dataTable.Columns.Add(col);
        }

        public void AddTimeColumn()
        {
            DataColumn col = new DataColumn();
            col.DataType = System.Type.GetType("System.DateTime");
            col.ColumnName = "Time";
            col.AutoIncrement = false;
            col.Caption = "Time";
            col.ReadOnly = false;
            col.Unique = false;
            dataTable.Columns.Add(col);
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
}
