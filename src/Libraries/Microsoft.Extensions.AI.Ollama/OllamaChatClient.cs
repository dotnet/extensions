// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S3358  // Ternary operators should not be nested

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for Ollama.</summary>
public sealed class OllamaChatClient : IChatClient
{
    private static readonly JsonElement _schemalessJsonResponseFormatValue = JsonDocument.Parse("\"json\"").RootElement;

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The api/chat endpoint URI.</summary>
    private readonly Uri _apiChatEndpoint;

    /// <summary>The <see cref="HttpClient"/> to use for sending requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The <see cref="JsonSerializerOptions"/> use for any serialization activities related to tool call arguments and results.</summary>
    private JsonSerializerOptions _toolCallJsonSerializerOptions = AIJsonUtilities.DefaultOptions;

    /// <summary>Initializes a new instance of the <see cref="OllamaChatClient"/> class.</summary>
    /// <param name="endpoint">The endpoint URI where Ollama is hosted.</param>
    /// <param name="modelId">
    /// The ID of the model to use. This ID can also be overridden per request via <see cref="ChatOptions.ModelId"/>.
    /// Either this parameter or <see cref="ChatOptions.ModelId"/> must provide a valid model ID.
    /// </param>
    /// <param name="httpClient">An <see cref="HttpClient"/> instance to use for HTTP operations.</param>
    public OllamaChatClient(string endpoint, string? modelId = null, HttpClient? httpClient = null)
        : this(new Uri(Throw.IfNull(endpoint)), modelId, httpClient)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OllamaChatClient"/> class.</summary>
    /// <param name="endpoint">The endpoint URI where Ollama is hosted.</param>
    /// <param name="modelId">
    /// The ID of the model to use. This ID can also be overridden per request via <see cref="ChatOptions.ModelId"/>.
    /// Either this parameter or <see cref="ChatOptions.ModelId"/> must provide a valid model ID.
    /// </param>
    /// <param name="httpClient">An <see cref="HttpClient"/> instance to use for HTTP operations.</param>
    public OllamaChatClient(Uri endpoint, string? modelId = null, HttpClient? httpClient = null)
    {
        _ = Throw.IfNull(endpoint);
        if (modelId is not null)
        {
            _ = Throw.IfNullOrWhitespace(modelId);
        }

        _apiChatEndpoint = new Uri(endpoint, "api/chat");
        _httpClient = httpClient ?? OllamaUtilities.SharedClient;

        _metadata = new("ollama", endpoint, modelId);
    }

    /// <summary>Gets or sets <see cref="JsonSerializerOptions"/> to use for any serialization activities related to tool call arguments and results.</summary>
    public JsonSerializerOptions ToolCallJsonSerializerOptions
    {
        get => _toolCallJsonSerializerOptions;
        set => _toolCallJsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        using var httpResponse = await _httpClient.PostAsJsonAsync(
            _apiChatEndpoint,
            ToOllamaChatRequest(chatMessages, options, stream: false),
            JsonContext.Default.OllamaChatRequest,
            cancellationToken).ConfigureAwait(false);

        if (!httpResponse.IsSuccessStatusCode)
        {
            await OllamaUtilities.ThrowUnsuccessfulOllamaResponseAsync(httpResponse, cancellationToken).ConfigureAwait(false);
        }

        var response = (await httpResponse.Content.ReadFromJsonAsync(
            JsonContext.Default.OllamaChatResponse,
            cancellationToken).ConfigureAwait(false))!;

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw new InvalidOperationException($"Ollama error: {response.Error}");
        }

        return new([FromOllamaMessage(response.Message!)])
        {
            CreatedAt = DateTimeOffset.TryParse(response.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset createdAt) ? createdAt : null,
            FinishReason = ToFinishReason(response),
            ModelId = response.Model ?? options?.ModelId ?? _metadata.ModelId,
            ResponseId = response.CreatedAt,
            Usage = ParseOllamaChatResponseUsage(response),
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        using HttpRequestMessage request = new(HttpMethod.Post, _apiChatEndpoint)
        {
            Content = JsonContent.Create(ToOllamaChatRequest(chatMessages, options, stream: true), JsonContext.Default.OllamaChatRequest)
        };
        using var httpResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!httpResponse.IsSuccessStatusCode)
        {
            await OllamaUtilities.ThrowUnsuccessfulOllamaResponseAsync(httpResponse, cancellationToken).ConfigureAwait(false);
        }

        using var httpResponseStream = await httpResponse.Content
#if NET
            .ReadAsStreamAsync(cancellationToken)
#else
            .ReadAsStreamAsync()
#endif
            .ConfigureAwait(false);

        using var streamReader = new StreamReader(httpResponseStream);
#if NET
        while ((await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is { } line)
#else
        while ((await streamReader.ReadLineAsync().ConfigureAwait(false)) is { } line)
#endif
        {
            var chunk = JsonSerializer.Deserialize(line, JsonContext.Default.OllamaChatResponse);
            if (chunk is null)
            {
                continue;
            }

            string? modelId = chunk.Model ?? _metadata.ModelId;

            ChatResponseUpdate update = new()
            {
                CreatedAt = DateTimeOffset.TryParse(chunk.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset createdAt) ? createdAt : null,
                FinishReason = ToFinishReason(chunk),
                ModelId = modelId,
                ResponseId = chunk.CreatedAt,
                Role = chunk.Message?.Role is not null ? new ChatRole(chunk.Message.Role) : null,
            };

            if (chunk.Message is { } message)
            {
                if (message.ToolCalls is { Length: > 0 })
                {
                    foreach (var toolCall in message.ToolCalls)
                    {
                        if (toolCall.Function is { } function)
                        {
                            update.Contents.Add(ToFunctionCallContent(function));
                        }
                    }
                }

                // Equivalent rule to the nonstreaming case
                if (message.Content?.Length > 0 || update.Contents.Count == 0)
                {
                    update.Contents.Insert(0, new TextContent(message.Content));
                }
            }

            if (ParseOllamaChatResponseUsage(chunk) is { } usage)
            {
                update.Contents.Add(new UsageContent(usage));
            }

            yield return update;
        }
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_httpClient != OllamaUtilities.SharedClient)
        {
            _httpClient.Dispose();
        }
    }

    private static UsageDetails? ParseOllamaChatResponseUsage(OllamaChatResponse response)
    {
        AdditionalPropertiesDictionary<long>? additionalCounts = null;
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.LoadDuration, "load_duration", ref additionalCounts);
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.TotalDuration, "total_duration", ref additionalCounts);
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.PromptEvalDuration, "prompt_eval_duration", ref additionalCounts);
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.EvalDuration, "eval_duration", ref additionalCounts);

        if (additionalCounts is not null || response.PromptEvalCount is not null || response.EvalCount is not null)
        {
            return new()
            {
                InputTokenCount = response.PromptEvalCount,
                OutputTokenCount = response.EvalCount,
                TotalTokenCount = response.PromptEvalCount.GetValueOrDefault() + response.EvalCount.GetValueOrDefault(),
                AdditionalCounts = additionalCounts,
            };
        }

        return null;
    }

    private static ChatFinishReason? ToFinishReason(OllamaChatResponse response) =>
        response.DoneReason switch
        {
            null => null,
            "length" => ChatFinishReason.Length,
            "stop" => ChatFinishReason.Stop,
            _ => new ChatFinishReason(response.DoneReason),
        };

    private static ChatMessage FromOllamaMessage(OllamaChatResponseMessage message)
    {
        List<AIContent> contents = [];

        // Add any tool calls.
        if (message.ToolCalls is { Length: > 0 })
        {
            foreach (var toolCall in message.ToolCalls)
            {
                if (toolCall.Function is { } function)
                {
                    contents.Add(ToFunctionCallContent(function));
                }
            }
        }

        // Ollama frequently sends back empty content with tool calls. Rather than always adding an empty
        // content, we only add the content if either it's not empty or there weren't any tool calls.
        if (message.Content?.Length > 0 || contents.Count == 0)
        {
            contents.Insert(0, new TextContent(message.Content));
        }

        return new ChatMessage(new(message.Role), contents);
    }

    private static FunctionCallContent ToFunctionCallContent(OllamaFunctionToolCall function)
    {
#if NET
        var id = System.Security.Cryptography.RandomNumberGenerator.GetHexString(8);
#else
        var id = Guid.NewGuid().ToString().Substring(0, 8);
#endif
        return new FunctionCallContent(id, function.Name, function.Arguments);
    }

    private static JsonElement? ToOllamaChatResponseFormat(ChatResponseFormat? format)
    {
        if (format is ChatResponseFormatJson jsonFormat)
        {
            return jsonFormat.Schema ?? _schemalessJsonResponseFormatValue;
        }
        else
        {
            return null;
        }
    }

    private OllamaChatRequest ToOllamaChatRequest(IList<ChatMessage> chatMessages, ChatOptions? options, bool stream)
    {
        OllamaChatRequest request = new()
        {
            Format = ToOllamaChatResponseFormat(options?.ResponseFormat),
            Messages = chatMessages.SelectMany(ToOllamaChatRequestMessages).ToArray(),
            Model = options?.ModelId ?? _metadata.ModelId ?? string.Empty,
            Stream = stream,
            Tools = options?.ToolMode is not NoneChatToolMode && options?.Tools is { Count: > 0 } tools ? tools.OfType<AIFunction>().Select(ToOllamaTool) : null,
        };

        if (options is not null)
        {
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.embedding_only), (options, value) => options.embedding_only = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.f16_kv), (options, value) => options.f16_kv = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.logits_all), (options, value) => options.logits_all = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.low_vram), (options, value) => options.low_vram = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.main_gpu), (options, value) => options.main_gpu = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.min_p), (options, value) => options.min_p = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.mirostat), (options, value) => options.mirostat = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.mirostat_eta), (options, value) => options.mirostat_eta = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.mirostat_tau), (options, value) => options.mirostat_tau = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.num_batch), (options, value) => options.num_batch = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.num_ctx), (options, value) => options.num_ctx = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.num_gpu), (options, value) => options.num_gpu = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.num_keep), (options, value) => options.num_keep = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.num_thread), (options, value) => options.num_thread = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.numa), (options, value) => options.numa = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.penalize_newline), (options, value) => options.penalize_newline = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.repeat_last_n), (options, value) => options.repeat_last_n = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.repeat_penalty), (options, value) => options.repeat_penalty = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.tfs_z), (options, value) => options.tfs_z = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.typical_p), (options, value) => options.typical_p = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.use_mmap), (options, value) => options.use_mmap = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.use_mlock), (options, value) => options.use_mlock = value);
            TransferMetadataValue<bool>(nameof(OllamaRequestOptions.vocab_only), (options, value) => options.vocab_only = value);

            if (options.FrequencyPenalty is float frequencyPenalty)
            {
                (request.Options ??= new()).frequency_penalty = frequencyPenalty;
            }

            if (options.MaxOutputTokens is int maxOutputTokens)
            {
                (request.Options ??= new()).num_predict = maxOutputTokens;
            }

            if (options.PresencePenalty is float presencePenalty)
            {
                (request.Options ??= new()).presence_penalty = presencePenalty;
            }

            if (options.StopSequences is { Count: > 0 })
            {
                (request.Options ??= new()).stop = [.. options.StopSequences];
            }

            if (options.Temperature is float temperature)
            {
                (request.Options ??= new()).temperature = temperature;
            }

            if (options.TopP is float topP)
            {
                (request.Options ??= new()).top_p = topP;
            }

            if (options.TopK is int topK)
            {
                (request.Options ??= new()).top_k = topK;
            }

            if (options.Seed is long seed)
            {
                (request.Options ??= new()).seed = seed;
            }
        }

        return request;

        void TransferMetadataValue<T>(string propertyName, Action<OllamaRequestOptions, T> setOption)
        {
            if (options.AdditionalProperties?.TryGetValue(propertyName, out T? t) is true)
            {
                request.Options ??= new();
                setOption(request.Options, t);
            }
        }
    }

    private IEnumerable<OllamaChatRequestMessage> ToOllamaChatRequestMessages(ChatMessage content)
    {
        // In general, we return a single request message for each understood content item.
        // However, various image models expect both text and images in the same request message.
        // To handle that, attach images to a previous text message if one exists.

        OllamaChatRequestMessage? currentTextMessage = null;
        foreach (var item in content.Contents)
        {
            if (item is DataContent dataContent && dataContent.MediaTypeStartsWith("image/") && dataContent.Data.HasValue)
            {
                IList<string> images = currentTextMessage?.Images ?? [];
                images.Add(Convert.ToBase64String(dataContent.Data.Value
#if NET
                    .Span));
#else
                    .ToArray()));
#endif

                if (currentTextMessage is not null)
                {
                    currentTextMessage.Images = images;
                }
                else
                {
                    yield return new OllamaChatRequestMessage
                    {
                        Role = content.Role.Value,
                        Images = images,
                    };
                }
            }
            else
            {
                if (currentTextMessage is not null)
                {
                    yield return currentTextMessage;
                    currentTextMessage = null;
                }

                switch (item)
                {
                    case TextContent textContent:
                        currentTextMessage = new OllamaChatRequestMessage
                        {
                            Role = content.Role.Value,
                            Content = textContent.Text,
                        };
                        break;

                    case FunctionCallContent fcc:
                    {
                        yield return new OllamaChatRequestMessage
                        {
                            Role = "assistant",
                            Content = JsonSerializer.Serialize(new OllamaFunctionCallContent
                            {
                                CallId = fcc.CallId,
                                Name = fcc.Name,
                                Arguments = JsonSerializer.SerializeToElement(fcc.Arguments, ToolCallJsonSerializerOptions.GetTypeInfo(typeof(IDictionary<string, object?>))),
                            }, JsonContext.Default.OllamaFunctionCallContent)
                        };
                        break;
                    }

                    case FunctionResultContent frc:
                    {
                        JsonElement jsonResult = JsonSerializer.SerializeToElement(frc.Result, ToolCallJsonSerializerOptions.GetTypeInfo(typeof(object)));
                        yield return new OllamaChatRequestMessage
                        {
                            Role = "tool",
                            Content = JsonSerializer.Serialize(new OllamaFunctionResultContent
                            {
                                CallId = frc.CallId,
                                Result = jsonResult,
                            }, JsonContext.Default.OllamaFunctionResultContent)
                        };
                        break;
                    }
                }
            }
        }

        if (currentTextMessage is not null)
        {
            yield return currentTextMessage;
        }
    }

    private static OllamaTool ToOllamaTool(AIFunction function)
    {
        return new()
        {
            Type = "function",
            Function = new OllamaFunctionTool
            {
                Name = function.Name,
                Description = function.Description,
                Parameters = JsonSerializer.Deserialize(function.JsonSchema, JsonContext.Default.OllamaFunctionToolParameters)!,
            }
        };
    }
}
