// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryChatClientTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExpectedInformationLogged_Async(bool enableSensitiveData, bool streaming)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = async (messages, options, cancellationToken) =>
            {
                await Task.Yield();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "The blue whale, I think."))
                {
                    ResponseId = "id123",
                    FinishReason = ChatFinishReason.Stop,
                    Usage = new UsageDetails
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 42,
                    },
                    AdditionalProperties = new()
                    {
                        ["system_fingerprint"] = "abcdefgh",
                        ["AndSomethingElse"] = "value2",
                    },
                };
            },
            GetStreamingResponseAsyncCallback = CallbackAsync,
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel") :
                null,
        };

        async static IAsyncEnumerable<ChatResponseUpdate> CallbackAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            foreach (string text in new[] { "The ", "blue ", "whale,", " ", "", "I", " think." })
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, text)
                {
                    ResponseId = "id123",
                };
            }

            yield return new ChatResponseUpdate
            {
                FinishReason = ChatFinishReason.Stop,
            };

            yield return new ChatResponseUpdate
            {
                Contents = [new UsageContent(new()
                {
                    InputTokenCount = 10,
                    OutputTokenCount = 20,
                    TotalTokenCount = 42,
                })],
                AdditionalProperties = new()
                {
                    ["system_fingerprint"] = "abcdefgh",
                    ["AndSomethingElse"] = "value2",
                },
            };
        }

        using var chatClient = innerClient
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
                instance.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Build();

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a close friend."),
            new(ChatRole.User, "Hey!"),
            new(ChatRole.Assistant, [new FunctionCallContent("12345", "GetPersonName")]),
            new(ChatRole.Tool, [new FunctionResultContent("12345", "John")]),
            new(ChatRole.Assistant, "Hey John, what's up?"),
            new(ChatRole.User, "What's the biggest animal?")
        ];

        var options = new ChatOptions
        {
            FrequencyPenalty = 3.0f,
            MaxOutputTokens = 123,
            ModelId = "replacementmodel",
            TopP = 4.0f,
            TopK = 7,
            PresencePenalty = 5.0f,
            ResponseFormat = ChatResponseFormat.Json,
            Temperature = 6.0f,
            Seed = 42,
            StopSequences = ["hello", "world"],
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
            Instructions = "You are helpful.",
            Tools =
            [
                AIFunctionFactory.Create((string personName) => personName, "GetPersonAge", "Gets the age of a person by name."),
                new HostedWebSearchTool(),
                new HostedFileSearchTool(),
                new HostedCodeInterpreterTool(),
                new HostedMcpServerTool("myAwesomeServer", "http://localhost:1234/somewhere"),
                AIFunctionFactory.Create((string location) => "", "GetCurrentWeather", "Gets the current weather for a location.").AsDeclarationOnly(),
            ],
        };

        if (streaming)
        {
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options))
            {
                await Task.Yield();
            }
        }
        else
        {
            await chatClient.GetResponseAsync(messages, options);
        }

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("chat replacementmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal("replacementmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(3.0f, activity.GetTagItem("gen_ai.request.frequency_penalty"));
        Assert.Equal(4.0f, activity.GetTagItem("gen_ai.request.top_p"));
        Assert.Equal(5.0f, activity.GetTagItem("gen_ai.request.presence_penalty"));
        Assert.Equal(6.0f, activity.GetTagItem("gen_ai.request.temperature"));
        Assert.Equal(7, activity.GetTagItem("gen_ai.request.top_k"));
        Assert.Equal(123, activity.GetTagItem("gen_ai.request.max_tokens"));
        Assert.Equal("""["hello", "world"]""", activity.GetTagItem("gen_ai.request.stop_sequences"));
        Assert.Equal(enableSensitiveData ? "value1" : null, activity.GetTagItem("service_tier"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("SomethingElse"));
        Assert.Equal(42L, activity.GetTagItem("gen_ai.request.seed"));

        Assert.Equal("id123", activity.GetTagItem("gen_ai.response.id"));
        Assert.Equal("""["stop"]""", activity.GetTagItem("gen_ai.response.finish_reasons"));
        Assert.Equal(10, activity.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.usage.output_tokens"));
        Assert.Equal(enableSensitiveData ? "abcdefgh" : null, activity.GetTagItem("system_fingerprint"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("AndSomethingElse"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);

        var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (enableSensitiveData)
        {
            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "role": "system",
                    "parts": [
                      {
                        "type": "text",
                        "content": "You are a close friend."
                      }
                    ]
                  },
                  {
                    "role": "user",
                    "parts": [
                      {
                        "type": "text",
                        "content": "Hey!"
                      }
                    ]
                  },
                  {
                    "role": "assistant",
                    "parts": [
                      {
                        "type": "tool_call",
                        "id": "12345",
                        "name": "GetPersonName"
                      }
                    ]
                  },
                  {
                    "role": "tool",
                    "parts": [
                      {
                        "type": "tool_call_response",
                        "id": "12345",
                        "response": "John"
                      }
                    ]
                  },
                  {
                    "role": "assistant",
                    "parts": [
                      {
                        "type": "text",
                        "content": "Hey John, what's up?"
                      }
                    ]
                  },
                  {
                    "role": "user",
                    "parts": [
                      {
                        "type": "text",
                        "content": "What's the biggest animal?"
                      }
                    ]
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.input.messages"]));

            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "role": "assistant",
                    "parts": [
                      {
                        "type": "text",
                        "content": "The blue whale, I think."
                      }
                    ],
                    "finish_reason": "stop"
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.output.messages"]));

            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                      "type": "text",
                      "content": "You are helpful."
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.system_instructions"]));

            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "type": "function",
                    "name": "GetPersonAge",
                    "description": "Gets the age of a person by name.",
                    "parameters": {
                      "type": "object",
                      "properties": {
                        "personName": {
                          "type": "string"
                        }
                      },
                      "required": [
                        "personName"
                      ]
                    }
                  },
                  {
                    "type": "web_search"
                  },
                  {
                    "type": "file_search"
                  },
                  {
                    "type": "code_interpreter"
                  },
                  {
                    "type": "mcp"
                  },
                  {
                    "type": "function",
                    "name": "GetCurrentWeather",
                    "description": "Gets the current weather for a location.",
                    "parameters": {
                      "type": "object",
                      "properties": {
                        "location": {
                          "type": "string"
                        }
                      },
                      "required": [
                        "location"
                      ]
                    }
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.tool.definitions"]));
        }
        else
        {
            Assert.False(tags.ContainsKey("gen_ai.input.messages"));
            Assert.False(tags.ContainsKey("gen_ai.output.messages"));
            Assert.False(tags.ContainsKey("gen_ai.system_instructions"));
            Assert.False(tags.ContainsKey("gen_ai.tool.definitions"));
        }

        static string ReplaceWhitespace(string? input) => Regex.Replace(input ?? "", @"\s+", " ").Trim();
    }
}
