namespace StockSharp.Studio.Controls
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	public partial class PositionEditWindow
	{
		public PositionEditWindow()
		{
			InitializeComponent();
		}

		private BasePosition _position;

		public BasePosition Position
		{
			get { return _position; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_position = value;
				PropertyGrid.SelectedObject = value;

				var pf = value as Portfolio;

				if (pf != null)
					Title = pf.Name.IsEmpty() ? LocalizedStrings.Str3246 : LocalizedStrings.Str3247;
				else
				{
					var position = value as Position;

					if (position != null)
					{
						Title = position.Security == null ? LocalizedStrings.Str3248 : LocalizedStrings.Str3249;
					}
				}

				IsNew = Title.Contains(LocalizedStrings.Str3250);
			}
		}

		public bool IsNew { get; private set; }

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			var portfolio = Position as Portfolio;

			if (portfolio != null)
			{
				if (portfolio.Name.IsEmptyOrWhiteSpace())
				{
					new MessageBoxBuilder()
						.Owner(this)
						.Warning()
						.Text(LocalizedStrings.Str3251)
						.Show();

					return;	
				}
			}
			else
			{
				var position = Position as Position;

				if (position != null)
				{
					if (position.Security == null)
					{
						new MessageBoxBuilder()
							.Owner(this)
							.Warning()
							.Text(LocalizedStrings.Str3252)
							.Show();

						return;	
					}

					if (position.Portfolio == null)
					{
						new MessageBoxBuilder()
							.Owner(this)
							.Warning()
							.Text(LocalizedStrings.Str3253)
							.Show();

						return;
					}
				}
			}

			//if (Portfolio.Board == null)
			//{
			//	new MessageBoxBuilder()
			//		.Owner(this)
			//		.Warning()
			//		.Text("Не указана торговая площадка.")
			//		.Show();

			//	return;
			//}

			DialogResult = true;
		}
	}
}