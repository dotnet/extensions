// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetrySpeechToTextClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new OpenTelemetrySpeechToTextClient(null!));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExpectedInformationLogged_Async(bool streaming, bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestSpeechToTextClient
        {
            GetTextAsyncCallback = async (request, options, cancellationToken) =>
            {
                await Task.Yield();
                return new("This is the recognized text.")
                {
                    Usage = new()
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 30,
                    },
                };
            },

            GetStreamingTextAsyncCallback = TestClientStreamAsync,

            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(SpeechToTextClientMetadata) ? new SpeechToTextClientMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel") :
                null,
        };

        static async IAsyncEnumerable<SpeechToTextResponseUpdate> TestClientStreamAsync(
            Stream request, SpeechToTextOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new("This is");
            yield return new(" the recognized");
            yield return new()
            {
                Contents =
                [
                    new TextContent(" text."),
                    new UsageContent(new()
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 30,
                    }),
                ]
            };
        }

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
            })
            .Build();

        SpeechToTextOptions options = new()
        {
            ModelId = "mycoolspeechmodel",
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
        };

        var response = streaming ?
            await client.GetStreamingTextAsync(Stream.Null, options).ToSpeechToTextResponseAsync() :
            await client.GetTextAsync(Stream.Null, options);

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("generate_content mycoolspeechmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal("mycoolspeechmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(enableSensitiveData ? "value1" : null, activity.GetTagItem("service_tier"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("SomethingElse"));

        Assert.Equal(10, activity.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.usage.output_tokens"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);

        var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (enableSensitiveData)
        {
            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "role": "assistant",
                    "parts": [
                      {
                        "type": "text",
                        "content": "This is the recognized text."
                      }
                    ]
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.output.messages"]));
        }
        else
        {
            Assert.False(tags.ContainsKey("gen_ai.output.messages"));
        }

        static string ReplaceWhitespace(string? input) => Regex.Replace(input ?? "", @"\s+", " ").Trim();
    }

    [Fact]
    public async Task ExceptionEventRecorded_Async()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestSpeechToTextClient
        {
            GetTextAsyncCallback = (request, options, cancellationToken) =>
            {
                throw new InvalidOperationException("Test exception");
            },
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetTextAsync(Stream.Null));

        var activity = Assert.Single(activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test exception", activity.StatusDescription);
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("error.type"));

        var exceptionEvent = Assert.Single(activity.Events.Where(e => e.Name == "exception"));
        Assert.Equal("System.InvalidOperationException", exceptionEvent.Tags.First(t => t.Key == "exception.type").Value);
        Assert.Equal("Test exception", exceptionEvent.Tags.First(t => t.Key == "exception.message").Value);
        Assert.NotNull(exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value);
    }

    [Fact]
    public async Task ExceptionEventRecorded_Streaming()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestSpeechToTextClient
        {
            GetStreamingTextAsyncCallback = ThrowingStreamAsync,
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(SpeechToTextClientMetadata) ? new SpeechToTextClientMetadata("testservice", new Uri("http://localhost:12345/something"), "testmodel") :
                null,
        };

        static async IAsyncEnumerable<SpeechToTextResponseUpdate> ThrowingStreamAsync(
            Stream request, SpeechToTextOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new("This is");
            throw new InvalidOperationException("Test exception");
        }

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var update in client.GetStreamingTextAsync(Stream.Null))
            {
                // Process updates
            }
        });

        var activity = Assert.Single(activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test exception", activity.StatusDescription);
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("error.type"));

        var exceptionEvent = Assert.Single(activity.Events.Where(e => e.Name == "exception"));
        Assert.Equal("System.InvalidOperationException", exceptionEvent.Tags.First(t => t.Key == "exception.type").Value);
        Assert.Equal("Test exception", exceptionEvent.Tags.First(t => t.Key == "exception.message").Value);
        Assert.NotNull(exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value);
    }
}
