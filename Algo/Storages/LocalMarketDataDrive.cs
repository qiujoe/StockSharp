namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using IOPath = System.IO.Path;

	using MoreLinq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The file storage for market data.
	/// </summary>
	public class LocalMarketDataDrive : BaseMarketDataDrive
	{
		private sealed class LocalMarketDataStorageDrive : IMarketDataStorageDrive
		{
			private readonly string _path;
			private readonly string _fileNameWithExtension;
			private readonly string _datesPath;

			private readonly SyncObject _cacheSync = new SyncObject();

			//private static readonly Version _dateVersion = new Version(1, 0);
			private const string _dateFormat = "yyyy_MM_dd";

			public LocalMarketDataStorageDrive(string fileName, string path, StorageFormats format, IMarketDataDrive drive)
			{
				if (drive == null)
					throw new ArgumentNullException(nameof(drive));

				if (fileName.IsEmpty())
					throw new ArgumentNullException(nameof(fileName));

				if (path.IsEmpty())
					throw new ArgumentNullException(nameof(path));

				_path = path;
				_drive = drive;
				_fileNameWithExtension = fileName + GetExtension(format);
				_datesPath = IOPath.Combine(_path, fileName + format + "Dates.txt");

				_datesDict = new Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>>(() =>
				{
					var retVal = new CachedSynchronizedOrderedDictionary<DateTime, DateTime>();

					if (File.Exists(_datesPath))
					{
						foreach (var date in LoadDates())
							retVal.Add(date, date);
					}
					else
					{
						var dates = InteropHelper
							.GetDirectories(_path)
							.Where(dir => File.Exists(IOPath.Combine(dir, _fileNameWithExtension)))
							.Select(dir => IOPath.GetFileName(dir).ToDateTime(_dateFormat).ChangeKind(DateTimeKind.Utc));

						foreach (var date in dates)
							retVal.Add(date, date);

						SaveDates(retVal.CachedValues);
					}

					return retVal;
				}).Track();
			}

			private readonly IMarketDataDrive _drive;

			IMarketDataDrive IMarketDataStorageDrive.Drive => _drive;

			public IEnumerable<DateTime> Dates => DatesDict.CachedValues;

			private readonly Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>> _datesDict;

			private CachedSynchronizedOrderedDictionary<DateTime, DateTime> DatesDict => _datesDict.Value;

			public void ClearDatesCache()
			{
				if (Directory.Exists(_path))
				{
					lock (_cacheSync)
						File.Delete(_datesPath);
				}

				ResetCache();
			}

			void IMarketDataStorageDrive.Delete(DateTime date)
			{
				date = date.ChangeKind(DateTimeKind.Utc);

				var path = GetPath(date, true);

				if (File.Exists(path))
				{
					File.Delete(path);

					var dir = GetDirectoryName(path);

					if (Directory.EnumerateFiles(dir).IsEmpty())
					{
						lock (_cacheSync)
							InteropHelper.BlockDeleteDir(dir);
					}
				}

				var dates = DatesDict;

				dates.Remove(date);

				SaveDates(Dates.ToArray());
			}

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			{
				date = date.ChangeKind(DateTimeKind.Utc);

				Directory.CreateDirectory(GetDataPath(date));

				using (var file = File.OpenWrite(GetPath(date, false)))
					stream.CopyTo(file);

				var dates = DatesDict;

				dates[date] = date;

				SaveDates(Dates.ToArray());
			}

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
			{
				var path = GetPath(date.ChangeKind(DateTimeKind.Utc), true);

				return File.Exists(path)
					? File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
					: Stream.Null;
			}

			private IEnumerable<DateTime> LoadDates()
			{
				try
				{
					return CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						using (var reader = new StreamReader(new FileStream(_datesPath, FileMode.Open, FileAccess.Read)))
						{
							//var version = new Version(file.ReadByte(), file.ReadByte());

							//if (version > _dateVersion)
							//	throw new InvalidOperationException(LocalizedStrings.Str1002Params.Put(_datesPath, version, _dateVersion));

							//var count = file.Read<int>();

							var dates = new List<DateTime>();

							while (true)
							{
								var line = reader.ReadLine();

								if (line == null)
									break;

								dates.Add(line.ToDateTime(_dateFormat).ChangeKind(DateTimeKind.Utc));
							}

							//for (var i = 0; i < count; i++)
							//{
							//	dates[i] = file.Read<DateTime>().ChangeKind(DateTimeKind.Utc);
							//}

							return dates;
						}
					});
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(LocalizedStrings.Str1003Params.Put(_datesPath), ex);
				}
			}

			private void SaveDates(DateTime[] dates)
			{
				try
				{
					if (!Directory.Exists(_path))
					{
						if (dates.IsEmpty())
							return;

						Directory.CreateDirectory(_path);
					}

					var stream = new MemoryStream();

					//stream.WriteByte((byte)_dateVersion.Major);
					//stream.WriteByte((byte)_dateVersion.Minor);
					//stream.Write(dates.Length);

					CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						var writer = new StreamWriter(stream) { AutoFlush = true };

						foreach (var date in dates)
						{
							writer.WriteLine(date.ToString(_dateFormat));
							//stream.Write(date);
						}
					});
					
					lock (_cacheSync)
					{
						stream.Position = 0;
						stream.Save(_datesPath);
					}
				}
				catch (UnauthorizedAccessException)
				{
					// если папка с данными с правами только на чтение
				}
			}

			private string GetDataPath(DateTime date)
			{
				return IOPath.Combine(_path, date.ToString(_dateFormat));
			}

			private string GetPath(DateTime date, bool isLoad)
			{
				var result = IOPath.Combine(GetDataPath(date), _fileNameWithExtension);

				System.Diagnostics.Trace.WriteLine("FileAccess ({0}): {1}".Put(isLoad ? "Load" : "Save", result));
				return result;
			}

			private static string GetDirectoryName(string path)
			{
				var name = IOPath.GetDirectoryName(path);

				if (name == null)
					throw new ArgumentException(LocalizedStrings.Str1004Params.Put(path));

				return name;
			}

			public void ResetCache()
			{
				_datesDict.Reset();
			}
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, Type, object, StorageFormats>, LocalMarketDataStorageDrive> _drives = new SynchronizedDictionary<Tuple<SecurityId, Type, object, StorageFormats>, LocalMarketDataStorageDrive>();

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalMarketDataDrive"/>.
		/// </summary>
		public LocalMarketDataDrive()
			: this(Directory.GetCurrentDirectory())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalMarketDataDrive"/>.
		/// </summary>
		/// <param name="path">The path to the directory with data.</param>
		public LocalMarketDataDrive(string path)
		{
			_path = path;
		}

		private string _path;

		/// <summary>
		/// The path to the directory with data.
		/// </summary>
		public override string Path
		{
			get { return _path; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				if (Path == value)
					return;

				_path = value;
				ResetDrives();
			}
		}

		private bool _useAlphabeticPath = true;

		/// <summary>
		/// Whether to use the alphabetical path to data. The default is enabled.
		/// </summary>
		[Obsolete]
		public bool UseAlphabeticPath
		{
			get { return _useAlphabeticPath; }
			set
			{
				if (value == UseAlphabeticPath)
					return;

				_useAlphabeticPath = value;
				ResetDrives();
			}
		}

		private void ResetDrives()
		{
			lock (_drives.SyncRoot)
				_drives.Values.ForEach(d => d.ResetCache());
		}

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		public override IEnumerable<SecurityId> AvailableSecurities
		{
			get
			{
				var idGenerator = new SecurityIdGenerator();

				if (!Directory.Exists(Path))
					return Enumerable.Empty<SecurityId>();

				return Directory
					.EnumerateDirectories(Path)
					.SelectMany(Directory.EnumerateDirectories)
					.Select(System.IO.Path.GetFileName)
					.Select(n => idGenerator.Split(n, true))
					.Where(t => !t.IsDefault());
			}
		}

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		public override IEnumerable<Tuple<Type, object>> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
			var secPath = GetSecurityPath(securityId);

			if (!Directory.Exists(secPath))
				return Enumerable.Empty<Tuple<Type, object>>();

			var ext = GetExtension(format);

			return InteropHelper
				.GetDirectories(secPath)
				.SelectMany(dir => Directory.GetFiles(dir, "candles_*" + ext).Select(IOPath.GetFileNameWithoutExtension))
				.Distinct()
				.Select(GetDataType)
				.Where(t => t != null);
		}

		/// <summary>
		/// Create storage for <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Storage for <see cref="IMarketDataStorage"/>.</returns>
		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
		{
			if (securityId.IsDefault())
				throw new ArgumentNullException(nameof(securityId));

			return _drives.SafeAdd(Tuple.Create(securityId, dataType, arg, format),
				key => new LocalMarketDataStorageDrive(GetFileName(dataType, arg), GetSecurityPath(securityId), format, this));
		}

		/// <summary>
		/// To get the file extension for the format.
		/// </summary>
		/// <param name="format">Format.</param>
		/// <returns>The extension.</returns>
		public static string GetExtension(StorageFormats format)
		{
			switch (format)
			{
				case StorageFormats.Binary:
					return ".bin";
				case StorageFormats.Csv:
					return ".csv";
				default:
					throw new ArgumentOutOfRangeException(nameof(format));
			}
		}

		private static readonly SynchronizedPairSet<Tuple<Type, object>, string> _fileNames = new SynchronizedPairSet<Tuple<Type, object>, string>
		{
			{ Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Tick), "trades" },
			{ Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.OrderLog), "orderLog" },
			{ Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Order), "transactions" },
			//{ Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Trade), "transactions" },
			{ Tuple.Create(typeof(QuoteChangeMessage), (object)null), "quotes" },
			{ Tuple.Create(typeof(Level1ChangeMessage), (object)null), "security" },
			{ Tuple.Create(typeof(NewsMessage), (object)null), "news" },
		};

		/// <summary>
		/// Get data type and parameter for the specified file name.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <returns>Data type and parameter associated with the type. For example, <see cref="CandleMessage.Arg"/>.</returns>
		public static Tuple<Type, object> GetDataType(string fileName)
		{
			var info = _fileNames.TryGetKey(fileName);

			if (info != null)
				return info;

			if (!fileName.ContainsIgnoreCase("Candle"))
				return null;

			var parts = fileName.Split('_');
			var type = "{0}.{1}Message, {2}".Put(typeof(CandleMessage).Namespace, parts[1], typeof(CandleMessage).Assembly.FullName).To<Type>();
			var arg = type.ToCandleArg(parts[2]);

			return Tuple.Create(type, arg);
		}

		/// <summary>
		/// To get the file name by the type of data.
		/// </summary>
		/// <param name="dataType">Data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <returns>The file name.</returns>
		public static string GetFileName(Type dataType, object arg)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (dataType.IsSubclassOf(typeof(CandleMessage)))
				return "candles_{0}_{1}".Put(dataType.Name.Replace("Message", string.Empty), TraderHelper.CandleArgToFolderName(arg));
			else
			{
				if (arg != null && arg.Equals(ExecutionTypes.Trade))
					arg = ExecutionTypes.Order;

				var fileName = _fileNames.TryGetValue(Tuple.Create(dataType, arg));

				if (fileName == null)
					throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.FullName));

				return fileName;
			}
		}

#pragma warning disable 612
		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			UseAlphabeticPath = storage.GetValue<bool>("UseAlphabeticPath");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("UseAlphabeticPath", UseAlphabeticPath);
		}

		/// <summary>
		/// To get the path to the folder with market data for the instrument.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>The path to the folder with market data.</returns>
		public string GetSecurityPath(SecurityId securityId)
		{
			if (securityId.IsDefault())
				throw new ArgumentNullException(nameof(securityId));

			var id = securityId.ToStringId();

			var folderName = id.SecurityIdToFolderName();

			return UseAlphabeticPath
				? IOPath.Combine(Path, id.Substring(0, 1), folderName)
				: IOPath.Combine(Path, folderName);
		}
#pragma warning restore 612
	}
}