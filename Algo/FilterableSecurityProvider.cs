namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Provider of information about instruments supporting search using <see cref="SecurityTrie"/>.
	/// </summary>
	public class FilterableSecurityProvider : Disposable, ISecurityProvider
	{
		private readonly SecurityTrie _trie = new SecurityTrie();

		private readonly ISecurityProvider _provider;
		private readonly bool _ownProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="provider">Security meta info provider.</param>
		/// <param name="ownProvider"><see langword="true"/> to leave the <paramref name="provider"/> open after the <see cref="FilterableSecurityProvider"/> object is disposed; otherwise, <see langword="false"/>.</param>
		///// <param name="excludeFilter">Filter for instruments exclusion.</param>
		public FilterableSecurityProvider(ISecurityProvider provider, bool ownProvider = false/*, Func<Security, bool> excludeFilter = null*/)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			_provider = provider;
			_ownProvider = ownProvider;

			//ExcludeFilter = excludeFilter;

			_provider.Added += AddSecurities;
			_provider.Removed += RemoveSecurities;
			_provider.Cleared += ClearSecurities;

			AddSecurities(_provider.LookupAll());
		}

		/// <summary>
		/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
		/// </summary>
		public int Count => _trie.Count;

		/// <summary>
		/// New instruments added.
		/// </summary>
		public event Action<IEnumerable<Security>> Added;

		/// <summary>
		/// Instruments removed.
		/// </summary>
		public event Action<IEnumerable<Security>> Removed;

		/// <summary>
		/// The storage was cleared.
		/// </summary>
		public event Action Cleared;

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var filter = criteria.Id.IsEmpty()
				? (criteria.IsLookupAll() ? string.Empty : criteria.Code.ToLowerInvariant())
				: criteria.Id.ToLowerInvariant();

			var securities = _trie.Retrieve(filter);

			if (!criteria.Id.IsEmpty())
				securities = securities.Where(s => s.Id.CompareIgnoreCase(criteria.Id));

			return securities;
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		private void AddSecurities(IEnumerable<Security> securities)
		{
			securities.ForEach(_trie.Add);
            Added.SafeInvoke(securities);
		}

		private void RemoveSecurities(IEnumerable<Security> securities)
		{
			_trie.RemoveRange(securities);
            Removed.SafeInvoke(securities);
		}

		private void ClearSecurities()
		{
			_trie.Clear();
			Cleared.SafeInvoke();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_provider.Added -= AddSecurities;
			_provider.Removed -= RemoveSecurities;
			_provider.Cleared -= ClearSecurities;

			if (_ownProvider)
				_provider.Dispose();

			base.DisposeManaged();
		}
	}
}
