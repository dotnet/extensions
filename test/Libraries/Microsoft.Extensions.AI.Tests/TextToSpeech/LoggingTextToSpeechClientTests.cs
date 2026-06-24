// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingTextToSpeechClientTests
{
    [Fact]
    public void LoggingTextToSpeechClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingTextToSpeechClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingTextToSpeechClient(new TestTextToSpeechClient(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestTextToSpeechClient();

        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingTextToSpeechClient)));
        Assert.Same(innerClient, innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(ITextToSpeechClient)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerClient.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingTextToSpeechClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerClient.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingTextToSpeechClient)));
        Assert.NotNull(innerClient.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingTextToSpeechClient)));
        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingTextToSpeechClient)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GetAudioAsync_LogsResponseInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using ITextToSpeechClient innerClient = new TestTextToSpeechClient
        {
            GetAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                return Task.FromResult(new TextToSpeechResponse([new DataContent(new byte[] { 1, 2, 3 }, "audio/mpeg")]));
            },
        };

        using ITextToSpeechClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await client.GetAudioAsync(
            "Hello, world!",
            new TextToSpeechOptions { VoiceId = "alloy" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            // Invocation is logged at Trace level with options, but completion avoids
            // serializing binary audio data, so it logs the same as Debug level.
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToSpeechClient.GetAudioAsync)} invoked:") && entry.Message.Contains("\"voiceId\": \"alloy\"")),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetAudioAsync)} completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToSpeechClient.GetAudioAsync)} invoked.") && !entry.Message.Contains("\"voiceId\": \"alloy\"")),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetAudioAsync)} completed.", entry.Message));
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
    public async Task GetStreamingAudioAsync_LogsUpdateReceived(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using ITextToSpeechClient innerClient = new TestTextToSpeechClient
        {
            GetStreamingAudioAsyncCallback = (text, options, cancellationToken) => GetUpdatesAsync()
        };

        static async IAsyncEnumerable<TextToSpeechResponseUpdate> GetUpdatesAsync()
        {
            await Task.Yield();
            yield return new([new DataContent(new byte[] { 1 }, "audio/mpeg")]);
            yield return new([new DataContent(new byte[] { 2 }, "audio/mpeg")]);
        }

        using ITextToSpeechClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await foreach (var update in client.GetStreamingAudioAsync(
            "Hello, world!",
            new TextToSpeechOptions { VoiceId = "alloy" }))
        {
            // nop
        }

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            // Invocation is logged at Trace level with options, but streaming updates
            // avoid serializing binary audio data, so they log the same as Debug level.
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} invoked:") && entry.Message.Contains("\"voiceId\": \"alloy\"")),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} received update.", entry.Message),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} received update.", entry.Message),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} invoked.") && !entry.Message.Contains("voiceId")),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} received update.", entry.Message),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} received update.", entry.Message),
                entry => Assert.Contains($"{nameof(ITextToSpeechClient.GetStreamingAudioAsync)} completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
