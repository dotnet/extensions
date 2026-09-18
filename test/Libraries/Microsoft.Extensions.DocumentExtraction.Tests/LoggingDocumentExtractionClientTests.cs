// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class LoggingDocumentExtractionClientTests
{
    [Fact]
    public void LoggingDocumentExtractionClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingDocumentExtractionClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingDocumentExtractionClient(new TestDocumentExtractionClient(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestDocumentExtractionClient();

        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingDocumentExtractionClient)));
        Assert.Same(innerClient, innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IDocumentExtractionClient)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerClient.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingDocumentExtractionClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerClient.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingDocumentExtractionClient)));
        Assert.NotNull(innerClient.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingDocumentExtractionClient)));
        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingDocumentExtractionClient)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task ExtractAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using IDocumentExtractionClient innerClient = new TestDocumentExtractionClient
        {
            ExtractAsyncCallback = (document, mediaType, options, cancellationToken) =>
                Task.FromResult(new DocumentExtractionResult([new DocumentPage(1, "blue whale")])),
        };

        using IDocumentExtractionClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        using var document = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        await client.ExtractAsync(document, "application/pdf", new DocumentExtractionOptions { ModelId = "mistral-ocr-4-0" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IDocumentExtractionClient.ExtractAsync)} invoked:") && entry.Message.Contains("mistral-ocr-4-0")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IDocumentExtractionClient.ExtractAsync)} completed:") && entry.Message.Contains("blue whale")));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.True(entry.Message.Contains($"{nameof(IDocumentExtractionClient.ExtractAsync)} invoked.") && !entry.Message.Contains("mistral-ocr-4-0")),
                entry => Assert.True(entry.Message.Contains($"{nameof(IDocumentExtractionClient.ExtractAsync)} completed.") && !entry.Message.Contains("blue whale")));
        }
        else
        {
            Assert.Empty(logs);
        }
    }
}
