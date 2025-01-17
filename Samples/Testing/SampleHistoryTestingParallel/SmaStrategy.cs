namespace SampleHistoryTestingParallel
{
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Messages;

	class SmaStrategy : Strategy
	{
		private readonly ICandleManager _candleManager;
		private readonly CandleSeries _series;
		private bool _isShortLessThenLong;

		private readonly StrategyParam<int> _longSmaPeriod;

		public int LongSmaPeriod
		{
			get { return _longSmaPeriod.Value; }
		}

		private readonly StrategyParam<int> _shortSmaPeriod;

		public int ShortSmaPeriod
		{
			get { return _shortSmaPeriod.Value; }
		}

		public SmaStrategy(ICandleManager candleManager, CandleSeries series, SimpleMovingAverage longSma, SimpleMovingAverage shortSma)
		{
			_candleManager = candleManager;
			_series = series;

			LongSma = longSma;
			ShortSma = shortSma;

			_longSmaPeriod = this.Param("LongSmaPeriod", longSma.Length);
			_shortSmaPeriod = this.Param("ShortSmaPeriod", shortSma.Length);
		}

		public SimpleMovingAverage LongSma { get; }
		public SimpleMovingAverage ShortSma { get; }

		protected override void OnStarted()
		{
			_candleManager
				.WhenCandlesFinished(_series)
				.Do(ProcessCandle)
				.Apply(this);

			this
				.GetCandleManager()
				.Start(_series);

			// store current values for short and long
			_isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

			base.OnStarted();
		}

		private void ProcessCandle(Candle candle)
		{
			// strategy are stopping
			if (ProcessState == ProcessStates.Stopping)
			{
				CancelActiveOrders();
				return;
			}

			// process new candle
			LongSma.Process(candle);
			ShortSma.Process(candle);

			// calc new values for short and long
			var isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

			// crossing happened
			if (_isShortLessThenLong != isShortLessThenLong)
			{
				// if short less than long, the sale, otherwise buy
				var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

				// calc size for open position or revert
				var volume = Position == 0 ? Volume : Position.Abs() * 2;

				// register order (limit order)
				RegisterOrder(this.CreateOrder(direction, (decimal)(Security.GetCurrentPrice(this, direction) ?? 0), volume));

				// or revert position via market quoting
				//var strategy = new MarketQuotingStrategy(direction, volume);
				//ChildStrategies.Add(strategy);

				// store current values for short and long
				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}
}