// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
#if NET
using System.Runtime.InteropServices;
#endif
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="AudioTranscriptionResponseUpdate"/> instances.
/// </summary>
public static class AudioTranscriptionResponseUpdateExtensions
{
    /// <summary>Combines <see cref="AudioTranscriptionResponseUpdate"/> instances into a single <see cref="AudioTranscriptionResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="coalesceContent">
    /// <see langword="true"/> to attempt to coalesce contiguous <see cref="AIContent"/> items, where applicable,
    /// into a single <see cref="AIContent"/>, in order to reduce the number of individual content items that are included in
    /// the manufactured <see cref="AudioTranscription"/> instances. When <see langword="false"/>, the original content items are used.
    /// The default is <see langword="true"/>.
    /// </param>
    /// <returns>The combined <see cref="AudioTranscriptionResponse"/>.</returns>
    public static AudioTranscriptionResponse ToAudioTranscriptionCompletion(
        this IEnumerable<AudioTranscriptionResponseUpdate> updates, bool coalesceContent = true)
    {
        _ = Throw.IfNull(updates);

        AudioTranscriptionResponse completion = new([]);
        Dictionary<int, AudioTranscription> choices = [];

        foreach (var update in updates)
        {
            ProcessUpdate(update, choices, completion);
        }

        AddChoicesToCompletion(choices, completion, coalesceContent);

        return completion;
    }

    /// <summary>Combines <see cref="AudioTranscriptionResponseUpdate"/> instances into a single <see cref="AudioTranscriptionResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="coalesceContent">
    /// <see langword="true"/> to attempt to coalesce contiguous <see cref="AIContent"/> items, where applicable,
    /// into a single <see cref="AIContent"/>, in order to reduce the number of individual content items that are included in
    /// the manufactured <see cref="AudioTranscription"/> instances. When <see langword="false"/>, the original content items are used.
    /// The default is <see langword="true"/>.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="AudioTranscriptionResponse"/>.</returns>
    public static Task<AudioTranscriptionResponse> ToAudioTranscriptionCompletionAsync(
        this IAsyncEnumerable<AudioTranscriptionResponseUpdate> updates, bool coalesceContent = true, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToAudioCompletionAsync(updates, coalesceContent, cancellationToken);

        static async Task<AudioTranscriptionResponse> ToAudioCompletionAsync(
            IAsyncEnumerable<AudioTranscriptionResponseUpdate> updates, bool coalesceContent, CancellationToken cancellationToken)
        {
            AudioTranscriptionResponse completion = new([]);
            Dictionary<int, AudioTranscription> choices = [];

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, choices, completion);
            }

            AddChoicesToCompletion(choices, completion, coalesceContent);

            return completion;
        }
    }

    /// <summary>Processes the <see cref="AudioTranscriptionResponseUpdate"/>, incorporating its contents into <paramref name="choices"/> and <paramref name="completion"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="choices">The dictionary mapping <see cref="AudioTranscriptionResponseUpdate.ChoiceIndex"/> to the <see cref="AudioTranscription"/> being built for that choice.</param>
    /// <param name="completion">The <see cref="AudioTranscriptionResponse"/> object whose properties should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(AudioTranscriptionResponseUpdate update, Dictionary<int, AudioTranscription> choices, AudioTranscriptionResponse completion)
    {
        completion.TranscriptionId ??= update.TranscriptionId;
        completion.ModelId ??= update.ModelId;

#if NET
        AudioTranscription choice = CollectionsMarshal.GetValueRefOrAddDefault(choices, update.ChoiceIndex, out _) ??=
            new(new List<AIContent>());
#else
        if (!choices.TryGetValue(update.ChoiceIndex, out AudioTranscription? choice))
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
                    // Use first-wins behavior to match the behavior of the other properties.
                    _ = choice.AdditionalProperties.TryAdd(entry.Key, entry.Value);
                }
            }
        }
    }

    /// <summary>Finalizes the <paramref name="completion"/> object by transferring the <paramref name="choices"/> into it.</summary>
    /// <param name="choices">The messages to process further and transfer into <paramref name="completion"/>.</param>
    /// <param name="completion">The result <see cref="AudioTranscriptionResponse"/> being built.</param>
    /// <param name="coalesceContent">The corresponding option value provided to <see cref="ToAudioTranscriptionCompletion"/> or <see cref="ToAudioTranscriptionCompletionAsync"/>.</param>
    private static void AddChoicesToCompletion(Dictionary<int, AudioTranscription> choices, AudioTranscriptionResponse completion, bool coalesceContent)
    {
        if (choices.Count <= 1)
        {
            // Add the single message if there is one.
            foreach (var entry in choices)
            {
                AddChoice(completion, coalesceContent, entry);
            }

            // In the vast majority case where there's only one choice, promote any additional properties
            // from the single choice to the audio transcription completion, making them more discoverable and more similar
            // to how they're typically surfaced from non-streaming services.
            if (completion.Choices.Count == 1 &&
                completion.Choices[0].AdditionalProperties is { } messageProps)
            {
                completion.Choices[0].AdditionalProperties = null;
                completion.AdditionalProperties = messageProps;
            }
        }
        else
        {
            // Add all of the messages, sorted by choice index.
            foreach (var entry in choices.OrderBy(entry => entry.Key))
            {
                AddChoice(completion, coalesceContent, entry);
            }

            // If there are multiple choices, we don't promote additional properties from the individual messages.
            // At a minimum, we'd want to know which choice the additional properties applied to, and if there were
            // conflicting values across the choices, it would be unclear which one should be used.
        }

        static void AddChoice(AudioTranscriptionResponse completion, bool coalesceContent, KeyValuePair<int, AudioTranscription> entry)
        {
            if (coalesceContent)
            {
                // Consider moving to a utility method.
                ChatResponseUpdateExtensions.CoalesceTextContent((List<AIContent>)entry.Value.Contents);
            }

            completion.Choices.Add(entry.Value);
        }
    }
}
