// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class DocumentBoundingRegionTests
{
    [Fact]
    public void Constructor_NullPolygon_Throws()
    {
        Assert.Throws<ArgumentNullException>("polygon", () => new DocumentBoundingRegion(1, null!));
    }

    [Fact]
    public void FromRectangle_ProducesClockwiseQuadrilateral()
    {
        var region = DocumentBoundingRegion.FromRectangle(2, left: 10, top: 20, right: 110, bottom: 220);

        Assert.Equal(2, region.PageNumber);
        Assert.Equal(new[] { new DocumentPoint(10, 20), new DocumentPoint(110, 20), new DocumentPoint(110, 220), new DocumentPoint(10, 220) }, region.Polygon);
    }

    [Fact]
    public void GetBounds_ReturnsAxisAlignedExtents()
    {
        var region = new DocumentBoundingRegion(1, [new DocumentPoint(30, 40), new DocumentPoint(100, 35), new DocumentPoint(110, 90), new DocumentPoint(25, 95)]);

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
        var region = new DocumentBoundingRegion(1, []);

        Assert.Null(region.GetBounds());
    }
}
