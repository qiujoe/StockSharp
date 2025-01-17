﻿namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Triple Exponential Moving Average.
	/// </summary>
	/// <remarks>
	/// http://tradingsim.com/blog/triple-exponential-moving-average/ (3 * EMA) – (3 * EMA of EMA) + EMA of EMA of EMA).
	/// </remarks>
	[DisplayName("TEMA")]
	[DescriptionLoc(LocalizedStrings.Str752Key)]
	public class TripleExponentialMovingAverage : LengthIndicator<decimal>
	{
		// http://www2.wealth-lab.com/WL5Wiki/GetFile.aspx?File=%2fTEMA.cs&Provider=ScrewTurn.Wiki.FilesStorageProvider

		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;
		private readonly ExponentialMovingAverage _ema3;

		/// <summary>
		/// Initializes a new instance of the <see cref="TripleExponentialMovingAverage"/>.
		/// </summary>
		public TripleExponentialMovingAverage()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();
			_ema3 = new ExponentialMovingAverage();

			Length = 32;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _ema1.IsFormed && _ema2.IsFormed && _ema3.IsFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_ema3.Length = _ema2.Length = _ema1.Length = Length;
			base.Reset();
		}

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

			if (!_ema2.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema3Value = _ema3.Process(ema2Value);

			return new DecimalIndicatorValue(this, 3 * ema1Value.GetValue<decimal>() - 3 * ema2Value.GetValue<decimal>() + ema3Value.GetValue<decimal>());
		}
	}
}