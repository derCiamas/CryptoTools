using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoTools.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> WherePredicateButNotItem<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, T item)
        {
            return enumerable
                    .Where(predicate)
                    .Where(e => !e.Equals(item));
        }
    }
}
