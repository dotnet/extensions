// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

/// <summary>Represents an <see cref="IAudioTranscriptionClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="OpenAI.Audio.AudioClient"/>.</summary>
public sealed class OpenAIAudioTranscriptionClient : IAudioTranscriptionClient
{
    /// <summary>Default OpenAI endpoint.</summary>
    private static readonly Uri _defaultOpenAIEndpoint = new("https://api.openai.com/v1");

    /// <summary>Metadata about the client.</summary>
    private readonly AudioTranscriptionClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIClient" />.</summary>
    private readonly OpenAIClient? _openAIClient;

    /// <summary>The underlying <see cref="AudioClient" />.</summary>
    private readonly AudioClient _audioClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIAudioTranscriptionClient"/> class for the specified <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The underlying client.</param>
    /// <param name="modelId">The model to use.</param>
    public OpenAIAudioTranscriptionClient(OpenAIClient openAIClient, string modelId)
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

    /// <summary>Initializes a new instance of the <see cref="OpenAIAudioTranscriptionClient"/> class for the specified <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The underlying client.</param>
    public OpenAIAudioTranscriptionClient(AudioClient audioClient)
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
            serviceType == typeof(AudioTranscriptionClientMetadata) ? _metadata :
            serviceType == typeof(OpenAIClient) ? _openAIClient :
            serviceType == typeof(AudioClient) ? _audioClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<AudioTranscriptionResponse> TranscribeAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrEmpty(audioContents);

        var openAIOptions = OpenAIModelMappers.ToOpenAIOptions(options);
        List<AudioTranscription> choices = [];

        for (var inputIndex = 0; inputIndex < audioContents.Count; inputIndex++)
        {
            var audioContent = audioContents[inputIndex];
            _ = Throw.IfNull(audioContent);

            var enumerator = audioContent.GetAsyncEnumerator(cancellationToken);
            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException($"The audio content provided in the index: {inputIndex} is empty.");
            }

            var firstChunk = enumerator.Current;

            OpenAI.Audio.AudioTranscription transcriptionResult;
            if (!firstChunk.Data.HasValue)
            {
                // Check if the first chunk is a file path (file://)
                var uri = new Uri(firstChunk.Uri);
                if (uri.Scheme.ToUpperInvariant() != "FILE")
                {
                    throw new NotSupportedException("Only file paths are supported.");
                }

                var filePath = uri.LocalPath;
                transcriptionResult = (await _audioClient.TranscribeAudioAsync(
                    audioFilePath: filePath,
                    options: OpenAIModelMappers.ToOpenAIOptions(options)).ConfigureAwait(false)).Value;
            }
            else
            {
                using var audioFileStream = audioContent.ToStream(firstChunk, cancellationToken);
                transcriptionResult = (await _audioClient.TranscribeAudioAsync(
                    audioFileStream,
                    "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                    OpenAIModelMappers.ToOpenAIOptions(options), cancellationToken).ConfigureAwait(false)).Value;
            }

            var choice = OpenAIModelMappers.FromOpenAIAudioTranscription(transcriptionResult, inputIndex);
            choices.Add(choice);
        }

        return new AudioTranscriptionResponse(choices)
        {
            RawRepresentation = choices[0].RawRepresentation,
        };
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IAudioTranscriptionClient interface.
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AudioTranscriptionResponseUpdate> TranscribeStreamingAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrEmpty(audioContents);

        for (var inputIndex = 0; inputIndex < audioContents.Count; inputIndex++)
        {
            var audioContent = audioContents[inputIndex];
            _ = Throw.IfNull(audioContent);

            var transcriptionCompletion = await TranscribeAsync([audioContent], options, cancellationToken).ConfigureAwait(false);

            foreach (var choice in transcriptionCompletion.Choices)
            {
                yield return new AudioTranscriptionResponseUpdate(choice.Contents)
                {
                    InputIndex = inputIndex,
                    Kind = AudioTranscriptionResponseUpdateKind.Transcribed,
                    RawRepresentation = choice.RawRepresentation
                };
            }
        }
    }
}

