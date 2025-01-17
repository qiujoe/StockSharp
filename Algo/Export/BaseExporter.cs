namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base class of export.
	/// </summary>
	public abstract class BaseExporter
	{
		private readonly Func<int, bool> _isCancelled;

		/// <summary>
		/// Initialize <see cref="BaseExporter"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The data parameter.</param>
		/// <param name="isCancelled">The processor, returning export interruption sign.</param>
		/// <param name="path">The path to file.</param>
		protected BaseExporter(Security security, object arg, Func<int, bool> isCancelled, string path)
		{
			//if (security == null)
			//	throw new ArgumentNullException("security");

			if (isCancelled == null)
				throw new ArgumentNullException(nameof(isCancelled));

			if (path.IsEmpty())
				throw new ArgumentNullException(nameof(path));

			Security = security;
			Arg = arg;
			_isCancelled = isCancelled;
			Path = path;
		}

		/// <summary>
		/// Security.
		/// </summary>
		protected Security Security { get; private set; }

		/// <summary>
		/// The data parameter.
		/// </summary>
		public object Arg { get; private set; }

		/// <summary>
		/// The path to file.
		/// </summary>
		protected string Path { get; private set; }

		/// <summary>
		/// To export values.
		/// </summary>
		/// <param name="dataType">Market data type.</param>
		/// <param name="values">Value.</param>
		public void Export(Type dataType, IEnumerable values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (dataType == typeof(Trade))
					Export(((IEnumerable<Trade>)values).Select(t => t.ToMessage()));
				else if (dataType == typeof(MarketDepth))
					Export(((IEnumerable<MarketDepth>)values).Select(d => d.ToMessage()));
				else if (dataType == typeof(QuoteChangeMessage))
					Export((IEnumerable<QuoteChangeMessage>)values);
				else if (dataType == typeof(Level1ChangeMessage))
					Export((IEnumerable<Level1ChangeMessage>)values);
				else if (dataType == typeof(OrderLogItem))
					Export(((IEnumerable<OrderLogItem>)values).Select(i => i.ToMessage()));
				else if (dataType == typeof(ExecutionMessage))
					Export((IEnumerable<ExecutionMessage>)values);
				else if (dataType.IsSubclassOf(typeof(Candle)))
					Export(((IEnumerable<Candle>)values).Select(c => c.ToMessage()));
				else if (dataType.IsSubclassOf(typeof(CandleMessage)))
					Export((IEnumerable<CandleMessage>)values);
				else if (dataType == typeof(News))
					Export(((IEnumerable<News>)values).Select(s => s.ToMessage()));
				else if (dataType == typeof(NewsMessage))
					Export((IEnumerable<NewsMessage>)values);
				else if (dataType == typeof(Security))
					Export(((IEnumerable<Security>)values).Select(s => s.ToMessage()));
				else if (dataType == typeof(SecurityMessage))
					Export((IEnumerable<SecurityMessage>)values);
				else
					throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str721);
			});
		}

		/// <summary>
		/// Is it possible to continue export.
		/// </summary>
		/// <param name="exported">The number of exported elements from previous call of the method.</param>
		/// <returns><see langword="true" />, if export can be continued, otherwise, <see langword="false" />.</returns>
		protected bool CanProcess(int exported = 1)
		{
			return !_isCancelled(exported);
		}

		/// <summary>
		/// To export <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected abstract void Export(IEnumerable<QuoteChangeMessage> messages);

		/// <summary>
		/// To export <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected abstract void Export(IEnumerable<Level1ChangeMessage> messages);

		/// <summary>
		/// To export <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected abstract void Export(IEnumerable<ExecutionMessage> messages);

		/// <summary>
		/// To export <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected abstract void Export(IEnumerable<CandleMessage> messages);

		/// <summary>
		/// To export <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected abstract void Export(IEnumerable<NewsMessage> messages);

		/// <summary>
		/// To export <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected abstract void Export(IEnumerable<SecurityMessage> messages);
	}
}