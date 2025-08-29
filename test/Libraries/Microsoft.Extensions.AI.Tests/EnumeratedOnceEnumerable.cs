// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.AI;

internal sealed class EnumeratedOnceEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
{
    private int _iterated;

    public IEnumerator<T> GetEnumerator()
    {
        if (Interlocked.Exchange(ref _iterated, 1) != 0)
        {
            throw new InvalidOperationException("This enumerable can only be enumerated once.");
        }

        foreach (var item in items)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
