namespace StockSharp.Hydra.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.IQFeed;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.PropertyGrid;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/f9e23239-7587-41ad-9644-c6df8e467c38.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.History |
		TaskCategories.Paid | TaskCategories.Ticks | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Stock | TaskCategories.Forex)]
	class IQFeedTask : ConnectorHydraTask<IQFeedMarketDataMessageAdapter>
	{
		private const string _sourceName = "IQFeed";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class IQFeedSettings : ConnectorHydraTaskSettings
		{
			private const string _category = _sourceName;

			public IQFeedSettings(HydraTaskSettings settings)
				: base(settings)
			{
				CollectionHelper.TryAdd(ExtensionInfo, "CandleDayStep", 30);
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str2535Key)]
			[DescriptionLoc(LocalizedStrings.Str2536Key)]
			[PropertyOrder(0)]
			public bool IsRealTime
			{
				get { return ExtensionInfo["IsRealTime"].To<bool>(); }
				set { ExtensionInfo["IsRealTime"] = value; }
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str2537Key)]
			[DescriptionLoc(LocalizedStrings.Str2538Key)]
			[PropertyOrder(1)]
			public bool IsDownloadSecurityFromSite
			{
				get { return ExtensionInfo["IsDownloadSecurityFromSite"].To<bool>(); }
				set { ExtensionInfo["IsDownloadSecurityFromSite"] = value; }
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str2539Key)]
			[DescriptionLoc(LocalizedStrings.Str2540Key)]
			[PropertyOrder(2)]
			[Editor(typeof(SecurityTypesComboBoxEditor), typeof(SecurityTypesComboBoxEditor))]
			public IEnumerable<SecurityTypes> Types
			{
				get
				{
					var types = ExtensionInfo.TryGetValue("Types");

					return types == null
						? Enumerable.Empty<SecurityTypes>()
						: ((IEnumerable<string>)types).Select(t => t.To<SecurityTypes>()).ToArray();
				}
				set
				{
					ExtensionInfo["Types"] = value.Select(s => s.To<string>()).ToArray();
				}
			}

			[CategoryLoc(LocalizedStrings.Str174Key)]
			[DisplayNameLoc(LocalizedStrings.Str2541Key)]
			[DescriptionLoc(LocalizedStrings.Str2542Key)]
			[PropertyOrder(0)]
			public EndPoint Level1Address
			{
				get { return ExtensionInfo["Level1Address"].To<EndPoint>(); }
				set { ExtensionInfo["Level1Address"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str174Key)]
			[DisplayNameLoc(LocalizedStrings.Str2543Key)]
			[DescriptionLoc(LocalizedStrings.Str2544Key)]
			[PropertyOrder(1)]
			public EndPoint Level2Address
			{
				get { return ExtensionInfo["Level2Address"].To<EndPoint>(); }
				set { ExtensionInfo["Level2Address"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str174Key)]
			[DisplayNameLoc(LocalizedStrings.Str2545Key)]
			[DescriptionLoc(LocalizedStrings.Str2546Key)]
			[PropertyOrder(2)]
			public EndPoint LookupAddress
			{
				get { return ExtensionInfo["LookupAddress"].To<EndPoint>(); }
				set { ExtensionInfo["LookupAddress"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str174Key)]
			[DisplayNameLoc(LocalizedStrings.Str2547Key)]
			[DescriptionLoc(LocalizedStrings.Str2548Key)]
			[PropertyOrder(3)]
			public EndPoint AdminAddress
			{
				get { return ExtensionInfo["AdminAddress"].To<EndPoint>(); }
				set { ExtensionInfo["AdminAddress"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
			[DescriptionLoc(LocalizedStrings.CandleTimeIntervalKey)]
			[PropertyOrder(4)]
			public int CandleDayStep
			{
				get { return ExtensionInfo["CandleDayStep"].To<int>(); }
				set
				{
					if (value < 1)
						throw new ArgumentOutOfRangeException();

					ExtensionInfo["CandleDayStep"] = value;
				}
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		private IQFeedSettings _settings;

		public IQFeedTask()
			: base(new IQFeedTrader())
		{
			_supportedCandleSeries = IQFeedMarketDataMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Candle), typeof(QuoteChangeMessage), typeof(Level1ChangeMessage) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new IQFeedSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Level1Address = IQFeedAddresses.DefaultLevel1Address;
			_settings.Level2Address = IQFeedAddresses.DefaultLevel2Address;
			_settings.LookupAddress = IQFeedAddresses.DefaultLookupAddress;
			_settings.AdminAddress = IQFeedAddresses.DefaultAdminAddress;
			_settings.Offset = 0;
			_settings.StartFrom = DateTime.Today.Subtract(TimeSpan.FromDays(30));
			_settings.IsDownloadSecurityFromSite = false;
			_settings.IsDownloadNews = true;
			_settings.Types = new[] { SecurityTypes.Stock };
			_settings.IsRealTime = false;
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
			_settings.CandleDayStep = 30;
		}

		protected override IQFeedMarketDataMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new IQFeedMarketDataMessageAdapter(generator)
			{
				Level1Address = _settings.Level1Address,
				Level2Address = _settings.Level2Address,
				LookupAddress = _settings.LookupAddress,
				AdminAddress = _settings.AdminAddress,
				IsDownloadSecurityFromSite = _settings.IsDownloadSecurityFromSite,
				SecurityTypesFilter = _settings.Types
			};
		}

		protected override TimeSpan OnProcess()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			if (this.GetAllSecurity() != null)
			{
				this.AddWarningLog(LocalizedStrings.Str2549);
				return TimeSpan.MaxValue;
			}

			if (_settings.IsRealTime)
				return base.OnProcess();
			
			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.Offset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			var hasSecurities = false;

			foreach (var security in GetWorkingSecurities())
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

				if (security.MarketDataTypesSet.Contains(typeof(Level1ChangeMessage)))
				{
					var tradeStorage = StorageRegistry.GetTickMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);

					foreach (var date in allDates.Except(tradeStorage.Dates))
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !security.IsTradeDate(date))
						{
							this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
							continue;
						}

						this.AddInfoLog(LocalizedStrings.Str2294Params, date, security.Security.Id);

						bool isSuccess;
						var trades = ((IQFeedTrader)Connector).GetHistoricalLevel1(security.Security.ToSecurityId(), date, date.EndOfDay(), out isSuccess);

						if (isSuccess)
						{
							if (trades.Any())
								SaveLevel1Changes(security, trades);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}
						else
							this.AddErrorLog(LocalizedStrings.Str2550);
					}
				}
				else
					this.AddDebugLog(LocalizedStrings.MarketDataNotEnabled, security.Security.Id, typeof(Level1ChangeMessage).Name);

				foreach (var series in security.CandleSeries)
				{
					if (!CanProcess())
						break;

					if (series.CandleType != typeof(TimeFrameCandle))
					{
						this.AddWarningLog(LocalizedStrings.Str2296Params, series);
						continue;
					}

					var candleStorage = StorageRegistry.GetCandleMessageStorage(series.CandleType.ToCandleMessageType(), security.Security, series.Arg, _settings.Drive, _settings.StorageFormat);
					var emptyDates = allDates.Except(candleStorage.Dates).ToArray();

					if (emptyDates.IsEmpty())
					{
						this.AddInfoLog(LocalizedStrings.Str2297Params, series.Arg, security.Security.Id);
						continue;
					}

					var currDate = emptyDates.First();
					var lastDate = emptyDates.Last();

					while (currDate <= lastDate)
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !security.IsTradeDate(currDate))
						{
							this.AddDebugLog(LocalizedStrings.WeekEndDate, currDate);
							currDate = currDate.AddDays(1);
							continue;
						}

						var till = currDate.AddDays(_settings.CandleDayStep - 1).EndOfDay();
						this.AddInfoLog(LocalizedStrings.Str2298Params, series, currDate, till, security.Security.Id);

						bool isSuccess;
						var candles = ((IQFeedTrader)Connector).GetHistoricalCandles(security.Security, series.CandleType, series.Arg, currDate, till, out isSuccess);

						if (isSuccess)
						{
							if (candles.Any())
								SaveCandles(security, candles);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}
						else
							this.AddErrorLog(LocalizedStrings.Str2550);

						currDate = currDate.AddDays(_settings.CandleDayStep);
					}
				}
			}

			if (!hasSecurities)
			{
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			if (CanProcess())
			{
				this.AddInfoLog(LocalizedStrings.Str2300);

				_settings.StartFrom = endDate;
				SaveSettings();
			}

			return _settings.Interval;
		}
	}
}