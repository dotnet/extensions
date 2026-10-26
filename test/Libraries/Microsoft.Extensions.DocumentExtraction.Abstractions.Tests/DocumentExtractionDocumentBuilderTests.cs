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
        DocumentSourcePage frame = new("page:1", 1);
        DocumentSourceReference firstReference = new(frame);
        DocumentSourceReference duplicateReference = new(frame);
        DocumentSourceReference observationReference = new(new DocumentSourcePage("provider-page:1", 1));
        DocumentSourceReference secondPageFirst = new(new DocumentSourcePage("second-page-first", 2));
        DocumentSourceReference secondPageLast = new(new DocumentSourcePage("second-page-last", 2));

        DocumentExtractionPage secondPage = new(
            2,
            [
                new DocumentExtractionText("provider-2", 2, DocumentTextKind.Paragraph, "second", "same", sources: [secondPageLast]),
                new DocumentExtractionText("provider-3", 1, DocumentTextKind.Paragraph, "first", "same", sources: [secondPageFirst])
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
                    sources: [observationReference])
            ],
            markdown: "page one",
            sources: [firstReference, duplicateReference]);

        using DocumentExtractionDocumentBuilder builder = new("document-id");
        builder.AddPage(secondPage);
        builder.AddPage(firstPage);

        using DocumentExtractionResult result = builder.Build();

        Assert.Equal(new[] { 1, 2 }, result.Pages.Select(static page => page.PageNumber));
        Assert.Equal(new[] { "same", "same", "same" }, result.Document.EnumerateContent().OfType<DocumentTextElement>().Select(static element => element.Text));
        DocumentTextElement firstElement = Assert.IsType<DocumentTextElement>(result.Document.Sections[0].Elements[0]);
        Assert.Equal(new[] { firstReference, duplicateReference, observationReference }, firstElement.Sources);
        Assert.Equal(new[] { firstReference, duplicateReference }, result.Document.Sections[0].Sources);
        Assert.Equal(new[] { secondPageFirst, secondPageLast },
            result.Document.Sections[1].Elements.Select(static element => Assert.Single(element.Sources)));
        Assert.Equal(new[] { "first", "second" }, result.Document.Sections[1].Elements.Select(static element => element.GetMarkdown()));
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
        Assert.True(content.CanRead);
        Assert.Same(artifact, Assert.Single(result.Document.Sections[0].Elements[0].Artifacts));
        result.Dispose();
        result.Dispose();
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

        Assert.False(content.CanRead);
    }

    [Fact]
    public async Task ExtractAsync_UsesPageOperationAndLeavesSourceOwnedByCaller()
    {
        using MemoryStream source = new([42]);
        using PageClient client = new();
        using CancellationTokenSource cancellationSource = new();
        using DocumentExtractionResult result = await client.ExtractAsync(source, "document-id", "application/pdf", cancellationToken: cancellationSource.Token);

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

        public IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
            Stream document, string identifier, string mediaType, DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
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

    [Fact]
    public void Sources_PreserveRepeatedReferencesAndBorrowedProviderMetadata()
    {
        DocumentSourceReference reference = new(new DocumentSourcePage("provider-page", 7));
        object providerData = new();
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7,
            [
                new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "visible", "visible",
                    sources: [reference, reference, reference], metadata: new Dictionary<string, object?> { ["vendor"] = providerData })
            ])
        }.ToDocumentExtractionResult("evidence");
        DocumentTextElement text = Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.EnumerateContent()));

        Assert.Equal(new[] { reference, reference, reference }, text.Sources);
        Assert.Same(providerData, text.Metadata["vendor"]);
        Assert.Single(text.Metadata);
        Assert.Equal("visible", result.Document.ToMarkdown());
        Assert.Empty(result.Document.SourceOutputs);
        Assert.Throws<ArgumentNullException>("page", () => new DocumentSourceReference(null!));
    }

    [Fact]
    public void Observation_PreservesOrderedPolygonFrameAndBorrowedNative()
    {
        DocumentSourceOutput output = new("operation", providerName: "synthetic", modelId: "model:v1");
        DocumentSourcePage frame = PdfFrame(output);
        DocumentPoint[] vertices = FidelityPolygon();
        using FidelityNative native = new();
        DocumentSourceReference reference = new(frame, vertices, native);
        List<DocumentSourceReference> sources = [reference];
        DocumentExtractionText observation = new("text", 2, DocumentTextKind.Header, "**heading**", "heading", 3, sources: sources);
        vertices[0] = new DocumentPoint(999, 999);
        sources.Clear();
        using DocumentExtractionResult result = new[] { new DocumentExtractionPage(7, [observation], sourcePage: frame) }.ToDocumentExtractionResult("polygon");
        DocumentTextElement text = Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.EnumerateContent()));
        DocumentSourceReference actual = Assert.Single(text.Sources);

        Assert.Equal(FidelityPolygon(), actual.Polygon);
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentPoint>)actual.Polygon).Clear());
        Assert.Same(frame, actual.Page);
        Assert.Equal("urn:frame:pdf-A", frame.Identifier);
        Assert.Equal(7, frame.PageNumber);
        Assert.Equal(new DocumentPageDimensions(200, 100), frame.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, frame.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, frame.CoordinateOrigin);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal("synthetic", actual.Page.SourceOutput!.ProviderName);
        Assert.Equal("model:v1", actual.Page.SourceOutput.ModelId);
        Assert.Same(native, actual.GetRawRepresentation());
        Assert.Equal(0, native.ToStringCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(DocumentTextKind.Header, text.Kind);
        Assert.Equal(3, text.HeaderLevel);
        Assert.Equal("heading", text.Text);
        Assert.Equal("**heading**", result.Document.ToMarkdown());
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentSourceReference>)observation.Sources).Clear());
    }

    [Fact]
    public void Build_PreservesPageOnlyAndPartialGeometryWithoutInventingMetadata()
    {
        DocumentSourceOutput output = new("urn:source:A", providerName: "synthetic", modelId: "engine:v1");
        DocumentSourcePage dimensions = new("dimensions", 7, new DocumentPageDimensions(200, 100));
        DocumentSourcePage unknown = new("unknown", 2);
        DocumentSourceReference[] sources = [new(dimensions), new(unknown, []), new(unknown, FidelityPolygon())];
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7,
            [
                new DocumentExtractionText("partial", 0, DocumentTextKind.Paragraph, "unchanged **body**", "unchanged body", sources: sources)
            ], sourcePage: dimensions, sourceOutputs: [output])
        }.ToDocumentExtractionResult("partial");
        DocumentElement text = Assert.Single(result.Document.EnumerateContent());

        Assert.Equal(sources, text.Sources);
        Assert.Empty(text.Sources[0].Polygon);
        Assert.Empty(text.Sources[1].Polygon);
        Assert.Null(text.Sources[0].GetRawRepresentation());
        Assert.Empty(text.Metadata);
        Assert.Equal(new DocumentPageDimensions(200, 100), dimensions.Dimensions);
        Assert.Null(dimensions.CoordinateUnit);
        Assert.Null(dimensions.CoordinateOrigin);
        Assert.Null(unknown.Dimensions);
        Assert.Null(unknown.CoordinateUnit);
        Assert.Null(unknown.CoordinateOrigin);
        Assert.Equal(FidelityPolygon(), text.Sources[2].Polygon);
        Assert.Equal(new[] { dimensions, unknown }, result.Document.SourcePages);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal("engine:v1", output.ModelId);
        Assert.Equal("unchanged **body**", result.Document.ToMarkdown());
        Assert.Equal("unchanged body", Assert.IsType<DocumentTextElement>(text).Text);
    }

    [Fact]
    public void Observation_ValidationAndAssociationSnapshotsFollowFinalContract()
    {
        DocumentSourceReference reference = new(new DocumentSourcePage("page:7", 7));
        using FidelityNative borrowed = new();
        List<DocumentSourceReference> references = [reference, reference];
        Dictionary<string, object?> metadata = new(StringComparer.Ordinal) { ["vendor"] = borrowed, ["Vendor"] = null };
        DocumentExtractionText observation = new("empty", 0, DocumentTextKind.Paragraph, "",
            sources: references, metadata: metadata);
        references.Clear();
        metadata.Clear();

        Assert.Equal(new[] { reference, reference }, observation.Sources);
        Assert.Equal(2, observation.Metadata.Count);
        Assert.Same(borrowed, observation.Metadata["vendor"]);
        Assert.Null(observation.Metadata["Vendor"]);
        Assert.False(observation.Metadata.ContainsKey("VENDOR"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object?>)observation.Metadata).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentSourceReference>)observation.Sources).Clear());
        Assert.Equal("", observation.Markdown);
        Assert.Null(observation.Text);
        Assert.Throws<ArgumentException>("providerId", () => new DocumentExtractionText("", 0, DocumentTextKind.Paragraph, ""));
        Assert.Throws<ArgumentOutOfRangeException>("order", () => new DocumentExtractionText("invalid", -1, DocumentTextKind.Paragraph, ""));
        Assert.Throws<ArgumentNullException>("markdown", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Paragraph, null!));
        Assert.Throws<ArgumentException>("sources", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Paragraph, "", sources: [null!]));
        Assert.Throws<ArgumentException>("sources", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Paragraph, "", artifacts: [null!]));
        Assert.Throws<ArgumentOutOfRangeException>("headerLevel", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Header, "", headerLevel: 11));
        using DocumentExtractionResult result = new[] { new DocumentExtractionPage(7, [observation]) }.ToDocumentExtractionResult("metadata");
        DocumentElement element = Assert.Single(result.Document.EnumerateContent());
        Assert.Equal(new[] { reference, reference }, element.Sources);
        Assert.Same(borrowed, element.Metadata["vendor"]);
        Assert.Null(element.Metadata["Vendor"]);
        Assert.False(element.Metadata.ContainsKey("VENDOR"));
        element.Metadata["vendor"] = "canonical change";
        Assert.Same(borrowed, observation.Metadata["vendor"]);
        result.Dispose();
        Assert.Equal(0, borrowed.DisposeCount);
        Assert.Equal(0, borrowed.ToStringCount);
    }

    [Fact]
    public void Page_SnapshotsObservationsAndReferencesWithoutDeduplicating()
    {
        DocumentExtractionText text = new("text", 0, DocumentTextKind.Paragraph, "body");
        DocumentSourceReference reference = new(new DocumentSourcePage("page:7", 7));
        using FidelityNative borrowed = new();
        DocumentSourceOutput output = new("operation");
        List<DocumentExtractionObservation> observations = [text];
        List<DocumentSourceReference> references = [reference, reference];
        Dictionary<string, object?> metadata = new(StringComparer.Ordinal) { ["vendor"] = borrowed, ["Vendor"] = null };
        List<DocumentSourceOutput> outputs = [output, output];
        DocumentExtractionPage page = new(7, observations, "page markdown", references, PdfFrame(output), outputs, metadata);
        observations.Clear();
        references.Clear();
        metadata.Clear();
        outputs.Clear();

        Assert.Same(text, Assert.Single(page.Observations));
        Assert.Equal(new[] { reference, reference }, page.Sources);
        Assert.Equal(2, page.Metadata.Count);
        Assert.Same(borrowed, page.Metadata["vendor"]);
        Assert.Null(page.Metadata["Vendor"]);
        Assert.False(page.Metadata.ContainsKey("VENDOR"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object?>)page.Metadata).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentSourceReference>)page.Sources).Clear());
        Assert.Equal(new[] { output, output }, page.SourceOutputs);
        Assert.Equal(7, page.SourcePage!.PageNumber);
        Assert.Equal("page markdown", page.Markdown);
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentExtractionObservation>)page.Observations).Clear());
        using DocumentExtractionResult result = new[] { page }.ToDocumentExtractionResult("snapshot");
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal(new[] { reference, reference }, result.Document.Sections[0].Elements[0].Sources);
        DocumentSection section = Assert.Single(result.Document.Sections);
        Assert.Equal(new[] { reference, reference }, section.Sources);
        Assert.Equal(2, section.Metadata.Count);
        Assert.Same(borrowed, section.Metadata["vendor"]);
        Assert.Null(section.Metadata["Vendor"]);
        Assert.False(section.Metadata.ContainsKey("VENDOR"));
        Assert.Empty(section.Elements[0].Metadata);
        section.Metadata["vendor"] = "canonical change";
        Assert.Same(borrowed, page.Metadata["vendor"]);
        Assert.Equal("page markdown", result.Document.GetMarkdown());
        Assert.Equal("body", result.Document.ToMarkdown());
        result.Dispose();
        Assert.Equal(0, borrowed.DisposeCount);
        Assert.Equal(0, borrowed.ToStringCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_InvalidNumberOrAssociationsRejectAccordingToFinalContract(int number)
    {
        List<DocumentExtractionObservation> observations = [new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "body")];
        Assert.Throws<ArgumentOutOfRangeException>("pageNumber", () => new DocumentExtractionPage(number, observations));
        Assert.Throws<ArgumentNullException>("observations", () => new DocumentExtractionPage(7, null!));
        Assert.Throws<ArgumentException>("observations", () => new DocumentExtractionPage(7, observations, sources: [null!]));
        Assert.Throws<ArgumentException>("observations", () => new DocumentExtractionPage(7, [null!]));
        Assert.Throws<ArgumentException>("observations", () => new DocumentExtractionPage(7, observations, sourceOutputs: [null!]));
        Assert.Equal("body", Assert.Single(observations).Markdown);
    }

    [Fact]
    public void TableCell_PreservesMergedAnchorSpansRolesAndEmptyGeometry()
    {
        DocumentSourcePage frame = PdfFrame();
        DocumentSourceReference layout = new(frame, FidelityPolygon());
        List<DocumentSourceReference> sources = [layout];
        DocumentExtractionTableCell anchor = new(0, 0, new DocumentExtractionText("anchor", 0, DocumentTextKind.Paragraph, "MERGED_A"), 2, 2, "columnHeader");
        DocumentExtractionTableCell empty = new(2, 2, null, kind: "empty", sources: sources);
        sources.Clear();
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7, [new DocumentExtractionTable("table", 0, "", 3, 3, [anchor, empty])])
        }.ToDocumentExtractionResult("cells");
        DocumentTableElement table = Assert.IsType<DocumentTableElement>(Assert.Single(result.Document.EnumerateContent().OfType<DocumentTableElement>()));
        DocumentTableCell sharedAnchor = table.Cells[0, 0]!;
        DocumentTableCell sharedEmpty = table.Cells[2, 2]!;

        Assert.Equal(2, sharedAnchor.RowSpan);
        Assert.Equal(2, sharedAnchor.ColumnSpan);
        Assert.Equal("columnHeader", sharedAnchor.Kind);
        Assert.Equal("MERGED_A", sharedAnchor.Content!.ToMarkdown());
        Assert.Empty(sharedAnchor.Sources);
        Assert.Equal(2, sharedEmpty.RowIndex);
        Assert.Equal(2, sharedEmpty.ColumnIndex);
        Assert.Null(sharedEmpty.Content);
        Assert.Equal("empty", sharedEmpty.Kind);
        Assert.False(table.Cells.IsCovered(2, 2));
        Assert.Same(sharedEmpty, table.Cells.GetCoveringCell(2, 2));
        Assert.Same(layout, Assert.Single(sharedEmpty.Sources));
        Assert.Equal(FidelityPolygon(), sharedEmpty.Sources[0].Polygon);
        Assert.Same(frame, Assert.Single(result.Document.SourcePages));
        Assert.Same(layout, Assert.Single(empty.Sources));
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentSourceReference>)empty.Sources).Clear());
    }

    [Theory]
    [InlineData(-1, 0, 1, 1, "row")]
    [InlineData(0, -1, 1, 1, "column")]
    [InlineData(0, 0, 0, 1, "rowSpan")]
    [InlineData(0, 0, -1, 1, "rowSpan")]
    [InlineData(0, 0, 1, 0, "columnSpan")]
    [InlineData(0, 0, 1, -1, "columnSpan")]
    public void TableCell_InvalidAddressesSpansOrRegionsRejectWithoutTakingCallerOwnership(int row, int column, int rowSpan, int columnSpan, string parameter)
    {
        using MemoryStream stream = new(new byte[] { 42, 43 });
        using DocumentArtifact artifact = new("image/png", stream);
        DocumentExtractionImage image = new("image", 0, "", artifacts: [artifact]);
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => new DocumentExtractionTableCell(row, column, image, rowSpan, columnSpan));
        Assert.True(stream.CanRead);
        Assert.Equal(42, stream.ReadByte());
        Assert.Same(artifact, Assert.Single(image.Artifacts));
    }

    [Theory]
    [InlineData("text")]
    [InlineData("image")]
    [InlineData("table")]
    [InlineData("extension")]
    public void Observation_AllDerivedTypesSnapshotSourcesArtifactsAndMetadataBeforeNestedFinalization(string kind)
    {
        using FidelityNative borrowed = new();
        FidelityStream stream = new([17, 23]);
        using DocumentArtifact artifact = new("application/octet-stream", stream);
        DocumentSourcePage frame = PdfFrame();
        DocumentSourceReference pageSource = new(frame);
        DocumentSourceReference ownSource = new(frame, FidelityPolygon(), borrowed);
        DocumentSourceReference otherSource = new(frame);
        List<DocumentSourceReference> sources = [ownSource, otherSource, ownSource];
        List<DocumentArtifact> artifacts = [artifact];
        Dictionary<string, object?> metadata = new(StringComparer.Ordinal) { ["vendor"] = borrowed, ["Vendor"] = null };
        DocumentExtractionObservation observation = kind switch
        {
            "text" => new DocumentExtractionText("nested", 0, DocumentTextKind.Paragraph, "visible", "plain",
                sources: sources, artifacts: artifacts, metadata: metadata),
            "image" => new DocumentExtractionImage("nested", 0, "visible", "plain",
                sources: sources, artifacts: artifacts, metadata: metadata),
            "table" => new DocumentExtractionTable("nested", 0, "visible", 0, 0,
                sources: sources, artifacts: artifacts, metadata: metadata),
            _ => new DocumentExtractionExtension("nested", 0, "vendor-kind", "visible", "plain",
                sources, artifacts, metadata, borrowed)
        };
        sources.Clear();
        artifacts.Clear();
        metadata.Clear();

        Assert.Equal(new[] { ownSource, otherSource, ownSource }, observation.Sources);
        Assert.Same(artifact, Assert.Single(observation.Artifacts));
        Assert.Same(borrowed, observation.Metadata["vendor"]);
        Assert.Null(observation.Metadata["Vendor"]);
        Assert.False(observation.Metadata.ContainsKey("VENDOR"));
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentSourceReference>)observation.Sources).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentArtifact>)observation.Artifacts).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object?>)observation.Metadata).Clear());

        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7,
                [new DocumentExtractionTable("outer", 0, "", 1, 1, [new DocumentExtractionTableCell(0, 0, observation)])],
                sources: [pageSource, pageSource])
        }.ToDocumentExtractionResult("nested-snapshots");
        DocumentTableElement outer = Assert.IsType<DocumentTableElement>(Assert.Single(result.Document.EnumerateContent()));
        DocumentElement element = outer.Cells[0, 0]!.Content!;
        Assert.Equal(new[] { pageSource, pageSource, ownSource, otherSource, ownSource }, element.Sources);
        Assert.Equal(new[] { pageSource, pageSource }, outer.Sources);
        Assert.Equal(new[] { pageSource, pageSource }, Assert.Single(result.Document.Sections).Sources);
        Assert.Equal(2, element.Metadata.Count);
        Assert.Same(borrowed, element.Metadata["vendor"]);
        Assert.Null(element.Metadata["Vendor"]);
        Assert.False(element.Metadata.ContainsKey("VENDOR"));
        Assert.Empty(outer.Metadata);
        Assert.Empty(result.Document.Sections[0].Metadata);
        Assert.Same(artifact, Assert.Single(element.Artifacts));
        if (kind == "extension")
        {
            Assert.Same(borrowed, Assert.IsType<DocumentExtensionElement>(element).Value);
        }

        element.Metadata.Clear();
        Assert.Equal(2, observation.Metadata.Count);
        Assert.Equal(new byte[] { 17, 23 }, stream.ToArray());
        Assert.Equal(0, stream.DisposeCount);
        result.Dispose();
        result.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
        Assert.Equal(0, borrowed.DisposeCount);
        Assert.Equal(0, borrowed.ToStringCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TableCell_SourcesSnapshotAndFinalizeIndependentlyOfContent(bool hasContent)
    {
        DocumentSourcePage frame = PdfFrame();
        DocumentSourceReference cellSource = new(frame, FidelityPolygon());
        DocumentSourceReference contentSource = new(frame);
        DocumentSourceReference pageSource = new(new DocumentSourcePage("page-level", 7));
        List<DocumentSourceReference> sources = [cellSource, cellSource];
        DocumentExtractionText? content = hasContent
            ? new("content", 0, DocumentTextKind.Paragraph, "visible", sources: [contentSource])
            : null;
        DocumentExtractionTableCell cell = new(0, 0, content, sources: sources);
        sources.Clear();
        Assert.Equal(new[] { cellSource, cellSource }, cell.Sources);
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentSourceReference>)cell.Sources).Clear());
        Assert.Same(content, cell.Content);
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7, [new DocumentExtractionTable("table", 0, "", 1, 1, [cell])], sources: [pageSource])
        }.ToDocumentExtractionResult("cell-sources");
        DocumentTableElement table = Assert.IsType<DocumentTableElement>(Assert.Single(result.Document.EnumerateContent()));
        DocumentTableCell actual = table.Cells[0, 0]!;
        Assert.Equal(new[] { cellSource, cellSource }, actual.Sources);
        Assert.Equal(new[] { pageSource }, table.Sources);
        Assert.Equal(new[] { pageSource.Page, frame }, result.Document.SourcePages);
        if (hasContent)
        {
            Assert.Equal(new[] { pageSource, contentSource }, actual.Content!.Sources);
            Assert.Equal("visible", Assert.IsType<DocumentTextElement>(actual.Content).ToMarkdown());
        }
        else
        {
            Assert.Null(actual.Content);
        }

        actual.Sources.Clear();
        Assert.Equal(new[] { cellSource, cellSource }, cell.Sources);
        if (hasContent)
        {
            Assert.Equal(new[] { pageSource, contentSource }, actual.Content!.Sources);
            Assert.Equal(new[] { contentSource }, content!.Sources);
        }

        Assert.False(table.Cells.IsCovered(0, 0));
        Assert.Same(actual, table.Cells.GetCoveringCell(0, 0));
    }

    [Fact]
    public void Build_PreservesPolygonOriginIdentityThroughPageEnumerationAndFinalization()
    {
        DocumentSourceOutput output = new("operation");
        DocumentSourcePage frame = PdfFrame(output);
        DocumentSourceReference layout = new(frame, FidelityPolygon());
        DocumentSourceReference reference = new(frame);
        DocumentExtractionPage seven = new(7,
        [
            new DocumentExtractionText("first", 1, DocumentTextKind.Paragraph, "same", "same", sources: [layout],
                metadata: new Dictionary<string, object?> { ["vendor-order"] = "first" }),
            new DocumentExtractionText("second", 1, DocumentTextKind.Paragraph, "same", "same", sources: [layout],
                metadata: new Dictionary<string, object?> { ["vendor-order"] = "second" })
        ], sources: [reference, reference], sourcePage: frame);
        DocumentExtractionPage two = new(2, [], sourcePage: new DocumentSourcePage("second-frame", 2, sourceOutput: output));
        using DocumentExtractionResult result = new[] { seven, two }.ToDocumentExtractionResult("order");
        DocumentSection section = result.Document.Sections[1];
        DocumentElement first = section.Elements[0];

        Assert.Equal(new[] { 2, 7 }, result.Pages.Select(static page => page.PageNumber));
        Assert.Equal(new[] { "same", "same" }, section.Elements.Select(static element => element.ToMarkdown()));
        Assert.Equal(new object?[] { "first", "second" }, section.Elements.Select(static element => element.Metadata["vendor-order"]));
        Assert.NotSame(first, section.Elements[1]);
        Assert.Same(first, result.Document.Sections[1].Elements[0]);
        Assert.Equal(new[] { reference, reference, layout }, first.Sources);
        Assert.Equal(FidelityPolygon(), layout.Polygon);
        Assert.Same(frame, layout.Page);
        result.Document.Sections.RemoveAt(1);
        result.Document.Sections.Insert(0, section);
        Assert.Same(first, result.Document.Sections[0].Elements[0]);
        Assert.Equal(7, first.Sources[2].Page.PageNumber);
        Assert.Equal("urn:frame:pdf-A", first.Sources[2].Page.Identifier);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
    }

    [Fact]
    public void Build_PreservesKnownPdfAndRasterOverlayFixturesWithoutGuessing()
    {
        DocumentSourceOutput output = new("overlay-operation");
        DocumentSourcePage pdf = PdfFrame(output);
        DocumentSourcePage raster = new("raster-frame", 7, new DocumentPageDimensions(400, 200), DocumentCoordinateUnit.Pixel, DocumentCoordinateOrigin.TopLeft, output);
        DocumentPoint[] overlay =
        [
            new(20.5, 159), new(81.5, 163.5), new(89, 79.75), new(25.75, 82.5), new(17, 129.5)
        ];
        DocumentSourceReference native = new(pdf, FidelityPolygon());
        DocumentSourceReference rendered = new(raster, overlay);
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7, [new DocumentExtractionText("overlay", 0, DocumentTextKind.Paragraph, "body", sources: [native, rendered])])
        }.ToDocumentExtractionResult("overlay");
        DocumentElement element = Assert.Single(result.Document.EnumerateContent());
        DocumentSourceReference original = element.Sources[0];
        DocumentSourceReference pixels = element.Sources[1];

        // This fixture owns the 2x scale and y-axis flip; no provider convention is inferred.
        DocumentPoint[] consumerOverlay = original.Polygon.Select(static point => new DocumentPoint(point.X * 2, (100 - point.Y) * 2)).ToArray();
        Assert.Equal(overlay, consumerOverlay);
        Assert.Equal(overlay, pixels.Polygon);
        Assert.Equal(FidelityPolygon(), original.Polygon);
        Assert.Equal(new DocumentPageDimensions(400, 200), pixels.Page.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Pixel, pixels.Page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.TopLeft, pixels.Page.CoordinateOrigin);
        Assert.Equal(new[] { pdf, raster }, result.Document.SourcePages);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal("body", result.Document.ToMarkdown());
    }

    [Fact]
    public void Build_PreservesMergedCoveredAndEmptyCellsAndProjectsContentOnce()
    {
        DocumentExtractionTable table = new("table", 0, "stored table", 3, 3,
        [
            new DocumentExtractionTableCell(0, 0, new DocumentExtractionText("anchor", 0, DocumentTextKind.Paragraph, "MERGED_A"), 2, 2, "columnHeader"),
            new DocumentExtractionTableCell(2, 0, new DocumentExtractionText("body", 0, DocumentTextKind.Paragraph, "B"), kind: "rowHeader"),
            new DocumentExtractionTableCell(2, 2, null)
        ]);
        using DocumentExtractionResult result = new[] { new DocumentExtractionPage(7, [table]) }.ToDocumentExtractionResult("merged");
        DocumentTableElement shared = Assert.IsType<DocumentTableElement>(result.Document.Sections[0].Elements[0]);
        DocumentTableCell anchor = shared.Cells[0, 0]!;
        foreach ((int row, int column) in new[] { (0, 1), (1, 0), (1, 1) })
        {
            Assert.True(shared.Cells.IsCovered(row, column));
            Assert.Null(shared.Cells[row, column]);
            Assert.Same(anchor, shared.Cells.GetCoveringCell(row, column));
        }

        Assert.False(shared.Cells.IsCovered(2, 2));
        Assert.Null(shared.Cells[2, 2]!.Content);
        Assert.Equal(new[] { (0, 0), (2, 0), (2, 2) }, shared.Cells.OfType<DocumentTableCell>().Select(static cell => (cell.RowIndex, cell.ColumnIndex)));
        Assert.Equal(
            "<table><tr><th rowspan=\"2\" colspan=\"2\" data-kind=\"columnHeader\">MERGED_A</th><td></td></tr>" +
            "<tr><td></td></tr><tr><th rowspan=\"1\" colspan=\"1\" data-kind=\"rowHeader\">B</th><td></td>" +
            "<td rowspan=\"1\" colspan=\"1\"></td></tr></table>", result.Document.ToMarkdown());
        Assert.Equal(new[] { "MERGED_A", "B" }, shared.Cells.Select(static cell => cell?.Content).OfType<DocumentTextElement>().Select(static text => text.ToMarkdown()));
        Assert.Equal("stored table", shared.GetMarkdown());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_InvalidMergedRegionsDisposesAllCollectedArtifacts(bool overlap)
    {
        FidelityStream firstStream = new(new byte[] { 1, 2 });
        FidelityStream nestedStream = new(new byte[] { 3, 4 });
        FidelityStream laterStream = new(new byte[] { 5, 6 });
        using DocumentArtifact first = new("image/png", firstStream);
        using DocumentArtifact nested = new("image/png", nestedStream);
        using DocumentArtifact later = new("image/png", laterStream);
        using MemoryStream caller = new(new byte[] { 42 });
        using DocumentExtractionDocumentBuilder builder = new("invalid-merged");
        builder.AddPage(new DocumentExtractionPage(7,
        [
            new DocumentExtractionTable("table", 0, "", 3, 3,
            [
                new DocumentExtractionTableCell(0, 0, new DocumentExtractionImage("first", 0, "", artifacts: [first]), overlap ? 2 : 4, 2),
                new DocumentExtractionTableCell(1, 1,
                    new DocumentExtractionTable("nested", 0, "", 1, 1,
                    [new DocumentExtractionTableCell(0, 0, new DocumentExtractionImage("nested-image", 0, "", artifacts: [nested]))]))
            ]),
            new DocumentExtractionImage("later", 1, "", artifacts: [later])
        ]));

        if (overlap)
        {
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }
        else
        {
            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        builder.Dispose();
        Assert.Equal(1, firstStream.DisposeCount);
        Assert.Equal(1, nestedStream.DisposeCount);
        Assert.Equal(1, laterStream.DisposeCount);
        Assert.False(firstStream.CanRead);
        Assert.False(nestedStream.CanRead);
        Assert.False(laterStream.CanRead);
        Assert.Equal(42, caller.ReadByte());
    }

    [Fact]
    public void Build_PreservesImageOnlyAndBlankPagesWithoutFabricatedText()
    {
        byte[] bytes = [0x89, 0x50, 0x4E, 0x47, 1, 2];
        FidelityStream stream = new(bytes);
        Uri source = new("urn:image:page7");
        using DocumentArtifact artifact = new("image/png", stream, source);
        DocumentSourcePage imageFrame = PdfFrame();
        DocumentSourcePage blankFrame = new("blank-frame", 2);
        DocumentExtractionImage observation = new("image", 0, "", content: bytes, mediaType: "image/png", source: source,
            alternativeText: "diagram only", artifacts: [artifact], sources: [new DocumentSourceReference(imageFrame, FidelityPolygon())]);
        using DocumentExtractionDocumentBuilder builder = new("image-only");
        builder.AddPage(new DocumentExtractionPage(7, [observation], sourcePage: imageFrame));
        builder.AddPage(new DocumentExtractionPage(2, [], sourcePage: blankFrame));
        using DocumentExtractionResult result = builder.Build();
        builder.Dispose();
        DocumentImageElement image = Assert.IsType<DocumentImageElement>(Assert.Single(result.Document.EnumerateContent()));

        Assert.Equal(new[] { 2, 7 }, result.Pages.Select(static page => page.PageNumber));
        Assert.All(result.Pages, static page => Assert.Null(page.Markdown));
        Assert.Empty(result.Document.Sections[0].Elements);
        Assert.Equal("", result.Document.Sections[0].StoredMarkdown);
        Assert.Equal("", image.GetMarkdown());
        Assert.Null(image.Text);
        Assert.Null(result.Document.StoredMarkdown);
        Assert.Equal("", result.Document.GetMarkdown());
        Assert.Equal(Environment.NewLine + "![diagram only](urn:image:page7)", result.Document.ToMarkdown());
        Assert.Equal(bytes, image.Content!.Value.ToArray());
        Assert.Equal(bytes, stream.ToArray());
        Assert.Equal("image/png", image.MediaType);
        Assert.Same(source, image.Source);
        Assert.Equal("diagram only", image.AlternativeText);
        Assert.Equal(new[] { blankFrame, imageFrame }, result.Document.SourcePages);
        Assert.Same(artifact, Assert.Single(image.Artifacts));
        Assert.Equal(0, stream.DisposeCount);
        result.Document.Dispose();
        result.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(bytes, image.Content.Value.ToArray());
    }

    [Fact]
    public void Build_PreservesStructuredPayloadSourcesAndMetadataInSharedDocumentAlone()
    {
        var payload = new { kind = "vendor-extra", nested = new { unknown = new object[] { 3, "x" } } };
        var attributes = new { vendorFlag = "kept", provenance = "urn:synthetic:engine:v1", target = "urn:target:extra-17" };
        DocumentSourceOutput output = new("payload-operation", providerName: "synthetic");
        DocumentSourcePage frame = PdfFrame(output);
        DocumentSourceReference pageSource = new(frame);
        DocumentSourceReference observationSource = new(frame, FidelityPolygon());
        Dictionary<string, object?> metadata = new(StringComparer.Ordinal) { ["vendor"] = attributes, ["Vendor"] = null };
        using Document document = BuildSharedDocument(new DocumentExtractionPage(7,
        [
            new DocumentExtractionExtension("extension", 0, "vendor-extra", "visible **text**", "visible text",
                sources: [observationSource], metadata: metadata, value: payload)
        ], sources: [pageSource], metadata: metadata));
        DocumentExtensionElement extension = Assert.IsType<DocumentExtensionElement>(Assert.Single(document.EnumerateContent()));

        Assert.Same(payload, extension.Value);
        Assert.Equal(new object[] { 3, "x" }, payload.nested.unknown);
        Assert.Equal("vendor-extra", extension.Type);
        Assert.Same(pageSource, Assert.Single(document.Sections[0].Sources));
        Assert.Same(attributes, document.Sections[0].Metadata["vendor"]);
        Assert.Equal(new[] { pageSource, observationSource }, extension.Sources);
        Assert.Equal(2, extension.Metadata.Count);
        Assert.Same(attributes, extension.Metadata["vendor"]);
        Assert.Null(extension.Metadata["Vendor"]);
        Assert.False(extension.Metadata.ContainsKey("VENDOR"));
        Assert.Equal("kept", attributes.vendorFlag);
        Assert.Equal("urn:synthetic:engine:v1", attributes.provenance);
        Assert.Equal("urn:target:extra-17", attributes.target);
        Assert.Same(extension, Assert.Single(document.Sections[0].Elements));
        Assert.Equal(FidelityPolygon(), extension.Sources[1].Polygon);
        Assert.Same(frame, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal("visible **text**", document.ToMarkdown());
        Assert.Equal("visible text", extension.Text);
    }

    [Fact]
    public void Build_OneOperationOutputSharedByMultiplePagesExcludesRawFromProjections()
    {
        using FidelityNative native = new();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("{\"marker\":\"RAW_BODY_ONLY_91\",\"pages\":[7,2]}");
        FidelityStream stream = new(bytes);
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("shared-operation", native, artifact, providerName: "synthetic", modelId: "model:v1");
        DocumentSourcePage seven = PdfFrame(output);
        DocumentSourcePage two = new("frame-two", 2, sourceOutput: output);
        using Document document = BuildSharedDocument(true,
            new DocumentExtractionPage(7, [new DocumentExtractionText("seven", 0, DocumentTextKind.Paragraph, "seven", "seven")], sourcePage: seven, sourceOutputs: [output]),
            new DocumentExtractionPage(2, [new DocumentExtractionText("two", 0, DocumentTextKind.Paragraph, "two", "two")], sourcePage: two, sourceOutputs: [output]));

        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal(new[] { two, seven }, document.SourcePages);
        Assert.Same(output, document.SourcePages[0].SourceOutput);
        Assert.Same(output, document.SourcePages[1].SourceOutput);
        Assert.Equal(DocumentRawContentStatus.Retained, output.ContentStatus);
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        Assert.Equal(bytes, stream.ToArray());
        Assert.Equal("two" + Environment.NewLine + "seven", document.ToMarkdown());
        Assert.Equal(new[] { "two", "seven" }, document.EnumerateContent().OfType<DocumentTextElement>().Select(static text => text.Text));
        Assert.Equal("", document.GetMarkdown());
        Assert.True(stream.CanRead);
        Assert.Equal(0, stream.DisposeCount);
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
    }

    private static DocumentPoint[] FidelityPolygon() =>
    [
        new(10.25, 20.5), new(40.75, 18.25), new(44.5, 60.125), new(12.875, 58.75), new(8.5, 35.25)
    ];

    private static DocumentSourcePage PdfFrame(DocumentSourceOutput? output = null) =>
        new("urn:frame:pdf-A", 7, new DocumentPageDimensions(200, 100), DocumentCoordinateUnit.Point, DocumentCoordinateOrigin.BottomLeft, output);

    private static Document BuildSharedDocument(params DocumentExtractionPage[] pages) => BuildSharedDocument(false, pages);

    private static Document BuildSharedDocument(bool captureRawContent, params DocumentExtractionPage[] pages)
    {
        using DocumentExtractionDocumentBuilder builder = new("shared-only", captureRawContent);
        foreach (DocumentExtractionPage page in pages)
        {
            builder.AddPage(page);
        }

        // The wrapper has no separate resources; ownership is handed to the shared Document.
        return builder.Build().Document;
    }

    private sealed class FidelityStream(byte[] bytes) : MemoryStream(bytes)
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

    private sealed class FidelityNative : IDisposable
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
    [InlineData(true)]
    [InlineData(false)]
    public void Build_RejectsForeignOwnedArtifactWithoutDisposingOwner(bool rawArtifact)
    {
        using Document owner = new("foreign-artifact-owner");
        byte[] foreignBytes = System.Text.Encoding.UTF8.GetBytes("{\"pages\":[2,3],\"unknown\":[91,92]}");
        FidelityStream foreignStream = new(foreignBytes);
        FidelityStream currentStream = new(new byte[] { 81, 82, 83 });
        FidelityStream pendingStream = new(new byte[] { 101, 102, 103 });
        using DocumentArtifact foreignArtifact = new("application/json", foreignStream);
        using DocumentArtifact currentArtifact = new("application/json", currentStream);
        using DocumentArtifact pendingArtifact = new("application/octet-stream", pendingStream);
        using FidelityNative native = new();
        DocumentSourceOutput foreignOutput = new("foreign-operation", native, foreignArtifact);
        DocumentSourceOutput currentOutput = new("current-operation", rawContent: currentArtifact);
        DocumentSourcePage ownerFrame = PdfFrame(foreignOutput);
        owner.AddSourcePage(ownerFrame);
        DocumentTextElement ownerText = new(DocumentTextKind.Paragraph, "owner **body**", "owner body");
        DocumentSection ownerSection = new("");
        ownerSection.Elements.Add(ownerText);
        owner.Sections.Add(ownerSection);
        Assert.True(foreignStream.CanRead);
        Assert.True(currentStream.CanRead);
        Assert.True(pendingStream.CanRead);

        DocumentExtractionPage first = new(1,
            [new DocumentExtractionText("first-text", 0, DocumentTextKind.Paragraph, "first body")],
            sourceOutputs: [currentOutput]);
        DocumentExtractionPage rejected = rawArtifact
            ? new DocumentExtractionPage(2,
                [new DocumentExtractionText("rejected-text", 0, DocumentTextKind.Paragraph, "rejected body")],
                sourcePage: new DocumentSourcePage("foreign-second-frame", 2, sourceOutput: foreignOutput),
                sourceOutputs: [foreignOutput])
            : new DocumentExtractionPage(2,
                [new DocumentExtractionImage("rejected-image", 0, "rejected image", artifacts: [foreignArtifact])]);
        DocumentExtractionPage later = new(3,
            [new DocumentExtractionImage("pending-image", 0, "pending image", artifacts: [pendingArtifact])],
            sourceOutputs: rawArtifact ? [foreignOutput] : []);
        using DocumentExtractionDocumentBuilder builder = new("doomed-finalization", true);

        // Arrival order differs from finalization order; page 1 transfers before page 2 rejects.
        builder.AddPage(later);
        builder.AddPage(rejected);
        builder.AddPage(first);

        Assert.Throws<InvalidOperationException>(() => builder.Build());

        // The catch must leave current-owned artifacts for document finally, and clean pending unowned artifacts.
        Assert.Equal(1, currentStream.DisposeCount);
        Assert.False(currentStream.CanRead);
        Assert.Equal(1, pendingStream.DisposeCount);
        Assert.False(pendingStream.CanRead);
        Assert.Equal(0, foreignStream.DisposeCount);
        Assert.True(foreignStream.CanRead);
        Assert.Equal(foreignBytes, foreignStream.ToArray());
        Assert.Equal(123, foreignStream.ReadByte());
        Assert.Equal(0, native.DisposeCount);
        Assert.Same(native, foreignOutput.GetRawRepresentation());
        Assert.Same(foreignArtifact, foreignOutput.GetRawContent());
        Assert.Same(foreignOutput, Assert.Single(owner.SourceOutputs));
        Assert.Same(ownerFrame, Assert.Single(owner.SourcePages));
        Assert.Same(ownerSection, Assert.Single(owner.Sections));
        Assert.Same(ownerText, Assert.Single(ownerSection.Elements));
        Assert.Equal("owner body", ownerText.Text);
        Assert.Equal("owner **body**", owner.ToMarkdown());
        if (rawArtifact)
        {
            Assert.Same(foreignOutput, Assert.Single(rejected.SourceOutputs));
            Assert.Same(foreignOutput, rejected.SourcePage!.SourceOutput);
            Assert.Same(foreignOutput, Assert.Single(later.SourceOutputs));
        }
        else
        {
            Assert.Same(foreignArtifact, Assert.Single(Assert.Single(rejected.Observations).Artifacts));
            Assert.Empty(rejected.SourceOutputs);
            Assert.Empty(later.SourceOutputs);
        }

        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Throws<InvalidOperationException>(() => builder.AddPage(first));
        builder.Dispose();
        builder.Dispose();
        Assert.Equal(1, currentStream.DisposeCount);
        Assert.Equal(1, pendingStream.DisposeCount);
        Assert.Equal(0, foreignStream.DisposeCount);
        Assert.True(foreignStream.CanRead);
        Assert.Equal(0, native.DisposeCount);
        owner.Dispose();
        owner.Dispose();
        Assert.Equal(1, foreignStream.DisposeCount);
        Assert.False(foreignStream.CanRead);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Finalization_DefaultAndFalseEntryPoints_DisableBodyCaptureWithoutRejectingNative(int entryPoint)
    {
        using FidelityNative native = new();
        DocumentSourceOutput output = new("native", native, contentCaptureDisabled: true);
        DocumentSourceReference annotation = new(PdfFrame(output), rawRepresentation: native);
        DocumentExtractionPage page = CaptureCarrierPage(output, annotation, "page");
        using CancellationTokenSource cancellation = new();
        using DocumentExtractionResult result = await FinalizeWithoutCapture(page, entryPoint, cancellation.Token);

        Assert.Same(page, Assert.Single(result.Pages));
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(annotation, Assert.Single(Assert.Single(result.Document.Sections).Sources));
        Assert.Same(native, annotation.GetRawRepresentation());
        Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
        Assert.Null(output.GetRawContent());
        result.Dispose();
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, native.ToStringCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Finalization_ExplicitCaptureEntryPoints_RetainOriginalBodyBytes(int entryPoint)
    {
        using FidelityNative native = new();
        byte[] bytes = CaptureBytes();
        FidelityStream stream = new(bytes);
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentExtractionPage page = new(7, [], sourceOutputs: [output]);
        using DocumentExtractionDocumentBuilder builder = new("document", true);
        builder.AddPage(page);
        using CancellationTokenSource cancellation = new();
        CancellationToken received = default;
        using DocumentExtractionResult result = entryPoint switch
        {
            0 => builder.Build(),
            1 => new[] { page }.ToDocumentExtractionResult("document", true),
            _ => await Pages().ToDocumentExtractionResultAsync("document", true, cancellation.Token)
        };
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        Assert.Same(artifact, Assert.Single(result.Document.SourceOutputs).GetRawContent());
        using MemoryStream copy = new();
        await artifact.Content.CopyToAsync(copy);
        Assert.Equal(bytes, copy.ToArray());
        if (entryPoint == 2)
        {
            Assert.Equal(cancellation.Token, received);
        }

        builder.Dispose();
        Assert.Equal(0, stream.DisposeCount);
        result.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);

        async IAsyncEnumerable<DocumentExtractionPage> Pages([EnumeratorCancellation] CancellationToken token = default)
        {
            received = token;
            yield return page;
            await Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData("page")]
    [InlineData("observation")]
    [InlineData("table")]
    [InlineData("cell")]
    [InlineData("empty-cell")]
    [InlineData("nested-cell")]
    [InlineData("nested-cell-source")]
    public async Task Finalization_DefaultAndFalseCapture_PreserveNativeSourcesAtEveryCarrier(string carrier)
    {
        foreach (int entryPoint in new[] { 2, 3, 4, 5 })
        {
            foreach (int sourceMode in new[] { 0, 1 })
            {
                await VerifyNativeSources(entryPoint, sourceMode, carrier);
            }
        }

        static async Task VerifyNativeSources(int entryPoint, int sourceMode, string carrier)
        {
            using FidelityNative native = new();
            using FidelityNative annotationNative = new();
            DocumentSourceOutput output = new("operation", native, contentCaptureDisabled: true);
            DocumentSourcePage frame = PdfFrame(sourceMode == 0 ? null : output);
            DocumentSourceReference annotation = new(frame, FidelityPolygon(), annotationNative);
            DocumentExtractionPage page = CaptureCarrierPage(output, annotation, carrier);
            using DocumentExtractionResult result = await FinalizeWithoutCapture(page, entryPoint);

            DocumentSourceReference[] projected = ProjectedSources(result.Document).ToArray();
            Assert.Equal(carrier == "page" ? 2 : 1, projected.Length);
            Assert.All(projected, source => Assert.Same(annotation, source));
            Assert.Same(annotationNative, annotation.GetRawRepresentation());
            Assert.Same(frame, annotation.Page);
            Assert.Same(frame, Assert.Single(result.Document.SourcePages));
            if (sourceMode == 0)
            {
                Assert.Empty(result.Document.SourceOutputs);
            }
            else
            {
                Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
            }

            Assert.Same(native, output.GetRawRepresentation());
            Assert.Same(sourceMode == 1 ? output : null, annotation.Page.SourceOutput);
            Assert.Equal(FidelityPolygon(), annotation.Polygon);
            Assert.Null(output.GetRawContent());
            Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
            result.Dispose();
            Assert.Equal(0, native.DisposeCount);
            Assert.Equal(0, annotationNative.DisposeCount);
            Assert.Equal(0, native.ToStringCount);
            Assert.Equal(0, annotationNative.ToStringCount);
        }
    }

    [Theory]
    [InlineData("page-output")]
    [InlineData("frame-output")]
    [InlineData("page")]
    [InlineData("observation")]
    [InlineData("table")]
    [InlineData("cell")]
    [InlineData("empty-cell")]
    [InlineData("nested-cell")]
    [InlineData("nested-cell-source")]
    public async Task Finalization_OptOutRejectsRawAtEveryCarrierAndCleansAcquiredBodies(string carrier)
    {
        foreach (int entryPoint in new[] { 2, 3, 4, 5 })
        {
            using FidelityNative native = new();
            FidelityStream stream = new(CaptureBytes());
            using DocumentArtifact artifact = new("application/json", stream);
            DocumentSourceOutput output = new("operation", native, artifact);
            DocumentSourceReference annotation = new(PdfFrame(output), rawRepresentation: native);
            DocumentExtractionPage page = CaptureCarrierPage(output, annotation, carrier);
            ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => FinalizeWithoutCapture(page, entryPoint));

            Assert.Equal("pages", error.ParamName);
            Assert.Contains("content capture opt-out", error.Message);
            Assert.Equal(1, stream.DisposeCount);
            Assert.False(stream.CanRead);
            Assert.Equal(0, native.DisposeCount);
        }

    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Finalization_OptOutAllowsNativeOnlyButRejectsContentOnly(bool hasNative, bool hasBody)
    {
        foreach (int entryPoint in new[] { 0, 1, 2, 3, 4, 5 })
        {
            await VerifyNativeAndContent(entryPoint, hasNative, hasBody);
        }

        static async Task VerifyNativeAndContent(int entryPoint, bool hasNative, bool hasBody)
        {
            using FidelityNative native = new();
            FidelityStream stream = new(CaptureBytes());
            using DocumentArtifact artifact = new("application/json", stream);
            DocumentSourceOutput output = new("operation", hasNative ? native : null, hasBody ? artifact : null,
                contentCaptureDisabled: !hasBody);
            DocumentExtractionPage page = new(7, [], sourceOutputs: [output]);
            if (hasBody)
            {
                ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => FinalizeWithoutCapture(page, entryPoint));
                Assert.Equal("pages", error.ParamName);
                Assert.Equal(1, stream.DisposeCount);
                Assert.False(stream.CanRead);
            }
            else
            {
                using DocumentExtractionResult result = await FinalizeWithoutCapture(page, entryPoint);
                Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
                Assert.Same(native, output.GetRawRepresentation());
                Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
                Assert.Null(output.GetRawContent());
                Assert.Equal(0, stream.DisposeCount);
            }

            Assert.Equal(0, native.DisposeCount);
            Assert.Equal(0, native.ToStringCount);
        }
    }

    [Theory]
    [InlineData("page")]
    [InlineData("observation")]
    [InlineData("table")]
    [InlineData("cell")]
    [InlineData("empty-cell")]
    [InlineData("nested-cell")]
    [InlineData("nested-cell-source")]
    public async Task Finalization_OptOutRejectsSourcePageRawAtNestedDepths(string carrier)
    {
        foreach (int entryPoint in new[] { 2, 3, 4, 5 })
        {
            using FidelityNative native = new();
            FidelityStream stream = new(CaptureBytes());
            using DocumentArtifact artifact = new("application/json", stream);
            DocumentSourceOutput output = new("region-only", native, artifact);
            DocumentSourcePage frame = PdfFrame(output);
            DocumentSourceReference annotation = new(frame, FidelityPolygon(), native);
            DocumentExtractionPage page = CaptureCarrierPage(output, annotation, carrier);
            ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => FinalizeWithoutCapture(page, entryPoint));

            Assert.Equal("pages", error.ParamName);
            Assert.Empty(page.SourceOutputs);
            Assert.Null(page.SourcePage);
            Assert.Same(output, annotation.Page.SourceOutput);
            Assert.Equal(FidelityPolygon(), annotation.Polygon);
            Assert.Same(native, annotation.GetRawRepresentation());
            Assert.Equal(1, stream.DisposeCount);
            Assert.False(stream.CanRead);
            Assert.Equal(0, native.DisposeCount);
        }
    }

    [Theory]
    [InlineData("page-output")]
    [InlineData("frame-output")]
    [InlineData("page")]
    [InlineData("observation")]
    [InlineData("table")]
    [InlineData("cell")]
    [InlineData("empty-cell")]
    [InlineData("nested-cell")]
    [InlineData("nested-cell-source")]
    public void Builder_DisposeBeforeBuild_CleansAbandonedBodiesAtEveryCarrier(string carrier)
    {
        using FidelityNative native = new();
        FidelityStream stream = new(CaptureBytes());
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentSourceReference annotation = new(PdfFrame(output), rawRepresentation: native);
        using DocumentExtractionDocumentBuilder builder = new("abandoned", true);
        builder.AddPage(CaptureCarrierPage(output, annotation, carrier));
        builder.AddPage(new DocumentExtractionPage(2, [], sourceOutputs: [output]));
        builder.Dispose();
        builder.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
        Assert.Equal(0, native.DisposeCount);
        Assert.Same(native, annotation.GetRawRepresentation());
        Assert.Throws<ObjectDisposedException>(() => builder.Build());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Result_DisposalOrders_CloseTransferredBodyOnceAndLeaveBorrowedObjectsAlive(bool documentFirst)
    {
        using MemoryStream input = new([42]);
        using FidelityNative native = new();
        using FidelityNative annotationNative = new();
        using FidelityNative opaque = new();
        FidelityStream stream = new(CaptureBytes());
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("shared", native, artifact);
        DocumentSourceReference annotation = new(PdfFrame(output), rawRepresentation: annotationNative);
        using DocumentExtractionDocumentBuilder builder = new("shared", true);
        DocumentExtractionPage carrierPage = CaptureCarrierPage(output, annotation, "nested-cell");
        builder.AddPage(new DocumentExtractionPage(carrierPage.PageNumber, carrierPage.Observations,
            metadata: new Dictionary<string, object?> { ["vendor"] = opaque }));
        builder.AddPage(new DocumentExtractionPage(2, [], sourceOutputs: [output]));
        using DocumentExtractionResult result = builder.Build();
        builder.Dispose();
        Assert.Same(artifact, Assert.Single(result.Document.SourceOutputs).GetRawContent());
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(annotation, Assert.Single(ProjectedSources(result.Document)));
        Assert.Same(opaque, result.Document.Sections[1].Metadata["vendor"]);
        Assert.True(stream.CanRead);
        Assert.Equal(0, stream.DisposeCount);
        if (documentFirst)
        {
            result.Document.Dispose();
            Assert.False(stream.CanRead);
            Assert.Equal(1, stream.DisposeCount);
        }

        result.Dispose();
        result.Document.Dispose();
        result.Dispose();
        builder.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(0, annotationNative.DisposeCount);
        Assert.Equal(0, opaque.DisposeCount);
        Assert.Equal(42, input.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_ExplicitCaptureWithoutBody_PreservesUnavailableContentStatus(bool hasNative)
    {
        using FidelityNative native = new();
        DocumentSourceOutput output = new("operation", hasNative ? native : null);
        using DocumentExtractionDocumentBuilder builder = new("native-only", true);
        builder.AddPage(new DocumentExtractionPage(7, [], sourceOutputs: [output]));
        using DocumentExtractionResult result = builder.Build();
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Same(hasNative ? native : null, output.GetRawRepresentation());
        Assert.Null(output.GetRawContent());
        Assert.All(result.Document.SourceOutputs, static source => Assert.Null(source.GetRawContent()));
        Assert.Equal(DocumentRawContentStatus.Unavailable, output.ContentStatus);
        result.Dispose();
        Assert.Equal(0, native.DisposeCount);
    }

    private static byte[] CaptureBytes() => System.Text.Encoding.UTF8.GetBytes(
        " {\r\n \"unknown\" : { \"z\" : [3, 1] }, \"pages\" : [7,2], \"marker\":\"RAW_BODY_ONLY_91\" \r\n}\n");

    private static DocumentExtractionPage CaptureCarrierPage(DocumentSourceOutput output, DocumentSourceReference annotation, string carrier)
    {
        DocumentExtractionObservation observation = new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "visible",
            sources: carrier is "observation" or "nested-cell" ? [annotation] : null);
        bool isNestedCell = carrier is "nested-cell" or "nested-cell-source";
        if (isNestedCell || carrier is "table" or "cell" or "empty-cell")
        {
            observation = new DocumentExtractionTable("inner", 0, "", 1, 1,
                [new DocumentExtractionTableCell(0, 0, carrier == "empty-cell" ? null : observation,
                    sources: carrier is "cell" or "empty-cell" or "nested-cell-source" ? [annotation] : null)],
                sources: carrier == "table" ? [annotation] : null);
            if (isNestedCell)
            {
                observation = new DocumentExtractionTable("outer", 0, "", 1, 1, [new DocumentExtractionTableCell(0, 0, observation)]);
            }
        }

        return new(7, [observation],
            sourcePage: carrier == "frame-output" ? PdfFrame(output) : null,
            sources: carrier == "page" ? [annotation] : null,
            sourceOutputs: carrier == "page-output" ? [output] : null);
    }

    private static async Task<DocumentExtractionResult> FinalizeWithoutCapture(
        DocumentExtractionPage page, int entryPoint, CancellationToken token = default)
    {
        if (entryPoint < 2)
        {
            using DocumentExtractionDocumentBuilder builder = entryPoint == 0 ? new("document") : new("document", false);
            builder.AddPage(page);
            return builder.Build();
        }

        return entryPoint switch
        {
            2 => new[] { page }.ToDocumentExtractionResult("document"),
            3 => new[] { page }.ToDocumentExtractionResult("document", false),
            4 => await Pages().ToDocumentExtractionResultAsync("document", token),
            _ => await Pages().ToDocumentExtractionResultAsync("document", false, token)
        };

        async IAsyncEnumerable<DocumentExtractionPage> Pages()
        {
            yield return page;
            await Task.CompletedTask;
        }
    }

    private static IEnumerable<DocumentSourceReference> ProjectedSources(Document document)
    {
        return document.Sections.SelectMany(static section => section.Sources)
            .Concat(document.Sections.SelectMany(static section => section.Elements).SelectMany(ElementSources));

        static IEnumerable<DocumentSourceReference> ElementSources(DocumentElement element)
        {
            IEnumerable<DocumentSourceReference> annotations = element.Sources;
            if (element is DocumentTableElement table)
            {
                annotations = annotations.Concat(table.Cells.OfType<DocumentTableCell>().SelectMany(static cell =>
                    cell.Sources.Concat(cell.Content is { } content ? ElementSources(content) : [])));
            }

            return annotations;
        }
    }
}
