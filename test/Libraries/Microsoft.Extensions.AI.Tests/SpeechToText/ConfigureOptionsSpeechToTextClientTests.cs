// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsSpeechToTextClientTests
{
    [Fact]
    public void ConfigureOptionsSpeechToTextClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsSpeechToTextClient(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsSpeechToTextClient(new TestSpeechToTextClient(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerClient = new TestSpeechToTextClient();
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        SpeechToTextOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        SpeechToTextOptions? returnedOptions = null;
        SpeechToTextResponse expectedResponse = new([]);
        var expectedUpdates = Enumerable.Range(0, 3).Select(i => new SpeechToTextResponseUpdate()).ToArray();
        using CancellationTokenSource cts = new();

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            },

            GetStreamingTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return YieldUpdates(expectedUpdates);
            },
        };

        using var client = innerClient
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                Assert.NotSame(providedOptions, options);
                if (nullProvidedOptions)
                {
                    Assert.Null(options.ModelId);
                }
                else
                {
                    Assert.Equal(providedOptions!.ModelId, options.ModelId);
                }

                returnedOptions = options;
            })
            .Build();

        using var audioSpeechStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var response = await client.GetTextAsync(audioSpeechStream, providedOptions, cts.Token);
        Assert.Same(expectedResponse, response);

        int i = 0;
        using var audioSpeechStream2 = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        await using var e = client.GetStreamingTextAsync(audioSpeechStream2, providedOptions, cts.Token).GetAsyncEnumerator();
        while (i < expectedUpdates.Length)
        {
            Assert.True(await e.MoveNextAsync());
            Assert.Same(expectedUpdates[i++], e.Current);
        }

        Assert.False(await e.MoveNextAsync());

        static async IAsyncEnumerable<SpeechToTextResponseUpdate> YieldUpdates(SpeechToTextResponseUpdate[] updates)
        {
            foreach (var update in updates)
            {
                await Task.Yield();
                yield return update;
            }
        }
    }
}
