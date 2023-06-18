using System;

namespace CryptoTools.Common.Model.Transactions
{
    public class SecurityRemovalTransaction : TransactionBase
    {
        public override TransactionType Type => TransactionType.SecurityRemoval;

        public SecurityRemovalTransaction(decimal ammount, Symbol symbol, DateTime time)
        {
            BaseAmmount = ammount;
            BaseSymbol = symbol;
            Time = time;
        }
    }
}
