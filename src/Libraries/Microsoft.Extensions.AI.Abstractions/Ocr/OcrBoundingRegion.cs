// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a positioned region on a page.</summary>
/// <remarks>
/// The region is a polygon (a clockwise sequence of <see cref="OcrPoint"/> vertices) so it can
/// faithfully carry a possibly rotation-skewed quadrilateral, such as Azure Document Intelligence's
/// <c>BoundingRegion.Polygon</c>, without loss. Engines that emit only an axis-aligned rectangle
/// (such as Mistral OCR) can convert via <see cref="FromRectangle"/>. The same type is reused for
/// layout-block geometry and for field grounding, providing one region primitive across providers.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrBoundingRegion
{
    /// <summary>Initializes a new instance of the <see cref="OcrBoundingRegion"/> class.</summary>
    /// <param name="pageNumber">The one-based page number the region is on.</param>
    /// <param name="polygon">The clockwise polygon vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="polygon"/> is <see langword="null"/>.</exception>
    public OcrBoundingRegion(int pageNumber, IReadOnlyList<OcrPoint> polygon)
    {
        PageNumber = pageNumber;
        Polygon = Throw.IfNull(polygon);
    }

    /// <summary>Gets the one-based page number the region is on.</summary>
    /// <remarks>A region can reference a different page than its parent element.</remarks>
    public int PageNumber { get; }

    /// <summary>Gets the polygon vertices, in clockwise order.</summary>
    /// <remarks>An Azure Document Intelligence quadrilateral is four points.</remarks>
    public IReadOnlyList<OcrPoint> Polygon { get; }

    /// <summary>Builds a clockwise quadrilateral region from an axis-aligned rectangle.</summary>
    /// <param name="pageNumber">The one-based page number the region is on.</param>
    /// <param name="left">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="right">The right coordinate.</param>
    /// <param name="bottom">The bottom coordinate.</param>
    /// <returns>A region whose polygon is the four corners of the rectangle.</returns>
    public static OcrBoundingRegion FromRectangle(int pageNumber, double left, double top, double right, double bottom)
        => new(pageNumber,
        [
            new OcrPoint((float)left, (float)top),
            new OcrPoint((float)right, (float)top),
            new OcrPoint((float)right, (float)bottom),
            new OcrPoint((float)left, (float)bottom),
        ]);

    /// <summary>Computes the axis-aligned bounds of the polygon.</summary>
    /// <returns>The axis-aligned bounds, or <see langword="default"/> when the polygon has no vertices.</returns>
    public OcrBoundingBox GetBounds()
    {
        if (Polygon.Count == 0)
        {
            return default;
        }

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (OcrPoint point in Polygon)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        return new OcrBoundingBox(minX, minY, maxX, maxY);
    }
}
