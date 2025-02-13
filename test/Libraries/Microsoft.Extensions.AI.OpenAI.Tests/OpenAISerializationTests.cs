// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public static partial class OpenAISerializationTests
{
    [Fact]
    public static async Task RequestDeserialization_SimpleMessage()
    {
        const string RequestJson = """
            {"messages":[{"role":"user","content":"hello"}],"model":"gpt-4o-mini","max_completion_tokens":10,"temperature":0.5}
            """;

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(RequestJson));
        OpenAIChatCompletionRequest request = await OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream);

        Assert.NotNull(request);
        Assert.False(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Equal(0.5f, request.Options.Temperature);
        Assert.Equal(10, request.Options.MaxOutputTokens);
        Assert.Null(request.Options.TopK);
        Assert.Null(request.Options.TopP);
        Assert.Null(request.Options.StopSequences);
        Assert.Null(request.Options.AdditionalProperties);
        Assert.Null(request.Options.Tools);

        ChatMessage message = Assert.Single(request.Messages);
        Assert.Equal(ChatRole.User, message.Role);
        AIContent content = Assert.Single(message.Contents);
        TextContent textContent = Assert.IsType<TextContent>(content);
        Assert.Equal("hello", textContent.Text);
        Assert.Null(textContent.RawRepresentation);
        Assert.Null(textContent.AdditionalProperties);
    }

    [Fact]
    public static async Task RequestDeserialization_SimpleMessage_Stream()
    {
        const string RequestJson = """
            {"messages":[{"role":"user","content":"hello"}],"model":"gpt-4o-mini","max_completion_tokens":20,"stream":true,"stream_options":{"include_usage":true},"temperature":0.5}
            """;

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(RequestJson));
        OpenAIChatCompletionRequest request = await OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream);

        Assert.NotNull(request);
        Assert.True(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Equal(0.5f, request.Options.Temperature);
        Assert.Equal(20, request.Options.MaxOutputTokens);
        Assert.Null(request.Options.TopK);
        Assert.Null(request.Options.TopP);
        Assert.Null(request.Options.StopSequences);
        Assert.Null(request.Options.AdditionalProperties);
        Assert.Null(request.Options.Tools);

        ChatMessage message = Assert.Single(request.Messages);
        Assert.Equal(ChatRole.User, message.Role);
        AIContent content = Assert.Single(message.Contents);
        TextContent textContent = Assert.IsType<TextContent>(content);
        Assert.Equal("hello", textContent.Text);
        Assert.Null(textContent.RawRepresentation);
        Assert.Null(textContent.AdditionalProperties);
    }

    [Fact]
    public static void RequestDeserialization_SimpleMessage_JsonSerializer()
    {
        const string RequestJson = """
            {"messages":[{"role":"user","content":"hello"}],"model":"gpt-4o-mini","max_completion_tokens":20,"stream":true,"stream_options":{"include_usage":true},"temperature":0.5}
            """;

        OpenAIChatCompletionRequest? request = JsonSerializer.Deserialize<OpenAIChatCompletionRequest>(RequestJson);

        Assert.NotNull(request);
        Assert.True(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Equal(0.5f, request.Options.Temperature);
        Assert.Equal(20, request.Options.MaxOutputTokens);
        Assert.Null(request.Options.TopK);
        Assert.Null(request.Options.TopP);
        Assert.Null(request.Options.StopSequences);
        Assert.Null(request.Options.AdditionalProperties);
        Assert.Null(request.Options.Tools);

        ChatMessage message = Assert.Single(request.Messages);
        Assert.Equal(ChatRole.User, message.Role);
        AIContent content = Assert.Single(message.Contents);
        TextContent textContent = Assert.IsType<TextContent>(content);
        Assert.Equal("hello", textContent.Text);
        Assert.Null(textContent.RawRepresentation);
        Assert.Null(textContent.AdditionalProperties);
    }

    [Fact]
    public static async Task RequestDeserialization_MultipleMessages()
    {
        const string RequestJson = """
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
                "stop": [ "great" ],
                "temperature": 0.25,
                "user": "user",
                "logprobs": true,
                "logit_bias": { "42" : 0 },
                "parallel_tool_calls": true,
                "top_logprobs": 42,
                "metadata": { "key": "value" },
                "store": true
            }
            """;

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(RequestJson));
        OpenAIChatCompletionRequest request = await OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream);

        Assert.NotNull(request);
        Assert.False(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Equal(0.25f, request.Options.Temperature);
        Assert.Equal(0.75f, request.Options.FrequencyPenalty);
        Assert.Equal(0.5f, request.Options.PresencePenalty);
        Assert.Equal(42, request.Options.Seed);
        Assert.Equal(["great"], request.Options.StopSequences);
        Assert.NotNull(request.Options.AdditionalProperties);
        Assert.Equal("user", request.Options.AdditionalProperties["EndUserId"]);
        Assert.True((bool)request.Options.AdditionalProperties["IncludeLogProbabilities"]!);
        Assert.Single((IDictionary<int, int>)request.Options.AdditionalProperties["LogitBiases"]!);
        Assert.True((bool)request.Options.AdditionalProperties["AllowParallelToolCalls"]!);
        Assert.Equal(42, request.Options.AdditionalProperties["TopLogProbabilityCount"]!);
        Assert.Single((IDictionary<string, string>)request.Options.AdditionalProperties["Metadata"]!);
        Assert.True((bool)request.Options.AdditionalProperties["StoredOutputEnabled"]!);

        Assert.Collection(request.Messages,
            msg =>
            {
                Assert.Equal(ChatRole.System, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                TextContent text = Assert.IsType<TextContent>(Assert.Single(msg.Contents));
                Assert.Equal("You are a really nice friend.", text.Text);
                Assert.Null(text.AdditionalProperties);
                Assert.Null(text.RawRepresentation);
                Assert.Null(text.AdditionalProperties);
            },
            msg =>
            {
                Assert.Equal(ChatRole.User, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                TextContent text = Assert.IsType<TextContent>(Assert.Single(msg.Contents));
                Assert.Equal("hello!", text.Text);
                Assert.Null(text.AdditionalProperties);
                Assert.Null(text.RawRepresentation);
                Assert.Null(text.AdditionalProperties);
            },
            msg =>
            {
                Assert.Equal(ChatRole.Assistant, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                TextContent text = Assert.IsType<TextContent>(Assert.Single(msg.Contents));
                Assert.Equal("hi, how are you?", text.Text);
                Assert.Null(text.AdditionalProperties);
                Assert.Null(text.RawRepresentation);
                Assert.Null(text.AdditionalProperties);
            },
            msg =>
            {
                Assert.Equal(ChatRole.User, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                TextContent text = Assert.IsType<TextContent>(Assert.Single(msg.Contents));
                Assert.Equal("i'm good. how are you?", text.Text);
                Assert.Null(text.AdditionalProperties);
                Assert.Null(text.RawRepresentation);
                Assert.Null(text.AdditionalProperties);
            });
    }

    [Fact]
    public static async Task RequestDeserialization_MultiPartSystemMessage()
    {
        const string RequestJson = """
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

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(RequestJson));
        OpenAIChatCompletionRequest request = await OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream);

        Assert.NotNull(request);
        Assert.False(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Null(request.Options.Temperature);
        Assert.Null(request.Options.FrequencyPenalty);
        Assert.Null(request.Options.PresencePenalty);
        Assert.Null(request.Options.Seed);
        Assert.Null(request.Options.StopSequences);

        Assert.Collection(request.Messages,
            msg =>
            {
                Assert.Equal(ChatRole.System, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                Assert.Collection(msg.Contents,
                    content =>
                    {
                        TextContent text = Assert.IsType<TextContent>(content);
                        Assert.Equal("You are a really nice friend.", text.Text);
                        Assert.Null(text.AdditionalProperties);
                        Assert.Null(text.RawRepresentation);
                        Assert.Null(text.AdditionalProperties);
                    },
                    content =>
                    {
                        TextContent text = Assert.IsType<TextContent>(content);
                        Assert.Equal("Really nice.", text.Text);
                        Assert.Null(text.AdditionalProperties);
                        Assert.Null(text.RawRepresentation);
                        Assert.Null(text.AdditionalProperties);
                    });
            },
            msg =>
            {
                Assert.Equal(ChatRole.User, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                TextContent text = Assert.IsType<TextContent>(Assert.Single(msg.Contents));
                Assert.Equal("hello!", text.Text);
                Assert.Null(text.AdditionalProperties);
                Assert.Null(text.RawRepresentation);
                Assert.Null(text.AdditionalProperties);
            });
    }

    [Fact]
    public static async Task RequestDeserialization_ToolCall()
    {
        const string RequestJson = """
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
                            "strict": true,
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

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(RequestJson));
        OpenAIChatCompletionRequest request = await OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream);

        Assert.NotNull(request);
        Assert.False(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Null(request.Options.Temperature);
        Assert.Null(request.Options.FrequencyPenalty);
        Assert.Null(request.Options.PresencePenalty);
        Assert.Null(request.Options.Seed);
        Assert.Null(request.Options.StopSequences);

        Assert.Same(ChatToolMode.Auto, request.Options.ToolMode);
        Assert.NotNull(request.Options.Tools);

        AIFunction function = Assert.IsAssignableFrom<AIFunction>(Assert.Single(request.Options.Tools));
        Assert.Equal("Gets the age of the specified person.", function.Description);
        Assert.Equal("GetPersonAge", function.Name);
        Assert.Equal("Strict", Assert.Single(function.AdditionalProperties).Key);

        Assert.Null(function.UnderlyingMethod);

        JsonObject parametersSchema = Assert.IsType<JsonObject>(JsonNode.Parse(function.JsonSchema.GetProperty("properties").GetRawText()));
        var parameterSchema = Assert.IsType<JsonObject>(Assert.Single(parametersSchema.Select(kvp => kvp.Value)));
        Assert.Equal(2, parameterSchema.Count);
        Assert.Equal("The person whose age is being requested", (string)parameterSchema["description"]!);
        Assert.Equal("string", (string)parameterSchema["type"]!);

        Dictionary<string, object?> functionArgs = new() { ["personName"] = "John" };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => function.InvokeAsync(functionArgs));
        Assert.Contains("does not support being invoked.", ex.Message);
    }

    [Fact]
    public static async Task RequestDeserialization_ToolChatMessage()
    {
        const string RequestJson = """
            {
                "messages": [
                    {
                        "role": "assistant",
                        "tool_calls": [
                            {
                                "id": "12345",
                                "type": "function",
                                "function": {
                                    "name": "SayHello",
                                    "arguments": "null"
                                }
                            }
                        ]
                    },
                    {
                        "role": "tool",
                        "tool_call_id": "12345",
                        "content": "42"
                    }
                ],
                "model": "gpt-4o-mini"
            }
            """;

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(RequestJson));
        OpenAIChatCompletionRequest request = await OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream);

        Assert.NotNull(request);
        Assert.False(request.Stream);
        Assert.Equal("gpt-4o-mini", request.ModelId);

        Assert.NotNull(request.Options);
        Assert.Equal("gpt-4o-mini", request.Options.ModelId);
        Assert.Null(request.Options.Temperature);
        Assert.Null(request.Options.FrequencyPenalty);
        Assert.Null(request.Options.PresencePenalty);
        Assert.Null(request.Options.Seed);
        Assert.Null(request.Options.StopSequences);

        Assert.Collection(request.Messages,
            msg =>
            {
                Assert.Equal(ChatRole.Assistant, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                FunctionCallContent text = Assert.IsType<FunctionCallContent>(Assert.Single(msg.Contents));
                Assert.Equal("12345", text.CallId);
                Assert.Null(text.AdditionalProperties);
                Assert.IsType<OpenAI.Chat.ChatToolCall>(text.RawRepresentation);
                Assert.Null(text.AdditionalProperties);
            },
            msg =>
            {
                Assert.Equal(ChatRole.Tool, msg.Role);
                Assert.Null(msg.RawRepresentation);
                Assert.Null(msg.AdditionalProperties);

                FunctionResultContent frc = Assert.IsType<FunctionResultContent>(Assert.Single(msg.Contents));
                Assert.Equal("12345", frc.CallId);
                Assert.Equal(42, Assert.IsType<JsonElement>(frc.Result).GetInt32());
                Assert.Null(frc.AdditionalProperties);
                Assert.Null(frc.RawRepresentation);
                Assert.Null(frc.AdditionalProperties);
            });
    }

    [Fact]
    public static async Task SerializeResponse_SingleChoice()
    {
        ChatMessage message = new()
        {
            Role = ChatRole.Assistant,
            Contents = [
                new TextContent("Hello! How can I assist you today?"),
                new FunctionCallContent(
                    "callId",
                    "MyCoolFunc",
                    new Dictionary<string, object?>
                    {
                        ["arg1"] = 42,
                        ["arg2"] = "str",
                    })
            ]
        };

        ChatResponse response = new(message)
        {
            ResponseId = "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
            ModelId = "gpt-4o-mini-2024-07-18",
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1_727_888_631),
            FinishReason = ChatFinishReason.Stop,
            Usage = new()
            {
                InputTokenCount = 8,
                OutputTokenCount = 9,
                TotalTokenCount = 17,
                AdditionalCounts = new()
                {
                    { "InputTokenDetails.AudioTokenCount", 1 },
                    { "InputTokenDetails.CachedTokenCount", 13 },
                    { "OutputTokenDetails.AudioTokenCount", 2 },
                    { "OutputTokenDetails.ReasoningTokenCount", 90 },
                }
            },
            AdditionalProperties = new()
            {
                [nameof(ChatCompletion.SystemFingerprint)] = "fp_f85bea6784",
            }
        };

        using MemoryStream stream = new();
        await OpenAISerializationHelpers.SerializeAsync(stream, response);
        string result = Encoding.UTF8.GetString(stream.ToArray());

        AssertJsonEqual("""
            {
              "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
              "model": "gpt-4o-mini-2024-07-18",
              "system_fingerprint": "fp_f85bea6784",
              "usage": {
                "completion_tokens": 9,
                "prompt_tokens": 8,
                "total_tokens": 17,
                "completion_tokens_details": {
                  "reasoning_tokens": 90,
                  "audio_tokens": 2,
                  "accepted_prediction_tokens": 0,
                  "rejected_prediction_tokens": 0
                },
                "prompt_tokens_details": {
                  "audio_tokens": 1,
                  "cached_tokens": 13
                }
              },
              "object": "chat.completion",
              "choices": [
                {
                  "finish_reason": "stop",
                  "index": 0,
                  "message": {
                    "refusal": null,
                    "tool_calls": [
                      {
                        "id": "callId",
                        "function": {
                          "name": "MyCoolFunc",
                          "arguments": "{\r\n  \u0022arg1\u0022: 42,\r\n  \u0022arg2\u0022: \u0022str\u0022\r\n}"
                        },
                        "type": "function"
                      }
                    ],
                    "role": "assistant",
                    "content": "Hello! How can I assist you today?"
                  },
                  "logprobs": {
                    "content": [],
                    "refusal": []
                  }
                }
              ],
              "created": 1727888631
            }
            """, result);
    }

    [Fact]
    public static async Task SerializeResponse_ManyChoices_ThrowsNotSupportedException()
    {
        ChatMessage message1 = new()
        {
            Role = ChatRole.Assistant,
            Text = "Hello! How can I assist you today?",
        };

        ChatMessage message2 = new()
        {
            Role = ChatRole.Assistant,
            Text = "Hey there! How can I help?",
        };

        ChatResponse response = new([message1, message2]);

        using MemoryStream stream = new();
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => OpenAISerializationHelpers.SerializeAsync(stream, response));
        Assert.Contains("multiple choices", ex.Message);
    }

    [Fact]
    public static async Task SerializeStreamingResponse()
    {
        static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingResponse()
        {
            for (int i = 0; i < 5; i++)
            {
                List<AIContent> contents = [new TextContent($"Streaming update {i}")];

                if (i == 2)
                {
                    FunctionCallContent fcc = new(
                        "callId",
                        "MyCoolFunc",
                        new Dictionary<string, object?>
                        {
                            ["arg1"] = 42,
                            ["arg2"] = "str",
                        });

                    contents.Add(fcc);
                }

                if (i == 4)
                {
                    UsageDetails usageDetails = new()
                    {
                        InputTokenCount = 8,
                        OutputTokenCount = 9,
                        TotalTokenCount = 17,
                        AdditionalCounts = new()
                        {
                            { "InputTokenDetails.AudioTokenCount", 1 },
                            { "InputTokenDetails.CachedTokenCount", 13 },
                            { "OutputTokenDetails.AudioTokenCount", 2 },
                            { "OutputTokenDetails.ReasoningTokenCount", 90 },
                        }
                    };

                    contents.Add(new UsageContent(usageDetails));
                }

                yield return new ChatResponseUpdate
                {
                    ResponseId = "chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl",
                    ModelId = "gpt-4o-mini-2024-07-18",
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1_727_888_631),
                    Role = ChatRole.Assistant,
                    Contents = contents,
                    FinishReason = i == 4 ? ChatFinishReason.Stop : null,
                    AdditionalProperties = new()
                    {
                        [nameof(ChatCompletion.SystemFingerprint)] = "fp_f85bea6784",
                    },
                };

                await Task.Yield();
            }
        }

        using MemoryStream stream = new();
        await OpenAISerializationHelpers.SerializeStreamingAsync(stream, CreateStreamingResponse());
        string result = Encoding.UTF8.GetString(stream.ToArray());

        AssertSseEqual("""
            data: {"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","object":"chat.completion.chunk","id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","choices":[{"delta":{"tool_calls":[],"role":"assistant","content":"Streaming update 0"},"logprobs":{"content":[],"refusal":[]},"index":0}],"created":1727888631}

            data: {"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","object":"chat.completion.chunk","id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","choices":[{"delta":{"tool_calls":[],"role":"assistant","content":"Streaming update 1"},"logprobs":{"content":[],"refusal":[]},"index":0}],"created":1727888631}

            data: {"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","object":"chat.completion.chunk","id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","choices":[{"delta":{"tool_calls":[{"index":0,"function":{"name":"MyCoolFunc","arguments":"{\r\n  \u0022arg1\u0022: 42,\r\n  \u0022arg2\u0022: \u0022str\u0022\r\n}"},"type":"function","id":"callId"}],"role":"assistant","content":"Streaming update 2"},"logprobs":{"content":[],"refusal":[]},"index":0}],"created":1727888631}

            data: {"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","object":"chat.completion.chunk","id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","choices":[{"delta":{"tool_calls":[],"role":"assistant","content":"Streaming update 3"},"logprobs":{"content":[],"refusal":[]},"index":0}],"created":1727888631}

            data: {"model":"gpt-4o-mini-2024-07-18","system_fingerprint":"fp_f85bea6784","object":"chat.completion.chunk","id":"chatcmpl-ADymNiWWeqCJqHNFXiI1QtRcLuXcl","choices":[{"delta":{"tool_calls":[],"role":"assistant","content":"Streaming update 4"},"logprobs":{"content":[],"refusal":[]},"finish_reason":"stop","index":0}],"created":1727888631,"usage":{"completion_tokens":9,"prompt_tokens":8,"total_tokens":17,"completion_tokens_details":{"reasoning_tokens":90,"audio_tokens":2,"accepted_prediction_tokens":0,"rejected_prediction_tokens":0},"prompt_tokens_details":{"audio_tokens":1,"cached_tokens":13}}}

            data: [DONE]


            """, result);
    }

    [Fact]
    public static async Task SerializationHelpers_NullArguments_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() => OpenAISerializationHelpers.SerializeAsync(null!, new(new ChatMessage())));
        await Assert.ThrowsAsync<ArgumentNullException>(() => OpenAISerializationHelpers.SerializeAsync(new MemoryStream(), null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() => OpenAISerializationHelpers.SerializeStreamingAsync(null!, GetStreamingChatResponse()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => OpenAISerializationHelpers.SerializeStreamingAsync(new MemoryStream(), null!));

        static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponse()
        {
            yield return new ChatResponseUpdate();
            await Task.CompletedTask;
        }
    }

    [Fact]
    public static async Task SerializationHelpers_HonorCancellationToken()
    {
        CancellationToken canceledToken = new(canceled: true);
        MemoryStream stream = new("{}"u8.ToArray());

        await Assert.ThrowsAsync<TaskCanceledException>(() => OpenAISerializationHelpers.DeserializeChatCompletionRequestAsync(stream, cancellationToken: canceledToken));
        await Assert.ThrowsAsync<TaskCanceledException>(() => OpenAISerializationHelpers.SerializeAsync(stream, new(new ChatMessage()), cancellationToken: canceledToken));
        await Assert.ThrowsAsync<TaskCanceledException>(() => OpenAISerializationHelpers.SerializeStreamingAsync(stream, GetStreamingChatResponse(), cancellationToken: canceledToken));

        static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponse()
        {
            yield return new ChatResponseUpdate();
            await Task.CompletedTask;
        }
    }

    [Fact]
    public static async Task SerializationHelpers_HonorJsonSerializerOptions()
    {
        FunctionCallContent fcc = new(
            "callId",
            "MyCoolFunc",
            new Dictionary<string, object?>
            {
                ["arg1"] = new SomeFunctionArgument(),
            });

        ChatResponse response = new(new ChatMessage
        {
            Role = ChatRole.Assistant,
            Contents = [fcc],
        });

        using MemoryStream stream = new();

        // Passing a JSO that contains a contract for the function argument results in successful serialization.
        await OpenAISerializationHelpers.SerializeAsync(stream, response, options: JsonContextWithFunctionArgument.Default.Options);
        stream.Position = 0;

        await OpenAISerializationHelpers.SerializeStreamingAsync(stream, GetStreamingResponse(), options: JsonContextWithFunctionArgument.Default.Options);
        stream.Position = 0;

        // Passing a JSO without a contract for the function argument result in failed serialization.
        await Assert.ThrowsAsync<NotSupportedException>(() => OpenAISerializationHelpers.SerializeAsync(stream, response, options: JsonContextWithoutFunctionArgument.Default.Options));
        await Assert.ThrowsAsync<NotSupportedException>(() => OpenAISerializationHelpers.SerializeStreamingAsync(stream, GetStreamingResponse(), options: JsonContextWithoutFunctionArgument.Default.Options));

        async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponse()
        {
            await Task.Yield();
            yield return new ChatResponseUpdate
            {
                Contents = [fcc],
            };
        }
    }

    private class SomeFunctionArgument;

    [JsonSerializable(typeof(SomeFunctionArgument))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    private partial class JsonContextWithFunctionArgument : JsonSerializerContext;

    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    private partial class JsonContextWithoutFunctionArgument : JsonSerializerContext;

    private static void AssertJsonEqual(string expected, string actual)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(actual);

        if (!JsonNode.DeepEquals(expectedNode, actualNode))
        {
            // JSON documents are not equal, assert on
            // normal form strings for better reporting.
            expected = expectedNode?.ToJsonString() ?? "null";
            actual = actualNode?.ToJsonString() ?? "null";
            Assert.Equal(expected.NormalizeNewLines(), actual.NormalizeNewLines());
        }
    }

    private static void AssertSseEqual(string expected, string actual)
    {
        Assert.Equal(expected.NormalizeNewLines(), actual.NormalizeNewLines());
    }

    private static string NormalizeNewLines(this string value) =>
        value.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
}
