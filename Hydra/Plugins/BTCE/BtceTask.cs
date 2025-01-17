namespace StockSharp.Hydra.Btce
{
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Hydra.Core;
	using StockSharp.Btce;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/4e435654-38af-4ede-9987-e268a6ae3b96.htm")]
	[TaskCategory(TaskCategories.Crypto | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.Ticks | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Transactions)]
	class BtceTask : ConnectorHydraTask<BtceMessageAdapter>
	{
		private const string _sourceName = "BTCE";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BtceSettings : ConnectorHydraTaskSettings
		{
			public BtceSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("Key", new SecureString());
				ExtensionInfo.TryAdd("Secret", new SecureString());
			}

			/// <summary>
			/// ����.
			/// </summary>
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3304Key)]
			[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
			[PropertyOrder(1)]
			public SecureString Key
			{
				get { return (SecureString)ExtensionInfo["Key"]; }
				set { ExtensionInfo["Key"] = value; }
			}

			/// <summary>
			/// ������.
			/// </summary>
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3306Key)]
			[DescriptionLoc(LocalizedStrings.Str3307Key)]
			[PropertyOrder(2)]
			public SecureString Secret
			{
				get { return (SecureString)ExtensionInfo["Secret"]; }
				set { ExtensionInfo["Secret"] = value; }
			}
		}

		private BtceSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new BtceSettings(settings);

			if (!_settings.IsDefault)
				return;

			_settings.Key = new SecureString();
			_settings.Secret = new SecureString();
		}

		protected override BtceMessageAdapter GetAdapter(IdGenerator generator)
		{
			var adapter = new BtceMessageAdapter(generator)
			{
				Key = _settings.Key,
				Secret = _settings.Secret,
			};

			if (adapter.Key.IsEmpty())
				adapter.RemoveTransactionalSupport();

			return adapter;
		}
	}
}