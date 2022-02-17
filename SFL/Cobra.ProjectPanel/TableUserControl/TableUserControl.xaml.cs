using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Cobra.Common;

namespace Cobra.ProjectPanel.Table
{
    /// <summary>
    /// Interaction logic for TableUserControl.xaml
    /// </summary>
    public partial class TableUserControl : SubUserControl
    {
        private UserControl m_parent;
        public UserControl parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private ProjFile m_projFile = null;
        public ProjFile projFile
        {
            get { return m_projFile; }
            set { m_projFile = value; }
        }

        private TextRange range = null;
        private System.IO.FileStream _stream = null;
        private List<byte[]> m_axis_list = new List<byte[]>();
        private List<byte> m_y_axis_data = new List<byte>();

        public TableUserControl()
        {
            InitializeComponent();
        }

        public TableUserControl(object pParent, ref ProjFile pf)
        {
            #region 相关初始化
            parent = (UserControl)pParent;
            if (parent == null) return;

            projFile = pf;
            if (projFile == null) return;

            this.InitializeComponent();
            Init(this);
            #endregion
        }

        public void Init(object pParent)
        {
            parent = (UserControl)pParent;
            OpenFile(projFile);
        }

        public override void OpenFile(ProjFile pf)
        {
            if (System.IO.File.Exists(pf.fullName))
            {
                range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                _stream = new System.IO.FileStream(pf.fullName, System.IO.FileMode.OpenOrCreate);
                range.Load(_stream, System.Windows.DataFormats.Text);
                _stream.Close();
            }
            LoadAXIS_File();
        }

        public UInt32 LoadAXIS_File()
        {
            string line;
            string[] sArray;
            double ddata = 0;
            UInt16 ulen = 0;
            int arrayindex = 0;
            byte[] m_axis_data = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (!File.Exists(projFile.fullName))
                return LibErrorCode.IDS_ERR_SECTION_PROJECT_CONTENT_FILE_NOTEXIST; //should return error

            using (StreamReader sr = File.OpenText(@projFile.fullName))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == string.Empty) continue;
                    if (line.IndexOf("//") == 0) continue;
                    if (line.IndexOf("//") != -1)
                        line = line.Remove(line.IndexOf("//")).Trim();
                    sArray = line.Split(',');//only s[0] is data what we want.
                    for (int i = 0; i < sArray.Length; i++)
                    {
                        if (Double.TryParse(sArray[i], out ddata))
                        {
                            #region ParseHeader
                            if (ulen == 0)
                            {
                                projFile.tableHeader.HeaderSize = (UInt16)ddata;
                                break;
                            }
                            if ((ulen != 0) && (ulen < projFile.tableHeader.HeaderSize))
                            {
                                if (ulen == 1)
                                {
                                    projFile.tableHeader.ScaleControl = (Int16)ddata;
                                    break;
                                }
                                if (ulen == 2)
                                {
                                    projFile.tableHeader.NumOfAxis = (UInt16)ddata;
                                    projFile.tableHeader.NumOfPoints = new byte[(int)ddata];
                                    break;
                                }
                                if ((ulen >= 3) && (ulen < (3 + projFile.tableHeader.NumOfAxis)))
                                {
                                    projFile.tableHeader.NumOfPoints[ulen - 3] = (byte)ddata; //X W V
                                    m_axis_data = new byte[(int)(ddata * 2)];
                                    m_axis_list.Add(m_axis_data);
                                    break;
                                }
                                if (ulen == 3 + projFile.tableHeader.NumOfAxis)
                                {
                                    projFile.tableHeader.YAxisEntry = (UInt16)ddata;
                                    break;
                                }
                                if (ulen == 4 + projFile.tableHeader.NumOfAxis)
                                {
                                    projFile.tableHeader.TotalPoints = (UInt16)ddata;
                                    break;
                                }
                            }
                            #endregion
                            #region ParseAxis
                            if ((ulen >= projFile.tableHeader.HeaderSize) && (ulen < (projFile.tableHeader.HeaderSize + projFile.tableHeader.NumOfAxis)))
                            {
                                m_axis_list[(ulen - projFile.tableHeader.HeaderSize)][arrayindex] = SharedFormula.LoByte((UInt16)ddata);
                                m_axis_list[(ulen - projFile.tableHeader.HeaderSize)][arrayindex + 1] = SharedFormula.HiByte((UInt16)ddata);
                                arrayindex += 2;
                            }
                            #endregion
                            if (ulen >= (projFile.tableHeader.HeaderSize + projFile.tableHeader.NumOfAxis))
                            {
                                m_y_axis_data.Add(SharedFormula.LoByte((UInt16)ddata));
                                m_y_axis_data.Add(SharedFormula.HiByte((UInt16)ddata));
                            }
                        }
                    }
                    ulen++;
                    arrayindex = 0;
                }
                sr.Close();
                BuildTableImage();
            }
            return ret;
        }

        #region Build Table Image
        public void BuildTableImage()
        {
            switch ((FILE_TYPE)projFile.type)
            {
                case FILE_TYPE.FILE_THERMAL_TABLE:
                case FILE_TYPE.FILE_OCV_TABLE:
                case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                    BuildXYPairTable();
                    break;
                case FILE_TYPE.FILE_RC_TABLE:
                    BuildRCTable();
                    break;
                case FILE_TYPE.FILE_FD_TABLE:
                    BuildFDTable();
                    break;
            }
        }

        private void BuildXYPairTable()
        {
            int arrayindex = 0;
            for (int i = 0; i < projFile.tableHeader.NumOfPoints[0]; i++)
            {
                projFile.data[arrayindex * 2] = m_axis_list[0][arrayindex];
                projFile.data[arrayindex * 2 + 1] = m_axis_list[0][arrayindex + 1];
                projFile.data[arrayindex * 2 + 2] = m_y_axis_data[arrayindex + 0];
                projFile.data[arrayindex * 2 + 3] = m_y_axis_data[arrayindex + 1];
                arrayindex += 2;
            }
        }

        private void BuildRCTable()
        {
            /*
            int arrayindex = 0;
            for (int i = 0; i < m_axis_list.Count; i++)
            {
                Array.Copy(m_axis_list[i], 0, projFile.data, arrayindex, m_axis_list[i].Length);
                arrayindex += (m_axis_list[i].Length / 16 + 1) * 16;
            }
            Array.Copy(m_y_axis_data.ToArray(), 0, projFile.data, arrayindex, m_y_axis_data.Count);*/
            for (int i = 0; i < m_axis_list.Count; i++)
            {
                if(i == 0)
                    Array.Copy(m_axis_list[i], 0, projFile.data, 0, m_axis_list[i].Length);
                if (i == 1)
                    Array.Copy(m_axis_list[i], 0, projFile.data, (projFile.yAxisPointsAddress- projFile.startAddress), m_axis_list[i].Length);
                if (i == 2)
                    Array.Copy(m_axis_list[i], 0, projFile.data, (projFile.zAxisPointsAddress- projFile.startAddress), m_axis_list[i].Length);
            }
            Array.Copy(m_y_axis_data.ToArray(), 0, projFile.data, (projFile.wAxisPointsAddress- projFile.startAddress), m_y_axis_data.Count);
        }

        private void BuildFDTable()
        {
            int arrayindex = 0;
            for (int i = 0; i < m_axis_list.Count; i++)
            {
                Array.Copy(m_axis_list[i], 0, projFile.data, arrayindex, m_axis_list[i].Length/2);
                arrayindex += m_axis_list[i].Length/2;
            }
            for (int i = 0; i < m_axis_list.Count; i++)
            {
                Array.Copy(m_axis_list[i], m_axis_list[i].Length / 2, projFile.data, arrayindex, m_axis_list[i].Length / 2);
                arrayindex += m_axis_list[i].Length/2;
            }
            for (int i = 0; i < projFile.tableHeader.NumOfPoints.Length; i++)
                projFile.tableHeader.NumOfPoints[i] = (byte)(projFile.tableHeader.NumOfPoints[i] / 4);
        }
        #endregion

        public override void SaveFile(ProjFile pf)
        {
        }

        public override void CloseFile()
        {
            GC.SuppressFinalize(this);
        }
    }
}
