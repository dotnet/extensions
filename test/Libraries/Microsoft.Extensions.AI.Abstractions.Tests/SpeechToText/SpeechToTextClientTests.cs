// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
