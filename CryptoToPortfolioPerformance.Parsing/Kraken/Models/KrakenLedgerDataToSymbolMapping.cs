using CryptoTools.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken.Models
{
    internal class KrakenLedgerDataToSymbolMapping
    {
        public string Symbol { get; set; }
        public List<string> KrakenInputValues { get; set; }
    }
}
