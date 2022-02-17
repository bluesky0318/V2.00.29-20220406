using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.DBG2Panel
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

        private UInt32 m_Address;
        public UInt32 address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        //参数在SFL参数列表中位置
        private MByte m_Byte0 = new MByte(0,false);
        public MByte byte0
        {
            get { return m_Byte0; }
            set
            {
                m_Byte0 = value;
                OnPropertyChanged("byte0");
            }
        }

        private MByte m_Byte1 = new MByte(0,false);
        public MByte byte1
        {
            get { return m_Byte1; }
            set
            {
                m_Byte1 = value;
                OnPropertyChanged("byte1");
            }
        }

        private MByte m_Byte2 = new MByte(0, false);
        public MByte byte2
        {
            get { return m_Byte2; }
            set
            {
                m_Byte2 = value;
                OnPropertyChanged("byte2");
            }
        }

        private MByte m_Byte3 = new MByte(0, false);
        public MByte byte3
        {
            get { return m_Byte3; }
            set
            {
                m_Byte3 = value;
                OnPropertyChanged("byte3");
            }
        }

        private MByte m_Byte4 = new MByte(0, false);
        public MByte byte4
        {
            get { return m_Byte4; }
            set
            {
                m_Byte4 = value;
                OnPropertyChanged("byte4");
            }
        }

        private MByte m_Byte5 = new MByte(0, false);
        public MByte byte5
        {
            get { return m_Byte5; }
            set
            {
                m_Byte5 = value;
                OnPropertyChanged("byte5");
            }
        }

        private MByte m_Byte6 = new MByte(0, false);
        public MByte byte6
        {
            get { return m_Byte6; }
            set
            {
                m_Byte6 = value;
                OnPropertyChanged("byte6");
            }
        }

        private MByte m_Byte7 = new MByte(0, false);
        public MByte byte7
        {
            get { return m_Byte7; }
            set
            {
                m_Byte7 = value;
                OnPropertyChanged("byte7");
            }
        }

        private MByte m_Byte8 = new MByte(0, false);
        public MByte byte8
        {
            get { return m_Byte8; }
            set
            {
                m_Byte8 = value;
                OnPropertyChanged("byte8");
            }
        }

        private MByte m_Byte9 = new MByte(0, false);
        public MByte byte9
        {
            get { return m_Byte9; }
            set
            {
                m_Byte9 = value;
                OnPropertyChanged("byte9");
            }
        }

        private MByte m_Byte10 = new MByte(0, false);
        public MByte byte10
        {
            get { return m_Byte10; }
            set
            {
                m_Byte10 = value;
                OnPropertyChanged("byte10");
            }
        }

        private MByte m_Byte11 = new MByte(0, false);
        public MByte byte11
        {
            get { return m_Byte11; }
            set
            {
                m_Byte11 = value;
                OnPropertyChanged("byte11");
            }
        }

        private MByte m_Byte12 = new MByte(0, false);
        public MByte byte12
        {
            get { return m_Byte12; }
            set
            {
                m_Byte12 = value;
                OnPropertyChanged("byte12");
            }
        }

        private MByte m_Byte13 = new MByte(0, false);
        public MByte byte13
        {
            get { return m_Byte13; }
            set
            {
                m_Byte13 = value;
                OnPropertyChanged("byte13");
            }
        }

        private MByte m_Byte14 = new MByte(0, false);
        public MByte byte14
        {
            get { return m_Byte14; }
            set
            {
                m_Byte14 = value;
                OnPropertyChanged("byte14");
            }
        }

        private MByte m_Byte15 = new MByte(0, false);
        public MByte byte15
        {
            get { return m_Byte15; }
            set
            {
                m_Byte15 = value;
                OnPropertyChanged("byte15");
            }
        }
    }

    public class MByte : INotifyPropertyChanged
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

        public MByte(byte val, bool bc)
        {
            data = val;
            bchange = bc;
        }

        //参数在SFL参数列表中位置
        private byte m_data;
        public byte data
        {
            get { return m_data; }
            set
            {
                if (value != data)
                    bchange = true;
                else
                    bchange = false;                
                m_data = value;
                OnPropertyChanged("data");
            }
        }

        private bool m_bChange;
        public bool bchange
        {
            get { return m_bChange; }
            set
            {
                m_bChange = value;
                OnPropertyChanged("bchange");
            }
        }
    }
}
