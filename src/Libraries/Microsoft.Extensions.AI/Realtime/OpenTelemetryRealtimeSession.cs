// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter
#pragma warning disable SA1204 // Static members should appear before non-static members

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating realtime session that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// <para>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.38, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this session is also subject to change.
/// </para>
/// <para>
/// The following standard OpenTelemetry GenAI conventions are supported:
/// <list type="bullet">
///   <item><c>gen_ai.operation.name</c> - Operation name ("chat")</item>
///   <item><c>gen_ai.request.model</c> - Model name from options</item>
///   <item><c>gen_ai.provider.name</c> - Provider name from metadata</item>
///   <item><c>gen_ai.response.id</c> - Response ID from ResponseDone messages</item>
///   <item><c>gen_ai.response.model</c> - Model ID from response</item>
///   <item><c>gen_ai.usage.input_tokens</c> - Input token count</item>
///   <item><c>gen_ai.usage.output_tokens</c> - Output token count</item>
///   <item><c>gen_ai.request.max_tokens</c> - Max output tokens from options</item>
///   <item><c>gen_ai.system_instructions</c> - Instructions from options (sensitive data)</item>
///   <item><c>gen_ai.conversation.id</c> - Conversation ID from response</item>
///   <item><c>gen_ai.tool.definitions</c> - Tool definitions (sensitive data)</item>
///   <item><c>gen_ai.input.messages</c> - Input tool/MCP messages (sensitive data)</item>
///   <item><c>gen_ai.output.messages</c> - Output tool/MCP messages (sensitive data)</item>
///   <item><c>server.address</c> / <c>server.port</c> - Server endpoint info</item>
///   <item><c>error.type</c> - Error type on failures</item>
/// </list>
/// </para>
/// <para>
/// MCP (Model Context Protocol) semantic conventions are supported for tool calls and responses, including:
/// <list type="bullet">
///   <item>MCP server tool calls and results</item>
///   <item>MCP approval requests and responses</item>
///   <item>Function calls and results</item>
/// </list>
/// </para>
/// <para>
/// Additionally, the following custom attributes are supported (not part of OpenTelemetry GenAI semantic conventions as of v1.39):
/// <list type="bullet">
///   <item><c>gen_ai.request.tool_choice</c> - Tool choice mode ("none", "auto", "required") or specific tool name</item>
///   <item><c>gen_ai.realtime.voice</c> - Voice setting from options</item>
///   <item><c>gen_ai.realtime.output_modalities</c> - Output modalities (text, audio)</item>
///   <item><c>gen_ai.realtime.voice_speed</c> - Voice speed setting</item>
///   <item><c>gen_ai.realtime.session_kind</c> - Session kind (Realtime/Transcription)</item>
/// </list>
/// </para>
/// <para>
/// Metrics include:
/// <list type="bullet">
///   <item><c>gen_ai.client.operation.duration</c> - Duration histogram</item>
///   <item><c>gen_ai.client.token.usage</c> - Token usage histogram</item>
/// </list>
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public sealed partial class OpenTelemetryRealtimeSession : DelegatingRealtimeSession
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _defaultModelId;
    private readonly string? _providerName;
    private readonly string? _serverAddress;
    private readonly int _serverPort;

    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryRealtimeSession"/> class.</summary>
    /// <param name="innerSession">The underlying <see cref="IRealtimeSession"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting any logging data from the session.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
#pragma warning disable IDE0060 // Remove unused parameter; it exists for backwards compatibility and future use
    public OpenTelemetryRealtimeSession(IRealtimeSession innerSession, ILogger? logger = null, string? sourceName = null)
#pragma warning restore IDE0060
        : base(innerSession)
    {
        Debug.Assert(innerSession is not null, "Should have been validated by the base ctor");

        // Try to get metadata from the inner session's ChatClientMetadata if available
        if (innerSession!.GetService(typeof(ChatClientMetadata)) is ChatClientMetadata metadata)
        {
            _defaultModelId = metadata.DefaultModelId;
            _providerName = metadata.ProviderName;
            _serverAddress = metadata.ProviderUri?.Host;
            _serverPort = metadata.ProviderUri?.Port ?? 0;
        }

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries }
            );

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
            );

        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when formatting realtime data into telemetry strings.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activitySource.Dispose();
            _meter.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Gets or sets a value indicating whether potentially sensitive information should be included in telemetry.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if potentially sensitive information should be included in telemetry;
    /// <see langword="false"/> if telemetry shouldn't include raw inputs and outputs.
    /// The default value is <see langword="false"/>, unless the <c>OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT</c>
    /// environment variable is set to "true" (case-insensitive).
    /// </value>
    /// <remarks>
    /// By default, telemetry includes metadata, such as token counts, but not raw inputs
    /// and outputs, such as message content, function call arguments, and function call results.
    /// The default value can be overridden by setting the <c>OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT</c>
    /// environment variable to "true". Explicitly setting this property will override the environment variable.
    /// </remarks>
    public bool EnableSensitiveData { get; set; } = TelemetryHelpers.EnableSensitiveDataDefault;

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource) ? _activitySource :
        base.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public override async Task UpdateAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(options);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options.Model ?? _defaultModelId;

        Exception? error = null;
        try
        {
            await base.UpdateAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceUpdateResponse(activity, requestModelId, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);
        _jsonSerializerOptions.MakeReadOnly();

        RealtimeSessionOptions? options = Options;
        string? requestModelId = options?.Model ?? _defaultModelId;

        // Start timing from the beginning of the streaming operation
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        // Determine if we should capture messages for telemetry
        bool captureMessages = EnableSensitiveData && _activitySource.HasListeners();

        // Wrap client messages to capture input content and create input activity
        IAsyncEnumerable<RealtimeClientMessage> wrappedUpdates = captureMessages
            ? WrapClientMessagesForTelemetryAsync(updates, options, cancellationToken)
            : updates;

        IAsyncEnumerable<RealtimeServerMessage> responses;
        try
        {
            responses = base.GetStreamingResponseAsync(wrappedUpdates, cancellationToken);
        }
        catch (Exception ex)
        {
            // Create an activity for the error case
            using Activity? errorActivity = CreateAndConfigureActivity(options);
            TraceStreamingResponse(errorActivity, requestModelId, response: null, ex, stopwatch);
            throw;
        }

        var responseEnumerator = responses.GetAsyncEnumerator(cancellationToken);
        Exception? error = null;
        List<RealtimeOtelMessage>? outputMessages = captureMessages ? [] : null;
        HashSet<string>? outputModalities = _activitySource.HasListeners() ? [] : null;
        try
        {
            while (true)
            {
                RealtimeServerMessage message;
                try
                {
                    if (!await responseEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    message = responseEnumerator.Current;
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }

                // Track output modalities
                if (outputModalities is not null)
                {
                    var modality = GetOutputModality(message);
                    if (modality is not null)
                    {
                        _ = outputModalities.Add(modality);
                    }
                }

                // Capture output content from all server message types
                if (outputMessages is not null)
                {
                    var otelMessage = ExtractServerOtelMessage(message);
                    if (otelMessage is not null)
                    {
                        outputMessages.Add(otelMessage);
                    }
                }

                // Create activity for ResponseDone message for telemetry
                if (message is RealtimeServerResponseCreatedMessage responseDoneMsg &&
                    responseDoneMsg.Type == RealtimeServerMessageType.ResponseDone)
                {
                    using Activity? responseActivity = CreateAndConfigureActivity(options);

                    // Add output modalities and messages tags
                    AddOutputModalitiesTag(responseActivity, outputModalities);
                    AddOutputMessagesTag(responseActivity, outputMessages);
                    TraceStreamingResponse(responseActivity, requestModelId, responseDoneMsg, error, stopwatch);
                }

                yield return message;
            }
        }
        finally
        {
            // Trace error if an exception was thrown during streaming
            if (error is not null)
            {
                using Activity? errorActivity = CreateAndConfigureActivity(options);
                AddOutputModalitiesTag(errorActivity, outputModalities);
                AddOutputMessagesTag(errorActivity, outputMessages);
                TraceStreamingResponse(errorActivity, requestModelId, response: null, error, stopwatch);
            }

            await responseEnumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Wraps client messages to capture content for telemetry with its own activity.</summary>
    private async IAsyncEnumerable<RealtimeClientMessage> WrapClientMessagesForTelemetryAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates,
        RealtimeSessionOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.Model ?? _defaultModelId;

        await foreach (var message in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Capture input content from current client message
            var otelMessage = ExtractClientOtelMessage(message);

            // Only create activity when there's content to log
            if (otelMessage is not null)
            {
                using Activity? inputActivity = CreateAndConfigureActivity(options: null);
                if (inputActivity is { IsAllDataRequested: true })
                {
                    _ = inputActivity.AddTag(OpenTelemetryConsts.GenAI.Input.Messages, SerializeMessage(otelMessage));
                }

                // Record metrics
                if (_operationDurationHistogram.Enabled && stopwatch is not null)
                {
                    TagList tags = default;
                    AddMetricTags(ref tags, requestModelId, responseModelId: null);
                    _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
                }
            }

            yield return message;
        }
    }

    /// <summary>Adds output modalities tag to the activity.</summary>
    private static void AddOutputModalitiesTag(Activity? activity, HashSet<string>? outputModalities)
    {
        if (activity is { IsAllDataRequested: true } && outputModalities is { Count: > 0 })
        {
            _ = activity.AddTag(OpenTelemetryConsts.GenAI.Realtime.ReceivedModalities, $"[{string.Join(", ", outputModalities.Select(m => $"\"{m}\""))}]");
        }
    }

    /// <summary>Adds output messages tag to the activity if there are messages to add.</summary>
    private static void AddOutputMessagesTag(Activity? activity, List<RealtimeOtelMessage>? outputMessages)
    {
        if (activity is { IsAllDataRequested: true } && outputMessages is { Count: > 0 })
        {
            _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Messages, SerializeMessages(outputMessages));
        }
    }

    /// <summary>Gets the output modality from a server message, if applicable.</summary>
    private static string? GetOutputModality(RealtimeServerMessage message)
    {
        if (message is RealtimeServerOutputTextAudioMessage textAudio)
        {
            if (textAudio.Type == RealtimeServerMessageType.OutputTextDelta || textAudio.Type == RealtimeServerMessageType.OutputTextDone)
            {
                return "text";
            }

            if (textAudio.Type == RealtimeServerMessageType.OutputAudioDelta || textAudio.Type == RealtimeServerMessageType.OutputAudioDone)
            {
                return "audio";
            }

            if (textAudio.Type == RealtimeServerMessageType.OutputAudioTranscriptionDelta || textAudio.Type == RealtimeServerMessageType.OutputAudioTranscriptionDone)
            {
                return "transcription";
            }
        }

        if (message is RealtimeServerResponseOutputItemMessage)
        {
            return "item";
        }

        return null;
    }

    /// <summary>Extracts an OTel message from a realtime client message.</summary>
    private RealtimeOtelMessage? ExtractClientOtelMessage(RealtimeClientMessage message)
    {
        switch (message)
        {
            case RealtimeClientConversationItemCreateMessage createMsg:
                return ExtractOtelMessage(createMsg.Item);

            case RealtimeClientInputAudioBufferAppendMessage audioAppendMsg:
                var audioMessage = new RealtimeOtelMessage { Role = "user" };
                audioMessage.Parts.Add(new RealtimeOtelBlobPart
                {
                    Content = audioAppendMsg.Content.Base64Data.ToString(),
                    MimeType = audioAppendMsg.Content.MediaType,
                    Modality = "audio",
                });
                return audioMessage;

            case RealtimeClientInputAudioBufferCommitMessage:
                // Commit message has no content, just a signal
                return new RealtimeOtelMessage
                {
                    Role = "user",
                    Parts = { new RealtimeOtelGenericPart { Type = "audio_commit" } },
                };

            case RealtimeClientResponseCreateMessage responseCreateMsg:
                var responseMessage = new RealtimeOtelMessage { Role = "user" };

                // Add instructions if present
                if (!string.IsNullOrWhiteSpace(responseCreateMsg.Instructions))
                {
                    responseMessage.Parts.Add(new RealtimeOtelGenericPart
                    {
                        Type = "instructions",
                        Content = responseCreateMsg.Instructions,
                    });
                }

                // Add items if present
                if (responseCreateMsg.Items is { Count: > 0 } items)
                {
                    foreach (var item in items)
                    {
                        var itemMessage = ExtractOtelMessage(item);
                        if (itemMessage is not null)
                        {
                            foreach (var part in itemMessage.Parts)
                            {
                                responseMessage.Parts.Add(part);
                            }
                        }
                    }
                }

                return responseMessage.Parts.Count > 0 ? responseMessage : null;

            default:
                return null;
        }
    }

    /// <summary>Extracts an OTel message from a realtime server message.</summary>
    private RealtimeOtelMessage? ExtractServerOtelMessage(RealtimeServerMessage message)
    {
        switch (message)
        {
            case RealtimeServerResponseOutputItemMessage outputItemMsg:
                return ExtractOtelMessage(outputItemMsg.Item);

            case RealtimeServerOutputTextAudioMessage textAudioMsg:
                string partType;
                string? content;

                if (textAudioMsg.Type == RealtimeServerMessageType.OutputAudioDelta || textAudioMsg.Type == RealtimeServerMessageType.OutputAudioDone)
                {
                    partType = "audio";
                    content = string.IsNullOrEmpty(textAudioMsg.Audio) ? "[audio data]" : textAudioMsg.Audio;
                }
                else if (textAudioMsg.Type == RealtimeServerMessageType.OutputAudioTranscriptionDelta || textAudioMsg.Type == RealtimeServerMessageType.OutputAudioTranscriptionDone)
                {
                    partType = "output_transcription";
                    content = textAudioMsg.Text;
                }
                else
                {
                    partType = "text";
                    content = textAudioMsg.Text;
                }

                // Skip if no meaningful content
                if (string.IsNullOrEmpty(content))
                {
                    return null;
                }

                var textAudioOtelMessage = new RealtimeOtelMessage { Role = "assistant" };
                textAudioOtelMessage.Parts.Add(new RealtimeOtelGenericPart
                {
                    Type = partType,
                    Content = content,
                });
                return textAudioOtelMessage;

            case RealtimeServerInputAudioTranscriptionMessage transcriptionMsg when !string.IsNullOrEmpty(transcriptionMsg.Transcription):
                var transcriptionOtelMessage = new RealtimeOtelMessage { Role = "user" };
                transcriptionOtelMessage.Parts.Add(new RealtimeOtelGenericPart
                {
                    Type = "input_transcription",
                    Content = transcriptionMsg.Transcription,
                });
                return transcriptionOtelMessage;

            case RealtimeServerErrorMessage errorMsg when errorMsg.Error is not null:
                var errorOtelMessage = new RealtimeOtelMessage { Role = "system" };
                errorOtelMessage.Parts.Add(new RealtimeOtelGenericPart
                {
                    Type = "error",
                    Content = errorMsg.Error.Message,
                });
                return errorOtelMessage;

            case RealtimeServerResponseCreatedMessage responseCreatedMsg when responseCreatedMsg.Items is { Count: > 0 }:
                // Only capture items from ResponseCreated, not ResponseDone (which we use for tracing)
                if (responseCreatedMsg.Type == RealtimeServerMessageType.ResponseCreated)
                {
                    var responseOtelMessage = new RealtimeOtelMessage { Role = "assistant" };
                    foreach (var item in responseCreatedMsg.Items)
                    {
                        var itemMessage = ExtractOtelMessage(item);
                        if (itemMessage is not null)
                        {
                            foreach (var part in itemMessage.Parts)
                            {
                                responseOtelMessage.Parts.Add(part);
                            }
                        }
                    }

                    return responseOtelMessage.Parts.Count > 0 ? responseOtelMessage : null;
                }

                return null;

            default:
                return null;
        }
    }

    /// <summary>Serializes a single message to OTel format (as an array with one element).</summary>
    private static string SerializeMessage(RealtimeOtelMessage message)
    {
        return JsonSerializer.Serialize(new[] { message }, RealtimeOtelContext.Default.IEnumerableRealtimeOtelMessage);
    }

    /// <summary>Serializes content items to OTel format.</summary>
    private static string SerializeMessages(IEnumerable<RealtimeOtelMessage> messages)
    {
        return JsonSerializer.Serialize(messages, RealtimeOtelContext.Default.IEnumerableRealtimeOtelMessage);
    }

    /// <summary>Extracts content from an AIContent list and converts to OTel format.</summary>
    private RealtimeOtelMessage? ExtractOtelMessage(RealtimeContentItem? item)
    {
        if (item?.Contents is null or { Count: 0 })
        {
            return null;
        }

        var message = new RealtimeOtelMessage
        {
            Role = item.Role?.Value,
        };

        foreach (var content in item.Contents)
        {
            switch (content)
            {
                // Standard text content
                case TextContent tc when !string.IsNullOrEmpty(tc.Text):
                    message.Parts.Add(new RealtimeOtelGenericPart { Content = tc.Text });
                    break;

                case TextReasoningContent trc when !string.IsNullOrEmpty(trc.Text):
                    message.Parts.Add(new RealtimeOtelGenericPart { Type = "reasoning", Content = trc.Text });
                    break;

                // Function call content
                case FunctionCallContent fcc:
                    message.Parts.Add(new RealtimeOtelToolCallPart
                    {
                        Id = fcc.CallId,
                        Name = fcc.Name,
                        Arguments = fcc.Arguments,
                    });
                    break;

                case FunctionResultContent frc:
                    message.Parts.Add(new RealtimeOtelToolCallResponsePart
                    {
                        Id = frc.CallId,
                        Response = frc.Result,
                    });
                    break;

                // Data content (binary data)
                case DataContent dc:
                    message.Parts.Add(new RealtimeOtelBlobPart
                    {
                        Content = dc.Base64Data.ToString(),
                        MimeType = dc.MediaType,
                        Modality = DeriveModalityFromMediaType(dc.MediaType),
                    });
                    break;

                // URI content
                case UriContent uc:
                    message.Parts.Add(new RealtimeOtelUriPart
                    {
                        Uri = uc.Uri.AbsoluteUri,
                        MimeType = uc.MediaType,
                        Modality = DeriveModalityFromMediaType(uc.MediaType),
                    });
                    break;

                // Hosted file content
                case HostedFileContent fc:
                    message.Parts.Add(new RealtimeOtelFilePart
                    {
                        FileId = fc.FileId,
                        MimeType = fc.MediaType,
                        Modality = DeriveModalityFromMediaType(fc.MediaType),
                    });
                    break;

                // Non-standard "generic" parts
                case HostedVectorStoreContent vsc:
                    message.Parts.Add(new RealtimeOtelGenericPart { Type = "vector_store", Content = vsc.VectorStoreId });
                    break;

                case ErrorContent ec:
                    message.Parts.Add(new RealtimeOtelGenericPart { Type = "error", Content = ec.Message });
                    break;

                // MCP server tool content
                case McpServerToolCallContent mstcc:
                    message.Parts.Add(new RealtimeOtelServerToolCallPart<RealtimeOtelMcpToolCall>
                    {
                        Id = mstcc.CallId,
                        Name = mstcc.ToolName,
                        ServerToolCall = new RealtimeOtelMcpToolCall
                        {
                            Arguments = mstcc.Arguments,
                            ServerName = mstcc.ServerName,
                        },
                    });
                    break;

                case McpServerToolResultContent mstrc:
                    message.Parts.Add(new RealtimeOtelServerToolCallResponsePart<RealtimeOtelMcpToolCallResponse>
                    {
                        Id = mstrc.CallId,
                        ServerToolCallResponse = new RealtimeOtelMcpToolCallResponse
                        {
                            Output = mstrc.Output,
                        },
                    });
                    break;

                case McpServerToolApprovalRequestContent mstarc:
                    message.Parts.Add(new RealtimeOtelServerToolCallPart<RealtimeOtelMcpApprovalRequest>
                    {
                        Id = mstarc.Id,
                        Name = mstarc.ToolCall.ToolName,
                        ServerToolCall = new RealtimeOtelMcpApprovalRequest
                        {
                            Arguments = mstarc.ToolCall.Arguments,
                            ServerName = mstarc.ToolCall.ServerName,
                        },
                    });
                    break;

                case McpServerToolApprovalResponseContent mstaresp:
                    message.Parts.Add(new RealtimeOtelServerToolCallResponsePart<RealtimeOtelMcpApprovalResponse>
                    {
                        Id = mstaresp.Id,
                        ServerToolCallResponse = new RealtimeOtelMcpApprovalResponse
                        {
                            Approved = mstaresp.Approved,
                        },
                    });
                    break;

                default:
                    // For unknown content types, try to serialize them
                    JsonElement element = default;
                    try
                    {
                        JsonTypeInfo? unknownContentTypeInfo = null;
                        if (_jsonSerializerOptions?.TryGetTypeInfo(content.GetType(), out JsonTypeInfo? ctsi) ?? false)
                        {
                            unknownContentTypeInfo = ctsi;
                        }
                        else if (AIJsonUtilities.DefaultOptions.TryGetTypeInfo(content.GetType(), out JsonTypeInfo? dtsi))
                        {
                            unknownContentTypeInfo = dtsi;
                        }

                        if (unknownContentTypeInfo is not null)
                        {
                            element = JsonSerializer.SerializeToElement(content, unknownContentTypeInfo);
                        }
                    }
                    catch
                    {
                        // Ignore serialization failures
                    }

                    if (element.ValueKind != JsonValueKind.Undefined)
                    {
                        message.Parts.Add(new RealtimeOtelGenericPart
                        {
                            Type = content.GetType().Name,
                            Content = element,
                        });
                    }

                    break;
            }
        }

        return message.Parts.Count > 0 ? message : null;
    }

    /// <summary>Derives modality from media type for telemetry purposes.</summary>
    private static string? DeriveModalityFromMediaType(string? mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
        {
            return null;
        }

        if (mediaType!.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return "audio";
        }

        if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return "video";
        }

        return null;
    }

    /// <summary>Creates an activity for a realtime session request, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(RealtimeSessionOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.Model ?? _defaultModelId;

            activity = _activitySource.StartActivity(
                string.IsNullOrWhiteSpace(modelId) ? OpenTelemetryConsts.GenAI.RealtimeName : $"{OpenTelemetryConsts.GenAI.RealtimeName} {modelId}",
                ActivityKind.Client);

            if (activity is { IsAllDataRequested: true })
            {
                _ = activity
                    .AddTag(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.ChatName)
                    .AddTag(OpenTelemetryConsts.GenAI.Request.Model, modelId)
                    .AddTag(OpenTelemetryConsts.GenAI.Provider.Name, _providerName);

                if (_serverAddress is not null)
                {
                    _ = activity
                        .AddTag(OpenTelemetryConsts.Server.Address, _serverAddress)
                        .AddTag(OpenTelemetryConsts.Server.Port, _serverPort);
                }

                if (options is not null)
                {
                    // Standard GenAI attributes
                    if (options.MaxOutputTokens is int maxTokens)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.MaxTokens, maxTokens);
                    }

                    // Realtime-specific attributes
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Realtime.SessionKind, options.SessionKind.ToString());

                    if (!string.IsNullOrEmpty(options.Voice))
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Realtime.Voice, options.Voice);
                    }

#pragma warning disable S1244 // Floating point numbers should not be tested for equality
                    if (options.VoiceSpeed != 1.0)
#pragma warning restore S1244
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Realtime.VoiceSpeed, options.VoiceSpeed);
                    }

                    if (options.OutputModalities is { Count: > 0 } modalities)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Realtime.OutputModalities, $"[{string.Join(", ", modalities.Select(m => $"\"{m}\""))}]");
                    }

                    if (EnableSensitiveData)
                    {
                        if (!string.IsNullOrWhiteSpace(options.Instructions))
                        {
                            _ = activity.AddTag(
                                OpenTelemetryConsts.GenAI.SystemInstructions,
                                JsonSerializer.Serialize(new object[1] { new RealtimeOtelGenericPart { Content = options.Instructions } }, RealtimeOtelContext.Default.IListObject));
                        }

                        if (options.Tools is { Count: > 0 })
                        {
                            _ = activity.AddTag(
                                OpenTelemetryConsts.GenAI.Tool.Definitions,
                                JsonSerializer.Serialize(options.Tools.Select(t => t switch
                                {
                                    _ when t.GetService<AIFunctionDeclaration>() is { } af => new RealtimeOtelFunction
                                    {
                                        Name = af.Name,
                                        Description = af.Description,
                                        Parameters = af.JsonSchema,
                                    },
                                    _ => new RealtimeOtelFunction { Type = t.Name },
                                }), RealtimeOtelContext.Default.IEnumerableRealtimeOtelFunction));
                        }
                    }

                    // Tool choice mode (custom attribute - not part of OTel GenAI spec)
                    string? toolChoice = null;
                    if (options.ToolMode is { } toolMode)
                    {
                        toolChoice = toolMode switch
                        {
                            RequiredChatToolMode r when r.RequiredFunctionName is not null => r.RequiredFunctionName,
                            RequiredChatToolMode => "required",
                            NoneChatToolMode => "none",
                            _ => "auto",
                        };
                    }

                    if (toolChoice is not null)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.ToolChoice, toolChoice);
                    }
                }
            }
        }

        return activity;
    }

    /// <summary>Adds update operation response information to the activity.</summary>
    private void TraceUpdateResponse(
        Activity? activity,
        string? requestModelId,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;
            AddMetricTags(ref tags, requestModelId, responseModelId: null);

            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (error is not null)
        {
            _ = activity?
                .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }
    }

    /// <summary>Adds streaming response information to the activity.</summary>
    private void TraceStreamingResponse(
        Activity? activity,
        string? requestModelId,
        RealtimeServerResponseCreatedMessage? response,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;
            AddMetricTags(ref tags, requestModelId, responseModelId: null);

            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && response?.Usage is { } usage)
        {
            if (usage.InputTokenCount is long inputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeInput);
                AddMetricTags(ref tags, requestModelId, responseModelId: null);
                _tokenUsageHistogram.Record((int)inputTokens, tags);
            }

            if (usage.OutputTokenCount is long outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeOutput);
                AddMetricTags(ref tags, requestModelId, responseModelId: null);
                _tokenUsageHistogram.Record((int)outputTokens, tags);
            }

            if (usage.InputAudioTokenCount is long inputAudioTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeInputAudio);
                AddMetricTags(ref tags, requestModelId, responseModelId: null);
                _tokenUsageHistogram.Record((int)inputAudioTokens, tags);
            }

            if (usage.InputTextTokenCount is long inputTextTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeInputText);
                AddMetricTags(ref tags, requestModelId, responseModelId: null);
                _tokenUsageHistogram.Record((int)inputTextTokens, tags);
            }

            if (usage.OutputAudioTokenCount is long outputAudioTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeOutputAudio);
                AddMetricTags(ref tags, requestModelId, responseModelId: null);
                _tokenUsageHistogram.Record((int)outputAudioTokens, tags);
            }

            if (usage.OutputTextTokenCount is long outputTextTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeOutputText);
                AddMetricTags(ref tags, requestModelId, responseModelId: null);
                _tokenUsageHistogram.Record((int)outputTextTokens, tags);
            }
        }

        if (error is not null)
        {
            _ = activity?
                .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }

        if (response is not null && activity is not null)
        {
            // Log metadata first so standard tags take precedence if keys collide
            if (EnableSensitiveData && response.AdditionalProperties is { } metadata)
            {
                foreach (var prop in metadata)
                {
                    _ = activity.AddTag(prop.Key, prop.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(response.ResponseId))
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Id, response.ResponseId);
            }

            if (!string.IsNullOrWhiteSpace(response.ConversationId))
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Conversation.Id, response.ConversationId);
            }

            if (!string.IsNullOrWhiteSpace(response.Status))
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.FinishReasons, $"[\"{response.Status}\"]");
            }

            if (response.Usage is { } responseUsage)
            {
                if (responseUsage.InputTokenCount is long inputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTokens, (int)inputTokens);
                }

                if (responseUsage.OutputTokenCount is long outputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputTokens, (int)outputTokens);
                }

                if (responseUsage.CachedInputTokenCount is long cachedInputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.CacheReadInputTokens, (int)cachedInputTokens);
                }

                if (responseUsage.InputAudioTokenCount is long inputAudioTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputAudioTokens, (int)inputAudioTokens);
                }

                if (responseUsage.InputTextTokenCount is long inputTextTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTextTokens, (int)inputTextTokens);
                }

                if (responseUsage.OutputAudioTokenCount is long outputAudioTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputAudioTokens, (int)outputAudioTokens);
                }

                if (responseUsage.OutputTextTokenCount is long outputTextTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputTextTokens, (int)outputTextTokens);
                }
            }

            // Log error content if available
            if (response.Error is { } responseError)
            {
                _ = activity.AddTag(OpenTelemetryConsts.Error.Type, responseError.ErrorCode ?? "RealtimeError");
                _ = activity.SetStatus(ActivityStatusCode.Error, responseError.Message);
            }
        }
    }

    private void AddMetricTags(ref TagList tags, string? requestModelId, string? responseModelId)
    {
        tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.ChatName);

        if (requestModelId is not null)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModelId);
        }

        tags.Add(OpenTelemetryConsts.GenAI.Provider.Name, _providerName);

        if (_serverAddress is string endpointAddress)
        {
            tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
            tags.Add(OpenTelemetryConsts.Server.Port, _serverPort);
        }

        if (responseModelId is string responseModel)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModel);
        }
    }

    #region OTel Serialization Types

    private sealed class RealtimeOtelGenericPart
    {
        public string Type { get; set; } = "text";
        public object? Content { get; set; }
    }

    private sealed class RealtimeOtelBlobPart
    {
        public string Type { get; set; } = "blob";
        public string? Content { get; set; } // base64-encoded binary data
        public string? MimeType { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class RealtimeOtelUriPart
    {
        public string Type { get; set; } = "uri";
        public string? Uri { get; set; }
        public string? MimeType { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class RealtimeOtelFilePart
    {
        public string Type { get; set; } = "file";
        public string? FileId { get; set; }
        public string? MimeType { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class RealtimeOtelFunction
    {
        public string Type { get; set; } = "function";
        public string? Name { get; set; }
        public string? Description { get; set; }
        public JsonElement? Parameters { get; set; }
    }

    private sealed class RealtimeOtelMessage
    {
        public string? Role { get; set; }
        public List<object> Parts { get; set; } = [];
    }

    private sealed class RealtimeOtelToolCallPart
    {
        public string Type { get; set; } = "tool_call";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public IDictionary<string, object?>? Arguments { get; set; }
    }

    private sealed class RealtimeOtelToolCallResponsePart
    {
        public string Type { get; set; } = "tool_call_response";
        public string? Id { get; set; }
        public object? Response { get; set; }
    }

    private sealed class RealtimeOtelServerToolCallPart<T>
        where T : class
    {
        public string Type { get; set; } = "server_tool_call";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public T? ServerToolCall { get; set; }
    }

    private sealed class RealtimeOtelServerToolCallResponsePart<T>
        where T : class
    {
        public string Type { get; set; } = "server_tool_call_response";
        public string? Id { get; set; }
        public T? ServerToolCallResponse { get; set; }
    }

    private sealed class RealtimeOtelMcpToolCall
    {
        public string Type { get; set; } = "mcp";
        public string? ServerName { get; set; }
        public IReadOnlyDictionary<string, object?>? Arguments { get; set; }
    }

    private sealed class RealtimeOtelMcpToolCallResponse
    {
        public string Type { get; set; } = "mcp";
        public object? Output { get; set; }
    }

    private sealed class RealtimeOtelMcpApprovalRequest
    {
        public string Type { get; set; } = "mcp_approval_request";
        public string? ServerName { get; set; }
        public IReadOnlyDictionary<string, object?>? Arguments { get; set; }
    }

    private sealed class RealtimeOtelMcpApprovalResponse
    {
        public string Type { get; set; } = "mcp_approval_response";
        public bool Approved { get; set; }
    }

    #endregion

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(IList<object>))]
    [JsonSerializable(typeof(RealtimeOtelGenericPart))]
    [JsonSerializable(typeof(RealtimeOtelBlobPart))]
    [JsonSerializable(typeof(RealtimeOtelUriPart))]
    [JsonSerializable(typeof(RealtimeOtelFilePart))]
    [JsonSerializable(typeof(IEnumerable<RealtimeOtelFunction>))]
    [JsonSerializable(typeof(IEnumerable<RealtimeOtelMessage>))]
    [JsonSerializable(typeof(RealtimeOtelMessage))]
    [JsonSerializable(typeof(RealtimeOtelToolCallPart))]
    [JsonSerializable(typeof(RealtimeOtelToolCallResponsePart))]
    [JsonSerializable(typeof(RealtimeOtelServerToolCallPart<RealtimeOtelMcpToolCall>))]
    [JsonSerializable(typeof(RealtimeOtelServerToolCallResponsePart<RealtimeOtelMcpToolCallResponse>))]
    [JsonSerializable(typeof(RealtimeOtelServerToolCallPart<RealtimeOtelMcpApprovalRequest>))]
    [JsonSerializable(typeof(RealtimeOtelServerToolCallResponsePart<RealtimeOtelMcpApprovalResponse>))]
    private sealed partial class RealtimeOtelContext : JsonSerializerContext;
}
