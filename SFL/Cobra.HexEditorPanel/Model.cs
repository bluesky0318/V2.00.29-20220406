using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Cobra.Common;

namespace Cobra.HexEditorPanel
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

        private UInt32 m_Guid;
        public UInt32 guid
        {
            get { return m_Guid; }
            set { m_Guid = value; }
        }

        private string m_Name;
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; OnPropertyChanged("name"); }
        }

        private string m_Ext;
        public string ext
        {
            get { return m_Ext; }
            set { m_Ext = value; OnPropertyChanged("ext"); }
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

        private string m_Info;
        public string info
        {
            get { return m_Info; }
            set { m_Info = value; OnPropertyChanged("info"); }
        }

        private string m_Used;
        public string used
        {
            get { return m_Used; }
            set { m_Used = value; OnPropertyChanged("used"); }
        }

        private byte[] m_SzBin;
        public byte[] szBin
        {
            get { return m_SzBin; }
            set { m_SzBin = value; }
        }

        private byte[] m_SzLen;
        public byte[] szLen
        {
            get { return m_SzLen; }
            set { m_SzLen = value; }
        }

        private Int16[] m_SzAdd;
        public Int16[] szAdd
        {
            get { return m_SzAdd; }
            set { m_SzAdd = value; }
        }

        private List<MemoryControl> m_BufferList;
        public List<MemoryControl> bufferList
        {
            get { return m_BufferList; }
            set { m_BufferList = value; }
        }

        private bool m_bExist;
        public bool bExist
        {
            get { return m_bExist; }
            set { m_bExist = value; OnPropertyChanged("bExist"); }
        }

        public Model()
        {
            bExist = false;
            name = "Hex File";
            folderPath = string.Empty;
            info = string.Empty;
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
