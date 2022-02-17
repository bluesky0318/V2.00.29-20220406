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
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;

namespace Cobra.UFPSBSPanel
{
    /// <summary>
    /// Interaction logic for RegControl.xaml
    /// </summary>
    public partial class RegControl : UserControl
    {
        public Parameter model30 = new Parameter();
        public Parameter model32 = new Parameter();
        public MainControl parent { get; set; }
        public RegControl()
        {
            InitializeComponent();
        }

        public void Init(object pParent)
        {
            parent = (MainControl)pParent;
            model30.guid = (UInt32)0x03300100;
            model30.regref = model30.phyref = 1;
            Reg reg = new Reg();
            reg.address = 0x01;
            reg.startbit = 0;
            reg.bitsnumber = 32;
            model30.reglist.Add("Low", reg);

            model32.guid = (UInt32)0x03320100;
            model32.regref = model32.phyref = 1;
            reg = new Reg();
            reg.address = 0x01;
            reg.startbit = 0;
            reg.bitsnumber = 32;
            model32.reglist.Add("Low", reg);
            parent.viewmode.wr_dm_parameterlist.parameterlist.Clear();
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            UInt16 uCur = 0;
            UInt16 uVol = 0;
            Button btn = sender as Button;
            parent.viewmode.wr_dm_parameterlist.parameterlist.Clear();

            string st = btn.Tag as string;
            switch (st)
            {
                case "0x30":
                    {
                        uCur = Convert.ToUInt16(Cur30.Text, 10);
                        uVol = Convert.ToUInt16(Vol30.Text, 10);
                        model30.phydata = (double)SharedFormula.MAKEDWORD(uCur, uVol);
                        parent.viewmode.wr_dm_parameterlist.parameterlist.Add(model30);
                    }
                    break;
                case "0x32":
                    {
                        uCur = Convert.ToUInt16(Cur32.Text, 10);
                        uVol = Convert.ToUInt16(Vol32.Text, 10);
                        model32.phydata = (double)SharedFormula.MAKEDWORD(uCur, uVol);
                        parent.viewmode.wr_dm_parameterlist.parameterlist.Add(model32);
                    }
                    break;
            }
            if (parent.runBtn.IsChecked == true) return;
            parent.ConvertPhysicalToHex(parent.viewmode.wr_dm_parameterlist);
            parent.Write(parent.viewmode.wr_dm_parameterlist);
#if false 
            Parameter Test = new Parameter();
            Test.guid = (UInt32)0x0302010c;
            Test.regref = Test.phyref = 1;
            Test.subtype = 1;
            Reg reg = new Reg();
            reg.address = 0x01;
            reg.startbit = 0x0c;
            reg.bitsnumber = 2;
            Test.reglist.Add("Low", reg);
            parent.viewmode.wr_dm_parameterlist.parameterlist.Clear();
            parent.viewmode.wr_dm_parameterlist.parameterlist.Add(Test);
            parent.Read(parent.viewmode.wr_dm_parameterlist);
            parent.ConvertHexToPhysical(parent.viewmode.wr_dm_parameterlist);
#endif
            
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            string st = cb.Tag as string;
            UInt16 index = (UInt16)(cb.SelectedIndex + 1);
            switch (st)
            {
                case "0x30":
                    model30.guid = (UInt32)(0x03300000 | index << 8);
                    if (model30.reglist.ContainsKey("Low"))
                        model30.reglist["Low"].address = index;
                    break;
                case "0x32":
                    model32.guid = (UInt32)(0x03320000 | index << 8);
                    if (model32.reglist.ContainsKey("Low"))
                        model32.reglist["Low"].address = index;
                    break;
            }
        }
    }
}
