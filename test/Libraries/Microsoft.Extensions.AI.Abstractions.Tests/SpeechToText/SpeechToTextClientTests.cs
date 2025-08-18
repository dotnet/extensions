// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextClientTests
{
    [Fact]
    public async Task GetTextAsync_CreatesTextMessageAsync()
    {
        // Arrange
        var expectedResponse = new SpeechToTextResponse("hello");
        var expectedOptions = new SpeechToTextOptions();
        using var cts = new CancellationTokenSource();

        using TestSpeechToTextClient client = new()
        {
            GetTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                // For the purpose of the test, we assume that the underlying implementation converts the audio speech stream into a transcription choice.
                // (In a real implementation, the audio speech data would be processed.)
                return Task.FromResult(new SpeechToTextResponse("hello"));
            },
        };

        // Act – call the extension method with a valid DataContent.
        SpeechToTextResponse response = await SpeechToTextClientExtensions.GetTextAsync(
            client,
            new DataContent("data:audio/wav;base64,AQIDBA=="),
            expectedOptions,
            cts.Token);

        // Assert
        Assert.Equal(expectedResponse.Text, response.Text);
    }

    [Fact]
    public async Task GetStreamingTextAsync_CreatesStreamingUpdatesAsync()
    {
        // Arrange
        var expectedOptions = new SpeechToTextOptions();
        using var cts = new CancellationTokenSource();

        using TestSpeechToTextClient client = new()
        {
            GetStreamingTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                // For the purpose of the test, we simulate a streaming response with multiple updates
                return GetStreamingUpdatesAsync();
            },
        };

        // Act – call the extension method with a valid DataContent
        List<SpeechToTextResponseUpdate> updates = [];
        await foreach (var update in SpeechToTextClientExtensions.GetStreamingTextAsync(
            client,
            new DataContent("data:audio/wav;base64,AQIDBA=="),
            expectedOptions,
            cts.Token))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Equal(3, updates.Count);
        Assert.Equal("hello ", updates[0].Text);
        Assert.Equal("world ", updates[1].Text);
        Assert.Equal("!", updates[2].Text);
    }

    // Helper method to simulate streaming updates
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingUpdatesAsync()
    {
        yield return new("hello ");
        yield return new("world ");
        yield return new("!");
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
