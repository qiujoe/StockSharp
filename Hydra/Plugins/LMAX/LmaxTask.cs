namespace StockSharp.Hydra.LMAX
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.LMAX;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/0b962432-d81d-4646-a818-9fa7093cbe4f.htm")]
	[TaskCategory(TaskCategories.Forex | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.History | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class LmaxTask : ConnectorHydraTask<LmaxMessageAdapter>
	{
		private const string _sourceName = "LMAX";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class LmaxSettings : ConnectorHydraTaskSettings
		{
			public LmaxSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.DemoKey)]
			[DescriptionLoc(LocalizedStrings.Str3388Key)]
			[PropertyOrder(2)]
			public bool IsDemo
			{
				get { return ExtensionInfo["IsDemo"].To<bool>(); }
				set { ExtensionInfo["IsDemo"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3710Key)]
			[DescriptionLoc(LocalizedStrings.Str3711Key)]
			[PropertyOrder(3)]
			public bool IsDownloadSecurityFromSite
			{
				get { return ExtensionInfo["IsDownloadSecurityFromSite"].To<bool>(); }
				set { ExtensionInfo["IsDownloadSecurityFromSite"] = value; }
			}
		}

		public LmaxTask()
		{
			_supportedCandleSeries = LmaxMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private LmaxSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return new[] { typeof(Candle), typeof(QuoteChangeMessage), typeof(Level1ChangeMessage), typeof(ExecutionMessage) }; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new LmaxSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsDemo = false;
			_settings.IsDownloadSecurityFromSite = false;
		}

		protected override LmaxMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new LmaxMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password,
				IsDemo = _settings.IsDemo,
				IsDownloadSecurityFromSite = _settings.IsDownloadSecurityFromSite
			};
		}
	}
}