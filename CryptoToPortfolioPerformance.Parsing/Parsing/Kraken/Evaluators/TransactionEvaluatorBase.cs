using CryptoTools.Common.Model;
using CryptoTools.Common.Model.Transactions;
using CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators.Models;
using System;
using System.Collections.Generic;

namespace CryptoToPortfolioPerformance.Parsing.Parsing.Kraken.Evaluators
{
    internal abstract class TransactionEvaluatorBase
    {
        private Dictionary<string, string> _krakenLedgerToSymbolMapping;
        internal TransactionEvaluatorBase(Dictionary<string, string> krakenLedgerToSymbolMappings)
        {
            _krakenLedgerToSymbolMapping = krakenLedgerToSymbolMappings;
        }
        public abstract TransactionBase Evaluate(ToBeEvaluatedItem evaluationItem, ref List<ToBeEvaluatedItem> allItems);

        protected CommonSymbol SymbolFromKrakenString(string val)
        {
            var symbol = "";
            if (_krakenLedgerToSymbolMapping.TryGetValue(val, out symbol))
            {
                return new CommonSymbol(symbol);
            }
            throw new ArgumentException($"Symbol '{val}' not found");

        }
    }
}
