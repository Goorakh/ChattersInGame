using System;
using System.Collections.Generic;

namespace ChattersInGame
{
    public static class CollectionExtensions
    {
        public static T SelectMin<T>(this IEnumerable<T> collection, Func<T, int> valueSelector)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (valueSelector is null)
                throw new ArgumentNullException(nameof(valueSelector));

            T minValue = default;
            long minComparerValue = long.MaxValue;
            bool hasAnyValue = false;

            foreach (T item in collection)
            {
                int comparerValue = valueSelector(item);
                if (comparerValue < minComparerValue)
                {
                    minValue = item;
                    minComparerValue = comparerValue;
                    hasAnyValue = true;
                }
            }

            if (!hasAnyValue)
            {
                throw new ArgumentException("Collection was empty");
            }

            return minValue;
        }
    }
}
