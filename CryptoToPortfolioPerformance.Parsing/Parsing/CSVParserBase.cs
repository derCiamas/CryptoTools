using CryptoTools.Common.Model.Transactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoTools.Parsing.Parsing
{
    public abstract class CSVParserBase
    {
        public abstract Task<IEnumerable<TransactionBase>> ParseFile(string filePath);
    }
}
