using System.Collections.Generic;
using System.Linq;
using DynamicData.Kernel;

namespace Wavee.UI.Extensions;

internal static class ObservableExtensions
{
    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        T[] bucket = null;
        var count = 0;

        foreach (var item in source)
        {
            if (bucket == null)
                bucket = new T[size];

            bucket[count++] = item;

            if (count != size)
                continue;

            yield return bucket.Select(x => x).AsList();

            bucket = null;
            count = 0;
        }

        // Return the last bucket with all remaining items
        if (bucket != null && count > 0)
            yield return bucket.Take(count).AsList();
    }
}