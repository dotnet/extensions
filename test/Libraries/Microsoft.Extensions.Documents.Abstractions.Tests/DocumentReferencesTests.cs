// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentReferencesTests
{
    [Fact]
    public void ReducedApi_ExposesOnlyTypedSourcesAndPublicOwnershipQuery()
    {
        var assembly = typeof(Document).Assembly;
        foreach (string name in new[] { "DocumentAnnotation", "DocumentRegion", "DocumentConfidence", "DocumentPageReference", "DocumentEvidenceReference" })
        {
            Assert.Null(assembly.GetType("Microsoft.Extensions.Documents." + name));
        }

        foreach (Type type in new[] { typeof(Document), typeof(DocumentElement), typeof(DocumentTableCell) })
        {
            foreach (string name in new[] { "Annotations", "Evidence", "PageReferences", "HasMetadata", "Address", "LocalId", "Version", "IsDisposed" })
            {
                Assert.Null(type.GetProperty(name));
            }
        }

        Assert.Null(typeof(Document).GetProperty("Sources"));
        Assert.Null(typeof(Document).GetProperty("Artifacts"));
        Assert.Null(typeof(DocumentArtifact).GetProperty("IsDisposed"));
        Assert.Equal(typeof(bool), typeof(DocumentArtifact).GetProperty("IsDocumentOwned")!.PropertyType);
        Assert.Null(typeof(DocumentSourceOutput).GetProperty("NativeStatus"));
        foreach (Type type in new[] { typeof(DocumentElement), typeof(DocumentTableCell) })
        {
            var property = type.GetProperty("Sources")!;
            Assert.Equal(typeof(System.Collections.Generic.IList<DocumentSourceReference>), property.PropertyType);
            Assert.False(property.CanWrite);
        }

        Assert.False(typeof(DocumentSourceReference).GetProperty("Page")!.CanWrite);
        Assert.False(typeof(DocumentSourceReference).GetProperty("Polygon")!.CanWrite);
        Assert.False(typeof(Document).GetProperty("SourcePages")!.CanWrite);
        Assert.False(typeof(Document).GetProperty("SourceOutputs")!.CanWrite);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceReference_NullOrEmptyPolygonIsPageOnly(bool empty)
    {
        DocumentSourcePage page = new("unknown", 3);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceReference reference = new(page, empty ? Array.Empty<DocumentPoint>() : null, native);
        DocumentSourceReference omitted = new(page);
        Assert.Same(page, reference.Page);
        Assert.Empty(reference.Polygon);
        Assert.Empty(omitted.Polygon);
        Assert.Null(omitted.GetRawRepresentation());
        Assert.Same(native, reference.GetRawRepresentation());
        Assert.Null(page.Dimensions);
        Assert.Null(page.CoordinateUnit);
        Assert.Null(page.CoordinateOrigin);
        Assert.Equal(0, native.DisposeCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void SourceReference_RejectsShortPolygons(int count)
    {
        Assert.Throws<ArgumentException>("polygon", () => new DocumentSourceReference(new("page", 1), new DocumentPoint[count]));
    }

    [Fact]
    public void SourceReference_MinimumTriangleSnapshotsVerticesAndBorrowsNative()
    {
        DocumentPoint[] points = [new(1, 2), new(3, 4), new(5, 6)];
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourcePage page = new("triangle", 1);
        DocumentSourceReference reference = new(page, points, native);
        points[2] = new(9, 10);
        Assert.Equal(new[] { new DocumentPoint(1, 2), new DocumentPoint(3, 4), new DocumentPoint(5, 6) }, reference.Polygon);
        Assert.Same(page, reference.Page);
        Assert.Same(native, reference.GetRawRepresentation());
        Assert.Equal(0, native.DisposeCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SourceIdentifiers_NullOrEmptyValue_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentSourcePage(value!, 1));
        Assert.ThrowsAny<ArgumentException>(() => new DocumentSourceOutput(value!));
    }

    [Fact]
    public void DocumentArtifact_PreservesMediaTypeContentAndSource()
    {
        using MemoryStream content = new(new byte[] { 1, 2, 3 });
        Uri source = new("urn:document-artifact:opaque");

        using DocumentArtifact artifact = new("application/octet-stream", content, source);

        Assert.Equal("application/octet-stream", artifact.MediaType);
        Assert.Same(content, artifact.Content);
        Assert.Same(source, artifact.Source);
        Assert.False(artifact.IsDisposed);
        Assert.Equal(3, artifact.Content.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentArtifact_NullOrEmptyMediaType_Throws(string? mediaType)
    {
        using MemoryStream content = new();

        Assert.ThrowsAny<ArgumentException>(() => new DocumentArtifact(mediaType!, content));
    }

    [Fact]
    public void DocumentArtifact_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            "content",
            () => new DocumentArtifact("application/octet-stream", null!));
    }

    [Fact]
    public void DocumentArtifact_Dispose_ClosesOwnedStreamAndIsIdempotent()
    {
        using MemoryStream content = new(new byte[] { 1, 2, 3 });
        using DocumentArtifact artifact = new("application/octet-stream", content);

        artifact.Dispose();

        Assert.True(artifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => content.ReadByte());

        artifact.Dispose();

        Assert.True(artifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => content.ReadByte());
    }

    [Fact]
    public void Geometry_PreservesOrderedFractionalVerticesAndFrameIdentity()
    {
        using Document document = new("geometry");
        DocumentPoint[] supplied = DocumentAnnotationTestData.Polygon();
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage();
        DocumentSourceReference region = new(page, supplied);
        supplied[0] = new DocumentPoint(999, 999);
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible");
        text.Sources.Add(region);
        DocumentSection section = new("");
        section.Elements.Add(text);
        document.Sections.Add(section);

        Assert.Equal(DocumentAnnotationTestData.Polygon(), region.Polygon);
        Assert.Same(page, region.Page);
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Equal("urn:frame:pdf-A", page.Identifier);
        Assert.Equal(7, page.PageNumber);
        Assert.Equal(new DocumentPageDimensions(200, 100), page.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, page.CoordinateOrigin);
        Assert.Equal("visible", document.ToMarkdown());
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<DocumentPoint>)region.Polygon)[0] = default);
    }

    [Fact]
    public void Geometry_KnownPdfAndRasterFixturesProduceDistinctExpectedOverlayVertices()
    {
        DocumentSourceReference pdf = new(DocumentAnnotationTestData.PdfPage(), DocumentAnnotationTestData.Polygon());
        DocumentPoint[] expected =
        [
            new(20.5, 159), new(81.5, 163.5), new(89, 79.75),
            new(25.75, 82.5), new(17, 129.5)
        ];
        DocumentSourcePage rasterPage = new("urn:frame:raster-A", 7, new DocumentPageDimensions(400, 200),
            DocumentCoordinateUnit.Pixel, DocumentCoordinateOrigin.TopLeft);
        DocumentSourceReference raster = new(rasterPage, expected);

        Assert.Equal(expected, ProjectKnownOverlay(pdf));
        Assert.Equal(expected, ProjectKnownOverlay(raster));
        Assert.Equal(DocumentAnnotationTestData.Polygon(), pdf.Polygon);
        Assert.Equal(new DocumentPageDimensions(400, 200), raster.Page.Dimensions);
        Assert.NotEqual(pdf.Polygon[0], raster.Polygon[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Geometry_UnknownUnitOrOriginDoesNotGuessConversion(bool unknownUnit)
    {
        DocumentSourcePage page = new("partial", 7, new DocumentPageDimensions(200, 100),
            unknownUnit ? null : DocumentCoordinateUnit.Point,
            unknownUnit ? DocumentCoordinateOrigin.BottomLeft : null);
        DocumentSourceReference region = new(page, DocumentAnnotationTestData.Polygon());

        Assert.Null(ProjectKnownOverlay(region));
        Assert.Equal(DocumentAnnotationTestData.Polygon(), region.Polygon);
        Assert.Equal(new DocumentPageDimensions(200, 100), page.Dimensions);
        Assert.Equal(unknownUnit ? null : DocumentCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(unknownUnit ? DocumentCoordinateOrigin.BottomLeft : null, page.CoordinateOrigin);
    }

    [Fact]
    public void Geometry_UnknownDimensionsDoNotInferSizeFromKnownFrameOrPolygon()
    {
        DocumentSourcePage page = new("no-size", 7, coordinateUnit: DocumentCoordinateUnit.Point,
            coordinateOrigin: DocumentCoordinateOrigin.BottomLeft);
        DocumentSourceReference reference = new(page, DocumentAnnotationTestData.Polygon());
        Assert.Null(page.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, page.CoordinateOrigin);
        Assert.Equal(DocumentAnnotationTestData.Polygon(), reference.Polygon);
        Assert.Null(ProjectKnownOverlay(reference));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Geometry_RejectsNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>("x", () => new DocumentPoint(value, 2));
        Assert.Throws<ArgumentOutOfRangeException>("y", () => new DocumentPoint(2, value));
        Assert.Throws<ArgumentOutOfRangeException>("width", () => new DocumentPageDimensions(value, 100));
        Assert.Throws<ArgumentOutOfRangeException>("height", () => new DocumentPageDimensions(200, value));
        DocumentSourceReference valid = new(DocumentAnnotationTestData.PdfPage(), DocumentAnnotationTestData.Polygon());
        Assert.Equal(5, valid.Polygon.Count);
        Assert.Equal(60.125, valid.Polygon[2].Y);
    }

    [Fact]
    public void SourceCarriers_RejectInvalidFramesAndReferences()
    {
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage();
        Assert.Throws<ArgumentOutOfRangeException>("width", () => new DocumentPageDimensions(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>("height", () => new DocumentPageDimensions(200, -1));
        Assert.Throws<ArgumentOutOfRangeException>("pageNumber", () => new DocumentSourcePage("frame", 0));
        Assert.Throws<ArgumentOutOfRangeException>("dimensions", () => new DocumentSourcePage("frame", 1, default(DocumentPageDimensions)));
        Assert.Throws<ArgumentOutOfRangeException>("coordinateUnit", () => new DocumentSourcePage("frame", 1, coordinateUnit: (DocumentCoordinateUnit)99));
        Assert.Throws<ArgumentOutOfRangeException>("coordinateOrigin", () => new DocumentSourcePage("frame", 1, coordinateOrigin: (DocumentCoordinateOrigin)99));
        Assert.Throws<ArgumentNullException>("page", () => new DocumentSourceReference(null!));
        Assert.Throws<ArgumentException>("polygon", () => new DocumentSourceReference(page, new[] { new DocumentPoint(1, 2), new DocumentPoint(3, 4) }));
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible");
        DocumentTableCell cell = new();
        Assert.Throws<ArgumentNullException>(() => text.Sources.Add(null!));
        Assert.Throws<ArgumentNullException>(() => cell.Sources.Add(null!));
        Assert.Empty(text.Sources);
        Assert.Empty(cell.Sources);
    }

    [Fact]
    public void RawOutput_PreservesOriginalBytesAndBorrowedNativeIdentity()
    {
        using Document document = new("raw");
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentAnnotationTestData.CountingStream stream = new(DocumentAnnotationTestData.OriginalBytes());
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("operation-A", native, artifact, providerName: "synthetic", modelId: "model:v1");
        document.AddSourceOutput(output);

        Assert.Equal(DocumentRawContentStatus.Retained, output.ContentStatus);
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Same(artifact, output.GetRawContent());
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), stream.ToArray());
        Assert.Equal("synthetic", output.ProviderName);
        Assert.Equal("model:v1", output.ModelId);
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        document.Dispose();
        document.Dispose();
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(1, stream.DisposeCount);
        Assert.True(artifact.IsDisposed);
    }

    [Fact]
    public void RawOutput_DetachedContentSurvivesTransportDisposalAndOwnedStreamDisposesOnce()
    {
        using Document document = new("detached");
        DocumentAnnotationTestData.CountingStream transport = new(DocumentAnnotationTestData.OriginalBytes());
        DocumentAnnotationTestData.CountingStream detached = new(transport.ToArray());
        using DocumentAnnotationTestData.BorrowedNative native = new();
        using DocumentArtifact artifact = new("application/json", detached);
        DocumentSourceOutput output = new("operation", native, artifact);
        transport.Dispose();
        document.AddSourcePage(DocumentAnnotationTestData.PdfPage(output));

        using MemoryStream observed = new();
        output.GetRawContent()!.Content.CopyTo(observed);
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), observed.ToArray());
        Assert.Equal(1, transport.DisposeCount);
        Assert.Equal(0, detached.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, detached.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => output.GetRawContent()!.Content.ReadByte());
    }

    [Fact]
    public void RawOutput_ContentCaptureDisabledUnavailableAndPartialRetentionRemainDistinct()
    {
        DocumentSourceOutput unavailable = new("unavailable");
        DocumentSourceOutput disabled = new("disabled", contentCaptureDisabled: true);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput nativeOnly = new("native-only", native);
        using DocumentArtifact content = new("application/json", new MemoryStream(DocumentAnnotationTestData.OriginalBytes()));
        DocumentSourceOutput contentOnly = new("content-only", rawContent: content);

        Assert.Equal(DocumentRawContentStatus.Unavailable, unavailable.ContentStatus);
        Assert.Null(unavailable.GetRawRepresentation());
        Assert.Null(unavailable.GetRawContent());
        Assert.Equal(DocumentRawContentStatus.Disabled, disabled.ContentStatus);
        Assert.Null(disabled.GetRawRepresentation());
        Assert.Null(disabled.GetRawContent());
        Assert.Same(native, nativeOnly.GetRawRepresentation());
        Assert.Equal(DocumentRawContentStatus.Unavailable, nativeOnly.ContentStatus);
        Assert.Null(contentOnly.GetRawRepresentation());
        Assert.Same(content, contentOnly.GetRawContent());
        Assert.Equal(DocumentRawContentStatus.Retained, contentOnly.ContentStatus);
        Assert.Throws<ArgumentException>("contentCaptureDisabled", () => new DocumentSourceOutput("bad", rawContent: content, contentCaptureDisabled: true));
        content.Dispose();
        Assert.Throws<ArgumentException>("rawContent", () => new DocumentSourceOutput("bad", rawContent: content));
        Assert.Equal(0, native.DisposeCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RawOutput_NativeAvailability_IsIndependentOfContentCapture(bool contentCaptureDisabled, bool hasNative)
    {
        using DocumentAnnotationTestData.BorrowedNative native = new();
        object? representation = hasNative ? native : null;
        DocumentSourceOutput output = new("operation", representation, contentCaptureDisabled: contentCaptureDisabled);

        Assert.Same(representation, output.GetRawRepresentation());
        Assert.Equal(contentCaptureDisabled ? DocumentRawContentStatus.Disabled : DocumentRawContentStatus.Unavailable, output.ContentStatus);
        Assert.Null(output.GetRawContent());
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void RawOutput_ContentCaptureDisabled_PreservesNativeAfterDocumentDisposal()
    {
        using Document document = new("native-only");
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput output = new("operation", native, contentCaptureDisabled: true);
        document.AddSourcePage(DocumentAnnotationTestData.PdfPage(output));

        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Equal(DocumentRawContentStatus.Disabled, output.ContentStatus);
        document.Dispose();
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void RawOutput_ContentCaptureDisabled_RejectsContentWithoutTakingOwnership()
    {
        DocumentAnnotationTestData.CountingStream stream = new(DocumentAnnotationTestData.OriginalBytes());
        using DocumentArtifact content = new("application/json", stream);
        using DocumentAnnotationTestData.BorrowedNative native = new();

        Assert.Throws<ArgumentException>("contentCaptureDisabled",
            () => new DocumentSourceOutput("operation", native, content, contentCaptureDisabled: true));
        Assert.False(content.IsDocumentOwned);
        Assert.Equal(0, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes()[0], content.Content.ReadByte());
    }

    // Consumer-owned oracle for these two known fixtures only; not a production conversion API.
    private static DocumentPoint[]? ProjectKnownOverlay(DocumentSourceReference region)
    {
        if (region.Page.Dimensions == new DocumentPageDimensions(200, 100) &&
            region.Page.CoordinateUnit == DocumentCoordinateUnit.Point &&
            region.Page.CoordinateOrigin == DocumentCoordinateOrigin.BottomLeft)
        {
            DocumentPoint[] result = new DocumentPoint[region.Polygon.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new DocumentPoint(2 * region.Polygon[i].X, 2 * (100 - region.Polygon[i].Y));
            }

            return result;
        }

        return region.Page.Dimensions == new DocumentPageDimensions(400, 200) &&
            region.Page.CoordinateUnit == DocumentCoordinateUnit.Pixel &&
            region.Page.CoordinateOrigin == DocumentCoordinateOrigin.TopLeft
                ? System.Linq.Enumerable.ToArray(region.Polygon)
                : null;
    }
}
