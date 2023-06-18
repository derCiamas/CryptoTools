using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CryptoToPortfolioPerformance.Parsing.Algorand.Exceptions
{
    public class UnknownTransactionTypeException : Exception
    {
        public UnknownTransactionTypeException()
        {
        }

        public UnknownTransactionTypeException(string message) : base(message)
        {
        }

        public UnknownTransactionTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownTransactionTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
