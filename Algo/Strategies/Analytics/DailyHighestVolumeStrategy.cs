namespace StockSharp.Algo.Strategies.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Media;

	using Ecng.Xaml.Charting;
	using Ecng.Xaml.Charting.Model.DataSeries;
	using Ecng.Xaml.Charting.Numerics;
	using Ecng.Xaml.Charting.Visuals.Axes;
	using Ecng.Xaml.Charting.Visuals.RenderableSeries;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The analytic strategy, calculating distribution of the biggest volume by hours.
	/// </summary>
	public class DailyHighestVolumeStrategy : BaseAnalyticsStrategy
	{
		private class GridRow : NotifiableObject
		{
			public TimeSpan Time { get; set; }

			private decimal _volume;

			public decimal Volume
			{
				get { return _volume; }
				set
				{
					_volume = value;
					NotifyChanged("Price");
				}
			}
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

		/// <summary>
		/// Time-frame.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str1242Key)]
		[DescriptionLoc(LocalizedStrings.Str1243Key)]
		[CategoryLoc(LocalizedStrings.Str1221Key)]
		[PropertyOrder(2)]
		public TimeSpan TimeFrame
		{
			get { return _timeFrame.Value; }
			set { _timeFrame.Value = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DailyHighestVolumeStrategy"/>.
		/// </summary>
		public DailyHighestVolumeStrategy()
		{
			_timeFrame = this.Param("TimeFrame", TimeSpan.FromMinutes(5));
		}

		/// <summary>
		/// To analyze.
		/// </summary>
		protected override void OnAnalyze()
		{
			var chart = Chart;
			var grid = Grid;

			var chartSeries = new XyzDataSeries<DateTime, double, double>();
			ThreadSafeObservableCollection<GridRow> gridSeries = null;

			chart.GuiSync(() =>
			{
				// clear prev values
				chart.RenderableSeries.Clear();
				grid.Columns.Clear();

				chart.RenderableSeries.Add(new FastBubbleRenderableSeries
				{
					ResamplingMode = ResamplingMode.Auto,
					BubbleColor = Colors.Chocolate,
					ZScaleFactor = 0.1,
					AutoZRange = true,
					DataSeries = chartSeries
				});

				chart.XAxis = new DateTimeAxis { GrowBy = new DoubleRange(0.0, 0.1) };
				chart.YAxis = new NumericAxis { GrowBy = new DoubleRange(0.1, 0.1) };

				grid.AddTextColumn("Time", LocalizedStrings.Time).Width = 150;
				var volumeColumn = grid.AddTextColumn("Volume", LocalizedStrings.Volume);
				volumeColumn.Width = 100;

				var gridSource = new ObservableCollectionEx<GridRow>();
				grid.ItemsSource = gridSource;
				gridSeries = new ThreadSafeObservableCollection<GridRow>(gridSource);

				grid.SetSort(volumeColumn, ListSortDirection.Descending);
			});

			// get candle storage
			var storage = StorateRegistry.GetCandleStorage(typeof(TimeFrameCandle), Security, TimeFrame, format: StorageFormat);
			
			// get available dates for the specified period
			var dates = storage.GetDates(From, To).ToArray();

			var rows = new Dictionary<TimeSpan, GridRow>();

			foreach (var loadDate in dates)
			{
				// check if stopped
				if (ProcessState != ProcessStates.Started)
					break;

				// load candles
				var candles = storage.Load(loadDate);

				// groupping candles by open time
				var groupedCandles = candles.GroupBy(c => c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1)));

				foreach (var group in groupedCandles.OrderBy(g => g.Key))
				{
					// check if stopped
					if (ProcessState != ProcessStates.Started)
						break;

					var time = group.Key;

					// calc total volume for the specified time frame
					var sumVol = group.Sum(c => c.TotalVolume);

					var row = rows.TryGetValue(time);
					if (row == null)
					{
						// new volume level
						rows.Add(time, row = new GridRow { Time = time, Volume = sumVol });

						// draw on chart
						chartSeries.Append(DateTime.Today + time, (double)sumVol, (double)sumVol / 1000);

						// draw on table
						gridSeries.Add(row);
					}
					else
					{
						// update existing volume level
						row.Volume += sumVol;

						// update chart
						chartSeries.Update(DateTime.Today + time, (double)row.Volume, (double)row.Volume / 1000);
					}
				}
				
				chart.GuiAsync(() =>
				{
					// update grid sorting
					grid.RefreshSort();

					// scale chart
					chart.ZoomExtents();
				});
			}

			// notify the script stopped
			Stop();
		}
	}
}