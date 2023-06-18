using System;

namespace CryptoTools.Common.Model.Transactions
{
    public abstract class TransactionBase
    {
        public string TransactionIdentifier { get; set; }
        public string TransactionGroupIdentifier { get; set; }
        public DateTime Time { get; set; }
        public decimal BaseAmmount { get; set; }
        public Symbol BaseSymbol { get; set; }
        public decimal? QuotedAmmount { get; set; }
        public Symbol QuotedSymbol { get; set; }
        public abstract TransactionType Type { get; }
        public decimal Fee { get; set; }
        public Symbol FeeSymbol { get; set; }


        public enum TransactionType
        {
            Unknown,
            BuySell,
            Reward,
            DepositWithdrawal,
            StakeUnstake,
            CryptoExchange,
            SecurityRemoval,
            SecurityDelivery
        }
    }

}
