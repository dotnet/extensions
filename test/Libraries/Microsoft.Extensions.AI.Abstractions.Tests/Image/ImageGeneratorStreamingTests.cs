// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGeneratorStreamingTests
{
    [Fact]
    public async Task GenerateStreamingImagesAsync_CallsCallback()
    {
        var expectedRequest = new ImageRequest("test prompt");
        var expectedOptions = new ImageOptions();
        using var cts = new CancellationTokenSource();
        var expectedUpdate = new ImageResponseUpdate();

        using var generator = new TestImageGenerator
        {
            GenerateStreamingImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return YieldUpdates(expectedUpdate);
            }
        };

        var updates = new List<ImageResponseUpdate>();
        await foreach (var update in generator.GenerateStreamingImagesAsync(expectedRequest, expectedOptions, cts.Token))
        {
            updates.Add(update);
        }

        Assert.Single(updates);
        Assert.Same(expectedUpdate, updates[0]);
    }

    [Fact]
    public async Task GenerateStreamingImagesAsync_NoCallback_ReturnsEmptySequence()
    {
        using var generator = new TestImageGenerator();
        var updates = new List<ImageResponseUpdate>();

        await foreach (var update in generator.GenerateStreamingImagesAsync(new ImageRequest("test prompt")))
        {
            updates.Add(update);
        }

        Assert.Empty(updates);
    }

    private static async IAsyncEnumerable<ImageResponseUpdate> YieldUpdates(params ImageResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}
