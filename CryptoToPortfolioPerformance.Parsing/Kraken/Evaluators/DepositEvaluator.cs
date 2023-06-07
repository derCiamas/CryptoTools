using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators
{
    internal class DepositEvaluator : TransactionEvaluatorBase
    {
        public DepositEvaluator(Dictionary<string, string> krakenLedgerToSymbolMappings) : base(krakenLedgerToSymbolMappings)
        {
        }

        public override TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
        {
            /// Deposit: 2 x 'deposit' type, same ammount, same refid
            var item = evaluationItem.Item;
            if (item.Type != KrakenLedgerItemTransactionTypes.Deposit)
            {
                return null;
            }
            var otherItems = allItems.Where(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item != item);
            if (otherItems.Count() != 1 || otherItems.FirstOrDefault()?.Item.Type != KrakenLedgerItemTransactionTypes.Deposit)
            {
                return null;
            }
            evaluationItem.AlreadyUsed = true;
            otherItems.ToList().ForEach(e => e.AlreadyUsed = true);
            var transaction = new DepositWithdrawalTransaction(
                    item.Amount,
                    SymbolFromKrakenString(item.Asset),
                    item.Time
                );
            return transaction;
        }
    }
}
