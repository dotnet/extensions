// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction.Tests;

public sealed class DocumentExtractionMiddlewareTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UseLogging_ResolvesFactoryIndependentlyForEachBuild(bool firstWithoutServices)
    {
        FakeLogCollector firstCollector = new();
        FakeLogCollector secondCollector = new();
        using TestLoggerFactory firstFactory = new(firstCollector);
        using TestLoggerFactory secondFactory = new(secondCollector);
        using ServiceProvider firstServices = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(firstFactory).BuildServiceProvider();
        using ServiceProvider secondServices = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(secondFactory).BuildServiceProvider();
        using Document expected = new("document");
        DocumentExtractionClientBuilder builder = new(_ => new TestClient(expected));
        builder.UseLogging();

        using IDocumentExtractionClient first = builder.Build(firstWithoutServices ? null : firstServices);
        using IDocumentExtractionClient second = builder.Build(secondServices);
        if (firstWithoutServices)
        {
            Assert.IsType<TestClient>(first);
        }
        else
        {
            Assert.IsType<LoggingDocumentExtractionClient>(first);
        }

        Assert.IsType<LoggingDocumentExtractionClient>(second);
        using MemoryStream input = new();
        Assert.Same(expected, await first.ExtractAsync(input, "first", "application/pdf"));
        Assert.Same(expected, await second.ExtractAsync(input, "second", "application/pdf"));
        Assert.Equal(firstWithoutServices ? 0 : 2, firstCollector.Count);
        Assert.Equal(2, secondCollector.Count);
        AssertLifecycle(secondCollector, "completed");
    }

    [Fact]
    public async Task UseLogging_ExplicitFactoryOverridesEachServiceProvider()
    {
        FakeLogCollector explicitCollector = new();
        FakeLogCollector serviceCollector = new();
        using TestLoggerFactory explicitFactory = new(explicitCollector);
        using TestLoggerFactory serviceFactory = new(serviceCollector);
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(serviceFactory).BuildServiceProvider();
        using Document expected = new("document");
        DocumentExtractionClientBuilder builder = new(_ => new TestClient(expected));
        builder.UseLogging(explicitFactory);
        using IDocumentExtractionClient first = builder.Build();
        using IDocumentExtractionClient second = builder.Build(services);
        using MemoryStream input = new();
        Assert.Same(expected, await first.ExtractAsync(input, "first", "application/pdf"));
        Assert.Same(expected, await second.ExtractAsync(input, "second", "application/pdf"));
        Assert.Equal(4, explicitCollector.Count);
        Assert.Equal(0, serviceCollector.Count);
        Assert.Equal(2, explicitCollector.GetSnapshot().Count(static entry => entry.Message == "Document extraction completed."));
    }

    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void FactoryRegistration_ScopesOwnIndependentClients(ServiceLifetime lifetime)
    {
        List<TestClient> clients = [];
        ServiceCollection services = new();
        services.AddDocumentExtractionClient(_ =>
        {
            TestClient client = new();
            clients.Add(client);
            return client;
        }, lifetime).Use(inner => new RecordingMiddleware(inner, () => { }));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        IDocumentExtractionClient first = firstScope.ServiceProvider.GetRequiredService<IDocumentExtractionClient>();
        IDocumentExtractionClient second = secondScope.ServiceProvider.GetRequiredService<IDocumentExtractionClient>();
        Assert.NotSame(first, second);
        Assert.Equal(2, clients.Count);
        Assert.NotSame(clients[0], clients[1]);
        firstScope.Dispose();
        Assert.Equal(1, clients[0].DisposeCount);
        Assert.Equal(0, clients[1].DisposeCount);
        secondScope.Dispose();
        Assert.Equal(1, clients[1].DisposeCount);
    }

    [Fact]
    public void BuilderAndDependencyInjection_PreserveConfiguredLifetimeAndMiddlewareOrder()
    {
        using TestClient inner = new();
        DocumentExtractionClientBuilder builder = new(inner);
        List<string> disposed = [];
        builder.Use((client, _) => new RecordingMiddleware(client, () => disposed.Add("first")));
        builder.Use((client, _) => new RecordingMiddleware(client, () => disposed.Add("second")));
        using (IDocumentExtractionClient client = builder.Build())
        {
            Assert.IsType<RecordingMiddleware>(client);
            Assert.IsType<RecordingMiddleware>(((RecordingMiddleware)client).Inner);
        }

        Assert.Equal(new[] { "first", "second" }, disposed);
        Assert.Equal(1, inner.DisposeCount);
        ServiceCollection services = new();
        using TestClient scopedClient = new();
        services.AddDocumentExtractionClient(scopedClient, ServiceLifetime.Scoped);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService<IDocumentExtractionClient>(), scope.ServiceProvider.GetRequiredService<IDocumentExtractionClient>());
    }

    [Fact]
    public void BuilderFailure_DisposesPartiallyBuiltPipeline()
    {
        using TestClient inner = new();
        List<string> disposed = [];
        DocumentExtractionClientBuilder builder = new(inner);
        builder.Use(_ => throw new InvalidOperationException("Factory failed."));
        builder.Use(client => new RecordingMiddleware(client, () => disposed.Add("wrapped")));
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal(new[] { "wrapped" }, disposed);
        Assert.Equal(1, inner.DisposeCount);
        Assert.Throws<ArgumentNullException>(() => new DocumentExtractionClientBuilder((IDocumentExtractionClient)null!));
    }

    [Fact]
    public void KeyedRegistration_ForwardsServicesAndOwnsPipeline()
    {
        using TestClient inner = new();
        ServiceCollection services = new();
        services.AddSingleton(new FakeLogger());
        services.AddKeyedDocumentExtractionClient("key", provider =>
        {
            Assert.NotNull(provider.GetRequiredService<FakeLogger>());
            return inner;
        }).Use(client => new RecordingMiddleware(client, () => { }));
        using ServiceProvider provider = services.BuildServiceProvider();
        IDocumentExtractionClient client = provider.GetRequiredKeyedService<IDocumentExtractionClient>("key");
        Assert.Same(client, provider.GetRequiredKeyedService<IDocumentExtractionClient>("key"));
        Assert.Same(client, client.GetService(typeof(RecordingMiddleware)));
        Assert.Null(client.GetService(typeof(RecordingMiddleware), "different"));
        provider.Dispose();
        Assert.Equal(1, inner.DisposeCount);
    }

    [Fact]
    public async Task Logging_AwaitsDocumentAndLogsOnlyExactLifecycle()
    {
        using MemoryStream input = new([42]);
        using Native native = new();
        CountingStream body = new(OriginalBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        using Document expected = new("secret/path");
        expected.AddSourceOutput(output);
        DocumentSection section = new("");
        section.Elements.Add(new DocumentImageElement("visible **text**", "visible text"));
        expected.Sections.Add(section);
        using SemaphoreSlim gate = new(0, 1);
        using TestClient inner = new(expected)
        {
            Extract = async () =>
            {
                await gate.WaitAsync();
                return expected;
            }
        };
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        DocumentExtractionOptions options = new() { ModelId = "OPTIONS_ONLY_93", CaptureRawContent = true };
        options.AdditionalProperties.Add("native", native);
        Task<Document> pending = client.ExtractAsync(input, "secret/path", "application/pdf", options);
        Assert.False(pending.IsCompleted);
        Assert.Equal(new[] { "Document extraction invoked." }, logs.GetSnapshot().Select(static entry => entry.Message));
        gate.Release();

        Document actual = await pending;

        Assert.Same(expected, actual);
        Assert.Equal("visible **text**", actual.ToMarkdown());
        Assert.Same(output, Assert.Single(actual.SourceOutputs));
        Assert.Equal(OriginalBytes(), body.ToArray());
        AssertLifecycle(logs, "completed");
        client.Dispose();
        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(0, body.DisposeCount);
        actual.Dispose();
        Assert.Equal(1, body.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Logging_FailureAndCancellationPreservePrimaryWithoutPayloads(bool cancel, bool synchronous)
    {
        using MemoryStream input = new([42]);
        using CancellationTokenSource cancellation = new();
        Exception primary = cancel
            ? new OperationCanceledException("SECRET_EXCEPTION_PAYLOAD", cancellation.Token)
            : new IOException("SECRET_EXCEPTION_PAYLOAD");
        using TestClient inner = new()
        {
            Extract = () => synchronous ? throw primary : Task.FromException<Document>(primary)
        };
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() =>
            client.ExtractAsync(input, "secret/path", "application/pdf", cancellationToken: cancellation.Token));
        Assert.Same(primary, actual);
        Assert.Equal(cancellation.Token, inner.Token);
        Assert.Same(input, inner.Source);
        Assert.Equal(1, inner.CallCount);
        if (cancel)
        {
            Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(actual).CancellationToken);
        }

        AssertLifecycle(logs, cancel ? "canceled" : "failed");
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logging_SnapshotsOptionsBeforeReturningWithoutMutatingCaller(bool capture)
    {
        using MemoryStream input = new([42]);
        using Native native = new();
        using CancellationTokenSource cancellation = new();
        using Document expected = new("document");
        using SemaphoreSlim gate = new(0, 1);
        using TestClient inner = new(expected)
        {
            Extract = async () =>
            {
                await gate.WaitAsync();
                return expected;
            }
        };
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        DocumentExtractionOptions options = new() { ModelId = "OPTIONS_ONLY_93", CaptureRawContent = capture };
        options.AdditionalProperties.Add("native", native);
        Task<Document> pending = client.ExtractAsync(input, "secret/path", "application/pdf", options, cancellation.Token);
        DocumentExtractionOptions forwarded = Assert.IsType<DocumentExtractionOptions>(inner.Options);
        Assert.NotSame(options, forwarded);
        Assert.NotSame(options.AdditionalProperties, forwarded.AdditionalProperties);
        Assert.Equal("OPTIONS_ONLY_93", forwarded.ModelId);
        Assert.Equal(capture, forwarded.CaptureRawContent);
        Assert.Same(native, forwarded.AdditionalProperties["native"]);
        Assert.Same(input, inner.Source);
        Assert.Equal("secret/path", inner.Identifier);
        Assert.Equal("application/pdf", inner.MediaType);
        Assert.Equal(cancellation.Token, inner.Token);
        options.ModelId = "caller changed";
        options.CaptureRawContent = !capture;
        options.AdditionalProperties.Clear();
        Assert.Equal("OPTIONS_ONLY_93", forwarded.ModelId);
        Assert.Equal(capture, forwarded.CaptureRawContent);
        forwarded.ModelId = "provider changed";
        forwarded.AdditionalProperties.Clear();
        Assert.Equal("caller changed", options.ModelId);
        Assert.Equal(!capture, options.CaptureRawContent);
        Assert.Empty(options.AdditionalProperties);
        gate.Release();
        Assert.Same(expected, await pending);
        AssertLifecycle(logs, "completed");
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Fact]
    public async Task Logging_DefaultOffNativeSourcesPreserveIdentityWithoutLeaks()
    {
        using MemoryStream input = new([42]);
        using Native native = new();
        using Document expected = new("document");
        DocumentSourceOutput output = new("operation", native, contentCaptureDisabled: true);
        DocumentSourceReference reference = new(new DocumentSourcePage("frame", 7, sourceOutput: output), rawRepresentation: native);
        DocumentImageElement image = new("visible **text**", "visible text");
        image.Sources.Add(reference);
        DocumentSection section = new("");
        section.Elements.Add(image);
        expected.Sections.Add(section);
        using TestClient inner = new(expected);
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));

        Document actual = await client.ExtractAsync(input, "secret/path", "application/pdf");

        Assert.Same(expected, actual);
        Assert.Null(inner.Options);
        Assert.Same(output, Assert.Single(actual.SourceOutputs));
        Assert.Same(reference, Assert.Single(image.Sources));
        Assert.Same(native, reference.GetRawRepresentation());
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
        Assert.Null(output.GetRawContent());
        Assert.Equal("visible **text**", actual.ToMarkdown());
        AssertLifecycle(logs, "completed");
        actual.Dispose();
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logging_CompletionLogFailureDisposesUnreturnedDocumentAndKeepsPrimaryFirst(bool throwingCleanup)
    {
        using MemoryStream input = new([42]);
        using Native native = new();
        IOException logFailure = new("completion logger failed");
        IOException firstCleanup = new("first body cleanup failed");
        IOException lastCleanup = new("last body cleanup failed");
        CountingStream first = new(OriginalBytes(), throwingCleanup ? firstCleanup : null);
        CountingStream middle = new([17, 23]);
        CountingStream last = new([31, 37], throwingCleanup ? lastCleanup : null);
        using DocumentArtifact firstBody = new("application/json", first);
        using DocumentArtifact middleBody = new("application/json", middle);
        using DocumentArtifact lastBody = new("application/json", last);
        using Document unreturned = new("secret/path");
        unreturned.AddSourceOutput(new("first", native, firstBody));
        unreturned.AddSourceOutput(new("middle", rawContent: middleBody));
        unreturned.AddSourceOutput(new("last", rawContent: lastBody));
        using TestClient inner = new(unreturned);
        ThrowingLogger logger = new("Document extraction completed.", logFailure);
        using LoggingDocumentExtractionClient client = new(inner, logger);

        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() =>
            client.ExtractAsync(input, "secret/path", "application/pdf", new DocumentExtractionOptions { CaptureRawContent = true }));

        if (throwingCleanup)
        {
            Assert.Equal(new Exception[] { logFailure, firstCleanup, lastCleanup },
                Assert.IsType<AggregateException>(actual).Flatten().InnerExceptions);
        }
        else
        {
            Assert.Same(logFailure, actual);
        }

        Assert.Equal(new[] { "Document extraction invoked.", "Document extraction completed." }, logger.Messages);
        Assert.Equal(1, inner.CallCount);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, middle.DisposeCount);
        Assert.Equal(1, last.DisposeCount);
        Assert.False(first.CanRead);
        Assert.False(middle.CanRead);
        Assert.False(last.CanRead);
        unreturned.Dispose();
        client.Dispose();
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, middle.DisposeCount);
        Assert.Equal(1, last.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Logging_FailureLogFailurePreservesProviderPrimaryAndCancellationToken(bool cancel, bool synchronous)
    {
        using MemoryStream input = new([42]);
        using Native native = new();
        using CancellationTokenSource cancellation = new();
        Exception primary = cancel
            ? new OperationCanceledException("SECRET_EXCEPTION_PAYLOAD", cancellation.Token)
            : new IOException("SECRET_EXCEPTION_PAYLOAD");
        IOException loggingFailure = new("terminal logger failed");
        string terminal = cancel ? "Document extraction canceled." : "Document extraction failed.";
        ThrowingLogger logger = new(terminal, loggingFailure);
        using TestClient inner = new()
        {
            Extract = () => synchronous ? throw primary : Task.FromException<Document>(primary)
        };
        using LoggingDocumentExtractionClient client = new(inner, logger);
        DocumentExtractionOptions options = new();
        options.AdditionalProperties.Add("native", native);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            client.ExtractAsync(input, "secret/path", "application/pdf", options, cancellation.Token));

        Assert.Equal(new[] { primary, loggingFailure }, actual.InnerExceptions);
        Assert.Same(primary, actual.InnerExceptions[0]);
        Assert.Equal(new[] { "Document extraction invoked.", terminal }, logger.Messages);
        Assert.Equal(cancellation.Token, inner.Token);
        Assert.Same(input, inner.Source);
        Assert.Equal(1, inner.CallCount);
        if (cancel)
        {
            Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(actual.InnerExceptions[0]).CancellationToken);
        }

        Assert.Same(native, options.AdditionalProperties["native"]);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    private sealed class ThrowingLogger(string terminal, Exception failure) : ILogger
    {
        internal List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            string message = formatter(state, exception);
            Messages.Add(message);
            if (message == terminal)
            {
                throw failure;
            }
        }
    }

    private static void AssertLifecycle(FakeLogCollector collector, string ending)
    {
        var entries = collector.GetSnapshot();
        Assert.Equal(new[] { "Document extraction invoked.", "Document extraction " + ending + "." },
            entries.Select(static entry => entry.Message));
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Assert.Null(entry.Exception);
            Assert.Equal(ending == "failed" && i == 1 ? LogLevel.Error : LogLevel.Debug, entry.Level);
            var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, string?>>>(entry.StructuredState);
            KeyValuePair<string, string?> originalFormat = Assert.Single(state);
            Assert.Equal("{OriginalFormat}", originalFormat.Key);
            Assert.Equal(entry.Message, originalFormat.Value);
            foreach (string marker in new[] { "RAW_BODY_ONLY_91", "NATIVE_ONLY_92", "OPTIONS_ONLY_93", "secret/path", "visible text", "visible **text**", "SECRET_EXCEPTION_PAYLOAD" })
            {
                Assert.DoesNotContain(marker, entry.Message);
                Assert.DoesNotContain(marker, originalFormat.Value ?? "");
            }
        }
    }

    private static byte[] OriginalBytes() => System.Text.Encoding.UTF8.GetBytes(
        " {\r\n \"unknown\" : {\"z\":[3,1]}, \"marker\":\"RAW_BODY_ONLY_91\" \r\n}\n");

    private sealed class TestClient(Document? result = null) : IDocumentExtractionClient
    {
        internal Func<Task<Document>>? Extract { get; set; }
        internal Stream? Source { get; private set; }
        internal string? Identifier { get; private set; }
        internal string? MediaType { get; private set; }
        internal DocumentExtractionOptions? Options { get; private set; }
        internal CancellationToken Token { get; private set; }
        internal int CallCount { get; private set; }
        internal int DisposeCount { get; private set; }
        public Task<Document> ExtractAsync(Stream document, string identifier, string mediaType,
            DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            Source = document;
            Identifier = identifier;
            MediaType = mediaType;
            Options = options;
            Token = cancellationToken;
            CallCount++;
            if (Extract is { } extract)
            {
                return extract();
            }

            return result is { } actual ? Task.FromResult(actual) :
                Task.FromException<Document>(new InvalidOperationException("No fake result configured."));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() => DisposeCount++;
    }

    private sealed class RecordingMiddleware(IDocumentExtractionClient inner, Action disposed) : DelegatingDocumentExtractionClient(inner)
    {
        internal IDocumentExtractionClient Inner => InnerClient;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                disposed();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class TestLoggerFactory(FakeLogCollector collector) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new FakeLogger(collector, categoryName);
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose()
        {
        }
    }

    private sealed class Native : IDisposable
    {
        internal int DisposeCount { get; private set; }
        internal int ToStringCount { get; private set; }
        public void Dispose() => DisposeCount++;
        public override string ToString()
        {
            ToStringCount++;
            return "NATIVE_ONLY_92";
        }
    }

    private sealed class CountingStream(byte[] bytes, Exception? failure = null) : MemoryStream(bytes)
    {
        internal int DisposeCount { get; private set; }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
            if (disposing && failure is not null)
            {
                throw failure;
            }
        }
    }
}
