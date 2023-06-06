﻿using CryptoTools.Common.Model;
using CsvHelper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoTools.Common.ExchangeRateProviders
{
    public class KrakenCSVExchangeRateProvider : IExchangeRateProvider
    {
        private string _dataDirectory;
        private KrakenCSVProviderItem.TimeGranularity[] _defaultGranularities = new KrakenCSVProviderItem.TimeGranularity[] { KrakenCSVProviderItem.TimeGranularity.Minute, KrakenCSVProviderItem.TimeGranularity.Hour, KrakenCSVProviderItem.TimeGranularity.Day };

        public KrakenCSVExchangeRateProvider(string dataDirectory)
        {
            if (!Directory.Exists(dataDirectory))
            {
                throw new ArgumentException($"Directory '{dataDirectory}' does not exist");
            }
            _dataDirectory = dataDirectory;
        }

        //TODO@Piotr => Need to refactor the whole method, it looks nasty
        public async Task<decimal?> ExchangeRateForPair(CommonSymbol baseSymbol, CommonSymbol quotedSymbol, DateTime time)
        {
            var pair = new SymbolPair(baseSymbol, quotedSymbol);
            var exchangeRateItems = ItemCache.Instance.GetForPair(pair, KrakenCSVProviderItem.TimeGranularity.Minute);
            if (exchangeRateItems == null)
            {
                await InitializeForPair(pair);
                exchangeRateItems = ItemCache.Instance.GetForPair(pair, KrakenCSVProviderItem.TimeGranularity.Minute);
            }
            var dateTimeWithoutSeconds = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
            var item = exchangeRateItems?.SingleOrDefault(i => i.TimestampWithoutSeconds == dateTimeWithoutSeconds);
            if (item == null)
            {
                //Try hour granularity
                exchangeRateItems = ItemCache.Instance.GetForPair(pair, KrakenCSVProviderItem.TimeGranularity.Hour);
                var dateTimeWithoutMinutes = new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0);
                item = exchangeRateItems?.SingleOrDefault(i => i.TimestampWithoutSeconds == dateTimeWithoutMinutes);
                if (item == null)
                {
                    //Day granularity...
                    exchangeRateItems = ItemCache.Instance.GetForPair(pair, KrakenCSVProviderItem.TimeGranularity.Day);
                    var dateTimeWithoutHours = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0);
                    item = exchangeRateItems.SingleOrDefault(i => i.TimestampWithoutSeconds == dateTimeWithoutHours);
                    if (item == null)
                    {
                        //Find nearest day if no price yet (happens with Airdrops)
                        var maxDifference = new TimeSpan(15, 0, 0, 0, 0);
                        item = exchangeRateItems.First();
                        if (item.TimestampWithoutSeconds - dateTimeWithoutHours > maxDifference)
                        {
                            item = null;
                        }
                    }
                }
            }
            return item?.Close;
        }

        public async Task<decimal> ExchangeSymbol(CommonSymbol baseSymbol, CommonSymbol quotedSymbol, decimal value, DateTime time)
        {
            var exchangeRate = await ExchangeRateForPair(baseSymbol, quotedSymbol, time);
            if (exchangeRate == null)
            {
                throw new ArgumentException($"No exchange rate for pair {baseSymbol.Description}{quotedSymbol.Description} for {time} found");
            }
            return value / exchangeRate.Value;
        }

        private async Task InitializeForPair(SymbolPair pair)
        {
            var pairItems = new List<KrakenCSVProviderItem>();
            foreach (var granularity in _defaultGranularities)
            {
                if (pair.QuotedSymbol.Equals(Symbol.EUR) && pair.BaseSymbol.Equals(Symbol.EUR))
                {
                    //Fake for EUR only with day granularity
                    var date = new DateTime(2015, 1, 1);
                    var difference = DateTime.UtcNow - date;
                    for (var i = 0; i < difference.TotalDays; i++)
                    {
                        var providerItem = new KrakenCSVProviderItem(KrakenCSVProviderItem.TimeGranularity.Day)
                        {
                            //TODO@Piotr => refactor, this is only quick fix
                            Timestamp = (date.Ticks - 621355968000000000) / 10000000,
                            Open = 1,
                            High = 1,
                            Low = 1,
                            Close = 1
                        };
                        pairItems.Add(providerItem);
                        date = date.AddDays(1);
                    }
                    break;
                }
                else
                {
                    var ohlcvtFileName = GetOHLCVTFileName(pair.BaseSymbol.Description, pair.QuotedSymbol.Description, granularity);
                    var ohlcvtFilePath = Path.Combine(_dataDirectory, ohlcvtFileName);
                    if (!File.Exists(ohlcvtFilePath))
                    {
                        //Could be that we need to flip the symbol pair
                        ohlcvtFileName = GetOHLCVTFileName(pair.QuotedSymbol.Description, pair.BaseSymbol.Description, granularity);
                        ohlcvtFilePath = Path.Combine(_dataDirectory, ohlcvtFileName);
                        if (!File.Exists(ohlcvtFilePath))
                        {
                            throw new Exception($"No data found for symbol {pair.BaseSymbol.Description}{pair.QuotedSymbol.Description}");
                        }
                    }

                    using (var reader = new StreamReader(ohlcvtFilePath))
                    {
                        using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CreateSpecificCulture("en-US")) { HasHeaderRecord = false }))
                        {
                            while (await csv.ReadAsync())
                            {
                                var providerItem = new KrakenCSVProviderItem(granularity)
                                {
                                    Timestamp = csv.GetField<long>(0),
                                    Open = csv.GetField<decimal>(1),
                                    High = csv.GetField<decimal>(2),
                                    Low = csv.GetField<decimal>(3),
                                    Close = csv.GetField<decimal>(4)
                                };
                                pairItems.Add(providerItem);
                            }
                        }
                    }
                }
            }

            //Also the other way around
            var contraSymbol = new SymbolPair(pair.QuotedSymbol, pair.BaseSymbol);
            var contraSymbolItems = pairItems.Select(e => e.CloneAndMirrorPrices()).ToList();
            ItemCache.Instance.AddForPair(contraSymbol, contraSymbolItems);
            ItemCache.Instance.AddForPair(pair, pairItems);
        }

        private string GetOHLCVTFileName(string baseSymbol, string quotedSymbol, KrakenCSVProviderItem.TimeGranularity granularity)
        {
            return $"{StandarizeSymbolName(baseSymbol)}{StandarizeSymbolName(quotedSymbol)}_{(int)granularity}.csv";
        }

        private string StandarizeSymbolName(string symbol)
        {
            return symbol.ToUpper();
        }

        private class KrakenCSVProviderItem
        {
            private long _timestamp;
            private DateTime _timestampDateTime;
            private DateTime _timestampDateTimeWithoutSeconds;
            public long Timestamp
            {
                get
                {
                    return _timestamp;
                }
                set
                {
                    _timestamp = value;
                    _timestampDateTime = DateTime.UnixEpoch.AddMilliseconds(value * 1000);
                    _timestampDateTimeWithoutSeconds = new DateTime(_timestampDateTime.Year, _timestampDateTime.Month, _timestampDateTime.Day, _timestampDateTime.Hour, _timestampDateTime.Minute, 0);
                }
            }
            public DateTime TimestampDateTime => _timestampDateTime;
            public DateTime TimestampWithoutSeconds => _timestampDateTimeWithoutSeconds;
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public TimeGranularity Granularity { get; private set; }

            public KrakenCSVProviderItem(TimeGranularity granularity)
            {
                Granularity = granularity;
            }

            public KrakenCSVProviderItem CloneAndMirrorPrices()
            {
                return new KrakenCSVProviderItem(Granularity)
                {
                    Timestamp = Timestamp,
                    Open = 1 / Open,
                    High = 1 / High,
                    Low = 1 / Low,
                    Close = 1 / Close
                };
            }

            public enum TimeGranularity
            {
                Minute = 1,
                Hour = 60,
                Day = 1440
            }
        }

        private class SymbolPair
        {
            public Symbol BaseSymbol { get; private set; }
            public Symbol QuotedSymbol { get; private set; }
            public SymbolPair(Symbol baseSymbol, Symbol quotedSymbol)
            {
                BaseSymbol = baseSymbol;
                QuotedSymbol = quotedSymbol;
            }
        }

        private class ItemCache
        {
            #region SingletonStuff
            private static ItemCache _instance;
            private static object _instanceLock = new object();
            public static ItemCache Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        lock (_instanceLock)
                        {
                            if (_instance == null)
                            {
                                _instance = new ItemCache();
                            }
                        }
                    }
                    return _instance;
                }
            }
            private ItemCache()
            {
                _symbolsDictionary = new ConcurrentDictionary<ItemCacheKey, List<KrakenCSVProviderItem>>();
            }
            #endregion


            private ConcurrentDictionary<ItemCacheKey, List<KrakenCSVProviderItem>> _symbolsDictionary;

            public void AddForPair(SymbolPair pair, List<KrakenCSVProviderItem> items)
            {
                var grouped = items.GroupBy(i => i.Granularity);
                foreach (var grouping in grouped)
                {
                    var list = grouping.ToList();
                    _symbolsDictionary.AddOrUpdate(
                                                    new ItemCacheKey(pair, grouping.Key),
                                                    list,
                                                    (s, ev) => list
                                                );
                }
            }

            public IEnumerable<KrakenCSVProviderItem> GetForPair(SymbolPair pair, KrakenCSVProviderItem.TimeGranularity granularity = KrakenCSVProviderItem.TimeGranularity.Minute)
            {
                List<KrakenCSVProviderItem> items;
                _symbolsDictionary.TryGetValue(new ItemCacheKey(pair, granularity), out items);
                return items?.Where(i => i.Granularity == granularity);
            }

            private class ItemCacheKey
            {
                public SymbolPair KeySymbol { get; }
                public KrakenCSVProviderItem.TimeGranularity Granularity { get; }

                public ItemCacheKey(SymbolPair symbolPair, KrakenCSVProviderItem.TimeGranularity granularity)
                {
                    KeySymbol = symbolPair;
                    Granularity = granularity;
                }

                public override bool Equals(object obj)
                {
                    var casted = obj as ItemCacheKey;
                    if (casted != null)
                    {
                        return Granularity == casted.Granularity && KeySymbol.BaseSymbol == casted.KeySymbol.BaseSymbol && KeySymbol.QuotedSymbol == casted.KeySymbol.QuotedSymbol;
                    }
                    return false;
                }

                public override int GetHashCode()
                {
                    return KeySymbol.BaseSymbol.Description.GetHashCode() + KeySymbol.QuotedSymbol.Description.GetHashCode() + Granularity.GetHashCode();
                }
            }
        }
    }
}
