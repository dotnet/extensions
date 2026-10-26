// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        DocumentExtractionOptions defaults = new();
        Assert.Null(defaults.ModelId);
        Assert.False(defaults.CaptureRawContent);
        Assert.Empty(defaults.AdditionalProperties);
        defaults.ModelId = "model:A";
        defaults.CaptureRawContent = false;
        defaults.AdditionalProperties.Add("setting", opaque);
        defaults.AdditionalProperties.Add("Setting", null);
        DocumentExtractionOptions copy = defaults.Clone();
        Assert.NotSame(defaults, copy);
        Assert.NotSame(defaults.AdditionalProperties, copy.AdditionalProperties);
        Assert.Equal("model:A", copy.ModelId);
        Assert.False(copy.CaptureRawContent);
        Assert.Same(opaque, copy.AdditionalProperties["setting"]);
        Assert.Null(copy.AdditionalProperties["Setting"]);
        defaults.AdditionalProperties.Clear();
        defaults.ModelId = null;
        defaults.CaptureRawContent = true;
        Assert.Equal(2, copy.AdditionalProperties.Count);
        Assert.Equal("model:A", copy.ModelId);
        copy.AdditionalProperties["setting"] = "replacement";
        Assert.Empty(defaults.AdditionalProperties);
        Assert.False(copy.CaptureRawContent);
        Assert.Equal(0, opaque.DisposeCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Options_InvalidModelDoesNotReplacePriorValue(string invalid)
    {
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = false };
        Assert.Throws<ArgumentException>("value", () => options.ModelId = invalid);
        Assert.Equal("model:A", options.ModelId);
        Assert.False(options.CaptureRawContent);
        options.ModelId = null;
        Assert.Null(options.ModelId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DelegatingClient_ForwardsExactInterfaceArgumentsWithoutTakingSourceOwnership(bool provideOptions)
    {
        using MemoryStream source = new([1, 2, 3]);
        using CancellationTokenSource cancellation = new();
        DocumentExtractionOptions? options = provideOptions ? new() { ModelId = "model:A", CaptureRawContent = false } : null;
        using Client inner = new((_, _, _, _, _) => PagesAsync(new DocumentExtractionPage(7, [])));
        using Delegating wrapper = new(inner);
        IDocumentExtractionClient client = wrapper;
        IAsyncEnumerable<DocumentExtractionPage> pages = client.ExtractPagesAsync(source, "urn:call:A", "application/pdf", options, cancellation.Token);
        ClientCall call = Assert.Single(inner.Calls);
        Assert.Same(source, call.Source);
        Assert.Equal("urn:call:A", call.Identifier);
        Assert.Equal("application/pdf", call.MediaType);
        Assert.Same(options, call.Options);
        Assert.Equal(cancellation.Token, call.Token);
        using DocumentExtractionResult result = await pages.ToDocumentExtractionResultAsync("urn:call:A", cancellation.Token);
        Assert.Equal(7, Assert.Single(result.Pages).PageNumber);
        Assert.Equal("urn:call:A", result.Document.Identifier);
        wrapper.Dispose();
        wrapper.Dispose();
        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, source.ReadByte());
    }

    [Fact]
    public void DelegatingClient_GetServiceAndNullGuardsPreserveFallbackKeys()
    {
        using Client inner = new((_, _, _, _, _) => PagesAsync());
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExtractAsync_SnapshotsCallerAndProviderOptionsBeforeInvocation(bool capture)
    {
        using MemoryStream source = new([1, 2, 3]);
        using CancellationTokenSource cancellation = new();
        using Native native = new();
        using Native setting = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = capture };
        options.AdditionalProperties.Add("opaque", setting);
        TaskCompletionSource<bool>? release = null;
        int cleanup = 0;
        using Client inner = new((_, _, _, request, _) =>
        {
            Assert.NotNull(request);
            Assert.NotSame(options, request);
            Assert.Equal("model:A", request.ModelId);
            Assert.Equal(capture, request.CaptureRawContent);
            Assert.Same(setting, request.AdditionalProperties["opaque"]);
            request.CaptureRawContent = !capture;
            return Pages();
        });
        using Delegating client = new(inner);
        Task<DocumentExtractionResult> pending = client.ExtractAsync(source, "urn:call:A", "application/pdf", options, cancellation.Token);
        options.ModelId = "model:B";
        options.CaptureRawContent = !capture;
        options.AdditionalProperties.Clear();
        ClientCall call = Assert.Single(inner.Calls);
        Assert.Equal("model:A", call.Options!.ModelId);
        Assert.Same(setting, call.Options.AdditionalProperties["opaque"]);
        Assert.Same(source, call.Source);
        Assert.Equal("urn:call:A", call.Identifier);
        Assert.Equal("application/pdf", call.MediaType);
        Assert.Equal(cancellation.Token, call.Token);
        call.Options.ModelId = "provider:changed";
        call.Options.CaptureRawContent = !capture;
        call.Options.AdditionalProperties.Clear();
        Assert.Equal("model:B", options.ModelId);
        Assert.Empty(options.AdditionalProperties);
        Assert.NotNull(release);
        release.SetResult(true);
        try
        {
            using DocumentExtractionResult result = await pending;
            Assert.True(capture);
            Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
            Assert.Equal(0, body.DisposeCount);
            result.Dispose();
            result.Dispose();
            Assert.False(body.CanRead);
        }
        catch (ArgumentException error)
        {
            Assert.False(capture);
            Assert.Equal("pages", error.ParamName);
        }

        Assert.Equal(1, body.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.Equal(1, cleanup);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, setting.DisposeCount);
        Assert.Equal(1, source.ReadByte());

        async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            TaskCompletionSource<bool> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            release = gate;
            try
            {
                await gate.Task;
                yield return Page(output);
            }
            finally
            {
                cleanup++;
            }
        }
    }

    [Fact]
    public async Task ExtractAsync_AbsentOverridesStayAbsentAcrossExplicitCall()
    {
        using MemoryStream source = new([4, 5, 6]);
        using CancellationTokenSource cancellation = new();
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = false };
        using Client client = new((_, identifier, _, request, _) =>
            PagesAsync(Page(new DocumentSourceOutput(identifier, contentCaptureDisabled: request?.CaptureRawContent != true))));
        using DocumentExtractionResult first = await client.ExtractAsync(source, "default:first", "application/pdf");
        using DocumentExtractionResult explicitResult = await client.ExtractAsync(source, "explicit", "application/pdf", options, cancellation.Token);
        using DocumentExtractionResult last = await client.ExtractAsync(source, "default:last", "application/pdf");
        Assert.Equal(3, client.Calls.Count);
        Assert.Null(client.Calls[0].Options);
        Assert.Null(client.Calls[2].Options);
        Assert.NotSame(options, client.Calls[1].Options);
        Assert.Equal("model:A", client.Calls[1].Options!.ModelId);
        Assert.False(client.Calls[1].Options!.CaptureRawContent);
        Assert.Equal(cancellation.Token, client.Calls[1].Token);
        Assert.Equal(CancellationToken.None, client.Calls[0].Token);
        Assert.Equal(CancellationToken.None, client.Calls[2].Token);
        Assert.Null(Assert.Single(first.Document.SourceOutputs).GetRawRepresentation());
        Assert.Null(Assert.Single(explicitResult.Document.SourceOutputs).GetRawRepresentation());
        Assert.Equal(DocumentRawContentStatus.Disabled, Assert.Single(last.Document.SourceOutputs).ContentStatus);
        Assert.Equal("model:A", options.ModelId);
        Assert.False(options.CaptureRawContent);
        Assert.Equal(4, source.ReadByte());
    }

    [Fact]
    public async Task ExtractAsync_ConcurrentGatedCallsKeepBodiesOptionsAndTokensIsolated()
    {
        using MemoryStream sourceA = new([1, 2, 3]);
        using MemoryStream sourceB = new([4, 5, 6]);
        using CancellationTokenSource tokenA = new();
        using CancellationTokenSource tokenB = new();
        using Native opaque = new();
        CountingStream streamA = new([17, 18]);
        CountingStream streamB = new([27, 28]);
        using DocumentArtifact artifactA = new("application/json", streamA);
        using DocumentArtifact artifactB = new("application/json", streamB);
        DocumentSourceOutput outputA = new("output:A", rawContent: artifactA);
        DocumentSourceOutput outputB = new("output:B", rawContent: artifactB);
        TaskCompletionSource<bool> gateA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> gateB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = true };
        options.AdditionalProperties.Add("opaque", opaque);
        using Client client = new((_, id, _, _, _) => Pages(id));
        Task<DocumentExtractionResult> pendingA = client.ExtractAsync(sourceA, "urn:call:A", "application/pdf", options, tokenA.Token);
        options.ModelId = "model:B";
        options.AdditionalProperties["opaque"] = "B";
        Task<DocumentExtractionResult> pendingB = client.ExtractAsync(sourceB, "urn:call:B", "image/png", options, tokenB.Token);
        options.ModelId = null;
        options.AdditionalProperties.Clear();
        gateB.SetResult(true);
        using DocumentExtractionResult resultB = await pendingB;
        Assert.False(pendingA.IsCompleted);
        gateA.SetResult(true);
        using DocumentExtractionResult resultA = await pendingA;
        Assert.Equal(2, client.Calls.Count);
        Assert.Same(sourceA, client.Calls[0].Source);
        Assert.Same(sourceB, client.Calls[1].Source);
        Assert.Equal("urn:call:A", client.Calls[0].Identifier);
        Assert.Equal("urn:call:B", client.Calls[1].Identifier);
        Assert.Equal("application/pdf", client.Calls[0].MediaType);
        Assert.Equal("image/png", client.Calls[1].MediaType);
        Assert.Equal(tokenA.Token, client.Calls[0].Token);
        Assert.Equal(tokenB.Token, client.Calls[1].Token);
        Assert.Equal("model:A", client.Calls[0].Options!.ModelId);
        Assert.Equal("model:B", client.Calls[1].Options!.ModelId);
        Assert.Same(opaque, client.Calls[0].Options!.AdditionalProperties["opaque"]);
        Assert.Equal("B", client.Calls[1].Options!.AdditionalProperties["opaque"]);
        Assert.NotSame(client.Calls[0].Options, client.Calls[1].Options);
        Assert.True(client.Calls[0].Options!.CaptureRawContent);
        Assert.True(client.Calls[1].Options!.CaptureRawContent);
        Assert.Equal("urn:call:A", resultA.Document.Identifier);
        Assert.Equal("urn:call:B", resultB.Document.Identifier);
        Assert.Same(outputA, Assert.Single(resultA.Document.SourceOutputs));
        Assert.Same(outputB, Assert.Single(resultB.Document.SourceOutputs));
        Assert.Equal(new byte[] { 17, 18 }, streamA.ToArray());
        Assert.Equal(new byte[] { 27, 28 }, streamB.ToArray());
        resultA.Dispose();
        resultB.Dispose();
        Assert.Equal(1, streamA.DisposeCount);
        Assert.Equal(1, streamB.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => streamA.ReadByte());
        Assert.Throws<ObjectDisposedException>(() => streamB.ReadByte());
        Assert.Equal(0, opaque.DisposeCount);
        Assert.Equal(1, sourceA.ReadByte());
        Assert.Equal(4, sourceB.ReadByte());

        async IAsyncEnumerable<DocumentExtractionPage> Pages(string id)
        {
            await (id == "urn:call:A" ? gateA.Task : gateB.Task);
            yield return Page(id == "urn:call:A" ? outputA : outputB);
        }
    }

    [Theory]
    [InlineData("client")]
    [InlineData("document")]
    [InlineData("identifier-null")]
    [InlineData("identifier-empty")]
    [InlineData("mediaType-null")]
    [InlineData("mediaType-empty")]
    public void ExtractAsync_InvalidArgumentsDoNotInvokeProvider(string invalid)
    {
        using MemoryStream source = new([42]);
        using Client client = new((_, _, _, _, _) => PagesAsync());
        string identifier = invalid switch { "identifier-null" => null!, "identifier-empty" => "", _ => "id" };
        string mediaType = invalid switch { "mediaType-null" => null!, "mediaType-empty" => "", _ => "application/pdf" };
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(() =>
        {
            _ = DocumentExtractionClientExtensions.ExtractAsync(
                invalid == "client" ? null! : client, invalid == "document" ? null! : source, identifier, mediaType);
        });
        Assert.Equal(invalid.Split('-')[0], error.ParamName);
        Assert.Empty(client.Calls);
        Assert.Equal(42, source.ReadByte());
    }

    [Theory]
    [InlineData("failure")]
    [InlineData("after-last")]
    [InlineData("during-yield")]
    public async Task ExtractAsync_FailureAndCancellationDisposeAcquiredRawOnceWithoutSuccess(string ending)
    {
        using MemoryStream source = new([42]);
        using CancellationTokenSource cancellation = new();
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        InvalidOperationException failure = new("synthetic failure");
        int cleanup = 0;
        using Client client = new((_, _, _, _, _) => Pages());
        try
        {
            using DocumentExtractionResult result = await client.ExtractAsync(source, "document", "application/pdf",
                new DocumentExtractionOptions { CaptureRawContent = true }, cancellation.Token);
            Assert.Fail("Failure or cancellation must not return a result.");
        }
        catch (OperationCanceledException error)
        {
            Assert.NotEqual("failure", ending);
            Assert.Equal(cancellation.Token, error.CancellationToken);
        }
        catch (InvalidOperationException error)
        {
            Assert.Equal("failure", ending);
            Assert.Same(failure, error);
        }

        Assert.Equal(cancellation.Token, Assert.Single(client.Calls).Token);
        Assert.Equal(1, cleanup);
        Assert.Equal(1, body.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());

        async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            try
            {
                if (ending == "during-yield")
                {
                    cancellation.Cancel();
                }

                yield return Page(output);
                await Task.CompletedTask;
                if (ending == "after-last")
                {
                    cancellation.Cancel();
                }
                else if (ending == "failure")
                {
                    throw failure;
                }
            }
            finally
            {
                cleanup++;
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Finalization_AsyncPositionalOverloadsHonorCaptureAndCancellation(int overload)
    {
        using CancellationTokenSource cancellation = new();
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        bool capture = overload == 1;
        DocumentSourceOutput output = capture ? new("operation", native, artifact) : new("operation", contentCaptureDisabled: true);
        int cleanup = 0;
        CancellationToken received = default;
        IAsyncEnumerable<DocumentExtractionPage> pages = Pages();
        using DocumentExtractionResult result = overload == 0
            ? await pages.ToDocumentExtractionResultAsync("document", cancellation.Token)
            : await pages.ToDocumentExtractionResultAsync("document", capture, cancellation.Token);
        Assert.Equal(cancellation.Token, received);
        Assert.Equal(1, cleanup);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal(new[] { 2, 7 }, result.Pages.Select(static page => page.PageNumber));
        Assert.All(result.Pages, page => Assert.Same(output, Assert.Single(page.SourceOutputs)));
        Assert.Equal("visible **text**" + Environment.NewLine + "visible **text**", result.Document.ToMarkdown());
        Assert.Equal(new[] { "visible text", "visible text" }, result.Document.EnumerateContent().OfType<DocumentTextElement>().Select(static text => text.Text));
        Assert.Equal(capture ? DocumentRawContentStatus.Retained : DocumentRawContentStatus.Disabled, output.ContentStatus);
        if (capture)
        {
            Assert.Same(native, output.GetRawRepresentation());
            Assert.Same(artifact, output.GetRawContent());
            using MemoryStream copy = new();
            await output.GetRawContent()!.Content.CopyToAsync(copy);
            Assert.Equal(RawBytes(), copy.ToArray());
        }
        else
        {
            Assert.Null(output.GetRawRepresentation());
            Assert.Null(output.GetRawContent());
        }

        Assert.Equal("visible **text**" + Environment.NewLine + "visible **text**", result.Document.ToMarkdown());
        Assert.Equal(0, body.DisposeCount);
        result.Document.Dispose();
        result.Dispose();
        Assert.Equal(capture, !body.CanRead);
        Assert.Equal(capture ? 1 : 0, body.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        if (capture)
        {
            Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        }

        async IAsyncEnumerable<DocumentExtractionPage> Pages([EnumeratorCancellation] CancellationToken token = default)
        {
            received = token;
            try
            {
                yield return Page(output, 7);
                await Task.CompletedTask;
                yield return Page(output, 2);
            }
            finally
            {
                cleanup++;
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Finalization_PreCanceledPositionalOverloadsNeverAcquirePages(int overload)
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", rawContent: artifact);
        int started = 0;
        IAsyncEnumerable<DocumentExtractionPage> pages = Pages();
        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            overload == 0 ? pages.ToDocumentExtractionResultAsync("document", cancellation.Token) :
            pages.ToDocumentExtractionResultAsync("document", overload == 1, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, started);
        Assert.Equal(0, body.DisposeCount);
        Assert.True(body.CanRead);

        async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            started++;
            await Task.CompletedTask;
            yield return Page(output);
        }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    public void Finalization_NativeAndContentAvailabilityAreIndependentAndDisabledIsNotUnavailable(bool hasNative, bool hasBody, bool disabled)
    {
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", hasNative ? native : null, hasBody ? artifact : null, disabled);
        using DocumentExtractionResult result = new[] { Page(output) }.ToDocumentExtractionResult("document", !disabled);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        DocumentRawContentStatus contentStatus = hasBody ? DocumentRawContentStatus.Retained : DocumentRawContentStatus.Unavailable;
        Assert.Equal(disabled ? DocumentRawContentStatus.Disabled : contentStatus, output.ContentStatus);
        Assert.Same(hasNative ? native : null, output.GetRawRepresentation());
        Assert.Same(hasBody ? artifact : null, output.GetRawContent());
        Assert.Equal("visible **text**", result.Document.ToMarkdown());
        Assert.Equal("visible text", Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.EnumerateContent())).Text);
        result.Dispose();
        result.Dispose();
        Assert.Equal(hasBody ? 1 : 0, body.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
    }

    [Theory]
    [InlineData("page-output")]
    [InlineData("frame-output")]
    [InlineData("page-source")]
    [InlineData("observation-source")]
    [InlineData("nested-cell")]
    [InlineData("raw-source")]
    public void Finalization_OptOutRejectsRawAtEveryCarrierAndCleansOwnedContent(string carrier)
    {
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentSourceReference annotation = new(new DocumentSourcePage("provider", 7, sourceOutput: output),
            rawRepresentation: carrier == "raw-source" ? native : null);
        DocumentExtractionObservation observation = new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "visible",
            sources: carrier is "observation-source" or "raw-source" ? [annotation] : null);
        if (carrier == "nested-cell")
        {
            observation = new DocumentExtractionTable("table", 0, "", 1, 1,
                [new DocumentExtractionTableCell(0, 0, observation, sources: [annotation])]);
        }

        DocumentExtractionPage page = new(7, [observation],
            sourcePage: carrier == "frame-output" ? new DocumentSourcePage("frame", 7, sourceOutput: output) : null,
            sources: carrier == "page-source" ? [annotation] : null,
            sourceOutputs: carrier == "page-output" ? [output] : null);
        ArgumentException error = Assert.Throws<ArgumentException>(() => new[] { page }.ToDocumentExtractionResult("document", false));
        Assert.Equal("pages", error.ParamName);
        Assert.Equal(1, body.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.False(body.CanRead);
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void Finalization_OptOutPreservesNativeSourceWithoutOperationOutput()
    {
        using Native native = new();
        DocumentExtractionPage page = new(7, [], sources: [new DocumentSourceReference(new DocumentSourcePage("provider", 7), rawRepresentation: native)]);
        using DocumentExtractionResult result = new[] { page }.ToDocumentExtractionResult("document", false);
        Assert.Same(page.Sources[0], Assert.Single(Assert.Single(result.Document.Sections).Sources));
        Assert.Empty(page.SourceOutputs);
        Assert.Same(native, page.Sources[0].GetRawRepresentation());
        Assert.Same(page.Sources[0].Page, Assert.Single(result.Document.SourcePages));
        Assert.Empty(result.Document.SourceOutputs);
        Assert.Equal(0, native.DisposeCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Builder_AbandonmentDisposesSharedRawBodyOnceWithoutOwningNative(bool capture)
    {
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        using DocumentExtractionDocumentBuilder builder = new("document", capture);
        builder.AddPage(Page(output, 7));
        builder.AddPage(Page(output, 2));
        builder.Dispose();
        builder.Dispose();
        Assert.Equal(1, body.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.False(body.CanRead);
        Assert.Equal(0, native.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => builder.Build());
    }

    [Fact]
    public async Task ExtractAsync_ExplicitCapture_RetainsExactOriginalBodyAndBorrowedNative()
    {
        using MemoryStream source = new([42]);
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        using Client client = new((_, _, _, _, _) => PagesAsync(Page(output)));
        DocumentExtractionOptions options = new() { CaptureRawContent = true };
        using DocumentExtractionResult result = await client.ExtractAsync(source, "document", "application/pdf", options);
        ClientCall call = Assert.Single(client.Calls);
        Assert.Equal(CancellationToken.None, call.Token);
        Assert.NotNull(call.Options);
        Assert.NotSame(options, call.Options);
        Assert.Null(call.Options.ModelId);
        Assert.True(call.Options.CaptureRawContent);
        Assert.Empty(call.Options.AdditionalProperties);

        Assert.Same(source, call.Source);
        Assert.Equal("document", call.Identifier);
        Assert.Equal("application/pdf", call.MediaType);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        using MemoryStream observed = new();
        await output.GetRawContent()!.Content.CopyToAsync(observed);
        Assert.Equal(RawBytes(), observed.ToArray());
        Assert.Equal(DocumentRawContentStatus.Retained, output.ContentStatus);
        Assert.Equal("visible **text**", result.Document.ToMarkdown());
        Assert.Equal("visible text", Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.EnumerateContent())).Text);
        Assert.Equal(0, body.DisposeCount);
        result.Dispose();
        result.Dispose();
        Assert.Equal(1, body.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Finalization_OptOutAllowsNativeOnlyButRejectsContentOnly(bool nativeOnly)
    {
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", nativeOnly ? native : null, nativeOnly ? null : artifact);
        if (nativeOnly)
        {
            using DocumentExtractionResult result = new[] { Page(output) }.ToDocumentExtractionResult("document", false);
            Assert.Same(native, Assert.Single(result.Document.SourceOutputs).GetRawRepresentation());
        }
        else
        {
            Assert.Throws<ArgumentException>("pages", () => new[] { Page(output) }.ToDocumentExtractionResult("document", false));
        }

        Assert.Equal(nativeOnly ? 0 : 1, body.DisposeCount);
        Assert.Equal(nativeOnly, body.CanRead);
        Assert.Equal(0, native.DisposeCount);
        if (!nativeOnly)
        {
            Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        }
    }

    [Fact]
    public async Task ExtractAsync_OptOutRejectsSourcePageRawAndPreservesCallerOwnership()
    {
        using MemoryStream source = new([42, 43]);
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("region-operation", native, artifact);
        DocumentSourcePage frame = new("region-frame", 7, sourceOutput: output);
        DocumentSourceReference annotation = new(frame, [new(1, 2), new(4, 2), new(3, 5)]);
        DocumentExtractionText observation = new("text", 0, DocumentTextKind.Paragraph, "visible", sources: [annotation]);
        DocumentExtractionPage page = new(7, [observation]);
        int cleanup = 0;
        using Client client = new((_, _, _, _, _) => Pages());

        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.ExtractAsync(source, "document", "application/pdf", new DocumentExtractionOptions { CaptureRawContent = false }));

        Assert.Equal("pages", error.ParamName);
        ClientCall call = Assert.Single(client.Calls);
        Assert.Same(source, call.Source);
        Assert.False(call.Options!.CaptureRawContent);
        Assert.Empty(page.SourceOutputs);
        Assert.Null(page.SourcePage);
        Assert.Empty(page.Sources);
        Assert.Null(annotation.GetRawRepresentation());
        Assert.Same(annotation, Assert.Single(observation.Sources));
        Assert.Same(output, annotation.Page.SourceOutput);
        Assert.Equal(1, cleanup);
        Assert.Equal(1, body.DisposeCount);
        Assert.False(body.CanRead);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());
        Assert.Equal(43, source.ReadByte());

        async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            try
            {
                yield return page;
                await Task.CompletedTask;
            }
            finally
            {
                cleanup++;
            }
        }
    }

    [Fact]
    public async Task ExtractAsync_NestedTablePreservesMergedEmptyGeometryPayloadAndRawOwnership()
    {
        using MemoryStream source = new([42]);
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("nested-operation", native, artifact);
        DocumentSourcePage frame = new("nested-frame", 7, new DocumentPageDimensions(200, 100),
            DocumentCoordinateUnit.Point, DocumentCoordinateOrigin.BottomLeft, output);
        DocumentPoint[] vertices =
        [
            new(10.25, 20.5), new(40.75, 18.25), new(44.5, 60.125), new(12.875, 58.75), new(8.5, 35.25)
        ];
        DocumentSourceReference emptyLayout = new(frame, vertices);
        var payload = new { kind = "vendor-extra", nested = new { unknown = new object[] { 3, "x" } } };
        var attributes = new { vendorFlag = "kept", provenance = "urn:synthetic:engine:v1", target = "urn:target:nested" };
        DocumentSourceReference extensionSource = new(frame, vertices);
        DocumentSourceReference reference = new(frame);
        Dictionary<string, object?> metadata = new(StringComparer.Ordinal) { ["vendor"] = attributes, ["Vendor"] = null };
        DocumentExtractionText anchorText = new("anchor", 0, DocumentTextKind.Paragraph, "MERGED_A", "merged plain");
        DocumentExtractionExtension extension = new("extension", 0, "vendor-extra", "visible **extra**", "visible extra",
            sources: [extensionSource], metadata: metadata, value: payload);
        DocumentExtractionTableCell emptyCell = new(2, 2, null, kind: "empty", sources: [emptyLayout]);
        DocumentExtractionTable inner = new("inner", 0, "stored inner", 3, 3,
        [
            new DocumentExtractionTableCell(0, 0, anchorText, 2, 2, "columnHeader"),
            new DocumentExtractionTableCell(2, 0, extension, kind: "rowHeader"),
            emptyCell
        ]);
        DocumentExtractionTable outer = new("outer", 0, "stored outer", 1, 1, [new DocumentExtractionTableCell(0, 0, inner)]);
        DocumentExtractionPage page = new(7, [outer], sources: [reference]);
        metadata.Clear();
        using Client client = new((_, _, _, _, _) => PagesAsync(page));
        using DocumentExtractionResult result = await client.ExtractAsync(source, "nested", "application/pdf",
            new DocumentExtractionOptions { CaptureRawContent = true });
        Document document = result.Document;

        Assert.Equal("nested", document.Identifier);
        Assert.Same(page, Assert.Single(result.Pages));
        Assert.Same(outer, Assert.Single(result.Pages[0].Observations));
        Assert.Same(inner, outer.Cells[0].Content);
        Assert.Same(extension, inner.Cells[1].Content);
        Assert.Same(emptyCell, inner.Cells[2]);
        Assert.Same(emptyLayout, Assert.Single(emptyCell.Sources));
        Assert.Same(payload, extension.Value);
        Assert.Equal(2, inner.Cells[0].RowSpan);
        Assert.Equal(2, inner.Cells[0].ColumnSpan);
        Assert.Equal("columnHeader", inner.Cells[0].Kind);
        Assert.Empty(page.SourceOutputs);
        Assert.Null(page.SourcePage);
        DocumentTableElement sharedOuter = Assert.IsType<DocumentTableElement>(Assert.Single(document.EnumerateContent()));
        Assert.Same(sharedOuter, Assert.Single(Assert.Single(document.Sections).Elements));
        Assert.Equal(1, sharedOuter.Cells.RowCount);
        Assert.Equal(1, sharedOuter.Cells.ColumnCount);
        DocumentTableCell outerCell = Assert.IsType<DocumentTableCell>(sharedOuter.Cells[0, 0]);
        Assert.Equal(1, outerCell.RowSpan);
        Assert.Equal(1, outerCell.ColumnSpan);
        Assert.Null(outerCell.Kind);
        DocumentTableElement sharedInner = Assert.IsType<DocumentTableElement>(outerCell.Content);
        Assert.Equal(3, sharedInner.Cells.RowCount);
        Assert.Equal(3, sharedInner.Cells.ColumnCount);
        DocumentTableCell anchor = Assert.IsType<DocumentTableCell>(sharedInner.Cells[0, 0]);
        Assert.Equal(2, anchor.RowSpan);
        Assert.Equal(2, anchor.ColumnSpan);
        Assert.Equal("columnHeader", anchor.Kind);
        Assert.False(sharedInner.Cells.IsCovered(0, 0));
        foreach ((int row, int column) in new[] { (0, 1), (1, 0), (1, 1) })
        {
            Assert.True(sharedInner.Cells.IsCovered(row, column));
            Assert.Null(sharedInner.Cells[row, column]);
            Assert.Same(anchor, sharedInner.Cells.GetCoveringCell(row, column));
        }

        DocumentTableCell sharedEmpty = Assert.IsType<DocumentTableCell>(sharedInner.Cells[2, 2]);
        Assert.Null(sharedEmpty.Content);
        Assert.Equal(1, sharedEmpty.RowSpan);
        Assert.Equal(1, sharedEmpty.ColumnSpan);
        Assert.Equal("empty", sharedEmpty.Kind);
        Assert.False(sharedInner.Cells.IsCovered(2, 2));
        Assert.Same(sharedEmpty, sharedInner.Cells.GetCoveringCell(2, 2));
        Assert.False(sharedInner.Cells.IsCovered(2, 1));
        Assert.Null(sharedInner.Cells.GetCoveringCell(2, 1));
        Assert.Same(emptyLayout, Assert.Single(sharedEmpty.Sources));
        Assert.Equal(vertices, emptyLayout.Polygon);
        Assert.Same(frame, emptyLayout.Page);
        Assert.Equal("nested-frame", frame.Identifier);
        Assert.Equal(7, frame.PageNumber);
        Assert.Equal(new DocumentPageDimensions(200, 100), frame.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, frame.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, frame.CoordinateOrigin);
        DocumentTableCell extensionCell = Assert.IsType<DocumentTableCell>(sharedInner.Cells[2, 0]);
        Assert.Equal("rowHeader", extensionCell.Kind);
        DocumentExtensionElement sharedExtension = Assert.IsType<DocumentExtensionElement>(extensionCell.Content);
        Assert.Same(payload, sharedExtension.Value);
        Assert.Equal(new object[] { 3, "x" }, payload.nested.unknown);
        Assert.Equal("vendor-extra", sharedExtension.Type);
        Assert.Equal("visible extra", sharedExtension.Text);
        Assert.Equal(new[] { reference, extensionSource }, sharedExtension.Sources);
        Assert.Equal(2, sharedExtension.Metadata.Count);
        Assert.Same(attributes, sharedExtension.Metadata["vendor"]);
        Assert.Null(sharedExtension.Metadata["Vendor"]);
        Assert.False(sharedExtension.Metadata.ContainsKey("VENDOR"));
        Assert.Empty(sharedOuter.Metadata);
        Assert.Empty(sharedInner.Metadata);
        Assert.Empty(Assert.Single(document.Sections).Metadata);
        Assert.Equal("kept", attributes.vendorFlag);
        Assert.Equal("urn:synthetic:engine:v1", attributes.provenance);
        Assert.Equal("urn:target:nested", attributes.target);
        Assert.Equal(new[] { reference }, sharedOuter.Sources);
        Assert.Equal(new[] { reference }, sharedInner.Sources);
        Assert.Equal(new[] { reference }, anchor.Content!.Sources);
        Assert.Empty(outerCell.Sources);
        Assert.Empty(extensionCell.Sources);
        Assert.Equal(new[] { reference }, Assert.Single(document.Sections).Sources);
        Assert.Equal(new[] { "MERGED_A", "visible **extra**" },
            sharedInner.Cells.Select(static cell => cell?.Content).OfType<DocumentElement>().Select(static element => element.ToMarkdown()));
        Assert.Equal("merged plain", Assert.IsType<DocumentTextElement>(anchor.Content).Text);
        const string InnerProjection =
            "<table><tr><th rowspan=\"2\" colspan=\"2\" data-kind=\"columnHeader\">MERGED_A</th><td></td></tr>" +
            "<tr><td></td></tr><tr><th rowspan=\"1\" colspan=\"1\" data-kind=\"rowHeader\">visible **extra**</th><td></td>" +
            "<td rowspan=\"1\" colspan=\"1\" data-kind=\"empty\"></td></tr></table>";
        Assert.Equal(InnerProjection, sharedInner.ToMarkdown());
        Assert.Equal("| " + InnerProjection + " |", document.ToMarkdown());
        Assert.Equal("stored outer", sharedOuter.GetMarkdown());
        Assert.Equal("stored inner", sharedInner.GetMarkdown());
        Assert.Equal("", document.GetMarkdown());
        Assert.Same(frame, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(output, frame.SourceOutput);
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        Assert.Equal(RawBytes(), body.ToArray());
        Assert.True(body.CanRead);
        Assert.Equal(0, body.DisposeCount);
        result.Dispose();
        document.Dispose();
        Assert.Equal(1, body.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => body.ReadByte());
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());
    }

    private static byte[] RawBytes() => System.Text.Encoding.UTF8.GetBytes(
        " {\r\n \"unknown\" : {\"z\":[3, 1]}, \"marker\":\"RAW_BODY_ONLY_91\", \"pages\":[7,2]\r\n}\n");

    private static DocumentExtractionPage Page(DocumentSourceOutput output, int number = 7) =>
        new(number, [new DocumentExtractionText("text:" + number, 0, DocumentTextKind.Paragraph, "visible **text**", "visible text")],
            sourceOutputs: [output]);

    private static async IAsyncEnumerable<DocumentExtractionPage> PagesAsync(params DocumentExtractionPage[] pages)
    {
        foreach (DocumentExtractionPage page in pages)
        {
            yield return page;
        }

        await Task.CompletedTask;
    }

    private sealed class Delegating(IDocumentExtractionClient innerClient) : DelegatingDocumentExtractionClient(innerClient);

    private sealed class ClientCall(Stream source, string identifier, string mediaType, DocumentExtractionOptions? options, CancellationToken token)
    {
        internal Stream Source { get; } = source;
        internal string Identifier { get; } = identifier;
        internal string MediaType { get; } = mediaType;
        internal DocumentExtractionOptions? Options { get; } = options;
        internal CancellationToken Token { get; } = token;
    }

    private sealed class Client(
        Func<Stream, string, string, DocumentExtractionOptions?, CancellationToken, IAsyncEnumerable<DocumentExtractionPage>> handler) : IDocumentExtractionClient
    {
        internal List<ClientCall> Calls { get; } = [];
        internal int DisposeCount { get; private set; }
        internal int ServiceCount { get; private set; }
        internal Type? ServiceType { get; private set; }
        internal object? ServiceKey { get; private set; }
        internal object Service { get; } = new();

        public IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
            Stream document, string identifier, string mediaType, DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls.Add(new ClientCall(document, identifier, mediaType, options, cancellationToken));
            return handler(document, identifier, mediaType, options, cancellationToken);
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

    private sealed class CountingStream(byte[] bytes) : MemoryStream(bytes)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Options_DefaultsAndClone_PreserveScalarsAndBorrowValuesWithIndependentOrdinalProperties(bool capture)
    {
        using Native opaque = new();
        Assert.False(new DocumentExtractionOptions().CaptureRawContent);
        DocumentExtractionOptions original = new() { ModelId = "original", CaptureRawContent = capture };
        original.AdditionalProperties.Add("setting", opaque);
        original.AdditionalProperties.Add("Setting", null);
        DocumentExtractionOptions copy = original.Clone();

        Assert.NotSame(original, copy);
        Assert.NotSame(original.AdditionalProperties, copy.AdditionalProperties);
        Assert.Equal("original", copy.ModelId);
        Assert.Equal(capture, copy.CaptureRawContent);
        Assert.Same(opaque, copy.AdditionalProperties["setting"]);
        Assert.Null(copy.AdditionalProperties["Setting"]);
        copy.ModelId = "copy";
        copy.CaptureRawContent = !capture;
        copy.AdditionalProperties["Setting"] = "copy-only";
        Assert.Equal("original", original.ModelId);
        Assert.Equal(capture, original.CaptureRawContent);
        Assert.Null(original.AdditionalProperties["Setting"]);
        original.ModelId = null;
        original.AdditionalProperties.Clear();
        Assert.Equal("copy", copy.ModelId);
        Assert.Equal(!capture, copy.CaptureRawContent);
        Assert.Equal(2, copy.AdditionalProperties.Count);
        Assert.Same(opaque, copy.AdditionalProperties["setting"]);
        Assert.Equal(0, opaque.DisposeCount);
    }

    [Fact]
    public void Options_PublicSurface_UsesCaptureRawContentWithoutCaptureRawOutputAlias()
    {
        Assert.Equal(typeof(bool), typeof(DocumentExtractionOptions).GetProperty("CaptureRawContent")!.PropertyType);
        Assert.Null(typeof(DocumentExtractionOptions).GetProperty("CaptureRawOutput"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ExtractAsync_AbsentDefaultAndFalseOptions_DisableBodyCaptureAndPreserveNativeIdentity(int optionKind)
    {
        using MemoryStream source = new([42]);
        using Native native = new();
        using Native annotationNative = new();
        DocumentExtractionOptions? options = optionKind switch
        {
            2 => new(),
            3 => new() { CaptureRawContent = false },
            _ => null
        };
        DocumentSourceOutput? output = null;
        DocumentSourceReference annotation = new(new DocumentSourcePage("native", 7), rawRepresentation: annotationNative);
        DocumentExtractionPage? page = null;
        using Client client = new((_, _, _, request, _) =>
        {
            output = new("operation", native, contentCaptureDisabled: request?.CaptureRawContent != true);
            page = new(7, [], sources: [annotation], sourceOutputs: [output]);
            return PagesAsync(page);
        });
        using DocumentExtractionResult result = optionKind == 0
            ? await client.ExtractAsync(source, "document", "application/pdf")
            : await client.ExtractAsync(source, "document", "application/pdf", options);

        ClientCall call = Assert.Single(client.Calls);
        if (options is null)
        {
            Assert.Null(call.Options);
        }
        else
        {
            Assert.NotSame(options, call.Options);
            Assert.False(call.Options!.CaptureRawContent);
        }

        Assert.Same(page, Assert.Single(result.Pages));
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output!.GetRawRepresentation());
        Assert.Null(output.GetRawContent());
        Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
        Assert.Same(annotation, Assert.Single(Assert.Single(result.Document.Sections).Sources));
        Assert.Same(annotationNative, annotation.GetRawRepresentation());
        result.Dispose();
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, annotationNative.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, annotationNative.ToStringCount);
        Assert.Equal(42, source.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExtractAsync_ProviderCaptureMutation_DoesNotChangeFrozenFinalizationPolicy(bool capture)
    {
        using MemoryStream source = new([42]);
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        using Native native = new();
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentExtractionOptions options = new() { CaptureRawContent = capture };
        using Client client = new((_, _, _, request, _) =>
        {
            request!.CaptureRawContent = !capture;
            return PagesAsync(Page(output));
        });
        if (capture)
        {
            using DocumentExtractionResult result = await client.ExtractAsync(source, "document", "application/pdf", options);
            Assert.Same(artifact, Assert.Single(result.Document.SourceOutputs).GetRawContent());
            using MemoryStream copy = new();
            await artifact.Content.CopyToAsync(copy);
            Assert.Equal(RawBytes(), copy.ToArray());
            Assert.Equal(0, body.DisposeCount);
        }
        else
        {
            ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.ExtractAsync(source, "document", "application/pdf", options));
            Assert.Equal("pages", error.ParamName);
            Assert.False(body.CanRead);
        }

        Assert.Equal(capture, options.CaptureRawContent);
        Assert.Equal(!capture, Assert.Single(client.Calls).Options!.CaptureRawContent);
        Assert.Equal(1, body.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task ExtractAsync_SourceOutputStatuses_KeepNativeAvailabilityIndependentOfBodyPolicy(bool hasNative, bool capture, bool hasBody)
    {
        using MemoryStream source = new([42]);
        using Native native = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", hasNative ? native : null, hasBody ? artifact : null,
            contentCaptureDisabled: !capture);
        using Client client = new((_, _, _, _, _) => PagesAsync(Page(output)));
        using DocumentExtractionResult result = await client.ExtractAsync(source, "document", "application/pdf",
            new DocumentExtractionOptions { CaptureRawContent = capture });
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        DocumentRawContentStatus contentStatus = hasBody ? DocumentRawContentStatus.Retained : DocumentRawContentStatus.Unavailable;
        Assert.Equal(!capture ? DocumentRawContentStatus.Disabled : contentStatus, output.ContentStatus);
        Assert.Same(hasNative ? native : null, output.GetRawRepresentation());
        Assert.Same(hasBody ? artifact : null, output.GetRawContent());
        result.Dispose();
        Assert.Equal(hasBody ? 1 : 0, body.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(42, source.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExtractAsync_NativeOnlyWithCaptureEnabled_DoesNotInventBody(bool hasNative)
    {
        using MemoryStream source = new([42]);
        using Native native = new();
        DocumentSourceOutput output = new("operation", hasNative ? native : null);
        using Client client = new((_, _, _, _, _) => PagesAsync(Page(output)));
        using DocumentExtractionResult result = await client.ExtractAsync(source, "document", "application/pdf",
            new DocumentExtractionOptions { CaptureRawContent = true });
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(hasNative ? native : null, output.GetRawRepresentation());
        Assert.Null(output.GetRawContent());
        Assert.All(result.Document.SourceOutputs, static sourceOutput => Assert.Null(sourceOutput.GetRawContent()));
        Assert.Equal(DocumentRawContentStatus.Unavailable, output.ContentStatus);
        result.Dispose();
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(42, source.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExtractPagesAsync_Delegation_ForwardsExactStreamOptionsAndToken(bool provideOptions)
    {
        using MemoryStream input = new([42]);
        using CancellationTokenSource cancellation = new();
        using Native native = new();
        using Native opaque = new();
        DocumentSourceOutput output = new("operation", native, contentCaptureDisabled: true);
        DocumentExtractionPage page = Page(output);
        DocumentExtractionOptions? options = provideOptions ? new() : null;
        options?.AdditionalProperties.Add("opaque", opaque);
        using Client inner = new((_, _, _, _, _) => PagesAsync(page));
        using Delegating client = new(inner);
        List<DocumentExtractionPage> observed = [];
        await foreach (DocumentExtractionPage item in client.ExtractPagesAsync(input, "urn:input", "application/pdf", options, cancellation.Token))
        {
            observed.Add(item);
        }

        Assert.Same(page, Assert.Single(observed));
        ClientCall call = Assert.Single(inner.Calls);
        Assert.Same(input, call.Source);
        Assert.Same(options, call.Options);
        Assert.Equal("urn:input", call.Identifier);
        Assert.Equal("application/pdf", call.MediaType);
        Assert.Equal(cancellation.Token, call.Token);
        client.Dispose();
        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, opaque.DisposeCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExtractAsync_FailureAndCancellation_LeaveBorrowedOptionValuesAlive(bool cancel)
    {
        using MemoryStream input = new([42]);
        using CancellationTokenSource cancellation = new();
        using Native native = new();
        using Native annotationNative = new();
        using Native opaque = new();
        CountingStream body = new(RawBytes());
        using DocumentArtifact artifact = new("application/json", body);
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentSourceReference annotation = new(new DocumentSourcePage("vendor", 7, sourceOutput: output), rawRepresentation: annotationNative);
        DocumentExtractionPage page = new(7, [], sources: [annotation], sourceOutputs: [output],
            metadata: new Dictionary<string, object?> { ["vendor"] = opaque });
        DocumentExtractionOptions options = new() { ModelId = "model:A", CaptureRawContent = true };
        options.AdditionalProperties.Add("opaque", opaque);
        Exception primary = cancel
            ? new OperationCanceledException(cancellation.Token)
            : new InvalidOperationException("provider failure");
        using Client client = new((_, _, _, _, _) => Pages());

        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() =>
            client.ExtractAsync(input, "document", "application/pdf", options, cancellation.Token));

        Assert.Same(primary, actual);
        Assert.Equal(1, body.DisposeCount);
        Assert.False(body.CanRead);
        ClientCall call = Assert.Single(client.Calls);
        Assert.NotSame(options, call.Options);
        Assert.Same(opaque, call.Options!.AdditionalProperties["opaque"]);
        Assert.Same(opaque, options.AdditionalProperties["opaque"]);
        Assert.Equal("model:A", options.ModelId);
        Assert.True(options.CaptureRawContent);
        Assert.Equal(cancellation.Token, call.Token);
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(annotationNative, annotation.GetRawRepresentation());
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, annotationNative.DisposeCount);
        Assert.Equal(0, opaque.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, annotationNative.ToStringCount);
        Assert.Equal(0, opaque.ToStringCount);
        Assert.Equal(42, input.ReadByte());

        async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            yield return page;
            await Task.CompletedTask;
            if (cancel)
            {
                cancellation.Cancel();
            }

            throw primary;
        }
    }
}
