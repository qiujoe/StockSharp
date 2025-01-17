namespace StockSharp.Algo.Commissions
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// The commission calculating manager.
	/// </summary>
	public class CommissionManager : ICommissionManager
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionManager"/>.
		/// </summary>
		public CommissionManager()
		{
		}

		private readonly CachedSynchronizedSet<ICommissionRule> _rules = new CachedSynchronizedSet<ICommissionRule>();

		/// <summary>
		/// The list of commission calculating rules.
		/// </summary>
		public ISynchronizedCollection<ICommissionRule> Rules => _rules;

		/// <summary>
		/// Total commission.
		/// </summary>
		public virtual decimal Commission { get; private set; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		public virtual void Reset()
		{
			Commission = 0;
			_rules.Cache.ForEach(r => r.Reset());
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		public virtual decimal? Process(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					return null;
				}
				case MessageTypes.Execution:
				{
					if (_rules.Count == 0)
						return null;

					var commission = _rules.Cache.Sum(rule => rule.Process(message));

					if (commission != null)
						Commission += commission.Value;

					return commission;
				}
				default:
					return null;
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Load(SettingsStorage storage)
		{
			Rules.AddRange(storage.GetValue<SettingsStorage[]>("Rules").Select(s => s.LoadEntire<ICommissionRule>()));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Rules", Rules.Select(r => r.SaveEntire(false)).ToArray());
		}

		string ICommissionRule.Title
		{
			get { throw new NotSupportedException(); }
		}

		Unit ICommissionRule.Value
		{
			get { throw new NotSupportedException(); }
		}
	}
}