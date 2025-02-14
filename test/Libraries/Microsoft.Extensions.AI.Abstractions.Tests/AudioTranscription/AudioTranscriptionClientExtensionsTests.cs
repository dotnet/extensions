// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = AudioTranscriptionClientExtensions.GetService<object>(null!);
        });
    }

    [Fact]
    public void TranscribeAsync_InvalidArgs_Throws()
    {
        // Note: the extension method now requires a DataContent (not a string).
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = AudioTranscriptionClientExtensions.TranscribeAsync(null!, new DataContent("data:,hello"));
        });

        Assert.Throws<ArgumentNullException>("audioContent", () =>
        {
            _ = AudioTranscriptionClientExtensions.TranscribeAsync(new TestAudioTranscriptionClient(), (DataContent)null!);
        });
    }

    [Fact]
    public void TranscribeStreamingAsync_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            using var stream = new MemoryStream();
            _ = AudioTranscriptionClientExtensions.TranscribeStreamingAsync(client: null!, new DataContent("data:,hello"));
        });

        Assert.Throws<ArgumentNullException>("audioContent", () =>
        {
            _ = AudioTranscriptionClientExtensions.TranscribeStreamingAsync(new TestAudioTranscriptionClient(), audioContent: null!);
        });
    }

    [Fact]
    public async Task TranscribeStreamingAsync_CreatesTextMessageAsync()
    {
        // Arrange
        var expectedOptions = new AudioTranscriptionOptions();
        using var cts = new CancellationTokenSource();

        using TestAudioTranscriptionClient client = new()
        {
            TranscribeStreamingAsyncCallback = (audioContents, options, cancellationToken) =>
            {
                Assert.Single(audioContents);

                // For testing, return an async enumerable yielding one streaming update with text "world".
                return YieldAsync(new AudioTranscriptionResponseUpdate { Text = "world" });
            },
        };

        int count = 0;
        await foreach (var update in AudioTranscriptionClientExtensions.TranscribeStreamingAsync(
            client,
            new DataContent("data:,hello"),
            expectedOptions,
            cts.Token))
        {
            Assert.Equal(0, count);
            Assert.Equal("world", update.Text);
            count++;
        }

        Assert.Equal(1, count);
    }

    private static async IAsyncEnumerable<AudioTranscriptionResponseUpdate> YieldAsync(params AudioTranscriptionResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}
