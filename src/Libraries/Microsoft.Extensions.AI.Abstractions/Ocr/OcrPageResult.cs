// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a single streaming update from an <see cref="IOcrClient"/>.</summary>
/// <remarks>
/// <para>
/// An <see cref="IOcrClient.ExtractPagesAsync"/> request produces a sequence of
/// <see cref="OcrPageResult"/> instances, one per page as that page finishes (carrying the completed
/// <see cref="Page"/>). Progress rides along on each result via <see cref="PagesProcessed"/> and
/// <see cref="TotalPages"/> for long-running operations such as Azure Document Intelligence. Completion is
/// signaled by the end of the sequence.
/// </para>
/// <para>
/// The relationship between <see cref="OcrResult"/> and <see cref="OcrPageResult"/> is codified in
/// <see cref="OcrPageResultExtensions.ToOcrResultAsync"/>, which reassembles a stream of updates into a
/// single <see cref="OcrResult"/>. The conversion can be slightly lossy: for example, only one
/// <see cref="RawRepresentation"/> slot is available on the assembled <see cref="OcrResult"/>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrPageResult
{
    /// <summary>Initializes a new instance of the <see cref="OcrPageResult"/> class with the page completed in this update.</summary>
    /// <param name="page">The page produced in this update.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="page"/> is <see langword="null"/>.</exception>
    [JsonConstructor]
    public OcrPageResult(OcrPage page)
    {
        Page = Throw.IfNull(page);
    }

    /// <summary>Gets the page produced in this update.</summary>
    public OcrPage Page { get; }

    /// <summary>Gets or sets the number of pages processed so far, when known.</summary>
    public int? PagesProcessed { get; set; }

    /// <summary>Gets or sets the total number of pages, when known.</summary>
    public int? TotalPages { get; set; }

    /// <summary>Gets or sets the unit in which this document's geometry coordinates are expressed, when known.</summary>
    /// <remarks>
    /// Reported at the document level; the reducer (<see cref="OcrPageResultExtensions.ToOcrResult"/>) carries
    /// the last non-<see langword="null"/> value onto <see cref="OcrResult.CoordinateUnit"/>.
    /// </remarks>
    public OcrCoordinateUnit? CoordinateUnit { get; set; }

    /// <summary>Gets or sets the origin corner and axis direction of this document's geometry coordinates, when known.</summary>
    public OcrCoordinateOrigin? CoordinateOrigin { get; set; }

    /// <summary>Gets or sets usage details associated with the request, when reported.</summary>
    /// <remarks>Usage is typically carried on a terminal update once the full document has been processed.</remarks>
    public OcrUsage? Usage { get; set; }

    /// <summary>Gets or sets the provider-native object underlying this update.</summary>
    /// <remarks>
    /// If an <see cref="OcrPageResult"/> is created to represent an underlying object from another object
    /// model, this property can store that original object. This can be useful for debugging or for enabling
    /// a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
