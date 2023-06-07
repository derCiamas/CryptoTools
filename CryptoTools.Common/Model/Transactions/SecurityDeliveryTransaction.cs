using System;

namespace CryptoTools.Common.Model.Transactions
{
    public class SecurityDeliveryTransaction : TransactionBase
    {
        public override TransactionType Type => TransactionType.SecurityDelivery;

        public SecurityDeliveryTransaction(decimal ammount, CommonSymbol symbol, DateTime time)
        {
            BaseAmmount = ammount;
            BaseSymbol = symbol;
            Time = time;
        }
    }
}
