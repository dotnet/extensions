// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="ITextToSpeechClient"/> that passes through calls to another instance.
/// </summary>
/// <remarks>
/// This is recommended as a base type when building clients that can be chained in any order around an underlying <see cref="ITextToSpeechClient"/>.
/// The default implementation simply passes each call to the inner client instance.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public class DelegatingTextToSpeechClient : ITextToSpeechClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingTextToSpeechClient"/> class.
    /// </summary>
    /// <param name="innerClient">The wrapped client instance.</param>
    protected DelegatingTextToSpeechClient(ITextToSpeechClient innerClient)
    {
        InnerClient = Throw.IfNull(innerClient);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the inner <see cref="ITextToSpeechClient" />.</summary>
    protected ITextToSpeechClient InnerClient { get; }

    /// <inheritdoc />
    public virtual Task<TextToSpeechResponse> GetAudioAsync(
        string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return InnerClient.GetAudioAsync(text, options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(
        string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return InnerClient.GetStreamingAudioAsync(text, options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerClient.GetService(serviceType, serviceKey);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerClient.Dispose();
        }
    }
}
