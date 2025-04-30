// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using OpenAI.Chat;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OpenAIChatClientTests
{
    [Fact]
    public void AsIChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("chatClient", () => ((ChatClient)null!).AsIChatClient());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsIChatClient_OpenAIClient_ProducesExpectedMetadata(bool useAzureOpenAI)
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var client = useAzureOpenAI ?
            new AzureOpenAIClient(endpoint, new ApiKeyCredential("key")) :
            new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IChatClient chatClient = client.GetChatClient(model).AsIChatClient();
        var metadata = chatClient.GetService<ChatClientMetadata>();
        Assert.Equal("openai", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);

        chatClient = client.GetChatClient(model).AsIChatClient();
        metadata = chatClient.GetService<ChatClientMetadata>();
        Assert.Equal("openai", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_OpenAIClient_SuccessfullyReturnsUnderlyingClient()
    {
        ChatClient openAIClient = new OpenAIClient(new ApiKeyCredential("key")).GetChatClient("model");
        IChatClient chatClient = openAIClient.AsIChatClient();

        Assert.Same(chatClient, chatClient.GetService<IChatClient>());

        Assert.Same(openAIClient, chatClient.GetService<ChatClient>());

        Assert.NotNull(chatClient.GetService<ChatClient>());

        using IChatClient pipeline = chatClient
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .Build();

        Assert.NotNull(pipeline.GetService<FunctionInvokingChatClient>());
        Assert.NotNull(pipeline.GetService<DistributedCachingChatClient>());
        Assert.NotNull(pipeline.GetService<CachingChatClient>());
        Assert.NotNull(pipeline.GetService<OpenTelemetryChatClient>());

        Assert.Same(openAIClient, pipeline.GetService<ChatClient>());
        Assert.IsType<FunctionInvokingChatClient>(pipeline.GetService<IChatClient>());
    }

    [Fact]
    public void GetService_ChatClient_SuccessfullyReturnsUnderlyingClient()
    {
        ChatClient openAIClient = new OpenAIClient(new ApiKeyCredential("key")).GetChatClient("model");
        IChatClient chatClient = openAIClient.AsIChatClient();

        Assert.Same(chatClient, chatClient.GetService<IChatClient>());
        Assert.Same(openAIClient, chatClient.GetService<ChatClient>());

        using IChatClient pipeline = chatClient
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .Build();

        Assert.NotNull(pipeline.GetService<FunctionInvokingChatClient>());
        Assert.NotNull(pipeline.GetService<DistributedCachingChatClient>());
        Assert.NotNull(pipeline.GetService<CachingChatClient>());
        Assert.NotNull(pipeline.GetService<OpenTelemetryChatClient>());

        Assert.Same(openAIClient, pipeline.GetService<ChatClient>());
        Assert.IsType<FunctionInvokingChatClient>(pipeline.GetService<IChatClient>());
    }

    [Fact]
    public async Task BasicRequestResponse_NonStreaming()
    {
        const string Input = """
            {
                "temperature":0.5,
                "messages":[{"role":"user","content":"hello"}],
                "model":"gpt-4o-mini",
                "max_completion_tokens":10
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "object": "chat.completion",
              "created": 1727888631,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?",
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "stop"
                }
              ],
              "usage": {
                "prompt_tokens": 8,
                "completion_tokens": 9,
                "total_tokens": 17,
                "prompt_tokens_details": {
                  "cached_tokens": 13
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync("hello", new()
        {
            MaxOutputTokens = 10,
            Temperature = 0.5f,
        });
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI", response.ResponseId);
        Assert.Equal("Hello! How can I assist you today?", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI", response.Messages.Single().MessageId);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_888_631), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(8, response.Usage.InputTokenCount);
        Assert.Equal(9, response.Usage.OutputTokenCount);
        Assert.Equal(17, response.Usage.TotalTokenCount);
        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 13 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task BasicRequestResponse_Streaming()
    {
        const string Input = """
            {
                "temperature":0.5,
                "messages":[{"role":"user","content":"hello"}],
                "model":"gpt-4o-mini",
                "stream":true,
                "stream_options":{"include_usage":true},
                "max_completion_tokens":20
            }
            """;

        const string Output = """
            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"role":"assistant","content":"","refusal":null},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":"Hello"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":"!"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":" How"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":" can"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":" I"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":" assist"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":" you"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":" today"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"content":"?"},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{},"logprobs":null,"finish_reason":"stop"}],"usage":null}

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[],"usage":{"prompt_tokens":8,"completion_tokens":9,"total_tokens":17,"prompt_tokens_details":{"cached_tokens":5,"audio_tokens":123},"completion_tokens_details":{"reasoning_tokens":90,"audio_tokens":456}}}

            data: [DONE]

            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync("hello", new()
        {
            MaxOutputTokens = 20,
            Temperature = 0.5f,
        }))
        {
            updates.Add(update);
        }

        Assert.Equal("Hello! How can I assist you today?", string.Concat(updates.Select(u => u.Text)));

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(1_727_889_370);
        Assert.Equal(12, updates.Count);
        for (int i = 0; i < updates.Count; i++)
        {
            Assert.Equal("chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK", updates[i].ResponseId);
            Assert.Equal("chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK", updates[i].MessageId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("gpt-4o-mini-2024-07-18", updates[i].ModelId);
            Assert.Equal(ChatRole.Assistant, updates[i].Role);
            Assert.NotNull(updates[i].AdditionalProperties);
            Assert.Equal("fp_f85bea6784", updates[i].AdditionalProperties![nameof(ChatCompletion.SystemFingerprint)]);
            Assert.Equal(i == 10 ? 0 : 1, updates[i].Contents.Count);
            Assert.Equal(i < 10 ? null : ChatFinishReason.Stop, updates[i].FinishReason);
        }

        UsageContent usage = updates.SelectMany(u => u.Contents).OfType<UsageContent>().Single();
        Assert.Equal(8, usage.Details.InputTokenCount);
        Assert.Equal(9, usage.Details.OutputTokenCount);
        Assert.Equal(17, usage.Details.TotalTokenCount);

        Assert.Equal(new AdditionalPropertiesDictionary<long>
        {
            { "InputTokenDetails.AudioTokenCount", 123 },
            { "InputTokenDetails.CachedTokenCount", 5 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 456 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, usage.Details.AdditionalCounts);
    }

    [Fact]
    public async Task NonStronglyTypedOptions_AllSent()
    {
        const string Input = """
            {
                "messages":[{"role":"user","content":"hello"}],
                "model":"gpt-4o-mini",
                "logprobs":true,
                "top_logprobs":42,
                "logit_bias":{"12":34},
                "parallel_tool_calls":false,
                "user":"12345",
                "metadata":{"something":"else"},
                "store":true
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "object": "chat.completion",
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?"
                  },
                  "finish_reason": "stop"
                }
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        Assert.NotNull(await client.GetResponseAsync("hello", new()
        {
            AllowMultipleToolCalls = false,
            AdditionalProperties = new()
            {
                ["StoredOutputEnabled"] = true,
                ["Metadata"] = new Dictionary<string, string>
                {
                    ["something"] = "else",
                },
                ["LogitBiases"] = new Dictionary<int, int> { { 12, 34 } },
                ["IncludeLogProbabilities"] = true,
                ["TopLogProbabilityCount"] = 42,
                ["EndUserId"] = "12345",
            },
        }));
    }

    [Fact]
    public async Task MultipleMessages_NonStreaming()
    {
        const string Input = """
            {
                "frequency_penalty": 0.75,
                "presence_penalty": 0.5,
                "temperature": 0.25,
                "messages": [
                    {
                        "role": "system",
                        "content": "You are a really nice friend."
                    },
                    {
                        "role": "user",
                        "content": "hello!"
                    },
                    {
                        "role": "assistant",
                        "content": "hi, how are you?"
                    },
                    {
                        "role": "user",
                        "content": "i'm good. how are you?"
                    }
                ],
                "model": "gpt-4o-mini",
                "stop": ["great"],
                "seed": 42
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P",
              "object": "chat.completion",
              "created": 1727894187,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "I’m doing well, thank you! What’s on your mind today?",
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "stop"
                }
              ],
              "usage": {
                "prompt_tokens": 42,
                "completion_tokens": 15,
                "total_tokens": 57,
                "prompt_tokens_details": {
                  "cached_tokens": 13,
                  "audio_tokens": 123
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90,
                  "audio_tokens": 456
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a really nice friend."),
            new(ChatRole.User, "hello!"),
            new(ChatRole.Assistant, "hi, how are you?"),
            new(ChatRole.User, "i'm good. how are you?"),
        ];

        var response = await client.GetResponseAsync(messages, new()
        {
            Temperature = 0.25f,
            FrequencyPenalty = 0.75f,
            PresencePenalty = 0.5f,
            StopSequences = ["great"],
            Seed = 42,
        });
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.ResponseId);
        Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.Messages.Single().MessageId);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 123 },
            { "InputTokenDetails.CachedTokenCount", 13 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 456 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task MultiPartSystemMessage_NonStreaming()
    {
        const string Input = """
            {
                "messages": [
                    {
                        "role": "system",
                        "content": [
                            {
                                "type": "text",
                                "text": "You are a really nice friend."
                            },
                            {
                                "type": "text",
                                "text": "Really nice."
                            }
                        ]
                    },
                    {
                        "role": "user",
                        "content": "hello!"
                    }
                ],
                "model": "gpt-4o-mini"
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P",
              "object": "chat.completion",
              "created": 1727894187,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "Hi! It's so good to hear from you!",
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "stop"
                }
              ],
              "usage": {
                "prompt_tokens": 42,
                "completion_tokens": 15,
                "total_tokens": 57,
                "prompt_tokens_details": {
                  "cached_tokens": 13
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<ChatMessage> messages =
        [
            new(ChatRole.System, [new TextContent("You are a really nice friend."), new TextContent("Really nice.")]),
            new(ChatRole.User, "hello!"),
        ];

        var response = await client.GetResponseAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.ResponseId);
        Assert.Equal("Hi! It's so good to hear from you!", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 13 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task EmptyAssistantMessage_NonStreaming()
    {
        const string Input = """
            {
                "messages": [
                    {
                        "role": "system",
                        "content": "You are a really nice friend."
                    },
                    {
                        "role": "user",
                        "content": "hello!"
                    },
                    {
                        "role": "assistant",
                        "content": ""
                    },
                    {
                        "role": "user",
                        "content": "i\u0027m good. how are you?"
                    }
                ],
                "model": "gpt-4o-mini"
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P",
              "object": "chat.completion",
              "created": 1727894187,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "I’m doing well, thank you! What’s on your mind today?",
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "stop"
                }
              ],
              "usage": {
                "prompt_tokens": 42,
                "completion_tokens": 15,
                "total_tokens": 57,
                "prompt_tokens_details": {
                  "cached_tokens": 13
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a really nice friend."),
            new(ChatRole.User, "hello!"),
            new(ChatRole.Assistant, (string?)null),
            new(ChatRole.User, "i'm good. how are you?"),
        ];

        var response = await client.GetResponseAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.ResponseId);
        Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 13 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task FunctionCallContent_NonStreaming()
    {
        const string Input = """
            {
                "tools": [
                    {
                        "function": {
                            "description": "Gets the age of the specified person.",
                            "name": "GetPersonAge",
                            "parameters": {
                                "type": "object",
                                "required": [
                                    "personName"
                                ],
                                "properties": {
                                    "personName": {
                                        "description": "The person whose age is being requested",
                                        "type": "string"
                                    }
                                },
                                "additionalProperties": false
                            }
                        },
                        "type": "function"
                    }
                ],
                "messages": [
                    {
                        "role": "user",
                        "content": "How old is Alice?"
                    }
                ],
                "model": "gpt-4o-mini",
                "tool_choice": "auto"
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADydKhrSKEBWJ8gy0KCIU74rN3Hmk",
              "object": "chat.completion",
              "created": 1727894702,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": null,
                    "tool_calls": [
                      {
                        "id": "call_8qbINM045wlmKZt9bVJgwAym",
                        "type": "function",
                        "function": {
                          "name": "GetPersonAge",
                          "arguments": "{\"personName\":\"Alice\"}"
                        }
                      }
                    ],
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "tool_calls"
                }
              ],
              "usage": {
                "prompt_tokens": 61,
                "completion_tokens": 16,
                "total_tokens": 77,
                "prompt_tokens_details": {
                  "cached_tokens": 13
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync("How old is Alice?", new()
        {
            Tools = [AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.")],
        });
        Assert.NotNull(response);

        Assert.Empty(response.Text);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_702), response.CreatedAt);
        Assert.Equal(ChatFinishReason.ToolCalls, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(61, response.Usage.InputTokenCount);
        Assert.Equal(16, response.Usage.OutputTokenCount);
        Assert.Equal(77, response.Usage.TotalTokenCount);

        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 13 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.Single(response.Messages.Single().Contents);
        FunctionCallContent fcc = Assert.IsType<FunctionCallContent>(response.Messages.Single().Contents[0]);
        Assert.Equal("GetPersonAge", fcc.Name);
        AssertExtensions.EqualFunctionCallParameters(new Dictionary<string, object?> { ["personName"] = "Alice" }, fcc.Arguments);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task UnavailableBuiltInFunctionCall_NonStreaming()
    {
        const string Input = """
            {
                "messages": [
                    {
                        "role": "user",
                        "content": "What day is it?"
                    }
                ],
                "model": "gpt-4o-mini"
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADydKhrSKEBWJ8gy0KCIU74rN3Hmk",
              "object": "chat.completion",
              "created": 1727894702,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "December 31, 2023",
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "stop"
                }
              ],
              "usage": {
                "prompt_tokens": 61,
                "completion_tokens": 16,
                "total_tokens": 77,
                "prompt_tokens_details": {
                  "cached_tokens": 13
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync("What day is it?", new()
        {
            Tools = [new HostedWebSearchTool()],
        });
        Assert.NotNull(response);

        Assert.Equal("December 31, 2023", response.Text);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_702), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(61, response.Usage.InputTokenCount);
        Assert.Equal(16, response.Usage.OutputTokenCount);
        Assert.Equal(77, response.Usage.TotalTokenCount);

        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 13 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.Single(response.Messages.Single().Contents);
        TextContent fcc = Assert.IsType<TextContent>(response.Messages.Single().Contents[0]);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task FunctionCallContent_Streaming()
    {
        const string Input = """
            {
                "tools": [
                    {
                        "function": {
                            "description": "Gets the age of the specified person.",
                            "name": "GetPersonAge",
                            "parameters": {
                                "type": "object",
                                "required": [
                                    "personName"
                                ],
                                "properties": {
                                    "personName": {
                                        "description": "The person whose age is being requested",
                                        "type": "string"
                                    }
                                },
                                "additionalProperties": false
                            }
                        },
                        "type": "function"
                    }
                ],
                "messages": [
                    {
                        "role": "user",
                        "content": "How old is Alice?"
                    }
                ],
                "model": "gpt-4o-mini",
                "stream": true,
                "stream_options": {
                    "include_usage": true
                },
                "tool_choice": "auto"
            }
            """;

        const string Output = """
            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"role":"assistant","content":null,"tool_calls":[{"index":0,"id":"call_F9ZaqPWo69u0urxAhVt8meDW","type":"function","function":{"name":"GetPersonAge","arguments":""}}],"refusal":null},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"{\""}}]},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"person"}}]},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"Name"}}]},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"\":\""}}]},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"Alice"}}]},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"\"}"}}]},"logprobs":null,"finish_reason":null}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[{"index":0,"delta":{},"logprobs":null,"finish_reason":"tool_calls"}],"usage":null}

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[],"usage":{"prompt_tokens":61,"completion_tokens":16,"total_tokens":77,"prompt_tokens_details":{"cached_tokens":0},"completion_tokens_details":{"reasoning_tokens":90}}}

            data: [DONE]

            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync("How old is Alice?", new()
        {
            Tools = [AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.")],
        }))
        {
            updates.Add(update);
        }

        Assert.Equal("", string.Concat(updates.Select(u => u.Text)));

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(1_727_895_263);
        Assert.Equal(10, updates.Count);
        for (int i = 0; i < updates.Count; i++)
        {
            Assert.Equal("chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl", updates[i].ResponseId);
            Assert.Equal("chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl", updates[i].MessageId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("gpt-4o-mini-2024-07-18", updates[i].ModelId);
            Assert.Equal(ChatRole.Assistant, updates[i].Role);
            Assert.NotNull(updates[i].AdditionalProperties);
            Assert.Equal("fp_f85bea6784", updates[i].AdditionalProperties![nameof(ChatCompletion.SystemFingerprint)]);
            Assert.Equal(i < 7 ? null : ChatFinishReason.ToolCalls, updates[i].FinishReason);
        }

        FunctionCallContent fcc = Assert.IsType<FunctionCallContent>(Assert.Single(updates[updates.Count - 1].Contents));
        Assert.Equal("call_F9ZaqPWo69u0urxAhVt8meDW", fcc.CallId);
        Assert.Equal("GetPersonAge", fcc.Name);
        AssertExtensions.EqualFunctionCallParameters(new Dictionary<string, object?> { ["personName"] = "Alice" }, fcc.Arguments);

        UsageContent usage = updates.SelectMany(u => u.Contents).OfType<UsageContent>().Single();
        Assert.Equal(61, usage.Details.InputTokenCount);
        Assert.Equal(16, usage.Details.OutputTokenCount);
        Assert.Equal(77, usage.Details.TotalTokenCount);

        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 0 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, usage.Details.AdditionalCounts);
    }

    [Fact]
    public async Task AssistantMessageWithBothToolsAndContent_NonStreaming()
    {
        const string Input = """
            {
                "messages": [
                    {
                        "role": "system",
                        "content": "You are a really nice friend."
                    },
                    {
                        "role": "user",
                        "content": "hello!"
                    },
                    {
                        "role": "assistant",
                        "content": "hi, how are you?",
                        "tool_calls": [
                            {
                                "id": "12345",
                                "function": {
                                    "name": "SayHello",
                                    "arguments": "null"
                                },
                                "type": "function"
                            },
                            {
                                "id": "12346",
                                "function": {
                                    "name": "SayHi",
                                    "arguments": "null"
                                },
                                "type": "function"
                            }
                        ]
                    },
                    {
                        "role": "tool",
                        "tool_call_id": "12345",
                        "content": "Said hello"
                    },
                    {
                        "role":"tool",
                        "tool_call_id":"12346",
                        "content":"Said hi"
                    },
                    {
                        "role":"assistant",
                        "content":"You are great."
                    },
                    {
                        "role":"user",
                        "content":"Thanks!"
                    }
                ],
                "model":"gpt-4o-mini"
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P",
              "object": "chat.completion",
              "created": 1727894187,
              "model": "gpt-4o-mini-2024-07-18",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "I’m doing well, thank you! What’s on your mind today?",
                    "refusal": null
                  },
                  "logprobs": null,
                  "finish_reason": "stop"
                }
              ],
              "usage": {
                "prompt_tokens": 42,
                "completion_tokens": 15,
                "total_tokens": 57,
                "prompt_tokens_details": {
                  "cached_tokens": 20
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 90
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a really nice friend."),
            new(ChatRole.User, "hello!"),
            new(ChatRole.Assistant,
            [
                new TextContent("hi, how are you?"),
                new FunctionCallContent("12345", "SayHello"),
                new FunctionCallContent("12346", "SayHi"),
            ]),
            new (ChatRole.Tool,
            [
                new FunctionResultContent("12345", "Said hello"),
                new FunctionResultContent("12346", "Said hi"),
            ]),
            new(ChatRole.Assistant, "You are great."),
            new(ChatRole.User, "Thanks!"),
        ];

        var response = await client.GetResponseAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.ResponseId);
        Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 20 },
            { "OutputTokenDetails.ReasoningTokenCount", 90 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public Task DataContentMessage_Image_AdditionalProperty_ChatImageDetailLevel_NonStreaming()
        => DataContentMessage_Image_AdditionalPropertyDetail_NonStreaming("high");

    [Fact]
    public Task DataContentMessage_Image_AdditionalProperty_StringDetail_NonStreaming()
        => DataContentMessage_Image_AdditionalPropertyDetail_NonStreaming(ChatImageDetailLevel.High);

    private static async Task DataContentMessage_Image_AdditionalPropertyDetail_NonStreaming(object detailValue)
    {
        string input = $$"""
            {
              "messages": [
                {
                  "role": "user",
                  "content": [
                    {
                      "type": "text",
                      "text": "What does this logo say?"
                    },
                    {
                      "type": "image_url",
                      "image_url": {
                        "detail": "high",
                        "url": "{{ImageDataUri.GetImageDataUri()}}"
                      }
                    }
                  ]
                }
              ],
              "model": "gpt-4o-mini"
            }
            """;

        const string Output = """
            {
              "choices": [
                {
                  "finish_reason": "stop",
                  "index": 0,
                  "logprobs": null,
                  "message": {
                    "content": "The logo says \".NET\", which is a software development framework created by Microsoft. It is used for building and running applications on Windows, macOS, and Linux environments. The logo typically also represents the broader .NET ecosystem, which includes various programming languages, libraries, and tools.",
                    "refusal": null,
                    "role": "assistant"
                  }
                }
              ],
              "created": 1743531271,
              "id": "chatcmpl-BHaQ3nkeSDGhLzLya3mGbB1EXSqve",
              "model": "gpt-4o-mini-2024-07-18",
              "object": "chat.completion",
              "system_fingerprint": "fp_b705f0c291",
              "usage": {
                "completion_tokens": 56,
                "completion_tokens_details": {
                  "accepted_prediction_tokens": 0,
                  "audio_tokens": 0,
                  "reasoning_tokens": 0,
                  "rejected_prediction_tokens": 0
                },
                "prompt_tokens": 8513,
                "prompt_tokens_details": {
                  "audio_tokens": 0,
                  "cached_tokens": 0
                },
                "total_tokens": 8569
              }
            }
            """;

        using VerbatimHttpHandler handler = new(input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync(
            [
            new(ChatRole.User,
                [
                    new TextContent("What does this logo say?"),
                    new DataContent(ImageDataUri.GetImageDataUri(), "image/png")
                    {
                        AdditionalProperties = new()
                        {
                            { "detail", detailValue }
                        }
                    }
                ])
            ]);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-BHaQ3nkeSDGhLzLya3mGbB1EXSqve", response.ResponseId);
        Assert.Equal("The logo says \".NET\", which is a software development framework created by Microsoft. It is used for building and running applications on Windows, macOS, and Linux environments. The logo typically also represents the broader .NET ecosystem, which includes various programming languages, libraries, and tools.", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("chatcmpl-BHaQ3nkeSDGhLzLya3mGbB1EXSqve", response.Messages.Single().MessageId);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_743_531_271), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(8513, response.Usage.InputTokenCount);
        Assert.Equal(56, response.Usage.OutputTokenCount);
        Assert.Equal(8569, response.Usage.TotalTokenCount);
        Assert.Equal(new Dictionary<string, long>
        {
            { "InputTokenDetails.AudioTokenCount", 0 },
            { "InputTokenDetails.CachedTokenCount", 0 },
            { "OutputTokenDetails.ReasoningTokenCount", 0 },
            { "OutputTokenDetails.AudioTokenCount", 0 },
            { "OutputTokenDetails.AcceptedPredictionTokenCount", 0 },
            { "OutputTokenDetails.RejectedPredictionTokenCount", 0 },
        }, response.Usage.AdditionalCounts);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_b705f0c291", response.AdditionalProperties[nameof(ChatCompletion.SystemFingerprint)]);
    }

    private static IChatClient CreateChatClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .GetChatClient(modelId)
        .AsIChatClient();
}
