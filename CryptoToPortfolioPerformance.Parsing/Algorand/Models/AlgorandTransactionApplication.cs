using Newtonsoft.Json;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.Models
{
    public class AlgorandTransactionApplication
    {
        [JsonProperty("application-id")]
        public string ApplicationId { get; set; }
    }

  
}
