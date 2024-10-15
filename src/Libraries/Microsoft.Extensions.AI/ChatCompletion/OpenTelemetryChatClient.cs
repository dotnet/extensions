// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// The draft specification this follows is available at https://opentelemetry.io/docs/specs/semconv/gen-ai/.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
public sealed class OpenTelemetryChatClient : DelegatingChatClient
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _modelId;
    private readonly string? _modelProvider;
    private readonly string? _endpointAddress;
    private readonly int _endpointPort;

    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    public OpenTelemetryChatClient(IChatClient innerClient, string? sourceName = null)
        : base(innerClient)
    {
        Debug.Assert(innerClient is not null, "Should have been validated by the base ctor");

        ChatClientMetadata metadata = innerClient!.Metadata;
        _modelId = metadata.ModelId;
        _modelProvider = metadata.ProviderName;
        _endpointAddress = metadata.ProviderUri?.GetLeftPart(UriPartial.Path);
        _endpointPort = metadata.ProviderUri?.Port ?? 0;

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
    /// Gets or sets a value indicating whether potentially sensitive information (e.g. prompts) should be included in telemetry.
    /// </summary>
    /// <remarks>
    /// The value is <see langword="false"/> by default, meaning that telemetry will include metadata such as token counts but not the raw text of prompts or completions.
    /// </remarks>
    public bool EnableSensitiveData { get; set; }

    /// <inheritdoc/>
    public override async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = StartActivity(chatMessages, options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _modelId;

        ChatCompletion? response = null;
        Exception? error = null;
        try
        {
            response = await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            SetCompletionResponse(activity, requestModelId, response, error, stopwatch);
        }

        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = StartActivity(chatMessages, options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _modelId;

        IAsyncEnumerable<StreamingChatCompletionUpdate> response;
        try
        {
            response = base.CompleteStreamingAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            SetCompletionResponse(activity, requestModelId, null, ex, stopwatch);
            throw;
        }

        var responseEnumerator = response.ConfigureAwait(false).GetAsyncEnumerator();
        List<StreamingChatCompletionUpdate>? streamedContents = activity is not null ? [] : null;
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
                    SetCompletionResponse(activity, requestModelId, null, ex, stopwatch);
                    throw;
                }

                streamedContents?.Add(update);
                yield return update;
            }
        }
        finally
        {
            if (activity is not null)
            {
                UsageContent? usageContent = streamedContents?.SelectMany(c => c.Contents).OfType<UsageContent>().LastOrDefault();
                SetCompletionResponse(
                    activity,
                    stopwatch,
                    requestModelId,
                    OrganizeStreamingContent(streamedContents),
                    streamedContents?.SelectMany(c => c.Contents).OfType<FunctionCallContent>(),
                    usage: usageContent?.Details);
            }

            await responseEnumerator.DisposeAsync();
        }
    }

    /// <summary>Gets a value indicating whether diagnostics are enabled.</summary>
    private bool Enabled => _activitySource.HasListeners();

    /// <summary>Convert chat history to a string aligned with the OpenAI format.</summary>
    private static string ToOpenAIFormat(IEnumerable<ChatMessage> messages, JsonSerializerOptions serializerOptions)
    {
        var sb = new StringBuilder().Append('[');

        string messageSeparator = string.Empty;
        foreach (var message in messages)
        {
            _ = sb.Append(messageSeparator);
            messageSeparator = ", \n";

            string text = string.Concat(message.Contents.OfType<TextContent>().Select(c => c.Text));
            _ = sb.Append("{\"role\": \"").Append(message.Role).Append("\", \"content\": ").Append(JsonSerializer.Serialize(text, serializerOptions.GetTypeInfo(typeof(string))));

            if (message.Contents.OfType<FunctionCallContent>().Any())
            {
                _ = sb.Append(", \"tool_calls\": ").Append('[');

                string messageItemSeparator = string.Empty;
                foreach (var functionCall in message.Contents.OfType<FunctionCallContent>())
                {
                    _ = sb.Append(messageItemSeparator);
                    messageItemSeparator = ", \n";

                    _ = sb.Append("{\"id\": \"").Append(functionCall.CallId)
                          .Append("\", \"function\": {\"arguments\": ").Append(JsonSerializer.Serialize(functionCall.Arguments, serializerOptions.GetTypeInfo(typeof(IDictionary<string, object?>))))
                          .Append(", \"name\": \"").Append(functionCall.Name)
                          .Append("\"}, \"type\": \"function\"}");
                }

                _ = sb.Append(']');
            }

            _ = sb.Append('}');
        }

        _ = sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Organize streaming content by choice index.</summary>
    private static Dictionary<int, List<StreamingChatCompletionUpdate>> OrganizeStreamingContent(IEnumerable<StreamingChatCompletionUpdate>? contents)
    {
        Dictionary<int, List<StreamingChatCompletionUpdate>> choices = [];
        if (contents is null)
        {
            return choices;
        }

        foreach (var content in contents)
        {
            if (!choices.TryGetValue(content.ChoiceIndex, out var choiceContents))
            {
                choices[content.ChoiceIndex] = choiceContents = [];
            }

            choiceContents.Add(content);
        }

        return choices;
    }

    /// <summary>Creates an activity for a chat completion request, or returns null if not enabled.</summary>
    private Activity? StartActivity(IList<ChatMessage> chatMessages, ChatOptions? options)
    {
        Activity? activity = null;
        if (Enabled)
        {
            string? modelId = options?.ModelId ?? _modelId;

            activity = _activitySource.StartActivity(
                $"chat.completions {modelId}",
                ActivityKind.Client,
                default(ActivityContext),
                [
                    new(OpenTelemetryConsts.GenAI.Operation.Name, "chat"),
                    new(OpenTelemetryConsts.GenAI.Request.Model, modelId),
                    new(OpenTelemetryConsts.GenAI.System, _modelProvider),
                ]);

            if (activity is not null)
            {
                if (_endpointAddress is not null)
                {
                    _ = activity
                        .SetTag(OpenTelemetryConsts.Server.Address, _endpointAddress)
                        .SetTag(OpenTelemetryConsts.Server.Port, _endpointPort);
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

                    if (options.AdditionalProperties?.TryGetConvertedValue("top_k", out double topK) is true)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.TopK, topK);
                    }

                    if (options.TopP is float top_p)
                    {
                        _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.TopP, top_p);
                    }
                }

                if (EnableSensitiveData)
                {
                    _ = activity.AddEvent(new ActivityEvent(
                        OpenTelemetryConsts.GenAI.Content.Prompt,
                        tags: new ActivityTagsCollection([new(OpenTelemetryConsts.GenAI.Prompt, ToOpenAIFormat(chatMessages, _jsonSerializerOptions))])));
                }
            }
        }

        return activity;
    }

    /// <summary>Adds chat completion information to the activity.</summary>
    private void SetCompletionResponse(
        Activity? activity,
        string? requestModelId,
        ChatCompletion? completions,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (!Enabled)
        {
            return;
        }

        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;

            AddMetricTags(ref tags, requestModelId, completions);
            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && completions?.Usage is { } usage)
        {
            if (usage.InputTokenCount is int inputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "input");
                AddMetricTags(ref tags, requestModelId, completions);
                _tokenUsageHistogram.Record(inputTokens);
            }

            if (usage.OutputTokenCount is int outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "output");
                AddMetricTags(ref tags, requestModelId, completions);
                _tokenUsageHistogram.Record(outputTokens);
            }
        }

        if (activity is null)
        {
            return;
        }

        if (error is not null)
        {
            _ = activity
                .SetTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
            return;
        }

        if (completions is not null)
        {
            if (completions.FinishReason is ChatFinishReason finishReason)
            {
#pragma warning disable CA1308 // Normalize strings to uppercase
                _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.FinishReasons, $"[\"{finishReason.Value.ToLowerInvariant()}\"]");
#pragma warning restore CA1308
            }

            if (!string.IsNullOrWhiteSpace(completions.CompletionId))
            {
                _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.Id, completions.CompletionId);
            }

            if (completions.ModelId is not null)
            {
                _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.Model, completions.ModelId);
            }

            if (completions.Usage?.InputTokenCount is int inputTokens)
            {
                _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.InputTokens, inputTokens);
            }

            if (completions.Usage?.OutputTokenCount is int outputTokens)
            {
                _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.OutputTokens, outputTokens);
            }

            if (EnableSensitiveData)
            {
                _ = activity.AddEvent(new ActivityEvent(
                    OpenTelemetryConsts.GenAI.Content.Completion,
                    tags: new ActivityTagsCollection([new(OpenTelemetryConsts.GenAI.Completion, ToOpenAIFormat(completions.Choices, _jsonSerializerOptions))])));
            }
        }
    }

    /// <summary>Adds streaming chat completion information to the activity.</summary>
    private void SetCompletionResponse(
        Activity? activity,
        Stopwatch? stopwatch,
        string? requestModelId,
        Dictionary<int, List<StreamingChatCompletionUpdate>> choices,
        IEnumerable<FunctionCallContent>? toolCalls,
        UsageDetails? usage)
    {
        if (activity is null || !Enabled || choices.Count == 0)
        {
            return;
        }

        string? id = null;
        ChatFinishReason? finishReason = null;
        string? modelId = null;
        List<ChatMessage> messages = new(choices.Count);

        foreach (var choice in choices)
        {
            ChatRole? role = null;
            List<AIContent> items = [];
            foreach (var update in choice.Value)
            {
                id ??= update.CompletionId;
                role ??= update.Role;
                finishReason ??= update.FinishReason;
                foreach (AIContent content in update.Contents)
                {
                    items.Add(content);
                    modelId ??= content.ModelId;
                }
            }

            messages.Add(new ChatMessage(role ?? ChatRole.Assistant, items));
        }

        if (toolCalls is not null && messages.FirstOrDefault()?.Contents is { } c)
        {
            foreach (var functionCall in toolCalls)
            {
                c.Add(functionCall);
            }
        }

        ChatCompletion completion = new(messages)
        {
            CompletionId = id,
            FinishReason = finishReason,
            ModelId = modelId,
            Usage = usage,
        };

        SetCompletionResponse(activity, requestModelId, completion, error: null, stopwatch);
    }

    private void AddMetricTags(ref TagList tags, string? requestModelId, ChatCompletion? completions)
    {
        tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, "chat");

        if (requestModelId is not null)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModelId);
        }

        tags.Add(OpenTelemetryConsts.GenAI.System, _modelProvider);

        if (_endpointAddress is string endpointAddress)
        {
            tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
            tags.Add(OpenTelemetryConsts.Server.Port, _endpointPort);
        }

        if (completions?.ModelId is string responseModel)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModel);
        }
    }
}
