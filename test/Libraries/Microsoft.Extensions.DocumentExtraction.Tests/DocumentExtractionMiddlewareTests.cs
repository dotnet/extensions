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
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction.Tests;

public sealed class DocumentExtractionMiddlewareTests
{
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
                annotations: [new DocumentAnnotation("vendor", sourceOutput: output, rawRepresentation: native)])],
            sourceOutputs: [output]);
        using RawLoggingClient inner = new(page);
        FakeLogCollector collector = new();
        using LoggingDocumentExtractionClient client = new(inner, new FakeLogger(collector));
        using DocumentExtractionResult result = await client.ExtractAsync(source, "secret/path", "application/pdf");

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

        public IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
            Stream document, string identifier, string mediaType, DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
            => Pages();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }

        private async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            try
            {
                yield return page;
                await Task.CompletedTask;
            }
            finally
            {
                CleanupCount++;
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
}
