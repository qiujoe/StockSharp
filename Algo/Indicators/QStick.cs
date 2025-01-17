namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// QStick.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/QStick.ashx.
	/// </remarks>
	public class QStick : LengthIndicator<IIndicatorValue>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Initializes a new instance of the <see cref="QStick"/>.
		/// </summary>
		public QStick()
		{
			_sma = new SimpleMovingAverage();
			Length = 15;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _sma.IsFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			return _sma.Process(input.SetValue(this, candle.OpenPrice - candle.ClosePrice));
		}
	}
}