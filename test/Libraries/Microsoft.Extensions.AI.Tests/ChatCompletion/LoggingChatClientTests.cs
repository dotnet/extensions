﻿// Licensed to the .NET Foundation under one or more agreements.
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

public class LoggingChatClientTests
{
    [Fact]
    public void LoggingChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingChatClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingChatClient(new TestChatClient(), null!));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task CompleteAsync_LogsStartAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using IChatClient innerClient = new TestChatClient
        {
            CompleteAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatCompletion([new(ChatRole.Assistant, "blue whale")]));
            },
        };

        using IChatClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await client.CompleteAsync(
            [new(ChatRole.User, "What's the biggest animal?")],
            new ChatOptions { FrequencyPenalty = 3.0f });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains("CompleteAsync invoked:") && entry.Message.Contains("biggest animal")),
                entry => Assert.True(entry.Message.Contains("CompleteAsync completed:") && entry.Message.Contains("blue whale")));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains("CompleteAsync invoked.") && !entry.Message.Contains("biggest animal")),
                entry => Assert.True(entry.Message.Contains("CompleteAsync completed.") && !entry.Message.Contains("blue whale")));
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
    public async Task CompleteStreamAsync_LogsStartUpdateCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using IChatClient innerClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = (messages, options, cancellationToken) => GetUpdatesAsync()
        };

        static async IAsyncEnumerable<StreamingChatCompletionUpdate> GetUpdatesAsync()
        {
            await Task.Yield();
            yield return new StreamingChatCompletionUpdate { Role = ChatRole.Assistant, Text = "blue " };
            yield return new StreamingChatCompletionUpdate { Role = ChatRole.Assistant, Text = "whale" };
        }

        using IChatClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await foreach (var update in client.CompleteStreamingAsync(
            [new(ChatRole.User, "What's the biggest animal?")],
            new ChatOptions { FrequencyPenalty = 3.0f }))
        {
            // nop
        }

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains("CompleteStreamingAsync invoked:") && entry.Message.Contains("biggest animal")),
                entry => Assert.True(entry.Message.Contains("CompleteStreamingAsync received update:") && entry.Message.Contains("blue")),
                entry => Assert.True(entry.Message.Contains("CompleteStreamingAsync received update:") && entry.Message.Contains("whale")),
                entry => Assert.Contains("CompleteStreamingAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains("CompleteStreamingAsync invoked.") && !entry.Message.Contains("biggest animal")),
                entry => Assert.True(entry.Message.Contains("CompleteStreamingAsync received update.") && !entry.Message.Contains("blue")),
                entry => Assert.True(entry.Message.Contains("CompleteStreamingAsync received update.") && !entry.Message.Contains("whale")),
                entry => Assert.Contains("CompleteStreamingAsync completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
