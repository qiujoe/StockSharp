namespace StockSharp.Algo.Positions
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating position.
	/// </summary>
	public class PositionMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public PositionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private IPositionManager _positionManager = new PositionManager(true);

		/// <summary>
		/// The position manager.
		/// </summary>
		public IPositionManager PositionManager
		{
			get { return _positionManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_positionManager = value;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			PositionManager.ProcessMessage(message);

			base.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			var position = PositionManager.ProcessMessage(message);

			if (position != null)
				((ExecutionMessage)message).Position = position;

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="PositionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new PositionMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}