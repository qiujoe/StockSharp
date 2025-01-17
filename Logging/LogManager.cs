namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Localization;

	/// <summary>
	/// Messages logging manager that monitors the <see cref="ILogSource.Log"/> event and forwards messages to the <see cref="LogManager.Listeners"/>.
	/// </summary>
	public class LogManager : Disposable, IPersistable
	{
		private static readonly MemoryStatisticsValue<LogMessage> _logMsgStat = new MemoryStatisticsValue<LogMessage>(LocalizedStrings.MessageLog);

		static LogManager()
		{
			MemoryStatistics.Instance.Values.Add(_logMsgStat);
		}

		private sealed class ApplicationReceiver : BaseLogReceiver
		{
			public ApplicationReceiver()
			{
				Name = TypeHelper.ApplicationName;
				LogLevel = LogLevels.Info;
			}
		}

		private sealed class LogSourceList : BaseList<ILogSource>
		{
			private readonly LogManager _parent;

			public LogSourceList(LogManager parent)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
			}

			protected override bool OnAdding(ILogSource item)
			{
				item.Log += _parent.SourceLog;
				return base.OnAdding(item);
			}

			protected override bool OnRemoving(ILogSource item)
			{
				item.Log -= _parent.SourceLog;
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in this)
					OnRemoving(item);

				return base.OnClearing();
			}
		}

		private static readonly LogMessage _disposeMessage = new LogMessage(new ApplicationReceiver(), DateTimeOffset.MinValue, LogLevels.Off, string.Empty) { IsDispose = true };

		private readonly object _syncRoot = new object();
		private readonly List<LogMessage> _pendingMessages = new List<LogMessage>();
		private readonly Timer _flushTimer;
		private bool _isFlusing;

		/// <summary>
		/// Initializes a new instance of the <see cref="LogManager"/>.
		/// </summary>
		public LogManager()
		{
			ConfigManager.TryRegisterService(this);

			Sources = new LogSourceList(this)
			{
				Application,
				new UnhandledExceptionSource()
			};

			_flushTimer = ThreadingHelper.Timer(Flush);

			FlushInterval = TimeSpan.FromMilliseconds(500);
		}

		private void Flush()
		{
			LogMessage[] temp;

			lock (_syncRoot)
			{
				if (_isFlusing)
					return;

				temp = _pendingMessages.CopyAndClear();

				if (temp.Length == 0)
					return;

				_isFlusing = true;
			}

			_logMsgStat.Remove(temp);

			try
			{
				var messages = new List<LogMessage>();

				ILogSource prevSource = null;
				var level = default(LogLevels);

				foreach (var message in temp)
				{
					if (prevSource == null || prevSource != message.Source)
					{
						prevSource = message.Source;
						level = prevSource.GetLogLevel();
					}

					if (level == LogLevels.Inherit)
						level = Application.LogLevel;

					if (level <= message.Level)
						messages.Add(message);
				}

				if (messages.Count > 0)
					_listeners.Cache.ForEach(l => l.WriteMessages(messages));
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			finally
			{
				lock (_syncRoot)
					_isFlusing = false;
			}
		}

		private ILogReceiver _application = new ApplicationReceiver();

		/// <summary>
		/// The all application level logs recipient .
		/// </summary>
		public ILogReceiver Application
		{
			get { return _application; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value == _application)
					return;

				Sources.Remove(_application);
				_application = value;
				Sources.Add(_application);
			}
		}

		private readonly CachedSynchronizedSet<ILogListener> _listeners = new CachedSynchronizedSet<ILogListener>();

		/// <summary>
		/// Messages loggers arriving from <see cref="LogManager.Sources"/>.
		/// </summary>
		public IList<ILogListener> Listeners => _listeners;

		/// <summary>
		/// Logs sources which are listened to the event <see cref="ILogSource.Log"/>.
		/// </summary>
		public IList<ILogSource> Sources { get; }

		/// <summary>
		/// Sending interval of messages collected from <see cref="LogManager.Sources"/> to the <see cref="LogManager.Listeners"/>. The default is 500 ms.
		/// </summary>
		public TimeSpan FlushInterval
		{
			get { return _flushTimer.Interval(); }
			set
			{
				if (value < TimeSpan.FromMilliseconds(1))
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.IntervalMustBePositive);

				_flushTimer.Interval(value);
			}
		}

		//private int _maxMessageCount = 1000;

		///// <summary>
		///// Максимальное количество накопленных от <see cref="Sources"/> сообщений, прежде чем они будут отправлены в <see cref="Listeners"/>.
		///// По умолчанию равно 1000.
		///// </summary>
		///// <remarks>Значение 0 означает бесконечный размер буфера. Значение -1 означает отсутствие буферизации.</remarks>
		//public int MaxMessageCount
		//{
		//	get { return _maxMessageCount; }
		//	set
		//	{
		//		if (value < -1) 
		//			throw new ArgumentOutOfRangeException("value", value, "Количество не может быть отрицательным.");

		//		_maxMessageCount = value;
		//	}
		//}

		private void SourceLog(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			_logMsgStat.Add(message);

			//bool needFlush;

			lock (_syncRoot)
			{
				_pendingMessages.Add(message);
				//needFlush = MaxMessageCount > 0 && _pendingMessages.Count > MaxMessageCount || MaxMessageCount == -1;

				// mika: если накопилось слишком много сообщений, то нужно принудительно вызвать таймер
				if (_pendingMessages.Count > 1000000)
					ImmediateFlush();
			}

			// mika: сбрасывание логов необходимо делать только в одном потоке, чтобы избежать
			// усложнения логики в Listener-ах

			//if (needFlush)
			//	Flush();
		}

		private void ImmediateFlush()
		{
			_flushTimer.Change(TimeSpan.Zero, FlushInterval);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			// сначала удаляем поставщиков логов
			Sources.Clear();

			lock (_syncRoot)
			{
				_pendingMessages.Clear();
				_pendingMessages.Add(_disposeMessage);
			}

			// сбрасываем в логи то, что еще не сбросилось и выключаем таймер
			ImmediateFlush();
			_flushTimer.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			FlushInterval = storage.GetValue<TimeSpan>("FlushInterval");
			//MaxMessageCount = storage.GetValue<int>("MaxMessageCount");
			Listeners.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>("Listeners").Select(s => s.LoadEntire<ILogListener>()));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("FlushInterval", FlushInterval);
			//storage.SetValue("MaxMessageCount", MaxMessageCount);
			storage.SetValue("Listeners", Listeners.Select(l => l.SaveEntire(false)).ToArray());
		}
	}
}