using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.Models
{
    /// <summary>
    /// Payment will happen only for ALGO (and not ASA's)
    /// </summary>
    public class AlgorandTransactionPayment
    {
        [JsonProperty("amount")]
        public long Amount { get; set; }
        [JsonProperty("receiver")]
        public string Receiver { get; set; }
    }
}
