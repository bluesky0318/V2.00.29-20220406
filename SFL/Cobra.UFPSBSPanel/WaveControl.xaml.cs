using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Cobra.Common;

namespace Cobra.UFPSBSPanel
{

    /// <summary>
    /// Interaction logic for WaveControl.xaml
    /// </summary>
    public partial class WaveControl : UserControl
    {
        private DateTime dt_start;
        private DatePointCollection dataPointCollection = new DatePointCollection(); 

        public WaveControl()
        {
            InitializeComponent();
        }

        public WaveControl(string name)
        {
            InitializeComponent();

            vAxisTitle.Content = name;
            var ds = new EnumerableDataSource<DatePoint>(dataPointCollection);
            ds.SetXMapping(x => x.time_point);
            ds.SetYMapping(y => y.ddata);
            //this.dateAxis.ShowMayorLabels = false;
            plot.AddLineGraph(ds, 2, name); // to use this method you need "using Microsoft.Research.DynamicDataDisplay;"
        }

        public void Reset()
        {
            dt_start = DateTime.Now;
            dataPointCollection.Clear();
        }

        public void Update(double data)
        {
            //Debug.WriteLine(string.Format("{0}", DateTime.Now.Subtract(dt_start).TotalSeconds));
            DatePoint dataPoint = new DatePoint(data, DateTime.Now.Subtract(dt_start).TotalSeconds);
            dataPointCollection.Add(dataPoint);          
        }
    }
}
