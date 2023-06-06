namespace CryptoTools.Common.Model.Transactions
{
    public class BuySellTransaction : TransactionBase
    {
        public override TransactionType Type => TransactionType.BuySell;
    }
}
