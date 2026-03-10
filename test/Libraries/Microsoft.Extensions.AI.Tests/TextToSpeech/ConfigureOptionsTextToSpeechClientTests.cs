// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsTextToSpeechClientTests
{
    [Fact]
    public void ConfigureOptionsTextToSpeechClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsTextToSpeechClient(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsTextToSpeechClient(new TestTextToSpeechClient(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerClient = new TestTextToSpeechClient();
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        TextToSpeechOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        TextToSpeechOptions? returnedOptions = null;
        TextToSpeechResponse expectedResponse = new([]);
        var expectedUpdates = Enumerable.Range(0, 3).Select(i => new TextToSpeechResponseUpdate()).ToArray();
        using CancellationTokenSource cts = new();

        using ITextToSpeechClient ttsInnerClient = new TestTextToSpeechClient
        {
            GetAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            },

            GetStreamingAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return YieldUpdates(expectedUpdates);
            },
        };

        using var client = ttsInnerClient
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

        var response = await client.GetAudioAsync("Hello, world!", providedOptions, cts.Token);
        Assert.Same(expectedResponse, response);

        int i = 0;
        await using var e = client.GetStreamingAudioAsync("Hello, world!", providedOptions, cts.Token).GetAsyncEnumerator();
        while (i < expectedUpdates.Length)
        {
            Assert.True(await e.MoveNextAsync());
            Assert.Same(expectedUpdates[i++], e.Current);
        }

        Assert.False(await e.MoveNextAsync());

        static async IAsyncEnumerable<TextToSpeechResponseUpdate> YieldUpdates(TextToSpeechResponseUpdate[] updates)
        {
            foreach (var update in updates)
            {
                await Task.Yield();
                yield return update;
            }
        }
    }
}
