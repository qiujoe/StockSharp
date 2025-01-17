namespace StockSharp.Algo
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Reasons for orders cancelling in the orders log.
	/// </summary>
	public enum OrderLogCancelReasons
	{
		/// <summary>
		/// The order re-registration.
		/// </summary>
		ReRegistered,

		/// <summary>
		/// Cancel order.
		/// </summary>
		Canceled,

		/// <summary>
		/// Group canceling of orders.
		/// </summary>
		GroupCanceled,

		/// <summary>
		/// The sign of deletion of order residual due to cross-trade.
		/// </summary>
		CrossTrade,
	}

	/// <summary>
	/// Building order book by the orders log.
	/// </summary>
	public static class OrderLogHelper
	{
		/// <summary>
		/// To check, does the string contain the order registration.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contains the order registration, otherwise, <see langword="false" />.</returns>
		public static bool IsOrderLogRegistered(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return item.OrderState == OrderStates.Active && item.TradePrice == null;
		}

		/// <summary>
		/// To check, does the string contain the order registration.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contains the order registration, otherwise, <see langword="false" />.</returns>
		public static bool IsRegistered(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogRegistered();
		}

		/// <summary>
		/// To check, does the string contain the cancelled order.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contain the cancelled order, otherwise, <see langword="false" />.</returns>
		public static bool IsOrderLogCanceled(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return item.OrderState == OrderStates.Done && item.TradePrice == null;
		}

		/// <summary>
		/// To check, does the string contain the cancelled order.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contain the cancelled order, otherwise, <see langword="false" />.</returns>
		public static bool IsCanceled(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogCanceled();
		}

		/// <summary>
		/// To check, does the string contain the order matching.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contains order matching, otherwise, <see langword="false" />.</returns>
		public static bool IsOrderLogMatched(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return item.TradeId != null;
		}

		/// <summary>
		/// To check, does the string contain the order matching.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contains order matching, otherwise, <see langword="false" />.</returns>
		public static bool IsMatched(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogMatched();
		}

		/// <summary>
		/// To get the reason for cancelling order in orders log.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>The reason for order cancelling in order log.</returns>
		public static OrderLogCancelReasons GetOrderLogCancelReason(this ExecutionMessage item)
		{
			if (!item.IsOrderLogCanceled())
				throw new ArgumentException(LocalizedStrings.Str937, nameof(item));

			if (item.OrderStatus == null)
				throw new ArgumentException(LocalizedStrings.Str938, nameof(item));

			var status = (int)item.OrderStatus;

			if (status.HasBits(0x100000))
				return OrderLogCancelReasons.ReRegistered;
			else if (status.HasBits(0x200000))
				return OrderLogCancelReasons.Canceled;
			else if (status.HasBits(0x400000))
				return OrderLogCancelReasons.GroupCanceled;
			else if (status.HasBits(0x800000))
				return OrderLogCancelReasons.CrossTrade;
			else
				throw new ArgumentOutOfRangeException(nameof(item), status, LocalizedStrings.Str939);
		}

		/// <summary>
		/// To get the reason for cancelling order in orders log.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>The reason for order cancelling in order log.</returns>
		public static OrderLogCancelReasons GetCancelReason(this OrderLogItem item)
		{
			return item.ToMessage().GetOrderLogCancelReason();
		}

		private sealed class DepthEnumerable : SimpleEnumerable<QuoteChangeMessage>, IEnumerableEx<QuoteChangeMessage>
		{
			private sealed class DepthEnumerator : IEnumerator<QuoteChangeMessage>
			{
				private readonly TimeSpan _interval;
				private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;
				private readonly IOrderLogMarketDepthBuilder _builder;
				private readonly int _maxDepth;

				public DepthEnumerator(IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval, int maxDepth)
				{
					if (builder == null)
						throw new ArgumentNullException(nameof(builder));

					if (items == null)
						throw new ArgumentNullException(nameof(items));

					if (maxDepth < 1)
						throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, LocalizedStrings.Str941);

					_itemsEnumerator = items.GetEnumerator();
					_builder = builder;
					_interval = interval;
					_maxDepth = maxDepth;
				}

				public QuoteChangeMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_itemsEnumerator.MoveNext())
					{
						var item = _itemsEnumerator.Current;

						//if (_builder == null)
						//	_builder = new OrderLogMarketDepthBuilder(new QuoteChangeMessage { SecurityId = item.SecurityId, IsSorted = true }, _maxDepth);

						if (!_builder.Update(item))
							continue;

						if (Current != null && (_builder.Depth.ServerTime - Current.ServerTime) < _interval)
							continue;

						Current = (QuoteChangeMessage)_builder.Depth.Clone();

						if (_maxDepth < int.MaxValue)
						{
							//Current.MaxDepth = _maxDepth;
							Current.Bids = Current.Bids.Take(_maxDepth).ToArray();
							Current.Asks = Current.Asks.Take(_maxDepth).ToArray();
						}

						return true;
					}

					Current = null;
					return false;
				}

				public void Reset()
				{
					_itemsEnumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Reset();
					_itemsEnumerator.Dispose();
				}
			}

			private readonly IEnumerableEx<ExecutionMessage> _items;

			public DepthEnumerable(IEnumerableEx<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval, int maxDepth)
				: base(() => new DepthEnumerator(items, builder, interval, maxDepth))
			{
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				if (interval < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(interval), interval, LocalizedStrings.Str940);

				_items = items;
			}

			int IEnumerableEx.Count => _items.Count;
		}

		/// <summary>
		/// Build market depths from order log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <param name="builder">Order log to market depth builder.</param>
		/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new string of orders log.</param>
		/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
		/// <returns>Market depths.</returns>
		public static IEnumerableEx<MarketDepth> ToMarketDepths(this IEnumerableEx<OrderLogItem> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default(TimeSpan), int maxDepth = int.MaxValue)
		{
			var first = items.FirstOrDefault();

			if (first == null)
				return Enumerable.Empty<MarketDepth>().ToEx();

			return items.ToMessages<OrderLogItem, ExecutionMessage>()
				.ToMarketDepths(builder, interval)
				.ToEntities<QuoteChangeMessage, MarketDepth>(first.Order.Security);
		}

		/// <summary>
		/// Build market depths from order log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <param name="builder">Order log to market depth builder.</param>
		/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new string of orders log.</param>
		/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
		/// <returns>Market depths.</returns>
		public static IEnumerableEx<QuoteChangeMessage> ToMarketDepths(this IEnumerableEx<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default(TimeSpan), int maxDepth = int.MaxValue)
		{
			return new DepthEnumerable(items, builder, interval, maxDepth);
		}

		private sealed class OrderLogTickEnumerable : SimpleEnumerable<ExecutionMessage>, IEnumerableEx<ExecutionMessage>
		{
			private sealed class OrderLogTickEnumerator : IEnumerator<ExecutionMessage>
			{
				private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;
				private readonly Dictionary<long, Tuple<long, Sides>> _trades = new Dictionary<long, Tuple<long, Sides>>();

				public OrderLogTickEnumerator(IEnumerable<ExecutionMessage> items)
				{
					if (items == null)
						throw new ArgumentNullException(nameof(items));

					_itemsEnumerator = items.GetEnumerator();
				}

				public ExecutionMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_itemsEnumerator.MoveNext())
					{
						var currItem = _itemsEnumerator.Current;

						var tradeId = currItem.TradeId;

						if (tradeId == null)
							continue;

						var prevItem = _trades.TryGetValue(tradeId.Value);

						if (prevItem == null)
						{
							_trades.Add(tradeId.Value, Tuple.Create(currItem.SafeGetOrderId(), currItem.Side));
						}
						else
						{
							_trades.Remove(tradeId.Value);

							Current = new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Tick,
								SecurityId = currItem.SecurityId,
								TradeId = tradeId,
								TradePrice = currItem.TradePrice,
								TradeStatus = currItem.TradeStatus,
								Volume = currItem.Volume,
								ServerTime = currItem.ServerTime,
								LocalTime = currItem.LocalTime,
								OpenInterest = currItem.OpenInterest,
								OriginSide = prevItem.Item2 == Sides.Buy
									? (prevItem.Item1 > currItem.OrderId ? Sides.Buy : Sides.Sell)
									: (prevItem.Item1 > currItem.OrderId ? Sides.Sell : Sides.Buy),
							};

							return true;
						}
					}

					Current = null;
					return false;
				}

				void IEnumerator.Reset()
				{
					_itemsEnumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					_itemsEnumerator.Dispose();
				}
			}

			private readonly IEnumerableEx<ExecutionMessage> _items;

			public OrderLogTickEnumerable(IEnumerableEx<ExecutionMessage> items)
				: base(() => new OrderLogTickEnumerator(items))
			{
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				_items = items;
			}

			int IEnumerableEx.Count => _items.Count;
		}

		/// <summary>
		/// To build tick trades from the orders log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerableEx<Trade> ToTrades(this IEnumerableEx<OrderLogItem> items)
		{
			var first = items.FirstOrDefault();

			if (first == null)
				return Enumerable.Empty<Trade>().ToEx(0);

			var ticks = items
				.Select(i => i.ToMessage())
				.ToEx(items.Count)
				.ToTicks();

			return ticks.Select(m => m.ToTrade(first.Order.Security)).ToEx(ticks.Count);
		}

		/// <summary>
		/// To build tick trades from the orders log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerableEx<ExecutionMessage> ToTicks(this IEnumerableEx<ExecutionMessage> items)
		{
			return new OrderLogTickEnumerable(items);
		}
	}
}