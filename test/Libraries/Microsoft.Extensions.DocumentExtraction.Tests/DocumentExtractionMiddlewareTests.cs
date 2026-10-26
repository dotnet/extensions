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
        DocumentExtractionClientBuilder builder = new(_ => new TestClient());
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
        await foreach (DocumentExtractionPage page in first.ExtractPagesAsync(input, "first", "application/pdf"))
        {
            Assert.Equal(1, page.PageNumber);
        }

        await foreach (DocumentExtractionPage page in second.ExtractPagesAsync(input, "second", "application/pdf"))
        {
            Assert.Equal(1, page.PageNumber);
        }

        Assert.Equal(firstWithoutServices ? 0 : 3, firstCollector.Count);
        Assert.Equal(3, secondCollector.Count);
        Assert.Equal(1, secondCollector.GetSnapshot().Count(entry => entry.Message == "Document extraction invoked."));
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
        DocumentExtractionClientBuilder builder = new(_ => new TestClient());
        builder.UseLogging(explicitFactory);

        using IDocumentExtractionClient first = builder.Build();
        using IDocumentExtractionClient second = builder.Build(services);
        using MemoryStream input = new();
        foreach (IDocumentExtractionClient client in new[] { first, second })
        {
            await foreach (DocumentExtractionPage page in client.ExtractPagesAsync(input, "document", "application/pdf"))
            {
                Assert.Equal(1, page.PageNumber);
            }
        }

        Assert.Equal(6, explicitCollector.Count);
        Assert.Equal(0, serviceCollector.Count);
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

    private sealed class TestLoggerFactory(FakeLogCollector collector) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new FakeLogger(collector, categoryName);
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose()
        {
        }
    }

    [Fact]
    public async Task LoggingMiddleware_LogsLifecycleOnlyAndDisposesStreamingEnumerator()
    {
        FakeLogCollector collector = new();
        FakeLogger logger = new(collector);
        using TestClient inner = new();
        using LoggingDocumentExtractionClient client = new(inner, logger);
        using MemoryStream source = new();

        await foreach (DocumentExtractionPage page in client.ExtractPagesAsync(
            source,
            "secret/path",
            "application/pdf"))
        {
            Assert.Equal(1, page.PageNumber);
        }

        string messages = string.Join(Environment.NewLine, collector.GetSnapshot().Select(static entry => entry.Message));
        Assert.Contains("Document extraction invoked.", messages);
        Assert.Contains("received page 1", messages);
        Assert.Contains("completed.", messages);
        Assert.DoesNotContain("secret text", messages);
        Assert.DoesNotContain("secret/path", messages);
        Assert.True(inner.EnumeratorDisposed);
    }

    [Fact]
    public async Task LoggingMiddleware_ForwardsCancellationAndDisposesEnumerator()
    {
        using CancellationTokenSource cancellationSource = new();
        using TestClient inner = new()
        {
            CancelAfterFirstPage = true
        };
        FakeLogger logger = new();
        using LoggingDocumentExtractionClient client = new(inner, logger);
        using MemoryStream source = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (DocumentExtractionPage _ in client.ExtractPagesAsync(
                source,
                "document",
                "application/pdf",
                cancellationToken: cancellationSource.Token))
            {
                cancellationSource.Cancel();
            }
        });

        Assert.Equal(cancellationSource.Token, inner.ReceivedToken);
        Assert.True(inner.EnumeratorDisposed);
    }

    [Fact]
    public async Task LoggingMiddleware_DoesNotLogExceptionMessages()
    {
        FakeLogCollector collector = new();
        using TestClient inner = new()
        {
            FailWithSecret = true
        };
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(collector));
        using MemoryStream source = new();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (DocumentExtractionPage page in client.ExtractPagesAsync(
                source,
                "document",
                "application/pdf"))
            {
                Assert.Equal(1, page.PageNumber);
            }
        });

        string messages = string.Join(Environment.NewLine, collector.GetSnapshot().Select(static entry => entry.Message));
        Assert.Contains("failed", messages);
        Assert.DoesNotContain("secret provider payload", messages);
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
        Assert.Equal(["wrapped"], disposed);
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

    private sealed class TestClient : IDocumentExtractionClient
    {
        internal bool CancelAfterFirstPage { get; set; }
        internal bool FailWithSecret { get; set; }
        internal CancellationToken ReceivedToken { get; private set; }
        internal bool EnumeratorDisposed { get; private set; }
        internal int DisposeCount { get; private set; }

        public IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
            Stream document,
            string identifier,
            string mediaType,
            DocumentExtractionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return PagesAsync(cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
            if (DisposeCount == 0)
            {
                DisposeCount++;
            }
        }

        private async IAsyncEnumerable<DocumentExtractionPage> PagesAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            try
            {
                yield return new DocumentExtractionPage(
                    1,
                    [new DocumentExtractionText("provider", 0, Microsoft.Extensions.Documents.DocumentTextKind.Paragraph, "stored", "secret text")]);
                await Task.Yield();
                if (FailWithSecret)
                {
                    throw new InvalidOperationException("secret provider payload");
                }

                if (CancelAfterFirstPage)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                EnumeratorDisposed = true;
            }
        }
    }

    private sealed class RecordingMiddleware : DelegatingDocumentExtractionClient
    {
        private readonly Action _disposed;

        internal RecordingMiddleware(IDocumentExtractionClient inner, Action disposed)
            : base(inner)
        {
            _disposed = disposed;
        }

        internal IDocumentExtractionClient Inner => InnerClient;
        internal int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
                _disposed();
            }

            base.Dispose(disposing);
        }
    }

    [Fact]
    public async Task LoggingMiddleware_RawBearingPageLogsOnlyExactLifecycleWithoutInspectingNative()
    {
        using MemoryStream source = new([42]);
        using MemoryStream body = new(System.Text.Encoding.UTF8.GetBytes(
            "{\"unknown\":{\"z\":[3,1]},\"marker\":\"RAW_BODY_ONLY_91\"}"));
        using DocumentArtifact artifact = new("application/json", body);
        using LoggingNative native = new();
        DocumentSourceOutput output = new("raw-operation", native, artifact);
        DocumentExtractionPage page = new(7,
            [new DocumentExtractionText("provider", 0, DocumentTextKind.Paragraph, "visible **text**", "visible text",
                sources: [new DocumentSourceReference(new DocumentSourcePage("vendor", 7, sourceOutput: output), rawRepresentation: native)],
                metadata: new Dictionary<string, object?> { ["vendor"] = native, ["Vendor"] = "METADATA_ONLY_94" })],
            sourceOutputs: [output]);
        using RawLoggingClient inner = new(page);
        FakeLogCollector collector = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(collector));
        using DocumentExtractionResult result = await client.ExtractAsync(source, "secret/path", "application/pdf",
            new DocumentExtractionOptions { CaptureRawContent = true });

        Assert.Equal(new[]
        {
            "Document extraction invoked.",
            "Document extraction received page 7 with 1 observations.",
            "Document extraction completed."
        }, collector.GetSnapshot().Select(static entry => entry.Message));
        Assert.All(collector.GetSnapshot(), static entry => Assert.Null(entry.Exception));
        Assert.All(collector.GetSnapshot(), static entry =>
        {
            Assert.NotNull(entry.StructuredState);
            string state = string.Join("|", entry.StructuredState.Select(static pair => pair.Key + "=" + pair.Value));
            Assert.DoesNotContain("RAW_BODY_ONLY_91", state);
            Assert.DoesNotContain("NATIVE_ONLY_92", state);
            Assert.DoesNotContain("secret/path", state);
            Assert.DoesNotContain("visible text", state);
            Assert.DoesNotContain("METADATA_ONLY_94", state);
        });
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Equal("visible **text**", result.Document.ToMarkdown());
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(1, inner.CleanupCount);
        result.Dispose();
        Assert.False(body.CanRead);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());
    }

    private sealed class RawLoggingClient(DocumentExtractionPage page) : IDocumentExtractionClient
    {
        internal int CleanupCount { get; private set; }
        internal Stream? Source { get; private set; }
        internal string? Identifier { get; private set; }
        internal string? MediaType { get; private set; }
        internal DocumentExtractionOptions? Options { get; private set; }
        internal CancellationToken Token { get; private set; }
        internal Exception? Failure { get; set; }
        internal Exception? CleanupFailure { get; set; }
        internal Action? OnSecondMove { get; set; }
        internal Action<DocumentExtractionOptions?>? OnInvoke { get; set; }
        internal Task? CompletionGate { get; set; }

        public IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
            Stream document, string identifier, string mediaType, DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            Source = document;
            Identifier = identifier;
            MediaType = mediaType;
            Options = options;
            Token = cancellationToken;
            OnInvoke?.Invoke(options);
            return new Sequence(this);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }

        private DocumentExtractionPage Page => page;

        private sealed class Sequence(RawLoggingClient owner) : IAsyncEnumerable<DocumentExtractionPage>, IAsyncEnumerator<DocumentExtractionPage>
        {
            private int _moves;
            public DocumentExtractionPage Current => owner.Page;
            public IAsyncEnumerator<DocumentExtractionPage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => this;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (++_moves == 1)
                {
                    return true;
                }

                if (owner.CompletionGate is { } gate)
                {
                    await gate;
                }

                owner.OnSecondMove?.Invoke();
                if (owner.Failure is { } failure)
                {
                    throw failure;
                }

                owner.Token.ThrowIfCancellationRequested();
                return false;
            }

            public ValueTask DisposeAsync()
            {
                owner.CleanupCount++;
                if (owner.CleanupFailure is { } failure)
                {
                    throw failure;
                }

                return default;
            }
        }
    }

    private sealed class LoggingNative : IDisposable
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logging_ExtractPagesAsync_ForwardsExactOptionsStreamAndToken(bool provideOptions)
    {
        using MemoryStream input = new([42]);
        using CancellationTokenSource cancellation = new();
        using LoggingNative native = new();
        DocumentSourceOutput output = new("operation", native, contentCaptureDisabled: true);
        DocumentExtractionPage page = LoggingPage(output, native);
        DocumentExtractionOptions? options = provideOptions ? new() { ModelId = "OPTIONS_ONLY_93" } : null;
        using RawLoggingClient inner = new(page);
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        List<DocumentExtractionPage> received = [];
        await foreach (DocumentExtractionPage item in client.ExtractPagesAsync(input, "secret/path", "application/pdf", options, cancellation.Token))
        {
            received.Add(item);
        }

        Assert.Same(page, Assert.Single(received));
        Assert.Same(input, inner.Source);
        Assert.Same(options, inner.Options);
        Assert.Equal("secret/path", inner.Identifier);
        Assert.Equal("application/pdf", inner.MediaType);
        Assert.Equal(cancellation.Token, inner.Token);
        Assert.Equal(1, inner.CleanupCount);
        AssertLifecycle(logs, "completed");
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Logging_DefaultOffNativeSourcesAndMetadata_PreserveIdentityWithoutLeaks(int optionKind)
    {
        using MemoryStream input = new([42]);
        using LoggingNative native = new();
        DocumentSourceOutput output = new("operation", native, contentCaptureDisabled: true);
        DocumentExtractionPage page = LoggingPage(output, native);
        using RawLoggingClient inner = new(page);
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        DocumentExtractionOptions? options = optionKind switch
        {
            1 => new(),
            2 => new() { CaptureRawContent = false, ModelId = "OPTIONS_ONLY_93" },
            _ => null
        };
        using DocumentExtractionResult result = optionKind == 0
            ? await client.ExtractAsync(input, "secret/path", "application/pdf")
            : await client.ExtractAsync(input, "secret/path", "application/pdf", options);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        DocumentElement element = Assert.Single(result.Document.EnumerateContent());
        DocumentSourceReference annotation = Assert.Single(element.Sources);
        Assert.Same(page.Observations[0].Sources[0], annotation);
        Assert.Same(native, annotation.GetRawRepresentation());
        Assert.Same(native, element.Metadata["vendor"]);
        Assert.Equal("METADATA_ONLY_94", element.Metadata["Vendor"]);
        Assert.False(element.Metadata.ContainsKey("VENDOR"));
        Assert.Equal("visible **text**", result.Document.ToMarkdown());
        Assert.Equal("visible text", Assert.IsType<DocumentTextElement>(element).Text);
        Assert.Equal("METADATA_ONLY_94", result.Document.Sections[0].Metadata["vendor"]);
        Assert.Null(output.GetRawContent());
        Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
        AssertLifecycle(logs, "completed");
        result.Dispose();
        Assert.Equal(1, inner.CleanupCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logging_ExtractAsync_ForwardsClonedSnapshotAndFrozenCaptureChoice(bool capture)
    {
        using MemoryStream input = new([42]);
        using LoggingNative native = new();
        using LoggingNative opaque = new();
        using LoggingStream body = new(LoggingBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        using RawLoggingClient inner = new(LoggingPage(output, native));
        TaskCompletionSource<bool> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        inner.CompletionGate = gate.Task;
        DocumentExtractionOptions options = new() { ModelId = "OPTIONS_ONLY_93", CaptureRawContent = capture };
        options.AdditionalProperties.Add("opaque", opaque);
        options.AdditionalProperties.Add("Opaque", "OPTIONS_ONLY_93");
        inner.OnInvoke = request =>
        {
            Assert.NotSame(options, request);
            Assert.Equal("OPTIONS_ONLY_93", request!.ModelId);
            Assert.Equal(capture, request.CaptureRawContent);
            Assert.Same(opaque, request.AdditionalProperties["opaque"]);
            Assert.Equal("OPTIONS_ONLY_93", request.AdditionalProperties["Opaque"]);
            request.CaptureRawContent = !capture;
        };
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        Task<DocumentExtractionResult> pending = client.ExtractAsync(input, "secret/path", "application/pdf", options);
        Assert.False(pending.IsCompleted);
        options.ModelId = "caller changed";
        options.AdditionalProperties.Clear();
        Assert.Equal("OPTIONS_ONLY_93", inner.Options!.ModelId);
        Assert.Equal(2, inner.Options.AdditionalProperties.Count);
        inner.Options.ModelId = "provider changed";
        inner.Options.AdditionalProperties.Clear();
        Assert.Equal("caller changed", options.ModelId);
        Assert.Empty(options.AdditionalProperties);
        Assert.Equal(capture, options.CaptureRawContent);
        gate.SetResult(true);
        if (capture)
        {
            using DocumentExtractionResult result = await pending;
            Assert.Same(artifact, Assert.Single(result.Document.SourceOutputs).GetRawContent());
            using MemoryStream copy = new();
            await artifact.Content.CopyToAsync(copy);
            Assert.Equal(LoggingBytes(), copy.ToArray());
        }
        else
        {
            ArgumentException? failure = null;
            try
            {
                using DocumentExtractionResult unexpected = await pending;
            }
            catch (ArgumentException caught)
            {
                failure = caught;
            }

            ArgumentException error = Assert.IsType<ArgumentException>(failure);
            Assert.Equal("pages", error.ParamName);
        }

        Assert.False(body.CanRead);
        Assert.Equal(1, body.DisposeCount);
        AssertLifecycle(logs, "completed");
        Assert.Equal(1, inner.CleanupCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, opaque.DisposeCount);
        Assert.Equal(0, opaque.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Fact]
    public async Task Logging_ExplicitCaptureSuccess_RetainsExactBodyWithoutLeaks()
    {
        using MemoryStream input = new([42]);
        using LoggingNative native = new();
        using LoggingStream body = new(LoggingBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        using RawLoggingClient inner = new(LoggingPage(output, native));
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        using DocumentExtractionResult result = await client.ExtractAsync(input, "secret/path", "application/pdf",
            new DocumentExtractionOptions { CaptureRawContent = true, ModelId = "OPTIONS_ONLY_93" });
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        using MemoryStream copy = new();
        await artifact.Content.CopyToAsync(copy);
        Assert.Equal(LoggingBytes(), copy.ToArray());
        AssertLifecycle(logs, "completed");
        result.Dispose();
        Assert.False(body.CanRead);
        Assert.Equal(1, body.DisposeCount);
        Assert.Equal(1, inner.CleanupCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Fact]
    public Task Logging_Failure_PreservesOriginalExceptionAndDoesNotLeakContent() =>
        VerifyLoggingFailure(cancel: false, throwingDisposal: false);

    [Fact]
    public Task Logging_Cancellation_PreservesTokenAndDoesNotLeakContent() =>
        VerifyLoggingFailure(cancel: true, throwingDisposal: false);

    [Fact]
    public Task Logging_EnumerationFailureWithThrowingEnumeratorDisposal_PreservesPrimaryError() =>
        VerifyLoggingFailure(cancel: false, throwingDisposal: true);

    [Fact]
    public Task Logging_CancellationWithThrowingEnumeratorDisposal_PreservesPrimaryToken() =>
        VerifyLoggingFailure(cancel: true, throwingDisposal: true);

    private static async Task VerifyLoggingFailure(bool cancel, bool throwingDisposal)
    {
        using MemoryStream input = new([42]);
        using LoggingNative native = new();
        using LoggingStream body = new(LoggingBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        using CancellationTokenSource cancellation = new();
        Exception primary = cancel
            ? new OperationCanceledException("RAW_BODY_ONLY_91 NATIVE_ONLY_92 OPTIONS_ONLY_93 secret/path", cancellation.Token)
            : new InvalidOperationException("RAW_BODY_ONLY_91 NATIVE_ONLY_92 OPTIONS_ONLY_93 secret/path");
        IOException cleanup = new("RAW_BODY_ONLY_91 cleanup");
        using RawLoggingClient inner = new(LoggingPage(output, native))
        {
            Failure = primary,
            CleanupFailure = throwingDisposal ? cleanup : null,
            OnSecondMove = cancel ? cancellation.Cancel : null
        };
        FakeLogCollector logs = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(logs));
        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() => client.ExtractAsync(
            input, "secret/path", "application/pdf",
            new DocumentExtractionOptions { CaptureRawContent = true, ModelId = "OPTIONS_ONLY_93" }, cancellation.Token));

        AssertLifecycle(logs, cancel ? "canceled" : "failed");
        Assert.Equal(1, inner.CleanupCount);
        Assert.False(body.CanRead);
        Assert.Equal(1, body.DisposeCount);
        Assert.Equal(cancellation.Token, inner.Token);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, input.ReadByte());
        if (throwingDisposal)
        {
            Assert.Equal(new[] { primary, cleanup }, Assert.IsType<AggregateException>(actual).Flatten().InnerExceptions);
        }
        else
        {
            Assert.Same(primary, actual);
        }

        if (cancel)
        {
            Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(primary).CancellationToken);
        }
    }

    private static DocumentExtractionPage LoggingPage(DocumentSourceOutput output, LoggingNative native) =>
        new(7, [new DocumentExtractionText("provider", 0, DocumentTextKind.Paragraph, "visible **text**", "visible text",
            sources: [new DocumentSourceReference(new DocumentSourcePage("vendor", 7, sourceOutput: output), rawRepresentation: native)],
            metadata: new Dictionary<string, object?> { ["vendor"] = native, ["Vendor"] = "METADATA_ONLY_94" })],
            sourceOutputs: [output], metadata: new Dictionary<string, object?> { ["vendor"] = "METADATA_ONLY_94" });

    private static byte[] LoggingBytes() => System.Text.Encoding.UTF8.GetBytes(
        " {\r\n \"unknown\": {\"z\":[3, 1]}, \"marker\" : \"RAW_BODY_ONLY_91\" \r\n}\n");

    private sealed class LoggingStream(byte[] bytes) : MemoryStream(bytes)
    {
        internal int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
        }
    }

    private static void AssertLifecycle(FakeLogCollector collector, string ending)
    {
        var entries = collector.GetSnapshot();
        Assert.Equal(new[]
        {
            "Document extraction invoked.",
            "Document extraction received page 7 with 1 observations.",
            "Document extraction " + ending + "."
        }, entries.Select(static entry => entry.Message));
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Assert.Null(entry.Exception);
            Assert.Equal(ending == "failed" && i == 2 ? LogLevel.Error : LogLevel.Debug, entry.Level);
            var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, string?>>>(entry.StructuredState);
            Assert.Equal(i == 1 ? 3 : 1, state.Count);
            Assert.Equal(i == 1
                ? "Document extraction received page {PageNumber} with {ObservationCount} observations."
                : entry.Message, state.Single(static pair => pair.Key == "{OriginalFormat}").Value);
            if (i == 1)
            {
                Assert.Equal("7", state.Single(static pair => pair.Key == "PageNumber").Value);
                Assert.Equal("1", state.Single(static pair => pair.Key == "ObservationCount").Value);
            }

            foreach (string marker in new[] { "RAW_BODY_ONLY_91", "NATIVE_ONLY_92", "OPTIONS_ONLY_93", "METADATA_ONLY_94", "secret/path", "visible text", "visible **text**" })
            {
                Assert.DoesNotContain(marker, entry.Message);
                Assert.All(state, pair =>
                {
                    Assert.DoesNotContain(marker, pair.Key);
                    Assert.DoesNotContain(marker, pair.Value ?? "");
                });
            }
        }
    }
}
