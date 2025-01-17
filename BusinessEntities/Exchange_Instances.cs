namespace StockSharp.BusinessEntities
{
	using Ecng.Common;

	partial class Exchange
	{
		static Exchange()
		{
			Test = new Exchange
			{
				Name = "TEST",
				RusName = "�������� �����",
				EngName = "Test Exchange",
			};

			Moex = new Exchange
			{
				Name = "MOEX",
				RusName = "���������� �����",
				EngName = "Moscow Exchange",
				CountryCode = CountryCodes.RU,
			};

			Ux = new Exchange
			{
				Name = "UX",
				RusName = "���������� �����",
				EngName = "Ukrain Exchange",
				CountryCode = CountryCodes.UA,
			};

			Amex = new Exchange
			{
				Name = "AMEX",
				RusName = "������������ �������� �����",
				EngName = "American Stock Exchange",
				CountryCode = CountryCodes.US,
			};

			Cme = new Exchange
			{
				Name = "CME",
				RusName = "��������� �������� �����",
				EngName = "Chicago Mercantile Exchange",
				CountryCode = CountryCodes.US,
			};

			Cce = new Exchange
			{
				Name = "CCE",
				RusName = "��������� ������������� �����",
				EngName = "Chicago Climate Exchange",
				CountryCode = CountryCodes.US,
			};

			Cbot = new Exchange
			{
				Name = "CBOT",
				RusName = "��������� �������� ������",
				EngName = "Chicago Board of Trade",
				CountryCode = CountryCodes.US,
			};

			Nymex = new Exchange
			{
				Name = "NYMEX",
				RusName = "���-�������� �������� �����",
				EngName = "New York Mercantile Exchange",
				CountryCode = CountryCodes.US,
			};

			Nyse = new Exchange
			{
				Name = "NYSE",
				RusName = "���-�������� �������� �����",
				EngName = "New York Stock Exchange",
				CountryCode = CountryCodes.US,
			};

			Nasdaq = new Exchange
			{
				Name = "NASDAQ",
				RusName = "������",
				EngName = "NASDAQ",
				CountryCode = CountryCodes.US,
			};

			Nqlx = new Exchange
			{
				Name = "NQLX",
				RusName = "������ LM",
				EngName = "Nasdaq-Liffe Markets",
				CountryCode = CountryCodes.US,
			};

			Tsx = new Exchange
			{
				Name = "TSX",
				RusName = "�������� ����� �������",
				EngName = "Toronto Stock Exchange",
				CountryCode = CountryCodes.CA,
			};

			Lse = new Exchange
			{
				Name = "LSE",
				RusName = "���������� �������� �����",
				EngName = "London Stock Exchange",
				CountryCode = CountryCodes.GB,
			};

			Tse = new Exchange
			{
				Name = "TSE",
				RusName = "��������� �������� �����",
				EngName = "Tokio Stock Exchange",
				CountryCode = CountryCodes.JP,
			};

			Hkex = new Exchange
			{
				Name = "HKEX",
				RusName = "����������� �������� �����",
				EngName = "Hong Kong Stock Exchange",
				CountryCode = CountryCodes.HK,
			};

			Hkfe = new Exchange
			{
				Name = "HKFE",
				RusName = "����������� ���������� �����",
				EngName = "Hong Kong Futures Exchange",
				CountryCode = CountryCodes.HK,
			};

			Sse = new Exchange
			{
				Name = "SSE",
				RusName = "��������� �������� �����",
				EngName = "Shanghai Stock Exchange",
				CountryCode = CountryCodes.CN,
			};

			Szse = new Exchange
			{
				Name = "SZSE",
				RusName = "������������� �������� �����",
				EngName = "Shenzhen Stock Exchange",
				CountryCode = CountryCodes.CN,
			};

			Tsec = new Exchange
			{
				Name = "TSEC",
				RusName = "����������� �������� �����",
				EngName = "Taiwan Stock Exchange",
				CountryCode = CountryCodes.TW,
			};

			Sgx = new Exchange
			{
				Name = "SGX",
				RusName = "������������ �����",
				EngName = "Singapore Exchange",
				CountryCode = CountryCodes.SG,
			};

			Pse = new Exchange
			{
				Name = "PSE",
				RusName = "������������ �������� �����",
				EngName = "Philippine Stock Exchange",
				CountryCode = CountryCodes.PH,
			};

			Klse = new Exchange
			{
				Name = "MYX",
				RusName = "������������ �����",
				EngName = "Bursa Malaysia",
				CountryCode = CountryCodes.MY,
			};

			Idx = new Exchange
			{
				Name = "IDX",
				RusName = "������������� �������� �����",
				EngName = "Indonesia Stock Exchange",
				CountryCode = CountryCodes.ID,
			};

			Set = new Exchange
			{
				Name = "SET",
				RusName = "�������� ����� ��������",
				EngName = "Stock Exchange of Thailand",
				CountryCode = CountryCodes.TH,
			};

			Bse = new Exchange
			{
				Name = "BSE",
				RusName = "���������� �������� �����",
				EngName = "Bombay Stock Exchange",
				CountryCode = CountryCodes.IN,
			};

			Nse = new Exchange
			{
				Name = "NSE",
				RusName = "������������ �������� ����� �����",
				EngName = "National Stock Exchange of India",
				CountryCode = CountryCodes.IN,
			};

			Cse = new Exchange
			{
				Name = "CSE",
				RusName = "������������ �������� �����",
				EngName = "Colombo Stock Exchange",
				CountryCode = CountryCodes.CO,
			};

			Krx = new Exchange
			{
				Name = "KRX",
				RusName = "��������� �����",
				EngName = "Korea Exchange",
				CountryCode = CountryCodes.KR,
			};

			Asx = new Exchange
			{
				Name = "ASX",
				RusName = "������������� �������� �����",
				EngName = "Australian Securities Exchange",
				CountryCode = CountryCodes.AU,
			};

			Nzx = new Exchange
			{
				Name = "NZSX",
				RusName = "�������������� �����",
				EngName = "New Zealand Exchange",
				CountryCode = CountryCodes.NZ,
			};

			Tase = new Exchange
			{
				Name = "TASE",
				RusName = "����-�������� �������� �����",
				EngName = "Tel Aviv Stock Exchange",
				CountryCode = CountryCodes.IL,
			};

			Fwb = new Exchange
			{
				Name = "FWB",
				RusName = "������������� �������� �����",
				EngName = "Frankfurt Stock Exchange",
				CountryCode = CountryCodes.DE,
			};

			Mse = new Exchange
			{
				Name = "MSE",
				RusName = "���������� �������� �����",
				EngName = "Madrid Stock Exchange",
				CountryCode = CountryCodes.ES,
			};

			Swx = new Exchange
			{
				Name = "SWX",
				RusName = "����������� �����",
				EngName = "Swiss Exchange",
				CountryCode = CountryCodes.CH,
			};

			Jse = new Exchange
			{
				Name = "JSE",
				RusName = "���������������� �������� �����",
				EngName = "Johannesburg Stock Exchange",
				CountryCode = CountryCodes.ZA,
			};

			Lmax = new Exchange
			{
				Name = "LMAX",
				RusName = "������ ������ LMAX",
				EngName = "LMAX",
				CountryCode = CountryCodes.GB,
			};

			DukasCopy = new Exchange
			{
				Name = "DUKAS",
				RusName = "������ ������ DukasCopy",
				EngName = "DukasCopy",
				CountryCode = CountryCodes.CH,
			};

			GainCapital = new Exchange
			{
				Name = "GAIN",
				RusName = "������ ������ GAIN Capital",
				EngName = "GAIN Capital",
				CountryCode = CountryCodes.US,
			};

			MBTrading = new Exchange
			{
				Name = "MBT",
				RusName = "������ ������ MB Trading",
				EngName = "MB Trading",
				CountryCode = CountryCodes.US,
			};

			TrueFX = new Exchange
			{
				Name = "TRUEFX",
				RusName = "������ ������ TrueFX",
				EngName = "TrueFX",
				CountryCode = CountryCodes.US,
			};

			Cfh = new Exchange
			{
				Name = "CFH",
				RusName = "CFH",
				EngName = "CFH",
				CountryCode = CountryCodes.GB,
			};

			Ond = new Exchange
			{
				Name = "OANDA",
				RusName = "������ ������ OANDA",
				EngName = "OANDA",
				CountryCode = CountryCodes.US,
			};

			Integral = new Exchange
			{
				Name = "INTGRL",
				RusName = "Integral",
				EngName = "Integral",
				CountryCode = CountryCodes.US,
			};

			Btce = new Exchange
			{
				Name = "BTCE",
				RusName = "BTCE",
				EngName = "BTCE",
				CountryCode = CountryCodes.RU,
			};

			BitStamp = new Exchange
			{
				Name = "BITSTAMP",
				RusName = "BitStamp",
				EngName = "BitStamp",
				CountryCode = CountryCodes.GB,
			};

			BtcChina = new Exchange
			{
				Name = "BTCCHINA",
				RusName = "BTCChina",
				EngName = "BTCChina",
				CountryCode = CountryCodes.CN,
			};

			Icbit = new Exchange
			{
				Name = "ICBIT",
				RusName = "iCBIT",
				EngName = "iCBIT",
				CountryCode = CountryCodes.RU,
			};
		}

		/// <summary>
		/// Information about the test exchange, which has no limitations in work schedule.
		/// </summary>
		public static Exchange Test { get; private set; }

		/// <summary>
		/// Information about MOEX (Moscow Exchange).
		/// </summary>
		public static Exchange Moex { get; private set; }

		/// <summary>
		/// Information about UX.
		/// </summary>
		public static Exchange Ux { get; private set; }

		/// <summary>
		/// Information about AMEX (American Stock Exchange).
		/// </summary>
		public static Exchange Amex { get; private set; }

		/// <summary>
		/// Information about CME (Chicago Mercantile Exchange).
		/// </summary>
		public static Exchange Cme { get; private set; }

		/// <summary>
		/// Information about CBOT (Chicago Board of Trade).
		/// </summary>
		public static Exchange Cbot { get; private set; }

		/// <summary>
		/// Information about CCE (Chicago Climate Exchange).
		/// </summary>
		public static Exchange Cce { get; private set; }

		/// <summary>
		/// Information about NYMEX (New York Mercantile Exchange).
		/// </summary>
		public static Exchange Nymex { get; private set; }

		/// <summary>
		/// Information about NYSE (New York Stock Exchange).
		/// </summary>
		public static Exchange Nyse { get; private set; }

		/// <summary>
		/// Information about NASDAQ.
		/// </summary>
		public static Exchange Nasdaq { get; private set; }

		/// <summary>
		/// Information about NQLX.
		/// </summary>
		public static Exchange Nqlx { get; private set; }

		/// <summary>
		/// Information about LSE (London Stock Exchange).
		/// </summary>
		public static Exchange Lse { get; private set; }

		/// <summary>
		/// Information about TSE (Tokio Stock Exchange).
		/// </summary>
		public static Exchange Tse { get; private set; }

		/// <summary>
		/// Information about HKEX (Hong Kong Stock Exchange).
		/// </summary>
		public static Exchange Hkex { get; private set; }

		/// <summary>
		/// Information about HKFE (Hong Kong Futures Exchange).
		/// </summary>
		public static Exchange Hkfe { get; private set; }

		/// <summary>
		/// Information about Sse (Shanghai Stock Exchange).
		/// </summary>
		public static Exchange Sse { get; private set; }

		/// <summary>
		/// Information about SZSE (Shenzhen Stock Exchange).
		/// </summary>
		public static Exchange Szse { get; private set; }

		/// <summary>
		/// Information about TSX (Toronto Stock Exchange).
		/// </summary>
		public static Exchange Tsx { get; private set; }

		/// <summary>
		/// Information about FWB (Frankfurt Stock Exchange).
		/// </summary>
		public static Exchange Fwb { get; private set; }

		/// <summary>
		/// Information about ASX (Australian Securities Exchange).
		/// </summary>
		public static Exchange Asx { get; private set; }

		/// <summary>
		/// Information about NZX (New Zealand Exchange).
		/// </summary>
		public static Exchange Nzx { get; private set; }

		/// <summary>
		/// Information about BSE (Bombay Stock Exchange).
		/// </summary>
		public static Exchange Bse { get; private set; }

		/// <summary>
		/// Information about NSE (National Stock Exchange of India).
		/// </summary>
		public static Exchange Nse { get; private set; }

		/// <summary>
		/// Information about SWX (Swiss Exchange).
		/// </summary>
		public static Exchange Swx { get; private set; }

		/// <summary>
		/// Information about KRX (Korea Exchange).
		/// </summary>
		public static Exchange Krx { get; private set; }

		/// <summary>
		/// Information about MSE (Madrid Stock Exchange).
		/// </summary>
		public static Exchange Mse { get; private set; }

		/// <summary>
		/// Information about JSE (Johannesburg Stock Exchange).
		/// </summary>
		public static Exchange Jse { get; private set; }

		/// <summary>
		/// Information about SGX (Singapore Exchange).
		/// </summary>
		public static Exchange Sgx { get; private set; }

		/// <summary>
		/// Information about TSEC (Taiwan Stock Exchange).
		/// </summary>
		public static Exchange Tsec { get; private set; }

		/// <summary>
		/// Information about PSE (Philippine Stock Exchange).
		/// </summary>
		public static Exchange Pse { get; private set; }

		/// <summary>
		/// Information about KLSE (Bursa Malaysia).
		/// </summary>
		public static Exchange Klse { get; private set; }

		/// <summary>
		/// Information about IDX (Indonesia Stock Exchange).
		/// </summary>
		public static Exchange Idx { get; private set; }

		/// <summary>
		/// Information about SET (Stock Exchange of Thailand).
		/// </summary>
		public static Exchange Set { get; private set; }

		/// <summary>
		/// Information about CSE (Colombo Stock Exchange).
		/// </summary>
		public static Exchange Cse { get; private set; }

		/// <summary>
		/// Information about TASE (Tel Aviv Stock Exchange).
		/// </summary>
		public static Exchange Tase { get; private set; }

		/// <summary>
		/// Information about LMAX (LMAX Exchange).
		/// </summary>
		public static Exchange Lmax { get; private set; }

		/// <summary>
		/// Information about DukasCopy.
		/// </summary>
		public static Exchange DukasCopy { get; private set; }

		/// <summary>
		/// Information about GAIN Capital.
		/// </summary>
		public static Exchange GainCapital { get; private set; }

		/// <summary>
		/// Information about MB Trading.
		/// </summary>
		public static Exchange MBTrading { get; private set; }

		/// <summary>
		/// Information about TrueFX.
		/// </summary>
		public static Exchange TrueFX { get; private set; }

		/// <summary>
		/// Information about CFH.
		/// </summary>
		public static Exchange Cfh { get; private set; }

		/// <summary>
		/// Information about OANDA.
		/// </summary>
		public static Exchange Ond { get; private set; }

		/// <summary>
		/// Information about Integral.
		/// </summary>
		public static Exchange Integral { get; private set; }

		/// <summary>
		/// Information about BTCE.
		/// </summary>
		public static Exchange Btce { get; private set; }

		/// <summary>
		/// Information about BitStamp.
		/// </summary>
		public static Exchange BitStamp { get; private set; }

		/// <summary>
		/// Information about BtcChina.
		/// </summary>
		public static Exchange BtcChina { get; private set; }

		/// <summary>
		/// Information about Icbit.
		/// </summary>
		public static Exchange Icbit { get; private set; }
	}
}