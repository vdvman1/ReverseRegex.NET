using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool TryGetCheapCount<T>(this IEnumerable<T> enumerable, out int length)
        {
            length = 0;

            switch(enumerable)
            {
                case ICollection<T> collectionT:
                    length = collectionT.Count;
                    return true;
                case ICollection collection:
                    length = collection.Count;
                    return true;
                case IReadOnlyCollection<T> readonlyCollection:
                    length = readonlyCollection.Count;
                    return true;
            }

            return false;
        }
        public static string CodePointsToString(this IEnumerable<int> chars)
        {
            var builder = chars.TryGetCheapCount(out int count) ? new StringBuilder(count) : new StringBuilder();
            foreach (int c in chars)
            {
                builder.Append(char.ConvertFromUtf32(c));
            }

            return builder.ToString();
        }
    }
}
