namespace CryptoTools.Common.Model.Transactions
{
    public class StakeUnstakeTransaction : TransactionBase
    {
        public override TransactionType Type => TransactionType.StakeUnstake;
        public bool IsStake { get; set; }
    }
}
