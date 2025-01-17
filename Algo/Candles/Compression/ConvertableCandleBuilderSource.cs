namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The base data source for <see cref="ICandleBuilder"/> which convert data from the <typeparamref name="TSourceValue" /> type to the <see cref="ICandleBuilderSourceValue"/>.
	/// </summary>
	/// <typeparam name="TSourceValue">The source data type (for example, <see cref="Trade"/>).</typeparam>
	public abstract class ConvertableCandleBuilderSource<TSourceValue> : BaseCandleBuilderSource
	{
		static ConvertableCandleBuilderSource()
		{
			if (typeof(TSourceValue) == typeof(Trade))
			{
				DefaultConverter = ((Func<Trade, ICandleBuilderSourceValue>)(t => new TradeCandleBuilderSourceValue(t))).To<Func<TSourceValue, ICandleBuilderSourceValue>>();
				DefaultFilter = ((Func<Trade, bool>)(t => t.IsSystem != false)).To<Func<TSourceValue, bool>>();
			}
			else if (typeof(TSourceValue) == typeof(MarketDepth))
			{
				DefaultConverter = ((Func<MarketDepth, ICandleBuilderSourceValue>)(d => new DepthCandleBuilderSourceValue(d))).To<Func<TSourceValue, ICandleBuilderSourceValue>>();
				DefaultFilter = v => true;
			}
			else
				throw new InvalidOperationException(LocalizedStrings.Str653Params.Put(typeof(TSourceValue)));
		}

		/// <summary>
		/// Initialize <see cref="ConvertableCandleBuilderSource{T}"/>.
		/// </summary>
		protected ConvertableCandleBuilderSource()
		{
		}

		/// <summary>
		/// The default function to convert data from the <typeparamref name="TSourceValue" /> type to the <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		public static Func<TSourceValue, ICandleBuilderSourceValue> DefaultConverter { get; }

		private Func<TSourceValue, ICandleBuilderSourceValue> _converter = DefaultConverter;

		/// <summary>
		/// The function to convert data from the <typeparamref name="TSourceValue" /> type to the <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		public Func<TSourceValue, ICandleBuilderSourceValue> Converter
		{
			get { return _converter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_converter = value;
			}
		}

		/// <summary>
		/// The default function to filter data <typeparamref name="TSourceValue" />.
		/// </summary>
		public static Func<TSourceValue, bool> DefaultFilter { get; }

		private Func<TSourceValue, bool> _filter = DefaultFilter;

		/// <summary>
		/// The function to filter data <typeparamref name="TSourceValue" />.
		/// </summary>
		public Func<TSourceValue, bool> Filter
		{
			get { return _filter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_filter = value;
			}
		}

		/// <summary>
		/// To convert new data using the <see cref="Converter"/>.
		/// </summary>
		/// <param name="values">New source data.</param>
		/// <returns>Data in format <see cref="ICandleBuilder"/>.</returns>
		protected IEnumerable<ICandleBuilderSourceValue> Convert(IEnumerable<TSourceValue> values)
		{
			return values.Where(Filter).Select(Converter);
		}

		/// <summary>
		/// To convert and pass new data to the method <see cref="BaseCandleBuilderSource.RaiseProcessing"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="values">New source data.</param>
		protected virtual void NewSourceValues(CandleSeries series, IEnumerable<TSourceValue> values)
		{
			RaiseProcessing(series, Convert(values));
		}
	}

	/// <summary>
	/// The data source working directly with ready data collection.
	/// </summary>
	/// <typeparam name="TSourceValue">The source data type (for example, <see cref="Trade"/>).</typeparam>
	public class RawConvertableCandleBuilderSource<TSourceValue> : ConvertableCandleBuilderSource<TSourceValue>
	{
		private readonly Security _security;
		private readonly DateTimeOffset _from;
		private readonly DateTimeOffset _to;

		/// <summary>
		/// Initializes a new instance of the <see cref="RawConvertableCandleBuilderSource{T}"/>.
		/// </summary>
		/// <param name="security">The instrument whose data is passed to the source.</param>
		/// <param name="from">The first time value.</param>
		/// <param name="to">The last time value.</param>
		/// <param name="values">Ready data collection.</param>
		public RawConvertableCandleBuilderSource(Security security, DateTimeOffset from, DateTimeOffset to, IEnumerable<TSourceValue> values)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (values == null)
				throw new ArgumentNullException(nameof(values));

			_security = security;
			_from = from;
			_to = to;

			Values = values;
		}

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public override int SpeedPriority => 0;

		/// <summary>
		/// Ready data collection.
		/// </summary>
		public IEnumerable<TSourceValue> Values { get; }

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (series.Security != _security)
				yield break;

			yield return new Range<DateTimeOffset>(_from, _to);
		}

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (series.Security != _security)
				return;

			NewSourceValues(series, Values);

			RaiseStopped(series);
		}

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public override void Stop(CandleSeries series)
		{
			RaiseStopped(series);
		}
	}
}