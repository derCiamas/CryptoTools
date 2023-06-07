using Newtonsoft.Json;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.Models
{
    /// <summary>
    /// Asset transfer transaction happens when transfering ASA's
    /// </summary>
    public class AlgorandTransactionAssetTransfer
    {
        [JsonProperty("amount")]
        public long Amount { get; set; }
        [JsonProperty("asset-id")]
        public int AssetId { get; set; }
        [JsonProperty("receiver")]
        public string Receiver { get; set; }
    }

  
}
