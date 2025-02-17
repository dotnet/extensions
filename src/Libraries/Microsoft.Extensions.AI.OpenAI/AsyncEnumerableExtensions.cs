// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="IAsyncEnumerable{T}"/>.</summary>
internal static class AsyncEnumerableExtensions
{
    /// <summary>Converts an <see cref="IAsyncEnumerable{DataContent}"/> to a <see cref="Stream"/>.</summary>
    /// <param name="dataAsyncEnumerable">The data content async enumerable to convert.</param>
    /// <param name="firstDataContent">The first data content chunk to write back to the stream.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The stream containing the data content.</returns>
    /// <remarks>
    /// <paramref name="firstDataContent"/> needs to be considered back in the stream if <paramref name="dataAsyncEnumerable"/> was iterated before creating the stream.
    /// This can happen to check if the first enumerable item contains data or is just a reference only content.
    /// </remarks>
    internal static Stream ToStream(this IAsyncEnumerable<DataContent> dataAsyncEnumerable, DataContent? firstDataContent = null, CancellationToken cancellationToken = default)
        => new DataContentAsyncEnumerableStream(Throw.IfNull(dataAsyncEnumerable), firstDataContent, cancellationToken);
}
