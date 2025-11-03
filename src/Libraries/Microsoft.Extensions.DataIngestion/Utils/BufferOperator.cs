// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

internal static class BufferOperator
{
    // Code copied from https://github.com/dotnet/reactive/blob/ddf18469a0d9e02fcabe9f606104c81c5822839b/Ix.NET/Source/System.Interactive.Async/System/Linq/Operators/Buffer.cs#L14
    internal static IAsyncEnumerable<IList<TSource>> BufferAsync<TSource>(this IAsyncEnumerable<TSource> source, int count)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfLessThanOrEqual(count, 0);

        return CoreAsync(source, count);

        static async IAsyncEnumerable<IList<TSource>> CoreAsync(IAsyncEnumerable<TSource> source, int count,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            List<TSource> buffer = new(count);

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                buffer.Add(item);

                if (buffer.Count == count)
                {
                    yield return buffer;

                    buffer = new List<TSource>(count);
                }
            }

            if (buffer.Count > 0)
            {
                yield return buffer;
            }
        }
    }
}
