using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.Models
{
    public class IndexerAccountTransactionsResponse
    {
        [JsonProperty("current-round")]
        public int CurrentRound { get; set; }
        [JsonProperty("next-token")]
        public string NextToken { get; set; }
        [JsonProperty("transactions")]
        public List<AlgorandTransaction> Transactions { get; set; }
    }
}
