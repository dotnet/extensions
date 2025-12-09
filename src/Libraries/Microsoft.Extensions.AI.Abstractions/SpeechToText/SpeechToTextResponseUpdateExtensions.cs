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
/// Provides extension methods for working with <see cref="SpeechToTextResponseUpdate"/> instances.
/// </summary>
[Experimental(DiagnosticIds.Experiments.SpeechToText, UrlFormat = DiagnosticIds.UrlFormat, Message = DiagnosticIds.Experiments.SpeechToTextMessage)]
public static class SpeechToTextResponseUpdateExtensions
{
    /// <summary>Combines <see cref="SpeechToTextResponseUpdate"/> instances into a single <see cref="SpeechToTextResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="SpeechToTextResponse"/>.</returns>
    public static SpeechToTextResponse ToSpeechToTextResponse(
        this IEnumerable<SpeechToTextResponseUpdate> updates)
    {
        _ = Throw.IfNull(updates);

        SpeechToTextResponse response = new();

        foreach (var update in updates)
        {
            ProcessUpdate(update, response);
        }

        ChatResponseExtensions.CoalesceContent((List<AIContent>)response.Contents);

        return response;
    }

    /// <summary>Combines <see cref="SpeechToTextResponseUpdate"/> instances into a single <see cref="SpeechToTextResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="SpeechToTextResponse"/>.</returns>
    public static Task<SpeechToTextResponse> ToSpeechToTextResponseAsync(
        this IAsyncEnumerable<SpeechToTextResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToResponseAsync(updates, cancellationToken);

        static async Task<SpeechToTextResponse> ToResponseAsync(
            IAsyncEnumerable<SpeechToTextResponseUpdate> updates, CancellationToken cancellationToken)
        {
            SpeechToTextResponse response = new();

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, response);
            }

            ChatResponseExtensions.CoalesceContent((List<AIContent>)response.Contents);

            return response;
        }
    }

    /// <summary>Processes the <see cref="SpeechToTextResponseUpdate"/>, incorporating its contents and properties.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="response">The <see cref="SpeechToTextResponse"/> object that should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(
        SpeechToTextResponseUpdate update,
        SpeechToTextResponse response)
    {
        if (update.ResponseId is not null)
        {
            response.ResponseId = update.ResponseId;
        }

        if (update.ModelId is not null)
        {
            response.ModelId = update.ModelId;
        }

        if (response.StartTime is null || (update.StartTime is not null && update.StartTime < response.StartTime))
        {
            // Track the first start time provided by the updates
            response.StartTime = update.StartTime;
        }

        if (response.EndTime is null || (update.EndTime is not null && update.EndTime > response.EndTime))
        {
            // Track the last end time provided by the updates
            response.EndTime = update.EndTime;
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
