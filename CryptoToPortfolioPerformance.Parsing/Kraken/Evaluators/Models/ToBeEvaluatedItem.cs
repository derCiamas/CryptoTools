using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Kraken.Evaluators.Models
{
    internal class ToBeEvaluatedItem
    {
        public KrakenLedgerItem Item { get; private set; }
        public bool AlreadyUsed { get; set; }

        public ToBeEvaluatedItem(KrakenLedgerItem item)
        {
            Item = item;
            AlreadyUsed = false;
        }
    }
}
