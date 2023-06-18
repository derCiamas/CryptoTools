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
        public async Task<decimal?> ExchangeRateForPair(Symbol baseSymbol, Symbol quotedSymbol, DateTime time)
        {
            return ExchangeRates[(baseSymbol.Description, quotedSymbol.Description)];
        }

        public async Task<decimal> ExchangeSymbol(Symbol baseSymbol, Symbol quotedSymbol, decimal value, DateTime time)
        {
            var exchangeRate = await ExchangeRateForPair(baseSymbol, quotedSymbol, time);
            return value / exchangeRate.Value;
        }
    }
}
