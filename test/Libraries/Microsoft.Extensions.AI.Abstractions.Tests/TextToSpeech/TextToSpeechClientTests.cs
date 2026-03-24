// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechClientTests
{
    [Fact]
    public async Task GetAudioAsync_CreatesAudioResponseAsync()
    {
        // Arrange
        var expectedOptions = new TextToSpeechOptions();
        using var cts = new CancellationTokenSource();

        using TestTextToSpeechClient client = new()
        {
            GetAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                return Task.FromResult(new TextToSpeechResponse([new DataContent(new byte[] { 1, 2, 3 }, "audio/mpeg")]));
            },
        };

        // Act
        TextToSpeechResponse response = await client.GetAudioAsync("Hello, world!", expectedOptions, cts.Token);

        // Assert
        Assert.Single(response.Contents);
        Assert.IsType<DataContent>(response.Contents[0]);
        Assert.Equal("audio/mpeg", ((DataContent)response.Contents[0]).MediaType);
    }

    [Fact]
    public async Task GetStreamingAudioAsync_CreatesStreamingUpdatesAsync()
    {
        // Arrange
        var expectedOptions = new TextToSpeechOptions();
        using var cts = new CancellationTokenSource();

        using TestTextToSpeechClient client = new()
        {
            GetStreamingAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                return GetStreamingUpdatesAsync();
            },
        };

        // Act
        List<TextToSpeechResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingAudioAsync("Hello!", expectedOptions, cts.Token))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Equal(3, updates.Count);
        Assert.IsType<DataContent>(updates[0].Contents[0]);
        Assert.IsType<DataContent>(updates[1].Contents[0]);
        Assert.IsType<DataContent>(updates[2].Contents[0]);
    }

    private static async IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingUpdatesAsync()
    {
        yield return new([new DataContent(new byte[] { 1 }, "audio/mpeg")]);
        yield return new([new DataContent(new byte[] { 2 }, "audio/mpeg")]);
        yield return new([new DataContent(new byte[] { 3 }, "audio/mpeg")]);
    }
}
