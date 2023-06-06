using System;

namespace CryptoTools.Common.Model.Transactions
{
    public abstract class TransactionBase
    {
        public DateTime Time { get; set; }
        public decimal BaseAmmount { get; set; }
        public CommonSymbol BaseSymbol { get; set; }
        public decimal? QuotedAmmount { get; set; }
        public CommonSymbol QuotedSymbol { get; set; }
        public abstract TransactionType Type { get; }
        public decimal Fee { get; set; }
        public CommonSymbol FeeSymbol { get; set; }


        public enum TransactionType
        {
            Unknown,
            BuySell,
            Reward,
            DepositWithdrawal,
            StakeUnstake,
            CryptoExchange
        }
    }

}
