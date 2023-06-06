using CryptoTools.Common.ExchangeRateProviders;
using CryptoTools.Common.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoTools.Tests.Helpers
{
    public class TestExchangeRateProvider : IExchangeRateProvider
    {
        public Dictionary<(string baseSymbolDescription, string quotedSymbolDescription), decimal> ExchangeRates = new Dictionary<(string baseSymbolDescription, string quotedSymbolDescription), decimal>();
        public async Task<decimal?> ExchangeRateForPair(CommonSymbol baseSymbol, CommonSymbol quotedSymbol, DateTime time)
        {
            return ExchangeRates[(baseSymbol.Description, quotedSymbol.Description)];
        }

        public async Task<decimal> ExchangeSymbol(CommonSymbol baseSymbol, CommonSymbol quotedSymbol, decimal value, DateTime time)
        {
            var exchangeRate = await ExchangeRateForPair(baseSymbol, quotedSymbol, time);
            return value / exchangeRate.Value;
        }
    }
}
