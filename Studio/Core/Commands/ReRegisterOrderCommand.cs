namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class ReRegisterOrderCommand : BaseStudioCommand
	{
		public ReRegisterOrderCommand(Order oldOrder, Order newOrder)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			OldOrder = oldOrder;
			NewOrder = newOrder;
		}

		public Order OldOrder { get; private set; }
		public Order NewOrder { get; private set; }
	}
}