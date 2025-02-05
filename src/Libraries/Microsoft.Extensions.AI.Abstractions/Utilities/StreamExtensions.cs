// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Shared.Diagnostics;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="Stream"/>.</summary>
public static class StreamExtensions
{
    /// <summary>Converts a <see cref="Stream"/> to an <see cref="IAsyncEnumerable{T}"/> where <typeparamref name="T"/> is a <see cref="DataContent"/>.</summary>
    /// <param name="audioStream">The audio stream to convert.</param>
    /// <param name="mediaType">The optional media type of the audio stream.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <typeparam name="T">The type of <see cref="DataContent"/> to return.</typeparam>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <typeparamref name="T"/> where <typeparamref name="T"/> is a <see cref="DataContent"/>.</returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this Stream audioStream,
        string? mediaType = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : DataContent
    {
        _ = Throw.IfNull(audioStream);

#if NET8_0_OR_GREATER
        Memory<byte> buffer = new byte[4096];
        while ((await audioStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
#else
        var buffer = new byte[4096];
        while ((await audioStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
#endif
        {
            yield return (T)Activator.CreateInstance(typeof(T), [(ReadOnlyMemory<byte>)buffer, mediaType])!;
        }
    }
}
