// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents one page of structured OCR output.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrPage
{
    /// <summary>Initializes a new instance of the <see cref="OcrPage"/> class.</summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="text">The structured text for this page.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public OcrPage(int pageNumber, string text)
    {
        PageNumber = pageNumber;
        Text = Throw.IfNull(text);
    }

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; }

    /// <summary>Gets the structured text for this page, with headings, tables, and reading order preserved.</summary>
    public string Text { get; }

    /// <summary>Gets or sets the elements extracted from this page, in reading order.</summary>
    /// <remarks>
    /// A single heterogeneous stream of blocks, tables, and images in the order a human would read them.
    /// Project a specific kind with <see cref="System.Linq.Enumerable.OfType{TResult}(System.Collections.IEnumerable)"/>,
    /// for example <c>Elements.OfType&lt;OcrTable&gt;()</c>. The full page text is available directly on
    /// <see cref="Text"/>, so reading-order consumers do not need geometry math.
    /// </remarks>
    public IReadOnlyList<OcrElement> Elements { get; set; } = [];

    /// <summary>Gets or sets the page dimensions (width and height), expressed in <see cref="CoordinateUnit"/>, when the engine provides them.</summary>
    /// <remarks>
    /// Together with <see cref="CoordinateUnit"/> and <see cref="CoordinateOrigin"/>, the dimensions let a consumer
    /// interpret or normalize the geometry (<see cref="OcrBoundingBox"/> / <see cref="OcrPoint"/>) on this page with
    /// engine-agnostic code. For example, dividing a coordinate by the corresponding dimension yields a page-relative
    /// [0, 1] value regardless of the native unit.
    /// </remarks>
    public OcrPageDimensions? Dimensions { get; set; }

    /// <summary>Gets or sets the unit in which this page's geometry coordinates are expressed, when known.</summary>
    /// <remarks>
    /// Reported per page: engines can emit different units for different pages of one document (for example, a batch
    /// mixing image and PDF inputs). Applies to every <see cref="OcrBoundingRegion"/> on the page and to
    /// <see cref="Dimensions"/>. When <see langword="null"/>, the geometry should be treated as an opaque,
    /// provider-specific coordinate space.
    /// </remarks>
    public OcrCoordinateUnit? CoordinateUnit { get; set; }

    /// <summary>Gets or sets the origin corner and axis direction of this page's geometry coordinates, when known.</summary>
    public OcrCoordinateOrigin? CoordinateOrigin { get; set; }

    /// <summary>Gets or sets the provider-native object underlying this page.</summary>
    /// <remarks>
    /// If an <see cref="OcrPage"/> is created to represent an underlying object from another object model, this
    /// property can store that original object. This can be useful for debugging or for enabling a consumer to
    /// access the underlying object model if needed. Because the page node rides through
    /// <see cref="OcrPageResultExtensions.ToOcrResult"/> reduction, provider-native page data set here survives
    /// into <see cref="OcrResult.Pages"/>.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the page.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
