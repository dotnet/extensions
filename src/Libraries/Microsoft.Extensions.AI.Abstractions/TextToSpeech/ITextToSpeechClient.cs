// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a text to speech client.</summary>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="ITextToSpeechClient"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="ITextToSpeechClient"/> support being used by multiple requests concurrently.
/// </para>
/// <para>
/// However, implementations of <see cref="ITextToSpeechClient"/> might mutate the arguments supplied to <see cref="GetAudioAsync"/> and
/// <see cref="GetStreamingAudioAsync"/>, such as by configuring the options instance. Thus, consumers of the interface either should avoid
/// using shared instances of these arguments for concurrent invocations or should otherwise ensure by construction that no
/// <see cref="ITextToSpeechClient"/> instances are used which might employ such mutation. For example, the ConfigureOptions method may be
/// provided with a callback that could mutate the supplied options argument, and that should be avoided if using a singleton options instance.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public interface ITextToSpeechClient : IDisposable
{
    /// <summary>Sends text content to the model and returns the generated audio speech.</summary>
    /// <param name="text">The text to synthesize into speech.</param>
    /// <param name="options">The text to speech options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The audio speech generated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    Task<TextToSpeechResponse> GetAudioAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends text content to the model and streams back the generated audio speech.</summary>
    /// <param name="text">The text to synthesize into speech.</param>
    /// <param name="options">The text to speech options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The audio speech updates representing the streamed output.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="ITextToSpeechClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the <see cref="ITextToSpeechClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
