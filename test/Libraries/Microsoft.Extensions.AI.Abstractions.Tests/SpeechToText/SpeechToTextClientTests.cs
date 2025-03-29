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
        var expectedResponse = new SpeechToTextResponse(new SpeechToTextMessage("hello"));
        var expectedOptions = new SpeechToTextOptions();
        using var cts = new CancellationTokenSource();

        using TestSpeechToTextClient client = new()
        {
            GetResponseAsyncCallback = (speechContents, options, cancellationToken) =>
            {
                // In our simulated client, we expect a single async enumerable.
                Assert.Single(speechContents);

                // For the purpose of the test, we assume that the underlying implementation converts the DataContent into a transcription choice.
                // (In a real implementation, the speech audio data would be processed.)
                SpeechToTextMessage choice = new("hello");
                return Task.FromResult(new SpeechToTextResponse(choice));
            },
        };

        // Act – call the extension method with a valid DataContent.
        SpeechToTextResponse response = await SpeechToTextClientExtensions.GetTextAsync(
            client,
            new DataContent("data:audio/wav;base64,AQIDBA=="),
            expectedOptions,
            cts.Token);

        // Assert
        Assert.Same(expectedResponse.Message.Text, response.Message.Text);
    }
}
