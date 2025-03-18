﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

    /// <summary>Generates text from speech providing a single speech audio <see cref="DataContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="speechContent">The single speech audio content.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated by the client.</returns>
    public static Task<SpeechToTextResponse> GetResponseAsync(
        this ISpeechToTextClient client,
        DataContent speechContent,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<DataContent> speechContents = [Throw.IfNull(speechContent)];
        return Throw.IfNull(client)
            .TranscribeAudioAsync(
                [speechContents.ToAsyncEnumerable()],
                options,
                cancellationToken);
    }

    /// <summary>Generates text from speech providing a single speech audio <see cref="Stream"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="speechStream">The single speech audio stream.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated by the client.</returns>
    public static Task<SpeechToTextResponse> GetResponseAsync(
        this ISpeechToTextClient client,
        Stream speechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => Throw.IfNull(client)
            .TranscribeAudioAsync(
                [speechStream.ToAsyncEnumerable(cancellationToken: cancellationToken)],
                options,
                cancellationToken);

    /// <summary>Generates text from speech providing a single speech audio <see cref="DataContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="speechStream">The single speech audio stream.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated by the client.</returns>
    public static IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsync(
        this ISpeechToTextClient client,
        Stream speechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => Throw.IfNull(client)
            .TranscribeStreamingAudioAsync(
                [speechStream.ToAsyncEnumerable(cancellationToken: cancellationToken)],
                options,
                cancellationToken);

    /// <summary>Generates text from speech providing a single speech audio <see cref="DataContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="speechContent">The single speech audio content.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated by the client.</returns>
    public static IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsync(
        this ISpeechToTextClient client,
        DataContent speechContent,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<DataContent> speechContents = [Throw.IfNull(speechContent)];
        return Throw.IfNull(client)
            .TranscribeStreamingAudioAsync(
                [speechContents.ToAsyncEnumerable()],
                options,
                cancellationToken);
    }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
    }
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Unused private types or members should be removed
}
