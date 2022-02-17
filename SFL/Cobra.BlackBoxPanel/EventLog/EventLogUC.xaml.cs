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

namespace Cobra.BlackBoxPanel.EventLog
{
    /// <summary>
    /// EventLogUC.xaml 的交互逻辑
    /// </summary>
    public partial class EventLogUC : UserControl
    {
        private ViewModel m_viewmodel;
        public EventLogUC()
        {
            InitializeComponent();
        }

        public void init(object pParent, string name)
        {
            m_viewmodel = new ViewModel(pParent, name); 
            if (m_viewmodel.RegisterList.Count == 0)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }
            dtgRegistersPresent.ItemsSource = m_viewmodel.lstclRegisterList;
        }


        #region Event Handler for all UI controls
        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            string tips = string.Empty;
            Label lbltmp = sender as Label;
            Model model = lbltmp.DataContext as Model;
            bool bShow = true;

            bShow = FindParameterTips(model, lbltmp.Content.ToString(), ref tips);
            if (bShow)
            {
                popText.Text = " " + tips + " ";
                popTip.PlacementTarget = lbltmp;
                popTip.HorizontalOffset = lbltmp.ActualWidth * 3 / 4;
                popTip.IsOpen = bShow;
            }
            else
            {
                popText.Text = " " + lbltmp.Content.ToString() + " ";
                popTip.PlacementTarget = lbltmp;
                popTip.HorizontalOffset = lbltmp.ActualWidth * 3 / 4;
                popTip.IsOpen = true;
                if ((popText.ActualWidth - 4) <= lbltmp.ActualWidth) popTip.IsOpen = false;
            }
        }

        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            popTip.IsOpen = false;
        }

        private void grdRegHigh_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txtbx = sender as TextBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key == Key.Subtract) || (e.Key == Key.Delete) ||
                (e.Key == Key.A) || (e.Key == Key.B) || (e.Key == Key.C) ||
                (e.Key == Key.D) || (e.Key == Key.E) || (e.Key == Key.F) ||
                ((e.Key >= Key.D0) && (e.Key <= Key.D9)))
            {
                //if ((txtbx.Text.Length <2) || (txtbx.Text.Length == 0))
                //{
                //e.Handled = true;
                //return;
                //}
                //else
                {
                    e.Handled = false;
                    return;
                }
                //e.Handled = false;
            }
            else if (e.Key == Key.Enter)
            {
                txtbx.RaiseEvent(new RoutedEventArgs(TextBox.LostFocusEvent));
                e.Handled = false;
                return;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txtbx = sender as TextBox;
            string strtmp = txtbx.Text.ToLower();
            Model expmtmp = txtbx.DataContext as Model;

            if (strtmp.IndexOf("0x") == -1)
            {
                txtbx.Text = "0x" + strtmp;
            }
            txtbx.CaretIndex = txtbx.Text.Length;
        }

        private bool FindParameterTips(Model model, string des, ref string tips)
        {
            bool bval = false;
            foreach (BitComponent ebctmp in model.ArrRegComponet)
            {
                if (ebctmp.strBitDescrip.Equals(des))
                {
                    tips = ebctmp.strBitTips;
                    if (string.IsNullOrEmpty(tips))
                        bval = false;
                    else
                        bval = true;
                }
            }
            return bval;
        }
        #endregion
    }
}
