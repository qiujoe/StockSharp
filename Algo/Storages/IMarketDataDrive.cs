namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing the storage, associated with <see cref="IMarketDataStorage"/>.
	/// </summary>
	public interface IMarketDataStorageDrive
	{
		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		IMarketDataDrive Drive { get; }

		/// <summary>
		/// To get all the dates for which market data are recorded.
		/// </summary>
		IEnumerable<DateTime> Dates { get; }

		/// <summary>
		/// To delete cache-files, containing information on available time ranges.
		/// </summary>
		void ClearDatesCache();

		/// <summary>
		/// To remove market data on specified date from the storage.
		/// </summary>
		/// <param name="date">Date, for which all data shall be deleted.</param>
		void Delete(DateTime date);

		/// <summary>
		/// To save data in the format of StockSharp storage.
		/// </summary>
		/// <param name="date">The date, for which data shall be saved.</param>
		/// <param name="stream">Data in the format of StockSharp storage.</param>
		void SaveStream(DateTime date, Stream stream);

		/// <summary>
		/// To load data in the format of StockSharp storage.
		/// </summary>
		/// <param name="date">Date, for which data shall be loaded.</param>
		/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
		Stream LoadStream(DateTime date);
	}

	/// <summary>
	/// The interface, describing the storage (database, file etc.).
	/// </summary>
	public interface IMarketDataDrive : IPersistable, IDisposable
	{
		/// <summary>
		/// Path to market data.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The news storage.</returns>
		IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer);

		/// <summary>
		/// To get the storage for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>The storage for the instrument.</returns>
		ISecurityMarketDataDrive GetSecurityDrive(Security security);

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		IEnumerable<SecurityId> AvailableSecurities { get; }

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		IEnumerable<Tuple<Type, object>> GetAvailableDataTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// To get the storage for <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Storage for <see cref="IMarketDataStorage"/>.</returns>
		IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format);
	}

	/// <summary>
	/// The base implementation <see cref="IMarketDataDrive"/>.
	/// </summary>
	public abstract class BaseMarketDataDrive : Disposable, IMarketDataDrive
	{
		/// <summary>
		/// Initialize <see cref="BaseMarketDataDrive"/>.
		/// </summary>
		protected BaseMarketDataDrive()
		{
		}

		/// <summary>
		/// Path to market data.
		/// </summary>
		public abstract string Path { get; set; }

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The news storage.</returns>
		public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// To get the storage for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>The storage for the instrument.</returns>
		public ISecurityMarketDataDrive GetSecurityDrive(Security security)
		{
			return new SecurityMarketDataDrive(this, security);
		}

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		public abstract IEnumerable<SecurityId> AvailableSecurities { get; }

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		public abstract IEnumerable<Tuple<Type, object>> GetAvailableDataTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// Create storage for <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Storage for <see cref="IMarketDataStorage"/>.</returns>
		public abstract IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Path = storage.GetValue<string>("Path");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Path", Path);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Path;
		}
	}
}