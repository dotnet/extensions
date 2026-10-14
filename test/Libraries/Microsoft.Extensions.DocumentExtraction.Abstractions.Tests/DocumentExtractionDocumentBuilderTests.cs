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
    public void Evidence_PreservesReferenceRoleConfidenceAndProvenance()
    {
        DocumentEvidenceReference reference = new("urn:evidence:42", "vendor-token");
        DocumentExtractionEvidence supplied = new(reference, "caption");
        DocumentAnnotation confidence = new("recognition", confidence: new DocumentConfidence(73, "vendor certainty", 0, 100));
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7,
            [
                new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "visible", "visible",
                    evidence: [supplied, new DocumentExtractionEvidence(reference), supplied], annotations: [confidence])
            ])
        }.ToDocumentExtractionResult("evidence");
        DocumentTextElement text = Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.EnumerateContent()));

        Assert.Equal(new[] { reference, reference, reference }, text.Evidence);
        Assert.Equal(new[] { "vendor-token", "vendor-token", "recognition" }, text.Annotations.Select(static annotation => annotation.Kind));
        Assert.Equal(new[] { "caption", "caption", null }, text.Annotations.Select(static annotation => annotation.Role));
        Assert.Equal("urn:evidence:42", text.Annotations[0].Value);
        Assert.Same(confidence, text.Annotations[2]);
        Assert.Equal(73, text.Annotations[2].Confidence!.Value);
        Assert.Equal(100, text.Annotations[2].Confidence!.Maximum);
        Assert.Equal("visible", result.Document.ToMarkdown());
        Assert.Empty(result.Document.SourceOutputs);
        Assert.Throws<ArgumentNullException>("reference", () => new DocumentExtractionEvidence(null!));
    }

    [Fact]
    public void Observation_PreservesOrderedPolygonFrameAndReportedConfidence()
    {
        DocumentSourceOutput output = new("operation", providerName: "synthetic", modelId: "model:v1");
        DocumentSourcePage frame = PdfFrame(output);
        DocumentPoint[] vertices = FidelityPolygon();
        DocumentRegion region = new(frame, vertices);
        DocumentAnnotation annotation = new("layout", role: "body", regions: [region],
            confidence: new DocumentConfidence(73, "recognition", 0, 100), sourceOutput: output);
        List<DocumentAnnotation> annotations = [annotation];
        DocumentExtractionText observation = new("text", 2, DocumentTextKind.Header, "**heading**", "heading", 3, annotations: annotations);
        vertices[0] = new DocumentPoint(999, 999);
        annotations.Clear();
        using DocumentExtractionResult result = new[] { new DocumentExtractionPage(7, [observation], sourcePage: frame) }.ToDocumentExtractionResult("polygon");
        DocumentTextElement text = Assert.IsType<DocumentTextElement>(Assert.Single(result.Document.EnumerateContent()));
        DocumentAnnotation actual = Assert.Single(text.Annotations);

        Assert.Equal(FidelityPolygon(), Assert.Single(actual.Regions).Polygon);
        Assert.Same(frame, actual.Regions[0].Page);
        Assert.Equal("urn:frame:pdf-A", frame.Identifier);
        Assert.Equal(7, frame.PageNumber);
        Assert.Equal(new DocumentPageDimensions(200, 100), frame.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, frame.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, frame.CoordinateOrigin);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal("synthetic", actual.SourceOutput!.ProviderName);
        Assert.Equal("model:v1", actual.SourceOutput.ModelId);
        Assert.Equal(73, actual.Confidence!.Value);
        Assert.Equal("recognition", actual.Confidence.Kind);
        Assert.Equal(0, actual.Confidence.Minimum);
        Assert.Equal(100, actual.Confidence.Maximum);
        Assert.Equal(DocumentTextKind.Header, text.Kind);
        Assert.Equal(3, text.HeaderLevel);
        Assert.Equal("heading", text.Text);
        Assert.Equal("**heading**", result.Document.ToMarkdown());
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentAnnotation>)observation.Annotations).Clear());
    }

    [Fact]
    public void Build_PreservesAbsentPartialGeometryAndConfidenceProvenance()
    {
        DocumentSourceOutput output = new("urn:source:A", providerName: "synthetic", modelId: "engine:v1");
        DocumentSourcePage dimensions = new("dimensions", 7, new DocumentPageDimensions(200, 100));
        DocumentSourcePage unknown = new("unknown", 2);
        DocumentAnnotation[] annotations =
        [
            new("absent"),
            new("dimensions-only"),
            new("polygon-only", regions: [new DocumentRegion(unknown, FidelityPolygon())]),
            new("score-only", confidence: new DocumentConfidence(-2.5, "uncalibrated margin", -10, 10)),
            new("symbol", value: "urn:synthetic:engine:v1",
                confidence: new DocumentConfidence(0.73, "provider-reported symbol certainty", 0, 1), sourceOutput: output),
            new("unscaled", confidence: new DocumentConfidence(-12.5, "vendor margin"))
        ];
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7,
            [
                new DocumentExtractionText("partial", 0, DocumentTextKind.Paragraph, "unchanged **body**", "unchanged body", annotations: annotations)
            ], sourcePage: dimensions)
        }.ToDocumentExtractionResult("partial");
        DocumentElement text = Assert.Single(result.Document.EnumerateContent());

        Assert.Equal(annotations, text.Annotations);
        Assert.Empty(text.Annotations[0].Regions);
        Assert.Null(text.Annotations[0].Confidence);
        Assert.Null(text.Annotations[0].Role);
        Assert.Null(text.Annotations[0].SourceOutput);
        Assert.Empty(text.Annotations[1].Regions);
        Assert.Null(text.Annotations[1].Confidence);
        Assert.Equal(new DocumentPageDimensions(200, 100), dimensions.Dimensions);
        Assert.Null(dimensions.CoordinateUnit);
        Assert.Null(dimensions.CoordinateOrigin);
        Assert.Null(unknown.Dimensions);
        Assert.Null(unknown.CoordinateUnit);
        Assert.Null(unknown.CoordinateOrigin);
        Assert.Equal(FidelityPolygon(), text.Annotations[2].Regions[0].Polygon);
        Assert.Null(text.Annotations[2].Confidence);
        Assert.Empty(text.Annotations[3].Regions);
        Assert.Equal(-2.5, text.Annotations[3].Confidence!.Value);
        Assert.Equal("uncalibrated margin", text.Annotations[3].Confidence!.Kind);
        Assert.Equal(-10, text.Annotations[3].Confidence!.Minimum);
        Assert.Equal(10, text.Annotations[3].Confidence!.Maximum);
        Assert.Equal(new[] { dimensions, unknown }, result.Document.SourcePages);
        Assert.Empty(text.Annotations[4].Regions);
        Assert.Equal(0.73, text.Annotations[4].Confidence!.Value);
        Assert.Equal("provider-reported symbol certainty", text.Annotations[4].Confidence!.Kind);
        Assert.Equal(0, text.Annotations[4].Confidence!.Minimum);
        Assert.Equal(1, text.Annotations[4].Confidence!.Maximum);
        Assert.Equal("urn:synthetic:engine:v1", text.Annotations[4].Value);
        Assert.Same(output, text.Annotations[4].SourceOutput);
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal("engine:v1", output.ModelId);
        Assert.Equal(-12.5, text.Annotations[5].Confidence!.Value);
        Assert.Null(text.Annotations[5].Confidence!.Minimum);
        Assert.Null(text.Annotations[5].Confidence!.Maximum);
        Assert.Equal("unchanged **body**", result.Document.ToMarkdown());
        Assert.Equal("unchanged body", Assert.IsType<DocumentTextElement>(text).Text);
    }

    [Fact]
    public void Observation_ValidationAndAssociationSnapshotsFollowFinalContract()
    {
        DocumentPageReference reference = new("page:7");
        DocumentExtractionEvidence evidence = new(new DocumentEvidenceReference("evidence:7"));
        DocumentAnnotation annotation = new("layout");
        List<DocumentPageReference> references = [reference, reference];
        List<DocumentExtractionEvidence> evidenceList = [evidence];
        List<DocumentAnnotation> annotations = [annotation];
        DocumentExtractionText observation = new("empty", 0, DocumentTextKind.Paragraph, "",
            pageReferences: references, evidence: evidenceList, annotations: annotations);
        references.Clear();
        evidenceList.Clear();
        annotations.Clear();

        Assert.Equal(new[] { reference, reference }, observation.PageReferences);
        Assert.Same(evidence, Assert.Single(observation.Evidence));
        Assert.Same(annotation, Assert.Single(observation.Annotations));
        Assert.Equal("", observation.Markdown);
        Assert.Null(observation.Text);
        Assert.Throws<ArgumentException>("providerId", () => new DocumentExtractionText("", 0, DocumentTextKind.Paragraph, ""));
        Assert.Throws<ArgumentOutOfRangeException>("order", () => new DocumentExtractionText("invalid", -1, DocumentTextKind.Paragraph, ""));
        Assert.Throws<ArgumentNullException>("markdown", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Paragraph, null!));
        Assert.Throws<ArgumentException>("pageReferences", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Paragraph, "", annotations: [null!]));
        Assert.Throws<ArgumentOutOfRangeException>("headerLevel", () => new DocumentExtractionText("invalid", 0, DocumentTextKind.Header, "", headerLevel: 11));
        Assert.Equal(new[] { reference, reference }, observation.PageReferences);
    }

    [Fact]
    public void Page_SnapshotsObservationsAndReferencesWithoutDeduplicating()
    {
        DocumentExtractionText text = new("text", 0, DocumentTextKind.Paragraph, "body");
        DocumentPageReference reference = new("page:7");
        DocumentAnnotation annotation = new("page-layout");
        DocumentSourceOutput output = new("operation");
        List<DocumentExtractionObservation> observations = [text];
        List<DocumentPageReference> references = [reference, reference];
        List<DocumentAnnotation> annotations = [annotation];
        List<DocumentSourceOutput> outputs = [output, output];
        DocumentExtractionPage page = new(7, observations, "page markdown", references, PdfFrame(output), annotations, outputs);
        observations.Clear();
        references.Clear();
        annotations.Clear();
        outputs.Clear();

        Assert.Same(text, Assert.Single(page.Observations));
        Assert.Equal(new[] { reference, reference }, page.PageReferences);
        Assert.Same(annotation, Assert.Single(page.Annotations));
        Assert.Equal(new[] { output, output }, page.SourceOutputs);
        Assert.Equal(7, page.SourcePage!.PageNumber);
        Assert.Equal("page markdown", page.Markdown);
        Assert.Throws<NotSupportedException>(() => ((IList<DocumentExtractionObservation>)page.Observations).Clear());
        using DocumentExtractionResult result = new[] { page }.ToDocumentExtractionResult("snapshot");
        Assert.Same(output, Assert.Single(result.Document.SourceOutputs));
        Assert.Equal(new[] { reference, reference }, result.Document.Sections[0].Elements[0].PageReferences);
        Assert.Equal("page markdown", result.Document.GetMarkdown());
        Assert.Equal("body", result.Document.ToMarkdown());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_InvalidNumberOrAssociationsRejectAccordingToFinalContract(int number)
    {
        List<DocumentExtractionObservation> observations = [new DocumentExtractionText("text", 0, DocumentTextKind.Paragraph, "body")];
        Assert.Throws<ArgumentOutOfRangeException>("pageNumber", () => new DocumentExtractionPage(number, observations));
        Assert.Throws<ArgumentNullException>("observations", () => new DocumentExtractionPage(7, null!));
        Assert.Throws<ArgumentException>("observations", () => new DocumentExtractionPage(7, observations, annotations: [null!]));
        Assert.Throws<ArgumentException>("observations", () => new DocumentExtractionPage(7, observations, sourceOutputs: [null!]));
        Assert.Equal("body", Assert.Single(observations).Markdown);
    }

    [Fact]
    public void TableCell_PreservesMergedAnchorSpansRolesAndEmptyGeometry()
    {
        DocumentSourcePage frame = PdfFrame();
        DocumentAnnotation layout = new("empty-layout", regions: [new DocumentRegion(frame, FidelityPolygon())],
            confidence: new DocumentConfidence(0.73, "symbol certainty", 0, 1));
        List<DocumentAnnotation> annotations = [layout];
        DocumentExtractionTableCell anchor = new(0, 0, new DocumentExtractionText("anchor", 0, DocumentTextKind.Paragraph, "MERGED_A"), 2, 2, "columnHeader");
        DocumentExtractionTableCell empty = new(2, 2, null, kind: "empty", annotations: annotations);
        annotations.Clear();
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
        Assert.Equal("s0/e0/c0,0", sharedAnchor.Address);
        Assert.Equal("s0/e0/c2,2", sharedEmpty.Address);
        Assert.Equal(2, sharedEmpty.RowIndex);
        Assert.Equal(2, sharedEmpty.ColumnIndex);
        Assert.Null(sharedEmpty.Content);
        Assert.Equal("empty", sharedEmpty.Kind);
        Assert.False(table.Cells.IsCovered(2, 2));
        Assert.Same(sharedEmpty, table.Cells.GetCoveringCell(2, 2));
        Assert.Same(layout, Assert.Single(sharedEmpty.Annotations));
        Assert.Equal(FidelityPolygon(), sharedEmpty.Annotations[0].Regions[0].Polygon);
        Assert.Equal(0.73, sharedEmpty.Annotations[0].Confidence!.Value);
        Assert.Equal("symbol certainty", sharedEmpty.Annotations[0].Confidence!.Kind);
        Assert.Same(frame, Assert.Single(result.Document.SourcePages));
        Assert.Same(layout, Assert.Single(empty.Annotations));
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
        Assert.False(artifact.IsDocumentOwned);
        Assert.False(artifact.IsDisposed);
        Assert.Equal(42, stream.ReadByte());
        Assert.Same(artifact, Assert.Single(image.Artifacts));
    }

    [Fact]
    public void Build_PreservesPolygonOriginIdentityThroughPageEnumerationAndFinalization()
    {
        DocumentSourceOutput output = new("operation");
        DocumentSourcePage frame = PdfFrame(output);
        DocumentRegion region = new(frame, FidelityPolygon());
        DocumentAnnotation layout = new("layout", regions: [region]);
        DocumentPageReference reference = new("page:7");
        DocumentExtractionPage seven = new(7,
        [
            new DocumentExtractionText("first", 1, DocumentTextKind.Paragraph, "same", "same", annotations: [layout]),
            new DocumentExtractionText("second", 1, DocumentTextKind.Paragraph, "same", "same", annotations: [layout])
        ], pageReferences: [reference, reference], sourcePage: frame);
        DocumentExtractionPage two = new(2, [], sourcePage: new DocumentSourcePage("second-frame", 2, sourceOutput: output));
        using DocumentExtractionResult result = new[] { seven, two }.ToDocumentExtractionResult("order");
        DocumentSection section = result.Document.Sections[1];
        DocumentElement first = section.Elements[0];

        Assert.Equal(new[] { 2, 7 }, result.Pages.Select(static page => page.PageNumber));
        Assert.Equal(new[] { "same", "same" }, section.Elements.Select(static element => element.ToMarkdown()));
        Assert.NotSame(first, section.Elements[1]);
        Assert.Equal("s1/e0", first.Address);
        Assert.Equal(new[] { reference, reference }, first.PageReferences);
        Assert.Same(region, first.Annotations[0].Regions[0]);
        Assert.Equal(FidelityPolygon(), region.Polygon);
        Assert.Same(frame, region.Page);
        result.Document.Sections.RemoveAt(1);
        result.Document.Sections.Insert(0, section);
        Assert.Equal("s0/e0", first.Address);
        Assert.Equal(7, first.Annotations[0].Regions[0].Page.PageNumber);
        Assert.Equal("urn:frame:pdf-A", first.Annotations[0].Regions[0].Page.Identifier);
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
        DocumentAnnotation native = new("pdf", regions: [new DocumentRegion(pdf, FidelityPolygon())]);
        DocumentAnnotation rendered = new("raster", regions: [new DocumentRegion(raster, overlay)]);
        using DocumentExtractionResult result = new[]
        {
            new DocumentExtractionPage(7, [new DocumentExtractionText("overlay", 0, DocumentTextKind.Paragraph, "body", annotations: [native, rendered])])
        }.ToDocumentExtractionResult("overlay");
        DocumentElement element = Assert.Single(result.Document.EnumerateContent());
        DocumentRegion original = element.Annotations[0].Regions[0];
        DocumentRegion pixels = element.Annotations[1].Regions[0];

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
        Assert.Equal(new[] { "s0/e0/c0,0", "s0/e0/c2,0", "s0/e0/c2,2" }, shared.Cells.Where(static cell => cell is not null).Select(static cell => cell!.Address));
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
        Assert.True(first.IsDisposed);
        Assert.True(nested.IsDisposed);
        Assert.True(later.IsDisposed);
        Assert.False(first.IsDocumentOwned);
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
            alternativeText: "diagram only", artifacts: [artifact], annotations: [new DocumentAnnotation("image", regions: [new DocumentRegion(imageFrame, FidelityPolygon())])]);
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
    public void Build_PreservesStructuredPayloadAndRolesInSharedDocumentAlone()
    {
        var payload = new { kind = "vendor-extra", nested = new { unknown = new object[] { 3, "x" } } };
        var attributes = new { vendorFlag = "kept", provenance = "urn:synthetic:engine:v1", target = "urn:target:extra-17" };
        DocumentSourceOutput output = new("payload-operation", providerName: "synthetic");
        DocumentSourcePage frame = PdfFrame(output);
        DocumentAnnotation pageAnnotation = new("page-layout", attributes, "page", [new DocumentRegion(frame, FidelityPolygon())], sourceOutput: output);
        DocumentAnnotation observationAnnotation = new("vendor-observation", attributes, "caption", [new DocumentRegion(frame, FidelityPolygon())], sourceOutput: output);
        DocumentEvidenceReference evidence = new("urn:evidence:42", "vendor-evidence");
        using Document document = BuildSharedDocument(new DocumentExtractionPage(7,
        [
            new DocumentExtractionExtension("extension", 0, "vendor-extra", "visible **text**", "visible text",
                evidence: [new DocumentExtractionEvidence(evidence, "support")], annotations: [observationAnnotation], value: payload)
        ], annotations: [pageAnnotation]));
        DocumentExtensionElement extension = Assert.IsType<DocumentExtensionElement>(Assert.Single(document.EnumerateContent()));

        Assert.Same(payload, extension.Value);
        Assert.Equal(new object[] { 3, "x" }, payload.nested.unknown);
        Assert.Equal("vendor-extra", extension.Type);
        Assert.Same(pageAnnotation, Assert.Single(document.Sections[0].Annotations));
        Assert.Equal("page", document.Sections[0].Annotations[0].Role);
        Assert.Equal(new[] { "support", "caption" }, extension.Annotations.Select(static annotation => annotation.Role));
        Assert.Equal("vendor-evidence", extension.Annotations[0].Kind);
        Assert.Equal("urn:evidence:42", extension.Annotations[0].Value);
        Assert.Same(evidence, Assert.Single(extension.Evidence));
        Assert.Same(attributes, extension.Annotations[1].Value);
        Assert.Equal("kept", attributes.vendorFlag);
        Assert.Equal("urn:synthetic:engine:v1", attributes.provenance);
        Assert.Equal("urn:target:extra-17", attributes.target);
        Assert.Same(extension, Assert.Single(document.Sections[0].Elements));
        Assert.Same(observationAnnotation, extension.Annotations[1]);
        Assert.Equal(FidelityPolygon(), extension.Annotations[1].Regions[0].Polygon);
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
        using Document document = BuildSharedDocument(
            new DocumentExtractionPage(7, [new DocumentExtractionText("seven", 0, DocumentTextKind.Paragraph, "seven", "seven")], sourcePage: seven, sourceOutputs: [output]),
            new DocumentExtractionPage(2, [new DocumentExtractionText("two", 0, DocumentTextKind.Paragraph, "two", "two")], sourcePage: two, sourceOutputs: [output]));

        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal(new[] { two, seven }, document.SourcePages);
        Assert.Same(output, document.SourcePages[0].SourceOutput);
        Assert.Same(output, document.SourcePages[1].SourceOutput);
        Assert.Equal(DocumentRawContentStatus.Retained, output.NativeStatus);
        Assert.Equal(DocumentRawContentStatus.Retained, output.ContentStatus);
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        Assert.Equal(bytes, stream.ToArray());
        Assert.Equal("two" + Environment.NewLine + "seven", document.ToMarkdown());
        Assert.Equal(new[] { "two", "seven" }, document.EnumerateContent().OfType<DocumentTextElement>().Select(static text => text.Text));
        Assert.Equal("", document.GetMarkdown());
        Assert.True(artifact.IsDocumentOwned);
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

    private static Document BuildSharedDocument(params DocumentExtractionPage[] pages)
    {
        using DocumentExtractionDocumentBuilder builder = new("shared-only");
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

        public void Dispose() => DisposeCount++;

        public override string ToString() => "NATIVE_ONLY_92";
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
        long ownerVersion = owner.Version;
        Assert.True(foreignArtifact.IsDocumentOwned);
        Assert.False(currentArtifact.IsDocumentOwned);
        Assert.False(pendingArtifact.IsDocumentOwned);

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
        using DocumentExtractionDocumentBuilder builder = new("doomed-finalization");

        // Arrival order differs from finalization order; page 1 transfers before page 2 rejects.
        builder.AddPage(later);
        builder.AddPage(rejected);
        builder.AddPage(first);

        Assert.Throws<InvalidOperationException>(() => builder.Build());

        // The catch must leave current-owned artifacts for document finally, and clean pending unowned artifacts.
        Assert.True(currentArtifact.IsDocumentOwned);
        Assert.True(currentArtifact.IsDisposed);
        Assert.Equal(1, currentStream.DisposeCount);
        Assert.False(currentStream.CanRead);
        Assert.False(pendingArtifact.IsDocumentOwned);
        Assert.True(pendingArtifact.IsDisposed);
        Assert.Equal(1, pendingStream.DisposeCount);
        Assert.False(pendingStream.CanRead);
        Assert.True(foreignArtifact.IsDocumentOwned);
        Assert.False(foreignArtifact.IsDisposed);
        Assert.Equal(0, foreignStream.DisposeCount);
        Assert.True(foreignStream.CanRead);
        Assert.Equal(foreignBytes, foreignStream.ToArray());
        Assert.Equal(123, foreignStream.ReadByte());
        Assert.Equal(0, native.DisposeCount);
        Assert.Same(native, foreignOutput.GetRawRepresentation());
        Assert.Same(foreignArtifact, foreignOutput.GetRawContent());
        Assert.False(owner.IsDisposed);
        Assert.Equal(ownerVersion, owner.Version);
        Assert.Same(foreignOutput, Assert.Single(owner.SourceOutputs));
        Assert.Same(ownerFrame, Assert.Single(owner.SourcePages));
        Assert.Same(foreignArtifact, Assert.Single(owner.Artifacts));
        Assert.Same(ownerSection, Assert.Single(owner.Sections));
        Assert.Same(ownerText, Assert.Single(ownerSection.Elements));
        Assert.Equal("s0/e0", ownerText.Address);
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
        Assert.True(owner.IsDisposed);
        Assert.True(foreignArtifact.IsDisposed);
        Assert.Equal(1, foreignStream.DisposeCount);
        Assert.False(foreignStream.CanRead);
        Assert.Equal(0, native.DisposeCount);
    }
}
