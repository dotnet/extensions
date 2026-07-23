// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the structured result of an OCR / document-parsing request.</summary>
/// <remarks>
/// The result normalizes the content common to every engine (text, pages, tables, bounding
/// regions) while preserving everything provider-specific via
/// <see cref="RawRepresentation"/> and <see cref="AdditionalProperties"/>, mirroring how
/// <c>ChatResponse</c> normalizes the common surface and preserves the raw.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrResult
{
    /// <summary>Initializes a new instance of the <see cref="OcrResult"/> class.</summary>
    /// <param name="pages">The per-page structured content.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="pages"/> is <see langword="null"/>.</exception>
    public OcrResult(IReadOnlyList<OcrPage> pages)
    {
        Pages = Throw.IfNull(pages);
    }

    /// <summary>Gets the per-page structured content (text, tables, blocks).</summary>
    public IReadOnlyList<OcrPage> Pages { get; }

    /// <summary>Gets the full-document text, formed by joining the per-page text.</summary>
    public string Text => string.Join("\n\n", Pages.Select(p => p.Text));

    /// <summary>Gets or sets the unit in which this document's geometry coordinates are expressed, when known.</summary>
    /// <remarks>
    /// Applies to every <see cref="OcrBoundingRegion"/> in the document. When <see langword="null"/>, the
    /// geometry should be treated as an opaque, provider-specific coordinate space.
    /// </remarks>
    public OcrCoordinateUnit? CoordinateUnit { get; set; }

    /// <summary>Gets or sets the origin corner and axis direction of this document's geometry coordinates, when known.</summary>
    public OcrCoordinateOrigin? CoordinateOrigin { get; set; }

    /// <summary>Gets or sets usage details associated with the request.</summary>
    public OcrUsage? Usage { get; set; }

    /// <summary>Gets or sets the provider-native object underlying this result.</summary>
    /// <remarks>
    /// The escape hatch for provider richness that does not map onto the normalized surface, mirroring
    /// <c>ChatResponse.RawRepresentation</c>. Nothing is lost.
    /// </remarks>
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the result.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
