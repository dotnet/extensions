// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IRealtimeSession"/> for the OpenAI Realtime API over WebSocket.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OpenAIRealtimeSession : IRealtimeSession
{
    /// <summary>The receive buffer size for WebSocket messages.</summary>
    private const int ReceiveBufferSize = 1024 * 16;

    /// <summary>The API key used for authentication.</summary>
    private readonly string _apiKey;

    /// <summary>The model to use for the session.</summary>
    private readonly string _model;

    /// <summary>Lock used to synchronize WebSocket send operations.</summary>
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    /// <summary>Channel used to relay parsed server events to the consumer.</summary>
    private readonly Channel<RealtimeServerMessage> _eventChannel;

    /// <summary>The WebSocket used for communication.</summary>
    private WebSocket? _webSocket;

    /// <summary>The cancellation token source for the session.</summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>Whether the session has been disposed (0 = false, 1 = true).</summary>
    private int _disposed;

    /// <summary>The background task that receives WebSocket messages.</summary>
    private Task? _receiveTask;

    /// <inheritdoc />
    public RealtimeSessionOptions? Options { get; private set; }

    /// <summary>Initializes a new instance of the <see cref="OpenAIRealtimeSession"/> class.</summary>
    /// <param name="apiKey">The API key used for authentication.</param>
    /// <param name="model">The model to use for the session.</param>
    internal OpenAIRealtimeSession(string apiKey, string model)
    {
        _apiKey = apiKey;
        _model = model;
        _eventChannel = Channel.CreateUnbounded<RealtimeServerMessage>();
    }

    /// <summary>Connects the WebSocket to the OpenAI Realtime API.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the connection succeeded; otherwise, <see langword="false"/>.</returns>
    internal async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
            _webSocket = clientWebSocket;

            // Use an independent CTS for the session lifetime. The creation-time token is only
            // used for the connect call itself; the background receive loop must not be tied to it,
            // since callers commonly use short-lived timeout tokens for creation.
            _cancellationTokenSource = new CancellationTokenSource();

            var uri = new Uri($"wss://api.openai.com/v1/realtime?model={Uri.EscapeDataString(_model)}");
            await clientWebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

            // Start receiving messages in the background.
            _receiveTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            return true;
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or IOException)
        {
            return false;
        }
    }

    /// <summary>Connects the session using an already-connected <see cref="WebSocket"/> instance. Used for testing.</summary>
    /// <param name="webSocket">The connected WebSocket to use.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    internal Task ConnectWithWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start receiving messages in the background.
        _receiveTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(options);

        var sessionElement = new JsonObject
        {
            ["type"] = "session.update",
        };
        var sessionObject = new JsonObject();
        sessionElement["session"] = sessionObject;

        var audioElement = new JsonObject();
        var audioInputElement = new JsonObject();
        var audioOutputElement = new JsonObject();
        sessionObject["audio"] = audioElement;
        audioElement["input"] = audioInputElement;
        audioElement["output"] = audioOutputElement;

        if (options.InputAudioFormat is not null)
        {
            var audioInputFormatElement = new JsonObject
            {
                ["type"] = options.InputAudioFormat.Type,
            };
            if (options.InputAudioFormat.SampleRate.HasValue)
            {
                audioInputFormatElement["rate"] = options.InputAudioFormat.SampleRate.Value;
            }

            audioInputElement["format"] = audioInputFormatElement;
        }

        if (options.NoiseReductionOptions.HasValue)
        {
            var noiseReductionObj = new JsonObject
            {
                ["type"] = options.NoiseReductionOptions.Value == NoiseReductionOptions.NearField ? "near_field" : "far_field",
            };
            audioInputElement["noise_reduction"] = noiseReductionObj;
        }

        if (options.TranscriptionOptions is not null)
        {
            var transcriptionOptionsObj = new JsonObject
            {
                ["language"] = options.TranscriptionOptions.SpeechLanguage,
                ["model"] = options.TranscriptionOptions.ModelId,
            };
            if (options.TranscriptionOptions.Prompt is not null)
            {
                transcriptionOptionsObj["prompt"] = options.TranscriptionOptions.Prompt;
            }

            audioInputElement["transcription"] = transcriptionOptionsObj;
        }

        if (options.VoiceActivityDetection is ServerVoiceActivityDetection serverVad)
        {
            audioInputElement["turn_detection"] = new JsonObject
            {
                ["type"] = "server_vad",
                ["create_response"] = serverVad.CreateResponse,
                ["idle_timeout_ms"] = serverVad.IdleTimeoutInMilliseconds,
                ["interrupt_response"] = serverVad.InterruptResponse,
                ["prefix_padding_ms"] = serverVad.PrefixPaddingInMilliseconds,
                ["silence_duration_ms"] = serverVad.SilenceDurationInMilliseconds,
                ["threshold"] = serverVad.Threshold,
            };
        }
        else if (options.VoiceActivityDetection is SemanticVoiceActivityDetection semanticVad)
        {
            audioInputElement["turn_detection"] = new JsonObject
            {
                ["type"] = "semantic_vad",
                ["create_response"] = semanticVad.CreateResponse,
                ["interrupt_response"] = semanticVad.InterruptResponse,
                ["eagerness"] = semanticVad.Eagerness.Value,
            };
        }

        if (options.SessionKind == RealtimeSessionKind.Realtime)
        {
            sessionObject["type"] = "realtime";

            if (options.OutputAudioFormat is not null)
            {
                var audioOutputFormatElement = new JsonObject
                {
                    ["type"] = options.OutputAudioFormat.Type,
                };
                if (options.OutputAudioFormat.SampleRate.HasValue)
                {
                    audioOutputFormatElement["rate"] = options.OutputAudioFormat.SampleRate.Value;
                }

                audioOutputElement["format"] = audioOutputFormatElement;
            }

            audioOutputElement["speed"] = options.VoiceSpeed;

            if (options.Voice is not null)
            {
                audioOutputElement["voice"] = options.Voice;
            }

            if (options.Instructions is not null)
            {
                sessionObject["instructions"] = options.Instructions;
            }

            if (options.MaxOutputTokens.HasValue)
            {
                sessionObject["max_output_tokens"] = options.MaxOutputTokens.Value;
            }

            if (options.Model is not null)
            {
                sessionObject["model"] = options.Model;
            }

            if (options.OutputModalities is not null && options.OutputModalities.Any())
            {
                sessionObject["output_modalities"] = CreateModalitiesArray(options.OutputModalities);
            }

            if (options.Tools is not null)
            {
                var toolsArray = new JsonArray();
                foreach (var tool in options.Tools)
                {
                    JsonObject? toolObj = tool is HostedMcpServerTool mcpTool
                        ? SerializeMcpToolToJson(mcpTool)
                        : SerializeAIFunctionToJson(tool as AIFunction);
                    if (toolObj is not null)
                    {
                        toolsArray.Add(toolObj);
                    }
                }

                sessionObject["tools"] = toolsArray;
            }
        }
        else if (options.SessionKind == RealtimeSessionKind.Transcription)
        {
            sessionObject["type"] = "transcription";
        }

        Options = options;

        await SendEventAsync(sessionElement, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task InjectClientMessageAsync(RealtimeClientMessage message, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(message);

        // In a realtime streaming scenario, cancellation is used to signal session teardown.
        // Silently returning avoids throwing during the natural shutdown path, where multiple
        // components may race to inject final messages while the session is being disposed.
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        JsonObject? jsonMessage = new JsonObject();

        if (message.EventId is not null)
        {
            jsonMessage["event_id"] = message.EventId;
        }

        switch (message)
        {
            case RealtimeClientResponseCreateMessage responseCreate:
                jsonMessage["type"] = "response.create";
                var responseObj = new JsonObject();

                // Handle OutputAudioOptions (audio.output.format).
                if (responseCreate.OutputAudioOptions is not null)
                {
                    var audioObj = new JsonObject();
                    var outputObj = new JsonObject();
                    var formatObj = new JsonObject();

                    switch (responseCreate.OutputAudioOptions.Type)
                    {
                        case "audio/pcm":
                            if (responseCreate.OutputAudioOptions.SampleRate == 24000)
                            {
                                formatObj["type"] = responseCreate.OutputAudioOptions.Type;
                                formatObj["rate"] = 24000;
                            }

                            break;

                        case "audio/pcmu":
                        case "audio/pcma":
                            formatObj["type"] = responseCreate.OutputAudioOptions.Type;
                            break;
                    }

                    outputObj["format"] = formatObj;

                    if (!string.IsNullOrEmpty(responseCreate.OutputVoice))
                    {
                        outputObj["voice"] = responseCreate.OutputVoice;
                    }

                    audioObj["output"] = outputObj;
                    responseObj["audio"] = audioObj;
                }
                else if (!string.IsNullOrEmpty(responseCreate.OutputVoice))
                {
                    responseObj["audio"] = new JsonObject
                    {
                        ["output"] = new JsonObject
                        {
                            ["voice"] = responseCreate.OutputVoice,
                        },
                    };
                }

                responseObj["conversation"] = responseCreate.ExcludeFromConversation ? "none" : "auto";

                if (responseCreate.Items is { } items && items.Any())
                {
                    var inputArray = new JsonArray();
                    foreach (var item in items)
                    {
                        if (item is RealtimeContentItem contentItem && contentItem.Contents is not null)
                        {
                            var itemObj = new JsonObject
                            {
                                ["type"] = "message",
                                ["object"] = "realtime.item",
                            };

                            if (contentItem.Role.HasValue)
                            {
                                itemObj["role"] = contentItem.Role.Value.Value;
                            }

                            if (contentItem.Id is not null)
                            {
                                itemObj["id"] = contentItem.Id;
                            }

                            itemObj["content"] = SerializeContentsToJsonArray(contentItem.Contents);
                            inputArray.Add(itemObj);
                        }
                    }

                    responseObj["input"] = inputArray;
                }

                if (!string.IsNullOrEmpty(responseCreate.Instructions))
                {
                    responseObj["instructions"] = responseCreate.Instructions;
                }

                if (responseCreate.MaxOutputTokens.HasValue)
                {
                    responseObj["max_output_tokens"] = responseCreate.MaxOutputTokens.Value;
                }

                if (responseCreate.Metadata is { Count: > 0 })
                {
                    var metadataObj = new JsonObject();
                    foreach (var kvp in responseCreate.Metadata)
                    {
                        metadataObj[kvp.Key] = JsonValue.Create(kvp.Value);
                    }

                    responseObj["metadata"] = metadataObj;
                }

                if (responseCreate.OutputModalities is not null && responseCreate.OutputModalities.Any())
                {
                    responseObj["output_modalities"] = CreateModalitiesArray(responseCreate.OutputModalities);
                }

                if (responseCreate.AIFunction is not null)
                {
                    responseObj["tool_choice"] = new JsonObject
                    {
                        ["type"] = "function",
                        ["name"] = responseCreate.AIFunction.Name,
                    };
                }
                else if (responseCreate.HostedMcpServerTool is not null)
                {
                    responseObj["tool_choice"] = new JsonObject
                    {
                        ["type"] = "mcp",
                        ["server_label"] = responseCreate.HostedMcpServerTool.ServerName,
                        ["name"] = responseCreate.HostedMcpServerTool.Name,
                    };
                }
                else if (responseCreate.ToolMode is { } toolMode)
                {
                    responseObj["tool_choice"] = toolMode switch
                    {
                        RequiredChatToolMode r when r.RequiredFunctionName is not null => r.RequiredFunctionName,
                        RequiredChatToolMode => "required",
                        NoneChatToolMode => "none",
                        _ => "auto",
                    };
                }

                if (responseCreate.Tools is not null && responseCreate.Tools.Any())
                {
                    var toolsArray = new JsonArray();

                    foreach (var tool in responseCreate.Tools)
                    {
                        JsonObject? toolObj = null;

                        if (tool is AIFunction aiFunction && !string.IsNullOrEmpty(aiFunction.Name))
                        {
                            toolObj = SerializeAIFunctionToJson(aiFunction);
                        }
                        else if (tool is HostedMcpServerTool mcpTool)
                        {
                            toolObj = SerializeMcpToolToJson(mcpTool);
                        }
                        else
                        {
                            var toolJson = JsonSerializer.SerializeToNode(tool);

                            if (toolJson is not null &&
                                (toolJson["server_label"] is not null || toolJson["server_url"] is not null || toolJson["connector_id"] is not null))
                            {
                                toolObj = new JsonObject { ["type"] = "mcp" };

                                CopyJsonPropertyIfExists(toolJson, toolObj, "server_label");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "server_url");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "connector_id");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "authorization");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "headers");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "require_approval");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "server_description");
                                CopyJsonPropertyIfExists(toolJson, toolObj, "allowed_tools");
                            }
                        }

                        if (toolObj is not null)
                        {
                            toolsArray.Add(toolObj);
                        }
                    }

                    responseObj["tools"] = toolsArray;
                }

                jsonMessage["response"] = responseObj;
                break;

            case RealtimeClientConversationItemCreateMessage itemCreate:
                if (itemCreate.Item is not null)
                {
                    jsonMessage["type"] = "conversation.item.create";

                    if (itemCreate.PreviousId is not null)
                    {
                        jsonMessage["previous_item_id"] = itemCreate.PreviousId;
                    }

                    if (itemCreate.Item is RealtimeContentItem contentItem && contentItem.Contents is not null)
                    {
                        var itemObj = new JsonObject();

                        if (contentItem.Id is not null)
                        {
                            itemObj["id"] = contentItem.Id;
                        }

                        if (contentItem.Contents.Count > 0 && contentItem.Contents[0] is FunctionResultContent functionResult)
                        {
                            itemObj["type"] = "function_call_output";
                            itemObj["call_id"] = functionResult.CallId;
                            itemObj["output"] = functionResult?.Result is not null
                                ? JsonSerializer.Serialize(functionResult.Result)
                                : null;
                        }
                        else if (contentItem.Contents.Count > 0 && contentItem.Contents[0] is FunctionCallContent functionCall)
                        {
                            itemObj["type"] = "function_call";
                            itemObj["call_id"] = functionCall.CallId;
                            itemObj["name"] = functionCall.Name;

                            if (functionCall.Arguments is not null)
                            {
                                itemObj["arguments"] = JsonSerializer.Serialize(functionCall.Arguments);
                            }
                        }
                        else if (contentItem.Contents.Count > 0 && contentItem.Contents[0] is McpServerToolApprovalResponseContent approvalResponse)
                        {
                            itemObj["type"] = "mcp_approval_response";
                            itemObj["approval_request_id"] = approvalResponse.Id;
                            itemObj["approve"] = approvalResponse.Approved;
                        }
                        else
                        {
                            itemObj["type"] = "message";

                            if (contentItem.Role.HasValue)
                            {
                                itemObj["role"] = contentItem.Role.Value.Value;
                            }

                            itemObj["content"] = SerializeContentsToJsonArray(contentItem.Contents);
                        }

                        jsonMessage["item"] = itemObj;
                    }
                }

                break;

            case RealtimeClientInputAudioBufferAppendMessage audioAppend:
                if (audioAppend.Content is not null && audioAppend.Content.HasTopLevelMediaType("audio"))
                {
                    jsonMessage["type"] = "input_audio_buffer.append";

                    string dataUri = audioAppend.Content.Uri?.ToString() ?? string.Empty;
                    int commaIndex = dataUri.LastIndexOf(',');

                    jsonMessage["audio"] = commaIndex >= 0 && commaIndex < dataUri.Length - 1
                        ? dataUri.Substring(commaIndex + 1)
                        : Convert.ToBase64String(audioAppend.Content.Data.ToArray());
                }

                break;

            case RealtimeClientInputAudioBufferCommitMessage:
                jsonMessage["type"] = "input_audio_buffer.commit";
                break;

            default:
                if (message.RawRepresentation is string rawString)
                {
                    jsonMessage = JsonSerializer.Deserialize<JsonObject>(rawString);
                }
                else if (message.RawRepresentation is JsonObject rawJsonObject)
                {
                    jsonMessage = rawJsonObject;
                }

                // Preserve EventId if it was set on the message but not in the raw representation.
                if (jsonMessage is not null && message.EventId is not null &&
                    !jsonMessage.ContainsKey("event_id"))
                {
                    jsonMessage["event_id"] = message.EventId;
                }

                break;
        }

        if (jsonMessage?.TryGetPropertyValue("type", out _) is true)
        {
            await SendEventAsync(jsonMessage, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        var processUpdatesTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    await InjectClientMessageAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the session is cancelled.
            }
            catch (ObjectDisposedException)
            {
                // Expected when the session is disposed concurrently.
            }
            catch (WebSocketException)
            {
                // Expected when the WebSocket is in an aborted state.
            }
        }, cancellationToken);

        await foreach (var serverEvent in _eventChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return serverEvent;
        }

        await processUpdatesTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    object? IRealtimeSession.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _ = _eventChannel.Writer.TryComplete();
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; nothing to do.
        }

        // Best-effort wait for the background receive loop to finish.
#pragma warning disable VSTHRD002 // Synchronous Dispose must block; callers should prefer DisposeAsync
        try
        {
            _receiveTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Expected when the session is cancelled during disposal.
        }
#pragma warning restore VSTHRD002

        _cancellationTokenSource?.Dispose();
        _webSocket?.Dispose();
        _sendLock.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _ = _eventChannel.Writer.TryComplete();
        try
        {
#if NET
            await (_cancellationTokenSource?.CancelAsync() ?? Task.CompletedTask).ConfigureAwait(false);
#else
            _cancellationTokenSource?.Cancel();
#endif
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; nothing to do.
        }

        // Wait for the background receive loop to finish before disposing resources.
        if (_receiveTask is not null)
        {
            try
            {
                await _receiveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the session is cancelled during disposal.
            }
        }

        _cancellationTokenSource?.Dispose();
        _webSocket?.Dispose();
        _sendLock.Dispose();
    }

    /// <summary>Sends a JSON event over the WebSocket.</summary>
    /// <param name="eventData">The JSON object to send.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    private async Task SendEventAsync(JsonObject eventData, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0 || _webSocket?.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(eventData);
            var lockTaken = false;
            try
            {
                await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(utf8Bytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (lockTaken)
                {
                    _ = _sendLock.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the session is cancelled.
        }
        catch (ObjectDisposedException)
        {
            // Expected when the session is disposed concurrently.
        }
        catch (WebSocketException)
        {
            // Expected when the WebSocket is in an aborted state.
        }
    }

    /// <summary>Background loop that receives WebSocket messages and writes them to the event channel.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                string message;
                if (result.EndOfMessage)
                {
                    // Fast path: single-frame message, no extra allocation.
                    message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                }
                else
                {
                    // Multi-frame: accumulate remaining frames.
                    using var ms = new MemoryStream();
#if NET
                    await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken).ConfigureAwait(false);
#else
                    await ms.WriteAsync(buffer, 0, result.Count, cancellationToken).ConfigureAwait(false);
#endif
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

#if NET
                        await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken).ConfigureAwait(false);
#else
                        await ms.WriteAsync(buffer, 0, result.Count, cancellationToken).ConfigureAwait(false);
#endif
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    message = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    ProcessServerEvent(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the session is cancelled.
        }
        catch (Exception ex)
        {
            // Surface the error to consumers reading from the channel.
            _ = _eventChannel.Writer.TryComplete(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _ = _eventChannel.Writer.TryComplete();
        }
    }

    /// <summary>Parses a raw JSON server event and writes the corresponding <see cref="RealtimeServerMessage"/> to the channel.</summary>
    /// <param name="message">The raw JSON message string.</param>
    private void ProcessServerEvent(string message)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;
            var eventType = root.GetProperty("type").GetString();

            try
            {
                var serverMessage = eventType switch
                {
                    "error" => CreateErrorMessage(root),
                    "conversation.item.input_audio_transcription.delta" or
                    "conversation.item.input_audio_transcription.completed" or
                    "conversation.item.input_audio_transcription.failed" =>
                        CreateInputAudioTranscriptionMessage(root, eventType),
                    "response.output_audio_transcript.delta" or
                    "response.output_audio_transcript.done" or
                    "response.output_audio.delta" or
                    "response.output_audio.done" =>
                        CreateOutputTextAudioMessage(root, eventType),
                    "response.created" or
                    "response.done" =>
                        CreateResponseCreatedMessage(root, eventType),
                    "response.output_item.added" or
                    "response.output_item.done" =>
                        CreateResponseOutItemMessage(root, eventType),
                    "session.created" or
                    "session.updated" =>
                        HandleSessionEvent(root),
                    "mcp_call.in_progress" or
                    "mcp_call.completed" or
                    "mcp_call.failed" =>
                        CreateMcpCallMessage(root, eventType),
                    "mcp_list_tools.in_progress" or
                    "mcp_list_tools.completed" or
                    "mcp_list_tools.failed" =>
                        CreateMcpListToolsMessage(root, eventType),
                    "conversation.item.added" or
                    "conversation.item.done" =>
                        CreateConversationItemMessage(root, eventType),
                    _ => new RealtimeServerMessage
                    {
                        Type = RealtimeServerMessageType.RawContentOnly,
                        RawRepresentation = root.Clone(),
                    },
                };

                if (serverMessage is not null)
                {
                    _ = _eventChannel.Writer.TryWrite(serverMessage);
                }
            }
            catch (Exception)
            {
                _ = _eventChannel.Writer.TryWrite(new RealtimeServerMessage
                {
                    Type = RealtimeServerMessageType.RawContentOnly,
                    RawRepresentation = root.Clone(),
                });
            }
        }
        catch (Exception)
        {
            // Failed to parse the raw JSON; nothing to write to the channel.
        }
    }

    #region Helper Methods

    private static void CopyJsonPropertyIfExists(JsonNode? source, JsonObject target, string propertyName)
    {
        if (source?[propertyName] is JsonNode value)
        {
            target[propertyName] = value.DeepClone();
        }
    }

    private static JsonArray CreateModalitiesArray(IEnumerable<string> modalities)
        => new([.. modalities.Select(m => JsonValue.Create(m))]);

    private static RealtimeAudioFormat? ParseAudioFormat(JsonElement formatElement)
    {
        if (formatElement.ValueKind != JsonValueKind.Object ||
            !formatElement.TryGetProperty("type", out var typeElement))
        {
            return null;
        }

        string? formatType = typeElement.GetString();
        if (formatType is null)
        {
            return null;
        }

        int sampleRate = formatElement.TryGetProperty("rate", out var rateElement) && rateElement.ValueKind == JsonValueKind.Number
            ? rateElement.GetInt32()
            : 0;
        return new RealtimeAudioFormat(formatType, sampleRate);
    }

    private static int? ParseMaxOutputTokens(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Number
            ? element.GetInt32()
            : element.ValueKind == JsonValueKind.String && element.GetString() == "inf"
                ? int.MaxValue
                : null;
    }

    private static List<string>? ParseOutputModalities(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var modalities = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.GetString() is string m && !string.IsNullOrEmpty(m))
            {
                modalities.Add(m);
            }
        }

        return modalities.Count > 0 ? modalities : null;
    }

    private static JsonObject? SerializeAIFunctionToJson(AIFunction? aiFunction)
    {
        if (aiFunction is null || string.IsNullOrEmpty(aiFunction.Name))
        {
            return null;
        }

        var toolObj = new JsonObject
        {
            ["type"] = "function",
            ["name"] = aiFunction.Name,
        };

        if (!string.IsNullOrEmpty(aiFunction.Description))
        {
            toolObj["description"] = aiFunction.Description;
        }

        toolObj["parameters"] = JsonNode.Parse(aiFunction.JsonSchema.GetRawText());
        return toolObj;
    }

    private static JsonObject? SerializeMcpToolToJson(HostedMcpServerTool? mcpTool)
    {
        if (mcpTool is null)
        {
            return null;
        }

        var toolObj = new JsonObject
        {
            ["type"] = "mcp",
            ["server_label"] = mcpTool.ServerName,
        };

        if (Uri.TryCreate(mcpTool.ServerAddress, UriKind.Absolute, out _))
        {
            toolObj["server_url"] = mcpTool.ServerAddress;

            if (mcpTool.Headers is { } headers)
            {
                var headersObj = new JsonObject();
                foreach (var kvp in headers)
                {
                    headersObj[kvp.Key] = kvp.Value;
                }

                if (headersObj.Count > 0)
                {
                    toolObj["headers"] = headersObj;
                }
            }
        }
        else
        {
            toolObj["connector_id"] = mcpTool.ServerAddress;

            if (mcpTool.AuthorizationToken is not null)
            {
                toolObj["authorization"] = new JsonObject
                {
                    ["token"] = mcpTool.AuthorizationToken,
                };
            }
        }

        if (mcpTool.ServerDescription is not null)
        {
            toolObj["server_description"] = mcpTool.ServerDescription;
        }

        if (mcpTool.AllowedTools is { Count: > 0 })
        {
            toolObj["allowed_tools"] = new JsonArray([.. mcpTool.AllowedTools.Select(t => JsonValue.Create(t))]);
        }

        if (mcpTool.ApprovalMode is not null)
        {
            toolObj["require_approval"] = mcpTool.ApprovalMode switch
            {
                HostedMcpServerToolAlwaysRequireApprovalMode => (JsonNode)"always",
                HostedMcpServerToolNeverRequireApprovalMode => (JsonNode)"never",
                HostedMcpServerToolRequireSpecificApprovalMode specific => CreateSpecificApprovalNode(specific),
                _ => (JsonNode)"always",
            };
        }

        return toolObj;
    }

    private static JsonObject CreateSpecificApprovalNode(HostedMcpServerToolRequireSpecificApprovalMode mode)
    {
        var approvalObj = new JsonObject();

        if (mode.AlwaysRequireApprovalToolNames is { Count: > 0 })
        {
            approvalObj["always"] = new JsonObject
            {
                ["tool_names"] = new JsonArray([.. mode.AlwaysRequireApprovalToolNames.Select(t => JsonValue.Create(t))]),
            };
        }

        if (mode.NeverRequireApprovalToolNames is { Count: > 0 })
        {
            approvalObj["never"] = new JsonObject
            {
                ["tool_names"] = new JsonArray([.. mode.NeverRequireApprovalToolNames.Select(t => JsonValue.Create(t))]),
            };
        }

        return approvalObj;
    }

    private static JsonArray SerializeContentsToJsonArray(IEnumerable<AIContent> contents)
    {
        var contentsArray = new JsonArray();

        foreach (var content in contents)
        {
            if (content is TextContent textContent)
            {
                contentsArray.Add(new JsonObject
                {
                    ["type"] = "input_text",
                    ["text"] = textContent.Text,
                });
            }
            else if (content is DataContent dataContent)
            {
                if (dataContent.MediaType.StartsWith("audio/", StringComparison.Ordinal))
                {
                    contentsArray.Add(new JsonObject
                    {
                        ["type"] = "input_audio",
                        ["audio"] = dataContent.Base64Data.ToString(),
                    });
                }
                else if (dataContent.MediaType.StartsWith("image/", StringComparison.Ordinal))
                {
                    contentsArray.Add(new JsonObject
                    {
                        ["type"] = "input_image",
                        ["image_url"] = dataContent.Uri,
                    });
                }
            }
        }

        return contentsArray;
    }

    private static ChatRole? ParseChatRole(string? roleString) => roleString switch
    {
        "assistant" => ChatRole.Assistant,
        "user" => ChatRole.User,
        "system" => ChatRole.System,
        _ => null,
    };

    private static UsageDetails? ParseUsageDetails(JsonElement usageElement, bool requireTypeCheck = false)
    {
        if (usageElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (requireTypeCheck &&
            (!usageElement.TryGetProperty("type", out var usageTypeElement) ||
             usageTypeElement.GetString() != "tokens"))
        {
            return null;
        }

        var usageData = new UsageDetails();

        if (usageElement.TryGetProperty("input_tokens", out var inputTokensElement))
        {
            usageData.InputTokenCount = inputTokensElement.GetInt32();
        }

        if (usageElement.TryGetProperty("output_tokens", out var outputTokensElement))
        {
            usageData.OutputTokenCount = outputTokensElement.GetInt32();
        }

        if (usageElement.TryGetProperty("total_tokens", out var totalTokensElement))
        {
            usageData.TotalTokenCount = totalTokensElement.GetInt32();
        }

        if (usageElement.TryGetProperty("input_token_details", out var inputTokenDetailsElement) &&
            inputTokenDetailsElement.ValueKind == JsonValueKind.Object)
        {
            if (inputTokenDetailsElement.TryGetProperty("audio_tokens", out var audioTokensElement))
            {
                usageData.InputAudioTokenCount = audioTokensElement.GetInt32();
            }

            if (inputTokenDetailsElement.TryGetProperty("text_tokens", out var textTokensElement))
            {
                usageData.InputTextTokenCount = textTokensElement.GetInt32();
            }
        }

        if (usageElement.TryGetProperty("output_token_details", out var outputTokenDetailsElement) &&
            outputTokenDetailsElement.ValueKind == JsonValueKind.Object)
        {
            if (outputTokenDetailsElement.TryGetProperty("audio_tokens", out var audioTokensElement))
            {
                usageData.OutputAudioTokenCount = audioTokensElement.GetInt32();
            }

            if (outputTokenDetailsElement.TryGetProperty("text_tokens", out var textTokensElement))
            {
                usageData.OutputTextTokenCount = textTokensElement.GetInt32();
            }
        }

        return usageData;
    }

    private static List<AIContent> ParseContentItems(JsonElement contentElements)
    {
        if (contentElements.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var contentList = new List<AIContent>();

        foreach (var contentElement in contentElements.EnumerateArray())
        {
            if (!contentElement.TryGetProperty("type", out var contentTypeElement))
            {
                continue;
            }

            string? contentType = contentTypeElement.GetString();

            if (contentType == "input_text")
            {
                if (contentElement.TryGetProperty("text", out var textElement))
                {
                    contentList.Add(new TextContent(textElement.GetString()));
                }
                else if (contentElement.TryGetProperty("transcript", out var transcriptElement))
                {
                    contentList.Add(new TextContent(transcriptElement.GetString()));
                }
            }
            else if (contentType == "input_audio" && contentElement.TryGetProperty("audio", out var audioDataElement))
            {
                contentList.Add(new DataContent($"data:audio/pcm;base64,{audioDataElement.GetString()}"));
            }
            else if (contentType == "input_image" && contentElement.TryGetProperty("image_url", out var imageUrlElement))
            {
                contentList.Add(new DataContent(imageUrlElement.GetString()!));
            }
        }

        return contentList;
    }

    private static RealtimeContentItem? ParseRealtimeContentItem(JsonElement itemElement)
    {
        if (!itemElement.TryGetProperty("type", out var itemTypeElement))
        {
            return null;
        }

        string? id = itemElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        string? itemType = itemTypeElement.GetString();

        if (itemType == "message")
        {
            if (!itemElement.TryGetProperty("content", out var contentElements) ||
                contentElements.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            ChatRole? chatRole = itemElement.TryGetProperty("role", out var roleElement)
                ? ParseChatRole(roleElement.GetString())
                : null;

            return new RealtimeContentItem(ParseContentItems(contentElements), id, chatRole);
        }
        else if (itemType == "function_call" &&
            itemElement.TryGetProperty("name", out var nameElement) &&
            itemElement.TryGetProperty("call_id", out var callerIdElement))
        {
            IDictionary<string, object?>? arguments = null;

            if (itemElement.TryGetProperty("arguments", out var argumentsElement))
            {
                if (argumentsElement.ValueKind == JsonValueKind.String)
                {
                    string argumentsJson = argumentsElement.GetString()!;
                    arguments = string.IsNullOrEmpty(argumentsJson) ? null : JsonSerializer.Deserialize<IDictionary<string, object?>>(argumentsJson);
                }
                else if (argumentsElement.ValueKind == JsonValueKind.Object)
                {
                    arguments = new AdditionalPropertiesDictionary();
                    foreach (var argProperty in argumentsElement.EnumerateObject())
                    {
                        arguments[argProperty.Name] = argProperty.Value.ValueKind == JsonValueKind.String
                            ? argProperty.Value.GetString()
                            : JsonSerializer.Deserialize<object?>(argProperty.Value.GetRawText());
                    }
                }
            }

            return new RealtimeContentItem(
                [new FunctionCallContent(callerIdElement.GetString()!, nameElement.GetString()!, arguments)],
                id);
        }
        else if (itemType == "mcp_call" &&
            itemElement.TryGetProperty("name", out var mcpNameElement))
        {
            string? serverLabel = itemElement.TryGetProperty("server_label", out var serverLabelElement) ? serverLabelElement.GetString() : null;
            string callId = id ?? string.Empty;

            IReadOnlyDictionary<string, object?>? mcpArguments = null;
            if (itemElement.TryGetProperty("arguments", out var mcpArgumentsElement) && mcpArgumentsElement.ValueKind == JsonValueKind.String)
            {
                string argsJson = mcpArgumentsElement.GetString()!;
                if (!string.IsNullOrEmpty(argsJson))
                {
                    mcpArguments = JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>>(argsJson);
                }
            }

            var contents = new List<AIContent>();
            contents.Add(new McpServerToolCallContent(callId, mcpNameElement.GetString()!, serverLabel)
            {
                Arguments = mcpArguments,
            });

            // Parse output/error into McpServerToolResultContent (same pattern as OpenAIResponsesChatClient.AddMcpToolCallContent)
            if (itemElement.TryGetProperty("output", out var outputElement) ||
                itemElement.TryGetProperty("error", out _))
            {
                AIContent resultContent = itemElement.TryGetProperty("error", out var errorElement) && errorElement.ValueKind != JsonValueKind.Null
                    ? new ErrorContent(errorElement.ToString())
                    : new TextContent(outputElement.ValueKind == JsonValueKind.String ? outputElement.GetString() : null);

                contents.Add(new McpServerToolResultContent(callId)
                {
                    Output = [resultContent],
                    RawRepresentation = itemElement.Clone(),
                });
            }

            return new RealtimeContentItem(contents, id);
        }
        else if (itemType == "mcp_approval_request" &&
            itemElement.TryGetProperty("name", out var approvalNameElement))
        {
            string approvalId = id ?? string.Empty;
            string? approvalServerLabel = itemElement.TryGetProperty("server_label", out var approvalServerElement) ? approvalServerElement.GetString() : null;

            IReadOnlyDictionary<string, object?>? approvalArguments = null;
            if (itemElement.TryGetProperty("arguments", out var approvalArgsElement) && approvalArgsElement.ValueKind == JsonValueKind.String)
            {
                string argsJson = approvalArgsElement.GetString()!;
                if (!string.IsNullOrEmpty(argsJson))
                {
                    approvalArguments = JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>>(argsJson);
                }
            }

            var toolCall = new McpServerToolCallContent(approvalId, approvalNameElement.GetString()!, approvalServerLabel)
            {
                Arguments = approvalArguments,
                RawRepresentation = itemElement.Clone(),
            };

            return new RealtimeContentItem(
                [new McpServerToolApprovalRequestContent(approvalId, toolCall) { RawRepresentation = itemElement.Clone() }],
                id);
        }

        return null;
    }

    #endregion

    #region Message Creation Methods

    private RealtimeServerMessage? HandleSessionEvent(JsonElement root)
    {
        if (root.TryGetProperty("session", out var sessionElement))
        {
            var newOptions = DeserializeSessionOptions(sessionElement);

            // Preserve client-side properties that the server cannot round-trip
            // as typed objects (tools are returned as JSON schemas, not AITool instances).
            if (Options is not null)
            {
                newOptions.Tools = Options.Tools;
                newOptions.AIFunction = Options.AIFunction;
                newOptions.HostedMcpServerTool = Options.HostedMcpServerTool;
                newOptions.ToolMode = Options.ToolMode;
                newOptions.EnableAutoTracing = Options.EnableAutoTracing;
                newOptions.TracingGroupId = Options.TracingGroupId;
                newOptions.TracingWorkflowName = Options.TracingWorkflowName;
                newOptions.TracingMetadata = Options.TracingMetadata;
            }

            Options = newOptions;
        }

        return new RealtimeServerMessage
        {
            Type = RealtimeServerMessageType.RawContentOnly,
            RawRepresentation = root.Clone(),
        };
    }

    private static RealtimeSessionOptions DeserializeSessionOptions(JsonElement session)
    {
        var options = new RealtimeSessionOptions();

        if (session.TryGetProperty("type", out var typeElement))
        {
            options.SessionKind = typeElement.GetString() == "transcription"
                ? RealtimeSessionKind.Transcription
                : RealtimeSessionKind.Realtime;
        }

        if (session.TryGetProperty("model", out var modelElement))
        {
            options.Model = modelElement.GetString();
        }

        if (session.TryGetProperty("instructions", out var instructionsElement) &&
            instructionsElement.ValueKind == JsonValueKind.String)
        {
            options.Instructions = instructionsElement.GetString();
        }

        if (session.TryGetProperty("max_output_tokens", out var maxTokensElement))
        {
            options.MaxOutputTokens = ParseMaxOutputTokens(maxTokensElement);
        }

        if (session.TryGetProperty("output_modalities", out var modalitiesElement))
        {
            options.OutputModalities = ParseOutputModalities(modalitiesElement);
        }

        // Audio configuration.
        if (session.TryGetProperty("audio", out var audioElement) &&
            audioElement.ValueKind == JsonValueKind.Object)
        {
            // Input audio.
            if (audioElement.TryGetProperty("input", out var inputElement) &&
                inputElement.ValueKind == JsonValueKind.Object)
            {
                if (inputElement.TryGetProperty("format", out var inputFormatElement))
                {
                    options.InputAudioFormat = ParseAudioFormat(inputFormatElement);
                }

                if (inputElement.TryGetProperty("noise_reduction", out var noiseElement) &&
                    noiseElement.ValueKind == JsonValueKind.Object &&
                    noiseElement.TryGetProperty("type", out var noiseTypeElement))
                {
                    options.NoiseReductionOptions = noiseTypeElement.GetString() switch
                    {
                        "near_field" => NoiseReductionOptions.NearField,
                        "far_field" => NoiseReductionOptions.FarField,
                        _ => null,
                    };
                }

                if (inputElement.TryGetProperty("transcription", out var transcriptionElement) &&
                    transcriptionElement.ValueKind == JsonValueKind.Object)
                {
                    string? language = transcriptionElement.TryGetProperty("language", out var langElement) ? langElement.GetString() : null;
                    string? model = transcriptionElement.TryGetProperty("model", out var modelEl) ? modelEl.GetString() : null;
                    string? prompt = transcriptionElement.TryGetProperty("prompt", out var promptElement) ? promptElement.GetString() : null;

                    if (language is not null && model is not null)
                    {
                        options.TranscriptionOptions = new TranscriptionOptions { SpeechLanguage = language, ModelId = model, Prompt = prompt };
                    }
                }

                // Turn detection (VAD).
                if (inputElement.TryGetProperty("turn_detection", out var turnDetectionElement) &&
                    turnDetectionElement.ValueKind == JsonValueKind.Object &&
                    turnDetectionElement.TryGetProperty("type", out var vadTypeElement))
                {
                    string? vadType = vadTypeElement.GetString();
                    if (vadType == "server_vad")
                    {
                        var serverVad = new ServerVoiceActivityDetection();
                        if (turnDetectionElement.TryGetProperty("create_response", out var crElement))
                        {
                            serverVad.CreateResponse = crElement.GetBoolean();
                        }

                        if (turnDetectionElement.TryGetProperty("interrupt_response", out var irElement))
                        {
                            serverVad.InterruptResponse = irElement.GetBoolean();
                        }

                        if (turnDetectionElement.TryGetProperty("idle_timeout_ms", out var itElement) && itElement.ValueKind == JsonValueKind.Number)
                        {
                            serverVad.IdleTimeoutInMilliseconds = itElement.GetInt32();
                        }

                        if (turnDetectionElement.TryGetProperty("prefix_padding_ms", out var ppElement) && ppElement.ValueKind == JsonValueKind.Number)
                        {
                            serverVad.PrefixPaddingInMilliseconds = ppElement.GetInt32();
                        }

                        if (turnDetectionElement.TryGetProperty("silence_duration_ms", out var sdElement) && sdElement.ValueKind == JsonValueKind.Number)
                        {
                            serverVad.SilenceDurationInMilliseconds = sdElement.GetInt32();
                        }

                        if (turnDetectionElement.TryGetProperty("threshold", out var thElement) && thElement.ValueKind == JsonValueKind.Number)
                        {
                            serverVad.Threshold = thElement.GetDouble();
                        }

                        options.VoiceActivityDetection = serverVad;
                    }
                    else if (vadType == "semantic_vad")
                    {
                        var semanticVad = new SemanticVoiceActivityDetection();
                        if (turnDetectionElement.TryGetProperty("create_response", out var crElement))
                        {
                            semanticVad.CreateResponse = crElement.GetBoolean();
                        }

                        if (turnDetectionElement.TryGetProperty("interrupt_response", out var irElement))
                        {
                            semanticVad.InterruptResponse = irElement.GetBoolean();
                        }

                        if (turnDetectionElement.TryGetProperty("eagerness", out var eagernessElement) &&
                            eagernessElement.GetString() is string eagerness)
                        {
                            semanticVad.Eagerness = new SemanticEagerness(eagerness);
                        }

                        options.VoiceActivityDetection = semanticVad;
                    }
                }
            }

            // Output audio.
            if (audioElement.TryGetProperty("output", out var outputElement) &&
                outputElement.ValueKind == JsonValueKind.Object)
            {
                if (outputElement.TryGetProperty("format", out var outputFormatElement))
                {
                    options.OutputAudioFormat = ParseAudioFormat(outputFormatElement);
                }

                if (outputElement.TryGetProperty("speed", out var speedElement) && speedElement.ValueKind == JsonValueKind.Number)
                {
                    options.VoiceSpeed = speedElement.GetDouble();
                }

                if (outputElement.TryGetProperty("voice", out var voiceElement))
                {
                    if (voiceElement.ValueKind == JsonValueKind.String)
                    {
                        options.Voice = voiceElement.GetString();
                    }
                    else if (voiceElement.ValueKind == JsonValueKind.Object &&
                             voiceElement.TryGetProperty("id", out var voiceIdElement))
                    {
                        options.Voice = voiceIdElement.GetString();
                    }
                }
            }
        }

        return options;
    }

    private static RealtimeServerErrorMessage? CreateErrorMessage(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var errorElement) ||
            !errorElement.TryGetProperty("message", out var messageElement))
        {
            return null;
        }

        var msg = new RealtimeServerErrorMessage
        {
            Error = new ErrorContent(messageElement.GetString()),
        };

        if (errorElement.TryGetProperty("code", out var codeElement))
        {
            msg.Error.ErrorCode = codeElement.GetString();
        }

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        if (errorElement.TryGetProperty("param", out var paramElement))
        {
            msg.Parameter = paramElement.GetString();
        }

        return msg;
    }

    private static RealtimeServerInputAudioTranscriptionMessage? CreateInputAudioTranscriptionMessage(JsonElement root, string messageType)
    {
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "conversation.item.input_audio_transcription.delta" => RealtimeServerMessageType.InputAudioTranscriptionDelta,
            "conversation.item.input_audio_transcription.completed" => RealtimeServerMessageType.InputAudioTranscriptionCompleted,
            "conversation.item.input_audio_transcription.failed" => RealtimeServerMessageType.InputAudioTranscriptionFailed,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        var msg = new RealtimeServerInputAudioTranscriptionMessage(serverMessageType);

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        if (root.TryGetProperty("content_index", out var contentIndexElement))
        {
            msg.ContentIndex = contentIndexElement.GetInt32();
        }

        if (root.TryGetProperty("item_id", out var itemIdElement))
        {
            msg.ItemId = itemIdElement.GetString();
        }

        if (root.TryGetProperty("delta", out var deltaElement))
        {
            msg.Transcription = deltaElement.GetString();
        }

        if (msg.Transcription is null && root.TryGetProperty("transcript", out deltaElement))
        {
            msg.Transcription = deltaElement.GetString();
        }

        if (root.TryGetProperty("error", out var errorElement) &&
            errorElement.TryGetProperty("message", out var errorMsgElement))
        {
            var errorContent = new ErrorContent(errorMsgElement.GetString());

            if (errorElement.TryGetProperty("code", out var errorCodeElement))
            {
                errorContent.ErrorCode = errorCodeElement.GetString();
            }

            if (errorElement.TryGetProperty("param", out var errorParamElement))
            {
                errorContent.Details = errorParamElement.GetString();
            }

            msg.Error = errorContent;
        }

        if (root.TryGetProperty("usage", out var usageElement))
        {
            msg.Usage = ParseUsageDetails(usageElement, requireTypeCheck: true);
        }

        return msg;
    }

    private static RealtimeServerOutputTextAudioMessage? CreateOutputTextAudioMessage(JsonElement root, string messageType)
    {
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "response.output_audio.delta" => RealtimeServerMessageType.OutputAudioDelta,
            "response.output_audio.done" => RealtimeServerMessageType.OutputAudioDone,
            "response.output_audio_transcript.delta" => RealtimeServerMessageType.OutputAudioTranscriptionDelta,
            "response.output_audio_transcript.done" => RealtimeServerMessageType.OutputAudioTranscriptionDone,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        var msg = new RealtimeServerOutputTextAudioMessage(serverMessageType);

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        if (root.TryGetProperty("response_id", out var responseIdElement))
        {
            msg.ResponseId = responseIdElement.GetString();
        }

        if (root.TryGetProperty("item_id", out var itemIdElement))
        {
            msg.ItemId = itemIdElement.GetString();
        }

        if (root.TryGetProperty("output_index", out var outputIndexElement))
        {
            msg.OutputIndex = outputIndexElement.GetInt32();
        }

        if (root.TryGetProperty("content_index", out var contentIndexElement))
        {
            msg.ContentIndex = contentIndexElement.GetInt32();
        }

        if (root.TryGetProperty("delta", out var deltaElement))
        {
            msg.Text = deltaElement.GetString();
        }

        if (msg.Text is null && root.TryGetProperty("transcript", out deltaElement))
        {
            msg.Text = deltaElement.GetString();
        }

        return msg;
    }

    private static RealtimeServerResponseOutputItemMessage? CreateResponseOutItemMessage(JsonElement root, string messageType)
    {
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "response.output_item.added" => RealtimeServerMessageType.ResponseOutputItemAdded,
            "response.output_item.done" => RealtimeServerMessageType.ResponseOutputItemDone,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        var msg = new RealtimeServerResponseOutputItemMessage(serverMessageType);

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        if (root.TryGetProperty("response_id", out var responseIdElement))
        {
            msg.ResponseId = responseIdElement.GetString();
        }

        if (root.TryGetProperty("output_index", out var outputIndexElement))
        {
            msg.OutputIndex = outputIndexElement.GetInt32();
        }

        if (root.TryGetProperty("item", out var itemElement))
        {
            msg.Item = ParseRealtimeContentItem(itemElement);
        }

        return msg;
    }

    private static RealtimeServerResponseCreatedMessage? CreateResponseCreatedMessage(JsonElement root, string messageType)
    {
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "response.created" => RealtimeServerMessageType.ResponseCreated,
            "response.done" => RealtimeServerMessageType.ResponseDone,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        if (!root.TryGetProperty("response", out var responseElement))
        {
            return null;
        }

        var msg = new RealtimeServerResponseCreatedMessage(serverMessageType);

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        if (responseElement.TryGetProperty("audio", out var responseAudioElement) &&
            responseAudioElement.ValueKind == JsonValueKind.Object &&
            responseAudioElement.TryGetProperty("output", out var outputElement) &&
            outputElement.ValueKind == JsonValueKind.Object)
        {
            if (outputElement.TryGetProperty("format", out var formatElement))
            {
                msg.OutputAudioOptions = ParseAudioFormat(formatElement);
            }

            if (outputElement.TryGetProperty("voice", out var voiceElement))
            {
                msg.OutputVoice = voiceElement.GetString();
            }
        }

        if (responseElement.TryGetProperty("conversation_id", out var conversationIdElement))
        {
            msg.ConversationId = conversationIdElement.GetString();
        }

        if (responseElement.TryGetProperty("id", out var idElement))
        {
            msg.ResponseId = idElement.GetString();
        }

        if (responseElement.TryGetProperty("max_output_tokens", out var maxOutputTokensElement))
        {
            msg.MaxOutputTokens = ParseMaxOutputTokens(maxOutputTokensElement);
        }

        if (responseElement.TryGetProperty("metadata", out var metadataElement) &&
            metadataElement.ValueKind == JsonValueKind.Object)
        {
            var metadataDict = new AdditionalPropertiesDictionary();
            foreach (var property in metadataElement.EnumerateObject())
            {
                metadataDict[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }

            msg.Metadata = metadataDict;
        }

        if (responseElement.TryGetProperty("output_modalities", out var outputModalitiesElement))
        {
            msg.OutputModalities = ParseOutputModalities(outputModalitiesElement);
        }

        if (responseElement.TryGetProperty("status", out var statusElement))
        {
            msg.Status = statusElement.GetString();
        }

        if (responseElement.TryGetProperty("status_details", out var statusDetailsElement) &&
            statusDetailsElement.ValueKind == JsonValueKind.Object &&
            statusDetailsElement.TryGetProperty("error", out var errorElement) &&
            errorElement.ValueKind == JsonValueKind.Object &&
            errorElement.TryGetProperty("type", out var errorTypeElement) &&
            errorElement.TryGetProperty("code", out var errorCodeElement))
        {
            msg.Error = new ErrorContent(errorTypeElement.GetString())
            {
                ErrorCode = errorCodeElement.GetString(),
            };
        }

        if (responseElement.TryGetProperty("usage", out var usageElement))
        {
            msg.Usage = ParseUsageDetails(usageElement);
        }

        if (responseElement.TryGetProperty("output", out outputElement) &&
            outputElement.ValueKind == JsonValueKind.Array)
        {
            var outputItems = new List<RealtimeContentItem>();
            foreach (var outputItemElement in outputElement.EnumerateArray())
            {
                if (ParseRealtimeContentItem(outputItemElement) is RealtimeContentItem item)
                {
                    outputItems.Add(item);
                }
            }

            msg.Items = outputItems;
        }

        return msg;
    }

    private static RealtimeServerResponseOutputItemMessage? CreateMcpCallMessage(JsonElement root, string messageType)
    {
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "mcp_call.in_progress" => RealtimeServerMessageType.McpCallInProgress,
            "mcp_call.completed" => RealtimeServerMessageType.McpCallCompleted,
            "mcp_call.failed" => RealtimeServerMessageType.McpCallFailed,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        var msg = new RealtimeServerResponseOutputItemMessage(serverMessageType)
        {
            RawRepresentation = root.Clone(),
        };

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        if (root.TryGetProperty("response_id", out var responseIdElement))
        {
            msg.ResponseId = responseIdElement.GetString();
        }

        if (root.TryGetProperty("output_index", out var outputIndexElement))
        {
            msg.OutputIndex = outputIndexElement.GetInt32();
        }

        if (root.TryGetProperty("item", out var itemElement))
        {
            msg.Item = ParseRealtimeContentItem(itemElement);
        }
        else if (root.TryGetProperty("item_id", out var itemIdElement))
        {
            // Some MCP events only include item_id without the full item
            msg.Item = new RealtimeContentItem([], itemIdElement.GetString());
        }

        return msg;
    }

    private static RealtimeServerResponseOutputItemMessage CreateMcpListToolsMessage(JsonElement root, string messageType)
    {
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "mcp_list_tools.in_progress" => RealtimeServerMessageType.McpListToolsInProgress,
            "mcp_list_tools.completed" => RealtimeServerMessageType.McpListToolsCompleted,
            "mcp_list_tools.failed" => RealtimeServerMessageType.McpListToolsFailed,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        var msg = new RealtimeServerResponseOutputItemMessage(serverMessageType)
        {
            RawRepresentation = root.Clone(),
            EventId = root.TryGetProperty("event_id", out var eventIdElement) ? eventIdElement.GetString() : null,
        };

        string? itemId = root.TryGetProperty("item_id", out var itemIdElement) ? itemIdElement.GetString() : null;

        // For completed events, parse the tools list from the item
        if (root.TryGetProperty("item", out var itemElement) && itemElement.ValueKind == JsonValueKind.Object)
        {
            itemId ??= itemElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            string? serverLabel = itemElement.TryGetProperty("server_label", out var slElement) ? slElement.GetString() : null;

            var contents = new List<AIContent>();
            if (itemElement.TryGetProperty("tools", out var toolsArrayElement) &&
                toolsArrayElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var toolElement in toolsArrayElement.EnumerateArray())
                {
                    string? toolName = toolElement.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                    string? toolDesc = toolElement.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;

                    if (toolName is not null)
                    {
                        // Represent each discovered tool as an McpServerToolCallContent with no arguments
                        // so consumers can enumerate tool names and descriptions.
                        contents.Add(new McpServerToolCallContent(toolName, toolName, serverLabel)
                        {
                            RawRepresentation = toolElement.Clone(),
                        });
                    }
                }
            }

            msg.Item = new RealtimeContentItem(contents, itemId);
        }
        else if (itemId is not null)
        {
            msg.Item = new RealtimeContentItem([], itemId);
        }

        return msg;
    }

    private static RealtimeServerResponseOutputItemMessage? CreateConversationItemMessage(JsonElement root, string messageType)
    {
        // conversation.item.added and conversation.item.done carry an "item" property
        // which may be an MCP item (mcp_call, mcp_approval_request) or a regular item.
        RealtimeServerMessageType serverMessageType = messageType switch
        {
            "conversation.item.added" => RealtimeServerMessageType.ResponseOutputItemAdded,
            "conversation.item.done" => RealtimeServerMessageType.ResponseOutputItemDone,
            _ => throw new InvalidOperationException($"Unknown message type: {messageType}"),
        };

        if (!root.TryGetProperty("item", out var itemElement))
        {
            return null;
        }

        var item = ParseRealtimeContentItem(itemElement);
        if (item is null)
        {
            // If we couldn't parse the item into a typed representation, return as raw
            return new RealtimeServerResponseOutputItemMessage(RealtimeServerMessageType.RawContentOnly)
            {
                RawRepresentation = root.Clone(),
                EventId = root.TryGetProperty("event_id", out var evtElement) ? evtElement.GetString() : null,
            };
        }

        var msg = new RealtimeServerResponseOutputItemMessage(serverMessageType)
        {
            Item = item,
            RawRepresentation = root.Clone(),
        };

        if (root.TryGetProperty("event_id", out var eventIdElement))
        {
            msg.EventId = eventIdElement.GetString();
        }

        return msg;
    }

    #endregion
}
