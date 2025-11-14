// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryImageGeneratorTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new OpenTelemetryImageGenerator(null!));
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

        using var innerGenerator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = async (request, options, cancellationToken) =>
            {
                await Task.Yield();

                return new()
                {
                    Contents =
                    [
                        new UriContent("http://example/output.png", "image/png"),
                        new DataContent(new byte[] { 1, 2, 3, 4 }, "image/png") { Name = "moreOutput.png" },
                    ],

                    Usage = new()
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 20,
                        TotalTokenCount = 30,
                    },
                };
            },

            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(ImageGeneratorMetadata) ? new ImageGeneratorMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel") :
                null,
        };

        using var g = innerGenerator
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
            })
            .Build();

        ImageGenerationRequest request = new()
        {
            Prompt = "This is the input prompt.",
            OriginalImages = [new UriContent("http://example/input.png", "image/png")],
        };

        ImageGenerationOptions options = new()
        {
            Count = 2,
            ImageSize = new(1024, 768),
            MediaType = "image/jpeg",
            ModelId = "mycoolimagemodel",
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

        Assert.Equal("generate_content mycoolimagemodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal("mycoolimagemodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(2, activity.GetTagItem("gen_ai.request.choice.count"));
        Assert.Equal(1024, activity.GetTagItem("gen_ai.request.image.width"));
        Assert.Equal(768, activity.GetTagItem("gen_ai.request.image.height"));
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
                    "role": "user",
                    "parts": [
                      {
                        "type": "text",
                        "content": "This is the input prompt."
                      },
                      {
                        "type": "uri",
                        "uri": "http://example/input.png",
                        "mime_type": "image/png",
                        "modality": "image"
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
                        "type": "uri",
                        "uri": "http://example/output.png",
                        "mime_type": "image/png",
                        "modality": "image"
                      },
                      {
                        "type": "blob",
                        "content": "AQIDBA==",
                        "mime_type": "image/png",
                        "modality": "image"
                      }
                    ]
                  }
                ]
                """), ReplaceWhitespace(tags["gen_ai.output.messages"]));
        }
        else
        {
            Assert.False(tags.ContainsKey("gen_ai.input.messages"));
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

        using var innerGenerator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                throw new InvalidOperationException("Test exception");
            },
        };

        using var generator = innerGenerator
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => generator.GenerateAsync(new ImageGenerationRequest { Prompt = "test" }));

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
