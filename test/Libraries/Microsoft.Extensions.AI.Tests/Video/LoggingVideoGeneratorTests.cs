// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingVideoGeneratorTests
{
    [Fact]
    public void LoggingVideoGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new LoggingVideoGenerator(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingVideoGenerator(new TestVideoGenerator(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopGenerator()
    {
        using var innerGenerator = new TestVideoGenerator();

        Assert.Null(innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingVideoGenerator)));
        Assert.Same(innerGenerator, innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IVideoGenerator)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingVideoGenerator)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingVideoGenerator)));
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingVideoGenerator)));
        Assert.Null(innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingVideoGenerator)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GenerateVideosAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using IVideoGenerator innerGenerator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, cancellationToken) =>
            {
                return Task.FromResult(new VideoGenerationResponse());
            },
        };

        using IVideoGenerator generator = innerGenerator
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await generator.GenerateAsync(
            new VideoGenerationRequest("A beautiful sunset"),
            new VideoGenerationOptions { ModelId = "sora" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IVideoGenerator.GenerateAsync)} invoked:") &&
                    entry.Message.Contains("A beautiful sunset") &&
                    entry.Message.Contains("sora")),
                entry => Assert.Contains($"{nameof(IVideoGenerator.GenerateAsync)} completed:", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IVideoGenerator.GenerateAsync)} invoked.") && !entry.Message.Contains("A beautiful sunset")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IVideoGenerator.GenerateAsync)} completed.") && !entry.Message.Contains("sora")));
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
    public async Task GenerateVideosAsync_WithOriginalMedia_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using IVideoGenerator innerGenerator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, cancellationToken) =>
            {
                return Task.FromResult(new VideoGenerationResponse());
            }
        };

        using IVideoGenerator generator = innerGenerator
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        AIContent[] originalMedia = [new DataContent((byte[])[1, 2, 3, 4], "video/mp4")];
        await generator.GenerateAsync(
            new VideoGenerationRequest("Make it more colorful", originalMedia),
            new VideoGenerationOptions { ModelId = "sora" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IVideoGenerator.GenerateAsync)} invoked:") &&
                    entry.Message.Contains("Make it more colorful") &&
                    entry.Message.Contains("sora")),
                entry => Assert.Contains($"{nameof(IVideoGenerator.GenerateAsync)} completed", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IVideoGenerator.GenerateAsync)} invoked.") && !entry.Message.Contains("Make it more colorful")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IVideoGenerator.GenerateAsync)} completed.") && !entry.Message.Contains("sora")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
