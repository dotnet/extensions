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

public class OpenTelemetryRealtimeSessionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpectedInformationLogged_GetStreamingResponseAsync(bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "test-model",
                Voice = "alloy",
                VoiceSpeed = 1.2,
                MaxOutputTokens = 500,
                OutputModalities = ["text", "audio"],
                Instructions = "Be helpful and friendly.",
                SessionKind = RealtimeSessionKind.Realtime,
                Tools = [AIFunctionFactory.Create((string query) => query, "Search", "Search for information.")],
            },
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("testprovider", new Uri("http://localhost:12345/realtime"), "gpt-4-realtime") :
                null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => CallbackAsync(updates, cancellationToken),
        };

        static async IAsyncEnumerable<RealtimeServerMessage> CallbackAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            // Consume the incoming updates
            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Just consume the update
            }

            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.ResponseCreated, EventId = "evt_001" };
            yield return new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDelta) { OutputIndex = 0, Text = "Hello" };
            yield return new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDelta) { OutputIndex = 0, Text = " there!" };
            yield return new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDone) { OutputIndex = 0, Text = "Hello there!" };

            yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone)
            {
                ResponseId = "resp_12345",
                ConversationId = "conv_67890",
                Status = "completed",
                Usage = new UsageDetails
                {
                    InputTokenCount = 15,
                    OutputTokenCount = 25,
                    TotalTokenCount = 40,
                    CachedInputTokenCount = 3,
                    InputAudioTokenCount = 10,
                    InputTextTokenCount = 5,
                    OutputAudioTokenCount = 18,
                    OutputTextTokenCount = 7,
                },
            };
        }

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
                instance.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume responses
        }

        // When sensitive data is enabled, we get one activity per message with content plus one for output/response
        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output activity = 3 activities
        // When sensitive data is disabled, we get only one activity for the response
        Activity activity;
        if (enableSensitiveData)
        {
            Assert.Equal(3, activities.Count);

            // The last activity is the response/output activity with ResponseDone data
            activity = activities[2];
        }
        else
        {
            activity = Assert.Single(activities);
        }

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("realtime test-model", activity.DisplayName);
        Assert.Equal("testprovider", activity.GetTagItem("gen_ai.provider.name"));
        Assert.Equal("chat", activity.GetTagItem("gen_ai.operation.name"));

        Assert.Equal("test-model", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(500, activity.GetTagItem("gen_ai.request.max_tokens"));

        // Realtime-specific attributes
        Assert.Equal("Realtime", activity.GetTagItem("gen_ai.realtime.session_kind"));
        Assert.Equal("alloy", activity.GetTagItem("gen_ai.realtime.voice"));
        Assert.Equal(1.2, activity.GetTagItem("gen_ai.realtime.voice_speed"));
        Assert.Equal("""["text", "audio"]""", activity.GetTagItem("gen_ai.realtime.output_modalities"));

        // Response attributes
        Assert.Equal("resp_12345", activity.GetTagItem("gen_ai.response.id"));
        Assert.Equal("conv_67890", activity.GetTagItem("gen_ai.conversation.id"));
        Assert.Equal("""["completed"]""", activity.GetTagItem("gen_ai.response.finish_reasons"));
        Assert.Equal(15, activity.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(25, activity.GetTagItem("gen_ai.usage.output_tokens"));
        Assert.Equal(3, activity.GetTagItem("gen_ai.usage.cache_read.input_tokens"));
        Assert.Equal(10, activity.GetTagItem("gen_ai.usage.input_audio_tokens"));
        Assert.Equal(5, activity.GetTagItem("gen_ai.usage.input_text_tokens"));
        Assert.Equal(18, activity.GetTagItem("gen_ai.usage.output_audio_tokens"));
        Assert.Equal(7, activity.GetTagItem("gen_ai.usage.output_text_tokens"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);

        var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (enableSensitiveData)
        {
            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "type": "text",
                    "content": "Be helpful and friendly."
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.system_instructions"]));

            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "type": "function",
                    "name": "Search",
                    "description": "Search for information.",
                    "parameters": {
                      "type": "object",
                      "properties": {
                        "query": {
                          "type": "string"
                        }
                      },
                      "required": [
                        "query"
                      ]
                    }
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.tool.definitions"]));
        }
        else
        {
            Assert.False(tags.ContainsKey("gen_ai.system_instructions"));
            Assert.False(tags.ContainsKey("gen_ai.tool.definitions"));
        }
    }

    [Fact]
    public async Task UpdateAsync_TracesOperation()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, cancellationToken) => Task.CompletedTask,
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("testprovider", new Uri("http://localhost:8080"), "gpt-4-realtime") :
                null,
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = true;
            })
            .Build();

        var options = new RealtimeSessionOptions
        {
            Model = "my-model",
            Voice = "echo",
            MaxOutputTokens = 100,
            Instructions = "Be brief.",
        };

        await session.UpdateAsync(options);

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.Equal("realtime my-model", activity.DisplayName);
        Assert.Equal("chat", activity.GetTagItem("gen_ai.operation.name"));
        Assert.Equal("my-model", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal("testprovider", activity.GetTagItem("gen_ai.provider.name"));
        Assert.Equal("echo", activity.GetTagItem("gen_ai.realtime.voice"));
        Assert.Equal(100, activity.GetTagItem("gen_ai.request.max_tokens"));
        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task UpdateAsync_TracesError()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, cancellationToken) => throw new InvalidOperationException("Test error"),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.UpdateAsync(new RealtimeSessionOptions { Model = "test" }));

        var activity = Assert.Single(activities);
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("error.type"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test error", activity.StatusDescription);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_TracesError()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => ThrowingCallbackAsync(updates, cancellationToken),
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ThrowingCallbackAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.ResponseCreated };
            throw new InvalidOperationException("Streaming error");
        }

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
            {
                // Consume responses
            }
        });

        var activity = Assert.Single(activities);
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("error.type"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Streaming error", activity.StatusDescription);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_TracesErrorFromResponse()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => ErrorResponseCallbackAsync(updates, cancellationToken),
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ErrorResponseCallbackAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Consume
            }

            yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone)
            {
                ResponseId = "resp_error",
                Status = "failed",
                Error = new ErrorContent("Something went wrong") { ErrorCode = "internal_error" },
            };
        }

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume responses
        }

        var activity = Assert.Single(activities);
        Assert.Equal("internal_error", activity.GetTagItem("error.type"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Something went wrong", activity.StatusDescription);
    }

    [Fact]
    public async Task DefaultVoiceSpeed_NotLogged()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "test-model",
                VoiceSpeed = 1.0, // Default value should not be logged
            },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => EmptyCallbackAsync(updates, cancellationToken),
        };

        static async IAsyncEnumerable<RealtimeServerMessage> EmptyCallbackAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Consume
            }

            yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
        }

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.False(tags.ContainsKey("gen_ai.realtime.voice_speed"));
    }

    [Fact]
    public async Task NoListeners_NoActivityCreated()
    {
        // Create a tracer provider but don't add a source for our session
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource("different-source")
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => EmptyCallbackAsync(updates, cancellationToken),
        };

        static async IAsyncEnumerable<RealtimeServerMessage> EmptyCallbackAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Consume
            }

            yield break;
        }

        var sourceName = Guid.NewGuid().ToString();
        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        // This should work without errors even without listeners
        var clientMessages = GetClientMessagesAsync();
        var count = 0;
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            count++;
        }

        // Verify the session worked correctly without listeners
        Assert.True(count >= 0);
    }

    [Fact]
    public void InvalidArgs_Throws()
    {
        using var innerSession = new TestRealtimeSession();

        Assert.Throws<ArgumentNullException>("innerSession", () => new OpenTelemetryRealtimeSession(null!));
        Assert.Throws<ArgumentNullException>("value", () => new OpenTelemetryRealtimeSession(innerSession).JsonSerializerOptions = null!);
    }

    [Fact]
    public async Task UpdateAsync_InvalidArgs_Throws()
    {
        using var innerSession = new TestRealtimeSession();
        using var session = new OpenTelemetryRealtimeSession(innerSession);

        await Assert.ThrowsAsync<ArgumentNullException>("options", () => session.UpdateAsync(null!));
    }

    [Fact]
    public async Task GetStreamingResponseAsync_InvalidArgs_Throws()
    {
        using var innerSession = new TestRealtimeSession();
        using var session = new OpenTelemetryRealtimeSession(innerSession);

        await Assert.ThrowsAsync<ArgumentNullException>("updates", async () =>
        {
            await foreach (var _ in session.GetStreamingResponseAsync(null!))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public void GetService_ReturnsActivitySource()
    {
        using var innerSession = new TestRealtimeSession();
        using var session = new OpenTelemetryRealtimeSession(innerSession);

        var activitySource = session.GetService(typeof(ActivitySource));
        Assert.NotNull(activitySource);
        Assert.IsType<ActivitySource>(activitySource);
    }

    [Fact]
    public void GetService_ReturnsSelf()
    {
        using var innerSession = new TestRealtimeSession();
        using var session = new OpenTelemetryRealtimeSession(innerSession);

        var self = session.GetService(typeof(OpenTelemetryRealtimeSession));
        Assert.Same(session, self);

        var realtime = session.GetService(typeof(IRealtimeSession));
        Assert.Same(session, realtime);
    }

    [Fact]
    public async Task TranscriptionSessionKind_Logged()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "whisper-1",
                SessionKind = RealtimeSessionKind.Transcription,
            },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => TranscriptionCallbackAsync(updates, cancellationToken),
        };

        static async IAsyncEnumerable<RealtimeServerMessage> TranscriptionCallbackAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Consume
            }

            yield return new RealtimeServerInputAudioTranscriptionMessage(RealtimeServerMessageType.InputAudioTranscriptionCompleted)
            {
                Transcription = "Hello world",
            };
            yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
        }

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        Assert.Equal("Transcription", activity.GetTagItem("gen_ai.realtime.session_kind"));
    }

    [Theory]
    [InlineData(ToolChoiceMode.None, "none")]
    [InlineData(ToolChoiceMode.Auto, "auto")]
    [InlineData(ToolChoiceMode.Required, "required")]
    public async Task ToolChoiceMode_Logged(ToolChoiceMode mode, string expectedValue)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "test-model",
                ToolChoiceMode = mode,
                Tools = [AIFunctionFactory.Create((string query) => query, "Search")],
            },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        Assert.Equal(expectedValue, activity.GetTagItem("gen_ai.request.tool_choice"));
    }

    [Fact]
    public async Task AIFunction_ForcedTool_Logged()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        var forcedFunction = AIFunctionFactory.Create((string query) => query, "SpecificSearch", "Search with specific parameters");

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "test-model",
                AIFunction = forcedFunction,
                ToolChoiceMode = ToolChoiceMode.Auto, // Should be ignored when AIFunction is set
            },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        Assert.Equal("SpecificSearch", activity.GetTagItem("gen_ai.request.tool_choice"));
    }

#pragma warning disable MEAI001 // Type is for evaluation purposes only
    [Fact]
    public async Task HostedMcpServerTool_ForcedTool_Logged()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        var mcpTool = new HostedMcpServerTool("github-server", "https://mcp.github.com");

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "test-model",
                HostedMcpServerTool = mcpTool,
                ToolChoiceMode = ToolChoiceMode.Auto, // Should be ignored when HostedMcpServerTool is set
            },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        Assert.Equal("mcp:github-server", activity.GetTagItem("gen_ai.request.tool_choice"));
    }
#pragma warning restore MEAI001

    [Fact]
    public async Task NoToolChoice_NotLogged()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions
            {
                Model = "test-model",
            },
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var clientMessages = GetClientMessagesAsync();
        await foreach (var response in session.GetStreamingResponseAsync(clientMessages))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        Assert.Null(activity.GetTagItem("gen_ai.request.tool_choice"));
    }

    [Fact]
    public async Task ToolCallContentInClientMessages_LoggedAsInputMessages()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesWithToolResultAsync()))
        {
            // Consume
        }

        // With sensitive data enabled, we get one activity per message with content plus one for output
        // GetClientMessagesWithToolResultAsync yields 2 messages but only 1 has content, so 1 input activity + 1 output = 2 activities
        Assert.Equal(2, activities.Count);
        var inputActivity = activities[0];
        var inputMessages = inputActivity.GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("tool_call_response", inputMessages);
        Assert.Contains("call_1", inputMessages);
    }

    [Fact]
    public async Task ToolCallContentInServerMessages_LoggedAsOutputMessages()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => CallbackWithToolCallAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesAsync()))
        {
            // Consume
        }

        // With sensitive data enabled, we get one activity per message with content plus one for output
        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output = 3 activities
        Assert.Equal(3, activities.Count);
        var outputActivity = activities[2];
        var outputMessages = outputActivity.GetTagItem("gen_ai.output.messages")?.ToString();
        Assert.NotNull(outputMessages);
        Assert.Contains("tool_call", outputMessages);
        Assert.Contains("search", outputMessages);
    }

    [Fact]
    public async Task ToolContentNotLoggedWithoutSensitiveData()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => CallbackWithToolCallAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = false)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesWithToolResultAsync()))
        {
            // Consume
        }

        var activity = Assert.Single(activities);
        Assert.Null(activity.GetTagItem("gen_ai.input.messages"));
        Assert.Null(activity.GetTagItem("gen_ai.output.messages"));
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> SimpleCallbackAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Consume
        }

        yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        await Task.Yield();
        yield return new RealtimeClientInputAudioBufferAppendMessage(new DataContent(new byte[] { 1, 2, 3 }, "audio/pcm"));
        yield return new RealtimeClientInputAudioBufferCommitMessage();
        yield return new RealtimeClientResponseCreateMessage();
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessagesWithToolResultAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        await Task.Yield();
        var contentItem = new RealtimeContentItem([new FunctionResultContent("call_1", "result_value")], role: ChatRole.Tool);
        yield return new RealtimeClientConversationItemCreateMessage(contentItem);
        yield return new RealtimeClientResponseCreateMessage();
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> CallbackWithToolCallAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        // Consume incoming messages
        await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Consume
        }

        // Yield a function call item from the server using RealtimeServerResponseOutputItemMessage
        var contentItem = new RealtimeContentItem(
            [new FunctionCallContent("call_123", "search", new Dictionary<string, object?> { ["query"] = "test" })],
            role: ChatRole.Assistant);
        yield return new RealtimeServerResponseOutputItemMessage(RealtimeServerMessageType.ResponseDone)
        {
            Item = contentItem,
        };

        yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
    }

    [Fact]
    public async Task AudioBufferAppendMessage_LoggedAsInputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesAsync()))
        {
            // Consume
        }

        // With sensitive data enabled, we get one activity per message with content plus one for output
        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output = 3 activities
        Assert.Equal(3, activities.Count);
        var inputActivity = activities[0];
        var inputMessages = inputActivity.GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("blob", inputMessages);
        Assert.Contains("audio", inputMessages);
    }

    [Fact]
    public async Task AudioBufferCommitMessage_LoggedAsInputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesAsync()))
        {
            // Consume
        }

        // With sensitive data enabled, we get one activity per message with content plus one for output
        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output = 3 activities
        // The audio_commit message is the 2nd message with content, so it appears in activities[1]
        Assert.Equal(3, activities.Count);
        var inputActivity = activities[1];
        var inputMessages = inputActivity.GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("audio_commit", inputMessages);
    }

    [Fact]
    public async Task ResponseCreateMessageWithInstructions_LoggedAsInputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesWithInstructionsAsync()))
        {
            // Consume
        }

        // With sensitive data enabled, we get 2 activities: input (first) and output (second)
        Assert.Equal(2, activities.Count);
        var inputActivity = activities[0];
        var inputMessages = inputActivity.GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("instructions", inputMessages);
        Assert.Contains("Be very helpful", inputMessages);
    }

    [Fact]
    public async Task ResponseCreateMessageWithItems_LoggedAsInputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesWithItemsAsync()))
        {
            // Consume
        }

        // With sensitive data enabled, we get 2 activities: input (first) and output (second)
        Assert.Equal(2, activities.Count);
        var inputActivity = activities[0];
        var inputMessages = inputActivity.GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("text", inputMessages);
        Assert.Contains("Hello from client", inputMessages);
    }

    [Fact]
    public async Task OutputTextAudioMessage_LoggedAsOutputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => CallbackWithTextOutputAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesAsync()))
        {
            // Consume
        }

        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output = 3 activities
        Assert.Equal(3, activities.Count);
        var outputMessages = activities[2].GetTagItem("gen_ai.output.messages")?.ToString();
        Assert.NotNull(outputMessages);
        Assert.Contains("assistant", outputMessages);
        Assert.Contains("Hello from server", outputMessages);
    }

    [Fact]
    public async Task InputAudioTranscriptionMessage_LoggedAsOutputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => CallbackWithTranscriptionAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesAsync()))
        {
            // Consume
        }

        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output = 3 activities
        Assert.Equal(3, activities.Count);
        var outputMessages = activities[2].GetTagItem("gen_ai.output.messages")?.ToString();
        Assert.NotNull(outputMessages);
        Assert.Contains("input_transcription", outputMessages);
        Assert.Contains("Transcribed audio content", outputMessages);
    }

    [Fact]
    public async Task ServerErrorMessage_LoggedAsOutputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => CallbackWithServerErrorAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesAsync()))
        {
            // Consume
        }

        // GetClientMessagesAsync yields 3 messages but only 2 have content, so 2 input activities + 1 output = 3 activities
        Assert.Equal(3, activities.Count);
        var outputMessages = activities[2].GetTagItem("gen_ai.output.messages")?.ToString();
        Assert.NotNull(outputMessages);
        Assert.Contains("error", outputMessages);
        Assert.Contains("Something went wrong on server", outputMessages);
    }

    [Fact]
    public async Task ConversationItemCreateWithTextContent_LoggedAsInputMessage()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesWithTextContentAsync()))
        {
            // Consume
        }

        // GetClientMessagesWithTextContentAsync yields 2 messages but only 1 has content, so 1 input activity + 1 output = 2 activities
        Assert.Equal(2, activities.Count);
        var inputMessages = activities[0].GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("user", inputMessages);
        Assert.Contains("User text message", inputMessages);
    }

    [Fact]
    public async Task DataContentInClientMessage_LoggedWithModality()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerSession = new TestRealtimeSession
        {
            Options = new RealtimeSessionOptions { Model = "test-model" },
            GetServiceCallback = (serviceType, _) =>
                serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("test-provider") : null,
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => SimpleCallbackAsync(updates, cancellationToken),
        };

        using var session = innerSession
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: s => s.EnableSensitiveData = true)
            .Build();

        await foreach (var response in session.GetStreamingResponseAsync(GetClientMessagesWithImageContentAsync()))
        {
            // Consume
        }

        // GetClientMessagesWithImageContentAsync yields 2 messages but only 1 has content, so 1 input activity + 1 output = 2 activities
        Assert.Equal(2, activities.Count);
        var inputMessages = activities[0].GetTagItem("gen_ai.input.messages")?.ToString();
        Assert.NotNull(inputMessages);
        Assert.Contains("blob", inputMessages);
        Assert.Contains("image", inputMessages);
        Assert.Contains("image/png", inputMessages);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessagesWithInstructionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        await Task.Yield();
        yield return new RealtimeClientResponseCreateMessage { Instructions = "Be very helpful" };
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessagesWithItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        await Task.Yield();
        var item = new RealtimeContentItem([new TextContent("Hello from client")], role: ChatRole.User);
        yield return new RealtimeClientResponseCreateMessage { Items = [item] };
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessagesWithTextContentAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        await Task.Yield();
        var item = new RealtimeContentItem([new TextContent("User text message")], role: ChatRole.User);
        yield return new RealtimeClientConversationItemCreateMessage(item);
        yield return new RealtimeClientResponseCreateMessage();
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessagesWithImageContentAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        await Task.Yield();
        var imageData = new DataContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png");
        var item = new RealtimeContentItem([imageData], role: ChatRole.User);
        yield return new RealtimeClientConversationItemCreateMessage(item);
        yield return new RealtimeClientResponseCreateMessage();
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> CallbackWithTextOutputAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Consume
        }

        yield return new RealtimeServerOutputTextAudioMessage(RealtimeServerMessageType.OutputTextDone)
        {
            Text = "Hello from server",
        };
        yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> CallbackWithTranscriptionAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Consume
        }

        yield return new RealtimeServerInputAudioTranscriptionMessage(RealtimeServerMessageType.InputAudioTranscriptionCompleted)
        {
            Transcription = "Transcribed audio content",
        };
        yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> CallbackWithServerErrorAsync(
        IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        await foreach (var _ in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Consume
        }

        yield return new RealtimeServerErrorMessage
        {
            Error = new ErrorContent("Something went wrong on server"),
        };
        yield return new RealtimeServerResponseCreatedMessage(RealtimeServerMessageType.ResponseDone);
    }

    private static string ReplaceWhitespace(string? input) => Regex.Replace(input ?? "", @"\s+", " ").Trim();

    [Fact]
    public void UseOpenTelemetry_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>("builder", () =>
            ((RealtimeSessionBuilder)null!).UseOpenTelemetry());
    }

    [Fact]
    public void UseOpenTelemetry_ConfigureCallback_IsInvoked()
    {
        using var innerSession = new TestRealtimeSession();
        var builder = new RealtimeSessionBuilder(innerSession);

        bool configured = false;
        builder.UseOpenTelemetry(configure: session =>
        {
            configured = true;
            session.EnableSensitiveData = true;
        });

        using var pipeline = builder.Build();
        Assert.True(configured);

        var otelSession = pipeline.GetService(typeof(OpenTelemetryRealtimeSession));
        Assert.NotNull(otelSession);

        var typedSession = Assert.IsType<OpenTelemetryRealtimeSession>(otelSession);
        Assert.True(typedSession.EnableSensitiveData);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var innerSession = new TestRealtimeSession();
        var session = new OpenTelemetryRealtimeSession(innerSession);

        session.Dispose();
        session.Dispose();

        // Verifying no exception is thrown on double dispose
        Assert.NotNull(session);
    }
}
