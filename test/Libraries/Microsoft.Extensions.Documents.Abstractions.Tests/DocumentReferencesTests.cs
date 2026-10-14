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
    public void DocumentPageReference_PreservesOpaqueValue()
    {
        const string Value = "provider-page/opaque-007";

        DocumentPageReference reference = new(Value);

        Assert.Equal(Value, reference.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentPageReference_NullOrEmptyValue_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentPageReference(value!));
    }

    [Fact]
    public void DocumentEvidenceReference_PreservesOpaqueValueAndKind()
    {
        const string Value = "provider-evidence:opaque#7";
        const string Kind = "citation-kind";

        DocumentEvidenceReference reference = new(Value, Kind);

        Assert.Equal(Value, reference.Value);
        Assert.Equal(Kind, reference.Kind);
    }

    [Fact]
    public void DocumentEvidenceReference_WithoutKind_HasNullKind()
    {
        DocumentEvidenceReference reference = new("provider-evidence:opaque#8");

        Assert.Null(reference.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentEvidenceReference_NullOrEmptyValue_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentEvidenceReference(value!));
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
        DocumentRegion region = new(page, supplied);
        supplied[0] = new DocumentPoint(999, 999);
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible");
        text.Annotations.Add(new DocumentAnnotation("layout", regions: new[] { region }));
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
        DocumentRegion pdf = new(DocumentAnnotationTestData.PdfPage(), DocumentAnnotationTestData.Polygon());
        DocumentPoint[] expected =
        [
            new(20.5, 159), new(81.5, 163.5), new(89, 79.75),
            new(25.75, 82.5), new(17, 129.5)
        ];
        DocumentSourcePage rasterPage = new("urn:frame:raster-A", 7, new DocumentPageDimensions(400, 200),
            DocumentCoordinateUnit.Pixel, DocumentCoordinateOrigin.TopLeft);
        DocumentRegion raster = new(rasterPage, expected);

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
        DocumentRegion region = new(page, DocumentAnnotationTestData.Polygon());

        Assert.Null(ProjectKnownOverlay(region));
        Assert.Equal(DocumentAnnotationTestData.Polygon(), region.Polygon);
        Assert.Equal(new DocumentPageDimensions(200, 100), page.Dimensions);
        Assert.Equal(unknownUnit ? null : DocumentCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(unknownUnit ? DocumentCoordinateOrigin.BottomLeft : null, page.CoordinateOrigin);
    }

    [Fact]
    public void Confidence_PreservesDeclaredScaleWithoutNormalization()
    {
        using Document document = new("confidence");
        DocumentSection section = new("");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible **text**", "visible text");
        section.Elements.Add(text);
        document.Sections.Add(section);
        long version = document.Version;
        text.Annotations.Add(new DocumentAnnotation("symbol",
            confidence: new DocumentConfidence(73, "provider-reported symbol certainty", 0, 100)));
        text.Annotations.Add(new DocumentAnnotation("unscaled",
            confidence: new DocumentConfidence(-12.5, "vendor margin")));

        DocumentConfidence reported = text.Annotations[0].Confidence!;
        Assert.Equal(73, reported.Value);
        Assert.Equal("provider-reported symbol certainty", reported.Kind);
        Assert.Equal(0, reported.Minimum);
        Assert.Equal(100, reported.Maximum);
        Assert.Equal(-12.5, text.Annotations[1].Confidence!.Value);
        Assert.Null(text.Annotations[1].Confidence!.Minimum);
        Assert.Null(text.Annotations[1].Confidence!.Maximum);
        Assert.Equal("visible **text**", document.ToMarkdown());
        Assert.Equal("visible text", text.Text);
        Assert.Equal(version, document.Version);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GeometryAndConfidence_RejectNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>("x", () => new DocumentPoint(value, 2));
        Assert.Throws<ArgumentOutOfRangeException>("y", () => new DocumentPoint(2, value));
        Assert.Throws<ArgumentOutOfRangeException>("width", () => new DocumentPageDimensions(value, 100));
        Assert.Throws<ArgumentOutOfRangeException>("height", () => new DocumentPageDimensions(200, value));
        Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => new DocumentConfidence(value, "recognition"));
        Assert.Throws<ArgumentOutOfRangeException>("minimum", () => new DocumentConfidence(5, "recognition", value));
        Assert.Throws<ArgumentOutOfRangeException>("maximum", () => new DocumentConfidence(5, "recognition", maximum: value));
        DocumentRegion valid = new(DocumentAnnotationTestData.PdfPage(), DocumentAnnotationTestData.Polygon());
        Assert.Equal(5, valid.Polygon.Count);
        Assert.Equal(60.125, valid.Polygon[2].Y);
    }

    [Fact]
    public void AnnotationCarriers_RejectInvalidFramesRegionsAndDeclaredScales()
    {
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage();
        Assert.Throws<ArgumentOutOfRangeException>("width", () => new DocumentPageDimensions(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>("height", () => new DocumentPageDimensions(200, -1));
        Assert.Throws<ArgumentOutOfRangeException>("pageNumber", () => new DocumentSourcePage("frame", 0));
        Assert.Throws<ArgumentOutOfRangeException>("dimensions", () => new DocumentSourcePage("frame", 1, default(DocumentPageDimensions)));
        Assert.Throws<ArgumentOutOfRangeException>("coordinateUnit", () => new DocumentSourcePage("frame", 1, coordinateUnit: (DocumentCoordinateUnit)99));
        Assert.Throws<ArgumentOutOfRangeException>("coordinateOrigin", () => new DocumentSourcePage("frame", 1, coordinateOrigin: (DocumentCoordinateOrigin)99));
        Assert.Throws<ArgumentNullException>("page", () => new DocumentRegion(null!, DocumentAnnotationTestData.Polygon()));
        Assert.Throws<ArgumentNullException>("polygon", () => new DocumentRegion(page, null!));
        Assert.Throws<ArgumentException>("polygon", () => new DocumentRegion(page, new[] { new DocumentPoint(1, 2), new DocumentPoint(3, 4) }));
        Assert.Throws<ArgumentException>("regions", () => new DocumentAnnotation("layout", regions: new DocumentRegion[] { null! }));
        Assert.ThrowsAny<ArgumentException>(() => new DocumentAnnotation(""));
        Assert.ThrowsAny<ArgumentException>(() => new DocumentConfidence(5, ""));
        Assert.Throws<ArgumentOutOfRangeException>("value", () => new DocumentConfidence(101, "recognition", 0, 100));
        Assert.Throws<ArgumentOutOfRangeException>("value", () => new DocumentConfidence(-1, "recognition", 0, 100));
        Assert.Throws<ArgumentOutOfRangeException>("value", () => new DocumentConfidence(5, "recognition", 5, 5));
        Assert.Throws<ArgumentOutOfRangeException>("value", () => new DocumentConfidence(5, "recognition", 10, 0));
        Assert.Equal(0, new DocumentConfidence(0, "recognition", 0, 100).Value);
        Assert.Equal(100, new DocumentConfidence(100, "recognition", 0, 100).Value);
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

        Assert.Equal(DocumentRawContentStatus.Retained, output.NativeStatus);
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
    public void RawOutput_DisabledUnavailableAndPartialRetentionRemainDistinct()
    {
        DocumentSourceOutput unavailable = new("unavailable");
        DocumentSourceOutput disabled = new("disabled", captureDisabled: true);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput nativeOnly = new("native-only", native);
        using DocumentArtifact content = new("application/json", new MemoryStream(DocumentAnnotationTestData.OriginalBytes()));
        DocumentSourceOutput contentOnly = new("content-only", rawContent: content);

        Assert.Equal(DocumentRawContentStatus.Unavailable, unavailable.NativeStatus);
        Assert.Equal(DocumentRawContentStatus.Unavailable, unavailable.ContentStatus);
        Assert.Null(unavailable.GetRawRepresentation());
        Assert.Null(unavailable.GetRawContent());
        Assert.Equal(DocumentRawContentStatus.Disabled, disabled.NativeStatus);
        Assert.Equal(DocumentRawContentStatus.Disabled, disabled.ContentStatus);
        Assert.Null(disabled.GetRawRepresentation());
        Assert.Null(disabled.GetRawContent());
        Assert.Equal(DocumentRawContentStatus.Retained, nativeOnly.NativeStatus);
        Assert.Equal(DocumentRawContentStatus.Unavailable, nativeOnly.ContentStatus);
        Assert.Equal(DocumentRawContentStatus.Unavailable, contentOnly.NativeStatus);
        Assert.Equal(DocumentRawContentStatus.Retained, contentOnly.ContentStatus);
        Assert.Throws<ArgumentException>("captureDisabled", () => new DocumentSourceOutput("bad", native, captureDisabled: true));
        Assert.Throws<ArgumentException>("captureDisabled", () => new DocumentSourceOutput("bad", rawContent: content, captureDisabled: true));
        content.Dispose();
        Assert.Throws<ArgumentException>("rawContent", () => new DocumentSourceOutput("bad", rawContent: content));
        Assert.Equal(0, native.DisposeCount);
    }

    // Consumer-owned oracle for these two known fixtures only; not a production conversion API.
    private static DocumentPoint[]? ProjectKnownOverlay(DocumentRegion region)
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
