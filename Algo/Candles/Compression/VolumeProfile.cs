namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Volume profile.
	/// </summary>
	public class VolumeProfile
	{
		private readonly Dictionary<decimal, CandlePriceLevel> _volumeProfileInfo = new Dictionary<decimal, CandlePriceLevel>();

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeProfile"/>.
		/// </summary>
		public VolumeProfile()
			: this(new List<CandlePriceLevel>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="priceLevels">Price levels.</param>
		public VolumeProfile(IList<CandlePriceLevel> priceLevels)
		{
			if (priceLevels == null)
				throw new ArgumentNullException(nameof(priceLevels));

			PriceLevels = priceLevels;
		}

		/// <summary>
		/// The upper price level.
		/// </summary>
		public CandlePriceLevel High { get; private set; }

		/// <summary>
		/// The lower price level.
		/// </summary>
		public CandlePriceLevel Low { get; private set; }

		/// <summary>
		/// Point of control.
		/// </summary>
		public CandlePriceLevel PoC { get; private set; }

		private decimal _volumePercent = 70;

		/// <summary>
		/// The percentage of total volume (the default is 70%).
		/// </summary>
		public decimal VolumePercent
		{
			get { return _volumePercent; }
			set
			{
				if (value < 0 || value > 100)
					throw new ArgumentOutOfRangeException(nameof(value));

				_volumePercent = value;
			}
		}

		/// <summary>
		/// Price levels.
		/// </summary>
		public IEnumerable<CandlePriceLevel> PriceLevels { get; }

		/// <summary>
		/// To update the profile with new value.
		/// </summary>
		/// <param name="value">Value.</param>
		public void Update(ICandleBuilderSourceValue value)
		{
			if (value.OrderDirection == null)
				return;

			UpdatePriceLevel(GetPriceLevel(value.Price), value);
		}

		/// <summary>
		/// To update the profile with new value.
		/// </summary>
		/// <param name="priceLevel">Value.</param>
		public void Update(CandlePriceLevel priceLevel)
		{
			var level = GetPriceLevel(priceLevel.Price);

			level.BuyVolume += priceLevel.BuyVolume;
			level.BuyCount += priceLevel.BuyCount;
			level.SellVolume += priceLevel.SellVolume;
			level.SellCount += priceLevel.SellCount;

			if (priceLevel.BuyVolumes != null)
				((List<decimal>)level.BuyVolumes).AddRange(priceLevel.BuyVolumes);

			if (priceLevel.SellVolumes != null)
				((List<decimal>)level.SellVolumes).AddRange(priceLevel.SellVolumes);
		}

		private CandlePriceLevel GetPriceLevel(decimal price)
		{
			return _volumeProfileInfo.SafeAdd(price, key =>
			{
				var level = new CandlePriceLevel
				{
					Price = key,
					BuyVolumes = new List<decimal>(),
					SellVolumes = new List<decimal>()
				};

				((IList<CandlePriceLevel>)PriceLevels).Add(level);

				return level;
			});
		}

		private void UpdatePriceLevel(CandlePriceLevel level, ICandleBuilderSourceValue value)
		{
			if (level == null)
				throw new ArgumentNullException(nameof(level));

			var side = value.OrderDirection;

			if (side == null)
				throw new ArgumentException(nameof(value));

			if (side == Sides.Buy)
			{
				level.BuyVolume += value.Volume;
				level.BuyCount++;

				((List<decimal>)level.BuyVolumes).Add(value.Volume);
			}
			else
			{
				level.SellVolume += value.Volume;
				level.SellCount++;

				((List<decimal>)level.SellVolumes).Add(value.Volume);
			}
		}

		/// <summary>
		/// To calculate the value area.
		/// </summary>
		public void Calculate()
		{
			// �������� ����:
			// ���� POC Vol �� ���� ���� � ���� ������� �� ��� ��������(������)
			// ����������� � ������������, �� ��� � ����� ������, ������������ � ����� �����, � ������� ���������� ����� POC Vol.
			// �� ��������� �������� ������� ��������� ��� ������ ����������� � ������������, � ����� ������� ����� ������� � ����� �����
			// � ��� �� ��� ��� ���� ����� ����� �� �������� �����, ������� ��������������� � ���������� ��������� � ����� ������.
			// ����� ���������� ������, ����� ������� � ����� ������ �����, �� ������� ����������� ����� ����� ����� VAH � VAL.
			// ��������� ������:
			// ���� POC Vol ��������� �� ������� �������� ���������, �� ������/����� ����� ������, �� "�����" ������� ������ � ���� �������.
			// ���� POC Vol ��������� �� ���� ��� ����/���� �������� ���������, �� ������/����� ����� ����� ������ ���� �������� ��� ��������� � ����� ������� ����������.
			// ������������ � ������� ��������� ����� ���� ��������� POC Vol, ���� ����� ��������� ������� ������� � ����������� �������,
			//   � ����� ������ ������ ������� POC Vol ������� ����� � ������. ������������ ��� ����� ���� ����� ������� �� ������.)))
			// ���� ����� ������������ ������� �����, �.�. ����� �����.

			var maxVolume = Math.Round(PriceLevels.Sum(p => p.BuyVolume + p.SellVolume) * VolumePercent / 100, 0);
			var currVolume = PriceLevels.Select(p => (p.BuyVolume + p.SellVolume)).Max();

			PoC = PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == currVolume);

			var abovePoc = Combine(PriceLevels.Where(p => p.Price > PoC.Price).OrderBy(p => p.Price));
			var belowePoc = Combine(PriceLevels.Where(p => p.Price < PoC.Price).OrderByDescending(p => p.Price));

			if (abovePoc.Count == 0)
			{
				LinkedListNode<CandlePriceLevel> node;

				for (node = belowePoc.First; node != null; node = node.Next)
				{
					var vol = node.Value.BuyVolume + node.Value.SellVolume;

					if (currVolume + vol > maxVolume)
					{
						High = PoC;
						Low = node.Value;
					}
					else
					{
						currVolume += vol;
					}
				}
			}
			else if (belowePoc.Count == 0)
			{
				LinkedListNode<CandlePriceLevel> node;

				for (node = abovePoc.First; node != null; node = node.Next)
				{
					var vol = node.Value.BuyVolume + node.Value.SellVolume;

					if (currVolume + vol > maxVolume)
					{
						High = node.Value;
						Low = PoC;
					}
					else
					{
						currVolume += vol;
					}
				}
			}
			else
			{
				var abovePocNode = abovePoc.First;
				var belowPocNode = belowePoc.First;

				while (true)
				{
					var aboveVol = abovePocNode.Value.BuyVolume + abovePocNode.Value.SellVolume;
					var belowVol = belowPocNode.Value.BuyVolume + belowPocNode.Value.SellVolume;

					if (aboveVol > belowVol)
					{
						if (currVolume + aboveVol > maxVolume)
						{
							High = abovePocNode.Value;
							Low = belowPocNode.Value;
							break;
						}

						currVolume += aboveVol;
						abovePocNode = abovePocNode.Next;
					}
					else
					{
						if (currVolume + belowVol > maxVolume)
						{
							High = abovePocNode.Value;
							Low = belowPocNode.Value;
							break;
						}

						currVolume += belowVol;
						belowPocNode = belowPocNode.Next;
					}
				}
			}
		}

		private static LinkedList<CandlePriceLevel> Combine(IEnumerable<CandlePriceLevel> prices)
		{
			var enumerator = prices.GetEnumerator();
			var list = new LinkedList<CandlePriceLevel>();

			while (true)
			{
				if (!enumerator.MoveNext())
					break;

				var pl = enumerator.Current;

				if (!enumerator.MoveNext())
				{
					list.AddLast(pl);
					break;
				}

				var buyVolumes = enumerator.Current.BuyVolumes.Concat(pl.BuyVolumes).ToArray();
				var sellVolumes = enumerator.Current.SellVolumes.Concat(pl.SellVolumes).ToArray();

				list.AddLast(new CandlePriceLevel
				{
					Price = enumerator.Current.Price,
					BuyCount = buyVolumes.Length,
					SellCount = sellVolumes.Length,
					BuyVolumes = buyVolumes,
					SellVolumes = sellVolumes,
					BuyVolume = buyVolumes.Sum(),
					SellVolume = sellVolumes.Sum()
				});
			}

			return list;
		}
	}
}
