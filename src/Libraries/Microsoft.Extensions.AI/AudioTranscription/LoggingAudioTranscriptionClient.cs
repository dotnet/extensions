// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that logs chat operations to an <see cref="ILogger"/>.</summary>
/// <para>
/// The provided implementation of <see cref="IAudioTranscriptionClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
public partial class LoggingAudioTranscriptionClient : DelegatingAudioTranscriptionClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingAudioTranscriptionClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IAudioTranscriptionClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingAudioTranscriptionClient(IAudioTranscriptionClient innerClient, ILogger logger)
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
    public override async Task<AudioTranscriptionResponse> TranscribeAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(TranscribeAsync), AsJson(audioContents), AsJson(options), AsJson(this.GetService<AudioTranscriptionClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(TranscribeAsync));
            }
        }

        try
        {
            var completion = await base.TranscribeAsync(audioContents, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(TranscribeAsync), AsJson(completion));
                }
                else
                {
                    LogCompleted(nameof(TranscribeAsync));
                }
            }

            return completion;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(TranscribeAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(TranscribeAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<AudioTranscriptionResponseUpdate> TranscribeStreamingAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(TranscribeStreamingAsync), AsJson(audioContents), AsJson(options), AsJson(this.GetService<AudioTranscriptionClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(TranscribeStreamingAsync));
            }
        }

        IAsyncEnumerator<AudioTranscriptionResponseUpdate> e;
        try
        {
            e = base.TranscribeStreamingAsync(audioContents, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(TranscribeStreamingAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(TranscribeStreamingAsync), ex);
            throw;
        }

        try
        {
            AudioTranscriptionResponseUpdate? update = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    update = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(TranscribeStreamingAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(TranscribeStreamingAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        LogStreamingUpdateSensitive(AsJson(update));
                    }
                    else
                    {
                        LogStreamingUpdate();
                    }
                }

                yield return update;
            }

            LogCompleted(nameof(TranscribeStreamingAsync));
        }
        finally
        {
            await e.DisposeAsync().ConfigureAwait(false);
        }
    }

    private string AsJson<T>(T value) => LoggingHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Audio contents: {AudioContents}. Options: {AudioTranscriptionOptions}. Metadata: {AudioTranscriptionClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string audioContents, string audioTranscriptionOptions, string audioTranscriptionClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {AudioTranscriptionResponse}.")]
    private partial void LogCompletedSensitive(string methodName, string audioTranscriptionResponse);

    [LoggerMessage(LogLevel.Debug, "TranscribeStreamingAsync received update.")]
    private partial void LogStreamingUpdate();

    [LoggerMessage(LogLevel.Trace, "TranscribeStreamingAsync received update: {StreamingAudioTranscriptionResponseUpdate}")]
    private partial void LogStreamingUpdateSensitive(string streamingAudioTranscriptionResponseUpdate);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
