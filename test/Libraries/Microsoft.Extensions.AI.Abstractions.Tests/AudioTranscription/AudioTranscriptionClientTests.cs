// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionClientTests
{
    [Fact]
    public async Task TranscribeAsync_CreatesTextMessageAsync()
    {
        // Arrange
        // We simulate a transcription result by returning an AudioTranscriptionResponse built from an AudioTranscription.
        var expectedResponse = new AudioTranscriptionResponse(new AudioTranscription("hello"));
        var expectedOptions = new AudioTranscriptionOptions();
        using var cts = new CancellationTokenSource();

        using TestAudioTranscriptionClient client = new()
        {
            TranscribeAsyncCallback = (audioContents, options, cancellationToken) =>
            {
                // In our simulated client, we expect a single async enumerable.
                Assert.Single(audioContents);

                // For the purpose of the test, we assume that the underlying implementation converts the DataContent into a transcription choice.
                // (In a real implementation, the audio data would be processed.)
                // Here, we simply return an AudioTranscription with the text "hello".
                AudioTranscription choice = new("hello");
                return Task.FromResult(new AudioTranscriptionResponse(choice));
            },
        };

        // Act – call the extension method with a valid DataContent.
        AudioTranscriptionResponse response = await AudioTranscriptionClientExtensions.TranscribeAsync(
            client,
            new DataContent("data:,hello"),
            expectedOptions,
            cts.Token);

        // Assert
        Assert.Same(expectedResponse.AudioTranscription.Text, response.AudioTranscription.Text);
    }
}
