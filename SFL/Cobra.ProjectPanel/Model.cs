using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Cobra.ProjectPanel
{
    public enum FILE_TYPE
    {
        FILE_HEX = 0x01,
        FILE_PARAM = 0x02,
        FILE_THERMAL_TABLE = 0x03,
        FILE_OCV_TABLE = 0x04,
        FILE_SELF_DISCH_TABLE = 0x05,
        FILE_RC_TABLE = 0x06,
        FILE_FD_TABLE = 0x07,
        FILE_FGLITE_TABLE = 0x08,
    }

    public class Proj : INotifyPropertyChanged //详细信息，可变动
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        private ObservableCollection<ProjFile> m_projFiles = new ObservableCollection<ProjFile>();
        public ObservableCollection<ProjFile> projFiles
        {
            get { return m_projFiles; }
            set { m_projFiles = value; NotifyPropertyChanged("projFiles"); }
        }

        private string m_Name;
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; NotifyPropertyChanged("name"); }
        }

        private string m_Info;
        public string info
        {
            get { return m_Info; }
            set { m_Info = value; NotifyPropertyChanged("info"); }
        }

        private bool m_bReady;
        public bool bReady
        {
            get { return m_bReady; }
            set { m_bReady = value; NotifyPropertyChanged("bReady"); }
        }


        public Proj(string name)
        {
            m_bReady = false;
            m_Name = name;
            m_Info = string.Empty;
        }

        public Proj DeepCopy()
        {
            Proj pj = new Proj(string.Empty);
            pj.m_Name = this.name;
            pj.info = this.info;
            pj.projFiles.Clear();
            foreach (ProjFile pf in this.projFiles)
            {
                ProjFile pfl = pf.DeepCopy();
                pj.projFiles.Add(pfl);
            }
            return pj;
        }

        public void Remove(ProjFile pfl)
        {
            foreach (ProjFile pf in projFiles)
            {
                if (pfl.type == pf.type)
                {
                    projFiles.Remove(pf);
                    return;
                }
            }
        }

        public ProjFile GetFileByType(FILE_TYPE type)
        {
            foreach (ProjFile pf in projFiles)
            {
                if (pf.type == (UInt16)type)
                    return pf;
            }
            return null;
        }
    }


    public class TableHeader
    {
        public UInt16 HeaderSize;
        public Int16 ScaleControl;
        public UInt16 NumOfAxis;
        public byte[] NumOfPoints;
        public UInt16 YAxisEntry;
        public UInt16 TotalPoints;

        public TableHeader()
        {
            HeaderSize = 0;
            ScaleControl = 0;
            NumOfAxis = 0;
            NumOfPoints = null;
            YAxisEntry = 0;
            TotalPoints = 0;
        }
    };

    public class ProjFile : INotifyPropertyChanged
    {
        public TableHeader tableHeader = new TableHeader();

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #region 来自XML信息
        private UInt16 m_Index;
        public UInt16 index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        private UInt16 m_Type;
        public UInt16 type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        private string m_Folder;
        public string folder
        {
            get { return m_Folder; }
            set { m_Folder = value; }
        }

        private UInt32 m_xAxisPointsStartAddress = 0;
        public UInt32 xAxisPointsAddress
        {
            get { return m_xAxisPointsStartAddress; }
            set { m_xAxisPointsStartAddress = value; }
        }

        private UInt32 m_yAxisPointsStartAddress = 0;
        public UInt32 yAxisPointsAddress
        {
            get { return m_yAxisPointsStartAddress; }
            set { m_yAxisPointsStartAddress = value; }
        }

        private UInt32 m_zAxisPointsStartAddress = 0;
        public UInt32 zAxisPointsAddress
        {
            get { return m_zAxisPointsStartAddress; }
            set { m_zAxisPointsStartAddress = value; }
        }

        private UInt32 m_wAxisPointsStartAddress = 0;
        public UInt32 wAxisPointsAddress
        {
            get { return m_wAxisPointsStartAddress; }
            set { m_wAxisPointsStartAddress = value; }
        }

        private UInt32 m_StartAddress;
        public UInt32 startAddress
        {
            get { return m_StartAddress; }
            set { m_StartAddress = value; }
        }

        private UInt32 m_Size;
        public UInt32 size
        {
            get { return m_Size; }
            set
            {
                if (m_Size != value)
                {
                    m_Size = value;
                    data = new byte[size];
                    for (int i = 0; i < size; i++)
                        data[i] = 0xFF;
                }
            }
        }
        #endregion

        #region 解析补充信息
        private string m_Name;
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; OnPropertyChanged("name"); }
        }

        private string m_FolderPath;
        public string folderPath
        {
            get { return m_FolderPath; }
            set { m_FolderPath = value; OnPropertyChanged("folderPath"); }
        }

        public string fullName
        {
            get { return System.IO.Path.Combine(folderPath, name); }
        }

        private string m_ToolTip;
        public string toolTip
        {
            get { return m_ToolTip; }
            set { m_ToolTip = value; OnPropertyChanged("toolTip"); }
        }

        private string m_Info;
        public string info
        {
            get { return m_Info; }
            set { m_Info = value; OnPropertyChanged("info"); }
        }

        private SubUserControl m_userCtrl;
        public SubUserControl userCtrl
        {
            get { return m_userCtrl; }
            set { m_userCtrl = value; OnPropertyChanged("userCtrl"); }
        }

        private byte[] m_data;
        public byte[] data
        {
            get { return m_data; }
            set { m_data = value; }
        }
        #endregion

        #region UI绑定数据
        private bool m_bExist;
        public bool bExist
        {
            get { return m_bExist; }
            set { m_bExist = value; OnPropertyChanged("bExist"); }
        }

        private bool m_bShow = false;
        public bool bshow
        {
            get { return m_bShow; }
            set { m_bShow = value; OnPropertyChanged("bshow"); }
        }
        #endregion

        public ProjFile()
        {

        }

        public ProjFile DeepCopy()
        {
            ProjFile pf = new ProjFile();
            pf.index = this.index;
            pf.type = this.type;
            pf.folder = this.folder;
            pf.startAddress = this.startAddress;
            pf.xAxisPointsAddress = this.xAxisPointsAddress;
            pf.yAxisPointsAddress = this.yAxisPointsAddress;
            pf.zAxisPointsAddress = this.zAxisPointsAddress;
            pf.wAxisPointsAddress = this.wAxisPointsAddress;
            pf.size = this.size;
            pf.name = this.name;
            pf.folderPath = this.folderPath;
            return pf;
        }
    }
}
