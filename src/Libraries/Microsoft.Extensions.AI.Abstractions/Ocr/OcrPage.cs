// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>Gets or sets the page width, expressed in the document-level <see cref="OcrResult.CoordinateUnit"/>, when the engine provides it.</summary>
    /// <remarks>
    /// Together with <see cref="Height"/> and the document-level <see cref="OcrResult.CoordinateUnit"/> and
    /// <see cref="OcrResult.CoordinateOrigin"/>, this lets a consumer interpret or normalize the geometry
    /// (<see cref="OcrBoundingBox"/> / <see cref="OcrPoint"/>) on this page with engine-agnostic code. For
    /// example, dividing a coordinate by the corresponding page dimension yields a page-relative [0, 1] value
    /// regardless of the native unit.
    /// </remarks>
    public float? Width { get; set; }

    /// <summary>Gets or sets the page height, expressed in the document-level <see cref="OcrResult.CoordinateUnit"/>, when the engine provides it.</summary>
    /// <remarks>See <see cref="Width"/> for how the page dimensions are used with the document-level coordinate unit.</remarks>
    public float? Height { get; set; }

    /// <summary>Gets or sets any additional properties associated with the page.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
