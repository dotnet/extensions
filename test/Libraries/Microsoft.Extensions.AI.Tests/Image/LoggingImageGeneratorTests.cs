// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingImageGeneratorTests
{
    [Fact]
    public void LoggingImageGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new LoggingImageGenerator(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingImageGenerator(new TestImageGenerator(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopGenerator()
    {
        using var innerGenerator = new TestImageGenerator();

        Assert.Null(innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingImageGenerator)));
        Assert.Same(innerGenerator, innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IImageGenerator)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingImageGenerator)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingImageGenerator)));
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingImageGenerator)));
        Assert.Null(innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingImageGenerator)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GenerateImagesAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using IImageGenerator innerGenerator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                return Task.FromResult(new ImageGenerationResponse());
            },
        };

        using IImageGenerator generator = innerGenerator
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await generator.GenerateAsync(
            new ImageGenerationRequest("A beautiful sunset"),
            new ImageGenerationOptions { ModelId = "dall-e-3" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IImageGenerator.GenerateAsync)} invoked:") &&
                    entry.Message.Contains("A beautiful sunset") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(IImageGenerator.GenerateAsync)} completed:", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageGenerator.GenerateAsync)} invoked.") && !entry.Message.Contains("A beautiful sunset")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageGenerator.GenerateAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GenerateImagesAsync_WithOriginalImages_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using IImageGenerator innerGenerator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                return Task.FromResult(new ImageGenerationResponse());
            }
        };

        using IImageGenerator generator = innerGenerator
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        AIContent[] originalImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];
        await generator.GenerateAsync(
            new ImageGenerationRequest("Make it more colorful", originalImages),
            new ImageGenerationOptions { ModelId = "dall-e-3" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IImageGenerator.GenerateAsync)} invoked:") &&
                    entry.Message.Contains("Make it more colorful") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(IImageGenerator.GenerateAsync)} completed", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageGenerator.GenerateAsync)} invoked.") && !entry.Message.Contains("Make it more colorful")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageGenerator.GenerateAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GenerateStreamingImagesAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using IImageGenerator innerGenerator = new TestImageGenerator
        {
            GenerateStreamingImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                return YieldUpdates(new ImageResponseUpdate([new DataContent(new byte[] { 1, 2, 3 }, "image/png")]));
            }
        };

        using IImageGenerator generator = innerGenerator
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        var updates = new List<ImageResponseUpdate>();
        await foreach (var update in generator.GenerateStreamingImagesAsync(
            new ImageGenerationRequest("A beautiful sunset"),
            new ImageGenerationOptions { ModelId = "dall-e-3" }))
        {
            updates.Add(update);
        }

        Assert.Single(updates);

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IImageGenerator.GenerateStreamingImagesAsync)} invoked:") &&
                    entry.Message.Contains("A beautiful sunset") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(IImageGenerator.GenerateStreamingImagesAsync)} completed", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageGenerator.GenerateStreamingImagesAsync)} invoked.") && !entry.Message.Contains("A beautiful sunset")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageGenerator.GenerateStreamingImagesAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    private static async IAsyncEnumerable<ImageResponseUpdate> YieldUpdates(params ImageResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}
