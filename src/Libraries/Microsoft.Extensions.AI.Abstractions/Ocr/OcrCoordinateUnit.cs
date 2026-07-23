// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes the unit in which OCR geometry coordinates (<see cref="OcrPoint"/> and
/// <see cref="OcrBoundingBox"/>) are expressed.
/// </summary>
/// <remarks>
/// Coordinate conventions differ across OCR engines: some report pixels of the rendered page image,
/// some report a physical unit such as points or inches, and some normalize to the page. The unit is
/// reported once at the document level on <see cref="OcrResult"/> and <see cref="OcrPageResult"/>,
/// paired with an <see cref="OcrCoordinateOrigin"/> and the page dimensions (<see cref="OcrPage.Width"/>
/// and <see cref="OcrPage.Height"/>), so a consumer can interpret or normalize regions with
/// engine-agnostic code. Unlike the taxonomy kinds (<see cref="OcrBlockKind"/>,
/// <see cref="OcrTableCellKind"/>), the set of coordinate units is physically bounded, so it is modeled
/// as a closed enumeration.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public enum OcrCoordinateUnit
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
