namespace SampleBlackwood
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<string, Level1Window> _level1Windows = new SynchronizedDictionary<string, Level1Window>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = new[]
			{
				TimeSpan.FromTicks(1),
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(5),
				//TimeSpan.FromMinutes(30),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1),
				TimeSpan.FromDays(7),
				TimeSpan.FromDays(30)
			};
			CandlesPeriods.SelectedIndex = 1;
		}

		protected override void OnClosed(EventArgs e)
		{
			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				if (_initialized)
					trader.MarketDepthsChanged -= TraderOnMarketDepthsChanged;

				_quotesWindows.SyncDo(d =>
				{
					foreach (var pair in d)
					{
						trader.UnRegisterMarketDepth(pair.Key);

						pair.Value.DeleteHideable();
						pair.Value.Close();
					}
				});

				trader.RegisteredSecurities.ForEach(trader.UnRegisterSecurity);
				trader.RegisteredTrades.ForEach(trader.UnRegisterTrades);
			}

			base.OnClosed(e);
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Level1.IsEnabled = NewStopOrder.IsEnabled = NewOrder.IsEnabled = Level2.IsEnabled = Depth.IsEnabled = security != null;

			TryEnableCandles();
		}

		private void NewStopOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderConditionalWindow
			{
				Order = new Order
				{
					Security = SecurityPicker.SelectedSecurity,
					Type = OrderTypes.Conditional,
				},
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
				Adapter = MainWindow.Instance.Trader.Adapter
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void ShowLevel1()
		{
			var window = _level1Windows.SafeAdd(SecurityPicker.SelectedSecurity.Code, security =>
			{
				// create level1 window
				var wnd = new Level1Window { Title = security + LocalizedStrings.Str3693 };
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility != Visibility.Visible)
				window.Show();

			if (!_initialized)
			{
				MainWindow.Instance.Trader.NewMessage += TraderOnNewMessage;
				_initialized = true;
			}
		}

		private void TraderOnNewMessage(Message msg)
		{
			if (msg.Type != MessageTypes.Level1Change)
				return;

			var level1Msg = (Level1ChangeMessage)msg;
			var wnd = _level1Windows.TryGetValue(level1Msg.SecurityId.SecurityCode);

			if (wnd != null)
				wnd.Level1Grid.Messages.Add(level1Msg);
		}

		private void Level2Click(object sender, RoutedEventArgs e)
		{
			ShowLevel1();

			// subscribe on order book flow
			MainWindow.Instance.Trader.RegisterMarketDepth(SecurityPicker.SelectedSecurity);
		}

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			ShowLevel1();

			var security = SecurityPicker.SelectedSecurity;
			var trader = MainWindow.Instance.Trader;

			// subscribe on level1 and tick data flow
			trader.RegisterSecurity(security);
			trader.RegisterTrades(security);

			//if (_bidAskSecurities.Contains(security))
			//{
			//	// unsubscribe from level1 and tick data flow
			//	trader.UnRegisterSecurity(security);
			//	trader.UnRegisterTrades(security);

			//	_bidAskSecurities.Remove(security);
			//}
			//else
			//{
			//	// subscribe on level1 and tick data flow
			//	trader.RegisterSecurity(security);
			//	trader.RegisterTrades(security);

			//	_bidAskSecurities.Add(security);
			//}
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			new FindSecurityWindow().ShowModal(this);
		}

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			var tf = (TimeSpan)CandlesPeriods.SelectedItem;
			var series = new CandleSeries(typeof(TimeFrameCandle), SecurityPicker.SelectedSecurity, tf);

			new ChartWindow(series, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 100)), DateTime.Now).Show();
		}

		private void CandlesPeriods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableCandles();
		}

		private void TryEnableCandles()
		{
			Candles.IsEnabled = CandlesPeriods.SelectedItem != null && SecurityPicker.SelectedSecurity != null;
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			var window = _quotesWindows.SafeAdd(SecurityPicker.SelectedSecurity, security =>
			{
				// create order book window
				var wnd = new QuotesWindow { Title = security.Id + " " + LocalizedStrings.MarketDepth };
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();

			if (!_initialized)
			{
				TraderOnMarketDepthsChanged(new[] { trader.GetMarketDepth(SecurityPicker.SelectedSecurity) });
				trader.MarketDepthsChanged += TraderOnMarketDepthsChanged;
				_initialized = true;
			}
		}

		private void TraderOnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			foreach (var depth in depths)
			{
				var wnd = _quotesWindows.TryGetValue(depth.Security);

				if (wnd != null)
					wnd.DepthCtrl.UpdateDepth(depth);
			}
		}
	}
}