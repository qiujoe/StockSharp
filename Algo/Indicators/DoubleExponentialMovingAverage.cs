﻿namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	/// <summary>
	/// Double Exponential Moving Average.
	/// </summary>
	/// <remarks>
	/// ((2 * EMA) – EMA of EMA).
	/// </remarks>
	[DisplayName("DEMA")]
	[Description("Double Exponential Moving Average")]
	public class DoubleExponentialMovingAverage : LengthIndicator<decimal>
	{
		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;

		/// <summary>
		/// Initializes a new instance of the <see cref="DoubleExponentialMovingAverage"/>.
		/// </summary>
		public DoubleExponentialMovingAverage()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();

			Length = 32;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_ema2.Length = _ema1.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _ema1.IsFormed && _ema2.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema1Value = _ema1.Process(input);

			if (!_ema1.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema2Value = _ema2.Process(ema1Value);

			return new DecimalIndicatorValue(this, 2 * ema1Value.GetValue<decimal>() - ema2Value.GetValue<decimal>());
		}
	}
}
