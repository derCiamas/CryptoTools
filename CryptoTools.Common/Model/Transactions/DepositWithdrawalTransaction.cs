using System;

namespace CryptoTools.Common.Model.Transactions
{
    public class DepositWithdrawalTransaction : TransactionBase
    {
        public override TransactionType Type => TransactionType.DepositWithdrawal;

        public DepositWithdrawalTransaction(decimal ammount, CommonSymbol symbol, DateTime time)
        {
            BaseAmmount = ammount;
            BaseSymbol = symbol;
            Time = time;
        }
    }
}
