// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="ISpeechToTextClient"/> that passes through calls to another instance.
/// </summary>
/// <remarks>
/// This is recommended as a base type when building clients that can be chained in any order around an underlying <see cref="ISpeechToTextClient"/>.
/// The default implementation simply passes each call to the inner client instance.
/// </remarks>
[Experimental("MEAI001")]
public class DelegatingSpeechToTextClient : ISpeechToTextClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingSpeechToTextClient"/> class.
    /// </summary>
    /// <param name="innerClient">The wrapped client instance.</param>
    protected DelegatingSpeechToTextClient(ISpeechToTextClient innerClient)
    {
        InnerClient = Throw.IfNull(innerClient);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the inner <see cref="ISpeechToTextClient" />.</summary>
    protected ISpeechToTextClient InnerClient { get; }

    /// <inheritdoc />
    public virtual Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        return InnerClient.GetTextAsync(audioSpeechStream, options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        return InnerClient.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken);
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
