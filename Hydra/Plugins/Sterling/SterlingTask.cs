namespace StockSharp.Hydra.Sterling
{
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Sterling;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/f43591c9-c215-4dee-b297-4b9ed34fd465.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class SterlingTask : ConnectorHydraTask<SterlingMessageAdapter>
	{
		private const string _sourceName = "Sterling";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class SterlingSettings : ConnectorHydraTaskSettings
		{
			public SterlingSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}
		}

		public SterlingTask()
		{
		}

		private SterlingSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new SterlingSettings(settings);

			if (settings.IsDefault)
			{
			}
		}

		protected override SterlingMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new SterlingMessageAdapter(generator);
		}
	}
}