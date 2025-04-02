// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="SpeechToTextResponseUpdate"/> instances.
/// </summary>
[Experimental("MEAI001")]
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
        List<AIContent> contents = [];
        string? responseId = null;
        string? modelId = null;
        AdditionalPropertiesDictionary? additionalProperties = null;

        TimeSpan? endTime = null;
        foreach (var update in updates)
        {
            // Track the first start time provided by the updates
            response.StartTime ??= update.StartTime;

            // Track the last end time provided by the updates
            if (update.EndTime is not null)
            {
                endTime = update.EndTime;
            }

            ProcessUpdate(update, contents, ref responseId, ref modelId, ref additionalProperties);
        }

        ChatResponseExtensions.CoalesceTextContent(contents);
        response.EndTime = endTime;
        response.Contents = contents;
        response.ResponseId = responseId;
        response.ModelId = modelId;
        response.AdditionalProperties = additionalProperties;

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
            List<AIContent> contents = [];
            string? responseId = null;
            string? modelId = null;
            AdditionalPropertiesDictionary? additionalProperties = null;

            TimeSpan? endTime = null;
            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Track the first start time provided by the updates
                response.StartTime ??= update.StartTime;

                // Track the last end time provided by the updates
                if (update.EndTime is not null)
                {
                    endTime = update.EndTime;
                }

                ProcessUpdate(update, contents, ref responseId, ref modelId, ref additionalProperties);
            }

            ChatResponseExtensions.CoalesceTextContent(contents);

            response.EndTime = endTime;
            response.Contents = contents;
            response.ResponseId = responseId;
            response.ModelId = modelId;
            response.AdditionalProperties = additionalProperties;

            return response;
        }
    }

    /// <summary>Processes the <see cref="SpeechToTextResponseUpdate"/>, incorporating its contents and properties.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="contents">The list of content items being accumulated.</param>
    /// <param name="responseId">The response ID to update if the update has one.</param>
    /// <param name="modelId">The model ID to update if the update has one.</param>
    /// <param name="additionalProperties">The additional properties to update if the update has any.</param>
    private static void ProcessUpdate(
        SpeechToTextResponseUpdate update,
        List<AIContent> contents,
        ref string? responseId,
        ref string? modelId,
        ref AdditionalPropertiesDictionary? additionalProperties)
    {
        if (update.ResponseId is not null)
        {
            responseId = update.ResponseId;
        }

        if (update.ModelId is not null)
        {
            modelId = update.ModelId;
        }

        contents.AddRange(update.Contents);

        if (update.AdditionalProperties is not null)
        {
            if (additionalProperties is null)
            {
                additionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                foreach (var entry in update.AdditionalProperties)
                {
                    additionalProperties[entry.Key] = entry.Value;
                }
            }
        }
    }
}
