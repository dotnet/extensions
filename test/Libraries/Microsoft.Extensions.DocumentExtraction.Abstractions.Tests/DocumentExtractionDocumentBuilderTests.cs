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

public sealed class DocumentExtractionDocumentBuilderTests
{
    [Fact]
    public void Build_SortsPagesAndObservations_PreservesEqualTextAndAssociations()
    {
        DocumentPageReference firstReference = new("page:1");
        DocumentPageReference duplicateReference = new("page:1");
        DocumentPageReference observationReference = new("provider-page:1");
        DocumentEvidenceReference evidenceReference = new("evidence:opaque");

        DocumentExtractionPage secondPage = new(
            2,
            [
                new DocumentExtractionText("provider-2", 2, DocumentTextKind.Paragraph, "second", "same"),
                new DocumentExtractionText("provider-3", 1, DocumentTextKind.Paragraph, "first", "same")
            ]);
        DocumentExtractionPage firstPage = new(
            1,
            [
                new DocumentExtractionText(
                    "provider-1",
                    1,
                    DocumentTextKind.Paragraph,
                    "first",
                    "same",
                    pageReferences: [observationReference],
                    evidence: [new DocumentExtractionEvidence(evidenceReference)])
            ],
            markdown: "page one",
            pageReferences: [firstReference, duplicateReference]);

        using DocumentExtractionDocumentBuilder builder = new("document-id");
        builder.AddPage(secondPage);
        builder.AddPage(firstPage);

        using DocumentExtractionResult result = builder.Build();

        Assert.Equal(new[] { 1, 2 }, result.Pages.Select(static page => page.PageNumber));
        Assert.Equal(new[] { "same", "same", "same" }, result.Document.EnumerateContent().OfType<DocumentTextElement>().Select(static element => element.Text));
        DocumentTextElement firstElement = Assert.IsType<DocumentTextElement>(result.Document.Sections[0].Elements[0]);
        Assert.Equal(new[] { "page:1", "page:1", "provider-page:1" }, firstElement.PageReferences.Select(static reference => reference.Value));
        Assert.Same(evidenceReference, firstElement.Evidence[0]);
        Assert.Equal("page one", result.Document.Sections[0].StoredMarkdown);
    }

    [Fact]
    public void Build_MapsTablesImagesAndOpaqueExtensions()
    {
        DocumentExtractionTable table = new(
            "table",
            0,
            "stored table",
            1,
            3,
            [
                new DocumentExtractionTableCell(
                    0,
                    0,
                    new DocumentExtractionText("cell-text", 0, DocumentTextKind.Paragraph, "cell", "cell")),
                new DocumentExtractionTableCell(
                    0,
                    1,
                    new DocumentExtractionImage("cell-image", 0, "cell image", alternativeText: "cell diagram")),
                new DocumentExtractionTableCell(
                    0,
                    2,
                    new DocumentExtractionExtension("barcode", 0, "barcode", "barcode markdown", "123"))
            ]);

        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(
            1,
            [
                new DocumentExtractionImage(
                    "image",
                    0,
                    "stored image",
                    content: new byte[] { 1, 2, 3 },
                    mediaType: "image/png",
                    alternativeText: "diagram"),
                table,
                new DocumentExtractionExtension("unknown", 2, "provider.unknown", "unknown markdown"),
                new DocumentExtractionExtension("list", 3, "provider.list", "- item"),
                new DocumentExtractionExtension("barcode-2", 4, "provider.barcode", "barcode")
            ]));

        using DocumentExtractionResult result = builder.Build();
        DocumentImageElement image = Assert.IsType<DocumentImageElement>(result.Document.Sections[0].Elements[0]);
        DocumentTableElement actualTable = Assert.IsType<DocumentTableElement>(result.Document.Sections[0].Elements[1]);
        Assert.IsType<DocumentTextElement>(actualTable.Cells[0, 0]!.Content);
        Assert.IsType<DocumentImageElement>(actualTable.Cells[0, 1]!.Content);
        Assert.IsType<DocumentExtensionElement>(actualTable.Cells[0, 2]!.Content);
        Assert.Collection(
            result.Document.Sections[0].Elements.Skip(1),
            element => Assert.IsType<DocumentTableElement>(element),
            element => Assert.Equal("provider.unknown", Assert.IsType<DocumentExtensionElement>(element).Type),
            element => Assert.Equal("provider.list", Assert.IsType<DocumentExtensionElement>(element).Type),
            element => Assert.Equal("provider.barcode", Assert.IsType<DocumentExtensionElement>(element).Type));
        Assert.Equal(new byte[] { 1, 2, 3 }, image.Content!.Value.ToArray());
    }

    [Fact]
    public void Build_RejectsRepeatedProviderIdEvenWhenTextMatches()
    {
        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(
            1,
            [
                new DocumentExtractionText("duplicate", 0, DocumentTextKind.Paragraph, "one", "same"),
                new DocumentExtractionText("duplicate", 1, DocumentTextKind.Paragraph, "two", "same")
            ]));

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_RejectsDuplicatePageNumbers()
    {
        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(2, []));
        builder.AddPage(new DocumentExtractionPage(2, []));

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_RejectsInvalidOrDuplicateTableAddresses()
    {
        using DocumentExtractionDocumentBuilder invalidBuilder = new("invalid");
        invalidBuilder.AddPage(new DocumentExtractionPage(
            1,
            [
                new DocumentExtractionTable(
                    "table",
                    0,
                    "table",
                    1,
                    1,
                    [new DocumentExtractionTableCell(1, 0, new DocumentExtractionText("cell", 0, DocumentTextKind.Paragraph, "cell"))])
            ]));
        Assert.Throws<ArgumentException>(() => invalidBuilder.Build());

        using DocumentExtractionDocumentBuilder duplicateBuilder = new("duplicate");
        duplicateBuilder.AddPage(new DocumentExtractionPage(
            1,
            [
                new DocumentExtractionTable(
                    "table",
                    0,
                    "table",
                    1,
                    1,
                    [
                        new DocumentExtractionTableCell(0, 0, new DocumentExtractionText("cell-1", 0, DocumentTextKind.Paragraph, "one")),
                        new DocumentExtractionTableCell(0, 0, new DocumentExtractionText("cell-2", 1, DocumentTextKind.Paragraph, "two"))
                    ])
            ]));
        Assert.Throws<ArgumentException>(() => duplicateBuilder.Build());
    }

    [Fact]
    public void BuildFailure_DisposesTransferredArtifacts()
    {
        using MemoryStream content = new(new byte[] { 1 });
        using DocumentArtifact artifact = new("image/png", content);
        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(
            1,
            [
                new DocumentExtractionImage("one", 0, "one", artifacts: [artifact]),
                new DocumentExtractionText("duplicate", 1, DocumentTextKind.Paragraph, "two"),
                new DocumentExtractionText("duplicate", 2, DocumentTextKind.Paragraph, "three")
            ]));

        Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.True(artifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => content.ReadByte());
    }

    [Fact]
    public async Task AsyncFinalization_CancellationDisposesPendingArtifacts()
    {
        using MemoryStream content = new(new byte[] { 1 });
        using DocumentArtifact artifact = new("image/png", content);
        using CancellationTokenSource cancellationSource = new();

        async IAsyncEnumerable<DocumentExtractionPage> PagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new DocumentExtractionPage(
                1,
                [new DocumentExtractionImage("image", 0, "image", artifacts: [artifact])]);
            cancellationSource.Cancel();
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => PagesAsync(cancellationSource.Token).ToDocumentExtractionResultAsync("document", cancellationSource.Token));

        Assert.True(artifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => content.ReadByte());
    }

    [Fact]
    public void DuplicatePages_DisposeAllArtifactsIncludingNestedCells()
    {
        using MemoryStream firstContent = new();
        using MemoryStream cellContent = new();
        using DocumentArtifact first = new("image/png", firstContent);
        using DocumentArtifact cell = new("image/png", cellContent);
        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(1, [new DocumentExtractionImage("image", 0, "image", artifacts: [first])]));
        builder.AddPage(new DocumentExtractionPage(1,
        [
            new DocumentExtractionTable("table", 0, "table", 1, 1,
            [
                new DocumentExtractionTableCell(0, 0, new DocumentExtractionImage("cell", 0, "cell", artifacts: [cell]))
            ])
        ]));

        Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.True(first.IsDisposed);
        Assert.True(cell.IsDisposed);
        Assert.False(firstContent.CanRead);
        Assert.False(cellContent.CanRead);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RepeatedArtifacts_DoNotPreventLaterArtifactCleanup(bool finalize)
    {
        using MemoryStream repeatedContent = new();
        using MemoryStream laterContent = new();
        using DocumentArtifact repeated = new("image/png", repeatedContent);
        using DocumentArtifact later = new("image/png", laterContent);
        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(1,
        [
            new DocumentExtractionImage("one", 0, "one", artifacts: [repeated]),
            new DocumentExtractionImage("two", 1, "two", artifacts: [repeated]),
            new DocumentExtractionImage("three", 2, "three", artifacts: [later])
        ]));

        if (finalize)
        {
            Assert.Throws<ArgumentException>(() => builder.Build());
        }
        else
        {
            builder.Dispose();
        }

        Assert.True(repeated.IsDisposed);
        Assert.True(later.IsDisposed);
        Assert.False(repeatedContent.CanRead);
        Assert.False(laterContent.CanRead);
    }

    [Fact]
    public void SuccessfulFinalization_TransfersArtifactLifetimeToResult()
    {
        using MemoryStream content = new();
        using DocumentArtifact artifact = new("image/png", content);
        using DocumentExtractionDocumentBuilder builder = new("document");
        builder.AddPage(new DocumentExtractionPage(1, [new DocumentExtractionImage("image", 0, "image", artifacts: [artifact])]));
        using DocumentExtractionResult result = builder.Build();

        builder.Dispose();
        Assert.False(artifact.IsDisposed);
        Assert.Same(artifact, Assert.Single(result.Document.Sections[0].Elements[0].Artifacts));
        result.Dispose();
        result.Dispose();
        Assert.True(result.IsDisposed);
        Assert.True(result.Document.IsDisposed);
        Assert.True(artifact.IsDisposed);
        Assert.False(content.CanRead);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsyncFinalization_FailureOrUncooperativeCancellationDisposesArtifacts(bool cancel)
    {
        using MemoryStream content = new();
        using DocumentArtifact artifact = new("image/png", content);
        using CancellationTokenSource cancellationSource = new();
        async IAsyncEnumerable<DocumentExtractionPage> PagesAsync()
        {
            yield return new DocumentExtractionPage(1, [new DocumentExtractionImage("image", 0, "image", artifacts: [artifact])]);
            await Task.Yield();
            if (cancel)
            {
                cancellationSource.Cancel();
            }
            else
            {
                throw new InvalidOperationException("Provider failed.");
            }
        }

        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PagesAsync().ToDocumentExtractionResultAsync("document", cancellationSource.Token));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => PagesAsync().ToDocumentExtractionResultAsync("document"));
        }

        Assert.True(artifact.IsDisposed);
        Assert.False(content.CanRead);
    }

    [Fact]
    public async Task ExtractAsync_UsesPageOperationAndLeavesSourceOwnedByCaller()
    {
        using MemoryStream source = new([42]);
        using PageClient client = new();
        using CancellationTokenSource cancellationSource = new();
        using DocumentExtractionResult result = await client.ExtractAsync(source, "document-id", "application/pdf", cancellationSource.Token);

        Assert.Equal("document-id", result.Document.Identifier);
        Assert.Same(source, client.Source);
        Assert.Equal("document-id", client.Identifier);
        Assert.Equal("application/pdf", client.MediaType);
        Assert.Equal(cancellationSource.Token, client.Token);
        Assert.Equal(1, client.CallCount);
        Assert.True(client.EnumeratorDisposed);
        Assert.Equal("text", Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.Sections[0].Elements)).Text);
        result.Dispose();
        client.Dispose();
        Assert.True(source.CanRead);
        Assert.Equal(42, source.ReadByte());
    }

    private sealed class PageClient : IDocumentExtractionClient
    {
        internal Stream? Source { get; private set; }
        internal string? Identifier { get; private set; }
        internal string? MediaType { get; private set; }
        internal CancellationToken Token { get; private set; }
        internal int CallCount { get; private set; }
        internal bool EnumeratorDisposed { get; private set; }

        public IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(Stream document, string identifier, string mediaType, CancellationToken cancellationToken = default)
        {
            Source = document;
            Identifier = identifier;
            MediaType = mediaType;
            Token = cancellationToken;
            CallCount++;
            return PagesAsync();
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
        }

        private async IAsyncEnumerable<DocumentExtractionPage> PagesAsync()
        {
            try
            {
                yield return new DocumentExtractionPage(1, [new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "markdown", "text")]);
                await Task.Yield();
            }
            finally
            {
                EnumeratorDisposed = true;
            }
        }
    }
}
