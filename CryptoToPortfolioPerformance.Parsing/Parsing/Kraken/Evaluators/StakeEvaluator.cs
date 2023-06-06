using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators
{
    internal class StakeEvaluator : TransactionEvaluatorBase
    {
        public StakeEvaluator(Dictionary<string, string> krakenLedgerToSymbolMappings) : base(krakenLedgerToSymbolMappings)
        {
        }

        public override TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
        {
            ///   5. Stake:
            /// - 1 x 'withdrawal' and 1 x 'transfer' sub 'spottostaking' sharing same refid
            /// - 1 x 'deposit' Symbol.S and 1 x 'transfer' sub 'stakingfromspot' sharing same refid
            /// - both 'transfer' share same ammount 'stakingtospot' > 0 'spotfromstaking' < 0
            var list = new List<ToBeEvaluatedItem>();
            var withdrawalItem = evaluationItem.Item;
            if (withdrawalItem.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
            {
                return null;
            }
            list.Add(evaluationItem);
            var transferItem = allItems.SingleOrDefault(i =>
                                                !i.AlreadyUsed &&
                                                i.Item.RefId == withdrawalItem.RefId &&
                                                i.Item.Type == KrakenLedgerItemTransactionTypes.Transfer &&
                                                i.Item.Subtype == KrakenLedgerItemTransactionTypes.SpotToStaking &&
                                                !list.Contains(i));
            if (transferItem == null)
            {
                return null;
            }
            list.Add(transferItem);

            var secondTransferItem = allItems.SingleOrDefault(i =>
                                               !i.AlreadyUsed &&
                                               i.Item.Type == KrakenLedgerItemTransactionTypes.Transfer &&
                                               i.Item.Subtype == KrakenLedgerItemTransactionTypes.StakingFromSpot &&
                                               i.Item.Amount == transferItem.Item.Amount * -1 &&
                                               !list.Contains(i));


            if (secondTransferItem == null)
            {
                return null;
            }

            var depositItems = allItems.Where(i =>
                                                !i.AlreadyUsed &&
                                                i.Item.Type == KrakenLedgerItemTransactionTypes.Deposit &&
                                                i.Item.RefId == secondTransferItem.Item.RefId &&
                                                !list.Contains(i));
            if (depositItems.Count() == 0)
            {
                return null;
            }




            var depositItem = depositItems.SingleOrDefault(i => i.Item.RefId == secondTransferItem.Item.RefId);
            if (depositItem == null)
            {
                return null;
            }
            list.Add(depositItem);

            list.Add(secondTransferItem);
            list.ForEach(e => e.AlreadyUsed = true);

            return new StakeUnstakeTransaction()
            {
                BaseAmmount = depositItem.Item.Amount,
                BaseSymbol = SymbolFromKrakenString(depositItem.Item.Asset),
                IsStake = true,
                Time = depositItem.Item.Time
            };
        }
    }
}
