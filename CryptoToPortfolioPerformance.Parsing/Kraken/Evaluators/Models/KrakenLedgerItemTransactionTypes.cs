using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators.Models
{
    internal class KrakenLedgerItemTransactionTypes
    {
        public static readonly string Deposit = "deposit";
        public static readonly string Trade = "trade";
        public static readonly string Spend = "spend";
        public static readonly string Receive = "receive";
        public static readonly string Withdrawal = "withdrawal";
        public static readonly string Transfer = "transfer";
        public static readonly string SpotToStaking = "spottostaking";
        public static readonly string StakingFromSpot = "stakingfromspot";
        public static readonly string StakingToSpot = "stakingtospot";
        public static readonly string SpotFromStaking = "spotfromstaking";
        public static readonly string Staking = "staking";
    }
}
