namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Envelope.
	/// </summary>
	[DisplayName("Envelope")]
	[Description("Envelope.")]
	public class Envelope : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Envelope"/>.
		/// </summary>
		public Envelope()
			: this(new SimpleMovingAverage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Envelope"/>.
		/// </summary>
		public Envelope(LengthIndicator<decimal> ma)
		{
			InnerIndicators.Add(Middle = ma);
			InnerIndicators.Add(Upper = (LengthIndicator<decimal>)ma.Clone());
			InnerIndicators.Add(Lower = (LengthIndicator<decimal>)ma.Clone());

			Upper.Name = "Upper";
			Lower.Name = "Lower";
		}

		/// <summary>
		/// Middle line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Middle { get; }

		/// <summary>
		/// Upper line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Upper { get; }

		/// <summary>
		/// Lower line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Lower { get; }

		/// <summary>
		/// Period length. By default equal to 1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str778Key)]
		[DescriptionLoc(LocalizedStrings.Str779Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get { return Middle.Length; }
			set
			{
				Middle.Length = Upper.Length = Lower.Length = value;
				Reset();
			}
		}

		private decimal _shift = 0.25m;

		/// <summary>
		/// The shift width. Specified as percentage from 0 to 1. The default equals to 0.25.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str783Key)]
		[DescriptionLoc(LocalizedStrings.Str784Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Shift
		{
			get { return _shift; }
			set
			{
				if (value < 0)
					throw new ArgumentNullException(nameof(value));

				_shift = value;
				Reset();
			}
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Middle.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = (ComplexIndicatorValue)base.OnProcess(input);

			var upper = value.InnerValues[Upper];
			value.InnerValues[Upper] = upper.SetValue(this, upper.GetValue<decimal>() * (1 + Shift));

			var lower = value.InnerValues[Lower];
			value.InnerValues[Lower] = lower.SetValue(this, lower.GetValue<decimal>() * (1 - Shift));

			return value;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Shift = settings.GetValue<decimal>("Shift");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Shift", Shift);
		}
	}
}