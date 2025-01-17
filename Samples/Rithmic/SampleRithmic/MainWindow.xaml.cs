namespace SampleRithmic
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Rithmic;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		public static MainWindow Instance { get; private set; }

		public static readonly DependencyProperty IsConnectedProperty = 
				DependencyProperty.Register("IsConnected", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}

		public RithmicTrader Trader { get; private set; }

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Rithmic");

			_securitiesWindow.MakeHideable();
			_ordersWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_myTradesWindow.MakeHideable();

			var guilistener = new GuiLogListener(LogControl);
			//guilistener.Filters.Add(msg => msg.Level > LogLevels.Debug);
			_logManager.Listeners.Add(guilistener);

			_logManager.Listeners.Add(new FileLogListener("rithmic")
			{
				LogDirectory = "Logs"
			});
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Properties.Settings.Default.Save();

			_securitiesWindow.DeleteHideable();
			_ordersWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();

			_securitiesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_myTradesWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			var pwd = PwdBox.Password;

			if (!IsConnected)
			{
				var settings = Properties.Settings.Default;

				if (settings.Username.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3751);
					return;
				}

				if (pwd.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new RithmicTrader { LogLevel = LogLevels.Debug };

					_logManager.Sources.Add(Trader);

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						this.GuiAsync(() => OnConnectionChanged(true));
					};

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						OnConnectionChanged(Trader.ConnectionState == ConnectionStates.Connected);
						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					Trader.Disconnected += () => this.GuiAsync(() => OnConnectionChanged(false));

					// subscribe on error event
					//Trader.Error += error =>
					//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), "Error"));

					// subscribe on error of market data subscription event
					Trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
					Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
					Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
					Trader.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);
					Trader.NewPortfolios += portfolios =>
					{
						// subscribe on portfolio updates
						portfolios.ForEach(Trader.RegisterPortfolio);

						_portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
					};
					Trader.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

					// subscribe on error of order registration event
					Trader.OrdersRegisterFailed += OrdersFailed;
					// subscribe on error of order cancelling event
					Trader.OrdersCancelFailed += OrdersFailed;

					// subscribe on error of stop-order registration event
					Trader.StopOrdersRegisterFailed += OrdersFailed;
					// subscribe on error of stop-order cancelling event
					Trader.StopOrdersCancelFailed += OrdersFailed;

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;
				}

				Trader.UserName = settings.Username;
				Trader.Server = settings.Server;
				Trader.Password = pwd;
				Trader.CertFile = settings.CertFile;

				Trader.Connect();
			}
			else
			{
				Trader.Disconnect();
			}
		}

		private void OnConnectionChanged(bool isConnected)
		{
			IsConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
				{
					var msg = fail.Error.ToString();
					MessageBox.Show(this, msg, LocalizedStrings.Str2960);
				}
			});
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowStopOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
		}

		private void CertificateButtonClick(object sender, RoutedEventArgs e)
		{
			var dialog = new VistaOpenFileDialog
			{
				Filter = @"Certificates files (*.pk12)|*.pk12|All files (*.*)|*.*",
				CheckFileExists = true,
				Multiselect = false,
			};

			if (dialog.ShowDialog(this) != true)
				return;

			Properties.Settings.Default.CertFile = dialog.FileName;
		}
	}
}