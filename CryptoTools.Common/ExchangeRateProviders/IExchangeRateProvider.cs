using CryptoTools.Common.Model;
using System;
using System.Threading.Tasks;

namespace CryptoTools.Common.ExchangeRateProviders
{
    public interface IExchangeRateProvider
    {
        public Task<decimal?> ExchangeRateForPair(Symbol baseSymbol, Symbol quotedSymbol, DateTime time);
        public Task<decimal> ExchangeSymbol(Symbol baseSymbol, Symbol quotedSymbol, decimal value, DateTime time);
    }
}
