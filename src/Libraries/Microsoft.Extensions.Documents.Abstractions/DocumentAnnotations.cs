// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>Identifies the unit of a source page's coordinates.</summary>
public enum DocumentCoordinateUnit
{
    /// <summary>Pixels in the source raster.</summary>
    Pixel,

    /// <summary>Points, each equal to 1/72 inch.</summary>
    Point,

    /// <summary>Inches.</summary>
    Inch,

    /// <summary>Coordinates relative to the page, in the range zero to one.</summary>
    Normalized
}

/// <summary>Identifies the origin and vertical axis of source coordinates.</summary>
public enum DocumentCoordinateOrigin
{
    /// <summary>Top-left origin, with the vertical axis increasing downwards.</summary>
    TopLeft,

    /// <summary>Bottom-left origin, with the vertical axis increasing upwards.</summary>
    BottomLeft
}

/// <summary>Represents a vertex in its source page's coordinate frame.</summary>
public readonly record struct DocumentPoint
{
    /// <summary>Initializes a new instance of the <see cref="DocumentPoint"/> struct.</summary>
    public DocumentPoint(double x, double y)
    {
        DocumentAnnotationValidation.Finite(x, nameof(x));
        DocumentAnnotationValidation.Finite(y, nameof(y));
        X = x;
        Y = y;
    }

    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; }

    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; }
}

/// <summary>Represents positive source page dimensions in the page's coordinate unit.</summary>
public readonly record struct DocumentPageDimensions
{
    /// <summary>Initializes a new instance of the <see cref="DocumentPageDimensions"/> struct.</summary>
    public DocumentPageDimensions(double width, double height)
    {
        DocumentAnnotationValidation.Finite(width, nameof(width));
        DocumentAnnotationValidation.Finite(height, nameof(height));
        Width = Throw.IfLessThanOrEqual(width, 0);
        Height = Throw.IfLessThanOrEqual(height, 0);
    }

    /// <summary>Gets the width.</summary>
    public double Width { get; }

    /// <summary>Gets the height.</summary>
    public double Height { get; }
}

/// <summary>Describes a producer-supplied source page, independently of document assembly order.</summary>
public sealed class DocumentSourcePage
{
    /// <summary>Initializes a new instance of the <see cref="DocumentSourcePage"/> class.</summary>
    public DocumentSourcePage(
        string identifier,
        int pageNumber,
        DocumentPageDimensions? dimensions = null,
        DocumentCoordinateUnit? coordinateUnit = null,
        DocumentCoordinateOrigin? coordinateOrigin = null,
        DocumentSourceOutput? sourceOutput = null)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
        PageNumber = Throw.IfLessThanOrEqual(pageNumber, 0);
        if (dimensions is { } size && (size.Width <= 0 || size.Height <= 0))
        {
            Throw.ArgumentOutOfRangeException(nameof(dimensions));
        }

        if (coordinateUnit is { } unit && !Enum.IsDefined(typeof(DocumentCoordinateUnit), unit))
        {
            Throw.ArgumentOutOfRangeException(nameof(coordinateUnit));
        }

        if (coordinateOrigin is { } origin && !Enum.IsDefined(typeof(DocumentCoordinateOrigin), origin))
        {
            Throw.ArgumentOutOfRangeException(nameof(coordinateOrigin));
        }

        Dimensions = dimensions;
        CoordinateUnit = coordinateUnit;
        CoordinateOrigin = coordinateOrigin;
        SourceOutput = sourceOutput;
    }

    /// <summary>Gets the producer's frame identifier, scoped to its source operation.</summary>
    public string Identifier { get; }

    /// <summary>Gets the one-based originating page number, not an assembly index.</summary>
    public int PageNumber { get; }

    /// <summary>Gets reported dimensions, or <see langword="null"/> when unknown.</summary>
    public DocumentPageDimensions? Dimensions { get; }

    /// <summary>Gets the reported unit, or <see langword="null"/> when unknown.</summary>
    public DocumentCoordinateUnit? CoordinateUnit { get; }

    /// <summary>Gets the reported origin, or <see langword="null"/> when unknown.</summary>
    public DocumentCoordinateOrigin? CoordinateOrigin { get; }

    /// <summary>Gets the source operation, when available.</summary>
    public DocumentSourceOutput? SourceOutput { get; }
}

/// <summary>Preserves an ordered polygon in its originating source coordinate frame.</summary>
public sealed class DocumentRegion
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRegion"/> class.</summary>
    public DocumentRegion(DocumentSourcePage page, IReadOnlyList<DocumentPoint> polygon)
    {
        Page = Throw.IfNull(page);
        Polygon = new ReadOnlyCollection<DocumentPoint>(Throw.IfNull(polygon).ToArray());
        if (Polygon.Count < 3)
        {
            Throw.ArgumentException(nameof(polygon), "A region must contain at least three vertices.");
        }
    }

    /// <summary>Gets the originating page and coordinate frame.</summary>
    public DocumentSourcePage Page { get; }

    /// <summary>Gets the vertices in the exact producer-supplied order.</summary>
    public IReadOnlyList<DocumentPoint> Polygon { get; }
}

/// <summary>Preserves a reported confidence measurement without implying calibration across providers.</summary>
public sealed class DocumentConfidence
{
    /// <summary>Initializes a new instance of the <see cref="DocumentConfidence"/> class.</summary>
    public DocumentConfidence(double value, string kind, double? minimum = null, double? maximum = null)
    {
        DocumentAnnotationValidation.Finite(value, nameof(value));
        if (minimum is { } min)
        {
            DocumentAnnotationValidation.Finite(min, nameof(minimum));
        }

        if (maximum is { } max)
        {
            DocumentAnnotationValidation.Finite(max, nameof(maximum));
        }

        if ((minimum is { } lower && value < lower) ||
            (maximum is { } upper && value > upper) ||
            (minimum is { } a && maximum is { } b && a >= b))
        {
            Throw.ArgumentOutOfRangeException(nameof(value), "The value must be within the declared confidence scale.");
        }

        Value = value;
        Kind = Throw.IfNullOrEmpty(kind);
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>Gets the reported value, without normalization or clipping.</summary>
    public double Value { get; }

    /// <summary>Gets the measurement kind, such as recognition or classification.</summary>
    public string Kind { get; }

    /// <summary>Gets the declared minimum, when known.</summary>
    public double? Minimum { get; }

    /// <summary>Gets the declared maximum, when known.</summary>
    public double? Maximum { get; }
}

/// <summary>Describes the availability of retained provider output.</summary>
public enum DocumentRawContentStatus
{
    /// <summary>The provider did not supply this representation.</summary>
    Unavailable,

    /// <summary>The original representation is retained.</summary>
    Retained,

    /// <summary>Original response-content capture was explicitly disabled.</summary>
    Disabled
}

/// <summary>Provides explicit break-glass access to one operation's output, outside semantic content.</summary>
/// <remarks>
/// Native objects are borrowed and are never implicitly disposed. Original content must be a detached
/// response body, not serialization of a mapped object. The document owns a registered content artifact.
/// Inspection is explicit; projections must not log, embed, or persist these representations automatically.
/// </remarks>
public sealed class DocumentSourceOutput
{
    private readonly object? _rawRepresentation;
    private readonly DocumentArtifact? _rawContent;

    /// <summary>Initializes a new instance of the <see cref="DocumentSourceOutput"/> class.</summary>
    public DocumentSourceOutput(
        string identifier,
        object? rawRepresentation = null,
        DocumentArtifact? rawContent = null,
        bool contentCaptureDisabled = false,
        string? providerName = null,
        string? modelId = null)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
        if (contentCaptureDisabled && rawContent is not null)
        {
            Throw.ArgumentException(nameof(contentCaptureDisabled), "Disabled content capture cannot contain original response content.");
        }

        if (rawContent is not null && (rawContent.IsDisposed || !rawContent.Content.CanRead))
        {
            Throw.ArgumentException(nameof(rawContent), "Original content must be readable.");
        }

        _rawRepresentation = rawRepresentation;
        _rawContent = rawContent;
        NativeStatus = rawRepresentation is null ? DocumentRawContentStatus.Unavailable : DocumentRawContentStatus.Retained;
        ContentStatus = contentCaptureDisabled ? DocumentRawContentStatus.Disabled :
            rawContent is null ? DocumentRawContentStatus.Unavailable : DocumentRawContentStatus.Retained;
        ProviderName = providerName;
        ModelId = modelId;
    }

    /// <summary>Gets the producer-local source operation identifier, not a durable document key.</summary>
    public string Identifier { get; }

    /// <summary>Gets the optional provider name.</summary>
    public string? ProviderName { get; }

    /// <summary>Gets the actual source model identifier, when reported.</summary>
    public string? ModelId { get; }

    /// <summary>Gets native representation availability.</summary>
    /// <remarks>Native availability is independent of original response-content capture and is never disabled by its policy.</remarks>
    public DocumentRawContentStatus NativeStatus { get; }

    /// <summary>Gets original response content availability.</summary>
    public DocumentRawContentStatus ContentStatus { get; }

    /// <summary>Gets the borrowed native result for explicit inspection, when supplied.</summary>
    /// <returns>The borrowed native result, or <see langword="null"/>.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Raw inspection is explicit and excluded from automatic property serialization.")]
    public object? GetRawRepresentation() => _rawRepresentation;

    /// <summary>Gets original response content, usable until the owning document is disposed.</summary>
    /// <returns>The document-owned original content, or <see langword="null"/>.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Raw inspection is explicit and excluded from automatic property serialization.")]
    public DocumentArtifact? GetRawContent() => _rawContent;
}

/// <summary>Associates structured producer information with a document, element, or cell.</summary>
/// <remarks>
/// Values are opaque borrowed data, not standardized detection schemas or semantic content.
/// Collections retain order. Geometry and confidence remain optional and do not assert authoritative grounding.
/// </remarks>
public sealed class DocumentAnnotation
{
    private readonly object? _rawRepresentation;

    /// <summary>Initializes a new instance of the <see cref="DocumentAnnotation"/> class.</summary>
    public DocumentAnnotation(
        string kind,
        object? value = null,
        string? role = null,
        IReadOnlyList<DocumentRegion>? regions = null,
        DocumentConfidence? confidence = null,
        DocumentSourceOutput? sourceOutput = null,
        object? rawRepresentation = null)
    {
        Kind = Throw.IfNullOrEmpty(kind);
        Value = value;
        Role = role;
        Regions = new ReadOnlyCollection<DocumentRegion>((regions ?? Array.Empty<DocumentRegion>()).ToArray());
        if (Regions.Any(static region => region is null))
        {
            Throw.ArgumentException(nameof(regions), "Regions cannot contain null entries.");
        }

        Confidence = confidence;
        SourceOutput = sourceOutput;
        _rawRepresentation = rawRepresentation;
    }

    /// <summary>Gets the producer's annotation kind.</summary>
    public string Kind { get; }

    /// <summary>Gets a structured value, without interpreting or flattening it.</summary>
    public object? Value { get; }

    /// <summary>Gets the annotation role, when supplied.</summary>
    public string? Role { get; }

    /// <summary>Gets the ordered originating regions.</summary>
    public IReadOnlyList<DocumentRegion> Regions { get; }

    /// <summary>Gets the reported confidence measurement.</summary>
    public DocumentConfidence? Confidence { get; }

    /// <summary>Gets the source operation, when available.</summary>
    public DocumentSourceOutput? SourceOutput { get; }

    /// <summary>Gets the borrowed native observation for explicit inspection, when supplied.</summary>
    /// <returns>The borrowed native observation, or <see langword="null"/>.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Raw inspection is explicit and excluded from automatic property serialization.")]
    public object? GetRawRepresentation() => _rawRepresentation;
}

internal static class DocumentAnnotationValidation
{
    internal static void Finite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            Throw.ArgumentOutOfRangeException(name, "A coordinate or measurement must be finite.");
        }
    }
}

internal sealed class DocumentAnnotationCollection : Collection<DocumentAnnotation>
{
    private readonly Func<Document?> _document;

    internal DocumentAnnotationCollection(Func<Document?> document) => _document = document;

    protected override void InsertItem(int index, DocumentAnnotation item)
    {
        _ = Throw.IfNull(item);
        _ = Throw.IfLessThan(index, 0);
        _ = Throw.IfGreaterThan(index, Count);
        Document? document = _document();
        document?.ThrowIfDisposed();
        document?.RegisterAnnotation(item);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, DocumentAnnotation item)
    {
        _ = Throw.IfNull(item);
        _ = this[index];
        Document? document = _document();
        document?.ThrowIfDisposed();
        document?.RegisterAnnotation(item);
        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        _document()?.ThrowIfDisposed();
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        _document()?.ThrowIfDisposed();
        base.ClearItems();
    }
}
