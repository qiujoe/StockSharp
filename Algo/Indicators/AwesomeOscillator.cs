namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Awesome Oscillator.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/awesome.
	/// </remarks>
	[DisplayName("AO")]
	[DescriptionLoc(LocalizedStrings.Str836Key)]
	public class AwesomeOscillator : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AwesomeOscillator"/>.
		/// </summary>
		public AwesomeOscillator()
			: this(new SimpleMovingAverage { Length = 34 }, new SimpleMovingAverage { Length = 5 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AwesomeOscillator"/>.
		/// </summary>
		/// <param name="longSma">Long moving average.</param>
		/// <param name="shortSma">Short moving average.</param>
		public AwesomeOscillator(SimpleMovingAverage longSma, SimpleMovingAverage shortSma)
		{
			if (longSma == null)
				throw new ArgumentNullException(nameof(longSma));

			if (shortSma == null)
				throw new ArgumentNullException(nameof(shortSma));

			ShortMa = shortSma;
			LongMa = longSma;
			MedianPrice = new MedianPrice();
		}

		/// <summary>
		/// Long moving average.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str798Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage LongMa { get; }

		/// <summary>
		/// Short moving average.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str800Key)]
		[DescriptionLoc(LocalizedStrings.Str799Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage ShortMa { get; }

		/// <summary>
		/// Median price.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str843Key)]
		[DescriptionLoc(LocalizedStrings.Str745Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public MedianPrice MedianPrice { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => LongMa.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var mpValue = MedianPrice.Process(input);

			var sValue = ShortMa.Process(mpValue).GetValue<decimal>();
			var lValue = LongMa.Process(mpValue).GetValue<decimal>();

			return new DecimalIndicatorValue(this, sValue - lValue);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			LongMa.LoadNotNull(settings, "LongMa");
			ShortMa.LoadNotNull(settings, "ShortMa");
			MedianPrice.LoadNotNull(settings, "MedianPrice");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("LongMa", LongMa.Save());
			settings.SetValue("ShortMa", ShortMa.Save());
			settings.SetValue("MedianPrice", MedianPrice.Save());
		}
	}
}