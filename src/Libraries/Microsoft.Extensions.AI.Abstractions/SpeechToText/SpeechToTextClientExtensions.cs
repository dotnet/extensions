// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Extensions for <see cref="ISpeechToTextClient"/>.</summary>
[Experimental("MEAI001")]
public static class SpeechToTextClientExtensions
{
    /// <summary>Asks the <see cref="ISpeechToTextClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="ISpeechToTextClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this ISpeechToTextClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        return (TService?)client.GetService(typeof(TService), serviceKey);
    }

    /// <summary>Generates text from speech providing a single audio speech <see cref="DataContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="audioSpeechContent">The single audio speech content.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated by the client.</returns>
    public static async Task<SpeechToTextResponse> GetTextAsync(
        this ISpeechToTextClient client,
        DataContent audioSpeechContent,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(audioSpeechContent);

        using var audioSpeechStream = MemoryMarshal.TryGetArray(audioSpeechContent.Data, out var array) ?
            new MemoryStream(array.Array!, array.Offset, array.Count) :
            new MemoryStream(audioSpeechContent.Data.ToArray());

        return await client.GetTextAsync(audioSpeechStream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates text from speech providing a single audio speech <see cref="DataContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="audioSpeechContent">The single audio speech content.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated by the client.</returns>
    public static async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        this ISpeechToTextClient client,
        DataContent audioSpeechContent,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(audioSpeechContent);

        using var audioSpeechStream = MemoryMarshal.TryGetArray(audioSpeechContent.Data, out var array) ?
            new MemoryStream(array.Array!, array.Offset, array.Count) :
            new MemoryStream(audioSpeechContent.Data.ToArray());

        await foreach (var update in client.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }
}
