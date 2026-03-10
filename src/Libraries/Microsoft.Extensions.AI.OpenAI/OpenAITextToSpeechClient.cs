// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Audio;

#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="ITextToSpeechClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="AudioClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
internal sealed class OpenAITextToSpeechClient : ITextToSpeechClient
{
    /// <summary>Default voice to use when none is specified.</summary>
    private const string DefaultVoice = "alloy";

    /// <summary>Metadata about the client.</summary>
    private readonly TextToSpeechClientMetadata _metadata;

    /// <summary>The underlying <see cref="AudioClient" />.</summary>
    private readonly AudioClient _audioClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAITextToSpeechClient"/> class for the specified <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The underlying client.</param>
    public OpenAITextToSpeechClient(AudioClient audioClient)
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
            serviceType == typeof(TextToSpeechClientMetadata) ? _metadata :
            serviceType == typeof(AudioClient) ? _audioClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<TextToSpeechResponse> GetAudioAsync(
        string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(text);

        SpeechGenerationOptions openAIOptions = ToOpenAISpeechOptions(options);

        var result = await _audioClient.GenerateSpeechAsync(
            text,
            new GeneratedSpeechVoice(options?.VoiceId ?? DefaultVoice),
            openAIOptions,
            cancellationToken).ConfigureAwait(false);

        string mediaType = GetMediaType(openAIOptions.ResponseFormat);

        return new TextToSpeechResponse([new DataContent(result.Value.ToMemory(), mediaType)])
        {
            ModelId = options?.ModelId ?? _metadata.DefaultModelId,
            RawRepresentation = result,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(
        string text, TextToSpeechOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // OpenAI's standard TTS API doesn't have a dedicated streaming endpoint in the SDK,
        // so we fall back to the non-streaming approach and yield the result as a single update.
        foreach (var update in (await GetAudioAsync(text, options, cancellationToken).ConfigureAwait(false)).ToTextToSpeechResponseUpdates())
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the ITextToSpeechClient interface.
    }

    /// <summary>Converts an extensions options instance to an OpenAI speech generation options instance.</summary>
    private SpeechGenerationOptions ToOpenAISpeechOptions(TextToSpeechOptions? options)
    {
        SpeechGenerationOptions result = options?.RawRepresentationFactory?.Invoke(this) as SpeechGenerationOptions ?? new();

        if (options?.Speed is float speed)
        {
            result.SpeedRatio ??= speed;
        }

        if (options?.AudioFormat is string audioFormat)
        {
            result.ResponseFormat ??= ToGeneratedSpeechFormat(audioFormat);
        }

        return result;
    }

    /// <summary>Maps a format string to a <see cref="GeneratedSpeechFormat"/>.</summary>
    private static GeneratedSpeechFormat? ToGeneratedSpeechFormat(string format) => format.ToUpperInvariant() switch
    {
        "MP3" or "AUDIO/MPEG" => GeneratedSpeechFormat.Mp3,
        "OPUS" or "AUDIO/OPUS" => GeneratedSpeechFormat.Opus,
        "AAC" or "AUDIO/AAC" => GeneratedSpeechFormat.Aac,
        "FLAC" or "AUDIO/FLAC" => GeneratedSpeechFormat.Flac,
        "WAV" or "AUDIO/WAV" => GeneratedSpeechFormat.Wav,
        "PCM" or "AUDIO/L16" => GeneratedSpeechFormat.Pcm,
        _ => new GeneratedSpeechFormat(format),
    };

    /// <summary>Gets the media type for the specified response format.</summary>
    private static string GetMediaType(GeneratedSpeechFormat? format) => format?.ToString() switch
    {
        "mp3" => "audio/mpeg",
        "opus" => "audio/opus",
        "aac" => "audio/aac",
        "flac" => "audio/flac",
        "wav" => "audio/wav",
        "pcm" => "audio/l16",
        null => "audio/mpeg", // OpenAI default is mp3
        _ => "application/octet-stream",
    };
}
