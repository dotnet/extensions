// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;
using OpenAI.Audio;

#pragma warning disable S3440 // Variables should not be checked against the values they're about to be assigned

namespace Microsoft.Extensions.AI;

internal static partial class OpenAIModelMappers
{
    public static SpeechToTextMessage FromOpenAIAudioTranslation(OpenAI.Audio.AudioTranslation audioTranslation, int inputIndex)
    {
        _ = Throw.IfNull(audioTranslation);

        var segmentCount = audioTranslation.Segments.Count;

        TimeSpan? endTime = null;
        TimeSpan? startTime = null;
        if (segmentCount > 0)
        {
            endTime = audioTranslation.Segments[segmentCount - 1].EndTime;
            startTime = audioTranslation.Segments[0].StartTime;
        }

        // Create the return choice.
        return new SpeechToTextMessage
        {
            RawRepresentation = audioTranslation,
            InputIndex = inputIndex,
            Text = audioTranslation.Text,
            StartTime = startTime,
            EndTime = endTime,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [nameof(audioTranslation.Language)] = audioTranslation.Language,
                [nameof(audioTranslation.Duration)] = audioTranslation.Duration
            },
        };
    }

    public static SpeechToTextOptions FromOpenAITranslationOptions(OpenAI.Audio.AudioTranslationOptions options)
    {
        SpeechToTextOptions result = new();

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

            if (options.Prompt is string prompt)
            {
                (result.AdditionalProperties ??= [])[nameof(options.Prompt)] = prompt;
            }

            if (options.ResponseFormat is AudioTranslationFormat jsonFormat)
            {
                (result.AdditionalProperties ??= [])[nameof(options.ResponseFormat)] = jsonFormat;
            }
        }

        return result;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    public static OpenAI.Audio.AudioTranslationOptions ToOpenAITranslationOptions(SpeechToTextOptions? options)
    {
        OpenAI.Audio.AudioTranslationOptions result = new();

        if (options is not null)
        {
            if (options.AdditionalProperties is { Count: > 0 } additionalProperties)
            {
                if (additionalProperties.TryGetValue(nameof(result.Temperature), out float? temperature))
                {
                    result.Temperature = temperature;
                }

                if (additionalProperties.TryGetValue(nameof(result.Prompt), out string? prompt))
                {
                    result.Prompt = prompt;
                }

                if (additionalProperties.TryGetValue(nameof(result.ResponseFormat), out AudioTranslationFormat? responseFormat))
                {
                    result.ResponseFormat = responseFormat;
                }
            }
        }

        return result;
    }
}
