// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;
using OpenAI.Audio;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S103 // Lines should not be too long
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3440 // Variables should not be checked against the values they're about to be assigned

namespace Microsoft.Extensions.AI;

internal static partial class OpenAIModelMappers
{
    public static AudioTranscription FromOpenAIAudioTranscription(OpenAI.Audio.AudioTranscription audioTranscription, int inputIndex)
    {
        _ = Throw.IfNull(audioTranscription);

        var segmentCount = audioTranscription.Segments.Count;
        var wordCount = audioTranscription.Words.Count;

        TimeSpan? endTime = null;
        TimeSpan? startTime = null;
        if (segmentCount > 0)
        {
            endTime = audioTranscription.Segments[segmentCount - 1].EndTime;
            startTime = audioTranscription.Segments[0].StartTime;
        }
        else if (wordCount > 0)
        {
            endTime = audioTranscription.Words[wordCount - 1].EndTime;
            startTime = audioTranscription.Words[0].StartTime;
        }

        // Create the return choice.
        return new AudioTranscription
        {
            RawRepresentation = audioTranscription,
            InputIndex = inputIndex,
            Text = audioTranscription.Text,
            StartTime = startTime,
            EndTime = endTime,
        };
    }

    public static AudioTranscriptionOptions FromOpenAIOptions(OpenAI.Audio.AudioTranscriptionOptions options)
    {
        AudioTranscriptionOptions result = new();

        if (options is not null)
        {
            result.ModelId = _getModelIdAccessor.Invoke(options, null)?.ToString() switch
            {
                null or "" => null,
                var modelId => modelId,
            };

            if (options.Temperature is float temperature)
            {
                (result.AdditionalProperties ??= [])[nameof(options.Temperature)] = temperature;
            }

            if (options.TimestampGranularities is AudioTimestampGranularities timestampGranularities)
            {
                (result.AdditionalProperties ??= [])[nameof(options.TimestampGranularities)] = timestampGranularities;
            }

            if (options.Prompt is string prompt)
            {
                (result.AdditionalProperties ??= [])[nameof(options.Prompt)] = prompt;
            }

            if (options.ResponseFormat is AudioTranscriptionFormat jsonFormat)
            {
                (result.AdditionalProperties ??= [])[nameof(options.ResponseFormat)] = jsonFormat;
            }
        }

        return result;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    public static OpenAI.Audio.AudioTranscriptionOptions ToOpenAIOptions(AudioTranscriptionOptions? options)
    {
        OpenAI.Audio.AudioTranscriptionOptions result = new();

        if (options is not null)
        {
            result.Language = options.AudioLanguage;

            if (options.AdditionalProperties is { Count: > 0 } additionalProperties)
            {
                if (additionalProperties.TryGetValue(nameof(result.Temperature), out float? temperature))
                {
                    result.Temperature = temperature;
                }

                if (additionalProperties.TryGetValue(nameof(result.TimestampGranularities), out object? timestampGranularities))
                {
                    result.TimestampGranularities = timestampGranularities is AudioTimestampGranularities granularities ? granularities : default;
                }

                if (additionalProperties.TryGetValue(nameof(result.Prompt), out string? prompt))
                {
                    result.Prompt = prompt;
                }

                if (additionalProperties.TryGetValue(nameof(result.ResponseFormat), out AudioTranscriptionFormat? responseFormat))
                {
                    result.ResponseFormat = responseFormat;
                }
            }
        }

        return result;
    }
}
