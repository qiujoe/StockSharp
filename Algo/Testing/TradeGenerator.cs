namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Tick trades generator using random method.
	/// </summary>
	public abstract class TradeGenerator : MarketDataGenerator
	{
		/// <summary>
		/// Initialize <see cref="TradeGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		protected TradeGenerator(SecurityId securityId)
			: base(securityId)
		{
			IdGenerator = new IncrementalIdGenerator();
		}

		/// <summary>
		/// Market data type.
		/// </summary>
		public override MarketDataTypes DataType => MarketDataTypes.Trades;

		private IdGenerator _idGenerator;

		/// <summary>
		/// The trade identifier generator <see cref="Trade.Id"/>.
		/// </summary>
		public IdGenerator IdGenerator
		{
			get { return _idGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_idGenerator = value;
			}
		}
	}

	/// <summary>
	/// The trade generator based on normal distribution.
	/// </summary>
	public class RandomWalkTradeGenerator : TradeGenerator
	{
		private decimal _lastTradePrice;

		/// <summary>
		/// Initializes a new instance of the <see cref="RandomWalkTradeGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		public RandomWalkTradeGenerator(SecurityId securityId)
			: base(securityId)
		{
			Interval = TimeSpan.FromMilliseconds(50);
		}

		/// <summary>
		/// To generate the value for <see cref="ExecutionMessage.OriginSide"/>. By default is disabled.
		/// </summary>
		public bool GenerateOriginSide { get; set; }

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The result of processing. If <see langword="null" /> is returned, then generator has no sufficient data to generate new message.</returns>
		protected override Message OnProcess(Message message)
		{
			DateTimeOffset time;

			switch (message.Type)
			{
				case MessageTypes.Board:
					return null;
				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;

					var value = l1Msg.Changes.TryGetValue(Level1Fields.LastTradePrice);

					if (value != null)
						_lastTradePrice = (decimal)value;

					time = l1Msg.ServerTime;

					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.Trade:
							_lastTradePrice = execMsg.TradePrice.Value;
							break;
						case ExecutionTypes.OrderLog:
							if (execMsg.TradePrice != null)
								_lastTradePrice = execMsg.TradePrice.Value;
							break;
						default:
							return null;
					}

					time = execMsg.ServerTime;

					break;
				}
				case MessageTypes.Time:
				{
					var timeMsg = (TimeMessage)message;

					time = timeMsg.ServerTime;

					break;
				}
				default:
					return null;
			}

			if (!IsTimeToGenerate(time))
				return null;

			var trade = new ExecutionMessage
			{
				SecurityId = SecurityId,
				TradeId = IdGenerator.GetNextId(),
				ServerTime = time,
				LocalTime = time.LocalDateTime,
				OriginSide = GenerateOriginSide ? RandomGen.GetEnum<Sides>() : (Sides?)null,
				Volume = Volumes.Next(),
				ExecutionType = ExecutionTypes.Tick
			};

			var priceStep = SecurityDefinition.PriceStep ?? 0.01m;

			_lastTradePrice += RandomGen.GetInt(-MaxPriceStepCount, MaxPriceStepCount) * priceStep;

			if (_lastTradePrice <= 0)
				_lastTradePrice = priceStep;

			trade.TradePrice = _lastTradePrice;

			LastGenerationTime = time;

			return trade;
		}

		/// <summary>
		/// Create a copy of <see cref="RandomWalkTradeGenerator"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override MarketDataGenerator Clone()
		{
			return new RandomWalkTradeGenerator(SecurityId)
			{
				_lastTradePrice = _lastTradePrice,

				MaxVolume = MaxVolume,
				MinVolume = MinVolume,
				MaxPriceStepCount = MaxPriceStepCount,
				Interval = Interval,
				Volumes = Volumes,
				Steps = Steps,

				GenerateOriginSide = GenerateOriginSide,
				IdGenerator = IdGenerator
			};
		}
	}
}