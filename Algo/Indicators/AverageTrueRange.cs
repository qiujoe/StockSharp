namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// The average true range <see cref="AverageTrueRange.TrueRange"/>.
	/// </summary>
	[DisplayName("ATR")]
	[DescriptionLoc(LocalizedStrings.Str758Key)]
	public class AverageTrueRange : LengthIndicator<IIndicatorValue>
	{
		private bool _isFormed;

		/// <summary>
		/// Initializes a new instance of the <see cref="AverageTrueRange"/>.
		/// </summary>
		public AverageTrueRange()
			: this(new WilderMovingAverage(), new TrueRange())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AverageTrueRange"/>.
		/// </summary>
		/// <param name="movingAverage">Moving Average.</param>
		/// <param name="trueRange">True range.</param>
		public AverageTrueRange(LengthIndicator<decimal> movingAverage, TrueRange trueRange)
		{
			if (movingAverage == null)
				throw new ArgumentNullException(nameof(movingAverage));

			if (trueRange == null)
				throw new ArgumentNullException(nameof(trueRange));

			MovingAverage = movingAverage;
			TrueRange = trueRange;
		}

		/// <summary>
		/// Moving Average.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; }

		/// <summary>
		/// True range.
		/// </summary>
		[Browsable(false)]
		public TrueRange TrueRange { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _isFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_isFormed = false;

			MovingAverage.Length = Length;
			TrueRange.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			// используем дополнительную переменную IsFormed, 
			// т.к. нужна задержка в один период для корректной инициализации скользящей средней
			_isFormed = MovingAverage.IsFormed;

			return MovingAverage.Process(TrueRange.Process(input));
		}
	}
}