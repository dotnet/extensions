// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryEmbeddingGeneratorTests
{
    [Fact]
    public async Task ExpectedInformationLogged_Async()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)));

        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = async (values, options, cancellationToken) =>
            {
                await Task.Yield();
                return new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1, 2, 3 })])
                {
                    Usage = new()
                    {
                        InputTokenCount = 10,
                        TotalTokenCount = 10,
                    },
                    AdditionalProperties = new()
                    {
                        ["system_fingerprint"] = "abcdefgh",
                        ["AndSomethingElse"] = "value2",
                    }
                };
            },
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(EmbeddingGeneratorMetadata) ? new EmbeddingGeneratorMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel", 384) :
                null,
        };

        using var generator = innerGenerator
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory, sourceName)
            .Build();

        var options = new EmbeddingGenerationOptions
        {
            ModelId = "replacementmodel",
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
        };

        await generator.GenerateEmbeddingVectorAsync("hello", options);

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("http://localhost:12345/something", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("embeddings replacementmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.system"));

        Assert.Equal("replacementmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal("value1", activity.GetTagItem("gen_ai.testservice.request.service_tier"));
        Assert.Equal("value2", activity.GetTagItem("gen_ai.testservice.request.something_else"));

        Assert.Equal(10, activity.GetTagItem("gen_ai.response.input_tokens"));
        Assert.Equal("abcdefgh", activity.GetTagItem("gen_ai.testservice.response.system_fingerprint"));
        Assert.Equal("value2", activity.GetTagItem("gen_ai.testservice.response.and_something_else"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }
}
