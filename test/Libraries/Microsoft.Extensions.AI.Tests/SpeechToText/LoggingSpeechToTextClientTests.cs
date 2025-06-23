// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingSpeechToTextClientTests
{
    [Fact]
    public void LoggingSpeechToTextClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingSpeechToTextClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingSpeechToTextClient(new TestSpeechToTextClient(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestSpeechToTextClient();

        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingSpeechToTextClient)));
        Assert.Same(innerClient, innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(ISpeechToTextClient)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerClient.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingSpeechToTextClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerClient.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingSpeechToTextClient)));
        Assert.NotNull(innerClient.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingSpeechToTextClient)));
        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingSpeechToTextClient)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GetTextAsync_LogsResponseInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                return Task.FromResult(new SpeechToTextResponse("blue whale"));
            },
        };

        using ISpeechToTextClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        using var audioSpeechStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        await client.GetTextAsync(
            audioSpeechStream,
            new SpeechToTextOptions { SpeechLanguage = "pt" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetTextAsync)} invoked:") && entry.Message.Contains("\"speechLanguage\": \"pt\"")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetTextAsync)} completed:") && entry.Message.Contains("blue whale")));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetTextAsync)} invoked.") && !entry.Message.Contains("\"speechLanguage\": \"pt\"")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetTextAsync)} completed.") && !entry.Message.Contains("blue whale")));
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
    public async Task GetStreamingTextAsync_LogsUpdateReceived(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetStreamingTextAsyncCallback = (audioSpeechStream, options, cancellationToken) => GetUpdatesAsync()
        };

        static async IAsyncEnumerable<SpeechToTextResponseUpdate> GetUpdatesAsync()
        {
            await Task.Yield();
            yield return new SpeechToTextResponseUpdate("blue ");
            yield return new SpeechToTextResponseUpdate("whale");
        }

        using ISpeechToTextClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        using var audioSpeechStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        await foreach (var update in client.GetStreamingTextAsync(
            audioSpeechStream,
            new SpeechToTextOptions { SpeechLanguage = "pt" }))
        {
            // nop
        }

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} invoked:") && entry.Message.Contains("\"speechLanguage\": \"pt\"")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} received update:") && entry.Message.Contains("blue")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} received update:") && entry.Message.Contains("whale")),
                entry => Assert.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} invoked.") && !entry.Message.Contains("speechLanguage")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} received update.") && !entry.Message.Contains("blue")),
                entry => Assert.True(entry.Message.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} received update.") && !entry.Message.Contains("whale")),
                entry => Assert.Contains($"{nameof(ISpeechToTextClient.GetStreamingTextAsync)} completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
