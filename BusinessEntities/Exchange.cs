namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Exchange info.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[KnownType(typeof(TimeZoneInfo))]
	[KnownType(typeof(TimeZoneInfo.AdjustmentRule))]
	[KnownType(typeof(TimeZoneInfo.AdjustmentRule[]))]
	[KnownType(typeof(TimeZoneInfo.TransitionTime))]
	[KnownType(typeof(DayOfWeek))]
	public partial class Exchange : Equatable<Exchange>, IExtendableEntity, IPersistable, INotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Exchange"/>.
		/// </summary>
		public Exchange()
		{
			ExtensionInfo = new Dictionary<object, object>();
			RusName = EngName = string.Empty;
		}

		private string _name;

		/// <summary>
		/// Exchange code name.
		/// </summary>
		[DataMember]
		[Identity]
		public string Name
		{
			get { return _name; }
			set
			{
				if (Name == value)
					return;

				_name = value;
				Notify("Name");
			}
		}

		private string _rusName;

		/// <summary>
		/// Russian exchange name.
		/// </summary>
		[DataMember]
		public string RusName
		{
			get { return _rusName; }
			set
			{
				if (RusName == value)
					return;

				_rusName = value;
				Notify("RusName");
			}
		}

		private string _engName;

		/// <summary>
		/// English exchange name.
		/// </summary>
		[DataMember]
		public string EngName
		{
			get { return _engName; }
			set
			{
				if (EngName == value)
					return;

				_engName = value;
				Notify("EngName");
			}
		}

		private CountryCodes? _countryCode;

		/// <summary>
		/// ISO country code.
		/// </summary>
		[DataMember]
		[Nullable]
		public CountryCodes? CountryCode
		{
			get { return _countryCode; }
			set
			{
				if (CountryCode == value)
					return;

				_countryCode = value;
				Notify("CountryCode");
			}
		}

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended exchange info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the exchange is stored in the program. .
		/// </remarks>
		[XmlIgnore]
		[Browsable(false)]
		[DataMember]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_extensionInfo = value;
				Notify("ExtensionInfo");
			}
		}

		[OnDeserialized]
		private void AfterDeserialization(StreamingContext ctx)
		{
			if (ExtensionInfo == null)
				ExtensionInfo = new Dictionary<object, object>();
		}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		private void Notify(string info)
		{
			_propertyChanged.SafeInvoke(this, info);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Compare <see cref="Exchange"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(Exchange other)
		{
			return Name == other.Name;
		}

		/// <summary>
		/// Get the hash code of the object <see cref="Exchange"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		/// <summary>
		/// Create a copy of <see cref="Exchange"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Exchange Clone()
		{
			return new Exchange
			{
				Name = Name,
				RusName = RusName,
				EngName = EngName,
				CountryCode = CountryCode,
			};
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>("Name");
			RusName = storage.GetValue<string>("RusName");
			EngName = storage.GetValue<string>("EngName");
			CountryCode = storage.GetValue<CountryCodes?>("CountryCode");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Name", Name);
			storage.SetValue("RusName", RusName);
			storage.SetValue("EngName", EngName);
			storage.SetValue("CountryCode", CountryCode.To<string>());
		}
	}
}