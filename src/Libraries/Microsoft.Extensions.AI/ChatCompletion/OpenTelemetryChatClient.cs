// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.38, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
public sealed partial class OpenTelemetryChatClient : DelegatingChatClient
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

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting any logging data from the client.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
#pragma warning disable IDE0060 // Remove unused parameter; it exists for backwards compatibility and future use
    public OpenTelemetryChatClient(IChatClient innerClient, ILogger? logger = null, string? sourceName = null)
#pragma warning restore IDE0060
        : base(innerClient)
    {
        Debug.Assert(innerClient is not null, "Should have been validated by the base ctor");

        if (innerClient!.GetService<ChatClientMetadata>() is ChatClientMetadata metadata)
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
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        AddInputMessagesTags(messages, options, activity);

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

        AddInputMessagesTags(messages, options, activity);

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

    internal static string SerializeChatMessages(
        IEnumerable<ChatMessage> messages, ChatFinishReason? chatFinishReason = null, JsonSerializerOptions? customContentSerializerOptions = null)
    {
        List<object> output = [];

        string? finishReason =
            chatFinishReason?.Value is null ? null :
            chatFinishReason == ChatFinishReason.Length ? "length" :
            chatFinishReason == ChatFinishReason.ContentFilter ? "content_filter" :
            chatFinishReason == ChatFinishReason.ToolCalls ? "tool_call" :
            "stop";

        foreach (ChatMessage message in messages)
        {
            OtelMessage m = new()
            {
                FinishReason = finishReason,
                Role =
                    message.Role == ChatRole.Assistant ? "assistant" :
                    message.Role == ChatRole.Tool ? "tool" :
                    message.Role == ChatRole.System || message.Role == new ChatRole("developer") ? "system" :
                    "user",
                Name = message.AuthorName,
            };

            foreach (AIContent content in message.Contents)
            {
                switch (content)
                {
                    // These are all specified in the convention:

                    case TextContent tc when !string.IsNullOrWhiteSpace(tc.Text):
                        m.Parts.Add(new OtelGenericPart { Content = tc.Text });
                        break;

                    case TextReasoningContent trc when !string.IsNullOrWhiteSpace(trc.Text):
                        m.Parts.Add(new OtelGenericPart { Type = "reasoning", Content = trc.Text });
                        break;

                    case FunctionCallContent fcc:
                        m.Parts.Add(new OtelToolCallRequestPart
                        {
                            Id = fcc.CallId,
                            Name = fcc.Name,
                            Arguments = fcc.Arguments,
                        });
                        break;

                    case FunctionResultContent frc:
                        m.Parts.Add(new OtelToolCallResponsePart
                        {
                            Id = frc.CallId,
                            Response = frc.Result,
                        });
                        break;

                    case DataContent dc:
                        m.Parts.Add(new OtelBlobPart
                        {
                            Content = dc.Base64Data.ToString(),
                            MimeType = dc.MediaType,
                            Modality = DeriveModalityFromMediaType(dc.MediaType),
                        });
                        break;

                    case UriContent uc:
                        m.Parts.Add(new OtelUriPart
                        {
                            Uri = uc.Uri.AbsoluteUri,
                            MimeType = uc.MediaType,
                            Modality = DeriveModalityFromMediaType(uc.MediaType),
                        });
                        break;

                    case HostedFileContent fc:
                        m.Parts.Add(new OtelFilePart
                        {
                            FileId = fc.FileId,
                            MimeType = fc.MediaType,
                            Modality = DeriveModalityFromMediaType(fc.MediaType),
                        });
                        break;

                    // These are non-standard and are using the "generic" non-text part that provides an extensibility mechanism:

                    case HostedVectorStoreContent vsc:
                        m.Parts.Add(new OtelGenericPart { Type = "vector_store", Content = vsc.VectorStoreId });
                        break;

                    case ErrorContent ec:
                        m.Parts.Add(new OtelGenericPart { Type = "error", Content = ec.Message });
                        break;

                    default:
                        JsonElement element = _emptyObject;
                        try
                        {
                            JsonTypeInfo? unknownContentTypeInfo =
                                customContentSerializerOptions?.TryGetTypeInfo(content.GetType(), out JsonTypeInfo? ctsi) is true ? ctsi :
                                _defaultOptions.TryGetTypeInfo(content.GetType(), out JsonTypeInfo? dtsi) ? dtsi :
                                null;

                            if (unknownContentTypeInfo is not null)
                            {
                                element = JsonSerializer.SerializeToElement(content, unknownContentTypeInfo);
                            }
                        }
                        catch
                        {
                            // Ignore the contents of any parts that can't be serialized.
                        }

                        m.Parts.Add(new OtelGenericPart
                        {
                            Type = content.GetType().FullName!,
                            Content = element,
                        });
                        break;
                }
            }

            output.Add(m);
        }

        return JsonSerializer.Serialize(output, _defaultOptions.GetTypeInfo(typeof(IList<object>)));
    }

    private static string? DeriveModalityFromMediaType(string? mediaType)
    {
        if (mediaType is not null)
        {
            int pos = mediaType.IndexOf('/');
            if (pos >= 0)
            {
                ReadOnlySpan<char> topLevel = mediaType.AsSpan(0, pos);
                return
                    topLevel.Equals("image", StringComparison.OrdinalIgnoreCase) ? "image" :
                    topLevel.Equals("audio", StringComparison.OrdinalIgnoreCase) ? "audio" :
                    topLevel.Equals("video", StringComparison.OrdinalIgnoreCase) ? "video" :
                    null;
            }
        }

        return null;
    }

    /// <summary>Creates an activity for a chat request, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(ChatOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.ModelId ?? _defaultModelId;

            activity = _activitySource.StartActivity(
                string.IsNullOrWhiteSpace(modelId) ? OpenTelemetryConsts.GenAI.ChatName : $"{OpenTelemetryConsts.GenAI.ChatName} {modelId}",
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

                    if (options.StopSequences is IList<string> { Count: > 0 } stopSequences)
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
                                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Type, OpenTelemetryConsts.TypeText);
                                break;
                            case ChatResponseFormatJson:
                                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Type, OpenTelemetryConsts.TypeJson);
                                break;
                        }
                    }

                    if (EnableSensitiveData)
                    {
                        if (options.Tools is { Count: > 0 })
                        {
                            _ = activity.AddTag(
                                OpenTelemetryConsts.GenAI.Tool.Definitions,
                                JsonSerializer.Serialize(options.Tools.Select(t => t switch
                                {
                                    _ when t.GetService<AIFunctionDeclaration>() is { } af => new OtelFunction
                                    {
                                        Name = af.Name,
                                        Description = af.Description,
                                        Parameters = af.JsonSchema,
                                    },
                                    _ => new OtelFunction { Type = t.Name },
                                }), OtelContext.Default.IEnumerableOtelFunction));
                        }

                        // Log all additional request options as raw values on the span.
                        // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data.
                        if (options.AdditionalProperties is { } props)
                        {
                            foreach (KeyValuePair<string, object?> prop in props)
                            {
                                _ = activity.AddTag(prop.Key, prop.Value);
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
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeInput);
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)inputTokens, tags);
            }

            if (usage.OutputTokenCount is long outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeOutput);
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)outputTokens, tags);
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
            AddOutputMessagesTags(response, activity);

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
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTokens, (int)inputTokens);
                }

                if (response.Usage?.OutputTokenCount is long outputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputTokens, (int)outputTokens);
                }

                // Log all additional response properties as raw values on the span.
                // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data.
                if (EnableSensitiveData && response.AdditionalProperties is { } props)
                {
                    foreach (KeyValuePair<string, object?> prop in props)
                    {
                        _ = activity.AddTag(prop.Key, prop.Value);
                    }
                }
            }
        }

        void AddMetricTags(ref TagList tags, string? requestModelId, ChatResponse? response)
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

            if (response?.ModelId is string responseModel)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModel);
            }
        }
    }

    private void AddInputMessagesTags(IEnumerable<ChatMessage> messages, ChatOptions? options, Activity? activity)
    {
        if (EnableSensitiveData && activity is { IsAllDataRequested: true })
        {
            if (!string.IsNullOrWhiteSpace(options?.Instructions))
            {
                _ = activity.AddTag(
                    OpenTelemetryConsts.GenAI.SystemInstructions,
                    JsonSerializer.Serialize(new object[1] { new OtelGenericPart { Content = options!.Instructions } }, _defaultOptions.GetTypeInfo(typeof(IList<object>))));
            }

            _ = activity.AddTag(
                OpenTelemetryConsts.GenAI.Input.Messages,
                SerializeChatMessages(messages, customContentSerializerOptions: _jsonSerializerOptions));
        }
    }

    private void AddOutputMessagesTags(ChatResponse response, Activity? activity)
    {
        if (EnableSensitiveData && activity is { IsAllDataRequested: true })
        {
            _ = activity.AddTag(
                OpenTelemetryConsts.GenAI.Output.Messages,
                SerializeChatMessages(response.Messages, response.FinishReason, customContentSerializerOptions: _jsonSerializerOptions));
        }
    }

    private sealed class OtelMessage
    {
        public string? Role { get; set; }
        public string? Name { get; set; }
        public List<object> Parts { get; set; } = [];
        public string? FinishReason { get; set; }
    }

    private sealed class OtelGenericPart
    {
        public string Type { get; set; } = "text";
        public object? Content { get; set; } // should be a string when Type == "text"
    }

    private sealed class OtelBlobPart
    {
        public string Type { get; set; } = "blob";
        public string? Content { get; set; } // base64-encoded binary data
        public string? MimeType { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class OtelUriPart
    {
        public string Type { get; set; } = "uri";
        public string? Uri { get; set; }
        public string? MimeType { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class OtelFilePart
    {
        public string Type { get; set; } = "file";
        public string? FileId { get; set; }
        public string? MimeType { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class OtelToolCallRequestPart
    {
        public string Type { get; set; } = "tool_call";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public IDictionary<string, object?>? Arguments { get; set; }
    }

    private sealed class OtelToolCallResponsePart
    {
        public string Type { get; set; } = "tool_call_response";
        public string? Id { get; set; }
        public object? Response { get; set; }
    }

    private sealed class OtelFunction
    {
        public string Type { get; set; } = "function";
        public string? Name { get; set; }
        public string? Description { get; set; }
        public JsonElement? Parameters { get; set; }
    }

    private static readonly JsonSerializerOptions _defaultOptions = CreateDefaultOptions();
    private static readonly JsonElement _emptyObject = JsonSerializer.SerializeToElement(new object(), _defaultOptions.GetTypeInfo(typeof(object)));

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        JsonSerializerOptions options = new(OtelContext.Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        options.TypeInfoResolverChain.Add(AIJsonUtilities.DefaultOptions.TypeInfoResolver!);
        options.MakeReadOnly();

        return options;
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(IList<object>))]
    [JsonSerializable(typeof(OtelMessage))]
    [JsonSerializable(typeof(OtelGenericPart))]
    [JsonSerializable(typeof(OtelBlobPart))]
    [JsonSerializable(typeof(OtelUriPart))]
    [JsonSerializable(typeof(OtelFilePart))]
    [JsonSerializable(typeof(OtelToolCallRequestPart))]
    [JsonSerializable(typeof(OtelToolCallResponsePart))]
    [JsonSerializable(typeof(IEnumerable<OtelFunction>))]
    private sealed partial class OtelContext : JsonSerializerContext;
}
