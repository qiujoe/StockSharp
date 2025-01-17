namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Localization;

	/// <summary>
	/// Average deviation.
	/// </summary>
	[DisplayName("MeanDeviation")]
	[DescriptionLoc(LocalizedStrings.Str744Key)]
	public class MeanDeviation : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MeanDeviation"/>.
		/// </summary>
		public MeanDeviation()
		{
			Sma = new SimpleMovingAverage();
			Length = 5;
		}

		/// <summary>
		/// Moving Average.
		/// </summary>
		[Browsable(false)]
		public SimpleMovingAverage Sma { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Sma.IsFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			Sma.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var val = input.GetValue<decimal>();

			if (input.IsFinal)
				Buffer.Add(val);

			var smaValue = Sma.Process(input).GetValue<decimal>();

			if (Buffer.Count > Length)
				Buffer.RemoveAt(0);

			// считаем значение отклонения
			var md = input.IsFinal
				? Buffer.Sum(t => Math.Abs(t - smaValue))
				: Buffer.Skip(IsFormed ? 1 : 0).Sum(t => Math.Abs(t - smaValue)) + Math.Abs(val - smaValue);

			return new DecimalIndicatorValue(this, md / Length);
		}
	}
}