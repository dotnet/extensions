// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Audio;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="ISpeechToTextClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="OpenAI.Audio.AudioClient"/>.</summary>
[Experimental("MEAI001")]
internal sealed class OpenAISpeechToTextClient : ISpeechToTextClient
{
    /// <summary>Default OpenAI endpoint.</summary>
    private static readonly Uri _defaultOpenAIEndpoint = new("https://api.openai.com/v1");

    /// <summary>Metadata about the client.</summary>
    private readonly SpeechToTextClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIClient" />.</summary>
    private readonly OpenAIClient? _openAIClient;

    /// <summary>The underlying <see cref="AudioClient" />.</summary>
    private readonly AudioClient _audioClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAISpeechToTextClient"/> class for the specified <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The underlying client.</param>
    /// <param name="modelId">The model to use.</param>
    public OpenAISpeechToTextClient(OpenAIClient openAIClient, string modelId)
    {
        _ = Throw.IfNull(openAIClient);
        _ = Throw.IfNullOrWhitespace(modelId);

        _openAIClient = openAIClient;
        _audioClient = openAIClient.GetAudioClient(modelId);

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(OpenAIClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(openAIClient) as Uri ?? _defaultOpenAIEndpoint;

        _metadata = new("openai", providerUrl, modelId);
    }

    /// <summary>Initializes a new instance of the <see cref="OpenAISpeechToTextClient"/> class for the specified <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The underlying client.</param>
    public OpenAISpeechToTextClient(AudioClient audioClient)
    {
        _ = Throw.IfNull(audioClient);

        _audioClient = audioClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(AudioClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(audioClient) as Uri ?? _defaultOpenAIEndpoint;
        string? model = typeof(AudioClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(audioClient) as string;

        _metadata = new("openai", providerUrl, model);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(SpeechToTextClientMetadata) ? _metadata :
            serviceType == typeof(OpenAIClient) ? _openAIClient :
            serviceType == typeof(AudioClient) ? _audioClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> TranscribeStreamingAudioAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrEmpty(speechContents);

        for (var inputIndex = 0; inputIndex < speechContents.Count; inputIndex++)
        {
            var speechContent = speechContents[inputIndex];
            _ = Throw.IfNull(speechContent);

            var speechResponse = await TranscribeAudioAsync([speechContent], options, cancellationToken).ConfigureAwait(false);

            foreach (var choice in speechResponse.Choices)
            {
                yield return new SpeechToTextResponseUpdate(choice.Contents)
                {
                    InputIndex = inputIndex,
                    Kind = SpeechToTextResponseUpdateKind.TextUpdated,
                    RawRepresentation = choice.RawRepresentation
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<SpeechToTextResponse> TranscribeAudioAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrEmpty(speechContents);

        List<SpeechToTextMessage> choices = [];

        // <summary>A translation is triggered when the target text language is specified and the source language is not provided or different.</summary>
        static bool IsTranslationRequest(SpeechToTextOptions? options)
             => options is not null && options.TextLanguage is not null
                && (options.SpeechLanguage is null || options.SpeechLanguage != options.TextLanguage);

        for (var inputIndex = 0; inputIndex < speechContents.Count; inputIndex++)
        {
            var speechContent = speechContents[inputIndex];
            _ = Throw.IfNull(speechContent);

            var enumerator = speechContent.GetAsyncEnumerator(cancellationToken);
            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException($"The audio content provided in the index: {inputIndex} is empty.");
            }

            var firstChunk = enumerator.Current;

            if (IsTranslationRequest(options))
            {
                _ = Throw.IfNull(options);

                // Translation request will be triggered whenever the source language is not specified and a target text language is and different from the output text language
                if (CultureInfo.GetCultureInfo(options.TextLanguage!).TwoLetterISOLanguageName != "en")
                {
                    throw new NotSupportedException($"Only translation to english is supported.");
                }

                AudioTranslation translationResult = await GetTranslationResultAsync(options, speechContent, firstChunk, cancellationToken).ConfigureAwait(false);

                var choice = FromOpenAIAudioTranslation(translationResult, inputIndex);
                choices.Add(choice);
            }
            else
            {
                var openAIOptions = ToOpenAITranscriptionOptions(options);

                // Transcription request
                AudioTranscription transcriptionResult = await GetTranscriptionResultAsync(speechContent, firstChunk, openAIOptions, cancellationToken).ConfigureAwait(false);

                var choice = FromOpenAIAudioTranscription(transcriptionResult, inputIndex);
                choices.Add(choice);
            }
        }

        return new SpeechToTextResponse(choices);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IAudioTranscriptionClient interface.
    }

    private static SpeechToTextMessage FromOpenAIAudioTranscription(AudioTranscription audioTranscription, int inputIndex)
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
        return new SpeechToTextMessage
        {
            RawRepresentation = audioTranscription,
            InputIndex = inputIndex,
            Text = audioTranscription.Text,
            StartTime = startTime,
            EndTime = endTime,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [nameof(audioTranscription.Language)] = audioTranscription.Language,
                [nameof(audioTranscription.Duration)] = audioTranscription.Duration
            },
        };
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private static AudioTranscriptionOptions ToOpenAITranscriptionOptions(SpeechToTextOptions? options)
    {
        AudioTranscriptionOptions result = new();

        if (options is not null)
        {
            if (options.SpeechLanguage is not null)
            {
                result.Language = options.SpeechLanguage;
            }

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

    private static SpeechToTextMessage FromOpenAIAudioTranslation(AudioTranslation audioTranslation, int inputIndex)
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

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private static AudioTranslationOptions ToOpenAITranslationOptions(SpeechToTextOptions? options)
    {
        AudioTranslationOptions result = new();

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

    private async Task<AudioTranscription> GetTranscriptionResultAsync(
        IAsyncEnumerable<DataContent> speechContent, DataContent firstChunk, AudioTranscriptionOptions openAIOptions, CancellationToken cancellationToken)
    {
        AudioTranscription transcriptionResult;

        var audioFileStream = speechContent.ToStream(firstChunk, cancellationToken);
#if NET
        await using (audioFileStream.ConfigureAwait(false))
#else
        using (audioFileStream)
#endif
        {
            transcriptionResult = (await _audioClient.TranscribeAudioAsync(
                audioFileStream,
                "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                openAIOptions, cancellationToken).ConfigureAwait(false)).Value;
        }

        return transcriptionResult;
    }

    private async Task<AudioTranslation> GetTranslationResultAsync(
        SpeechToTextOptions? options, IAsyncEnumerable<DataContent> speechContent, DataContent firstChunk, CancellationToken cancellationToken)
    {
        var openAIOptions = ToOpenAITranslationOptions(options);
        AudioTranslation translationResult;

        var audioFileStream = speechContent.ToStream(firstChunk, cancellationToken);
#if NET
        await using (audioFileStream.ConfigureAwait(false))
#else
        using (audioFileStream)
#endif
        {
            translationResult = (await _audioClient.TranslateAudioAsync(
                audioFileStream,
                "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                openAIOptions, cancellationToken).ConfigureAwait(false)).Value;
        }

        return translationResult;
    }
}

