using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators.Models
{
    internal class KrakenLedgerItem
    {
        [Name("txid")]
        public string TxId { get; set; }
        [Name("refid")]
        public string RefId { get; set; }
        [Name("time")]
        public DateTime Time { get; set; }
        [Name("type")]
        public string Type { get; set; }
        [Name("subtype")]
        public string Subtype { get; set; }
        [Name("aclass")]
        public string AssetClass { get; set; }
        [Name("asset")]
        public string Asset { get; set; }
        [Name("amount")]
        public decimal Amount { get; set; }
        [Name("fee")]
        public decimal Fee { get; set; }
        [Name("balance")]
        public decimal? Balance { get; set; }
    }
}
