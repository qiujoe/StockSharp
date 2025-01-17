﻿namespace StockSharp.Studio.Core
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	public abstract class VirtualPortfolio : Portfolio, IPersistable
	{
		public abstract IEnumerable<Order> SplitOrder(Order order);

		public virtual void Load(SettingsStorage storage)
		{
		}

		public virtual void Save(SettingsStorage storage)
		{
		}
	}

	public abstract class WeighedVirtualPortfolio : VirtualPortfolio
	{
		private readonly Dictionary<Portfolio, decimal> _innerPortfolios = new Dictionary<Portfolio, decimal>();

		public Dictionary<Portfolio, decimal> InnerPortfolios
		{
			get { return _innerPortfolios; }
		}

		public override void Load(SettingsStorage storage)
		{
			var values = storage.GetValue<KeyValuePair<string, decimal>[]>("InnerPortfolios");
			if (values != null)
			{
				var pairs = values
					.Select(v => new KeyValuePair<Portfolio, decimal>(ConfigManager.GetService<IConnector>().Portfolios.FirstOrDefault(p => p.Name == v.Key), v.Value))
					.ToArray();

				InnerPortfolios.AddRange(pairs);
			}
		}

		public override void Save(SettingsStorage storage)
		{
			var values = InnerPortfolios
				.Select(p => new KeyValuePair<string, decimal>(p.Key.Name, p.Value))
				.ToArray();

			storage.SetValue("InnerPortfolios", values);
		}
	}
}
