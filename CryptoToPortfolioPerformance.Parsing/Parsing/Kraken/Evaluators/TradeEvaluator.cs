using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators
{
    internal class TradeEvaluator : TransactionEvaluatorBase
    {
        public TradeEvaluator(Dictionary<string, string> krakenLedgerToSymbolMappings) : base(krakenLedgerToSymbolMappings)
        {
        }

        public override TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems)
        {
            /// - 2 x 'trade' type same refid
            /// - 1st => what is being spent
            /// - 2nd => what is being received
            var item = evaluationItem.Item;
            if (item.Type != KrakenLedgerItemTransactionTypes.Trade)
            {
                return null;
            }
            var secondItem = allItems.SingleOrDefault(i => !i.AlreadyUsed && i.Item.RefId == item.RefId && i.Item != item);
            if (secondItem == null || secondItem.Item.Type != KrakenLedgerItemTransactionTypes.Trade)
            {
                return null;
            }
            var pair = new List<ToBeEvaluatedItem>() { evaluationItem, secondItem };
            var baseAsset = pair.SingleOrDefault(i => i.Item.Amount > 0);
            var quotedAsset = pair.SingleOrDefault(e => e != baseAsset);

            baseAsset.AlreadyUsed = true;
            quotedAsset.AlreadyUsed = true;

            var feeSymbol = secondItem.Item.Fee > 0 ? SymbolFromKrakenString(secondItem.Item.Asset) : SymbolFromKrakenString(item.Asset);
            var fee = secondItem.Item.Fee > 0 ? secondItem.Item.Fee : item.Fee;
            return new BuySellTransaction()
            {
                BaseAmmount = baseAsset.Item.Amount,
                BaseSymbol = SymbolFromKrakenString(baseAsset.Item.Asset),
                QuotedAmmount = quotedAsset.Item.Amount,
                QuotedSymbol = SymbolFromKrakenString(quotedAsset.Item.Asset),
                Time = baseAsset.Item.Time,
                //Theoretically the secondItem should contain Fee, wasn't able to find the documentation for Ledgers.csv so doing this check
                Fee = fee,
                FeeSymbol = feeSymbol
            };
        }
    }
}
