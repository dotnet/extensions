// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

    /// <summary>The underlying <see cref="AudioClient" />.</summary>
    private readonly AudioClient _audioClient;

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
            serviceType == typeof(AudioClient) ? _audioClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        var speechResponse = await GetTextAsync(audioSpeechStream, options, cancellationToken).ConfigureAwait(false);

        foreach (var update in speechResponse.ToSpeechToTextResponseUpdates())
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        SpeechToTextResponse response = new();

        // <summary>A translation is triggered when the target text language is specified and the source language is not provided or different.</summary>
        static bool IsTranslationRequest(SpeechToTextOptions? options)
             => options is not null && options.TextLanguage is not null
                && (options.SpeechLanguage is null || options.SpeechLanguage != options.TextLanguage);

        if (IsTranslationRequest(options))
        {
            _ = Throw.IfNull(options);

            var openAIOptions = ToOpenAITranslationOptions(options);
            AudioTranslation translationResult;

#if NET
            await using (audioSpeechStream.ConfigureAwait(false))
#else
            using (audioSpeechStream)
#endif
            {
                translationResult = (await _audioClient.TranslateAudioAsync(
                    audioSpeechStream,
                    "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                    openAIOptions, cancellationToken).ConfigureAwait(false)).Value;
            }

            UpdateResponseFromOpenAIAudioTranslation(response, translationResult);
        }
        else
        {
            var openAIOptions = ToOpenAITranscriptionOptions(options);

            // Transcription request
            AudioTranscription transcriptionResult;

#if NET
            await using (audioSpeechStream.ConfigureAwait(false))
#else
            using (audioSpeechStream)
#endif
            {
                transcriptionResult = (await _audioClient.TranscribeAudioAsync(
                    audioSpeechStream,
                    "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                    openAIOptions, cancellationToken).ConfigureAwait(false)).Value;
            }

            UpdateResponseFromOpenAIAudioTranscription(response, transcriptionResult);
        }

        return response;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IAudioTranscriptionClient interface.
    }

    /// <summary>Updates a <see cref="SpeechToTextResponse"/> from an OpenAI <see cref="AudioTranscription"/>.</summary>
    /// <param name="response">The response to update.</param>
    /// <param name="audioTranscription">The OpenAI audio transcription.</param>
    private static void UpdateResponseFromOpenAIAudioTranscription(SpeechToTextResponse response, AudioTranscription audioTranscription)
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

        // Update the response
        response.RawRepresentation = audioTranscription;
        response.Contents = [new TextContent(audioTranscription.Text)];
        response.StartTime = startTime;
        response.EndTime = endTime;
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            [nameof(audioTranscription.Language)] = audioTranscription.Language,
            [nameof(audioTranscription.Duration)] = audioTranscription.Duration
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

                if (additionalProperties.TryGetValue(nameof(result.ResponseFormat), out AudioTranscriptionFormat? responseFormat))
                {
                    result.ResponseFormat = responseFormat;
                }

                if (additionalProperties.TryGetValue(nameof(result.Prompt), out string? prompt))
                {
                    result.Prompt = prompt;
                }
            }
        }

        return result;
    }

    /// <summary>Updates a <see cref="SpeechToTextResponse"/> from an OpenAI <see cref="AudioTranslation"/>.</summary>
    /// <param name="response">The response to update.</param>
    /// <param name="audioTranslation">The OpenAI audio translation.</param>
    private static void UpdateResponseFromOpenAIAudioTranslation(SpeechToTextResponse response, AudioTranslation audioTranslation)
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

        // Update the response
        response.RawRepresentation = audioTranslation;
        response.Contents = [new TextContent(audioTranslation.Text)];
        response.StartTime = startTime;
        response.EndTime = endTime;
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            [nameof(audioTranslation.Language)] = audioTranslation.Language,
            [nameof(audioTranslation.Duration)] = audioTranslation.Duration
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

                if (additionalProperties.TryGetValue(nameof(result.ResponseFormat), out AudioTranslationFormat? responseFormat))
                {
                    result.ResponseFormat = responseFormat;
                }

                if (additionalProperties.TryGetValue(nameof(result.Prompt), out string? prompt))
                {
                    result.Prompt = prompt;
                }
            }
        }

        return result;
    }
}

