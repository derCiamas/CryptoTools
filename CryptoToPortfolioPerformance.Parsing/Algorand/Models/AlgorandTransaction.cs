using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.Models
{
    public class AlgorandTransaction
    {
        //https://developer.algorand.org/docs/rest-apis/indexer/#transaction
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("inner-txns")]
        public List<AlgorandTransaction> InnerTransactions { get; set; }

        [JsonProperty("application-transaction")]
        public AlgorandTransactionApplication ApplicationTransaction { get; set; }

        [JsonProperty("asset-transfer-transaction")]
        public AlgorandTransactionAssetTransfer AssetTransferTransaction { get; set; }

        [JsonProperty("payment-transaction")]
        public AlgorandTransactionPayment PaymentTransaction { get; set; }

        [JsonProperty("tx-type")]
        public AlgorandTransactionType TransactionType { get; set; }

        [JsonProperty("round-time")]
        public int RoundTime { get; set; }

        public enum AlgorandTransactionType
        {
            None,
            [EnumMember(Value = "pay")]
            Payment,
            [EnumMember(Value = "keyreg")]
            KeyReg,
            [EnumMember(Value = "acfg")]
            AssetConfig,
            [EnumMember(Value = "axfer")]
            AssetTransfer,
            [EnumMember(Value = "afrz")]
            AssetFreeze,
            [EnumMember(Value = "appl")]
            ApplicationTransaction,
            [EnumMember(Value = "stpf")]
            StateProofTransaction
        }
    }

  
}
