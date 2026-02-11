// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating realtime session that logs operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IRealtimeSession"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// messages and options are logged. These messages and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Messages and options are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public partial class LoggingRealtimeSession : DelegatingRealtimeSession
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingRealtimeSession"/> class.</summary>
    /// <param name="innerSession">The underlying <see cref="IRealtimeSession"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingRealtimeSession(IRealtimeSession innerSession, ILogger logger)
        : base(innerSession)
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
    public override async Task UpdateAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(UpdateAsync), AsJson(options));
            }
            else
            {
                LogInvoked(nameof(UpdateAsync));
            }
        }

        try
        {
            await base.UpdateAsync(options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogCompleted(nameof(UpdateAsync));
            }
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(UpdateAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(UpdateAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task InjectClientMessageAsync(RealtimeClientMessage message, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(message);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInjectMessageSensitive(GetLoggableString(message));
            }
            else
            {
                LogInjectMessage();
            }
        }

        try
        {
            await base.InjectClientMessageAsync(message, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogCompleted(nameof(InjectClientMessageAsync));
            }
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(InjectClientMessageAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(InjectClientMessageAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogInvoked(nameof(GetStreamingResponseAsync));
        }

        IAsyncEnumerator<RealtimeServerMessage> e;
        try
        {
            e = base.GetStreamingResponseAsync(WrapUpdatesWithLoggingAsync(updates, cancellationToken), cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetStreamingResponseAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetStreamingResponseAsync), ex);
            throw;
        }

        try
        {
            RealtimeServerMessage? message = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    message = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(GetStreamingResponseAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(GetStreamingResponseAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        LogStreamingServerMessageSensitive(GetLoggableString(message));
                    }
                    else
                    {
                        LogStreamingServerMessage();
                    }
                }

                yield return message;
            }

            LogCompleted(nameof(GetStreamingResponseAsync));
        }
        finally
        {
            await e.DisposeAsync().ConfigureAwait(false);
        }
    }

    private string GetLoggableString(RealtimeClientMessage message)
    {
        var obj = new JsonObject
        {
            ["type"] = message.GetType().Name,
        };

        if (message.RawRepresentation is string s)
        {
            obj["content"] = s;
        }
        else if (message.RawRepresentation is not null)
        {
            obj["content"] = AsJson(message.RawRepresentation);
        }
        else if (message.EventId is not null)
        {
            obj["eventId"] = message.EventId;
        }

        return obj.ToJsonString();
    }

    private string GetLoggableString(RealtimeServerMessage message)
    {
        var obj = new JsonObject
        {
            ["type"] = message.Type.ToString(),
        };

        if (message.RawRepresentation is string s)
        {
            obj["content"] = s;
        }
        else if (message.RawRepresentation is not null)
        {
            obj["content"] = AsJson(message.RawRepresentation);
        }
        else if (message.EventId is not null)
        {
            obj["eventId"] = message.EventId;
        }

        return obj.ToJsonString();
    }

    private async IAsyncEnumerable<RealtimeClientMessage> WrapUpdatesWithLoggingAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogStreamingClientMessageSensitive(GetLoggableString(message));
                }
                else
                {
                    LogStreamingClientMessage();
                }
            }

            yield return message;
        }
    }

    private string AsJson<T>(T value) => TelemetryHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Options: {Options}.")]
    private partial void LogInvokedSensitive(string methodName, string options);

    [LoggerMessage(LogLevel.Debug, "InjectClientMessageAsync invoked.")]
    private partial void LogInjectMessage();

    [LoggerMessage(LogLevel.Trace, "InjectClientMessageAsync invoked: Message: {Message}.")]
    private partial void LogInjectMessageSensitive(string message);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Debug, "GetStreamingResponseAsync sending client message.")]
    private partial void LogStreamingClientMessage();

    [LoggerMessage(LogLevel.Trace, "GetStreamingResponseAsync sending client message: {ClientMessage}")]
    private partial void LogStreamingClientMessageSensitive(string clientMessage);

    [LoggerMessage(LogLevel.Debug, "GetStreamingResponseAsync received server message.")]
    private partial void LogStreamingServerMessage();

    [LoggerMessage(LogLevel.Trace, "GetStreamingResponseAsync received server message: {ServerMessage}")]
    private partial void LogStreamingServerMessageSensitive(string serverMessage);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
