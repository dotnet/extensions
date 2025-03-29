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
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating speech to text client that logs speech to text operations to an <see cref="ILogger"/>.</summary>
/// <para>
/// The provided implementation of <see cref="ISpeechToTextClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
[Experimental("MEAI001")]
public partial class LoggingSpeechToTextClient : DelegatingSpeechToTextClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingSpeechToTextClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="ISpeechToTextClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingSpeechToTextClient(ISpeechToTextClient innerClient, ILogger logger)
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
    public override async Task<SpeechToTextResponse> TranscribeAudioAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(TranscribeAudioAsync), AsJson(speechContents), AsJson(options), AsJson(this.GetService<SpeechToTextClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(TranscribeAudioAsync));
            }
        }

        try
        {
            var completion = await base.TranscribeAudioAsync(speechContents, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(TranscribeAudioAsync), AsJson(completion));
                }
                else
                {
                    LogCompleted(nameof(TranscribeAudioAsync));
                }
            }

            return completion;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(TranscribeAudioAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(TranscribeAudioAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> TranscribeStreamingAudioAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(TranscribeStreamingAudioAsync), AsJson(speechContents), AsJson(options), AsJson(this.GetService<SpeechToTextClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(TranscribeStreamingAudioAsync));
            }
        }

        IAsyncEnumerator<SpeechToTextResponseUpdate> e;
        try
        {
            e = base.TranscribeStreamingAudioAsync(speechContents, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(TranscribeStreamingAudioAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(TranscribeStreamingAudioAsync), ex);
            throw;
        }

        try
        {
            SpeechToTextResponseUpdate? update = null;
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
                    LogInvocationCanceled(nameof(TranscribeStreamingAudioAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(TranscribeStreamingAudioAsync), ex);
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

            LogCompleted(nameof(TranscribeStreamingAudioAsync));
        }
        finally
        {
            await e.DisposeAsync().ConfigureAwait(false);
        }
    }

    private string AsJson<T>(T value) => LoggingHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, $"{{MethodName}} invoked: Audio contents: {{AudioContents}}. Options: {{{nameof(AI.SpeechToTextOptions)}}}. Metadata: {{{nameof(AI.SpeechToTextClientMetadata)}}}.")]
    private partial void LogInvokedSensitive(string methodName, string audioContents, string SpeechToTextOptions, string SpeechToTextClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, $"{{MethodName}} completed: {{{nameof(AI.SpeechToTextResponse)}}}.")]
    private partial void LogCompletedSensitive(string methodName, string SpeechToTextResponse);

    [LoggerMessage(LogLevel.Debug, $"{nameof(ISpeechToTextClient.TranscribeStreamingAudioAsync)} received update.")]
    private partial void LogStreamingUpdate();

    [LoggerMessage(LogLevel.Trace, $"{nameof(ISpeechToTextClient.TranscribeStreamingAudioAsync)} received update: {{{nameof(SpeechToTextResponseUpdate)}}}")]
    private partial void LogStreamingUpdateSensitive(string speechToTextResponseUpdate);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
