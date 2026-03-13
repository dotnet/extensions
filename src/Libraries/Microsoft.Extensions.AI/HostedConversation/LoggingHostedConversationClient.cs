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

/// <summary>A delegating hosted conversation client that logs operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IHostedConversationClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// messages and options are logged. These messages and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Messages and options are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public partial class LoggingHostedConversationClient : DelegatingHostedConversationClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingHostedConversationClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IHostedConversationClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingHostedConversationClient(IHostedConversationClient innerClient, ILogger logger)
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
    public override async Task<HostedConversation> CreateAsync(
        HostedConversationCreationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(CreateAsync), AsJson(options), AsJson(this.GetService<HostedConversationClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(CreateAsync));
            }
        }

        try
        {
            var conversation = await base.CreateAsync(options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(CreateAsync), AsJson(conversation));
                }
                else
                {
                    LogCompleted(nameof(CreateAsync));
                }
            }

            return conversation;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(CreateAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(CreateAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<HostedConversation> GetAsync(
        string conversationId, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogInvokedWithConversationId(nameof(GetAsync), conversationId);
        }

        try
        {
            var conversation = await base.GetAsync(conversationId, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(GetAsync), AsJson(conversation));
                }
                else
                {
                    LogCompleted(nameof(GetAsync));
                }
            }

            return conversation;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        string conversationId, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogInvokedWithConversationId(nameof(DeleteAsync), conversationId);
        }

        try
        {
            await base.DeleteAsync(conversationId, cancellationToken);
            LogCompleted(nameof(DeleteAsync));
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(DeleteAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task AddMessagesAsync(
        string conversationId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogAddMessagesInvokedSensitive(conversationId, AsJson(messages));
            }
            else
            {
                LogInvokedWithConversationId(nameof(AddMessagesAsync), conversationId);
            }
        }

        try
        {
            await base.AddMessagesAsync(conversationId, messages, cancellationToken);
            LogCompleted(nameof(AddMessagesAsync));
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(AddMessagesAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(AddMessagesAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        string conversationId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogInvokedWithConversationId(nameof(GetMessagesAsync), conversationId);
        }

        IAsyncEnumerator<ChatMessage> e;
        try
        {
            e = base.GetMessagesAsync(conversationId, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetMessagesAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetMessagesAsync), ex);
            throw;
        }

        try
        {
            ChatMessage? message = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        break;
                    }

                    message = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(GetMessagesAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(GetMessagesAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        LogMessageReceivedSensitive(AsJson(message));
                    }
                    else
                    {
                        LogMessageReceived();
                    }
                }

                yield return message;
            }

            LogCompleted(nameof(GetMessagesAsync));
        }
        finally
        {
            await e.DisposeAsync();
        }
    }

    private string AsJson<T>(T value) => TelemetryHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked. ConversationId: {ConversationId}.")]
    private partial void LogInvokedWithConversationId(string methodName, string conversationId);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Options: {HostedConversationCreationOptions}. Metadata: {HostedConversationClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string hostedConversationCreationOptions, string hostedConversationClientMetadata);

    [LoggerMessage(LogLevel.Trace, "AddMessagesAsync invoked. ConversationId: {ConversationId}. Messages: {Messages}.")]
    private partial void LogAddMessagesInvokedSensitive(string conversationId, string messages);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {HostedConversationResponse}.")]
    private partial void LogCompletedSensitive(string methodName, string hostedConversationResponse);

    [LoggerMessage(LogLevel.Debug, "GetMessagesAsync received message.")]
    private partial void LogMessageReceived();

    [LoggerMessage(LogLevel.Trace, "GetMessagesAsync received message: {ChatMessage}")]
    private partial void LogMessageReceivedSensitive(string chatMessage);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
