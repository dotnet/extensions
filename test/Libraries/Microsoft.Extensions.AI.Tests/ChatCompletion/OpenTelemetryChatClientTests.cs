// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryChatClientTests
{
    [Fact]
    public async Task ExpectedInformationLogged_NonStreaming_Async()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestChatClient
        {
            Metadata = new("testservice", new Uri("http://localhost:12345/something"), "amazingmodel"),
            CompleteAsyncCallback = async (messages, options, cancellationToken) =>
            {
                await Task.Yield();
                return new ChatCompletion([new ChatMessage(ChatRole.Assistant, "blue whale")])
                {
                    CompletionId = "id123",
                    FinishReason = ChatFinishReason.Stop,
                    Usage = new UsageDetails
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 42,
                    },
                };
            }
        };

        var chatClient = new ChatClientBuilder()
            .UseOpenTelemetry(sourceName, instance =>
            {
                instance.EnableSensitiveData = true;
                instance.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Use(innerClient);

        await chatClient.CompleteAsync(
            [new(ChatRole.User, "What's the biggest animal?")],
            new ChatOptions
            {
                FrequencyPenalty = 3.0f,
                MaxOutputTokens = 123,
                ModelId = "replacementmodel",
                TopP = 4.0f,
                PresencePenalty = 5.0f,
                ResponseFormat = ChatResponseFormat.Json,
                Temperature = 6.0f,
                StopSequences = ["hello", "world"],
                AdditionalProperties = new() { ["top_k"] = 7.0f },
            });

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("http://localhost:12345/something", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("chat.completions replacementmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.system"));

        Assert.Equal("replacementmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(3.0f, activity.GetTagItem("gen_ai.request.frequency_penalty"));
        Assert.Equal(4.0f, activity.GetTagItem("gen_ai.request.top_p"));
        Assert.Equal(5.0f, activity.GetTagItem("gen_ai.request.presence_penalty"));
        Assert.Equal(6.0f, activity.GetTagItem("gen_ai.request.temperature"));
        Assert.Equal(7.0, activity.GetTagItem("gen_ai.request.top_k"));
        Assert.Equal(123, activity.GetTagItem("gen_ai.request.max_tokens"));
        Assert.Equal("""["hello", "world"]""", activity.GetTagItem("gen_ai.request.stop_sequences"));

        Assert.Equal("id123", activity.GetTagItem("gen_ai.response.id"));
        Assert.Equal("""["stop"]""", activity.GetTagItem("gen_ai.response.finish_reasons"));
        Assert.Equal(10, activity.GetTagItem("gen_ai.response.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.response.output_tokens"));

        Assert.Collection(activity.Events,
            evt =>
            {
                Assert.Equal("gen_ai.content.prompt", evt.Name);
                Assert.Equal("""[{"role": "user", "content": "What\u0027s the biggest animal?"}]""", evt.Tags.FirstOrDefault(t => t.Key == "gen_ai.prompt").Value);
            },
            evt =>
            {
                Assert.Equal("gen_ai.content.completion", evt.Name);
                Assert.Contains("whale", (string)evt.Tags.FirstOrDefault(t => t.Key == "gen_ai.completion").Value!);
            });

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task ExpectedInformationLogged_Streaming_Async()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        async static IAsyncEnumerable<StreamingChatCompletionUpdate> CallbackAsync(
            IList<ChatMessage> messages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new StreamingChatCompletionUpdate
            {
                Role = ChatRole.Assistant,
                Text = "blue ",
                CompletionId = "id123",
            };
            await Task.Yield();
            yield return new StreamingChatCompletionUpdate
            {
                Role = ChatRole.Assistant,
                Text = "whale",
                FinishReason = ChatFinishReason.Stop,
            };
            yield return new StreamingChatCompletionUpdate
            {
                Contents = [new UsageContent(new()
                {
                    InputTokenCount = 10,
                    OutputTokenCount = 20,
                    TotalTokenCount = 42,
                })],
            };
        }

        using var innerClient = new TestChatClient
        {
            Metadata = new("testservice", new Uri("http://localhost:12345/something"), "amazingmodel"),
            CompleteStreamingAsyncCallback = CallbackAsync,
        };

        var chatClient = new ChatClientBuilder()
            .UseOpenTelemetry(sourceName, instance =>
            {
                instance.EnableSensitiveData = true;
                instance.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Use(innerClient);

        await foreach (var update in chatClient.CompleteStreamingAsync(
            [new(ChatRole.User, "What's the biggest animal?")],
            new ChatOptions
            {
                FrequencyPenalty = 3.0f,
                MaxOutputTokens = 123,
                ModelId = "replacementmodel",
                TopP = 4.0f,
                PresencePenalty = 5.0f,
                ResponseFormat = ChatResponseFormat.Json,
                Temperature = 6.0f,
                StopSequences = ["hello", "world"],
                AdditionalProperties = new() { ["top_k"] = 7.0 },
            }))
        {
            // Drain the stream.
        }

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("http://localhost:12345/something", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("chat.completions replacementmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.system"));

        Assert.Equal("replacementmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(3.0f, activity.GetTagItem("gen_ai.request.frequency_penalty"));
        Assert.Equal(4.0f, activity.GetTagItem("gen_ai.request.top_p"));
        Assert.Equal(5.0f, activity.GetTagItem("gen_ai.request.presence_penalty"));
        Assert.Equal(6.0f, activity.GetTagItem("gen_ai.request.temperature"));
        Assert.Equal(7.0, activity.GetTagItem("gen_ai.request.top_k"));
        Assert.Equal(123, activity.GetTagItem("gen_ai.request.max_tokens"));
        Assert.Equal("""["hello", "world"]""", activity.GetTagItem("gen_ai.request.stop_sequences"));

        Assert.Equal("id123", activity.GetTagItem("gen_ai.response.id"));
        Assert.Equal("""["stop"]""", activity.GetTagItem("gen_ai.response.finish_reasons"));
        Assert.Equal(10, activity.GetTagItem("gen_ai.response.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.response.output_tokens"));

        Assert.Collection(activity.Events,
            evt =>
            {
                Assert.Equal("gen_ai.content.prompt", evt.Name);
                Assert.Equal("""[{"role": "user", "content": "What\u0027s the biggest animal?"}]""", evt.Tags.FirstOrDefault(t => t.Key == "gen_ai.prompt").Value);
            },
            evt =>
            {
                Assert.Equal("gen_ai.content.completion", evt.Name);
                Assert.Contains("whale", (string)evt.Tags.FirstOrDefault(t => t.Key == "gen_ai.completion").Value!);
            });

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }
}
