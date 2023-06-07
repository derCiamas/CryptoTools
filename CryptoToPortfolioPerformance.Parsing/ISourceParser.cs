using CryptoTools.Common.Model.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing
{
    public interface ISourceParser
    {
        Task<IEnumerable<TransactionBase>> ParseUnderlyingSource();
    }
}
