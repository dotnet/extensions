// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingTextToImageClientTests
{
    [Fact]
    public void LoggingTextToImageClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingTextToImageClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingTextToImageClient(new TestTextToImageClient(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestTextToImageClient();

        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingTextToImageClient)));
        Assert.Same(innerClient, innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(ITextToImageClient)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerClient.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingTextToImageClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerClient.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingTextToImageClient)));
        Assert.NotNull(innerClient.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingTextToImageClient)));
        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingTextToImageClient)));
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

        using ITextToImageClient innerClient = new TestTextToImageClient
        {
            GenerateImagesAsyncCallback = (prompt, options, cancellationToken) =>
            {
                return Task.FromResult(new TextToImageResponse());
            },
        };

        using ITextToImageClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await client.GenerateImagesAsync(
            "A beautiful sunset",
            new TextToImageOptions { ModelId = "dall-e-3" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(ITextToImageClient.GenerateImagesAsync)} invoked:") &&
                    entry.Message.Contains("A beautiful sunset") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(ITextToImageClient.GenerateImagesAsync)} completed:", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToImageClient.GenerateImagesAsync)} invoked.") && !entry.Message.Contains("A beautiful sunset")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToImageClient.GenerateImagesAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
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
    public async Task GenerateEditImageAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using ITextToImageClient innerClient = new TestTextToImageClient
        {
            GenerateEditImageAsyncCallback = (originalImage, originalImageFileName, prompt, options, cancellationToken) =>
            {
                return Task.FromResult(new TextToImageResponse());
            }
        };

        using ITextToImageClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        using var imageStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        await client.GenerateEditImageAsync(
            imageStream,
            "test.png",
            "Make it more colorful",
            new TextToImageOptions { ModelId = "dall-e-3" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(
                    entry.Message.Contains($"{nameof(ITextToImageClient.GenerateEditImageAsync)} invoked:") &&
                    entry.Message.Contains("Make it more colorful") &&
                    entry.Message.Contains("dall-e-3")),
                entry => Assert.Contains($"{nameof(ITextToImageClient.GenerateEditImageAsync)} completed:", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToImageClient.GenerateEditImageAsync)} invoked.") && !entry.Message.Contains("Make it more colorful")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToImageClient.GenerateEditImageAsync)} completed.") && !entry.Message.Contains("dall-e-3")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
