namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public class HistoryMessageAdapter : MessageAdapter
	{
		private Thread _loadingThread;
		private bool _disconnecting;
		private bool _isSuspended;
		private readonly SyncObject _suspendLock = new SyncObject();

		private IEnumerable<ExchangeBoard> Boards
		{
			get
			{
				return SecurityProvider
					.LookupAll()
					.Select(s => s.Board)
					.Distinct();
			}
		}

		/// <summary>
		/// The number of loaded events.
		/// </summary>
		public int LoadedMessageCount { get; private set; }

		private int _postTradeMarketTimeChangedCount = 2;

		/// <summary>
		/// The number of the event <see cref="IConnector.MarketTimeChanged"/> calls after end of trading. By default it is equal to 2.
		/// </summary>
		/// <remarks>
		/// It is required for activation of post-trade rules (rules, basing on events, occurring after end of trading).
		/// </remarks>
		public int PostTradeMarketTimeChangedCount
		{
			get { return _postTradeMarketTimeChangedCount; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_postTradeMarketTimeChangedCount = value;
			}
		}

		private IStorageRegistry _storageRegistry;

		/// <summary>
		/// Market data storage.
		/// </summary>
		public IStorageRegistry StorageRegistry
		{
			get { return _storageRegistry; }
			set
			{
				_storageRegistry = value;

				if (value != null)
					Drive = value.DefaultDrive;
			}
		}

		private IMarketDataDrive _drive;

		/// <summary>
		/// The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.
		/// </summary>
		public IMarketDataDrive Drive
		{
			get { return _drive; }
			set
			{
				if (value == null && StorageRegistry != null)
					throw new ArgumentNullException();

				_drive = value;
			}
		}

		/// <summary>
		/// The format of market data. <see cref="StorageFormats.Binary"/> is used by default.
		/// </summary>
		public StorageFormats StorageFormat { get; set; }

		/// <summary>
		/// The aggregator-storage.
		/// </summary>
		public BasketMarketDataStorage<Message> BasketStorage { get; }

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; }

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromSeconds(1);

		/// <summary>
		/// The interval of message <see cref="TimeMessage"/> generation. By default, it is equal to 1 sec.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get { return _marketTimeChangedInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			BasketStorage = new BasketMarketDataStorage<Message>();

			StartDate = DateTimeOffset.MinValue;
			StopDate = DateTimeOffset.MaxValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider)
			: this(transactionIdGenerator)
		{
			SecurityProvider = securityProvider;
			
			this.AddMarketDataSupport();
			this.AddSupportedMessage(ExtendedMessageTypes.EmulationState);
			this.AddSupportedMessage(ExtendedMessageTypes.HistorySource);
		}

		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		public DateTimeOffset StopDate { get; set; }

		private DateTimeOffset _currentTime;

		/// <summary>
		/// The current time.
		/// </summary>
		public override DateTimeOffset CurrentTime => _currentTime;

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			BasketStorage.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired => true;

		private void TryResume()
		{
			lock (_suspendLock)
			{
				if (!_isSuspended)
					return;

				_isSuspended = false;
				_suspendLock.PulseAll();
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					TryResume();
					
					LoadedMessageCount = 0;
					_disconnecting = _loadingThread != null;
					_loadingThread = null;

					if (!_disconnecting)
						SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_loadingThread != null)
						throw new InvalidOperationException(LocalizedStrings.Str1116);

					SendOutMessage(new ConnectMessage { LocalTime = StartDate.LocalDateTime });
					return;
				}

				case MessageTypes.Disconnect:
				{
					_disconnecting = true;
					TryResume();
					return;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;

					//ThreadingHelper.Thread(() =>
					//{
					//	try
					//	{
							SecurityProvider.LookupAll().ForEach(security =>
							{
								SendOutMessage(security.Board.ToMessage());

								var secMsg = security.ToMessage();
								secMsg.OriginalTransactionId = lookupMsg.TransactionId;
								SendOutMessage(secMsg);

								//SendOutMessage(new Level1ChangeMessage { SecurityId = security.ToSecurityId() }
								//	.Add(Level1Fields.StepPrice, security.StepPrice)
								//	.Add(Level1Fields.MinPrice, security.MinPrice)
								//	.Add(Level1Fields.MaxPrice, security.MaxPrice)
								//	.Add(Level1Fields.MarginBuy, security.MarginBuy)
								//	.Add(Level1Fields.MarginSell, security.MarginSell));
							});

							SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
					//	}
					//	catch (Exception ex)
					//	{
					//		SendOutError(ex);
					//	}
					//}).Name("History sec lookup").Start();
					return;
				}

				case MessageTypes.MarketData:
				case ExtendedMessageTypes.HistorySource:
					ProcessMarketDataMessage((MarketDataMessage)message);
					return;

				case ExtendedMessageTypes.EmulationState:
					var stateMsg = (EmulationStateMessage)message;
					var isSuspended = false;

					switch (stateMsg.State)
					{
						case EmulationStates.Starting:
						{
							if (_loadingThread != null)
							{
								TryResume();
								break;
							}

							_loadingThread = ThreadingHelper
								.Thread(OnLoad)
								.Name("HistoryMessageAdapter")
								.Launch();

							break;
						}

						case EmulationStates.Suspending:
						{
							lock (_suspendLock)
								_isSuspended = true;

							isSuspended = true;
							break;
						}
					}

					SendOutMessage(message);

					if (isSuspended)
						SendOutMessage(new EmulationStateMessage { State = EmulationStates.Suspended });

					return;
			}

			//SendOutMessage(message);
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var securityId = message.SecurityId;
			var security = SecurityProvider.LookupById(securityId.ToStringId());

			if (security == null)
			{
				RaiseMarketDataMessage(message, new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId)));
				return;
			}

			if (StorageRegistry == null)
			{
				RaiseMarketDataMessage(message, new InvalidOperationException(LocalizedStrings.Str1117Params.Put(message.DataType, securityId)));
				return;
			}

			var history = message as HistorySourceMessage;
			var storages = BasketStorage.InnerStorages;

			Exception error = null;

			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
						{
							storages.Add(StorageRegistry.GetLevel1MessageStorage(security, Drive, StorageFormat));

							storages.Add(new InMemoryMarketDataStorage<ClearingMessage>(security, null, date => new[]
							{
								new ClearingMessage
								{
									LocalTime = date.Date + security.Board.ExpiryTime,
									SecurityId = securityId,
									ClearMarketDepth = true
								}
							}));
						}
						else
						{
							storages.Add(new InMemoryMarketDataStorage<Level1ChangeMessage>(security, null, history.GetMessages));
						}
					}
					else
					{
						RemoveStorage<IMarketDataStorage<Level1ChangeMessage>>(security, MessageTypes.Level1Change, null);
						RemoveStorage<InMemoryMarketDataStorage<ClearingMessage>>(security, ExtendedMessageTypes.Clearing, null);
					}

					break;
				}

				case MarketDataTypes.MarketDepth:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
							storages.Add((IMarketDataStorage<QuoteChangeMessage>)StorageRegistry.GetMarketDepthStorage(security, Drive, StorageFormat));
						else
							storages.Add(new InMemoryMarketDataStorage<QuoteChangeMessage>(security, null, history.GetMessages));
					}
					else
						RemoveStorage<IMarketDataStorage<QuoteChangeMessage>>(security, MessageTypes.QuoteChange, null);
					
					break;
				}

				case MarketDataTypes.Trades:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
							storages.Add((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetTradeStorage(security, Drive, StorageFormat));
						else
							storages.Add(new InMemoryMarketDataStorage<ExecutionMessage>(security, null, history.GetMessages));
					}
					else
						RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, ExecutionTypes.Tick);
					
					break;
				}

				case MarketDataTypes.OrderLog:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
							storages.Add((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetOrderLogStorage(security, Drive, StorageFormat));
						else
							storages.Add(new InMemoryMarketDataStorage<ExecutionMessage>(security, null, history.GetMessages));
					}
					else
						RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, ExecutionTypes.OrderLog);

					break;
				}

				case MarketDataTypes.CandleTimeFrame:
				case MarketDataTypes.CandleTick:
				case MarketDataTypes.CandleVolume:
				case MarketDataTypes.CandleRange:
				case MarketDataTypes.CandlePnF:
				case MarketDataTypes.CandleRenko:
				{
					var msgType = message.DataType.ToCandleMessageType();

					if (message.IsSubscribe)
					{
						var candleType = message.DataType.ToCandleMessage();

						if (history == null)
							storages.Add(StorageRegistry.GetCandleMessageStorage(candleType, security, message.Arg, Drive, StorageFormat));
						else
							storages.Add(new InMemoryMarketDataStorage<CandleMessage>(security, message.Arg, history.GetMessages, candleType));
					}
					else
						RemoveStorage<IMarketDataStorage<CandleMessage>>(security, msgType, message.Arg);

					break;
				}

				default:
					error = new InvalidOperationException(LocalizedStrings.Str1118Params.Put(message.DataType));
					break;
			}

			RaiseMarketDataMessage(message, error);
		}

		private void RemoveStorage<T>(Security security, MessageTypes messageType, object arg)
			where T : class, IMarketDataStorage
		{
			var storage = BasketStorage
				.InnerStorages
				.OfType<T>()
				.FirstOrDefault(s => s.Security == security && s.Arg.Compare(arg) == 0);

			if (storage != null)
				BasketStorage.InnerStorages.Remove(storage);

			SendOutMessage(new ClearQueueMessage
			{
				ClearMessageType = messageType,
				SecurityId = security.ToSecurityId(),
				Arg = arg
			});
		}

		private void RaiseMarketDataMessage(MarketDataMessage message, Exception error)
		{
			var reply = (MarketDataMessage)message.Clone();
			reply.OriginalTransactionId = message.TransactionId;
			reply.Error = error;
			SendOutMessage(reply);
		}

		private void OnLoad()
		{
			try
			{
				var loadDate = StartDate;

				var messageTypes = new[] { MessageTypes.Time, ExtendedMessageTypes.Clearing };

				BasketStorage.InnerStorages.Add(new InMemoryMarketDataStorage<TimeMessage>(null, null, GetTimeLine));

				while (loadDate.Date <= StopDate.Date && !_disconnecting)
				{
					if (Boards.Any(b => b.IsTradeDate(loadDate, true)))
					{
						this.AddInfoLog("Loading {0} Events: {1}", loadDate.Date, LoadedMessageCount);

						using (var enumerator = BasketStorage.Load(loadDate.UtcDateTime.Date))
						{
							// storage for the specified date contains only time messages and clearing events
							var noData = !enumerator.DataTypes.Except(messageTypes).Any();

							if (noData)
								SendOutMessages(loadDate, GetSimpleTimeLine(loadDate).GetEnumerator());
							else
								SendOutMessages(loadDate, enumerator);	
						}
					}

					loadDate = loadDate.Date.AddDays(1).ApplyTimeZone(loadDate.Offset);
				}

				SendOutMessage(new LastMessage { LocalTime = StopDate.LocalDateTime });
			}
			catch (Exception ex)
			{
				SendOutError(ex);
				SendOutMessage(new LastMessage { IsError = true });
			}

			if (_disconnecting)
				SendOutMessage(new DisconnectMessage());

			_disconnecting = false;

			if (_loadingThread == null)
				SendOutMessage(new ResetMessage());

			_loadingThread = null;
			BasketStorage.InnerStorages.Clear();
		}

		/// <summary>
		/// Send outgoing message and raise <see cref="MessageAdapter.NewOutMessage"/> event.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendOutMessage(Message message)
		{
			LoadedMessageCount++;
			
			var serverTime = message.GetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			base.SendOutMessage(message);
		}

		private void SendOutMessages(DateTimeOffset loadDate, IEnumerator<Message> enumerator)
		{
			var checkFromTime = loadDate.Date == StartDate.Date && loadDate.TimeOfDay != TimeSpan.Zero;
			var checkToTime = loadDate.Date == StopDate.Date;

			while (enumerator.MoveNext() && !_disconnecting)
			{
				var msg = enumerator.Current;

				var serverTime = msg.GetServerTime();

				if (serverTime == null)
					throw new InvalidOperationException();

				msg.LocalTime = serverTime.Value.LocalDateTime;

				if (checkFromTime)
				{
					// пропускаем только стаканы, тики и ОЛ
					if (msg.Type == MessageTypes.QuoteChange || msg.Type == MessageTypes.Execution)
					{
						if (msg.LocalTime < StartDate)
							continue;

						checkFromTime = false;
					}
				}

				if (checkToTime)
				{
					if (msg.LocalTime > StopDate)
						break;
				}

				lock (_suspendLock)
				{
					if (_isSuspended)
						_suspendLock.Wait();	
				}

				SendOutMessage(msg);
			}
		}

		//private void EnqueueGenerators<TGenerator>(IEnumerable<KeyValuePair<SecurityId, TGenerator>> generators, MarketDataTypes type)
		//	where TGenerator : MarketDataGenerator
		//{
		//	foreach (var pair in generators)
		//	{
		//		SendOutMessage(new GeneratorMessage
		//		{
		//			SecurityId = pair.Key,
		//			Generator = pair.Value,
		//			IsSubscribe = true,
		//			TransactionId = TransactionIdGenerator.GetNextId(),
		//			DataType = type,
		//		});
		//	}
		//}

		private IEnumerable<Tuple<ExchangeBoard, Range<TimeSpan>>> GetOrderedRanges(DateTimeOffset date)
		{
			var orderedRanges = Boards
				.Where(b => b.IsTradeDate(date, true))
				.SelectMany(board =>
				{
					var period = board.WorkingTime.GetPeriod(date.ToLocalTime(board.TimeZone));

					return period == null || period.Times.Length == 0
						? new[] { Tuple.Create(board, new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)) }
						: period.Times.Select(t => Tuple.Create(board, ToUtc(board, t)));
				})
				.OrderBy(i => i.Item2.Min)
				.ToList();

			for (var i = 0; i < orderedRanges.Count - 1; )
			{
				if (orderedRanges[i].Item2.Contains(orderedRanges[i + 1].Item2))
				{
					orderedRanges.RemoveAt(i + 1);
				}
				else if (orderedRanges[i + 1].Item2.Contains(orderedRanges[i].Item2))
				{
					orderedRanges.RemoveAt(i);
				}
				else if (orderedRanges[i].Item2.Intersect(orderedRanges[i + 1].Item2) != null)
				{
					orderedRanges[i] = Tuple.Create(orderedRanges[i].Item1, new Range<TimeSpan>(orderedRanges[i].Item2.Min, orderedRanges[i + 1].Item2.Max));
					orderedRanges.RemoveAt(i + 1);
				}
				else
					i++;
			}

			return orderedRanges;
		}

		private static Range<TimeSpan> ToUtc(ExchangeBoard board, Range<TimeSpan> range)
		{
			var min = DateTime.MinValue + range.Min;
			var max = DateTime.MinValue + range.Max;

			var utcMin = min.To(board.TimeZone);
			var utcMax = max.To(board.TimeZone);

			return new Range<TimeSpan>(utcMin.TimeOfDay, utcMax.TimeOfDay);
		}

		private IEnumerable<TimeMessage> GetTimeLine(DateTimeOffset date)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				for (var time = range.Item2.Min; time <= range.Item2.Max; time += MarketTimeChangedInterval)
				{
					var serverTime = GetTime(date, time);

					if (serverTime.Date < date.Date)
						continue;

					lastTime = serverTime.TimeOfDay;
					yield return new TimeMessage { ServerTime = serverTime };
				}
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime))
			{
				yield return m;
			}
		}

		private IEnumerable<TimeMessage> GetSimpleTimeLine(DateTimeOffset date)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				var time = GetTime(date, range.Item2.Min);
				if (time.Date >= date.Date)
					yield return new TimeMessage { ServerTime = time };

				time = GetTime(date, range.Item2.Max);
				if (time.Date >= date.Date)
					yield return new TimeMessage { ServerTime = time };

				lastTime = range.Item2.Max;
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime))
			{
				yield return m;
			}
		}

		private static DateTimeOffset GetTime(DateTimeOffset date, TimeSpan timeOfDay)
		{
			return (date.Date + timeOfDay).ApplyTimeZone(date.Offset);
		}

		private IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTimeOffset date, TimeSpan lastTime)
		{
			for (var i = 0; i < PostTradeMarketTimeChangedCount; i++)
			{
				lastTime += MarketTimeChangedInterval;

				if (lastTime > TimeHelper.LessOneDay)
					break;

				yield return new TimeMessage
				{
					ServerTime = GetTime(date, lastTime)
				};
			}
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str1127Params.Put(StartDate, StopDate);
		}
	}
}