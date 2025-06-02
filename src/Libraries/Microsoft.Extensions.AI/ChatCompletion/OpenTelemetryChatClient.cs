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
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.34, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
public sealed partial class OpenTelemetryChatClient : DelegatingChatClient
{
    private const LogLevel EventLogLevel = LogLevel.Information;

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly ILogger _logger;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _defaultModelId;
    private readonly string? _system;
    private readonly string? _serverAddress;
    private readonly int _serverPort;

    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    public OpenTelemetryChatClient(IChatClient innerClient, ILogger? logger = null, string? sourceName = null)
        : base(innerClient)
    {
        Debug.Assert(innerClient is not null, "Should have been validated by the base ctor");

        _logger = logger ?? NullLogger.Instance;

        if (innerClient!.GetService<ChatClientMetadata>() is ChatClientMetadata metadata)
        {
            _defaultModelId = metadata.DefaultModelId;
            _system = metadata.ProviderName;
            _serverAddress = metadata.ProviderUri?.GetLeftPart(UriPartial.Path);
            _serverPort = metadata.ProviderUri?.Port ?? 0;
        }

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries }
#endif
            );

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
#endif
            );

        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when formatting chat data into telemetry strings.</summary>
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
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// By default, telemetry includes metadata, such as token counts, but not raw inputs
    /// and outputs, such as message content, function call arguments, and function call results.
    /// </remarks>
    public bool EnableSensitiveData { get; set; }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource) ? _activitySource :
        base.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        LogChatMessages(messages);

        ChatResponse? response = null;
        Exception? error = null;
        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, requestModelId, response, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        LogChatMessages(messages);

        IAsyncEnumerable<ChatResponseUpdate> updates;
        try
        {
            updates = base.GetStreamingResponseAsync(messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            TraceResponse(activity, requestModelId, response: null, ex, stopwatch);
            throw;
        }

        var responseEnumerator = updates.GetAsyncEnumerator(cancellationToken);
        List<ChatResponseUpdate> trackedUpdates = [];
        Exception? error = null;
        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await responseEnumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = responseEnumerator.Current;
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }

                trackedUpdates.Add(update);
                yield return update;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }
        }
        finally
        {
            TraceResponse(activity, requestModelId, trackedUpdates.ToChatResponse(), error, stopwatch);

            await responseEnumerator.DisposeAsync();
        }
    }

    /// <summary>Creates an activity for a chat request, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(ChatOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.ModelId ?? _defaultModelId;

            activity = _activitySource.StartActivity(
                string.IsNullOrWhiteSpace(modelId) ? OpenTelemetryConsts.GenAI.Chat : $"{OpenTelemetryConsts.GenAI.Chat} {modelId}",
                ActivityKind.Client);

            if (activity is not null)
            {
                _ = activity
                    .AddTag(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Chat)
                    .AddTag(OpenTelemetryConsts.GenAI.Request.Model, modelId)
                    .AddTag(OpenTelemetryConsts.GenAI.SystemName, _system);

                if (_serverAddress is not null)
                {
                    _ = activity
                        .AddTag(OpenTelemetryConsts.Server.Address, _serverAddress)
                        .AddTag(OpenTelemetryConsts.Server.Port, _serverPort);
                }

                if (options is not null)
                {
                    if (options.ConversationId is string conversationId)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Conversation.Id, conversationId);
                    }

                    if (options.FrequencyPenalty is float frequencyPenalty)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.FrequencyPenalty, frequencyPenalty);
                    }

                    if (options.MaxOutputTokens is int maxTokens)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.MaxTokens, maxTokens);
                    }

                    if (options.PresencePenalty is float presencePenalty)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.PresencePenalty, presencePenalty);
                    }

                    if (options.Seed is long seed)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.Seed, seed);
                    }

                    if (options.StopSequences is IList<string> stopSequences)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.StopSequences, $"[{string.Join(", ", stopSequences.Select(s => $"\"{s}\""))}]");
                    }

                    if (options.Temperature is float temperature)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.Temperature, temperature);
                    }

                    if (options.TopK is int topK)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.TopK, topK);
                    }

                    if (options.TopP is float top_p)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.TopP, top_p);
                    }

                    if (options.ResponseFormat is not null)
                    {
                        switch (options.ResponseFormat)
                        {
                            case ChatResponseFormatText:
                                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Type, "text");
                                break;
                            case ChatResponseFormatJson:
                                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Type, "json");
                                break;
                        }
                    }

                    if (_system is not null)
                    {
                        // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data
                        if (EnableSensitiveData && options.AdditionalProperties is { } props)
                        {
                            // Log all additional request options as per-provider tags. This is non-normative, but it covers cases where
                            // there's a per-provider specification in a best-effort manner (e.g. gen_ai.openai.request.service_tier),
                            // and more generally cases where there's additional useful information to be logged.
                            foreach (KeyValuePair<string, object?> prop in props)
                            {
                                _ = activity.AddTag(
                                    OpenTelemetryConsts.GenAI.Request.PerProvider(_system, JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Key)),
                                    prop.Value);
                            }
                        }
                    }
                }
            }
        }

        return activity;
    }

    /// <summary>Adds chat response information to the activity.</summary>
    private void TraceResponse(
        Activity? activity,
        string? requestModelId,
        ChatResponse? response,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;

            AddMetricTags(ref tags, requestModelId, response);
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
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "input");
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)inputTokens);
            }

            if (usage.OutputTokenCount is long outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "output");
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)outputTokens);
            }
        }

        if (error is not null)
        {
            _ = activity?
                .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }

        if (response is not null)
        {
            LogChatResponse(response);

            if (activity is not null)
            {
                if (response.FinishReason is ChatFinishReason finishReason)
                {
#pragma warning disable CA1308 // Normalize strings to uppercase
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.FinishReasons, $"[\"{finishReason.Value.ToLowerInvariant()}\"]");
#pragma warning restore CA1308
                }

                if (!string.IsNullOrWhiteSpace(response.ResponseId))
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Id, response.ResponseId);
                }

                if (response.ModelId is not null)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Model, response.ModelId);
                }

                if (response.Usage?.InputTokenCount is long inputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.InputTokens, (int)inputTokens);
                }

                if (response.Usage?.OutputTokenCount is long outputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.OutputTokens, (int)outputTokens);
                }

                if (_system is not null)
                {
                    // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data
                    if (EnableSensitiveData && response.AdditionalProperties is { } props)
                    {
                        // Log all additional response properties as per-provider tags. This is non-normative, but it covers cases where
                        // there's a per-provider specification in a best-effort manner (e.g. gen_ai.openai.response.system_fingerprint),
                        // and more generally cases where there's additional useful information to be logged.
                        foreach (KeyValuePair<string, object?> prop in props)
                        {
                            _ = activity.AddTag(
                                OpenTelemetryConsts.GenAI.Response.PerProvider(_system, JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Key)),
                                prop.Value);
                        }
                    }
                }
            }
        }

        void AddMetricTags(ref TagList tags, string? requestModelId, ChatResponse? response)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Chat);

            if (requestModelId is not null)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModelId);
            }

            tags.Add(OpenTelemetryConsts.GenAI.SystemName, _system);

            if (_serverAddress is string endpointAddress)
            {
                tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
                tags.Add(OpenTelemetryConsts.Server.Port, _serverPort);
            }

            if (response?.ModelId is string responseModel)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModel);
            }
        }
    }

    private void LogChatMessages(IEnumerable<ChatMessage> messages)
    {
        if (!_logger.IsEnabled(EventLogLevel))
        {
            return;
        }

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                Log(new(1, OpenTelemetryConsts.GenAI.Assistant.Message),
                    JsonSerializer.Serialize(CreateAssistantEvent(message.Contents), OtelContext.Default.AssistantEvent));
            }
            else if (message.Role == ChatRole.Tool)
            {
                foreach (FunctionResultContent frc in message.Contents.OfType<FunctionResultContent>())
                {
                    Log(new(1, OpenTelemetryConsts.GenAI.Tool.Message),
                        JsonSerializer.Serialize(new()
                        {
                            Id = frc.CallId,
                            Content = EnableSensitiveData && frc.Result is object result ?
                                JsonSerializer.SerializeToNode(result, _jsonSerializerOptions.GetTypeInfo(result.GetType())) :
                                null,
                        }, OtelContext.Default.ToolEvent));
                }
            }
            else
            {
                Log(new(1, message.Role == ChatRole.System ? OpenTelemetryConsts.GenAI.System.Message : OpenTelemetryConsts.GenAI.User.Message),
                    JsonSerializer.Serialize(new()
                    {
                        Role = message.Role != ChatRole.System && message.Role != ChatRole.User && !string.IsNullOrWhiteSpace(message.Role.Value) ? message.Role.Value : null,
                        Content = GetMessageContent(message.Contents),
                    }, OtelContext.Default.SystemOrUserEvent));
            }
        }
    }

    private void LogChatResponse(ChatResponse response)
    {
        if (!_logger.IsEnabled(EventLogLevel))
        {
            return;
        }

        EventId id = new(1, OpenTelemetryConsts.GenAI.Choice);
        Log(id, JsonSerializer.Serialize(new()
        {
            FinishReason = response.FinishReason?.Value ?? "error",
            Index = 0,
            Message = CreateAssistantEvent(response.Messages is { Count: 1 } ? response.Messages[0].Contents : response.Messages.SelectMany(m => m.Contents)),
        }, OtelContext.Default.ChoiceEvent));
    }

    private void Log(EventId id, [StringSyntax(StringSyntaxAttribute.Json)] string eventBodyJson)
    {
        // This is not the idiomatic way to log, but it's necessary for now in order to structure
        // the data in a way that the OpenTelemetry collector can work with it. The event body
        // can be very large and should not be logged as an attribute.

        KeyValuePair<string, object?>[] tags =
        [
            new(OpenTelemetryConsts.Event.Name, id.Name),
            new(OpenTelemetryConsts.GenAI.SystemName, _system),
        ];

        _logger.Log(EventLogLevel, id, tags, null, (_, __) => eventBodyJson);
    }

    private AssistantEvent CreateAssistantEvent(IEnumerable<AIContent> contents)
    {
        var toolCalls = contents.OfType<FunctionCallContent>().Select(fc => new ToolCall
        {
            Id = fc.CallId,
            Function = new()
            {
                Name = fc.Name,
                Arguments = EnableSensitiveData ?
                    JsonSerializer.SerializeToNode(fc.Arguments, _jsonSerializerOptions.GetTypeInfo(typeof(IDictionary<string, object?>))) :
                    null,
            },
        }).ToArray();

        return new()
        {
            Content = GetMessageContent(contents),
            ToolCalls = toolCalls.Length > 0 ? toolCalls : null,
        };
    }

    private string? GetMessageContent(IEnumerable<AIContent> contents)
    {
        if (EnableSensitiveData)
        {
            string content = string.Concat(contents.OfType<TextContent>());
            if (content.Length > 0)
            {
                return content;
            }
        }

        return null;
    }

    private sealed class SystemOrUserEvent
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private sealed class AssistantEvent
    {
        public string? Content { get; set; }
        public ToolCall[]? ToolCalls { get; set; }
    }

    private sealed class ToolEvent
    {
        public string? Id { get; set; }
        public JsonNode? Content { get; set; }
    }

    private sealed class ChoiceEvent
    {
        public string? FinishReason { get; set; }
        public int Index { get; set; }
        public AssistantEvent? Message { get; set; }
    }

    private sealed class ToolCall
    {
        public string? Id { get; set; }
        public string? Type { get; set; } = "function";
        public ToolCallFunction? Function { get; set; }
    }

    private sealed class ToolCallFunction
    {
        public string? Name { get; set; }
        public JsonNode? Arguments { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(SystemOrUserEvent))]
    [JsonSerializable(typeof(AssistantEvent))]
    [JsonSerializable(typeof(ToolEvent))]
    [JsonSerializable(typeof(ChoiceEvent))]
    [JsonSerializable(typeof(object))]
    private sealed partial class OtelContext : JsonSerializerContext;
}
