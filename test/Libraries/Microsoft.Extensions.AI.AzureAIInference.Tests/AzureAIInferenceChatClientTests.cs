﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Inference;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

#pragma warning disable S103 // Lines should not be too long
#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

public class AzureAIInferenceChatClientTests
{
    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("chatCompletionsClient", () => new AzureAIInferenceChatClient(null!, "model"));

        ChatCompletionsClient client = new(new("http://somewhere"), new AzureKeyCredential("key"));
        Assert.Throws<ArgumentException>("modelId", () => new AzureAIInferenceChatClient(client, "   "));
    }

    [Fact]
    public void ToolCallJsonSerializerOptions_HasExpectedValue()
    {
        using AzureAIInferenceChatClient client = new(new(new("http://somewhere"), new AzureKeyCredential("key")), "mode");

        Assert.Same(client.ToolCallJsonSerializerOptions, AIJsonUtilities.DefaultOptions);
        Assert.Throws<ArgumentNullException>("value", () => client.ToolCallJsonSerializerOptions = null!);

        JsonSerializerOptions options = new();
        client.ToolCallJsonSerializerOptions = options;
        Assert.Same(options, client.ToolCallJsonSerializerOptions);
    }

    [Fact]
    public void AsChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("chatCompletionsClient", () => ((ChatCompletionsClient)null!).AsChatClient("model"));

        ChatCompletionsClient client = new(new("http://somewhere"), new AzureKeyCredential("key"));
        Assert.Throws<ArgumentException>("modelId", () => client.AsChatClient("   "));
    }

    [Fact]
    public void AsChatClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        ChatCompletionsClient client = new(endpoint, new AzureKeyCredential("key"));

        IChatClient chatClient = client.AsChatClient(model);
        var metadata = chatClient.GetService<ChatClientMetadata>();
        Assert.Equal("az.ai.inference", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.ModelId);
    }

    [Fact]
    public void GetService_SuccessfullyReturnsUnderlyingClient()
    {
        ChatCompletionsClient client = new(new("http://localhost"), new AzureKeyCredential("key"));
        IChatClient chatClient = client.AsChatClient("model");

        Assert.Same(chatClient, chatClient.GetService<IChatClient>());
        Assert.Same(chatClient, chatClient.GetService<AzureAIInferenceChatClient>());

        Assert.Same(client, chatClient.GetService<ChatCompletionsClient>());

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
        Assert.NotNull(pipeline.GetService<object>());

        Assert.Same(client, pipeline.GetService<ChatCompletionsClient>());
        Assert.IsType<FunctionInvokingChatClient>(pipeline.GetService<IChatClient>());

        Assert.Null(pipeline.GetService<ChatCompletionsClient>("key"));
        Assert.Null(pipeline.GetService<AzureAIInferenceChatClient>("key"));
        Assert.Null(pipeline.GetService<string>("key"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BasicRequestResponse_NonStreaming(bool multiContent)
    {
        const string Input = """
            {
                "messages": [{"role":"user", "content":"hello"}],
                "max_tokens":10,
                "temperature":0.5,
                "model":"gpt-4o-mini"
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

        List<ChatMessage> messages = multiContent ?
            [new ChatMessage(ChatRole.User, "hello".Select(c => (AIContent)new TextContent(c.ToString())).ToList())] :
            [new ChatMessage(ChatRole.User, "hello")];

        var response = await client.GetResponseAsync(messages, new()
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
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BasicRequestResponse_Streaming(bool multiContent)
    {
        const string Input = """
            {
                "messages": [{"role":"user", "content":"hello"}],
                "max_tokens":20,
                "temperature":0.5,
                "stream":true,
                "model":"gpt-4o-mini"}
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

        List<ChatMessage> messages = multiContent ?
            [new ChatMessage(ChatRole.User, "hello".Select(c => (AIContent)new TextContent(c.ToString())).ToList())] :
            [new ChatMessage(ChatRole.User, "hello")];

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync(messages, new()
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
            Assert.Equal(i is < 10 or 11 ? 1 : 0, updates[i].Contents.Count);
            Assert.Equal(i < 10 ? null : ChatFinishReason.Stop, updates[i].FinishReason);
        }
    }

    [Fact]
    public async Task AdditionalOptions_NonStreaming()
    {
        const string Input = """
            {
                "messages":[{"role":"user", "content":"hello"}],
                "max_tokens":10,
                "temperature":0.5,
                "top_p":0.5,
                "stop":["yes","no"],
                "presence_penalty":0.5,
                "frequency_penalty":0.75,
                "seed":42,
                "model":"gpt-4o-mini",
                "top_k":40,
                "something_else":"value1",
                "and_something_further":123
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "object": "chat.completion",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?"
                  }
                }
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        Assert.NotNull(await client.GetResponseAsync("hello", new()
        {
            MaxOutputTokens = 10,
            Temperature = 0.5f,
            TopP = 0.5f,
            TopK = 40,
            FrequencyPenalty = 0.75f,
            PresencePenalty = 0.5f,
            Seed = 42,
            StopSequences = ["yes", "no"],
            AdditionalProperties = new()
            {
                ["something_else"] = "value1",
                ["and_something_further"] = 123,
            },
        }));
    }

    [Fact]
    public async Task ResponseFormat_Text_NonStreaming()
    {
        const string Input = """
            {
                "messages":[{"role":"user", "content":"hello"}],
                "model":"gpt-4o-mini",
                "response_format":{"type":"text"}
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "object": "chat.completion",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?"
                  }
                }
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        Assert.NotNull(await client.GetResponseAsync("hello", new()
        {
            ResponseFormat = ChatResponseFormat.Text,
        }));
    }

    [Fact]
    public async Task ResponseFormat_Json_NonStreaming()
    {
        const string Input = """
            {
                "messages":[{"role":"user", "content":"hello"}],
                "model":"gpt-4o-mini",
                "response_format":{"type":"json_object"}
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "object": "chat.completion",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?"
                  }
                }
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        Assert.NotNull(await client.GetResponseAsync("hello", new()
        {
            ResponseFormat = ChatResponseFormat.Json,
        }));
    }

    [Fact]
    public async Task ResponseFormat_JsonSchema_NonStreaming()
    {
        const string Input = """
            {
                "messages":[{"role":"user", "content":"hello"}],
                "model":"gpt-4o-mini",
                "response_format":
                {
                    "type":"json_schema",
                    "json_schema":
                    {
                        "name": "DescribedObject",
                        "schema":
                        {
                            "type":"object",
                            "properties":
                            {
                                "description":
                                {
                                    "type":"string"
                                }
                            },
                            "required":["description"],
                            "additionalProperties":false
                         },
                         "description":"An object with a description",
                         "strict":true
                    }
                }
            }
            """;

        const string Output = """
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "object": "chat.completion",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?"
                  }
                }
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        Assert.NotNull(await client.GetResponseAsync("hello", new()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(JsonSerializer.Deserialize<JsonElement>("""
                {
                  "type": "object",
                  "properties": {
                    "description": {
                      "type": "string"
                    }
                  },
                  "required": ["description"]
                }
                """), "DescribedObject", "An object with a description"),
        }));
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
                    },
                    {
                        "role": "assistant",
                        "content": "",
                        "tool_calls": [{"id":"abcd123","type":"function","function":{"name":"GetMood","arguments":"null"}}]
                    },
                    {
                        "role": "tool",
                        "content": "happy",
                        "tool_call_id": "abcd123"
                    }
                ],
                "temperature": 0.25,
                "stop": [
                    "great"
                ],
                "presence_penalty": 0.5,
                "frequency_penalty": 0.75,
                "seed": 42,
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
            new(ChatRole.Assistant, "hi, how are you?"),
            new(ChatRole.User, "i'm good. how are you?"),
            new(ChatRole.Assistant, [new FunctionCallContent("abcd123", "GetMood")]),
            new(ChatRole.Tool, [new FunctionResultContent("abcd123", "happy")]),
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
    }

    [Fact]
    public async Task MultipleContent_NonStreaming()
    {
        const string Input = """
            {
                "messages":
                [
                    {
                        "role": "user",
                        "content":
                        [
                            {
                                "type": "text",
                                "text": "Describe this picture."
                            },
                            {
                                "type": "image_url",
                                "image_url":
                                {
                                    "url": "http://dot.net/someimage.png"
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
              "id": "chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P",
              "object": "chat.completion",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "A picture of a dog."
                  }
                }
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        Assert.NotNull(await client.GetResponseAsync([new(ChatRole.User,
        [
            new TextContent("Describe this picture."),
            new UriContent("http://dot.net/someimage.png", mediaType: "image/*"),
        ])]));
    }

    [Fact]
    public async Task NullAssistantText_ContentEmpty_NonStreaming()
    {
        const string Input = """
            {
                "messages": [
                    {
                        "role": "assistant",
                        "content": ""
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
                    "content": "Hello.",
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
            new(ChatRole.Assistant, (string?)null),
            new(ChatRole.User, "hello!"),
        ];

        var response = await client.GetResponseAsync(messages);
        Assert.NotNull(response);

        Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.ResponseId);
        Assert.Equal("Hello.", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.InputTokenCount);
        Assert.Equal(15, response.Usage.OutputTokenCount);
        Assert.Equal(57, response.Usage.TotalTokenCount);
    }

    public static IEnumerable<object[]> FunctionCallContent_NonStreaming_MemberData()
    {
        yield return [ChatToolMode.Auto];
        yield return [ChatToolMode.None];
        yield return [ChatToolMode.RequireAny];
        yield return [ChatToolMode.RequireSpecific("GetPersonAge")];
    }

    [Theory]
    [MemberData(nameof(FunctionCallContent_NonStreaming_MemberData))]
    public async Task FunctionCallContent_NonStreaming(ChatToolMode mode)
    {
        string input = $$"""
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
                            "name": "GetPersonAge",
                            "description": "Gets the age of the specified person.",
                            "parameters": {
                                "type": "object",
                                "required": ["personName"],
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
                "tool_choice": {{(
                    mode is NoneChatToolMode ? "\"none\"" :
                    mode is AutoChatToolMode ? "\"auto\"" :
                    mode is RequiredChatToolMode { RequiredFunctionName: not null } f ? "{\"type\":\"function\",\"function\":{\"name\":\"GetPersonAge\"}}" :
                    "\"required\""
                    )}}
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

        using VerbatimHttpHandler handler = new(input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync("How old is Alice?", new()
        {
            Tools = [AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.")],
            ToolMode = mode,
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

        Assert.Single(response.Messages.Single().Contents);
        FunctionCallContent fcc = Assert.IsType<FunctionCallContent>(response.Messages.Single().Contents[0]);
        Assert.Equal("GetPersonAge", fcc.Name);
        AssertExtensions.EqualFunctionCallParameters(new Dictionary<string, object?> { ["personName"] = "Alice" }, fcc.Arguments);
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
                "stream": true,
                "model": "gpt-4o-mini",
                "tools": [
                    {
                        "type": "function",
                        "function": {
                            "name": "GetPersonAge",
                            "description": "Gets the age of the specified person.",
                            "parameters": {
                                "type": "object",
                                "required": ["personName"],
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
            Assert.Equal(i < 7 ? null : ChatFinishReason.ToolCalls, updates[i].FinishReason);
        }

        FunctionCallContent fcc = Assert.IsType<FunctionCallContent>(Assert.Single(updates[updates.Count - 1].Contents));
        Assert.Equal("call_F9ZaqPWo69u0urxAhVt8meDW", fcc.CallId);
        Assert.Equal("GetPersonAge", fcc.Name);
        AssertExtensions.EqualFunctionCallParameters(new Dictionary<string, object?> { ["personName"] = "Alice" }, fcc.Arguments);
    }

    private static IChatClient CreateChatClient(HttpClient httpClient, string modelId) =>
        new ChatCompletionsClient(
            new("http://somewhere"),
            new AzureKeyCredential("key"),
            new AzureAIInferenceClientOptions { Transport = new HttpClientTransport(httpClient) })
            .AsChatClient(modelId);
}
