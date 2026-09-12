// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Describes the unit in which OCR geometry coordinates (<see cref="DocumentPoint"/> and
/// <see cref="DocumentBoundingBox"/>) are expressed.
/// </summary>
/// <remarks>
/// Coordinate conventions differ across OCR engines: some report pixels of the rendered page image,
/// some report a physical unit such as points or inches, and some normalize to the page. The unit is
/// reported per page on <see cref="DocumentPage.CoordinateUnit"/>, paired with an
/// <see cref="DocumentCoordinateOrigin"/> and the page dimensions (<see cref="DocumentPageDimensions"/>), so a
/// consumer can interpret or normalize regions with engine-agnostic code. It is per page because engines
/// can emit different units for different pages of one document (for example, a batch mixing image and
/// PDF inputs). Unlike the taxonomy kinds (<see cref="DocumentBlockKind"/>, <see cref="DocumentTableCellKind"/>),
/// the set of coordinate units is physically bounded, so it is modeled as a closed enumeration.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public enum DocumentCoordinateUnit
{
    /// <summary>Coordinates expressed in pixels of the rendered page image.</summary>
    Pixel,

    /// <summary>Coordinates expressed in points (1/72 inch), the native unit of PDF content.</summary>
    Point,

    /// <summary>Coordinates expressed in inches.</summary>
    Inch,

    /// <summary>Coordinates normalized to the range [0, 1] relative to the page width and height.</summary>
    Normalized,
}
