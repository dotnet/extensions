// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

// Once .NET 10 is shipped, we are going to switch to System.Linq.AsyncEnumerable.
internal static class IAsyncEnumerableExtensions
{
    internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (T item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }

    internal static async ValueTask<int> CountAsync<T>(this IAsyncEnumerable<T> source)
    {
        int count = 0;
        await foreach (T _ in source)
        {
            count++;
        }

        return count;
    }

    internal static async ValueTask<T> SingleAsync<T>(this IAsyncEnumerable<T> source)
    {
        bool found = false;
        T result = default!;
        await foreach (T item in source)
        {
            if (found)
            {
                throw new InvalidOperationException();
            }

            result = item;
            found = true;
        }

        return found
            ? result
            : throw new InvalidOperationException();
    }

    internal static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        List<T> list = [];
        await foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }
}
