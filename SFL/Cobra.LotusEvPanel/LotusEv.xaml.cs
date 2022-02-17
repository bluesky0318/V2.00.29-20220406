using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using Cobra.EM;
using Cobra.Common;

namespace Cobra.LotusEvPanel
{
	/// <summary>
	/// Interaction logic for LotusEv.xaml
	/// </summary>
	public partial class LotusEvControl
	{
		#region private members

		private TASKMessage m_tskmsgLotus = new TASKMessage();	//Device working thread
		private GeneralMessage m_gnlmsgLotus = new GeneralMessage("LotusEv SFL", "", 2);	//General Message object for Warning Control 
		private ControlMessage m_ctlmsgLotus = new ControlMessage();	//Control Message object for Waiting Control
		private Device devParent { get; set; }	//save Device handler reference
		private LotusEvViewMode myViewMode { get; set; }	//data and action model
		//private Int16 uTglNumber = 0;
		private System.Windows.Threading.DispatcherTimer tmrStatusMonitor = new System.Windows.Threading.DispatcherTimer();
		private System.Windows.Threading.DispatcherTimer tmrRandomEn = new System.Windows.Threading.DispatcherTimer();
        private BackgroundWorker bgwkCombination = new BackgroundWorker();
        private int iPrevRandom = -1;     //to save which one had been random to send command
		private int iDelayToRead = 10;		//delay how millisecond to set Telemetry Selection and Read it
        private int iBgwkIndentify = 0;
        private int iReliaSelected = -1;
        private UInt16 iReliaTimes = 1000;

		#endregion

		#region public members

		public string sflname { get; set; }	//save sfl name string
		public bool bFranTestMode { get; set; }	//Evaluation controlled by francis
		public DCLDOModel dcldoSelected { get; set; }
		//use to bind with Random TabItem
		public static readonly DependencyProperty ButtonExperProperty = DependencyProperty.Register(
			"bTabItemShow", typeof(Visibility), typeof(LotusEvControl));
		public Visibility bTabItemShow
		{
			get { return (Visibility)GetValue(ButtonExperProperty); }
			set { SetValue(ButtonExperProperty, value); }
		}
		//public bool bLowLevelLog = false;
        public int iErrorCount = 0;

		#endregion

		#region Constructor/Destructor

		// <summary>
		// Constructor, 
		// </summary>
		public LotusEvControl(object pParent, string name)
		{
			InitializeComponent();
			#region Initialization of private/public members

			devParent = (Device)pParent;
			if (devParent == null) return;

			sflname = name;
			if (String.IsNullOrEmpty(sflname)) return;

			m_tskmsgLotus.PropertyChanged += new PropertyChangedEventHandler(tskmsg_PropertyChanged);
			m_tskmsgLotus.gm.sflname = name;
			m_tskmsgLotus.gm.level = 2;

			//LotusWaitControl.SetParent(grdLotusParent);
			//LotusWarnMsg.SetParent(grdLotusParent);

			#endregion

			bFranTestMode = false;
			myViewMode = new LotusEvViewMode(pParent, this);	//creat data
			lstboxDC.ItemsSource = myViewMode.DCLDORegList;
			lstboxStatus.ItemsSource = myViewMode.StatusList;
            dtgCommandsI2C.ItemsSource = myViewMode.I2CList;
			dtgVIDSelect.ItemsSource = myViewMode.VIDList;
			lstboxRandom.ItemsSource = myViewMode.DCLDORegRandomList;		//(A150714)Francis, 
			//grpVIDSelect.Visibility = Visibility.Collapsed;
			//dtgVIDSelect.Visibility = Visibility.Collapsed;
			//grdLotusParent.IsVisibleChanged += (o, e) => Dispatcher.BeginInvoke(new DependencyPropertyChangedEventHandler(grdLotusParent_IsVisibleChanged), o, e);
            
            //(A150915)Francis, for Combination and Reliability
            lstboxCombineRegister.ItemsSource = myViewMode.CmbinaRegList;
            lstboxCombine.ItemsSource = myViewMode.DCCombinationList;
            //lstboxReliability.ItemsSource = myViewMode.DCCombinationList;
            lstboxCombineMessage.ItemsSource = myViewMode.CombineMessageList;
            bgwkCombination.WorkerReportsProgress = false;
            bgwkCombination.WorkerSupportsCancellation = true;
            bgwkCombination.RunWorkerCompleted += bgwkCombination_RunWorkerCompleted;
            bgwkCombination.DoWork += bgwkCombination_DoWork;
            lstboxCombine.Focus();
            lstboxCombine.SelectedIndex = 0;
            //(E150915)

			tmrStatusMonitor.Tick += new EventHandler(tmrStatusMonitor_Elapsed);
            tmrRandomEn.Tick += new EventHandler(tmrRandom_Elapsed);
			Thread.Sleep(30);
			grpVIDSelect.Visibility = Visibility.Collapsed;
			if (!myViewMode.bEnableRandomTab)
				bTabItemShow = Visibility.Collapsed;
			else
				bTabItemShow = Visibility.Visible;
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.SFL);
		}

		// <summary>
		// Destructor, 
		// </summary>
		~LotusEvControl()
		{
			//MessageBox.Show("SFL expired");
			tmrStatusMonitor.Stop();
			tmrRandomEn.Stop();
		}

		#endregion

		#region DC/LDO methods

		//no used
		private void btnDCRead_Click(object sender, RoutedEventArgs e)
		{
			Button btntmp = sender as Button;
			//UInt16 u16tmp = (UInt16)btntmp.Tag;
			DCLDOModel dcldotmp = btntmp.DataContext as DCLDOModel;

			if (dcldotmp == null)
			{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EXPSFL_DATABINDING));
			}
			else
			{
				//have Binding Data, try to read from Device
				//if (!myViewMode.ReadRegFromDevice(ref m_tskmsgLotus, dcldotmp))
				//{
					//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(m_tskmsgLotus.errorcode));
				//}
				//else
				//{
					//bBtnLotus = true;
				//}
			}
		}

		private void btnDCEnable_Click(object sender, RoutedEventArgs e)
		{
			ToggleButton tgen = sender as ToggleButton;
			DCLDOModel dcldomd = null;

			if (tgen != null)
				dcldomd = tgen.DataContext as DCLDOModel;
			else
			{
				return;
			}

			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}

			WaitControlLotusSetup(true, 50, "Send Enable or Disable command to DC/LDO channel");
			if (!myViewMode.WriteEnableToDevice(ref m_tskmsgLotus, dcldomd))
			{
				//MessageBox.Show("Write Error", "Chip Access error", MessageBoxButton.OK);
				WaitControlLotusSetup(true, -1, "Command send failed.");
			}
			else
			{
				//WaitControlLotusSetup(true, 100, "Command successfully done.");
				LotusWaitControlClear();
			}
			//devParent.bBusy = false;
		}

		private void btnDCMarginEnable_Click(object sender, RoutedEventArgs e)
		{
			ToggleButton tgen = sender as ToggleButton;
			DCLDOModel dcldomd = null;

			if (tgen != null)
				dcldomd = tgen.DataContext as DCLDOModel;
			else
			{
				return;
			}

			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}

			WaitControlLotusSetup(true, 50, "Send Margin command to  channel");
			if (!myViewMode.WriteMarginToDevice(ref m_tskmsgLotus, dcldomd))
			{
				//MessageBox.Show("Write Error", "Chip Access error", MessageBoxButton.OK);
				WaitControlLotusSetup(true, -1, "Command send failed.");
			}
			else
			{
				//WaitControlLotusSetup(true, 100, "Command successfully done.");
				LotusWaitControlClear();
			}
			//devParent.bBusy = false;
		}

		private void btnDCMarginValue_Click(object sender, RoutedEventArgs e)
		{
			ToggleButton tgen = sender as ToggleButton;
			DCLDOModel dcldomd  = null;

			if (tgen != null)
				dcldomd = tgen.DataContext as DCLDOModel;
			else
			{
				return;
			}

			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}

			WaitControlLotusSetup(true, 50, "Send Margin value to DC/LDO channel");
			if (!myViewMode.WriteMarginToDevice(ref m_tskmsgLotus, dcldomd))
			{
				//MessageBox.Show("Write Error", "Chip Access error", MessageBoxButton.OK);
				WaitControlLotusSetup(true, -1, "Command send failed.");
			}
			else
			{
				//WaitControlLotusSetup(true, 100, "Command successfully done.");
				LotusWaitControlClear();
			}
			//devParent.bBusy = false;
		}

		private void lblDCVolt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			Label lbltmp = sender as Label;
			DCLDOModel dcld = (DCLDOModel)lbltmp.DataContext;
			int iRow = 0, iCol = 0;

			dcldoSelected = dcld;
			iRow = (int)(dcldoSelected.yMarginHex >> 4);
			iCol = (int)(dcldoSelected.yMarginHex & 0x0F);
			grpVIDSelect.Visibility = Visibility.Visible;
			grpVIDSelect.Focus();
			dtgVIDSelect.Focus();
			MouseEventArgs mear = new MouseEventArgs(Mouse.PrimaryDevice, 1);
			mear.RoutedEvent = Mouse.MouseEnterEvent;
			dtgVIDSelect.RaiseEvent(mear);
			//dtgVIDSelect.RaiseEvent(new RoutedEventArgs(DataGrid.MouseDownEvent));
			//Keyboard.Focus(dtgVIDSelect);
			//dtgVIDSelect.Visibility = Visibility.Visible;
			dtgVIDSelect.SelectedCells.Clear();
			object item = dtgVIDSelect.Items[iRow];
			DataGridRow row = dtgVIDSelect.ItemContainerGenerator.ContainerFromIndex(iRow) as DataGridRow;
			if (row == null)
			{
				dtgVIDSelect.ScrollIntoView(item);
				row = dtgVIDSelect.ItemContainerGenerator.ContainerFromIndex(iRow) as DataGridRow;
			}
			if (row != null)
			{
				DataGridCell cell = LotusEvControl.GetCell(dtgVIDSelect, row, iCol);
				dtgVIDSelect.CurrentCell = new DataGridCellInfo(dtgVIDSelect.Items[iRow], dtgVIDSelect.Columns[iCol]);
				dtgVIDSelect.SelectedCells.Add(dtgVIDSelect.CurrentCell);
				cell.Focus();
			}
		}

		private void dtgVIDSelect_MouseUp(object sender, MouseButtonEventArgs e)
		{
			//DataRowView dataRow = (DataRowView)dtgVIDSelect.SelectedItem;
			//int index = dtgVIDSelect.CurrentCell.Column.DisplayIndex;
			//string cellValue = dataRow.Row.ItemArray[index].ToString(); 
			//string cellValue;
			int iRow = 0, iColl = 0;
			DataGridCellInfo curcell;
			VIDVoltage vittmp;

			//IList<DataGridCellInfo> cells = dtgVIDSelect.SelectedCells;
			//cellValue = string.Format("{0}", cells.Count);
			//cellValue = cells[0].Item.ToString();
			curcell = dtgVIDSelect.CurrentCell;
			vittmp = (VIDVoltage)curcell.Item;
			iRow = vittmp.iRowNum;
			iColl = curcell.Column.DisplayIndex;
			//cellValue = string.Format("{0}, {1}", iRow, iColl);
			//MessageBox.Show(cellValue);
			dcldoSelected.yMarginHex = (byte)(iRow * 16 + iColl);

			grpVIDSelect.Visibility = Visibility.Collapsed;
		}

		private void btnLDOMarginSet_Click(object sender, RoutedEventArgs e)
		{
			Button tgen = sender as Button;
			DCLDOModel dcldomd = null;

			if (tgen != null)
				dcldomd = tgen.DataContext as DCLDOModel;
			else
			{
				return;
			}

			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}

			WaitControlLotusSetup(true, 50, "Send Margin value to DC/LDO channel");
			if (!myViewMode.WriteHexMarginToDevice(ref m_tskmsgLotus, dcldomd))
			{
				//MessageBox.Show("Write Error", "Chip Access error", MessageBoxButton.OK);
				WaitControlLotusSetup(true, -1, "Command send failed.");
			}
			else
			{
				//WaitControlLotusSetup(true, 100, "Command successfully done.");
				LotusWaitControlClear();
			}
			//devParent.bBusy = false;
		}

		private void btnReadDCLDO_Click(object sender, RoutedEventArgs e)
		{
			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}

			WaitControlLotusSetup(true, 50, "Read DC/LDO channel data");
			if (myViewMode.ReadRegFromDevice(ref m_tskmsgLotus))
			{
				//WaitControlLotusSetup(true, 100, "Read successfully");
				if (myViewMode.ConvertChannelToBit())
				{
					LotusWaitControlClear();
					//devParent.bBusy = false;
					return;
				}
			}
			//MessageBox.Show("Read Error", "Chip Access error", MessageBoxButton.OK);
			//devParent.bBusy = false;
			WaitControlLotusSetup(true, -1, "Read failed.");
		}

		private void dtgVIDSelect_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				grpVIDSelect.Visibility = Visibility.Collapsed;
		}

		private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(obj, i);
				if (child != null && child is T)
					return (T)child;
				else
				{
					T childOfChild = FindVisualChild<T>(child);
					if (childOfChild != null)
						return childOfChild;
				}
			}
			return null;
		}

		private static DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, int column)
		{
			if (rowContainer != null)
			{
				DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
				if (presenter == null)
				{
					/* if the row has been virtualized away, call its ApplyTemplate() method 
					 * to build its visual tree in order for the DataGridCellsPresenter
					 * and the DataGridCells to be created */
					rowContainer.ApplyTemplate();
					presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
				}
				if (presenter != null)
				{
					DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
					if (cell == null)
					{
						/* bring the column into view
						 * in case it has been virtualized away */
						dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
						cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
					}
					return cell;
				}
			}
			return null;
		}

		#endregion

		#region Status group

		private void tgbtnMonitor_Click(object sender, RoutedEventArgs e)
		{
			ToggleButton tbnMonitor = sender as ToggleButton;

			if ((bool)tbnMonitor.IsChecked)
			{
				//if (devParent.bBusy)
				//{
					//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
					//return;
				//}
				//else
				//{
					//devParent.bBusy = true;
				//}

				tbnMonitor.Content = "Stop".ToString();
				//WaitControlLotusSetup(true, 50, "Read Status register...");
				if (myViewMode.ReadStatusFromDevice(ref m_tskmsgLotus))
				{
					//WaitControlLotusSetup(true, 100, "Read successfully.");
					if (myViewMode.ConvertStatusToBit())
					{
						tmrStatusMonitor.Interval = TimeSpan.FromMilliseconds(1000);
						tmrStatusMonitor.Start();
					}
					else
					{
						tmrStatusMonitor.Stop();
						tgbtnMonitor.IsChecked = false;
						tgbtnMonitor.Content = "Monitor".ToString();
						//MessageBox.Show("Read Error", "Chip access error", MessageBoxButton.OK);
					}
				}
				else
				{
					tmrStatusMonitor.Stop();
					tgbtnMonitor.IsChecked = false;
					tgbtnMonitor.Content = "Monitor".ToString();
					WaitControlLotusSetup(true, -1, "Read failed.");
				}
				//devParent.bBusy = false;
			}
			else
			{
				tmrStatusMonitor.Stop();
				tbnMonitor.Content = "Monitor";
			}
		}

		private void tmrStatusMonitor_Elapsed(object sender, EventArgs e)
		{
			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}

			//btnScanCalibrate.IsEnabled = false;
			WaitControlLotusSetup(true, 50, "Read Status register...");
			if (myViewMode.ReadStatusFromDevice(ref m_tskmsgLotus))
			{
				//WaitControlLotusSetup(true, 100, "Read successfully.");
				LotusWaitControlClear();
				if (!myViewMode.ConvertStatusToBit())
				{
					tmrStatusMonitor.Stop();
					tgbtnMonitor.IsChecked = false;
					tgbtnMonitor.Content = "Monitor".ToString();
				}
			}
			else
			{
				tmrStatusMonitor.Stop();
				tgbtnMonitor.IsChecked = false;
				tgbtnMonitor.Content = "Monitor".ToString();
				WaitControlLotusSetup(true, -1, "Read failed.");
			}
			//devParent.bBusy = false;
		}

		#endregion

		#region I2C commands group

		private void btnRunI2C_Click(object sender, RoutedEventArgs e)
        {
			//if (devParent.bBusy)
			//{
				//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
				//return;
			//}
			//else
			//{
				//devParent.bBusy = true;
			//}
		
			GatherDataCommandsI2C();
            myViewMode.ConstructI2CCollection();
			WaitControlLotusSetup(true, 50, "Send I2C command to device");
			if (!myViewMode.DeliverI2CSetNDelay())
			{
				//MessageBox.Show("Error", "I2C command access error", MessageBoxButton.OK);
				WaitControlLotusSetup(true, -1, "Send command failed.");
			}
			else
			{
				WaitControlLotusSetup(true, 100, "Command successfully done.");
			}
			//devParent.bBusy = false;
        }

        private void btnRepeatI2C_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "I2C config File";
            openFileDialog.Filter = "(*.xml)|*.xml||";
            openFileDialog.FileName = "default";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "xml";
            openFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (openFileDialog.ShowDialog() == true)
            {
                {
                    if(!myViewMode.OpenI2CCmdCfg(openFileDialog.FileName))
                    {
                        MessageBox.Show("Open I2C config file failed.");
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();

            GatherDataCommandsI2C();
            saveFileDialog.Title = "I2C config File";
            saveFileDialog.Filter = "(*.xml)|*.xml||";
            saveFileDialog.FileName = "NewI2CConfig";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "xml";
            saveFileDialog.InitialDirectory = FolderMap.m_currentproj_folder;
            if (saveFileDialog.ShowDialog() == true)
            {
                {
                    if(!myViewMode.SaveI2CCmdCfg(saveFileDialog.FileName))
                    {
                        MessageBox.Show("Save I2C config file failed.");
                    }
                }
            }

        }

		private void TextBlock_KeyDown(object sender, KeyEventArgs e)
		{
			TextBox txtbx = sender as TextBox;

			if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
				(e.Key == Key.Subtract) || (e.Key == Key.Delete) ||
				(e.Key == Key.A) || (e.Key == Key.B) || (e.Key == Key.C) ||
				(e.Key == Key.D) || (e.Key == Key.E) || (e.Key == Key.F) ||
				((e.Key >= Key.D0) && (e.Key <= Key.D9)))
			{
				{
					e.Handled = false;
					return;
				}
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

		private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			TextBlock txtbx = sender as TextBlock;
			CommandsI2C cmdtmp = txtbx.DataContext as CommandsI2C;
			bool btmp;
			UInt16 itmp;

			if (cmdtmp != null)
			{
				itmp = Convert.ToUInt16(txtbx.Tag);
				if (itmp == 0)
				{
					btmp = cmdtmp.bCmdRW;
					cmdtmp.bCmdRW = !btmp;
				}
				else if (itmp == 4)
				{
					btmp = cmdtmp.bRepeat;
					cmdtmp.bRepeat = !btmp;
				}
			}
		}

        private void TextBox_KeyDown_NumbOnly(object sender, KeyEventArgs e)
        {
            TextBox txtbx = sender as TextBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key == Key.Subtract) || (e.Key == Key.Delete) ||
                ((e.Key >= Key.D0) && (e.Key <= Key.D9)))
            {
                {
                    e.Handled = false;
                    return;
                }
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

		private void GatherDataCommandsI2C()
		{
			int iRowcount = dtgCommandsI2C.Items.Count - 1; //last one is empty
			DataGridRow dgrow;
			TextBox txtIndexBox;
			TextBox txtValueBox;
			TextBox txtBlank;
			CommandsI2C cmdtmp;
			//byte ytmp;
			for (int i = 0; i < iRowcount; i++)
			{
				dgrow = dtgCommandsI2C.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
				if (dgrow != null)
				{
					cmdtmp = (CommandsI2C)dgrow.DataContext;
					txtIndexBox = FindChild<TextBox>(dgrow, "txbIndex");
					txtValueBox = FindChild<TextBox>(dgrow, "txbValue");
					txtBlank = FindChild<TextBox>(dgrow, "txbBlank");
					//Byte.TryParse(txtIndexBox.Text, NumberStyles.HexNumber, null as IFormatProvider, out ytmp);
					//cmdtmp.yRegIndex = ytmp;
					//Byte.TryParse(txtValueBox.Text, NumberStyles.HexNumber, null as IFormatProvider, out ytmp);
					//cmdtmp.yRegValue = ytmp;
					cmdtmp.yRegIndex = Convert.ToByte(txtIndexBox.Text, 16);
					cmdtmp.yRegValue = Convert.ToByte(txtValueBox.Text, 16);
					cmdtmp.uBlanktime = Convert.ToUInt16(txtBlank.Text);
				}
			}
		}

		private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
		{
			if (parent == null)
			{
				return null;
			}

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				T childType = child as T;

				if (childType == null)
				{
					foundChild = FindChild<T>(child, childName);

					if (foundChild != null) break;
				}
				else
					if (!string.IsNullOrEmpty(childName))
					{
						var frameworkElement = child as FrameworkElement;

						if (frameworkElement != null && frameworkElement.Name == childName)
						{
							foundChild = (T)child;
							break;
						}
						else
						{
							foundChild = FindChild<T>(child, childName);

							if (foundChild != null)
							{
								break;
							}
						}
					}
					else
					{
						foundChild = (T)child;
						break;
					}
			}

			return foundChild;
		}

		#endregion

		//(A150714)Francis
		#region Random one Enable/Disable DC/LDO channel
		private void btnRandom_Click(object sender, RoutedEventArgs e)
		{
			UInt16 utmp = 1000;		//default use 1 second duration
			DCLDO3Model tmpModel = null;

            if ((bool)tbnRandom.IsChecked)
			{
				//if (devParent.bBusy)
				//{
					//LotusWarnMsgInvoke(LibErrorCode.GetErrorDescription(LibErrorCode.IDS_ERR_EM_THREAD_BKWORKER_BUSY));
					//return;
				//}
				//else
				//{
					//devParent.bBusy = true;
				//}
                tbnRandom.Content = "Stop".ToString();
				if (!UInt16.TryParse(txbDuraTime.Text, NumberStyles.Integer, null, out utmp))
					utmp = 1000;	//default 1 second
				myViewMode.LogList.Clear();
				myViewMode.CreateLogFile();
				if (RandomChannel())
				{
					if (utmp > 0)
					{
						if ((utmp - iDelayToRead * 3) < 50)
							tmrRandomEn.Interval = TimeSpan.FromMilliseconds(50);
						else
							tmrRandomEn.Interval = TimeSpan.FromMilliseconds(utmp - (iDelayToRead * 3));
						tmrRandomEn.Start();
					}
					else
					{
						tbnRandom.IsChecked = false;
						tbnRandom.Content = "Random";
						myViewMode.SaveAllLogListAndClose();
					}
				}
				else
				{
					tbnRandom.IsChecked = false;
					tbnRandom.Content = "Random";
					myViewMode.SaveAllLogListAndClose();
				}
				//devParent.bBusy = false;
			}
			else
			{
                tmrRandomEn.Stop();
				while (tmrRandomEn.IsEnabled)
					Thread.Sleep(10);
				Thread.Sleep(500);
				if (iPrevRandom != -1)
				{
					if (bFranTestMode)
						Debug.WriteLine("Clear last random channel");
					tmpModel = myViewMode.DCLDORegRandomList[iPrevRandom];
					SetOrClearDCLDO3Model(ref tmpModel, true);
					myViewMode.WriteTelemetrySelectionToDevice(ref m_tskmsgLotus, iPrevRandom, true);	//clear Telemetry Selection
					iPrevRandom = -1;
				}
				tbnRandom.Content = "Random";
				myViewMode.SaveAllLogListAndClose();
            }
		}

        private void tmrRandom_Elapsed(object sender, EventArgs e)
        {
			if (!RandomChannel())
			{
				tmrRandomEn.Stop();
				tbnRandom.IsChecked = false;
				tbnRandom.Content = "Random";
				myViewMode.SaveAllLogListAndClose();
			}
        }

        private bool RandomChannel()
        {
            bool bReturn = true;
            Random rnd = new Random();
            int iTotal = 0, iChl = 0, iEnableMar = 0, iMargin5 = 0;
            byte yMargin = 0;
            DCLDO3Model tmpModel = null;

            iTotal = myViewMode.DCLDORegRandomList.Count;
            iChl = rnd.Next(0, iTotal);

			//clear before enable channel
            if(iPrevRandom != -1)
            {
				if(bFranTestMode)
					Debug.WriteLine("Clear last random channel");
                tmpModel = myViewMode.DCLDORegRandomList[iPrevRandom];
                bReturn &= SetOrClearDCLDO3Model(ref tmpModel, true);
				bReturn &= myViewMode.WriteTelemetrySelectionToDevice(ref m_tskmsgLotus, iPrevRandom, true);	//clear Telemetry Selection
				if (!bReturn)
					return bReturn;
            }

            tmpModel = myViewMode.DCLDORegRandomList[iChl];
			tmpModel.bChannelEnable = true;
            if (tmpModel.yDCCatagory == DCLDO3Model.CatagoryDC1)    //DC1~4, Enable Margin and +/-5 Margin
            {
                iEnableMar = rnd.Next(0, 2);
                tmpModel.bMarginEnable = Convert.ToBoolean(iEnableMar);
                iMargin5 = rnd.Next(0, 2);
                tmpModel.bMarginValue = Convert.ToBoolean(iMargin5);
            }
            else            //DC5-6, LDO1-2, 256 VID voltage set
            {
                yMargin = (byte)rnd.Next(0, 256);
                tmpModel.yMarginHex = yMargin;
            }

			if (bFranTestMode)
			{
				string strdbg = string.Format("Channel={0:X2}, MarginEn={1:X1}, MarginVal={2:X1},  MarginPhysical={3}", iChl+1, iEnableMar,iMargin5, yMargin);
				Debug.WriteLine(strdbg);
			}
			//RandomLogModel rlm = new RandomLogModel(iChl+1, Convert.ToBoolean(iEnableMar), Convert.ToBoolean(iMargin5), yMargin);
			RandomLogModel rlm = new RandomLogModel(iChl + 1, Convert.ToBoolean(iEnableMar), Convert.ToBoolean(iMargin5), tmpModel.MarginPhysical);
			//if (myViewMode.LogList.Count > 30)
			//{
				//myViewMode.SaveAllLogListAndClose(false);
				//myViewMode.LogList.Clear();
			//}
			myViewMode.LogList.Add(rlm);
			//myViewMode.SaveAllLogListAndClose(false);
			//myViewMode.LogList.Clear();
			//myViewMode.SaveLogFile(rlm);
			bReturn &= SetOrClearDCLDO3Model(ref tmpModel);
			if (bReturn)
			{
				iPrevRandom = iChl;
				Thread.Sleep(iDelayToRead);
				bReturn &= myViewMode.WriteTelemetrySelectionToDevice(ref m_tskmsgLotus, iChl);
				Thread.Sleep(iDelayToRead);
				bReturn &= myViewMode.ReadTelemetryConvertionFromDevice(ref m_tskmsgLotus, iChl);
			}
			else
			{
				WaitControlLotusSetup(true, -1, "Communication Failed.");
			}

            return bReturn;
        }

        private bool SetOrClearDCLDO3Model(ref DCLDO3Model targetDCLDO3M, bool bClear = false)
        {
            bool bReturn = true;

            if (targetDCLDO3M.yDCCatagory == DCLDO3Model.CatagoryDC1)
            {
                if(bClear)
                {
                    targetDCLDO3M.bChannelEnable = false;
                    targetDCLDO3M.bMarginEnable = false;
                    targetDCLDO3M.bMarginValue = false;
					targetDCLDO3M.yADTelemetryVal = 0x00;
                }
                //send I2C commnad here
				//(M150721)Francis, as Dejun request, send Margin first before enable
				bReturn &= myViewMode.WriteRandomMarginToDevice(ref m_tskmsgLotus, targetDCLDO3M);
				if(bReturn)
					bReturn &= myViewMode.WriteRandomEnableToDevice(ref m_tskmsgLotus, targetDCLDO3M);
            }
            else
            {
                if(bClear)
                {
                    targetDCLDO3M.bChannelEnable = false;
                    targetDCLDO3M.yMarginHex = 0;
					targetDCLDO3M.yADTelemetryVal = 0x00;
                }
                //send I2C command here
				//(M150721)Francis, as Dejun request, send Vout value first before enable
				bReturn &= myViewMode.WriteRandomHexMarginToDevice(ref m_tskmsgLotus, targetDCLDO3M);
				if(bReturn)
					bReturn &= myViewMode.WriteRandomEnableToDevice(ref m_tskmsgLotus, targetDCLDO3M);
            }

            return bReturn;
        }

		private void txbDuraTime_KeyDown(object sender, KeyEventArgs e)
		{
			TextBox txtbx = sender as TextBox;

			if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
				(e.Key == Key.Decimal) || (e.Key == Key.Subtract) || (e.Key == Key.Delete) ||
				((e.Key >= Key.D0) && (e.Key <= Key.D9)))
			{
				//if (txtbx.Text.Length < 2)
				{
					e.Handled = false;
					return;
				}
				//e.Handled = false;
			}
			else if (e.Key == Key.Enter)
			{
				//btnGoto.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
				//e.Handled = false;
				e.Handled = true;
				return;
			}
			e.Handled = true;
		}

		private void btnViewFolder_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer.exe", LotusEvViewMode.strRandomLogFolder);
		}

		#endregion
		//(E150714)

		//(A150914)Francis, Combination test
		#region Test all DC combination, and thermal

        //This is recursive function, so the array should be constructed outside, not in function internal
        private void RecurCombination(ref bool[] refChannels, int iSubNumber, ref UInt32 uErrCode, int iIndexStart = 0)
        {
            //byte yTargetChls = 0x00;
            int i;

            if(refChannels.Length < iSubNumber)
            {
                uErrCode = LibErrorCode.IDS_ERR_SECTION_LOTUSSFL;
                return;
            }

            //string strdbg = string.Format("coming iSubNumber={0}, iIndexStart={1}", iSubNumber, iIndexStart);
            //Debug.WriteLine(strdbg);
            if (iSubNumber <= 0)
            {
                //convert channels to byte that will be wrote to EnableRegister
                //yTargetChls = 0x00;
                //for (i = 0; i < refChannels.Length; i++)
                //{
                    //if (refChannels[i] == true)
                    //{
                        //yTargetChls |= (byte)(0x01 << (7 - i));
                        //refChannels[j] = false;
                    //}
                //}
                //string strchl = string.Format("TargetChls = {0:X2}", yTargetChls);
                //Debug.WriteLine(strchl);
                myViewMode.EnableDCNWriteOut(ref m_tskmsgLotus, refChannels);     //after all channels are enable
                myViewMode.EnableOTPNClearReg(ref m_tskmsgLotus, refChannels);          //then write OTP threshold
                return;
            }

            //for(int i=0; i<iSubNumber; i++)
            for (i = iIndexStart; i <= (refChannels.Length-iSubNumber); i++)
            {
                refChannels[i] = true;
                //strdbg = string.Format("enable channel {0}", i);
                //Debug.WriteLine(strdbg);
                //for(j=(i); j< (iSubNumber+i); j++)
                //j = i;
                {
                    //strdbg = string.Format("iSubNumber={0}, iIndexStart={1}, i={2}", iSubNumber, iIndexStart, i);
                    //Debug.WriteLine(strdbg);
                    RecurCombination(ref refChannels, iSubNumber-1, ref uErrCode, i+1);
                    //refChannels[j] = true;
                }
                refChannels[i] = false;
                //strdbg = string.Format("disable channel {0}", i);
                //Debug.WriteLine(strdbg);
            }

        }

        private bool RunAllDCCombinationEnable()
        {
            bool bRet = true;
            UInt32 uErrCode = LibErrorCode.IDS_ERR_SUCCESSFUL;
            bool[] bEnChannels = new bool[myViewMode.DCCombinationList.Count - 2];  //skip OTP and ALERT item

            for (int i = 0; i < bEnChannels.Length; i++)
            {
                //bEnChannels[i] = (byte)(0x01 << (7 - i));
                //bEnChannels[i] = i;
                bEnChannels[i] = false;
            }
            RecurCombination(ref bEnChannels, 1, ref uErrCode);     //sequentially enable 1 channel
            RecurCombination(ref bEnChannels, 2, ref uErrCode);     //2 channels' combination
            RecurCombination(ref bEnChannels, 3, ref uErrCode);     //3 channels' combination
            RecurCombination(ref bEnChannels, 4, ref uErrCode);     //4 channels' combination
            RecurCombination(ref bEnChannels, 5, ref uErrCode);     //5 channels' combination
            RecurCombination(ref bEnChannels, 6, ref uErrCode);     //6 channels' combination
            bRet &= myViewMode.DisableEnRegister(ref m_tskmsgLotus);
            if(uErrCode != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                bRet = false;
            }

            return bRet;
        }

        private void tbnCombine_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)tbnCombine.IsChecked)
            {
                CombineMessageModel msgmdl = new CombineMessageModel("Start DC Combination test.");
                msgmdl.bRedAlert = false;
                myViewMode.CombineMessageList.Add(msgmdl);
                lstboxCombineMessage.ScrollIntoView(msgmdl);

                tbnCombine.Content = "Doing...".ToString();
                //tbnCombine.IsEnabled = false;
                grdCombineCommand.IsEnabled = false;
                iBgwkIndentify = 1;
				//bLowLevelLog = true;
                bgwkCombination.RunWorkerAsync();
                //myViewMode.LogList.Clear();
                //myViewMode.CreateLogFile();
                //step 1, Read all registers to check the default values
                //if (!myViewMode.ReadAllNMapToCmbRegister(ref m_tskmsgLotus))
                //{
                    //MessageBox.Show("Read all register failed.");
                    //tbnRandom.Content = "Start";
                    //return;
                //}
                //Thread.Sleep(1);
                //if (!RunAllDCCombinationEnable())
                //{
                    //MessageBox.Show("Enable All DC channels failed.");
                    //return;
                //}
                //tbnCombine.Content = "Start";
            }
            else
            {

            }
        }

        private void bthReliability_Click(object sender, RoutedEventArgs e)
        {
            if (lstboxCombine.SelectedIndex <= 5)
            {
                CombineMessageModel msgmdl = new CombineMessageModel("Start Reliability test.");
                msgmdl.bRedAlert = false;
                myViewMode.CombineMessageList.Add(msgmdl);
                lstboxCombineMessage.ScrollIntoView(msgmdl);

                iReliaSelected = lstboxCombine.SelectedIndex;
				if (!UInt16.TryParse(txblkReliaTimes.Text, NumberStyles.Integer, null, out iReliaTimes))
					iReliaTimes = 1000;	//default 1000 times
                bthReliability.Content = "Doing...".ToString();
                grdCombineCommand.IsEnabled = false;
                iBgwkIndentify = 2;
				//bLowLevelLog = true;
                bgwkCombination.RunWorkerAsync();
            }
        }

        //no used
        private void lstboxCombine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int iselect = lstboxCombine.SelectedIndex;
            //MessageBox.Show(string.Format("{0} is selected", iselect));
            //if (iselect >= 6)
                //lstboxCombine.SelectedIndex = 0;
        }

        private void txblkReliaTimes_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txtbx = sender as TextBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key == Key.Decimal) || (e.Key == Key.Subtract) || (e.Key == Key.Delete) ||
                ((e.Key >= Key.D0) && (e.Key <= Key.D9)))
            {
                //if (txtbx.Text.Length < 2)
                {
                    e.Handled = false;
                    return;
                }
                //e.Handled = false;
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                return;
            }
            e.Handled = true;
        }

        private void btnViewLog_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", LotusEvViewMode.strRandomLogFolder);
        }

        private void bgwkCombination_DoWork(object sender, DoWorkEventArgs e)
        {
            myViewMode.LogList.Clear();
            if (iBgwkIndentify == 1)
            {
                myViewMode.CreateLogFile("Combination", ".csv");
                //step 1, Read all registers to check the default values
                if (!myViewMode.ReadAllNMapToCmbRegister(ref m_tskmsgLotus))
                {
                    MessageBox.Show("Read all register failed.");
                    tbnRandom.Content = "Start";
                    return;
                }
                Thread.Sleep(1);
                if (!RunAllDCCombinationEnable())
                {
                    MessageBox.Show("Enable All DC channels failed.");
                    return;
                }
            }
            else if(iBgwkIndentify == 2)
            {
                //MessageBox.Show()
                myViewMode.CreateLogFile("Reliability", ".csv");
                if(!myViewMode.ReliableReadDC(ref m_tskmsgLotus, iReliaSelected, iReliaTimes))
                {
                    MessageBox.Show(string.Format("Reliabile read DC channel:{0} is failed", iReliaSelected + 1));
                    return;
                }
            }
        }

        private void bgwkCombination_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CombineMessageModel msgmdl = new CombineMessageModel();
            if (iBgwkIndentify == 1)
            {
                Thread.Sleep(1);
                tbnCombine.Content = "Start All Combination";
                tbnCombine.IsChecked = false;
                if (iErrorCount > 0)
                {
                    msgmdl.strMessage = string.Format("Finished Combination test, there are {0} errors happened.", iErrorCount);
                    msgmdl.bRedAlert = true;
                }
                else
                {
                    msgmdl.strMessage = string.Format("Finished Combination test.");
                    msgmdl.bRedAlert = false;
                }
            }
            else if(iBgwkIndentify == 2)
            {
                Thread.Sleep(1);
                bthReliability.Content = "Start Reliable Read";
                if (iErrorCount > 0)
                {
                    msgmdl.strMessage = string.Format("Finished Reliability test, there are {0} errors happened.", iErrorCount);
                    msgmdl.bRedAlert = true;
                }
                else
                {
                    msgmdl.strMessage = string.Format("Finished Reliability test.");
                    msgmdl.bRedAlert = false;
                }
            }
            //tbnCombine.IsEnabled = true;
			//bLowLevelLog = false;
            myViewMode.CombineMessageList.Add(msgmdl);
            iErrorCount = 0;
            //lstboxCombineMessage.SelectedIndex = lstboxCombineMessage.Items.Count - 1;
            lstboxCombineMessage.ScrollIntoView(msgmdl);
            myViewMode.SaveAllLogListAndClose(true, true);
            grdCombineCommand.IsEnabled = true;
        }

        #endregion
		//(E150914)

		#region Message/Waiting Control

		// <summary>
		// Common Controls, WarningControl and WaitingControl message handler
		// </summary>
		// <param name="sender"></param>
		// <param name="e"></param>
		void tskmsg_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			TASKMessage tskmsgSend = sender as TASKMessage;
			switch (e.PropertyName)
			{
				case "controlreq":
					switch (tskmsgSend.controlreq)
					{
						case COMMON_CONTROL.COMMON_CONTROL_WARNING:
							{
								//WarningControlPInvoke(gnlmsgProduct);
								break;
							}

						case COMMON_CONTROL.COMMON_CONTROL_WAITTING:
							{
								LotusWaitControlInvoke(m_ctlmsgLotus);
								break;
							}
					}
					break;
			}
		}

		// <summary>
		// Warning Control invoke function, display parsing in message in Warning Control, and setup its level
		// </summary>
		// <param name="strGnlMsgIn"></param>
		// <param name="iLevelIn"></param>
		private void LotusWarnMsgInvoke(string strGnlMsgIn, int iLevelIn = 2)
		{
			if ((m_tskmsgLotus.errorcode & LibErrorCode.IDS_ERR_SECTION_LOTUSSFL) != 0)
			{
				//bBtnLotus = false;
			}

			//m_gnlmsgLotus.level = 2;
			m_gnlmsgLotus.level = iLevelIn;
			m_gnlmsgLotus.message = strGnlMsgIn;
			LotusWarnMsg.Dispatcher.Invoke(new Action(() =>
			{
				LotusWarnMsg.ShowDialog(m_gnlmsgLotus);
			}));
		}

		// <summary>
		// Wait Control clear function, clear value in Wait Control and hide it
		// </summary>
		private void LotusWaitControlClear()
		{
			m_ctlmsgLotus.bshow = false;
			m_ctlmsgLotus.percent = 0;
			m_ctlmsgLotus.message = String.Empty;
			LotusWaitControlInvoke(m_ctlmsgLotus);
		}

		// <summary>
		// Wait Control Invoke function, display Wait Control according to parsed-in ControlMessage
		// </summary>
		// <param name="ctlmsgInput"></param>
		private void LotusWaitControlInvoke(ControlMessage ctlmsgInput)
		{
			LotusWaitControl.Dispatcher.Invoke(new Action(() =>
			{
				LotusWaitControl.IsBusy = ctlmsgInput.bshow;
				LotusWaitControl.Text = ctlmsgInput.message;
				LotusWaitControl.Percent = String.Format("{0}%", ctlmsgInput.percent);
			}));
		}

		// <summary>
		// Set up ControlMessage, then call LotusWaitControlInvoke() to show Wait Control
		// iDelay will makes Control display longer, and defult is 0
		// </summary>
		// <param name="bShowsIn"></param>
		// <param name="iPercentIn"></param>
		// <param name="strMessageIn"></param>
		// <param name="iDelayIn"></param>
		private void WaitControlLotusSetup(bool bShowsIn, int iPercentIn, string strMessageIn, int iDelayIn = 0)
		{
			m_ctlmsgLotus.bshow = bShowsIn;
			m_ctlmsgLotus.percent = iPercentIn;
			m_ctlmsgLotus.message = strMessageIn;

			if (iPercentIn == 100)
			{
				LotusWaitControlClear();
				LotusWarnMsgInvoke(strMessageIn, 0);
			}
			else if (iPercentIn == -1)
			{
				LotusWaitControlClear();
				LotusWarnMsgInvoke(strMessageIn);
			}
			else
			{
				if (iDelayIn > 0)
				{
					Action EmptyDelegate = delegate() { };
					LotusWaitControlInvoke(m_ctlmsgLotus);
					LotusWaitControl.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
					m_tskmsgLotus.controlreq = COMMON_CONTROL.COMMON_CONTROL_WAITTING;
					for (int i = 0; i < iDelayIn; i++)
					{
						System.Windows.Forms.Application.DoEvents();
						Thread.Sleep(1);
					}
				}
			}
		}

		#endregion

	}
}
