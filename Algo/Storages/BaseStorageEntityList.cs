namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The base class for representation in the form of list of trade objects, stored in database.
	/// </summary>
	/// <typeparam name="T">The type of the trading object (for example, <see cref="Security"/> or <see cref="MyTrade"/>).</typeparam>
	public abstract class BaseStorageEntityList<T> : HierarchicalPageLoadList<T>, IStorageEntityList<T>, ICollectionEx<T>
		where T : class
	{
		/// <summary>
		/// The object of synchronization.
		/// </summary>
		public SyncObject SyncRoot { get; } = new SyncObject();

		/// <summary>
		/// The time designating field.
		/// </summary>
		protected virtual Field TimeField => Schema.Fields["Time"];

		/// <summary>
		/// Initialize <see cref="BaseStorageEntityList{T}"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		protected BaseStorageEntityList(IStorage storage)
			: base(storage)
		{
		}

		/// <summary>
		/// To add the trading object to the collection.
		/// </summary>
		/// <param name="entity">The trading object.</param>
		public override void Add(T entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			base.Add(entity);
		}

		/// <summary>
		/// To delete the trading object from the collection.
		/// </summary>
		/// <param name="entity">The trading object.</param>
		/// <returns><see langword="true" />, if the element was deleted. Otherwise, <see langword="false" />.</returns>
		public override bool Remove(T entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			return base.Remove(entity);
		}

		/// <summary>
		/// To save the trading object.
		/// </summary>
		/// <param name="entity">The trading object.</param>
		public override void Save(T entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			lock (SyncRoot)
				base.Save(entity);
		}

		/// <summary>
		/// To load last created data.
		/// </summary>
		/// <param name="count">The amount of requested data.</param>
		/// <returns>The data range.</returns>
		public virtual IEnumerable<T> ReadLasts(int count)
		{
			return ReadLasts(count, TimeField);
		}

		/// <summary>
		/// It is called when adding element to the storage.
		/// </summary>
		/// <param name="entity">The trading object.</param>
		protected override void OnAdd(T entity)
		{
			lock (SyncRoot)
				base.OnAdd(entity);
		}

		/// <summary>
		/// It is called at deleting all elements in the storage.
		/// </summary>
		protected override void OnClear()
		{
			lock (SyncRoot)
				base.OnClear();
		}

		/// <summary>
		/// It is called at getting number of elements in the storage.
		/// </summary>
		/// <returns>The number of elements in the storage.</returns>
		protected override long OnGetCount()
		{
			lock (SyncRoot)
				return base.OnGetCount();
		}

		/// <summary>
		/// It is called at selection elements from the storage.
		/// </summary>
		/// <param name="startIndex">First element index.</param>
		/// <param name="count">The number of elements.</param>
		/// <param name="orderBy">The sorting condition.</param>
		/// <param name="direction">The sorting direction.</param>
		/// <returns>The set of elements.</returns>
		protected override IEnumerable<T> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			lock (SyncRoot)
				return base.OnGetGroup(startIndex, count, orderBy, direction);
		}

		/// <summary>
		/// It is called when deleting element from the storage.
		/// </summary>
		/// <param name="entity">Element.</param>
		protected override void OnRemove(T entity)
		{
			lock (SyncRoot)
				base.OnRemove(entity);
		}

		/// <summary>
		/// It is called at renewal element in the storage.
		/// </summary>
		/// <param name="entity">Element.</param>
		protected override void OnUpdate(T entity)
		{
			lock (SyncRoot)
				base.OnUpdate(entity);
		}

		/// <summary>
		/// To add items.
		/// </summary>
		/// <param name="items">New items.</param>
		public void AddRange(IEnumerable<T> items)
		{
			items.ForEach(Add);
		}

		/// <summary>
		/// To delete elements.
		/// </summary>
		/// <param name="items">Elements to be deleted.</param>
		/// <returns>Deleted elements.</returns>
		public void RemoveRange(IEnumerable<T> items)
		{
			items.ForEach(i => Remove(i));
		}

		/// <summary>
		/// To delete elements.
		/// </summary>
		/// <param name="index">The index, starting with which the elements have to be deleted.</param>
		/// <param name="count">The number of elements to be deleted.</param>
		/// <returns>The number of deleted elements.</returns>
		public int RemoveRange(int index, int count)
		{
			throw new NotSupportedException();
		}

		private Action<IEnumerable<T>> _addedRange;

		event Action<IEnumerable<T>> ICollectionEx<T>.AddedRange
		{
			add { _addedRange += value; }
			remove { _addedRange -= value; }
		}

		private Action<IEnumerable<T>> _removedRange;

		event Action<IEnumerable<T>> ICollectionEx<T>.RemovedRange
		{
			add { _removedRange += value; }
			remove { _removedRange -= value; }
		}
	}
}