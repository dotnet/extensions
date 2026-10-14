// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Produces provider-neutral page observations from a document stream.
/// </summary>
/// <remarks>
/// Implementations do not own or dispose the source stream. They must honor the supplied cancellation token
/// and dispose any provider resources when the returned async enumerator is disposed.
/// </remarks>
public interface IDocumentExtractionClient : IDisposable
{
    /// <summary>
    /// Streams extracted pages.
    /// </summary>
    /// <param name="document">The source document stream, which remains owned by the caller.</param>
    /// <param name="identifier">The identifier for the eventual Documents document.</param>
    /// <param name="mediaType">The source media type.</param>
    /// <param name="options">Optional per-call overrides. Providers must reject unsupported explicit settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted pages.</returns>
    IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
        Stream document,
        string identifier,
        string mediaType,
        DocumentExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an optional service exposed by this client.
    /// </summary>
    /// <param name="serviceType">The requested service type.</param>
    /// <param name="serviceKey">An optional service key.</param>
    /// <returns>The service, or <see langword="null"/>.</returns>
    object? GetService(Type serviceType, object? serviceKey = null);
}
