// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrBoundingRegionTests
{
    [Fact]
    public void Constructor_NullPolygon_Throws()
    {
        Assert.Throws<ArgumentNullException>("polygon", () => new OcrBoundingRegion(1, null!));
    }

    [Fact]
    public void FromRectangle_ProducesClockwiseQuadrilateral()
    {
        var region = OcrBoundingRegion.FromRectangle(2, left: 10, top: 20, right: 110, bottom: 220);

        Assert.Equal(2, region.PageNumber);
        Assert.Equal(new[] { new OcrPoint(10, 20), new OcrPoint(110, 20), new OcrPoint(110, 220), new OcrPoint(10, 220) }, region.Polygon);
    }

    [Fact]
    public void GetBounds_ReturnsAxisAlignedExtents()
    {
        var region = new OcrBoundingRegion(1, [new OcrPoint(30, 40), new OcrPoint(100, 35), new OcrPoint(110, 90), new OcrPoint(25, 95)]);

        var bounds = region.GetBounds();

        Assert.NotNull(bounds);
        var (left, top, right, bottom) = bounds.Value;

        Assert.Equal(25, left);
        Assert.Equal(35, top);
        Assert.Equal(110, right);
        Assert.Equal(95, bottom);
    }

    [Fact]
    public void GetBounds_EmptyPolygon_ReturnsNull()
    {
        var region = new OcrBoundingRegion(1, []);

        Assert.Null(region.GetBounds());
    }
}
