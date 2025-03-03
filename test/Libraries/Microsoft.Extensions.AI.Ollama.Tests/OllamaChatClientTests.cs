// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OllamaChatClientTests
{
    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("endpoint", () => new OllamaChatClient((Uri)null!));
        Assert.Throws<ArgumentException>("modelId", () => new OllamaChatClient("http://localhost", "   "));
    }

    [Fact]
    public void ToolCallJsonSerializerOptions_HasExpectedValue()
    {
        using OllamaChatClient client = new("http://localhost", "model");

        Assert.Same(client.ToolCallJsonSerializerOptions, AIJsonUtilities.DefaultOptions);
        Assert.Throws<ArgumentNullException>("value", () => client.ToolCallJsonSerializerOptions = null!);

        JsonSerializerOptions options = new();
        client.ToolCallJsonSerializerOptions = options;
        Assert.Same(options, client.ToolCallJsonSerializerOptions);
    }

    [Fact]
    public void GetService_SuccessfullyReturnsUnderlyingClient()
    {
        using OllamaChatClient client = new("http://localhost");

        Assert.Same(client, client.GetService<OllamaChatClient>());
        Assert.Same(client, client.GetService<IChatClient>());

        using IChatClient pipeline = client
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .Build();

        Assert.NotNull(pipeline.GetService<FunctionInvokingChatClient>());
        Assert.NotNull(pipeline.GetService<DistributedCachingChatClient>());
        Assert.NotNull(pipeline.GetService<CachingChatClient>());
        Assert.NotNull(pipeline.GetService<OpenTelemetryChatClient>());

        Assert.Same(client, pipeline.GetService<OllamaChatClient>());
        Assert.IsType<FunctionInvokingChatClient>(pipeline.GetService<IChatClient>());
    }

    [Fact]
    public void AsChatClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        using IChatClient chatClient = new OllamaChatClient(endpoint, model);
        var metadata = chatClient.GetService<ChatClientMetadata>();
        Assert.Equal("ollama", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.ModelId);
    }

    [Fact]
    public async Task BasicRequestResponse_NonStreaming()
    {
        const string Input = """
            {
                "model":"llama3.1",
                "messages":[{"role":"user","content":"hello"}],
                "stream":false,
                "options":{"num_predict":10,"temperature":0.5}
            }
            """;

        const string Output = """
            {
                "model": "llama3.1",
                "created_at": "2024-10-01T15:46:10.5248793Z",
                "message": {
                    "role": "assistant",
                    "content": "Hello! How are you today? Is there something"
                },
                "done_reason": "length",
                "done": true,
                "total_duration": 22186844400,
                "load_duration": 17947219100,
                "prompt_eval_count": 11,
                "prompt_eval_duration": 1953805000,
                "eval_count": 10,
                "eval_duration": 2277274000
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using OllamaChatClient client = new("http://localhost:11434", "llama3.1", httpClient);
        var response = await client.GetResponseAsync("hello", new()
        {
            MaxOutputTokens = 10,
            Temperature = 0.5f,
        });
        Assert.NotNull(response);

        Assert.Equal("Hello! How are you today? Is there something", response.Message.Text);
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("llama3.1", response.ModelId);
        Assert.Equal(DateTimeOffset.Parse("2024-10-01T15:46:10.5248793Z"), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Length, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(11, response.Usage.InputTokenCount);
        Assert.Equal(10, response.Usage.OutputTokenCount);
        Assert.Equal(21, response.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task BasicRequestResponse_Streaming()
    {
        const string Input = """
            {
                "model":"llama3.1",
                "messages":[{"role":"user","content":"hello"}],
                "stream":true,
                "options":{"num_predict":20,"temperature":0.5}
            }
            """;

        const string Output = """
            {"model":"llama3.1","created_at":"2024-10-01T16:15:20.4965315Z","message":{"role":"assistant","content":"Hello"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:20.763058Z","message":{"role":"assistant","content":"!"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:20.9751134Z","message":{"role":"assistant","content":" How"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:21.1788125Z","message":{"role":"assistant","content":" are"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:21.3883171Z","message":{"role":"assistant","content":" you"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:21.5912498Z","message":{"role":"assistant","content":" today"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:21.7968039Z","message":{"role":"assistant","content":"?"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:22.0034152Z","message":{"role":"assistant","content":" Is"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:22.1931196Z","message":{"role":"assistant","content":" there"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:22.3827484Z","message":{"role":"assistant","content":" something"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:22.5659027Z","message":{"role":"assistant","content":" I"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:22.7488871Z","message":{"role":"assistant","content":" can"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:22.9339881Z","message":{"role":"assistant","content":" help"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:23.1201564Z","message":{"role":"assistant","content":" you"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:23.303447Z","message":{"role":"assistant","content":" with"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:23.4964909Z","message":{"role":"assistant","content":" or"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:23.6837816Z","message":{"role":"assistant","content":" would"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:23.8723142Z","message":{"role":"assistant","content":" you"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:24.064613Z","message":{"role":"assistant","content":" like"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:24.2504498Z","message":{"role":"assistant","content":" to"},"done":false}
            {"model":"llama3.1","created_at":"2024-10-01T16:15:24.2514508Z","message":{"role":"assistant","content":""},"done_reason":"length", "done":true,"total_duration":11912402900,"load_duration":6824559200,"prompt_eval_count":11,"prompt_eval_duration":1329601000,"eval_count":20,"eval_duration":3754262000}
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = new OllamaChatClient("http://localhost:11434", "llama3.1", httpClient);

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync("hello", new()
        {
            MaxOutputTokens = 20,
            Temperature = 0.5f,
        }))
        {
            updates.Add(update);
        }

        Assert.Equal(21, updates.Count);

        DateTimeOffset[] createdAts = Regex.Matches(Output, @"2024.*?Z").Cast<Match>().Select(m => DateTimeOffset.Parse(m.Value)).ToArray();

        for (int i = 0; i < updates.Count; i++)
        {
            Assert.NotNull(updates[i].ResponseId);
            Assert.Equal(i < updates.Count - 1 ? 1 : 2, updates[i].Contents.Count);
            Assert.Equal(ChatRole.Assistant, updates[i].Role);
            Assert.Equal("llama3.1", updates[i].ModelId);
            Assert.Equal(createdAts[i], updates[i].CreatedAt);
            Assert.Equal(i < updates.Count - 1 ? null : ChatFinishReason.Length, updates[i].FinishReason);
        }

        Assert.Equal("Hello! How are you today? Is there something I can help you with or would you like to", string.Concat(updates.Select(u => u.Text)));
        Assert.Equal(2, updates[updates.Count - 1].Contents.Count);
        Assert.IsType<TextContent>(updates[updates.Count - 1].Contents[0]);
        UsageContent usage = Assert.IsType<UsageContent>(updates[updates.Count - 1].Contents[1]);
        Assert.Equal(11, usage.Details.InputTokenCount);
        Assert.Equal(20, usage.Details.OutputTokenCount);
        Assert.Equal(31, usage.Details.TotalTokenCount);
    }

    [Fact]
    public async Task MultipleMessages_NonStreaming()
    {
        const string Input = """
            {
                "model": "llama3.1",
                "messages": [
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
                "stream": false,
                "options": {
                    "frequency_penalty": 0.75,
                    "presence_penalty": 0.5,
                    "seed": 42,
                    "stop": ["great"],
                    "temperature": 0.25
                }
            }
            """;

        const string Output = """
            {
              "model": "llama3.1",
              "created_at": "2024-10-01T17:18:46.308987Z",
              "message": {
                "role": "assistant",
                "content": "I'm just a computer program, so I don't have feelings or emotions like humans do, but I'm functioning properly and ready to help with any questions or tasks you may have! How about we chat about something in particular or just shoot the breeze? Your choice!"
              },
              "done_reason": "stop",
              "done": true,
              "total_duration": 23229369000,
              "load_duration": 7724086300,
              "prompt_eval_count": 36,
              "prompt_eval_duration": 4245660000,
              "eval_count": 55,
              "eval_duration": 11256470000
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = new OllamaChatClient("http://localhost:11434", httpClient: httpClient);

        List<ChatMessage> messages =
        [
            new(ChatRole.User, "hello!"),
            new(ChatRole.Assistant, "hi, how are you?"),
            new(ChatRole.User, "i'm good. how are you?"),
        ];

        var response = await client.GetResponseAsync(messages, new()
        {
            ModelId = "llama3.1",
            Temperature = 0.25f,
            FrequencyPenalty = 0.75f,
            PresencePenalty = 0.5f,
            StopSequences = ["great"],
            Seed = 42,
        });
        Assert.NotNull(response);

        Assert.Equal(
            VerbatimHttpHandler.RemoveWhiteSpace("""
                I'm just a computer program, so I don't have feelings or emotions like humans do,
                but I'm functioning properly and ready to help with any questions or tasks you may have!
                How about we chat about something in particular or just shoot the breeze ? Your choice!
                """),
            VerbatimHttpHandler.RemoveWhiteSpace(response.Message.Text));
        Assert.Single(response.Message.Contents);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("llama3.1", response.ModelId);
        Assert.Equal(DateTimeOffset.Parse("2024-10-01T17:18:46.308987Z"), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(36, response.Usage.InputTokenCount);
        Assert.Equal(55, response.Usage.OutputTokenCount);
        Assert.Equal(91, response.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task FunctionCallContent_NonStreaming()
    {
        const string Input = """
            {
                "model": "llama3.1",
                "messages": [
                    {
                        "role": "user",
                        "content": "How old is Alice?"
                    }
                ],
                "stream": false,
                "tools": [
                    {
                        "type": "function",
                        "function": {
                            "name": "GetPersonAge",
                            "description": "Gets the age of the specified person.",
                            "parameters": {
                                "type": "object",
                                "properties": {
                                    "personName": {
                                        "description": "The person whose age is being requested",
                                        "type": "string"
                                    }
                                },
                                "required": ["personName"]
                            }
                        }
                    }
                ]
            }
            """;

        const string Output = """
            {
                "model": "llama3.1",
                "created_at": "2024-10-01T18:48:30.2669578Z",
                "message": {
                    "role": "assistant",
                    "content": "",
                    "tool_calls": [
                        {
                            "function": {
                                "name": "GetPersonAge",
                                "arguments": {
                                    "personName": "Alice"
                                }
                            }
                        }
                    ]
                },
                "done_reason": "stop",
                "done": true,
                "total_duration": 27351311300,
                "load_duration": 8041538400,
                "prompt_eval_count": 170,
                "prompt_eval_duration": 16078776000,
                "eval_count": 19,
                "eval_duration": 3227962000
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using IChatClient client = new OllamaChatClient("http://localhost:11434", "llama3.1", httpClient)
        {
            ToolCallJsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        var response = await client.GetResponseAsync("How old is Alice?", new()
        {
            Tools = [AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.")],
        });
        Assert.NotNull(response);

        Assert.Null(response.Message.Text);
        Assert.Equal("llama3.1", response.ModelId);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal(DateTimeOffset.Parse("2024-10-01T18:48:30.2669578Z"), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(170, response.Usage.InputTokenCount);
        Assert.Equal(19, response.Usage.OutputTokenCount);
        Assert.Equal(189, response.Usage.TotalTokenCount);

        Assert.Single(response.Choices);
        Assert.Single(response.Message.Contents);
        FunctionCallContent fcc = Assert.IsType<FunctionCallContent>(response.Message.Contents[0]);
        Assert.Equal("GetPersonAge", fcc.Name);
        AssertExtensions.EqualFunctionCallParameters(new Dictionary<string, object?> { ["personName"] = "Alice" }, fcc.Arguments);
    }

    [Fact]
    public async Task FunctionResultContent_NonStreaming()
    {
        const string Input = """
            {
                "model": "llama3.1",
                "messages": [
                    {
                        "role": "user",
                        "content": "How old is Alice?"
                    },
                    {
                        "role": "assistant",
                        "content": "{\u0022call_id\u0022:\u0022abcd1234\u0022,\u0022name\u0022:\u0022GetPersonAge\u0022,\u0022arguments\u0022:{\u0022personName\u0022:\u0022Alice\u0022}}"
                    },
                    {
                        "role": "tool",
                        "content": "{\u0022call_id\u0022:\u0022abcd1234\u0022,\u0022result\u0022:42}"
                    }
                ],
                "stream": false,
                "tools": [
                    {
                        "type": "function",
                        "function": {
                            "name": "GetPersonAge",
                            "description": "Gets the age of the specified person.",
                            "parameters": {
                                "type": "object",
                                "properties": {
                                    "personName": {
                                        "description": "The person whose age is being requested",
                                        "type": "string"
                                    }
                                },
                                "required": ["personName"]
                            }
                        }
                    }
                ]
            }
            """;

        const string Output = """
            {
                "model": "llama3.1",
                "created_at": "2024-10-01T20:57:20.157266Z",
                "message": {
                    "role": "assistant",
                    "content": "Alice is 42 years old."
                },
                "done_reason": "stop",
                "done": true,
                "total_duration": 20320666000,
                "load_duration": 8159642600,
                "prompt_eval_count": 106,
                "prompt_eval_duration": 10846727000,
                "eval_count": 8,
                "eval_duration": 1307842000
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using IChatClient client = new OllamaChatClient("http://localhost:11434", "llama3.1", httpClient)
        {
            ToolCallJsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        var response = await client.GetResponseAsync(
            [
                new(ChatRole.User, "How old is Alice?"),
                new(ChatRole.Assistant, [new FunctionCallContent("abcd1234", "GetPersonAge", new Dictionary<string, object?> { ["personName"] = "Alice" })]),
                new(ChatRole.Tool, [new FunctionResultContent("abcd1234", 42)]),
            ],
            new()
            {
                Tools = [AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.")],
            });
        Assert.NotNull(response);

        Assert.Equal("Alice is 42 years old.", response.Message.Text);
        Assert.Equal("llama3.1", response.ModelId);
        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal(DateTimeOffset.Parse("2024-10-01T20:57:20.157266Z"), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);
        Assert.NotNull(response.Usage);
        Assert.Equal(106, response.Usage.InputTokenCount);
        Assert.Equal(8, response.Usage.OutputTokenCount);
        Assert.Equal(114, response.Usage.TotalTokenCount);
    }
}
