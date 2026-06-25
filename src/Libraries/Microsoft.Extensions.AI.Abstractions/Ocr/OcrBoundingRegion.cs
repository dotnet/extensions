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
/// The region is a polygon (a flattened, clockwise sequence of <c>[x1, y1, x2, y2, ...]</c> vertices)
/// so it can faithfully carry a possibly rotation-skewed quadrilateral, such as Azure Document
/// Intelligence's <c>BoundingRegion.Polygon</c>, without loss. Engines that emit only an axis-aligned
/// rectangle (such as Mistral OCR) can convert via <see cref="FromRectangle"/>. The same type is reused
/// for layout-block geometry and for field grounding, providing one region primitive across providers.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrBoundingRegion
{
    /// <summary>Initializes a new instance of the <see cref="OcrBoundingRegion"/> class.</summary>
    /// <param name="pageNumber">The one-based page number the region is on.</param>
    /// <param name="polygon">The flattened, clockwise polygon vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="polygon"/> is <see langword="null"/>.</exception>
    public OcrBoundingRegion(int pageNumber, IReadOnlyList<float> polygon)
    {
        PageNumber = pageNumber;
        Polygon = Throw.IfNull(polygon);
    }

    /// <summary>Gets the one-based page number the region is on.</summary>
    /// <remarks>A region can reference a different page than its parent element.</remarks>
    public int PageNumber { get; }

    /// <summary>Gets the flattened polygon vertices <c>[x1, y1, x2, y2, ...]</c>, in clockwise order.</summary>
    /// <remarks>An Azure Document Intelligence quadrilateral is eight floats.</remarks>
    public IReadOnlyList<float> Polygon { get; }

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
            (float)left, (float)top,
            (float)right, (float)top,
            (float)right, (float)bottom,
            (float)left, (float)bottom,
        ]);

    /// <summary>Computes the axis-aligned bounds of the polygon.</summary>
    /// <returns>The minimum and maximum coordinates of the polygon.</returns>
    public (float Left, float Top, float Right, float Bottom) GetBounds()
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i + 1 < Polygon.Count; i += 2)
        {
            minX = Math.Min(minX, Polygon[i]);
            maxX = Math.Max(maxX, Polygon[i]);
            minY = Math.Min(minY, Polygon[i + 1]);
            maxY = Math.Max(maxY, Polygon[i + 1]);
        }

        return (minX, minY, maxX, maxY);
    }
}
