// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = SpeechToTextClientExtensions.GetService<object>(null!);
        });
    }

    [Fact]
    public void GetTextAsync_InvalidArgs_Throws()
    {
        // Note: the extension method now requires a DataContent (not a string).
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = SpeechToTextClientExtensions.GetTextAsync(null!, new DataContent("data:audio/wav;base64,AQIDBA=="));
        });

        Assert.Throws<ArgumentNullException>("speechContent", () =>
        {
            _ = SpeechToTextClientExtensions.GetTextAsync(new TestSpeechToTextClient(), (DataContent)null!);
        });
    }

    [Fact]
    public void GetStreamingTextAsync_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            using var stream = new MemoryStream();
            _ = SpeechToTextClientExtensions.GetStreamingTextAsync(client: null!, new DataContent("data:audio/wav;base64,AQIDBA=="));
        });

        Assert.Throws<ArgumentNullException>("speechContent", () =>
        {
            _ = SpeechToTextClientExtensions.GetStreamingTextAsync(new TestSpeechToTextClient(), speechContent: null!);
        });
    }

    [Fact]
    public async Task GetStreamingTextAsync_CreatesTextMessageAsync()
    {
        // Arrange
        var expectedOptions = new SpeechToTextOptions();
        using var cts = new CancellationTokenSource();

        using TestSpeechToTextClient client = new()
        {
            GetStreamingResponseAsyncCallback = (speechContents, options, cancellationToken) =>
            {
                Assert.Single(speechContents);

                // For testing, return an async enumerable yielding one streaming update with text "world".
                return YieldAsync(new SpeechToTextResponseUpdate { Text = "world" });
            },
        };

        int count = 0;
        await foreach (var update in SpeechToTextClientExtensions.GetStreamingTextAsync(
            client,
            new DataContent("data:audio/wav;base64,AQIDBA=="),
            expectedOptions,
            cts.Token))
        {
            Assert.Equal(0, count);
            Assert.Equal("world", update.Text);
            count++;
        }

        Assert.Equal(1, count);
    }

    private static async IAsyncEnumerable<SpeechToTextResponseUpdate> YieldAsync(params SpeechToTextResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}
