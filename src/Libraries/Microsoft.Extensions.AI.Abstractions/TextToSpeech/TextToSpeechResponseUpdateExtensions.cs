// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="TextToSpeechResponseUpdate"/> instances.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public static class TextToSpeechResponseUpdateExtensions
{
    /// <summary>Combines <see cref="TextToSpeechResponseUpdate"/> instances into a single <see cref="TextToSpeechResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="TextToSpeechResponse"/>.</returns>
    public static TextToSpeechResponse ToTextToSpeechResponse(
        this IEnumerable<TextToSpeechResponseUpdate> updates)
    {
        _ = Throw.IfNull(updates);

        TextToSpeechResponse response = new();

        foreach (var update in updates)
        {
            ProcessUpdate(update, response);
        }

        return response;
    }

    /// <summary>Combines <see cref="TextToSpeechResponseUpdate"/> instances into a single <see cref="TextToSpeechResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="TextToSpeechResponse"/>.</returns>
    public static Task<TextToSpeechResponse> ToTextToSpeechResponseAsync(
        this IAsyncEnumerable<TextToSpeechResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToResponseAsync(updates, cancellationToken);

        static async Task<TextToSpeechResponse> ToResponseAsync(
            IAsyncEnumerable<TextToSpeechResponseUpdate> updates, CancellationToken cancellationToken)
        {
            TextToSpeechResponse response = new();

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, response);
            }

            return response;
        }
    }

    /// <summary>Processes the <see cref="TextToSpeechResponseUpdate"/>, incorporating its contents and properties.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="response">The <see cref="TextToSpeechResponse"/> object that should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(
        TextToSpeechResponseUpdate update,
        TextToSpeechResponse response)
    {
        if (update.ResponseId is not null)
        {
            response.ResponseId = update.ResponseId;
        }

        if (update.ModelId is not null)
        {
            response.ModelId = update.ModelId;
        }

        foreach (var content in update.Contents)
        {
            switch (content)
            {
                // Usage content is treated specially and propagated to the response's Usage.
                case UsageContent usage:
                    (response.Usage ??= new()).Add(usage.Details);
                    break;

                default:
                    response.Contents.Add(content);
                    break;
            }
        }

        if (update.AdditionalProperties is not null)
        {
            if (response.AdditionalProperties is null)
            {
                response.AdditionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                foreach (var entry in update.AdditionalProperties)
                {
                    response.AdditionalProperties[entry.Key] = entry.Value;
                }
            }
        }
    }
}
