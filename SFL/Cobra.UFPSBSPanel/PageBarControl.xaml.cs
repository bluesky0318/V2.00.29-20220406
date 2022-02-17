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

namespace Cobra.UFPSBSPanel
{
    /// <summary>
    /// Interaction logic for PageBarControl.xaml
    /// </summary>
    public partial class PageBarControl : UserControl
    {
        public delegate void PageSwitchEventHandler(int param);
        public event PageSwitchEventHandler OnPageSwitch;

        public PageBarControl()
        {
            InitializeComponent();
        }

        private void ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Ellipse ellipse = (Ellipse)sender;
            switch (ellipse.Name)
            {
                case "ellipse1":
                    ellipse1.Fill = new SolidColorBrush(Colors.Red);
                    ellipse2.Fill = new SolidColorBrush(Colors.Green);
                    OnPageSwitch(0);
                    break;
                case "ellipse2":
                    ellipse1.Fill = new SolidColorBrush(Colors.Gray);
                    ellipse2.Fill = new SolidColorBrush(Colors.Green);
                    OnPageSwitch(1);
                    break;
            }
        }
    }
}
