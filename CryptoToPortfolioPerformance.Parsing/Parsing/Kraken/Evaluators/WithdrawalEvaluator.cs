using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators
{
    internal class WithdrawalEvaluator : TransactionEvaluatorBase
    {
        public WithdrawalEvaluator(Dictionary<string, string> krakenLedgerToSymbolMappings) : base(krakenLedgerToSymbolMappings)
        {
        }

        public override TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
        {
            ///  4. Withdrawal:
            /// - 2 x 'withdrawal' type, same ammount same refid
            var item = evaluationItem.Item;
            if (item.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
            {
                return null;
            }
            var secondItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item.Amount == item.Amount && i.Item != item);
            if (secondItem == null || secondItem.Item.Type != KrakenLedgerItemTransactionTypes.Withdrawal)
            {
                return null;
            }

            evaluationItem.AlreadyUsed = true;
            secondItem.AlreadyUsed = true;

            var transaction = new DepositWithdrawalTransaction(item.Amount, SymbolFromKrakenString(item.Asset), item.Time);
            return transaction;
        }
    }
}
