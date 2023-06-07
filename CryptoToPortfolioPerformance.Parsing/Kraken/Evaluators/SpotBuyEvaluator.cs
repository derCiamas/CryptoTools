using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators
{
    internal class SpotBuyEvaluator : TransactionEvaluatorBase
    {
        public SpotBuyEvaluator(Dictionary<string, string> krakenLedgerToSymbolMappings) : base(krakenLedgerToSymbolMappings)
        {
        }

        public override TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
        {
            /// 3. Spot buy:
            /// - 'spend' type same refid
            /// - 'receive' type
            /// </summary>
            var item = evaluationItem.Item;
            if (item.Type != KrakenLedgerItemTransactionTypes.Spend)
            {
                return null;
            }
            var secondItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item != item);
            if (secondItem == null || secondItem.Item.Type != KrakenLedgerItemTransactionTypes.Receive)
            {
                return null;
            }

            evaluationItem.AlreadyUsed = true;
            secondItem.AlreadyUsed = true;

            return new BuySellTransaction()
            {
                BaseAmmount = secondItem.Item.Amount,
                BaseSymbol = SymbolFromKrakenString(secondItem.Item.Asset),
                QuotedAmmount = evaluationItem.Item.Amount,
                QuotedSymbol = SymbolFromKrakenString(evaluationItem.Item.Asset),
                Time = secondItem.Item.Time
            };
        }
    }
}
