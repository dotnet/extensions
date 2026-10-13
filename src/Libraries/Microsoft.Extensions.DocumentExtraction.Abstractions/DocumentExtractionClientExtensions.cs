// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Provides convenience methods for document extraction clients.
/// </summary>
public static class DocumentExtractionClientExtensions
{
    /// <summary>
    /// Extracts and finalizes a document from a stream.
    /// </summary>
    /// <returns>A task producing the finalized extraction result.</returns>
    public static Task<DocumentExtractionResult> ExtractAsync(
        this IDocumentExtractionClient client,
        Stream document,
        string identifier,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(document);
        _ = Throw.IfNullOrEmpty(identifier);
        _ = Throw.IfNullOrEmpty(mediaType);
        return client.ExtractPagesAsync(document, identifier, mediaType, cancellationToken)
            .ToDocumentExtractionResultAsync(identifier, cancellationToken);
    }
}
