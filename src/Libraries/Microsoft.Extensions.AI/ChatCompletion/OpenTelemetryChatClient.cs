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

#pragma warning disable S1135 // Track uses of "TODO" tags
#pragma warning disable S3358 // Ternary operators should not be nested

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// The draft specification this follows is available at https://opentelemetry.io/docs/specs/semconv/gen-ai/.
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

    private readonly string? _modelId;
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

        ChatClientMetadata metadata = innerClient!.Metadata;
        _modelId = metadata.ModelId;
        _system = metadata.ProviderName;
        _serverAddress = metadata.ProviderUri?.GetLeftPart(UriPartial.Path);
        _serverPort = metadata.ProviderUri?.Port ?? 0;

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries });

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries });

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
    /// <remarks>
    /// The value is <see langword="false"/> by default, meaning that telemetry will include metadata such as token counts but not raw inputs
    /// and outputs such as message content, function call arguments, and function call results.
    /// </remarks>
    public bool EnableSensitiveData { get; set; }

    /// <inheritdoc/>
    public override async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _modelId;

        LogChatMessages(chatMessages);

        ChatCompletion? completion = null;
        Exception? error = null;
        try
        {
            completion = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
            return completion;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceCompletion(activity, requestModelId, completion, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _modelId;

        LogChatMessages(chatMessages);

        IAsyncEnumerable<StreamingChatCompletionUpdate> updates;
        try
        {
            updates = base.CompleteStreamingAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            TraceCompletion(activity, requestModelId, completion: null, ex, stopwatch);
            throw;
        }

        var responseEnumerator = updates.ConfigureAwait(false).GetAsyncEnumerator();
        List<StreamingChatCompletionUpdate> trackedUpdates = [];
        Exception? error = null;
        try
        {
            while (true)
            {
                StreamingChatCompletionUpdate update;
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
            }
        }
        finally
        {
            TraceCompletion(activity, requestModelId, ComposeStreamingUpdatesIntoChatCompletion(trackedUpdates), error, stopwatch);

            await responseEnumerator.DisposeAsync();
        }
    }

    /// <summary>Creates a <see cref="ChatCompletion"/> from a collection of <see cref="StreamingChatCompletionUpdate"/> instances.</summary>
    /// <remarks>
    /// This only propagates information that's later used by the telemetry. If additional information from the <see cref="ChatCompletion"/>
    /// is needed, this implementation should be updated to include it.
    /// </remarks>
    private static ChatCompletion ComposeStreamingUpdatesIntoChatCompletion(
        List<StreamingChatCompletionUpdate> updates)
    {
        // Group updates by choice index.
        Dictionary<int, List<StreamingChatCompletionUpdate>> choices = [];
        foreach (var update in updates)
        {
            if (!choices.TryGetValue(update.ChoiceIndex, out var choiceContents))
            {
                choices[update.ChoiceIndex] = choiceContents = [];
            }

            choiceContents.Add(update);
        }

        // Add a ChatMessage for each choice.
        string? id = null;
        ChatFinishReason? finishReason = null;
        string? modelId = null;
        List<ChatMessage> messages = new(choices.Count);
        foreach (var choice in choices.OrderBy(c => c.Key))
        {
            ChatRole? role = null;
            List<AIContent> items = [];
            foreach (var update in choice.Value)
            {
                id ??= update.CompletionId;
                finishReason ??= update.FinishReason;
                role ??= update.Role;
                items.AddRange(update.Contents);
                modelId ??= update.Contents.FirstOrDefault(c => c.ModelId is not null)?.ModelId;
            }

            messages.Add(new ChatMessage(role ?? ChatRole.Assistant, items));
        }

        return new(messages)
        {
            CompletionId = id,
            FinishReason = finishReason,
            ModelId = modelId,
            Usage = updates.SelectMany(c => c.Contents).OfType<UsageContent>().LastOrDefault()?.Details,
        };
    }

    /// <summary>Creates an activity for a chat completion request, or returns null if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(ChatOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.ModelId ?? _modelId;

            activity = _activitySource.StartActivity(
                $"{OpenTelemetryConsts.GenAI.Chat} {modelId}",
                ActivityKind.Client,
                default(ActivityContext),
                [
                    new(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Chat),
                    new(OpenTelemetryConsts.GenAI.Request.Model, modelId),
                    new(OpenTelemetryConsts.GenAI.SystemName, _system),
                ]);

            if (activity is not null)
            {
                if (_serverAddress is not null)
                {
                    _ = activity
                        .SetTag(OpenTelemetryConsts.Server.Address, _serverAddress)
                        .SetTag(OpenTelemetryConsts.Server.Port, _serverPort);
                }

                if (options is not null)
                {
                    if (options.FrequencyPenalty is float frequencyPenalty)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.FrequencyPenalty, frequencyPenalty);
                    }

                    if (options.MaxOutputTokens is int maxTokens)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.MaxTokens, maxTokens);
                    }

                    if (options.PresencePenalty is float presencePenalty)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.PresencePenalty, presencePenalty);
                    }

                    if (options.StopSequences is IList<string> stopSequences)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.StopSequences, $"[{string.Join(", ", stopSequences.Select(s => $"\"{s}\""))}]");
                    }

                    if (options.Temperature is float temperature)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.Temperature, temperature);
                    }

                    if (options.TopK is int topK)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.TopK, topK);
                    }

                    if (options.TopP is float top_p)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.TopP, top_p);
                    }

                    if (_system is not null)
                    {
                        if (options.ResponseFormat is not null)
                        {
                            string responseFormat = options.ResponseFormat switch
                            {
                                ChatResponseFormatText => "text",
                                ChatResponseFormatJson { Schema: null } => "json_schema",
                                ChatResponseFormatJson => "json_object",
                                _ => "_OTHER",
                            };
                            _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.PerProvider(_system, "response_format"), responseFormat);
                        }

                        if (options.AdditionalProperties?.TryGetValue("seed", out long seed) is true)
                        {
                            _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.PerProvider(_system, "seed"), seed);
                        }
                    }
                }
            }
        }

        return activity;
    }

    /// <summary>Adds chat completion information to the activity.</summary>
    private void TraceCompletion(
        Activity? activity,
        string? requestModelId,
        ChatCompletion? completion,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;

            AddMetricTags(ref tags, requestModelId, completion);
            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && completion?.Usage is { } usage)
        {
            if (usage.InputTokenCount is int inputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "input");
                AddMetricTags(ref tags, requestModelId, completion);
                _tokenUsageHistogram.Record(inputTokens);
            }

            if (usage.OutputTokenCount is int outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "output");
                AddMetricTags(ref tags, requestModelId, completion);
                _tokenUsageHistogram.Record(outputTokens);
            }
        }

        if (error is not null)
        {
            _ = activity?
                .SetTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }

        if (completion is not null)
        {
            LogChatCompletion(completion);

            if (activity is not null)
            {
                if (completion.FinishReason is ChatFinishReason finishReason)
                {
#pragma warning disable CA1308 // Normalize strings to uppercase
                    _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.FinishReasons, $"[\"{finishReason.Value.ToLowerInvariant()}\"]");
#pragma warning restore CA1308
                }

                if (!string.IsNullOrWhiteSpace(completion.CompletionId))
                {
                    _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.Id, completion.CompletionId);
                }

                if (completion.ModelId is not null)
                {
                    _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.Model, completion.ModelId);
                }

                if (completion.Usage?.InputTokenCount is int inputTokens)
                {
                    _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.InputTokens, inputTokens);
                }

                if (completion.Usage?.OutputTokenCount is int outputTokens)
                {
                    _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.OutputTokens, outputTokens);
                }
            }
        }

        void AddMetricTags(ref TagList tags, string? requestModelId, ChatCompletion? completions)
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

            if (completions?.ModelId is string responseModel)
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
                    JsonSerializer.Serialize(CreateAssistantEvent(message), OtelContext.Default.AssistantEvent));
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
                        Content = GetMessageContent(message),
                    }, OtelContext.Default.SystemOrUserEvent));
            }
        }
    }

    private void LogChatCompletion(ChatCompletion completion)
    {
        if (!_logger.IsEnabled(EventLogLevel))
        {
            return;
        }

        EventId id = new(1, OpenTelemetryConsts.GenAI.Choice);
        int choiceCount = completion.Choices.Count;
        for (int choiceIndex = 0; choiceIndex < choiceCount; choiceIndex++)
        {
            Log(id, JsonSerializer.Serialize(new()
            {
                FinishReason = completion.FinishReason?.Value ?? "error",
                Index = choiceIndex,
                Message = CreateAssistantEvent(completion.Choices[choiceIndex]),
            }, OtelContext.Default.ChoiceEvent));
        }
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

    private AssistantEvent CreateAssistantEvent(ChatMessage message)
    {
        var toolCalls = message.Contents.OfType<FunctionCallContent>().Select(fc => new ToolCall
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
            Content = GetMessageContent(message),
            ToolCalls = toolCalls.Length > 0 ? toolCalls : null,
        };
    }

    private string? GetMessageContent(ChatMessage message)
    {
        if (EnableSensitiveData)
        {
            // TODO: Include other content types once the genai specification details what's expected.
            string content = string.Concat(message.Contents.OfType<TextContent>().Select(c => c.Text));
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
