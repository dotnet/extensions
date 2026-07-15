// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrResponseUpdateExtensionsTests
{
    [Fact]
    public void ToOcrResult_NullUpdates_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((IEnumerable<OcrResponseUpdate>)null!).ToOcrResult());
    }

    [Fact]
    public async Task ToOcrResultAsync_NullUpdates_ThrowsAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("updates", () => ((IAsyncEnumerable<OcrResponseUpdate>)null!).ToOcrResultAsync());
    }

    [Fact]
    public void ToOcrResult_AssemblesPagesModelIdAndUsage()
    {
        OcrResponseUpdate[] updates =
        [
            new(new OcrPage(1, "page one")),
            new() { PagesProcessed = 1, TotalPages = 2, Status = "processing" },
            new(new OcrPage(2, "page two")) { ModelId = "model-x", Usage = new() { PagesProcessed = 2 } },
        ];

        OcrResult result = updates.ToOcrResult();

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("page one\n\npage two", result.Markdown);
        Assert.Equal("model-x", result.ModelId);
        Assert.NotNull(result.Usage);
        Assert.Equal(2, result.Usage!.PagesProcessed);
    }

    [Fact]
    public async Task ToOcrResultAsync_AssemblesPagesModelIdAndUsageAsync()
    {
        OcrResponseUpdate[] updates =
        [
            new(new OcrPage(1, "page one")),
            new(new OcrPage(2, "page two")) { ModelId = "model-y" },
        ];

        OcrResult result = await YieldAsync(updates).ToOcrResultAsync();

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("page one\n\npage two", result.Markdown);
        Assert.Equal("model-y", result.ModelId);
    }

    [Fact]
    public void ToOcrResult_MergesAdditionalProperties()
    {
        OcrResponseUpdate[] updates =
        [
            new(new OcrPage(1, "page one")) { AdditionalProperties = new() { ["a"] = "1" } },
            new(new OcrPage(2, "page two")) { AdditionalProperties = new() { ["b"] = "2" } },
        ];

        OcrResult result = updates.ToOcrResult();

        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal("1", result.AdditionalProperties!["a"]);
        Assert.Equal("2", result.AdditionalProperties!["b"]);
    }

    private static async IAsyncEnumerable<OcrResponseUpdate> YieldAsync(IEnumerable<OcrResponseUpdate> updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
