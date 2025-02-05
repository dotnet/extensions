// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="IAsyncEnumerable{T}"/>.</summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>Converts an <see cref="IAsyncEnumerable{T}"/> where <typeparamref name="T"/> is a <see cref="DataContent"/> to a <see cref="Stream"/>.</summary>
    /// <param name="dataAsyncEnumerable">The data content async enumerable to convert.</param>
    /// <param name="firstDataContent">The first data content chunk to write back to the stream.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <typeparam name="T">The type of <see cref="DataContent"/> to return.</typeparam>
    /// <returns>The stream containing the data content.</returns>
    /// <remarks>
    /// <paramref name="firstDataContent"/> needs to be considered back in the stream if <paramref name="dataAsyncEnumerable"/> was iterated before creating the stream.
    /// This can happen to check if the first enumerable item contains data or is just a reference only content.
    /// </remarks>
    public static Stream ToStream<T>(this IAsyncEnumerable<T> dataAsyncEnumerable, T? firstDataContent = null, CancellationToken cancellationToken = default)
        where T : DataContent
        => new DataContentAsyncEnumerableStream<T>(Throw.IfNull(dataAsyncEnumerable), firstDataContent, cancellationToken);
}
