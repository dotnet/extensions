// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Provides extension methods for working with <see cref="DocumentExtractionPageResult"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DocumentExtractionPageResultExtensions
{
    /// <summary>Combines <see cref="DocumentExtractionPageResult"/> instances into a single <see cref="DocumentExtractionResult"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="DocumentExtractionResult"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    public static DocumentExtractionResult ToDocumentExtractionResult(this IEnumerable<DocumentExtractionPageResult> updates)
    {
        _ = Throw.IfNull(updates);

        List<DocumentPage> pages = [];
        DocumentExtractionResult result = new(pages);

        foreach (var update in updates)
        {
            ProcessUpdate(update, pages, result);
        }

        return result;
    }

    /// <summary>Combines <see cref="DocumentExtractionPageResult"/> instances into a single <see cref="DocumentExtractionResult"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="DocumentExtractionResult"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    public static Task<DocumentExtractionResult> ToDocumentExtractionResultAsync(
        this IAsyncEnumerable<DocumentExtractionPageResult> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToResultAsync(updates, cancellationToken);

        static async Task<DocumentExtractionResult> ToResultAsync(
            IAsyncEnumerable<DocumentExtractionPageResult> updates, CancellationToken cancellationToken)
        {
            List<DocumentPage> pages = [];
            DocumentExtractionResult result = new(pages);

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, pages, result);
            }

            return result;
        }
    }

    /// <summary>Incorporates one <see cref="DocumentExtractionPageResult"/> into the assembled <see cref="DocumentExtractionResult"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="pages">The accumulating list of pages backing <see cref="DocumentExtractionResult.Pages"/>.</param>
    /// <param name="result">The <see cref="DocumentExtractionResult"/> being assembled.</param>
    private static void ProcessUpdate(DocumentExtractionPageResult update, List<DocumentPage> pages, DocumentExtractionResult result)
    {
        pages.Add(update.Page);

        if (update.Usage is not null)
        {
            result.Usage = update.Usage;
        }

        if (update.AdditionalProperties is not null)
        {
            if (result.AdditionalProperties is null)
            {
                result.AdditionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                foreach (var entry in update.AdditionalProperties)
                {
                    result.AdditionalProperties[entry.Key] = entry.Value;
                }
            }
        }
    }
}
