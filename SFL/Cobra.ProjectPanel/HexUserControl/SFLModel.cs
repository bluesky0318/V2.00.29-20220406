using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Cobra.ProjectPanel.Hex
{
    public class SFLModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));

            }
        }

        public string straddress
        {
            get { return m_address.ToString("X4"); }
            set { straddress = value; NotifyPropertyChanged("straddress"); }
        }

        private UInt32 m_address;
        public UInt32 address
        {
            get { return m_address; }
            set { m_address = value; NotifyPropertyChanged("address"); }
        }

        private string m_Index00;
        public string strIndex00
        {
            get { return m_data[00].ToString("X2"); }
            set { m_Index00 = value; NotifyPropertyChanged("strIndex00"); }
        }

        private string m_Index01;
        public string strIndex01
        {
            get { return m_data[01].ToString("X2"); }
            set { m_Index01 = value; NotifyPropertyChanged("strIndex01"); }
        }

        private string m_Index02;
        public string strIndex02
        {
            get { return m_data[02].ToString("X2"); }
            set { m_Index02 = value; NotifyPropertyChanged("strIndex02"); }
        }

        private string m_Index03;
        public string strIndex03
        {
            get { return m_data[03].ToString("X2"); }
            set { m_Index03 = value; NotifyPropertyChanged("strIndex03"); }
        }

        private string m_Index04;
        public string strIndex04
        {
            get { return m_data[04].ToString("X2"); }
            set { m_Index04 = value; NotifyPropertyChanged("strIndex04"); }
        }

        private string m_Index05;
        public string strIndex05
        {
            get { return m_data[05].ToString("X2"); }
            set { m_Index05 = value; NotifyPropertyChanged("strIndex05"); }
        }

        private string m_Index06;
        public string strIndex06
        {
            get { return m_data[06].ToString("X2"); }
            set { m_Index06 = value; NotifyPropertyChanged("strIndex06"); }
        }

        private string m_Index07;
        public string strIndex07
        {
            get { return m_data[07].ToString("X2"); }
            set { m_Index07 = value; NotifyPropertyChanged("strIndex07"); }
        }

        private string m_Index08;
        public string strIndex08
        {
            get { return m_data[08].ToString("X2"); }
            set { m_Index08 = value; NotifyPropertyChanged("strIndex08"); }
        }

        private string m_Index09;
        public string strIndex09
        {
            get { return m_data[09].ToString("X2"); }
            set { m_Index09 = value; NotifyPropertyChanged("strIndex09"); }
        }

        private string m_Index0A;
        public string strIndex0A
        {
            get { return m_data[10].ToString("X2"); }
            set { m_Index0A = value; NotifyPropertyChanged("strIndex0A"); }
        }

        private string m_Index0B;
        public string strIndex0B
        {
            get { return m_data[11].ToString("X2"); }
            set { m_Index0B = value; NotifyPropertyChanged("strIndex0B"); }
        }

        private string m_Index0C;
        public string strIndex0C
        {
            get { return m_data[12].ToString("X2"); }
            set { m_Index0C = value; NotifyPropertyChanged("strIndex0C"); }
        }

        private string m_Index0D;
        public string strIndex0D
        {
            get { return m_data[13].ToString("X2"); }
            set { m_Index0D = value; NotifyPropertyChanged("strIndex0D"); }
        }

        private string m_Index0E;
        public string strIndex0E
        {
            get { return m_data[14].ToString("X2"); }
            set { m_Index0E = value; NotifyPropertyChanged("strIndex0E"); }
        }

        private string m_Index0F;
        public string strIndex0F
        {
            get { return m_data[15].ToString("X2"); }
            set { m_Index0F = value; NotifyPropertyChanged("strIndex0F"); }
        }

        public Byte[] m_data { get; set; }

        public SFLModel()
        {
            m_data = new Byte[16];
        }
    }
}
