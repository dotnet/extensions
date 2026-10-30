// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction.Tests;

public sealed class DelegatingDocumentExtractionClientTests
{
    [Fact]
    public void Options_DefaultsAndClonePreserveBorrowedValuesWithIndependentContainers()
    {
        using Native opaque = new();
        DocumentExtractionOptions options = new();
        Assert.Null(options.ModelId);
        Assert.False(options.CaptureRawContent);
        Assert.Empty(options.AdditionalProperties);
        options.ModelId = "model:A";
        options.CaptureRawContent = true;
        options.AdditionalProperties.Add("setting", opaque);
        options.AdditionalProperties.Add("Setting", null);

        DocumentExtractionOptions copy = options.Clone();
        Assert.NotSame(options, copy);
        Assert.NotSame(options.AdditionalProperties, copy.AdditionalProperties);
        Assert.Equal("model:A", copy.ModelId);
        Assert.True(copy.CaptureRawContent);
        Assert.Same(opaque, copy.AdditionalProperties["setting"]);
        Assert.Null(copy.AdditionalProperties["Setting"]);
        Assert.False(copy.AdditionalProperties.ContainsKey("SETTING"));
        options.AdditionalProperties.Clear();
        options.ModelId = null;
        options.CaptureRawContent = false;
        Assert.Equal(2, copy.AdditionalProperties.Count);
        Assert.Equal("model:A", copy.ModelId);
        Assert.True(copy.CaptureRawContent);
        copy.AdditionalProperties["setting"] = "replacement";
        Assert.Empty(options.AdditionalProperties);
        Assert.Equal(0, opaque.DisposeCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Options_InvalidModelDoesNotReplacePriorValue(string invalid)
    {
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = true };
        Assert.Throws<ArgumentException>("value", () => options.ModelId = invalid);
        Assert.Equal("model:A", options.ModelId);
        Assert.True(options.CaptureRawContent);
        options.ModelId = null;
        Assert.Null(options.ModelId);
    }

    [Fact]
    public async Task DelegatingClient_ForwardsArgumentsAndReturnsExactDocument()
    {
        using MemoryStream input = new([42, 43]);
        using CancellationTokenSource cancellation = new();
        using Document expected = new("urn:document");
        DocumentSection section = new("provider section");
        section.Elements.Add(new DocumentTextElement("**body**", "plain body", "heading") { HeadingLevel = 3 });
        expected.Sections.Add(section);
        using Client inner = new((_, _, _, _, _) => Task.FromResult(expected));
        using Delegating wrapper = new(inner);

        Document actual = await wrapper.ExtractAsync(input, "urn:document", "application/pdf", cancellationToken: cancellation.Token);

        Assert.Same(expected, actual);
        Assert.Equal("urn:document", actual.Identifier);
        Assert.Equal("**body**", actual.ToMarkdown());
        Assert.Equal(1, inner.CallCount);
        Assert.Same(input, inner.Source);
        Assert.Equal("urn:document", inner.Identifier);
        Assert.Equal("application/pdf", inner.MediaType);
        Assert.Equal(cancellation.Token, inner.Token);
        Assert.Null(inner.Options);
        wrapper.Dispose();
        wrapper.Dispose();
        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal("**body**", actual.ToMarkdown());
        Assert.Equal(42, input.ReadByte());
    }

    [Fact]
    public void DelegatingClient_ValidatesArgumentsBeforeCallingInner()
    {
        using MemoryStream input = new();
        using Client inner = new((_, _, _, _, _) => throw new InvalidOperationException("Must not call provider."));
        using Delegating wrapper = new(inner);
        Assert.Throws<ArgumentNullException>("document", () => { _ = wrapper.ExtractAsync(null!, "document", "application/pdf"); });
        Assert.Throws<ArgumentNullException>("identifier", () => { _ = wrapper.ExtractAsync(input, null!, "application/pdf"); });
        Assert.Throws<ArgumentException>("identifier", () => { _ = wrapper.ExtractAsync(input, "", "application/pdf"); });
        Assert.Throws<ArgumentNullException>("mediaType", () => { _ = wrapper.ExtractAsync(input, "document", null!); });
        Assert.Throws<ArgumentException>("mediaType", () => { _ = wrapper.ExtractAsync(input, "document", ""); });
        Assert.Equal(0, inner.CallCount);
        Assert.True(input.CanRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelegatingClient_SnapshotsOptionsSynchronouslyWithoutMutatingEitherContainer(bool capture)
    {
        using MemoryStream input = new([42]);
        using Native opaque = new();
        using Document expected = new("document");
        using SemaphoreSlim completion = new(0, 1);
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = capture };
        options.AdditionalProperties.Add("setting", opaque);
        options.AdditionalProperties.Add("Setting", null);
        using Client inner = new(async (_, _, _, _, _) =>
        {
            await completion.WaitAsync(CancellationToken.None);
            return expected;
        });
        using Delegating wrapper = new(inner);

        Task<Document> pending = wrapper.ExtractAsync(input, "document", "application/pdf", options);
        DocumentExtractionOptions forwarded = Assert.IsType<DocumentExtractionOptions>(inner.Options);
        Assert.NotSame(options, forwarded);
        Assert.NotSame(options.AdditionalProperties, forwarded.AdditionalProperties);
        Assert.Equal("model:A", forwarded.ModelId);
        Assert.Equal(capture, forwarded.CaptureRawContent);
        Assert.Same(opaque, forwarded.AdditionalProperties["setting"]);
        Assert.Null(forwarded.AdditionalProperties["Setting"]);
        Assert.False(forwarded.AdditionalProperties.ContainsKey("SETTING"));
        Assert.False(pending.IsCompleted);

        options.ModelId = "caller:B";
        options.CaptureRawContent = !capture;
        options.AdditionalProperties.Clear();
        Assert.Equal("model:A", forwarded.ModelId);
        Assert.Equal(capture, forwarded.CaptureRawContent);
        Assert.Equal(2, forwarded.AdditionalProperties.Count);
        forwarded.ModelId = "provider:C";
        forwarded.CaptureRawContent = !capture;
        forwarded.AdditionalProperties.Clear();
        Assert.Equal("caller:B", options.ModelId);
        Assert.Equal(!capture, options.CaptureRawContent);
        Assert.Empty(options.AdditionalProperties);
        completion.Release();
        Assert.Same(expected, await pending);
        wrapper.Dispose();
        Assert.Equal(0, opaque.DisposeCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Fact]
    public async Task DelegatingClient_AbsentOptionsStayAbsent()
    {
        using MemoryStream input = new([42]);
        using Document expected = new("document");
        using Client inner = new((_, _, _, _, _) => Task.FromResult(expected));
        using Delegating wrapper = new(inner);
        Assert.Same(expected, await wrapper.ExtractAsync(input, "first", "application/pdf"));
        Assert.Null(inner.Options);
        DocumentExtractionOptions explicitOptions = new() { ModelId = "explicit" };
        Assert.Same(expected, await wrapper.ExtractAsync(input, "second", "application/pdf", explicitOptions));
        Assert.NotSame(explicitOptions, inner.Options);
        Assert.Equal("explicit", inner.Options!.ModelId);
        Assert.Same(expected, await wrapper.ExtractAsync(input, "third", "application/pdf", null));
        Assert.Null(inner.Options);
        Assert.Equal(3, inner.CallCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelegatingClient_PreservesFailureAndCancellationIdentity(bool cancel)
    {
        using MemoryStream input = new([42]);
        using CancellationTokenSource cancellation = new();
        Exception primary = cancel ? new OperationCanceledException(cancellation.Token) : new IOException("provider failure");
        using Client inner = new((_, _, _, _, _) => Task.FromException<Document>(primary));
        using Delegating wrapper = new(inner);
        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() =>
            wrapper.ExtractAsync(input, "document", "application/pdf", cancellationToken: cancellation.Token));
        Assert.Same(primary, actual);
        Assert.Equal(cancellation.Token, inner.Token);
        Assert.Equal(1, inner.CallCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Fact]
    public void DelegatingClient_GetServiceAndDisposePreserveFallbackKeys()
    {
        using Client inner = new((_, _, _, _, _) => throw new NotSupportedException());
        using Delegating wrapper = new(inner);
        Assert.Same(wrapper, wrapper.GetService(typeof(IDocumentExtractionClient)));
        Assert.Same(wrapper, wrapper.GetService(typeof(Delegating)));
        Assert.Equal(0, inner.ServiceCount);
        object key = new();
        Assert.Same(inner.Service, wrapper.GetService(typeof(Delegating), key));
        Assert.Equal(typeof(Delegating), inner.ServiceType);
        Assert.Same(key, inner.ServiceKey);
        Assert.Same(inner.Service, wrapper.GetService(typeof(Stream)));
        Assert.Equal(typeof(Stream), inner.ServiceType);
        Assert.Null(inner.ServiceKey);
        Assert.Equal(2, inner.ServiceCount);
        Assert.Throws<ArgumentNullException>("serviceType", () => wrapper.GetService(null!));
        Assert.Throws<ArgumentNullException>("innerClient", () => new Delegating(null!));
        Assert.Equal(2, inner.ServiceCount);
        wrapper.Dispose();
        wrapper.Dispose();
        Assert.Equal(1, inner.DisposeCount);
    }

    private sealed class Delegating(IDocumentExtractionClient inner) : DelegatingDocumentExtractionClient(inner);

    private sealed class Native : IDisposable
    {
        internal int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    private sealed class Client(Func<Stream, string, string, DocumentExtractionOptions?, CancellationToken, Task<Document>> extract)
        : IDocumentExtractionClient
    {
        internal int CallCount { get; private set; }
        internal Stream? Source { get; private set; }
        internal string? Identifier { get; private set; }
        internal string? MediaType { get; private set; }
        internal DocumentExtractionOptions? Options { get; private set; }
        internal CancellationToken Token { get; private set; }
        internal int DisposeCount { get; private set; }
        internal object Service { get; } = new();
        internal int ServiceCount { get; private set; }
        internal Type? ServiceType { get; private set; }
        internal object? ServiceKey { get; private set; }

        public Task<Document> ExtractAsync(Stream document, string identifier, string mediaType,
            DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            CallCount++;
            Source = document;
            Identifier = identifier;
            MediaType = mediaType;
            Options = options;
            Token = cancellationToken;
            return extract(document, identifier, mediaType, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            ServiceCount++;
            ServiceType = serviceType;
            ServiceKey = serviceKey;
            return Service;
        }

        public void Dispose() => DisposeCount++;
    }
}
