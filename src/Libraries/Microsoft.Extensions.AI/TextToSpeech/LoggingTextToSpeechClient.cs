// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating text to speech client that logs text to speech operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="ITextToSpeechClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// messages and options are logged. These messages and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Messages and options are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public partial class LoggingTextToSpeechClient : DelegatingTextToSpeechClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingTextToSpeechClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="ITextToSpeechClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingTextToSpeechClient(ITextToSpeechClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<TextToSpeechResponse> GetAudioAsync(
        string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GetAudioAsync), AsJson(options), AsJson(this.GetService<TextToSpeechClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GetAudioAsync));
            }
        }

        try
        {
            var response = await base.GetAudioAsync(text, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                // TTS responses always contain binary audio data; avoid serializing it.
                LogCompleted(nameof(GetAudioAsync));
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetAudioAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetAudioAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(
        string text, TextToSpeechOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GetStreamingAudioAsync), AsJson(options), AsJson(this.GetService<TextToSpeechClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GetStreamingAudioAsync));
            }
        }

        IAsyncEnumerator<TextToSpeechResponseUpdate> e;
        try
        {
            e = base.GetStreamingAudioAsync(text, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetStreamingAudioAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetStreamingAudioAsync), ex);
            throw;
        }

        try
        {
            TextToSpeechResponseUpdate? update = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        break;
                    }

                    update = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(GetStreamingAudioAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(GetStreamingAudioAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    // TTS updates always contain binary audio data; avoid serializing it.
                    LogStreamingUpdate();
                }

                yield return update;
            }

            LogCompleted(nameof(GetStreamingAudioAsync));
        }
        finally
        {
            await e.DisposeAsync();
        }
    }

    private string AsJson<T>(T value) => TelemetryHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Options: {TextToSpeechOptions}. Metadata: {TextToSpeechClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string textToSpeechOptions, string textToSpeechClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Debug, "GetStreamingAudioAsync received update.")]
    private partial void LogStreamingUpdate();

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
