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

public class LoggingImageClientTests
{
    [Fact]
    public void LoggingImageClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingImageClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingImageClient(new TestImageClient(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestImageClient();

        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingImageClient)));
        Assert.Same(innerClient, innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IImageClient)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerClient.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingImageClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerClient.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingImageClient)));
        Assert.NotNull(innerClient.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingImageClient)));
        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingImageClient)));
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

        using IImageClient innerClient = new TestImageClient
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                return Task.FromResult(new ImageResponse());
            },
        };

        using IImageClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await client.GenerateImagesAsync(
            new ImageRequest("A beautiful sunset"),
            new ImageOptions { ModelId = "dall-e-3" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IImageClient.GenerateImagesAsync)} invoked:") &&
                    entry.Message.Contains("A beautiful sunset") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(IImageClient.GenerateImagesAsync)} completed:", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageClient.GenerateImagesAsync)} invoked.") && !entry.Message.Contains("A beautiful sunset")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageClient.GenerateImagesAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
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

        using IImageClient innerClient = new TestImageClient
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                return Task.FromResult(new ImageResponse());
            }
        };

        using IImageClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        AIContent[] originalImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];
        await client.GenerateImagesAsync(
            new ImageRequest("Make it more colorful", originalImages),
            new ImageOptions { ModelId = "dall-e-3" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(IImageClient.GenerateImagesAsync)} invoked:") &&
                    entry.Message.Contains("Make it more colorful") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(IImageClient.GenerateImagesAsync)} completed:", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageClient.GenerateImagesAsync)} invoked.") && !entry.Message.Contains("Make it more colorful")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IImageClient.GenerateImagesAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
