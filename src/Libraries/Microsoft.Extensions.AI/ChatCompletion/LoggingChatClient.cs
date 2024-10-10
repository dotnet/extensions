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
public partial class LoggingChatClient : DelegatingChatClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingChatClient(IChatClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = JsonDefaults.Options;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(CompleteAsync), AsJson(chatMessages), AsJson(options), AsJson(Metadata));
            }
            else
            {
                LogInvoked(nameof(CompleteAsync));
            }
        }

        try
        {
            var completion = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(CompleteAsync), AsJson(completion));
                }
                else
                {
                    LogCompleted(nameof(CompleteAsync));
                }
            }

            return completion;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(CompleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(CompleteAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(CompleteStreamingAsync), AsJson(chatMessages), AsJson(options), AsJson(Metadata));
            }
            else
            {
                LogInvoked(nameof(CompleteStreamingAsync));
            }
        }

        IAsyncEnumerator<StreamingChatCompletionUpdate> e;
        try
        {
            e = base.CompleteStreamingAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(CompleteStreamingAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(CompleteStreamingAsync), ex);
            throw;
        }

        try
        {
            StreamingChatCompletionUpdate? update = null;
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
                    LogInvocationCanceled(nameof(CompleteStreamingAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(CompleteStreamingAsync), ex);
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

            LogCompleted(nameof(CompleteStreamingAsync));
        }
        finally
        {
            await e.DisposeAsync().ConfigureAwait(false);
        }
    }

    private string AsJson<T>(T value) => JsonSerializer.Serialize(value, _jsonSerializerOptions.GetTypeInfo(typeof(T)));

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: {ChatMessages}. Options: {ChatOptions}. Metadata: {ChatClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string chatMessages, string chatOptions, string chatClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {ChatCompletion}.")]
    private partial void LogCompletedSensitive(string methodName, string chatCompletion);

    [LoggerMessage(LogLevel.Debug, "CompleteStreamingAsync received update.")]
    private partial void LogStreamingUpdate();

    [LoggerMessage(LogLevel.Trace, "CompleteStreamingAsync received update: {StreamingChatCompletionUpdate}")]
    private partial void LogStreamingUpdateSensitive(string streamingChatCompletionUpdate);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
