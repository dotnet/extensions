// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
public sealed class OpenAISpeechToTextClient : ISpeechToTextClient
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
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrEmpty(speechContents);

        for (var inputIndex = 0; inputIndex < speechContents.Count; inputIndex++)
        {
            var speechContent = speechContents[inputIndex];
            _ = Throw.IfNull(speechContent);

            var speechResponse = await GetResponseAsync([speechContent], options, cancellationToken).ConfigureAwait(false);

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
    public async Task<SpeechToTextResponse> GetResponseAsync(
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

                var choice = OpenAIModelMappers.FromOpenAIAudioTranslation(translationResult, inputIndex);
                choices.Add(choice);
            }
            else
            {
                var openAIOptions = OpenAIModelMappers.ToOpenAITranscriptionOptions(options);

                // Transcription request
                AudioTranscription transcriptionResult = await GetTranscriptionResultAsync(speechContent, firstChunk, openAIOptions, cancellationToken).ConfigureAwait(false);

                var choice = OpenAIModelMappers.FromOpenAIAudioTranscription(transcriptionResult, inputIndex);
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

    private async Task<AudioTranscription> GetTranscriptionResultAsync(
        IAsyncEnumerable<DataContent> speechContent, DataContent firstChunk, AudioTranscriptionOptions openAIOptions, CancellationToken cancellationToken)
    {
        OpenAI.Audio.AudioTranscription transcriptionResult;

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
        var openAIOptions = OpenAIModelMappers.ToOpenAITranslationOptions(options);
        OpenAI.Audio.AudioTranslation translationResult;

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

