// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        bool captureRawContent = false)
    {
        _ = Throw.IfNull(pages);
        using DocumentExtractionDocumentBuilder builder = new(identifier, captureRawContent);
        try
        {
            foreach (DocumentExtractionPage page in pages)
            {
                builder.AddPage(page);
            }

            return builder.Build();
        }
        catch (Exception failure)
        {
            DisposeAfterFailure(builder, failure);
            throw;
        }
    }

    /// <summary>
    /// Finalizes an asynchronous page sequence and observes cancellation.
    /// </summary>
    /// <returns>A task producing the finalized extraction result.</returns>
    public static Task<DocumentExtractionResult> ToDocumentExtractionResultAsync(
        this IAsyncEnumerable<DocumentExtractionPage> pages,
        string identifier,
        CancellationToken cancellationToken = default)
        => ToDocumentExtractionResultAsync(pages, identifier, false, cancellationToken);

    /// <summary>Finalizes an asynchronous sequence using the selected original response-content retention policy.</summary>
    /// <returns>The finalized result.</returns>
    public static Task<DocumentExtractionResult> ToDocumentExtractionResultAsync(
        this IAsyncEnumerable<DocumentExtractionPage> pages,
        string identifier,
        bool captureRawContent,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(pages);
        _ = Throw.IfNullOrEmpty(identifier);
        return BuildAsync(pages, identifier, captureRawContent, cancellationToken);

        static async Task<DocumentExtractionResult> BuildAsync(
            IAsyncEnumerable<DocumentExtractionPage> pages,
            string identifier,
            bool captureRawContent,
            CancellationToken cancellationToken)
        {
            using DocumentExtractionDocumentBuilder builder = new(identifier, captureRawContent);
            try
            {
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
            catch (Exception failure)
            {
                DisposeAfterFailure(builder, failure);
                throw;
            }
        }
    }

    private static void DisposeAfterFailure(DocumentExtractionDocumentBuilder builder, Exception failure)
    {
        try
        {
            builder.Dispose();
        }
        catch (AggregateException cleanupFailure)
        {
            List<Exception> failures = [failure];
            failures.AddRange(cleanupFailure.InnerExceptions);
            throw new AggregateException(failures);
        }
    }
}
