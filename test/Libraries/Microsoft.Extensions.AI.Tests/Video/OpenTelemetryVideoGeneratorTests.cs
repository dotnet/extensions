// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryVideoGeneratorTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new OpenTelemetryVideoGenerator(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpectedInformationLogged_Async(bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerGenerator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = async (request, options, cancellationToken) =>
            {
                await Task.Yield();

                return new TestVideoGenerationOperation
                {
                    Usage = new()
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 30,
                    },
                };
            },

            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(VideoGeneratorMetadata) ? new VideoGeneratorMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel") :
                null,
        };

        using var g = innerGenerator
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
            })
            .Build();

        VideoGenerationRequest request = new()
        {
            Prompt = "This is the input prompt.",
            SourceVideo = new UriContent("http://example/input.mp4", "video/mp4"),
        };

        VideoGenerationOptions options = new()
        {
            Count = 2,
            VideoSize = new(1920, 1080),
            Duration = TimeSpan.FromSeconds(10),
            FramesPerSecond = 24,
            MediaType = "video/mp4",
            ModelId = "mycoolvideomodel",
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
        };

        await g.GenerateAsync(request, options);

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("generate_content mycoolvideomodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal("mycoolvideomodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(2, activity.GetTagItem("gen_ai.request.choice.count"));
        Assert.Equal(1920, activity.GetTagItem("gen_ai.request.video.width"));
        Assert.Equal(1080, activity.GetTagItem("gen_ai.request.video.height"));
        Assert.Equal(10.0, activity.GetTagItem("gen_ai.request.video.duration"));
        Assert.Equal(24, activity.GetTagItem("gen_ai.request.video.fps"));
        Assert.Equal(enableSensitiveData ? "value1" : null, activity.GetTagItem("service_tier"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("SomethingElse"));

        Assert.Equal(10, activity.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(20, activity.GetTagItem("gen_ai.usage.output_tokens"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);

        var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Operation metadata is always recorded
        Assert.Equal("test-op-id", activity.GetTagItem("gen_ai.operation.id"));
        Assert.Equal("completed", activity.GetTagItem("gen_ai.operation.status"));

        if (enableSensitiveData)
        {
            Assert.Equal(ReplaceWhitespace("""
                [
                  {
                    "role": "user",
                    "parts": [
                      {
                        "type": "text",
                        "content": "This is the input prompt."
                      },
                      {
                        "type": "uri",
                        "uri": "http://example/input.mp4",
                        "mime_type": "video/mp4",
                        "modality": "video"
                      }
                    ]
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.input.messages"]));
        }
        else
        {
            Assert.False(tags.ContainsKey("gen_ai.input.messages"));
        }

        static string ReplaceWhitespace(string? input) => Regex.Replace(input ?? "", @"\s+", " ").Trim();
    }

    [Fact]
    public async Task ExceptionLogged_Async()
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

        using var innerGenerator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, cancellationToken) => throw expectedException,
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(VideoGeneratorMetadata) ? new VideoGeneratorMetadata("testservice", new Uri("http://localhost:12345"), "testmodel") :
                null,
        };

        using var g = innerGenerator
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory, sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            g.GenerateAsync(new VideoGenerationRequest { Prompt = "a cat video" }));

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
