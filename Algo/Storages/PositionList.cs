namespace StockSharp.Algo.Storages
{
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of positions, stored in external storage.
	/// </summary>
	public class PositionList : BaseStorageEntityList<Position>, IStoragePositionList
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public PositionList(IStorage storage)
			: base(storage)
		{
		}

		/// <summary>
		/// To get data from essence for creation.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <returns>Data for creation.</returns>
		protected override SerializationItemCollection GetOverridedAddSource(Position entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// To get data from essence for deletion.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <returns>Data for deletion.</returns>
		protected override SerializationItemCollection GetOverridedRemoveSource(Position entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// To load the position.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Position.</returns>
		public Position ReadBySecurityAndPortfolio(Security security, Portfolio portfolio)
		{
			return Read(CreateSource(security, portfolio));
		}

		/// <summary>
		/// To save the trading object.
		/// </summary>
		/// <param name="entity">The trading object.</param>
		public override void Save(Position entity)
		{
			if (ReadBySecurityAndPortfolio(entity.Security, entity.Portfolio) == null)
				Add(entity);
			else
				UpdateByKey(entity);
		}

		private void UpdateByKey(Position position)
		{
			var keyFields = new[]
			{
				Schema.Fields["Portfolio"],
				Schema.Fields["Security"]
			};
			var fields = Schema.Fields.Where(f => !keyFields.Contains(f)).ToArray();

			Database.Update(position, new FieldList(keyFields), new FieldList(fields));
		}

		private static SerializationItemCollection CreateSource(Position position)
		{
			return CreateSource(position.Security, position.Portfolio);
		}

		private static SerializationItemCollection CreateSource(Security security, Portfolio portfolio)
		{
			return new SerializationItemCollection
			{
				new SerializationItem<string>(new VoidField<string>("Security"), security.Id),
				new SerializationItem<string>(new VoidField<string>("Portfolio"), portfolio.Name)
			};
		}
	}
}