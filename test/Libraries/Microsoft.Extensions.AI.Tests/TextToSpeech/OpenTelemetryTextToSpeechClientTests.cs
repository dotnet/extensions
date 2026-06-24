// Licensed to the .NET Foundation under one or more agreements.
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

public class OpenTelemetryTextToSpeechClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new OpenTelemetryTextToSpeechClient(null!));
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

        using var innerClient = new TestTextToSpeechClient
        {
            GetAudioAsyncCallback = async (text, options, cancellationToken) =>
            {
                await Task.Yield();
                return new([new DataContent(new byte[] { 1, 2, 3 }, "audio/mpeg")])
                {
                    Usage = new()
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 30,
                    },
                };
            },

            GetStreamingAudioAsyncCallback = TestClientStreamAsync,

            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(TextToSpeechClientMetadata) ? new TextToSpeechClientMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel") :
                null,
        };

        static async IAsyncEnumerable<TextToSpeechResponseUpdate> TestClientStreamAsync(
            string text, TextToSpeechOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new([new DataContent(new byte[] { 1 }, "audio/mpeg")]);
            yield return new()
            {
                Contents =
                [
                    new DataContent(new byte[] { 2 }, "audio/mpeg"),
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

        TextToSpeechOptions options = new()
        {
            ModelId = "mycoolttsmodel",
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
        };

        if (streaming)
        {
            await foreach (var update in client.GetStreamingAudioAsync("Hello, world!", options))
            {
                // consume
            }
        }
        else
        {
            await client.GetAudioAsync("Hello, world!", options);
        }

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("generate_content mycoolttsmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal("mycoolttsmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(enableSensitiveData ? "value1" : null, activity.GetTagItem("service_tier"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("SomethingElse"));

        Assert.Equal(10, activity.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.usage.output_tokens"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExceptionLogged_Async(bool streaming)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        var collector = new FakeLogCollector();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)));

        var expectedException = new InvalidOperationException("test exception message");

        using var innerClient = new TestTextToSpeechClient
        {
            GetAudioAsyncCallback = (text, options, cancellationToken) => throw expectedException,
            GetStreamingAudioAsyncCallback = (text, options, cancellationToken) => throw expectedException,
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(TextToSpeechClientMetadata) ? new TextToSpeechClientMetadata("testservice", new Uri("http://localhost:12345"), "testmodel") :
                null,
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory, sourceName)
            .Build();

        if (streaming)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var update in client.GetStreamingAudioAsync("Hello"))
                {
                    _ = update;
                }
            });
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GetAudioAsync("Hello"));
        }

        var activity = Assert.Single(activities);

        // Existing error behavior is preserved
        Assert.Equal(expectedException.GetType().FullName, activity.GetTagItem("error.type"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        // Exception is logged via ILogger
        var logEntry = Assert.Single(collector.GetSnapshot());
        Assert.Equal("gen_ai.client.operation.exception", logEntry.Id.Name);
        Assert.Equal(LogLevel.Warning, logEntry.Level);
        Assert.Same(expectedException, logEntry.Exception);
    }
}
