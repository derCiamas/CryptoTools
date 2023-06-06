using CryptoTools.Common.Model;
using System;
using System.Threading.Tasks;

namespace CryptoTools.Common.ExchangeRateProviders
{
    public interface IExchangeRateProvider
    {
        public Task<decimal?> ExchangeRateForPair(CommonSymbol baseSymbol, CommonSymbol quotedSymbol, DateTime time);
        public Task<decimal> ExchangeSymbol(CommonSymbol baseSymbol, CommonSymbol quotedSymbol, decimal value, DateTime time);
    }
}
