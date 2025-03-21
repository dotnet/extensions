// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if NET
using System.Runtime.InteropServices;
#endif
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
    /// <param name="coalesceContent">
    /// <see langword="true"/> to attempt to coalesce contiguous <see cref="AIContent"/> items, where applicable,
    /// into a single <see cref="AIContent"/>, in order to reduce the number of individual content items that are included in
    /// the manufactured <see cref="SpeechToTextMessage"/> instances. When <see langword="false"/>, the original content items are used.
    /// The default is <see langword="true"/>.
    /// </param>
    /// <returns>The combined <see cref="SpeechToTextResponse"/>.</returns>
    public static SpeechToTextResponse ToSpeechToTextResponse(
        this IEnumerable<SpeechToTextResponseUpdate> updates, bool coalesceContent = true)
    {
        _ = Throw.IfNull(updates);

        SpeechToTextResponse response = new([]);
        Dictionary<int, SpeechToTextMessage> choices = [];

        foreach (var update in updates)
        {
            ProcessUpdate(update, choices, response);
        }

        AddChoicesToCompletion(choices, response, coalesceContent);

        return response;
    }

    /// <summary>Combines <see cref="SpeechToTextResponseUpdate"/> instances into a single <see cref="SpeechToTextResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="coalesceContent">
    /// <see langword="true"/> to attempt to coalesce contiguous <see cref="AIContent"/> items, where applicable,
    /// into a single <see cref="AIContent"/>, in order to reduce the number of individual content items that are included in
    /// the manufactured <see cref="SpeechToTextMessage"/> instances. When <see langword="false"/>, the original content items are used.
    /// The default is <see langword="true"/>.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="SpeechToTextResponse"/>.</returns>
    public static Task<SpeechToTextResponse> ToSpeechToTextResponseAsync(
        this IAsyncEnumerable<SpeechToTextResponseUpdate> updates, bool coalesceContent = true, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToResponseAsync(updates, coalesceContent, cancellationToken);

        static async Task<SpeechToTextResponse> ToResponseAsync(
            IAsyncEnumerable<SpeechToTextResponseUpdate> updates, bool coalesceContent, CancellationToken cancellationToken)
        {
            SpeechToTextResponse response = new([]);
            Dictionary<int, SpeechToTextMessage> choices = [];

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, choices, response);
            }

            AddChoicesToCompletion(choices, response, coalesceContent);

            return response;
        }
    }

    /// <summary>Processes the <see cref="SpeechToTextResponseUpdate"/>, incorporating its contents into <paramref name="choices"/> and <paramref name="response"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="choices">The dictionary mapping <see cref="SpeechToTextResponseUpdate.ChoiceIndex"/> to the <see cref="SpeechToTextMessage"/> being built for that choice.</param>
    /// <param name="response">The <see cref="SpeechToTextResponse"/> object whose properties should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(SpeechToTextResponseUpdate update, Dictionary<int, SpeechToTextMessage> choices, SpeechToTextResponse response)
    {
        if (update.ResponseId is not null)
        {
            response.ResponseId = update.ResponseId;
        }

        if (update.ModelId is not null)
        {
            response.ModelId = update.ModelId;
        }

#if NET
        SpeechToTextMessage choice = CollectionsMarshal.GetValueRefOrAddDefault(choices, update.ChoiceIndex, out _) ??=
            new(new List<AIContent>());
#else
        if (!choices.TryGetValue(update.ChoiceIndex, out SpeechToTextMessage? choice))
        {
            choices[update.ChoiceIndex] = choice = new(new List<AIContent>());
        }
#endif

        ((List<AIContent>)choice.Contents).AddRange(update.Contents);

        if (update.AdditionalProperties is not null)
        {
            if (choice.AdditionalProperties is null)
            {
                choice.AdditionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                foreach (var entry in update.AdditionalProperties)
                {
                    choice.AdditionalProperties[entry.Key] = entry.Value;
                }
            }
        }
    }

    /// <summary>Finalizes the <paramref name="response"/> object by transferring the <paramref name="choices"/> into it.</summary>
    /// <param name="choices">The messages to process further and transfer into <paramref name="response"/>.</param>
    /// <param name="response">The result <see cref="SpeechToTextResponse"/> being built.</param>
    /// <param name="coalesceContent">The corresponding option value provided to <see cref="ToSpeechToTextResponse"/> or <see cref="ToSpeechToTextResponseAsync"/>.</param>
    private static void AddChoicesToCompletion(Dictionary<int, SpeechToTextMessage> choices, SpeechToTextResponse response, bool coalesceContent)
    {
        if (choices.Count <= 1)
        {
            // Add the single message if there is one.
            foreach (var entry in choices)
            {
                AddChoice(response, coalesceContent, entry);
            }

            // In the vast majority case where there's only one choice, promote any additional properties
            // from the single choice to the speech to text response, making them more discoverable and more similar
            // to how they're typically surfaced from non-streaming services.
            if (response.Choices.Count == 1 &&
                response.Choices[0].AdditionalProperties is { } messageProps)
            {
                response.Choices[0].AdditionalProperties = null;
                response.AdditionalProperties = messageProps;
            }
        }
        else
        {
            // Add all of the messages, sorted by choice index.
            foreach (var entry in choices.OrderBy(entry => entry.Key))
            {
                AddChoice(response, coalesceContent, entry);
            }

            // If there are multiple choices, we don't promote additional properties from the individual messages.
            // At a minimum, we'd want to know which choice the additional properties applied to, and if there were
            // conflicting values across the choices, it would be unclear which one should be used.
        }

        static void AddChoice(SpeechToTextResponse response, bool coalesceContent, KeyValuePair<int, SpeechToTextMessage> entry)
        {
            if (coalesceContent)
            {
                // Consider moving to a utility method.
                ChatResponseExtensions.CoalesceTextContent((List<AIContent>)entry.Value.Contents);
            }

            response.Choices.Add(entry.Value);
        }
    }
}
