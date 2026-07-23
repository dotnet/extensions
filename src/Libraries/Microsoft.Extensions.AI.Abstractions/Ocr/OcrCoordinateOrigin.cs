// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes the origin corner and vertical axis direction of an OCR coordinate space.
/// </summary>
/// <remarks>
/// Engines disagree on where a page's coordinate origin sits and which way the y axis grows: rasterized
/// page images place the origin at the top-left with y increasing downward, whereas PDF-native
/// (point) coordinates place it at the bottom-left with y increasing upward. The origin is reported once
/// at the document level on <see cref="OcrResult"/> and <see cref="OcrPageResult"/>, alongside
/// <see cref="OcrCoordinateUnit"/>, so bounding regions from different engines can be compared and
/// normalized without guessing the convention.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public enum OcrCoordinateOrigin
{
    /// <summary>The origin sits at the top-left corner, with the y axis increasing downward. The convention for rasterized page images.</summary>
    TopLeft,

    /// <summary>The origin sits at the bottom-left corner, with the y axis increasing upward. The convention for PDF-native point coordinates.</summary>
    BottomLeft,
}
