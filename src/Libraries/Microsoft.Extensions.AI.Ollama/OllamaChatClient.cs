// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)

namespace Microsoft.Extensions.AI;

/// <summary>An <see cref="IChatClient"/> for Ollama.</summary>
public sealed class OllamaChatClient : IChatClient
{
    /// <summary>The api/chat endpoint URI.</summary>
    private readonly Uri _apiChatEndpoint;

    /// <summary>The <see cref="HttpClient"/> to use for sending requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="OllamaChatClient"/> class.</summary>
    /// <param name="endpoint">The endpoint URI where Ollama is hosted.</param>
    /// <param name="modelId">
    /// The id of the model to use. This may also be overridden per request via <see cref="ChatOptions.ModelId"/>.
    /// Either this parameter or <see cref="ChatOptions.ModelId"/> must provide a valid model id.
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
        Metadata = new("ollama", endpoint, modelId);
    }

    /// <inheritdoc />
    public ChatClientMetadata Metadata { get; }

    /// <summary>Gets or sets <see cref="JsonSerializerOptions"/> to use for any serialization activities related to tool call arguments and results.</summary>
    public JsonSerializerOptions? ToolCallJsonSerializerOptions { get; set; }

    /// <inheritdoc />
    public async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        using var httpResponse = await _httpClient.PostAsJsonAsync(
            _apiChatEndpoint,
            ToOllamaChatRequest(chatMessages, options, stream: false),
            JsonContext.Default.OllamaChatRequest,
            cancellationToken).ConfigureAwait(false);

        var response = (await httpResponse.Content.ReadFromJsonAsync(
            JsonContext.Default.OllamaChatResponse,
            cancellationToken).ConfigureAwait(false))!;

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw new InvalidOperationException($"Ollama error: {response.Error}");
        }

        return new([FromOllamaMessage(response.Message!)])
        {
            CompletionId = response.CreatedAt,
            ModelId = response.Model ?? options?.ModelId ?? Metadata.ModelId,
            CreatedAt = DateTimeOffset.TryParse(response.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset createdAt) ? createdAt : null,
            AdditionalProperties = ParseOllamaChatResponseProps(response),
            FinishReason = ToFinishReason(response),
            Usage = ParseOllamaChatResponseUsage(response),
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        if (options?.Tools is { Count: > 0 })
        {
            // We can actually make it work by using the /generate endpoint like the eShopSupport sample does,
            // but it's complicated. Really it should be Ollama's job to support this.
            throw new NotSupportedException(
                "Currently, Ollama does not support function calls in streaming mode. " +
                "See Ollama docs at https://github.com/ollama/ollama/blob/main/docs/api.md#parameters-1 to see whether support has since been added.");
        }

        using HttpRequestMessage request = new(HttpMethod.Post, _apiChatEndpoint)
        {
            Content = JsonContent.Create(ToOllamaChatRequest(chatMessages, options, stream: true), JsonContext.Default.OllamaChatRequest)
        };
        using var httpResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        using var httpResponseStream = await httpResponse.Content
#if NET
            .ReadAsStreamAsync(cancellationToken)
#else
            .ReadAsStreamAsync()
#endif
            .ConfigureAwait(false);

        await foreach (OllamaChatResponse? chunk in JsonSerializer.DeserializeAsyncEnumerable(
            httpResponseStream,
            JsonContext.Default.OllamaChatResponse,
            topLevelValues: true,
            cancellationToken).ConfigureAwait(false))
        {
            if (chunk is null)
            {
                continue;
            }

            StreamingChatCompletionUpdate update = new()
            {
                Role = chunk.Message?.Role is not null ? new ChatRole(chunk.Message.Role) : null,
                CreatedAt = DateTimeOffset.TryParse(chunk.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset createdAt) ? createdAt : null,
                AdditionalProperties = ParseOllamaChatResponseProps(chunk),
                FinishReason = ToFinishReason(chunk),
            };

            string? modelId = chunk.Model ?? Metadata.ModelId;

            if (chunk.Message is { } message)
            {
                update.Contents.Add(new TextContent(message.Content) { ModelId = modelId });
            }

            if (ParseOllamaChatResponseUsage(chunk) is { } usage)
            {
                update.Contents.Add(new UsageContent(usage) { ModelId = modelId });
            }

            yield return update;
        }
    }

    /// <inheritdoc />
    public TService? GetService<TService>(object? key = null)
        where TService : class
        => key is null ? this as TService : null;

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
        if (response.PromptEvalCount is not null || response.EvalCount is not null)
        {
            return new()
            {
                InputTokenCount = response.PromptEvalCount,
                OutputTokenCount = response.EvalCount,
                TotalTokenCount = response.PromptEvalCount.GetValueOrDefault() + response.EvalCount.GetValueOrDefault(),
            };
        }

        return null;
    }

    private static AdditionalPropertiesDictionary? ParseOllamaChatResponseProps(OllamaChatResponse response)
    {
        AdditionalPropertiesDictionary? metadata = null;

        OllamaUtilities.TransferNanosecondsTime(response, static r => r.LoadDuration, "load_duration", ref metadata);
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.TotalDuration, "total_duration", ref metadata);
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.PromptEvalDuration, "prompt_eval_duration", ref metadata);
        OllamaUtilities.TransferNanosecondsTime(response, static r => r.EvalDuration, "eval_duration", ref metadata);

        return metadata;
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
                    var id = Guid.NewGuid().ToString().Substring(0, 8);
                    contents.Add(new FunctionCallContent(id, function.Name, function.Arguments));
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

    private OllamaChatRequest ToOllamaChatRequest(IList<ChatMessage> chatMessages, ChatOptions? options, bool stream)
    {
        OllamaChatRequest request = new()
        {
            Format = options?.ResponseFormat is ChatResponseFormatJson ? "json" : null,
            Messages = chatMessages.SelectMany(ToOllamaChatRequestMessages).ToArray(),
            Model = options?.ModelId ?? Metadata.ModelId ?? string.Empty,
            Stream = stream,
            Tools = options?.Tools is { Count: > 0 } tools ? tools.OfType<AIFunction>().Select(ToOllamaTool) : null,
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
            TransferMetadataValue<long>(nameof(OllamaRequestOptions.seed), (options, value) => options.seed = value);
            TransferMetadataValue<float>(nameof(OllamaRequestOptions.tfs_z), (options, value) => options.tfs_z = value);
            TransferMetadataValue<int>(nameof(OllamaRequestOptions.top_k), (options, value) => options.top_k = value);
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
        }

        return request;

        void TransferMetadataValue<T>(string propertyName, Action<OllamaRequestOptions, T> setOption)
        {
            if (options.AdditionalProperties?.TryGetConvertedValue(propertyName, out T? t) is true)
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
            if (currentTextMessage is not null && item is not ImageContent)
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
                        Content = textContent.Text ?? string.Empty,
                    };
                    break;

                case ImageContent imageContent when imageContent.Data is not null:
                    IList<string> images = currentTextMessage?.Images ?? [];
                    images.Add(Convert.ToBase64String(imageContent.Data.Value
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

                    break;

                case FunctionCallContent fcc:
                    yield return new OllamaChatRequestMessage
                    {
                        Role = "assistant",
                        Content = JsonSerializer.Serialize(new OllamaFunctionCallContent
                        {
                            CallId = fcc.CallId,
                            Name = fcc.Name,
                            Arguments = FunctionCallHelpers.FormatFunctionParametersAsJsonElement(fcc.Arguments, ToolCallJsonSerializerOptions),
                        }, JsonContext.Default.OllamaFunctionCallContent)
                    };
                    break;

                case FunctionResultContent frc:
                    JsonElement jsonResult = FunctionCallHelpers.FormatFunctionResultAsJsonElement(frc.Result, ToolCallJsonSerializerOptions);
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

        if (currentTextMessage is not null)
        {
            yield return currentTextMessage;
        }
    }

    private OllamaTool ToOllamaTool(AIFunction function) => new()
    {
        Type = "function",
        Function = new OllamaFunctionTool
        {
            Name = function.Metadata.Name,
            Description = function.Metadata.Description,
            Parameters = new OllamaFunctionToolParameters
            {
                Properties = function.Metadata.Parameters.ToDictionary(
                    p => p.Name,
                    p => FunctionCallHelpers.InferParameterJsonSchema(p, function.Metadata, ToolCallJsonSerializerOptions)),
                Required = function.Metadata.Parameters.Where(p => p.IsRequired).Select(p => p.Name).ToList(),
            },
        }
    };
}
