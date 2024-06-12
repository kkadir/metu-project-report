using System;
using System.Collections.Generic;
using System.Linq;


namespace Metu.MarketData.Client.Extensions;

public static class EnumerableExtensions
{

    public static Random randomizer = new Random();

    public static IEnumerable<T> GetRandom<T>(this IEnumerable<T> list, int numItems)
    {
        return (list as T[] ?? list.ToArray()).GetRandom(numItems);
    }

    // just because the parentheses were getting confusing
    public static IEnumerable<T> GetRandom<T>(this T[] list, int numItems)
    {
        var items = new HashSet<T>();
        while (numItems > 0)
            // if we successfully added it, move on
            if (items.Add(list[randomizer.Next(list.Length)])) numItems--;

        return items;
    }

    public static IEnumerable<T> PluckRandomly<T>(this IEnumerable<T> list)
    {
        while (true)
            yield return list.ElementAt(randomizer.Next(list.Count()));
    }

}
