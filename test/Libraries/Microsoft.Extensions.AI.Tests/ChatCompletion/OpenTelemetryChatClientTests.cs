﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
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

        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)));

        using var innerClient = new TestChatClient
        {
            Metadata = new("testservice", new Uri("http://localhost:12345/something"), "amazingmodel"),
            CompleteAsyncCallback = async (messages, options, cancellationToken) =>
            {
                await Task.Yield();
                return new ChatCompletion([new ChatMessage(ChatRole.Assistant, "The blue whale, I think.")])
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
            },
            CompleteStreamingAsyncCallback = CallbackAsync,
        };

        async static IAsyncEnumerable<StreamingChatCompletionUpdate> CallbackAsync(
            IList<ChatMessage> messages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            foreach (string text in new[] { "The ", "blue ", "whale,", " ", "", "I", " think." })
            {
                await Task.Yield();
                yield return new StreamingChatCompletionUpdate
                {
                    Role = ChatRole.Assistant,
                    Text = text,
                    CompletionId = "id123",
                };
            }

            yield return new StreamingChatCompletionUpdate
            {
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

        var chatClient = new ChatClientBuilder(innerClient)
            .UseOpenTelemetry(loggerFactory, sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
                instance.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Build();

        List<ChatMessage> chatMessages =
        [
            new(ChatRole.System, "You are a close friend."),
            new(ChatRole.User, "Hey!"),
            new(ChatRole.Assistant, [new FunctionCallContent("12345", "GetPersonName")]),
            new(ChatRole.Tool, [new FunctionResultContent("12345", "GetPersonName", "John")]),
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
            StopSequences = ["hello", "world"],
        };

        if (streaming)
        {
            await foreach (var update in chatClient.CompleteStreamingAsync(chatMessages, options))
            {
                await Task.Yield();
            }
        }
        else
        {
            await chatClient.CompleteAsync(chatMessages, options);
        }

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("http://localhost:12345/something", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("chat replacementmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.system"));

        Assert.Equal("replacementmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(3.0f, activity.GetTagItem("gen_ai.request.frequency_penalty"));
        Assert.Equal(4.0f, activity.GetTagItem("gen_ai.request.top_p"));
        Assert.Equal(5.0f, activity.GetTagItem("gen_ai.request.presence_penalty"));
        Assert.Equal(6.0f, activity.GetTagItem("gen_ai.request.temperature"));
        Assert.Equal(7, activity.GetTagItem("gen_ai.request.top_k"));
        Assert.Equal(123, activity.GetTagItem("gen_ai.request.max_tokens"));
        Assert.Equal("""["hello", "world"]""", activity.GetTagItem("gen_ai.request.stop_sequences"));

        Assert.Equal("id123", activity.GetTagItem("gen_ai.response.id"));
        Assert.Equal("""["stop"]""", activity.GetTagItem("gen_ai.response.finish_reasons"));
        Assert.Equal(10, activity.GetTagItem("gen_ai.response.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.response.output_tokens"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);

        var logs = collector.GetSnapshot();
        if (enableSensitiveData)
        {
            Assert.Collection(logs,
                log => Assert.Equal("""{"content":"You are a close friend."}""", log.Message),
                log => Assert.Equal("""{"content":"Hey!"}""", log.Message),
                log => Assert.Equal("""{"tool_calls":[{"id":"12345","type":"function","function":{"name":"GetPersonName"}}]}""", log.Message),
                log => Assert.Equal("""{"id":"12345","content":"John"}""", log.Message),
                log => Assert.Equal("""{"content":"Hey John, what\u0027s up?"}""", log.Message),
                log => Assert.Equal("""{"content":"What\u0027s the biggest animal?"}""", log.Message),
                log => Assert.Equal("""{"finish_reason":"stop","index":0,"message":{"content":"The blue whale, I think."}}""", log.Message));
        }
        else
        {
            Assert.Collection(logs,
                log => Assert.Equal("""{}""", log.Message),
                log => Assert.Equal("""{}""", log.Message),
                log => Assert.Equal("""{"tool_calls":[{"id":"12345","type":"function","function":{"name":"GetPersonName"}}]}""", log.Message),
                log => Assert.Equal("""{"id":"12345"}""", log.Message),
                log => Assert.Equal("""{}""", log.Message),
                log => Assert.Equal("""{}""", log.Message),
                log => Assert.Equal("""{"finish_reason":"stop","index":0,"message":{}}""", log.Message));
        }

        Assert.Collection(logs,
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.system.message"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]),
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.user.message"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]),
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.assistant.message"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]),
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.tool.message"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]),
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.assistant.message"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]),
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.user.message"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]),
            log => Assert.Equal(new KeyValuePair<string, string?>("event.name", "gen_ai.choice"), ((IList<KeyValuePair<string, string?>>)log.State!)[0]));

        Assert.All(logs, log =>
        {
            Assert.Equal(new KeyValuePair<string, string?>("gen_ai.system", "testservice"), ((IList<KeyValuePair<string, string?>>)log.State!)[1]);
        });
    }
}
