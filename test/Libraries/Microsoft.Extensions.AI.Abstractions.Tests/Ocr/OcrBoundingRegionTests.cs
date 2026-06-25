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
        Assert.Equal(new float[] { 10, 20, 110, 20, 110, 220, 10, 220 }, region.Polygon);
    }

    [Fact]
    public void GetBounds_ReturnsAxisAlignedExtents()
    {
        var region = new OcrBoundingRegion(1, [30, 40, 100, 35, 110, 90, 25, 95]);

        var (left, top, right, bottom) = region.GetBounds();

        Assert.Equal(25, left);
        Assert.Equal(35, top);
        Assert.Equal(110, right);
        Assert.Equal(95, bottom);
    }
}
