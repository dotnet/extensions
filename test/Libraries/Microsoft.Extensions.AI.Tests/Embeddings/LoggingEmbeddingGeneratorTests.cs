// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingEmbeddingGeneratorTests
{
    [Fact]
    public void LoggingEmbeddingGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new LoggingEmbeddingGenerator<string, Embedding<float>>(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingEmbeddingGenerator<string, Embedding<float>>(new TestEmbeddingGenerator(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerGenerator = new TestEmbeddingGenerator();

        Assert.Null(innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingEmbeddingGenerator<string, Embedding<float>>)));
        Assert.Same(innerGenerator, innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IEmbeddingGenerator<string, Embedding<float>>)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingEmbeddingGenerator<string, Embedding<float>>)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingEmbeddingGenerator<string, Embedding<float>>)));
        Assert.NotNull(innerGenerator.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingEmbeddingGenerator<string, Embedding<float>>)));
        Assert.Null(innerGenerator.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingEmbeddingGenerator<string, Embedding<float>>)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GetResponseAsync_LogsResponseInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using IEmbeddingGenerator<string, Embedding<float>> innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1f, 2f, 3f })]));
            },
        };

        using IEmbeddingGenerator<string, Embedding<float>> generator = innerGenerator
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await generator.GenerateEmbeddingAsync("Blue whale");

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains("GenerateAsync invoked:") && entry.Message.Contains("Blue whale")),
                entry => Assert.Contains("GenerateAsync generated 1 embedding(s).", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains("GenerateAsync invoked.") && !entry.Message.Contains("Blue whale")),
                entry => Assert.Contains("GenerateAsync generated 1 embedding(s).", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
