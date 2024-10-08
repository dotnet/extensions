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

#pragma warning disable EA0000 // Use source generated logging methods for improved performance
#pragma warning disable CA2254 // Template should be a static expression

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that logs chat operations to an <see cref="ILogger"/>.</summary>
public class LoggingChatClient : DelegatingChatClient
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
        LogStart(chatMessages, options);
        try
        {
            var completion = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.Log(LogLevel.Trace, 0, (completion, _jsonSerializerOptions), null, static (state, _) =>
                        $"CompleteAsync completed: {JsonSerializer.Serialize(state.completion, state._jsonSerializerOptions.GetTypeInfo(typeof(ChatCompletion)))}");
                }
                else
                {
                    _logger.LogDebug("CompleteAsync completed.");
                }
            }

            return completion;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CompleteAsync failed.");
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogStart(chatMessages, options);

        IAsyncEnumerator<StreamingChatCompletionUpdate> e;
        try
        {
            e = base.CompleteStreamingAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CompleteStreamingAsync failed.");
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
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "CompleteStreamingAsync failed.");
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.Log(LogLevel.Trace, 0, (update, _jsonSerializerOptions), null, static (state, _) =>
                            $"CompleteStreamingAsync received update: {JsonSerializer.Serialize(state.update, state._jsonSerializerOptions.GetTypeInfo(typeof(StreamingChatCompletionUpdate)))}");
                    }
                    else
                    {
                        _logger.LogDebug("CompleteStreamingAsync received update.");
                    }
                }

                yield return update;
            }

            _logger.LogDebug("CompleteStreamingAsync completed.");
        }
        finally
        {
            await e.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void LogStart(IList<ChatMessage> chatMessages, ChatOptions? options, [CallerMemberName] string? methodName = null)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.Log(LogLevel.Trace, 0, (methodName, chatMessages, options, this), null, static (state, _) =>
                    $"{state.methodName} invoked: " +
                    $"Messages: {JsonSerializer.Serialize(state.chatMessages, state.Item4._jsonSerializerOptions.GetTypeInfo(typeof(IList<ChatMessage>)))}. " +
                    $"Options: {JsonSerializer.Serialize(state.options, state.Item4._jsonSerializerOptions.GetTypeInfo(typeof(ChatOptions)))}. " +
                    $"Metadata: {JsonSerializer.Serialize(state.Item4.Metadata, state.Item4._jsonSerializerOptions.GetTypeInfo(typeof(ChatClientMetadata)))}.");
            }
            else
            {
                _logger.LogDebug($"{methodName} invoked.");
            }
        }
    }
}
