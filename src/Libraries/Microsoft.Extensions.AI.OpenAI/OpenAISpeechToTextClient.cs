// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Audio;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="ISpeechToTextClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="OpenAI.Audio.AudioClient"/>.</summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.SpeechToText, UrlFormat = DiagnosticIds.UrlFormat)]
internal sealed class OpenAISpeechToTextClient : ISpeechToTextClient
{
    /// <summary>Filename to use when audio lacks a name.</summary>
    /// <remarks>This information internally is required but is only being used to create a header name in the multipart request.</remarks>
    private const string Filename = "audio.mp3";

    /// <summary>Metadata about the client.</summary>
    private readonly SpeechToTextClientMetadata _metadata;

    /// <summary>The underlying <see cref="AudioClient" />.</summary>
    private readonly AudioClient _audioClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAISpeechToTextClient"/> class for the specified <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The underlying client.</param>
    public OpenAISpeechToTextClient(AudioClient audioClient)
    {
        _audioClient = Throw.IfNull(audioClient);

        _metadata = new("openai", audioClient.Endpoint, _audioClient.Model);
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
    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        SpeechToTextResponse response = new();

        string filename = audioSpeechStream is FileStream fileStream ?
            Path.GetFileName(fileStream.Name) : // Use the file name if we can get one from the stream.
            Filename; // Otherwise, use a default name; this is only used to create a header name in the multipart request.

        if (IsTranslationRequest(options))
        {
            var translation = (await _audioClient.TranslateAudioAsync(audioSpeechStream, filename, ToOpenAITranslationOptions(options), cancellationToken).ConfigureAwait(false)).Value;

            response.Contents = [new TextContent(translation.Text)];
            response.RawRepresentation = translation;

            int segmentCount = translation.Segments.Count;
            if (segmentCount > 0)
            {
                response.StartTime = translation.Segments[0].StartTime;
                response.EndTime = translation.Segments[segmentCount - 1].EndTime;
            }
        }
        else
        {
            var transcription = (await _audioClient.TranscribeAudioAsync(audioSpeechStream, filename, ToOpenAITranscriptionOptions(options), cancellationToken).ConfigureAwait(false)).Value;

            response.Contents = [new TextContent(transcription.Text)];
            response.RawRepresentation = transcription;

            int segmentCount = transcription.Segments.Count;
            if (segmentCount > 0)
            {
                response.StartTime = transcription.Segments[0].StartTime;
                response.EndTime = transcription.Segments[segmentCount - 1].EndTime;
            }
            else
            {
                int wordCount = transcription.Words.Count;
                if (wordCount > 0)
                {
                    response.StartTime = transcription.Words[0].StartTime;
                    response.EndTime = transcription.Words[wordCount - 1].EndTime;
                }
            }
        }

        return response;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        string filename = audioSpeechStream is FileStream fileStream ?
            Path.GetFileName(fileStream.Name) : // Use the file name if we can get one from the stream.
            Filename; // Otherwise, use a default name; this is only used to create a header name in the multipart request.

        if (IsTranslationRequest(options))
        {
            foreach (var update in (await GetTextAsync(audioSpeechStream, options, cancellationToken).ConfigureAwait(false)).ToSpeechToTextResponseUpdates())
            {
                yield return update;
            }
        }
        else
        {
            await foreach (var update in _audioClient.TranscribeAudioStreamingAsync(
                audioSpeechStream,
                filename,
                ToOpenAITranscriptionOptions(options),
                cancellationToken).ConfigureAwait(false))
            {
                SpeechToTextResponseUpdate result = new()
                {
                    ModelId = options?.ModelId,
                    RawRepresentation = update,
                };

                switch (update)
                {
                    case StreamingAudioTranscriptionTextDeltaUpdate deltaUpdate:
                        result.Kind = SpeechToTextResponseUpdateKind.TextUpdated;
                        result.Contents = [new TextContent(deltaUpdate.Delta)];
                        break;

                    case StreamingAudioTranscriptionTextDoneUpdate doneUpdate:
                        result.Kind = SpeechToTextResponseUpdateKind.SessionClose;
                        break;
                }

                yield return result;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IAudioTranscriptionClient interface.
    }

    // <summary>A translation is triggered when the target text language is specified and the source language is not provided or different.</summary>
    private static bool IsTranslationRequest(SpeechToTextOptions? options) =>
        options is not null &&
        options.TextLanguage is not null &&
        (options.SpeechLanguage is null || options.SpeechLanguage != options.TextLanguage);

    /// <summary>Converts an extensions options instance to an OpenAI transcription options instance.</summary>
    private AudioTranscriptionOptions ToOpenAITranscriptionOptions(SpeechToTextOptions? options)
    {
        AudioTranscriptionOptions result = options?.RawRepresentationFactory?.Invoke(this) as AudioTranscriptionOptions ?? new();

        result.Language ??= options?.SpeechLanguage;

        return result;
    }

    /// <summary>Converts an extensions options instance to an OpenAI translation options instance.</summary>
    private AudioTranslationOptions ToOpenAITranslationOptions(SpeechToTextOptions? options)
    {
        AudioTranslationOptions result = options?.RawRepresentationFactory?.Invoke(this) as AudioTranslationOptions ?? new();

        return result;
    }
}
