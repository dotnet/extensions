// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task CompleteAsync_LogsStartAndCompletion(LogLevel level)
    {
        using CapturingLoggerProvider clp = new();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(clp).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using IEmbeddingGenerator<string, Embedding<float>> innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1f, 2f, 3f })]));
            },
        };

        using IEmbeddingGenerator<string, Embedding<float>> generator = innerGenerator
            .ToBuilder()
            .UseLogging()
            .Build(services);

        await generator.GenerateEmbeddingAsync("Blue whale");

        if (level is LogLevel.Trace)
        {
            Assert.Collection(clp.Logger.Entries,
                entry => Assert.True(entry.Message.Contains("GenerateAsync invoked:") && entry.Message.Contains("Blue whale")),
                entry => Assert.Contains("GenerateAsync generated 1 embedding(s).", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(clp.Logger.Entries,
                entry => Assert.True(entry.Message.Contains("GenerateAsync invoked.") && !entry.Message.Contains("Blue whale")),
                entry => Assert.Contains("GenerateAsync generated 1 embedding(s).", entry.Message));
        }
        else
        {
            Assert.Empty(clp.Logger.Entries);
        }
    }
}
