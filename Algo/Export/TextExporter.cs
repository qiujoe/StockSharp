namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;

	using SmartFormat;
	using SmartFormat.Core.Formatting;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The export into text file.
	/// </summary>
	public class TextExporter : BaseExporter
	{
		private readonly string _template;
		private readonly string _header;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextExporter"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The data parameter.</param>
		/// <param name="isCancelled">The processor, returning export interruption sign.</param>
		/// <param name="fileName">The path to file.</param>
		/// <param name="template">The string formatting template.</param>
		/// <param name="header">Header at the first line. Do not add header while empty string.</param>
		public TextExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName, string template, string header)
			: base(security, arg, isCancelled, fileName)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException(nameof(template));

			_template = template;
			_header = header;
		}

		/// <summary>
		/// To export <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// To export <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages.SelectMany(d => d.Asks.Concat(d.Bids).OrderByDescending(q => q.Price).Select(q => new TimeQuoteChange(q, d))));
		}

		/// <summary>
		/// To export <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// To export <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// To export <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// To export <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages);
		}

		private void Do<TValue>(IEnumerable<TValue> values)
		{
			using (var writer = new StreamWriter(Path))
			{
				if (!_header.IsEmpty())
					writer.WriteLine(_header);

				FormatCache templateCache = null;
				var formater = Smart.Default;

				foreach (var value in values)
				{
					if (!CanProcess())
						break;

					writer.WriteLine(formater.FormatWithCache(ref templateCache, _template, value));
				}

				writer.Flush();
			}
		}
	}
}