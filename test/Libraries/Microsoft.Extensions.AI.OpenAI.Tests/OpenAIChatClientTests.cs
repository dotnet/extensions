// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => new OpenAIChatClient(null!, "model"));
        Assert.Throws<ArgumentNullException>("chatClient", () => new OpenAIChatClient(null!));

        OpenAIClient openAIClient = new("key");
        Assert.Throws<ArgumentNullException>("modelId", () => new OpenAIChatClient(openAIClient, null!));
        Assert.Throws<ArgumentException>("modelId", () => new OpenAIChatClient(openAIClient, ""));
        Assert.Throws<ArgumentException>("modelId", () => new OpenAIChatClient(openAIClient, "   "));
    }

    [Fact]
    public void ToolCallJsonSerializerOptions_HasExpectedValue()
    {
        using OpenAIChatClient client = new(new("key"), "model");

        Assert.Same(client.ToolCallJsonSerializerOptions, AIJsonUtilities.DefaultOptions);
        Assert.Throws<ArgumentNullException>("value", () => client.ToolCallJsonSerializerOptions = null!);

        JsonSerializerOptions options = new();
        client.ToolCallJsonSerializerOptions = options;
        Assert.Same(options, client.ToolCallJsonSerializerOptions);
    }

    [Fact]
    public void AsChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => ((OpenAIClient)null!).AsChatClient("model"));
        Assert.Throws<ArgumentNullException>("chatClient", () => ((ChatClient)null!).AsChatClient());

        OpenAIClient client = new("key");
        Assert.Throws<ArgumentNullException>("modelId", () => client.AsChatClient(null!));
        Assert.Throws<ArgumentException>("modelId", () => client.AsChatClient("   "));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsChatClient_OpenAIClient_ProducesExpectedMetadata(bool useAzureOpenAI)
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var client = useAzureOpenAI ?
            new AzureOpenAIClient(endpoint, new ApiKeyCredential("key")) :
            new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IChatClient chatClient = client.AsChatClient(model);
        Assert.Equal("openai", chatClient.Metadata.ProviderName);
        Assert.Equal(endpoint, chatClient.Metadata.ProviderUri);
        Assert.Equal(model, chatClient.Metadata.ModelId);

        chatClient = client.GetChatClient(model).AsChatClient();
        Assert.Equal("openai", chatClient.Metadata.ProviderName);
        Assert.Equal(endpoint, chatClient.Metadata.ProviderUri);
        Assert.Equal(model, chatClient.Metadata.ModelId);
    }

    [Fact]
    public void GetService_OpenAIClient_SuccessfullyReturnsUnderlyingClient()
    {
        OpenAIClient openAIClient = new(new ApiKeyCredential("key"));
        IChatClient chatClient = openAIClient.AsChatClient("model");

        Assert.Same(chatClient, chatClient.GetService<IChatClient>());
        Assert.Same(chatClient, chatClient.GetService<OpenAIChatClient>());

        Assert.Same(openAIClient, chatClient.GetService<OpenAIClient>());

        Assert.NotNull(chatClient.GetService<ChatClient>());

        using IChatClient pipeline = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation()
            .UseOpenTelemetry()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .Build();

        Assert.NotNull(pipeline.GetService<FunctionInvokingChatClient>());
        Assert.NotNull(pipeline.GetService<DistributedCachingChatClient>());
        Assert.NotNull(pipeline.GetService<CachingChatClient>());
        Assert.NotNull(pipeline.GetService<OpenTelemetryChatClient>());

        Assert.Same(openAIClient, pipeline.GetService<OpenAIClient>());
        Assert.IsType<FunctionInvokingChatClient>(pipeline.GetService<IChatClient>());
    }

    [Fact]
    public void GetService_ChatClient_SuccessfullyReturnsUnderlyingClient()
    {
        ChatClient openAIClient = new OpenAIClient(new ApiKeyCredential("key")).GetChatClient("model");
        IChatClient chatClient = openAIClient.AsChatClient();

        Assert.Same(chatClient, chatClient.GetService<IChatClient>());
        Assert.Same(openAIClient, chatClient.GetService<ChatClient>());

        using IChatClient pipeline = new ChatClientBuilder(chatClient)
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
            {"messages":[{"role":"user","content":"hello"}],"model":"gpt-4o-mini","max_completion_tokens":10,"temperature":0.5}
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
                  "cached_tokens": 0
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 0
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.CompleteAsync("hello", new()
        {
            MaxOutputTokens = 10,
            Temperature = 0.5f,
        });
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI", response.CompletionId);
        Assert.Equal("Hello! How can I assist you today?", response.Message.Text);
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_888_631), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(8, response.Usage.InputTokenCount);
        Assert.Equal(9, response.Usage.OutputTokenCount);
        Assert.Equal(17, response.Usage.TotalTokenCount);
        Assert.NotNull(response.Usage.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task BasicRequestResponse_Streaming()
    {
        const string Input = """
            {"messages":[{"role":"user","content":"hello"}],"model":"gpt-4o-mini","max_completion_tokens":20,"stream":true,"stream_options":{"include_usage":true},"temperature":0.5}
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

            data: {"id":"chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK","object":"chat.completion.chunk","created":1727889370,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[],"usage":{"prompt_tokens":8,"completion_tokens":9,"total_tokens":17,"prompt_tokens_details":{"cached_tokens":0},"completion_tokens_details":{"reasoning_tokens":0}}}

            data: [DONE]

            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<StreamingChatCompletionUpdate> updates = [];
        await foreach (var update in client.CompleteStreamingAsync("hello", new()
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
            Assert.Equal("chatcmpl-ADxFKtX6xIwdWRN42QvBj2u1RZpCK", updates[i].CompletionId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("gpt-4o-mini-2024-07-18", updates[i].ModelId);
            Assert.Equal(ChatRole.Assistant, updates[i].Role);
            Assert.NotNull(updates[i].AdditionalProperties);
            Assert.Equal("fp_f85bea6784", updates[i].AdditionalProperties![nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
            Assert.Equal(i == 10 ? 0 : 1, updates[i].Contents.Count);
            Assert.Equal(i < 10 ? null : ChatFinishReason.Stop, updates[i].FinishReason);
        }

        UsageContent usage = updates.SelectMany(u => u.Contents).OfType<UsageContent>().Single();
        Assert.Equal(8, usage.Details.InputTokenCount);
        Assert.Equal(9, usage.Details.OutputTokenCount);
        Assert.Equal(17, usage.Details.TotalTokenCount);
        Assert.NotNull(usage.Details.AdditionalProperties);
        Assert.Equal(new Dictionary<string, object> { [nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)] = 0 }, usage.Details.AdditionalProperties[nameof(ChatTokenUsage.OutputTokenDetails)]);
    }

    [Fact]
    public async Task MultipleMessages_NonStreaming()
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
                        "content": "hi, how are you?"
                    },
                    {
                        "role": "user",
                        "content": "i\u0027m good. how are you?"
                    }
                ],
                "model": "gpt-4o-mini",
                "frequency_penalty": 0.75,
                "presence_penalty": 0.5,
                "seed":42,
                "stop": [
                    "great"
                ],
                "temperature": 0.25
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
                  "cached_tokens": 0
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 0
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

        var response = await client.CompleteAsync(messages, new()
        {
            Temperature = 0.25f,
            FrequencyPenalty = 0.75f,
            PresencePenalty = 0.5f,
            StopSequences = ["great"],
            Seed = 42,
        });
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.CompletionId);
        Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Message.Text);
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.NotNull(response.Usage.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
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
                  "cached_tokens": 0
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 0
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

        var response = await client.CompleteAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.CompletionId);
        Assert.Equal("Hi! It's so good to hear from you!", response.Message.Text);
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.NotNull(response.Usage.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
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
                  "cached_tokens": 0
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 0
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

        var response = await client.CompleteAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.CompletionId);
        Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Message.Text);
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.NotNull(response.Usage.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task FunctionCallContent_NonStreaming()
    {
        const string Input = """
            {
                "messages": [
                    {
                        "role": "user",
                        "content": "How old is Alice?"
                    }
                ],
                "model": "gpt-4o-mini",
                "tools": [
                    {
                        "type": "function",
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
                                }
                            }
                        }
                    }
                ],
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
                  "cached_tokens": 0
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 0
                }
              },
              "system_fingerprint": "fp_f85bea6784"
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.CompleteAsync("How old is Alice?", new()
        {
            Tools = [AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.")],
        });
        Assert.NotNull(response);

        Assert.Null(response.Message.Text);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_702), response.CreatedAt);
        Assert.Equal(ChatFinishReason.ToolCalls, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(61, response.Usage.InputTokenCount);
        Assert.Equal(16, response.Usage.OutputTokenCount);
        Assert.Equal(77, response.Usage.TotalTokenCount);

        Assert.Single(response.Choices);
        Assert.Single(response.Message.Contents);
        FunctionCallContent fcc = Assert.IsType<FunctionCallContent>(response.Message.Contents[0]);
        Assert.Equal("GetPersonAge", fcc.Name);
        AssertExtensions.EqualFunctionCallParameters(new Dictionary<string, object?> { ["personName"] = "Alice" }, fcc.Arguments);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
    }

    [Fact]
    public async Task FunctionCallContent_Streaming()
    {
        const string Input = """
            {
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
                "tools": [
                    {
                        "type": "function",
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
                                }
                            }
                        }
                    }
                ],
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

            data: {"id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","object":"chat.completion.chunk","created":1727895263,"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","choices":[],"usage":{"prompt_tokens":61,"completion_tokens":16,"total_tokens":77,"prompt_tokens_details":{"cached_tokens":0},"completion_tokens_details":{"reasoning_tokens":0}}}

            data: [DONE]

            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        List<StreamingChatCompletionUpdate> updates = [];
        await foreach (var update in client.CompleteStreamingAsync("How old is Alice?", new()
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
            Assert.Equal("chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl", updates[i].CompletionId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("gpt-4o-mini-2024-07-18", updates[i].ModelId);
            Assert.Equal(ChatRole.Assistant, updates[i].Role);
            Assert.NotNull(updates[i].AdditionalProperties);
            Assert.Equal("fp_f85bea6784", updates[i].AdditionalProperties![nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
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
        Assert.NotNull(usage.Details.AdditionalProperties);
        Assert.Equal(new Dictionary<string, object> { [nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)] = 0 }, usage.Details.AdditionalProperties[nameof(ChatTokenUsage.OutputTokenDetails)]);
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
                                "type": "function",
                                "function": {
                                    "name": "SayHello",
                                    "arguments": "null"
                                }
                            },
                            {
                                "id": "12346",
                                "type": "function",
                                "function": {
                                    "name": "SayHi",
                                    "arguments": "null"
                                }
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
                  "cached_tokens": 0
                },
                "completion_tokens_details": {
                  "reasoning_tokens": 0
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
                new FunctionResultContent("12345", "SayHello", "Said hello"),
                new FunctionResultContent("12346", "SayHi", "Said hi"),
            ]),
            new(ChatRole.Assistant, "You are great."),
            new(ChatRole.User, "Thanks!"),
        ];

        var response = await client.CompleteAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.CompletionId);
        Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Message.Text);
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
        Assert.NotNull(response.Usage.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
    }

    private static IChatClient CreateChatClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .AsChatClient(modelId);
}
