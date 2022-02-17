using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using System.Data;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.UFPSBSPanel
{
    /// <summary>
    /// Interaction logic for LogDataControl.xaml
    /// </summary>
    public partial class LogDataControl : UserControl
    {
        private MainControl m_Parent;
        public MainControl parent { get; set; }

        private DataTable m_logDT = new DataTable();
        public DataTable logdt
        {
            get { return m_logDT; }
            set { m_logDT = value; }
        }

        private AsyncObservableCollection<DataBaseRecord> m_DataBaseRecords = new AsyncObservableCollection<DataBaseRecord>();
        public AsyncObservableCollection<DataBaseRecord> dataBaseRecords    //LogData的集合
        {
            get { return m_DataBaseRecords; }
            set
            {
                m_DataBaseRecords = value;
            }
        }

        public LogDataControl()
        {
            InitializeComponent();
        }

        public void Init(object pParent)
        {
            parent = (MainControl)pParent;

            BuildColumn();
            UpdateDBRecordList();
            logDataGrid.DataContext = logdt.DefaultView;
            dbRecordDataGrid.ItemsSource = dataBaseRecords;
        }

        public void update()
        {
            Dictionary<string, string> records = new Dictionary<string, string>();
            try
            {
                Dispatcher.Invoke(new Action(delegate()
                {
                    Model mo = null;
                    DataRow row = logdt.NewRow();
                    foreach (DataColumn col in logdt.Columns)
                    {
                        mo = parent.viewmode.GetParameterByColumName(col.ColumnName);
                        if (mo == null) continue;

                        decimal num = new decimal((double)mo.data);
                        row[col.ColumnName] = Decimal.Round(num, 1).ToString();
                        records.Add(col.ColumnName, mo.sphydata);
                    }
                    row["Time"] = DateTime.Now;
                    if (logdt.Rows.Count >= 1000)
                        logdt.Rows.RemoveAt(0);

                    logdt.Rows.Add(row);
                    parent.parent.db_Manager.BeginNewRow(parent.session_id, records);
                    parent.session_row_number += 1;
                    logDataGrid.ScrollIntoView(logDataGrid.Items[logDataGrid.Items.Count - 1]);
                }));
            }
            catch (System.Exception ex)
            {

            }
        }


        public void BuildColumn()
        {
            DataColumn col;
            logdt.Clear();
            logdt.Columns.Clear();
            foreach (Model mo in parent.viewmode.sfl_parameterlist)
            {
                col = new DataColumn();
                col.DataType = System.Type.GetType("System.String");
                col.ColumnName = mo.nickname;
                col.AutoIncrement = false;
                col.ReadOnly = false;
                col.Unique = false;
                logdt.Columns.Add(col);
            }
            col = new DataColumn();
            col.DataType = System.Type.GetType("System.DateTime");
            col.ColumnName = "Time";
            col.AutoIncrement = false;
            col.Caption = "Time";
            col.ReadOnly = false;
            col.Unique = false;
            logdt.Columns.Add(col);

            logDataGrid.DataContext = null;
            logDataGrid.DataContext = logdt.DefaultView;
        }

        #region DB Operation
        public void UpdateDBRecordList()
        {
            List<List<String>> records = new List<List<string>>();
            if (parent.session_id != 0 && parent.session_row_number != 0)
                parent.parent.db_Manager.UpdateSessionSize(parent.session_id, parent.session_row_number);
            parent.parent.db_Manager.GetSessionsInfor(parent.sflname, ref records);

            dataBaseRecords.Clear();
            foreach (var record in records)
            {
                DataBaseRecord ld = new DataBaseRecord();
                ld.Timestamp = record[0];
                ld.RecordNumber = Convert.ToInt64(record[1]);
                dataBaseRecords.Add(ld);
            }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = "";
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            DataBaseRecord dbRecord = (sender as Button).DataContext as DataBaseRecord;
            string tmp = dbRecord.Timestamp;
            char[] skip = { ' ', '/', ':' };
            foreach (var s in skip)
            {
                tmp = tmp.Replace(s, '_');
            }
            tmp = "Scan_" + tmp;
            saveFileDialog.FileName = tmp;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.Title = "Export DB data to csv file";
            saveFileDialog.Filter = "CSV file (*.csv)|*.csv||";
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.InitialDirectory = FolderMap.m_logs_folder;
            if (saveFileDialog.ShowDialog() == true)
            {
                DataTable dt = new DataTable();
                parent.parent.db_Manager.GetOneSession(parent.sflname, dbRecord.Timestamp, ref dt);
                fullpath = saveFileDialog.FileName;
                ExportDB(fullpath, dt);
            }
        }

        public bool ExportDB(string fullpath, DataTable dt) //Save buffer content to hard disk as temperary file, then clear buffer
        {
            FileStream file = new FileStream(fullpath, FileMode.Create);
            StreamWriter sw = new StreamWriter(file);
            int length;
            string str = "";
            foreach (DataColumn col in dt.Columns)
            {
                str += col.ColumnName + ",";
            }
            length = str.Length;
            str = str.Remove(length - 1);
            sw.WriteLine(str);

            foreach (DataRow row in dt.Rows)
            {
                str = "";
                foreach (DataColumn col in dt.Columns)
                {
                    str += row[col.ColumnName] + ",";
                }
                length = str.Length;
                str = str.Remove(length - 1);
                sw.WriteLine(str);
            }
            sw.Close();
            file.Close();
            return true;
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            DataBaseRecord dbRecord = (sender as Button).DataContext as DataBaseRecord;

            DataTable dt = new DataTable();
            parent.parent.db_Manager.DeleteOneSession(parent.sflname, dbRecord.Timestamp);
            UpdateDBRecordList();
        }
        #endregion
    }
}
