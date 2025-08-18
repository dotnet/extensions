// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a speech to text client.</summary>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="ISpeechToTextClient"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="ISpeechToTextClient"/> support being used by multiple requests concurrently.
/// </para>
/// <para>
/// However, implementations of <see cref="ISpeechToTextClient"/> might mutate the arguments supplied to <see cref="GetTextAsync"/> and
/// <see cref="GetStreamingTextAsync"/>, such as by configuring the options instance. Thus, consumers of the interface either should avoid
/// using shared instances of these arguments for concurrent invocations or should otherwise ensure by construction that no
/// <see cref="ISpeechToTextClient"/> instances are used which might employ such mutation. For example, the ConfigureOptions method be
/// provided with a callback that could mutate the supplied options argument, and that should be avoided if using a singleton options instance.
/// The audio speech stream passed to these methods will not be closed or disposed by the implementation.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public interface ISpeechToTextClient : IDisposable
{
    /// <summary>Sends audio speech content to the model and returns the generated text.</summary>
    /// <param name="audioSpeechStream">The audio speech stream to send.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text generated.</returns>
    Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends audio speech content to the model and streams back the generated text.</summary>
    /// <param name="audioSpeechStream">The audio speech stream to send.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The text updates representing the streamed output.</returns>
    IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="ISpeechToTextClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the <see cref="ISpeechToTextClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
