 using Cobra.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Cobra.ProjectPanel.Table
{
    /// <summary>
    /// FGTableUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class FGTableUserControl : SubUserControl
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
        private string m_csvPath = string.Empty;
        private string m_cPath = string.Empty;

        public FGTableUserControl()
        {
            InitializeComponent();
        }

        public FGTableUserControl(object pParent, ref ProjFile pf)
        {
            #region 相关初始化
            parent = (UserControl)pParent;
            if (parent == null) return;

            projFile = pf;
            if (projFile == null) return;

            this.InitializeComponent();
            OpenFile(projFile);
            #endregion
        }

        public override void OpenFile(ProjFile pf)
        {
            string type = string.Empty;
            foreach (string file in System.IO.Directory.GetFiles(pf.folderPath))
            {
                type = System.IO.Path.GetExtension(file);
                switch (type)
                {
                    case ".c":
                        m_cPath = System.IO.Path.GetFullPath(file);
                        range = new TextRange(crichTB.Document.ContentStart, crichTB.Document.ContentEnd);
                        _stream = new System.IO.FileStream(file, System.IO.FileMode.OpenOrCreate);
                        range.Load(_stream, System.Windows.DataFormats.Text);
                        _stream.Close();
                        break;
                    case ".csv":
                        m_csvPath = System.IO.Path.GetFullPath(file);
                        string text = File.ReadAllText(m_csvPath, Encoding.Default);
                        range = new TextRange(csvrichTB.Document.ContentStart, csvrichTB.Document.ContentEnd);
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                        {
                            range.Load(ms, DataFormats.Text);
                        }
                        break;
                }
            }
        }

        public override void SaveFile(ProjFile pf)
        {
            string type = string.Empty;
            string filename = string.Empty;
            string targetpathfile = string.Empty;
            foreach (string file in System.IO.Directory.GetFiles(pf.folderPath))
            {
                type = System.IO.Path.GetExtension(file);
                switch (type)
                {
                    case ".c":
                        if(string.Compare(file,m_cPath) != 0)
                        {
                            File.Delete(file);
                            targetpathfile = System.IO.Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(m_cPath));
                            File.Copy(m_cPath, targetpathfile);
                        }
                        break;
                    case ".csv":
                        if (string.Compare(file, m_csvPath) != 0)
                        {
                            File.Delete(file);
                            targetpathfile = System.IO.Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(m_csvPath));
                            File.Copy(m_csvPath, targetpathfile);
                        }
                        break;
                }
            }
        }

        public override void CloseFile()
        {

        }

        private void LoadCSV_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = string.Empty;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.Title = "Load CSV File";
            openFileDialog.Filter = "Device Configuration file (*.csv)|*.csv||";
            openFileDialog.DefaultExt = "csv";
            openFileDialog.FileName = "default";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (openFileDialog.ShowDialog() == false) return;

            m_csvPath = openFileDialog.FileName;
            string text = File.ReadAllText(m_csvPath, Encoding.Default);
            range = new TextRange(csvrichTB.Document.ContentStart, csvrichTB.Document.ContentEnd);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                range.Load(ms, DataFormats.Text);
            }
        }

        private void LoadCFile_Click(object sender, RoutedEventArgs e)
        {
            string fullpath = string.Empty;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.Title = "Load FGLite File";
            openFileDialog.Filter = "Device Configuration file (*.c)|*.c||";
            openFileDialog.DefaultExt = "c";
            openFileDialog.FileName = "default";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (openFileDialog.ShowDialog() == false) return;

            m_cPath = openFileDialog.FileName;
            range = new TextRange(crichTB.Document.ContentStart, crichTB.Document.ContentEnd);
            _stream = new System.IO.FileStream(openFileDialog.FileName, System.IO.FileMode.OpenOrCreate);
            range.Load(_stream, System.Windows.DataFormats.Text);
            _stream.Close();
        }

        public void WriteDevice()
        {
            TextRange textRange = new TextRange(crichTB.Document.ContentStart, crichTB.Document.ContentEnd);
            (parent as MainControl).subTask_Dic.Add(m_cPath, textRange.Text); 
            textRange = new TextRange(csvrichTB.Document.ContentStart, csvrichTB.Document.ContentEnd);
            (parent as MainControl).subTask_Dic.Add(m_csvPath, textRange.Text);
        }

        public void UpdateTable(string subJson)
        {
            MemoryStream stream = null;
            StreamWriter writer = null;
            Dictionary<string,string> json = SharedAPI.DeserializeStringToDictionary<string, string>(subJson);
            foreach(string key in json.Keys)
            {
                switch (Path.GetExtension(key))
                {
                    case ".c":
                        m_cPath = key;
                        range = new TextRange(crichTB.Document.ContentStart, crichTB.Document.ContentEnd);
                        stream = new MemoryStream();
                        writer = new StreamWriter(stream);
                        writer.Write(json[key]);
                        writer.Flush();
                        stream.Position = 0;

                        range.Load(stream, System.Windows.DataFormats.Text);
                        _stream.Close();
                        break;
                    case ".csv":
                        m_csvPath = key;
                        range = new TextRange(csvrichTB.Document.ContentStart, csvrichTB.Document.ContentEnd);
                        stream = new MemoryStream();
                        writer = new StreamWriter(stream);
                        writer.Write(json[key]);
                        writer.Flush();
                        stream.Position = 0;

                        range.Load(stream, System.Windows.DataFormats.Text);
                        _stream.Close();
                        break;
                }
            }
        }
    }
}
