using System;

static class Arrays
{
    public static T[] FindBest<T>(T[] items, int topN, Func<T, T, int> comparer)
    {
        var best = new T[topN];
        var picked = new bool[items.Length];
        for (var i = 0; i < best.Length; i++)
        {
            // For each slot of best[], loop through Population
            // and assign the top value. Store that index in the bool
            // array, and use that to skip previously found values.
            var id = 0;
            for (var k = 0; k < items.Length; k++)
            {
                if (!picked[k] && (best[i] == null || comparer(items[k], best[i]) > 0))
                {
                    best[i] = items[k];
                    // Track the last index k
                    id = k;
                }
            }
            picked[id] = true;
        }
        return best;
    }
}