// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    public async Task GetTextAsync_InvalidArgs_Throws()
    {
        // Note: the extension method now requires a DataContent (not a string).
        ISpeechToTextClient? client = null;
        var content = new DataContent("data:audio/wav;base64,AQIDBA==");
        var ex1 = await Assert.ThrowsAsync<ArgumentNullException>(() => SpeechToTextClientExtensions.GetTextAsync(client!, content));
        Assert.Equal("client", ex1.ParamName);

        using var testClient = new TestSpeechToTextClient();
        DataContent? nullContent = null;
        var ex2 = await Assert.ThrowsAsync<ArgumentNullException>(() => SpeechToTextClientExtensions.GetTextAsync(testClient, nullContent!));
        Assert.Equal("audioSpeechContent", ex2.ParamName);
    }

    [Fact]
    public async Task GetStreamingTextAsync_InvalidArgs_Throws()
    {
        ISpeechToTextClient? client = null;
        var content = new DataContent("data:audio/wav;base64,AQIDBA==");
        var ex1 = await Assert.ThrowsAsync<ArgumentNullException>(() => SpeechToTextClientExtensions.GetStreamingTextAsync(client!, content).GetAsyncEnumerator().MoveNextAsync().AsTask());
        Assert.Equal("client", ex1.ParamName);

        using var testClient = new TestSpeechToTextClient();
        DataContent? nullContent = null;
        var ex2 = await Assert.ThrowsAsync<ArgumentNullException>(() => SpeechToTextClientExtensions.GetStreamingTextAsync(testClient, nullContent!).GetAsyncEnumerator().MoveNextAsync().AsTask());
        Assert.Equal("audioSpeechContent", ex2.ParamName);
    }

    [Fact]
    public async Task GetStreamingTextAsync_CreatesTextMessageAsync()
    {
        // Arrange
        var expectedOptions = new SpeechToTextOptions();
        using var cts = new CancellationTokenSource();

        using TestSpeechToTextClient client = new()
        {
            GetStreamingTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                // For testing, return an async enumerable yielding one streaming update with text "world".
                var update = new SpeechToTextResponseUpdate();
                update.Contents.Add(new TextContent("world"));
                return YieldAsync(update);
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
