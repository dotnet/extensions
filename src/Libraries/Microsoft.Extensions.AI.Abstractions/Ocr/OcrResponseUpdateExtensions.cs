// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="OcrResponseUpdate"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public static class OcrResponseUpdateExtensions
{
    /// <summary>Combines <see cref="OcrResponseUpdate"/> instances into a single <see cref="OcrResult"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="OcrResult"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    public static OcrResult ToOcrResult(this IEnumerable<OcrResponseUpdate> updates)
    {
        _ = Throw.IfNull(updates);

        List<OcrPage> pages = [];
        OcrResult result = new(pages);

        foreach (var update in updates)
        {
            ProcessUpdate(update, pages, result);
        }

        return result;
    }

    /// <summary>Combines <see cref="OcrResponseUpdate"/> instances into a single <see cref="OcrResult"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="OcrResult"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    public static Task<OcrResult> ToOcrResultAsync(
        this IAsyncEnumerable<OcrResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToResultAsync(updates, cancellationToken);

        static async Task<OcrResult> ToResultAsync(
            IAsyncEnumerable<OcrResponseUpdate> updates, CancellationToken cancellationToken)
        {
            List<OcrPage> pages = [];
            OcrResult result = new(pages);

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, pages, result);
            }

            return result;
        }
    }

    /// <summary>Incorporates one <see cref="OcrResponseUpdate"/> into the assembled <see cref="OcrResult"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="pages">The accumulating list of pages backing <see cref="OcrResult.Pages"/>.</param>
    /// <param name="result">The <see cref="OcrResult"/> being assembled.</param>
    private static void ProcessUpdate(OcrResponseUpdate update, List<OcrPage> pages, OcrResult result)
    {
        if (update.Page is not null)
        {
            pages.Add(update.Page);
        }

        if (update.ModelId is not null)
        {
            result.ModelId = update.ModelId;
        }

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
