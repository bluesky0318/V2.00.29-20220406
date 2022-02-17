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
using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Cobra.Common;

namespace Cobra.FSBS2Panel
{
    /// <summary>
    /// Interaction logic for WaveControl.xaml
    /// </summary>
    public partial class WaveControl : Window
    {
        private ulong ts = 0;
        public DataPointCollection dataPointCollection = new DataPointCollection(); 

        public WaveControl()
        {
            InitializeComponent();
        }

        public WaveControl(string name)
        {
            InitializeComponent();

            ts = 0;
            this.Title = name;
            vAxisTitle.Content = name;
            var ds = new EnumerableDataSource<DataPoint>(dataPointCollection);
            ds.SetXMapping(x => x.time_point);
            ds.SetYMapping(y => y.ddata);
            plot.AddLineGraph(ds, 2, name); // to use this method you need "using Microsoft.Research.DynamicDataDisplay;"
        }

        public void Reset()
        {
            ts = 0;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate()
            {
                dataPointCollection.Clear();
            })); 
        }

        public void Update(double data)
        {
            DataPoint dataPoint = new DataPoint(data, ts++);
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate()
                {
                    dataPointCollection.Add(dataPoint);
                }));      
            }
            catch (System.Exception ex)
            {
            	
            }      
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;  // cancels the window close    
            this.Hide();      // Programmatically hides the window
        }
    }
}
