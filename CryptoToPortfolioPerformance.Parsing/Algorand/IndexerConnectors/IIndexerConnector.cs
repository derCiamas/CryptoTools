using CryptoToPortfolioPerformance.Parsing.Algorand.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.IndexerConnectors
{
    public interface IIndexerConnector
    {
        public Task<IEnumerable<AlgorandTransaction>> GetWalletTransactions(string walletAddress);
    }
}
