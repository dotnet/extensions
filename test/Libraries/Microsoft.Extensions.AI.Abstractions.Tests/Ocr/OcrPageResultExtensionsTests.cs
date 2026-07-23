// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrPageResultExtensionsTests
{
    [Fact]
    public void ToOcrResult_NullUpdates_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((IEnumerable<OcrPageResult>)null!).ToOcrResult());
    }

    [Fact]
    public async Task ToOcrResultAsync_NullUpdates_ThrowsAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("updates", () => ((IAsyncEnumerable<OcrPageResult>)null!).ToOcrResultAsync());
    }

    [Fact]
    public void ToOcrResult_AssemblesPagesAndUsage()
    {
        OcrPageResult[] updates =
        [
            new(new OcrPage(1, "page one")) { PagesProcessed = 1, TotalPages = 2 },
            new(new OcrPage(2, "page two")) { Usage = new() { PagesProcessed = 2 } },
        ];

        OcrResult result = updates.ToOcrResult();

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("page one\n\npage two", result.Text);
        Assert.NotNull(result.Usage);
        Assert.Equal(2, result.Usage!.PagesProcessed);
    }

    [Fact]
    public async Task ToOcrResultAsync_AssemblesPagesAndUsageAsync()
    {
        OcrPageResult[] updates =
        [
            new(new OcrPage(1, "page one")),
            new(new OcrPage(2, "page two")),
        ];

        OcrResult result = await YieldAsync(updates).ToOcrResultAsync();

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("page one\n\npage two", result.Text);
    }

    [Fact]
    public void ToOcrResult_MergesAdditionalProperties()
    {
        OcrPageResult[] updates =
        [
            new(new OcrPage(1, "page one")) { AdditionalProperties = new() { ["a"] = "1" } },
            new(new OcrPage(2, "page two")) { AdditionalProperties = new() { ["b"] = "2" } },
        ];

        OcrResult result = updates.ToOcrResult();

        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal("1", result.AdditionalProperties!["a"]);
        Assert.Equal("2", result.AdditionalProperties!["b"]);
    }

    [Fact]
    public void ToOcrResult_PropagatesCoordinateMetadata_LastNonNullWins()
    {
        OcrPageResult[] updates =
        [
            new(new OcrPage(1, "page one")) { CoordinateUnit = OcrCoordinateUnit.Pixel, CoordinateOrigin = OcrCoordinateOrigin.TopLeft },
            new(new OcrPage(2, "page two")),
            new(new OcrPage(3, "page three")) { CoordinateUnit = OcrCoordinateUnit.Point },
        ];

        OcrResult result = updates.ToOcrResult();

        Assert.Equal(OcrCoordinateUnit.Point, result.CoordinateUnit);
        Assert.Equal(OcrCoordinateOrigin.TopLeft, result.CoordinateOrigin);
    }

    private static async IAsyncEnumerable<OcrPageResult> YieldAsync(IEnumerable<OcrPageResult> updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
