namespace CryptoTools.Common.Model.Transactions
{
    public class CryptoExchangeTransaction : TransactionBase
    {
        public override TransactionType Type => TransactionType.CryptoExchange;
    }
}
