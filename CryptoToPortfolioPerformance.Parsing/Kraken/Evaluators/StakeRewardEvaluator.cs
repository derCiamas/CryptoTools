using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators
{
    internal class StakeRewardEvaluator : TransactionEvaluatorBase
    {
        public StakeRewardEvaluator(Dictionary<string, string> krakenLedgerToSymbolMappings) : base(krakenLedgerToSymbolMappings)
        {
        }

        public override TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
        {
            ///   6. Staking reward which is afterwards automatic restaked (see 7)
            /// - 1 x 'deposit' Symbol.S
            ///  7. Automatic restake
            /// - 1 x 'staking'
            ///     9. Parachain rewards (ETH Rewards)
            /// - 1 x 'deposit' and 1 x 'staking' sharing same ammount and currency
            if (evaluationItem.Item.Type != KrakenLedgerItemTransactionTypes.Deposit)
            {
                return null;
            }
            //Problem with small stakes (like BTC), need to order by date and find the nearest one
            var possibleItems = allItems.Where(i => !i.AlreadyUsed && i != evaluationItem && i.Item.Type == KrakenLedgerItemTransactionTypes.Staking && i.Item.Time > evaluationItem.Item.Time && i.Item.Asset == evaluationItem.Item.Asset && i.Item.Amount == evaluationItem.Item.Amount);

            var orderedByDate = possibleItems.OrderBy(i => i.Item.Time);
            var restakeItem = orderedByDate.FirstOrDefault(i => i.Item.Time > evaluationItem.Item.Time);

            //var restakeItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i != evaluationItem && i.Item.Type == KrakenLedgerItemTransactionTypes.Staking && i.Item.Time > evaluationItem.Item.Time && i.Item.Asset == evaluationItem.Item.Asset && i.Item.Amount == evaluationItem.Item.Amount);

            if (restakeItem == null)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(evaluationItem.Item.Subtype) || !string.IsNullOrEmpty(restakeItem.Item.Subtype))
            {
                return null;
            }

            restakeItem.AlreadyUsed = true;
            evaluationItem.AlreadyUsed = true;

            return new RewardTransaction()
            {
                BaseAmmount = evaluationItem.Item.Amount,
                BaseSymbol = SymbolFromKrakenString(evaluationItem.Item.Asset),
                Time = evaluationItem.Item.Time
            };
        }
    }
}
