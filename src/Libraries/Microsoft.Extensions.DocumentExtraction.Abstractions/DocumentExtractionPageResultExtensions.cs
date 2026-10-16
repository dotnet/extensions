// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Provides explicit finalization helpers for streamed extraction pages.
/// </summary>
public static class DocumentExtractionPageResultExtensions
{
    /// <summary>
    /// Finalizes a synchronous page sequence.
    /// </summary>
    /// <returns>The finalized extraction result.</returns>
    public static DocumentExtractionResult ToDocumentExtractionResult(
        this IEnumerable<DocumentExtractionPage> pages,
        string identifier,
        bool captureRawOutput = true)
    {
        _ = Throw.IfNull(pages);
        using DocumentExtractionDocumentBuilder builder = new(identifier, captureRawOutput);
        foreach (DocumentExtractionPage page in pages)
        {
            builder.AddPage(page);
        }

        return builder.Build();
    }

    /// <summary>
    /// Finalizes an asynchronous page sequence and observes cancellation.
    /// </summary>
    /// <returns>A task producing the finalized extraction result.</returns>
    public static Task<DocumentExtractionResult> ToDocumentExtractionResultAsync(
        this IAsyncEnumerable<DocumentExtractionPage> pages,
        string identifier,
        CancellationToken cancellationToken = default)
        => ToDocumentExtractionResultAsync(pages, identifier, true, cancellationToken);

    /// <summary>Finalizes an asynchronous sequence using the selected raw-output retention policy.</summary>
    /// <returns>The finalized result.</returns>
    public static Task<DocumentExtractionResult> ToDocumentExtractionResultAsync(
        this IAsyncEnumerable<DocumentExtractionPage> pages,
        string identifier,
        bool captureRawOutput,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(pages);
        _ = Throw.IfNullOrEmpty(identifier);
        return BuildAsync(pages, identifier, captureRawOutput, cancellationToken);

        static async Task<DocumentExtractionResult> BuildAsync(
            IAsyncEnumerable<DocumentExtractionPage> pages,
            string identifier,
            bool captureRawOutput,
            CancellationToken cancellationToken)
        {
            using DocumentExtractionDocumentBuilder builder = new(identifier, captureRawOutput);
            cancellationToken.ThrowIfCancellationRequested();
            await foreach (DocumentExtractionPage page in pages
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                builder.AddPage(page);
                cancellationToken.ThrowIfCancellationRequested();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return builder.Build();
        }
    }
}
