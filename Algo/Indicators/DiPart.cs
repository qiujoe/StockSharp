﻿namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// The part of the indicator <see cref="DirectionalIndex"/>.
	/// </summary>
	public abstract class DiPart : LengthIndicator<decimal>
	{
		private readonly AverageTrueRange _averageTrueRange;
		private readonly LengthIndicator<decimal> _movingAverage;
		private Candle _lastCandle;
		private bool _isFormed;

		/// <summary>
		/// Initialize <see cref="DiPart"/>.
		/// </summary>
		protected DiPart()
		{
			_averageTrueRange = new AverageTrueRange(new WilderMovingAverage(), new TrueRange());
			_movingAverage = new WilderMovingAverage();

			Length = 5;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_averageTrueRange.Length = Length;
			_movingAverage.Length = Length;

			_lastCandle = null;
			_isFormed = false;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _isFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			decimal? result = null;

			var candle = input.GetValue<Candle>();

			// задержка в 1 период
			_isFormed = _averageTrueRange.IsFormed && _movingAverage.IsFormed;

			_averageTrueRange.Process(input);

			if (_lastCandle != null)
			{
				var trValue = _averageTrueRange.GetCurrentValue();

				// не вносить в тернарный оператор! 
				var maValue = _movingAverage.Process(new DecimalIndicatorValue(this, GetValue(candle, _lastCandle)) { IsFinal = input.IsFinal });

				if (!maValue.IsEmpty)
					result = (trValue != 0m) ? (100m * maValue.GetValue<decimal>() / trValue) : 0m;
			}

			if (input.IsFinal)
				_lastCandle = candle;

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}

		/// <summary>
		/// To get the part value.
		/// </summary>
		/// <param name="current">The current candle.</param>
		/// <param name="prev">The previous candle.</param>
		/// <returns>Value.</returns>
		protected abstract decimal GetValue(Candle current, Candle prev);
	}
}